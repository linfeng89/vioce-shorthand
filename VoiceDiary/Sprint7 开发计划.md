# Sprint 7 开发计划

**Sprint**: 导出/备份功能  
**状态**: 🔄 开发中  
**日期**: 2026-05-03  
**开发者**: linfeng89

---

## 开发目标

实现日记导出和数据备份功能，支持单篇导出和批量导出多种格式。

---

## 开发内容

### P0 核心功能（必须完成）

| 任务 | 优先级 | 预计工时 | 状态 | 说明 |
|------|--------|----------|------|------|
| 导出服务接口 | P0 | 1h | ⏳ | IExportService |
| TXT 格式导出 | P0 | 1h | ⏳ | 纯文本格式 |
| Markdown 格式导出 | P0 | 1h | ⏳ | 带格式的 MD |
| JSON 格式导出 | P0 | 1h | ⏳ | 完整数据结构 |
| 单篇导出 UI | P0 | 2h | ⏳ | 详情页导出按钮 |
| 批量导出 UI | P0 | 3h | ⏳ | 多选导出 |
| 分享到其他应用 | P0 | 2h | ⏳ | Android/iOS 分享 |

### P1 增强功能

| 任务 | 优先级 | 预计工时 | 状态 | 说明 |
|------|--------|----------|------|------|
| 备份到 ZIP | P1 | 2h | ⏳ | 压缩全部数据 |
| 恢复备份 | P1 | 3h | ⏳ | 从 ZIP 恢复 |
| 导出设置页 | P1 | 1h | ⏳ | 默认格式配置 |

---

## 技术设计

### 1. 导出服务接口

```csharp
public interface IExportService
{
    Task<string> ExportToTextAsync(DiaryEntry entry);
    Task<string> ExportToMarkdownAsync(DiaryEntry entry);
    Task<string> ExportToJsonAsync(DiaryEntry entry);
    Task<string> ExportMultipleAsync(IEnumerable<DiaryEntry> entries, ExportFormat format);
    Task ShareAsync(string content, string title);
}

public enum ExportFormat
{
    Text,
    Markdown,
    Json
}
```

---

### 2. TXT 格式导出

**格式示例**：
```
【日记标题】
日期：2024 年 1 月 1 日 14:30
时长：5 分 32 秒
标签：工作、会议

【正文内容】
今天开了一个重要的项目会议...

【录音文件】
录音：20240101_143000.aac
转写：Completed
```

**实现代码**：
```csharp
public async Task<string> ExportToTextAsync(DiaryEntry entry)
{
    var sb = new StringBuilder();
    
    sb.AppendLine($"【{entry.Title}】");
    sb.AppendLine($"日期：{entry.CreatedAt:yyyy 年 M 月 d 日 HH:mm}");
    sb.AppendLine($"时长：{FormatDuration(entry.AudioDuration)}");
    
    if (entry.Tags?.Any() == true)
        sb.AppendLine($"标签：{string.Join("、", entry.Tags)}");
    
    sb.AppendLine();
    sb.AppendLine("【正文内容】");
    sb.AppendLine(entry.Content);
    
    sb.AppendLine();
    sb.AppendLine("【录音文件】");
    sb.AppendLine($"录音：{Path.GetFileName(entry.AudioFilePath)}");
    sb.AppendLine($"转写：{(entry.TranscriptionStatus == TranscriptionStatus.Completed ? "Completed" : "Pending")}");
    
    return sb.ToString();
}
```

---

### 3. Markdown 格式导出

**格式示例**：
```markdown
# 日记标题

> 📅 2024 年 1 月 1 日 14:30 | ⏱️ 5 分 32 秒 | 🏷️ 工作、会议

## 正文内容

今天开了一个重要的项目会议...

---

**录音文件**: `20240101_143000.aac`  
**转写状态**: ✅ 已完成
```

**实现代码**：
```csharp
public async Task<string> ExportToMarkdownAsync(DiaryEntry entry)
{
    var sb = new StringBuilder();
    
    sb.AppendLine($"# {entry.Title}");
    sb.AppendLine();
    sb.AppendLine($"> 📅 {entry.CreatedAt:yyyy 年 M 月 d 日 HH:mm} | ⏱️ {FormatDuration(entry.AudioDuration)} | 🏷️ {string.Join("、", entry.Tags ?? new())}");
    sb.AppendLine();
    sb.AppendLine("## 正文内容");
    sb.AppendLine();
    sb.AppendLine(entry.Content);
    sb.AppendLine();
    sb.AppendLine("---");
    sb.AppendLine($"**录音文件**: `{Path.GetFileName(entry.AudioFilePath)}`  ");
    sb.AppendLine($"**转写状态**: {(entry.TranscriptionStatus == TranscriptionStatus.Completed ? "✅ 已完成" : "⏳ 待处理")}");
    
    return sb.ToString();
}
```

---

### 4. JSON 格式导出

**格式示例**：
```json
{
  "id": 1,
  "title": "日记标题",
  "content": "正文内容...",
  "createdAt": "2024-01-01T14:30:00",
  "audioFilePath": "/path/to/audio.aac",
  "audioDuration": 332,
  "transcriptionStatus": "Completed",
  "tags": ["工作", "会议"],
  "mood": "Happy",
  "location": "北京"
}
```

**实现代码**：
```csharp
public async Task<string> ExportToJsonAsync(DiaryEntry entry)
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    
    return JsonSerializer.Serialize(entry, options);
}
```

---

### 5. 批量导出

```csharp
public async Task<string> ExportMultipleAsync(IEnumerable<DiaryEntry> entries, ExportFormat format)
{
    var results = new List<string>();
    
    foreach (var entry in entries)
    {
        var content = format switch
        {
            ExportFormat.Text => await ExportToTextAsync(entry),
            ExportFormat.Markdown => await ExportToMarkdownAsync(entry),
            ExportFormat.Json => await ExportToJsonAsync(entry),
            _ => throw new ArgumentException("Invalid format")
        };
        
        if (format == ExportFormat.Json)
            results.Add(content);
        else
            results.Add($"---\n\n{content}");
    }
    
    return format == ExportFormat.Json 
        ? $"[\n{string.Join(",\n", results)}\n]" 
        : string.Join("\n\n", results);
}
```

---

### 6. 分享功能

**Android 实现**：
```csharp
public async Task ShareAsync(string content, string title)
{
    var intent = new Intent(Intent.ActionSend);
    intent.PutExtra(Intent.ExtraText, content);
    intent.PutExtra(Intent.ExtraSubject, title);
    intent.SetType("text/plain");
    
    var sharesheet = Intent.CreateChooser(intent, "分享到");
    sharesheet.AddFlags(ActivityFlags.NewTask);
    
    Platform.CurrentActivity.StartActivity(sharesheet);
}
```

**iOS 实现**：
```csharp
public async Task ShareAsync(string content, string title)
{
    var items = new NSObject[] { new NSString(content), new NSString(title) };
    var activityController = new UIActivityViewController(items, null);
    
    var viewController = Platform.GetCurrentUIViewController();
    viewController.PresentViewController(activityController, true, null);
}
```

---

### 7. 批量导出 UI

**多选模式**：
```xml
<ContentPage>
    <CollectionView ItemsSource="{Binding DiaryEntries}"
                    SelectionMode="Multiple"
                    SelectedItems="{Binding SelectedEntries}">
        <CollectionView.ItemTemplate>
            <DataTemplate>
                <SwipeView>
                    <CollectionView.ItemsSource>
                        <SwipeItems>
                            <SwipeItem Text="导出"
                                       Command="{Binding ExportCommand}"
                                       BackgroundColor="#007AFF"/>
                        </SwipeItems>
                    </CollectionView.ItemsSource>
                    <!-- 日记内容 -->
                </SwipeView>
            </DataTemplate>
        </CollectionView.ItemTemplate>
    </CollectionView>
    
    <!-- 底部工具栏 -->
    <ContentView IsVisible="{Binding IsInSelectionMode}">
        <HorizontalStackLayout>
            <Button Text="导出所选" Command="{Binding ExportSelectedCommand}"/>
            <Button Text="取消" Command="{Binding CancelSelectionCommand}"/>
        </HorizontalStackLayout>
    </ContentView>
</ContentPage>
```

---

## 验收标准

### 单篇导出
- [ ] 详情页显示导出按钮
- [ ] 支持选择 TXT/MD/JSON 格式
- [ ] 导出内容格式正确
- [ ] 可以分享到其他应用

### 批量导出
- [ ] 列表页支持多选
- [ ] 可选择导出格式
- [ ] 一次性导出多篇
- [ ] 进度显示

### 格式正确性
- [ ] TXT 格式清晰易读
- [ ] MD 格式美观，有 emoji
- [ ] JSON 格式完整，可导入

---

## 开发步骤

### Day 1：导出服务

**上午**：
- [ ] IExportService 接口定义
- [ ] ExportToTextAsync 实现
- [ ] ExportToMarkdownAsync 实现
- [ ] ExportToJsonAsync 实现

**下午**：
- [ ] ExportMultipleAsync 实现
- [ ] ShareAsync 平台实现
- [ ] 单元测试

### Day 2：单篇导出 UI

**上午**：
- [ ] DiaryDetailPage 添加导出按钮
- [ ] 导出格式选择弹窗
- [ ] 分享功能集成

**下午**：
- [ ] 文件保存 dialog
- [ ] 分享功能测试
- [ ] 错误处理

### Day 3：批量导出 UI

**上午**：
- [ ] DiaryListPage 多选模式
- [ ] 批量导出按钮
- [ ] 进度条显示

**下午**：
- [ ] 整体测试
- [ ] 性能优化
- [ ] 文档编写

---

## 依赖项

```
导出服务
├── TXT 导出
├── Markdown 导出
├── JSON 导出
│
└── 分享功能
    ├── Android Intent
    └── iOS UIActivityViewController

批量导出 UI
├── CollectionView MultiSelect
├── 底部工具栏
└── 进度显示
```

---

## 风险评估

| 风险 | 影响 | 概率 | 应对 |
|------|------|------|------|
| 大文件分享失败 | 中 | 中 | 限制单次导出数量 |
| iOS 分享界面位置 | 低 | 低 | 使用 popover |
| JSON 编码问题 | 低 | 低 | 使用 UnsafeRelaxedJsonEscaping |
| 内存溢出（批量） | 中 | 低 | 分批处理 |

---

## 交付物

- ✅ IExportService 接口
- ✅ ExportService 实现
- ✅ DiaryDetailPage 导出按钮
- ✅ 导出格式选择弹窗
- ✅ DiaryListPage 多选模式
- ✅ 分享功能集成

---

**文档版本**: v1.0  
**创建时间**: 2026-05-03  
**最后更新**: 2026-05-03
