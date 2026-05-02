# Sprint 4 开发计划

**Sprint**: UI 打磨 + 删除功能  
**状态**: 🔄 开发中  
**日期**: 2026-05-02  
**开发者**: linfeng89

---

## 开发内容

### P0 核心功能（必须完成）

| 任务 | 优先级 | 预计工时 | 状态 | 说明 |
|------|--------|----------|------|------|
| 手动编辑转写文字 | P0 | 2h | ⏳ | 详情页 + 保存 |
| 音频播放器 UI | P0 | 3h | ⏳ | 播放/暂停/进度条 |
| 左滑删除 | P0 | 2h | ⏳ | 软删除 + 回收站 |
| 撤销按钮（3 秒） | P1 | 2h | ⏳ | 删除后 toast + 撤销 |
| 回收站页面完善 | P0 | 3h | ⏳ | 列表 + 恢复 + 永久删除 |
| 回收站自动清理 | P0 | 2h | ⏳ | 30 天自动物理删除 |
| 详情页 UI 完善 | P1 | 2h | ⏳ | 文字展示 + 音频回放 |
| 自定义日期范围选择器 | P1 | 2h | ⏳ | 从 Sprint 3 延后 |

### P1 改进功能（时间允许）

| 任务 | 优先级 | 预计工时 | 状态 | 说明 |
|------|--------|----------|------|------|
| 搜索历史记录 | P2 | 2h | ⏳ | 最近搜索记录 |
| 删除动画优化 | P2 | 2h | ⏳ | 流畅过渡动画 |
| 列表项点击动画 | P2 | 1h | ⏳ | 点击反馈 |
| 空状态动画 | P2 | 2h | ⏳ | Lottie 动画 |

---

## 技术设计

### 1. 手动编辑文字

**编辑页面**：
```csharp
// DiaryEditPage.xaml
<Editor Text="{Binding TranscribedText}" 
        AutoSize="TextChanges"
        Placeholder="输入或编辑转写文字..."
        HeightRequest="300"/>

<Button Text="保存" 
        Command="{Binding SaveCommand}"
        IsEnabled="{Binding HasChanges}"/>
```

**ViewModel**：
```csharp
public class DiaryEditViewModel : BaseViewModel
{
    private DiaryEntry _entry;
    
    [ObservableProperty]
    private string transcribedText;
    
    [ObservableProperty]
    private bool hasChanges;
    
    public SaveCommand => new AsyncRelayCommand(SaveAsync);
    
    private async Task SaveAsync()
    {
        entry.TranscribedText = TranscribedText;
        entry.UpdatedAt = DateTime.Now;
        await _databaseService.UpdateEntryAsync(entry);
        await Shell.Current.Navigation.PopAsync();
    }
}
```

---

### 2. 音频播放器 UI

**播放器控件**：
```csharp
// AudioPlayerView.xaml
<StackLayout>
    <!-- 进度条 -->
    <Slider Minimum="0" 
            Maximum="{Binding Duration}"
            Value="{Binding CurrentPosition}"
            DragCompleted="OnSeek"/>
    
    <!-- 控制按钮 -->
    <HorizontalStackLayout>
        <ImageButton Source="{Binding PlayPauseIcon}"
                     Command="{Binding PlayPauseCommand}"/>
        <Label Text="{Binding CurrentPosition, StringFormat='{0:mm\\:ss}'}"/>
        <Label Text="{Binding Duration, StringFormat='{0:mm\\:ss}'}"/>
    </HorizontalStackLayout>
</StackLayout>
```

**播放器服务**：
```csharp
public interface IAudioPlayerService
{
    void Load(string audioPath);
    void Play();
    void Pause();
    void SeekTo(double position);
    event EventHandler<PositionChangedEventArgs> PositionChanged;
    event EventHandler PlaybackCompleted;
}
```

---

### 3. 删除功能

**软删除流程**：
```csharp
public async Task DeleteEntryAsync(DiaryEntry entry)
{
    entry.IsDeleted = true;
    entry.DeletedAt = DateTime.Now;
    await _databaseService.UpdateEntryAsync(entry);
    
    // 从 FTS 索引移除
    await _searchService.RemoveFromIndexAsync(entry.Id);
    
    // 触发 30 天后自动清理
    ScheduleAutoCleanup(entry.Id, entry.DeletedAt.Value);
}
```

**回收站列表**：
```sql
SELECT * FROM DiaryEntry
WHERE IsDeleted = 1
AND DeletedAt > datetime('now', '-30 days')
ORDER BY DeletedAt DESC;
```

**恢复功能**：
```csharp
public async Task RestoreEntryAsync(DiaryEntry entry)
{
    entry.IsDeleted = false;
    entry.DeletedAt = null;
    await _databaseService.UpdateEntryAsync(entry);
    
    // 重新加入 FTS 索引
    if (entry.IsTranscribed)
    {
        await _searchService.AddToIndexAsync(entry);
    }
}
```

**永久删除**：
```csharp
public async Task PermanentlyDeleteAsync(DiaryEntry entry)
{
    // 删除音频文件
    if (File.Exists(entry.AudioFilePath))
    {
        File.Delete(entry.AudioFilePath);
    }
    
    // 从数据库删除
    await _databaseService.DeleteEntryAsync(entry);
    
    // 从 FTS 索引移除
    await _searchService.RemoveFromIndexAsync(entry.Id);
}
```

---

### 4. 撤销删除（3 秒）

**Toast + 撤销**：
```csharp
public async Task DeleteWithUndoAsync(DiaryEntry entry)
{
    // 先软删除
    await DeleteEntryAsync(entry);
    
    // 显示 Toast
    var undoTask = _toastService.ShowAsync(
        "已删除",
        "撤销",
        timeout: TimeSpan.FromSeconds(3)
    );
    
    var result = await undoTask;
    
    if (result == "撤销")
    {
        // 用户点击撤销
        await RestoreEntryAsync(entry);
    }
    else
    {
        // 超时或关闭，删除完成
        _logger.LogInformation($"Entry {entry.Id} permanently deleted");
    }
}
```

---

### 5. 自定义日期范围选择器

**日期选择弹窗**：
```csharp
// DatePickerDialog.xaml
<StackLayout>
    <DatePicker x:Name="StartDatePicker" 
                Date="{Binding StartDate}"
                MaximumDate="{Binding EndDate}"/>
    
    <DatePicker x:Name="EndDatePicker" 
                Date="{Binding EndDate}"
                MinimumDate="{Binding StartDate}"/>
    
    <Button Text="应用" 
            Command="{Binding ApplyCommand}"/>
    
    <Button Text="清除" 
            Command="{Binding ClearCommand}"/>
</StackLayout>
```

---

### 6. 搜索历史

**历史记录存储**：
```csharp
public class SearchHistory
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    public string Query { get; set; }
    public DateTime SearchedAt { get; set; }
    public int ResultCount { get; set; }
}
```

**保存历史**：
```csharp
public async Task SaveSearchHistoryAsync(string query, int resultCount)
{
    await _database.InsertOrReplaceAsync(new SearchHistory
    {
        Query = query,
        SearchedAt = DateTime.Now,
        ResultCount = resultCount
    });
    
    // 清理 30 天前的记录
    await _database.ExecuteAsync(
        "DELETE FROM SearchHistory WHERE SearchedAt < datetime('now', '-30 days')");
}
```

---

## 验收标准

### 手动编辑
- [ ] 详情页可以进入编辑模式
- [ ] 编辑后自动保存
- [ ] UpdatedAt 字段更新
- [ ] 编辑后同步更新 FTS 索引

### 音频播放
- [ ] 播放/暂停功能正常
- [ ] 进度条可以拖动
- [ ] 进度实时更新
- [ ] 播放完成后自动停止

### 删除功能
- [ ] 左滑显示删除按钮
- [ ] 删除后移入回收站
- [ ] 删除后显示撤销 Toast（3 秒）
- [ ] 回收站支持恢复
- [ ] 回收站支持永久删除
- [ ] 30 天前删除自动清理

### 详情页
- [ ] 文字展示清晰
- [ ] 音频播放器可见
- [ ] 编辑按钮可见
- [ ] 删除按钮可见

### 日期选择器
- [ ] 可以选择开始日期
- [ ] 可以选择结束日期
- [ ] 开始日期不能晚于结束日期
- [ ] 清除后恢复全部范围

### 搜索历史
- [ ] 显示最近搜索词
- [ ] 点击历史词快速搜索
- [ ] 30 天前记录自动清理
- [ ] 最多保留 50 条记录

---

## 开发步骤

### Day 1：删除功能 + 回收站

**上午**：
- [ ] 实现左滑删除 SwipeView
- [ ] 实现软删除逻辑
- [ ] 实现撤销删除 Toast
- [ ] 创建回收站页面

**下午**：
- [ ] 实现恢复功能
- [ ] 实现永久删除
- [ ] 实现 30 天自动清理
- [ ] 完善回收站 UI

### Day 2：详情页 + 播放器 + 其他

**上午**：
- [ ] 详情页 UI 完善
- [ ] 手动编辑功能
- [ ] 音频播放器控件

**下午**：
- [ ] 自定义日期范围选择器
- [ ] 搜索历史记录
- [ ] 删除动画优化
- [ ] 整体测试和修复

---

## 依赖关系

```
左滑删除
    └── 软删除逻辑
        ├── 撤销删除
        │   └── Toast 服务
        │
        └── 回收站
            ├── 恢复功能
            │   └── FTS 索引重新加入
            │
            └── 永久删除
                └── 音频文件删除

详情页完善
    ├── 手动编辑
    │   └── FTS 索引更新
    │
    └── 音频播放器
        └── 播放控制服务

P1 功能
    ├── 自定义日期选择器
    │   └── 搜索页面集成
    │
    └── 搜索历史
        └── 历史记录表 +UI
```

---

## 风险评估

| 风险 | 影响 | 概率 | 应对 |
|------|------|------|------|
| 音频播放进度同步 | 中 | 中 | 使用定时器 + 事件 |
| 删除撤销时序 | 低 | 低 | TaskCompletionSource |
| FTS 索引一致性 | 高 | 低 | 事务保证 |
| 回收站性能 | 低 | 低 | 索引优化 |

---

## 交付物

- ✅ DiaryEditPage.xaml/cs - 编辑页面
- ✅ AudioPlayerView.xaml/cs - 播放器控件
- ✅ TrashPage.xaml/cs - 回收站
- ✅ DatePickerDialog.xaml/cs - 日期选择器
- ✅ ToastService.cs - Toast 服务
- ✅ SearchHistory 表 + 服务
- ✅ 自动清理后台任务

---

**文档版本**: v1.0  
**创建时间**: 2026-05-02  
**最后更新**: 2026-05-02
