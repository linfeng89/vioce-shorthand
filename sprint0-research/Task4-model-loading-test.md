# Task 4: 模型加载时机验证

## 测试目标

对比不同模型加载策略的性能表现，找到最优方案。

## 测试方案

### 方案 A：App 启动预加载

```csharp
public class App : Application
{
    public App()
    {
        InitializeComponent();
        
        // App 启动时后台预加载
        _ = SpeechRecognizerService.PreloadModelAsync();
        
        MainPage = new MainPage();
    }
}

public class SpeechRecognizerService
{
    private static OnlineRecognizer? _recognizer;
    private static Task? _preloadTask;
    private static DateTime? _loadedAt;
    
    public static async Task PreloadModelAsync()
    {
        _preloadTask = Task.Run(async () =>
        {
            var sw = Stopwatch.StartNew();
            
            _recognizer = await Task.Run(() => 
            {
                var config = new OnlineRecognizerConfig { ... };
                return new OnlineRecognizer(config);
            });
            
            sw.Stop();
            _loadedAt = DateTime.Now;
            
            Debug.WriteLine($"✅ 模型预加载完成：{sw.ElapsedMilliseconds}ms");
        });
    }
    
    public static async Task<OnlineRecognizer> GetRecognizerAsync()
    {
        if (_recognizer != null)
            return _recognizer;
            
        // 如果还没加载完，等待
        if (_preloadTask != null)
            await _preloadTask;
            
        return _recognizer!;
    }
}
```

**测试方法**：
1. 冷启动 App
2. 记录 App 启动到 UI 可交互的时间
3. 记录模型加载完成的总耗时
4. 立即开始录音，记录转写延迟

**预期**：
- App 启动时间：+0ms（后台加载不阻塞 UI）
- 首次录音延迟：0ms（模型已就绪）或 < 2 秒（未加载完需等待）

---

### 方案 B：首次录音时懒加载

```csharp
public class SpeechRecognizerService
{
    private OnlineRecognizer? _recognizer;
    
    public async Task InitializeAsync()
    {
        if (_recognizer != null)
            return;
        
        var sw = Stopwatch.StartNew();
        _recognizer = await Task.Run(() => 
        {
            var config = new OnlineRecognizerConfig { ... };
            return new OnlineRecognizer(config);
        });
        sw.Stop();
        
        Debug.WriteLine($"模型加载完成：{sw.ElapsedMilliseconds}ms");
    }
}

// 录音按钮点击时
private async void OnRecordButtonPressed()
{
    await _speechService.InitializeAsync();  // 加载中显示 loading
    await StartRecordingAsync();
}
```

**测试方法**：
1. 冷启动 App
2. 记录 App 启动时间
3. 立即点击录音按钮
4. 记录加载耗时 + 录音启动总耗时

**预期**：
- App 启动时间：快
- 首次录音延迟：2-3 秒（需等待模型加载）

---

### 方案 C：按需加载 + 低内存释放

```csharp
public class SpeechRecognizerService
{
    private OnlineRecognizer? _recognizer;
    
    public async Task EnsureLoadedAsync()
    {
        if (_recognizer != null)
            return;
        
        await InitializeAsync();
    }
    
    public void UnloadModel()
    {
        _recognizer?.Dispose();
        _recognizer = null;
        GC.Collect();
        Debug.WriteLine("模型已释放");
    }
}

// 监听内存警告
Microsoft.Maui.ApplicationModel.MemoryPressure.Low += (s, e) =>
{
    _speechService.UnloadModel();
};
```

**测试方法**：
1. 正常使用 App（加载模型 → 转写）
2. 模拟低内存警告
3. 验证模型释放成功
4. 再次录音，验证重新加载

**预期**：
- 低内存时正确释放模型
- 重新加载耗时与首次加载相同

---

## 对比测试

| 指标 | 方案 A（预加载） | 方案 B（懒加载） | 方案 C（按需 + 释放） |
|------|-----------------|-----------------|-------------------|
| App 启动速度 | 快 | 快 | 快 |
| 首次录音延迟 | 0-2 秒 | 2-3 秒 | 2-3 秒 |
| 内存占用（常驻） | 高（~200MB） | 低 | 低 |
| 低内存表现 | 可能被系统杀 | 良好 | 优秀 |
| 用户体验 | 最流畅 | 首次需等待 | 首次需等待 |

---

## 推荐方案

**方案 A（预加载）+ 方案 C（低内存释放）组合**：

```csharp
// App 启动时后台预加载
// 低内存时释放模型
// 释放后下次录音时重新加载
```

理由：
- 多数情况下模型常驻，首次录音无延迟
- 低内存时自动释放，避免被系统杀
- 平衡了性能和稳定性

---

## 验证清单

- [ ] 预加载不阻塞 UI
- [ ] 首次录音延迟 < 2 秒
- [ ] 低内存警告时正确释放
- [ ] 释放后重新加载成功
- [ ] 内存占用 < 200MB

---

## 问题记录

（在此记录测试中发现的问题）

