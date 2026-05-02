# Task 3: 录音→转写→压缩 全流程验证

## 测试目标

验证完整的音频处理链路是否正常工作，以及资源并发控制是否生效。

## 流程说明

```
录音（PCM→WAV）
    ↓
保存 DB (IsTranscribed=false, IsCompressed=false)
    ↓
异步转写 (WAV→文字)
    ↓
更新 DB (IsTranscribed=true)
    ↓
异步压缩 (WAV→M4A)
    ↓
验证 M4A 可播放
    ↓
删除 WAV，更新 DB (IsCompressed=true, AudioFileName 改为.m4a)
```

## 验证步骤

### 步骤 1：录音功能验证

```csharp
// 使用 Plugin.Maui.Audio
using Plugin.Maui.Audio;

public class AudioRecorderService
{
    private IAudioSource? _recorder;
    
    public async Task StartRecordingAsync()
    {
        _recorder = await AudioRecorder.Default.StartRecordingAsync(
            new AudioRecordingOptions
            {
                SampleRate = 16000,
                Channels = 1,  // Mono
                BitsPerSample = 16
            }
        );
    }
    
    public async Task<string> StopRecordingAsync()
    {
        var filePath = await _recorder.StopRecordingAsync();
        return filePath;  // WAV 文件路径
    }
}
```

**验证清单**：
- [ ] 录音启动延迟 < 100ms
- [ ] WAV 文件格式正确（16kHz, Mono, 16bit）
- [ ] 文件可正常播放
- [ ] 录音中断后来电/杀后台），已录制部分可播放

### 步骤 2：转写功能验证

```csharp
public async Task<string> TranscribeAsync(string wavFilePath)
{
    var audioData = await File.ReadAllBytesAsync(wavFilePath);
    
    var stream = _recognizer.CreateStream();
    stream.AcceptWaveform(audioData, 16000);
    
    while (_recognizer.IsReady(stream))
    {
        _recognizer.Decode(stream);
    }
    
    return _recognizer.GetResult(stream).Text;
}
```

**验证清单**：
- [ ] 转写结果准确（与原文对比 > 80%）
- [ ] 转写耗时符合预期（参考 Task2）
- [ ] 中文带标点符号
- [ ] 转写失败有错误信息

### 步骤 3：压缩功能验证

```csharp
public async Task<CompressionResult> CompressToM4AAsync(string wavFilePath)
{
    // Android: MediaCodec
    // iOS: AVAssetExportSession
    
    // 1. 解码 WAV 到 PCM
    // 2. 用 AAC 编码器压缩
    // 3. 封装为 M4A
    
    // 验证 M4A 文件
    var fileInfo = new FileInfo(m4aPath);
    if (!fileInfo.Exists || fileInfo.Length == 0)
        throw new Exception("M4A 文件创建失败");
    
    // 验证可播放
    using var player = MediaPlayer.Create();
    player.SetDataSource(m4aPath);
    player.Prepare();
    if (player.Duration <= 0)
        throw new Exception("M4A 文件无法播放");
    
    // 验证压缩率（M4A 应该 < WAV 的 50%）
    var wavSize = new FileInfo(wavFilePath).Length;
    if (fileInfo.Length >= wavSize * 0.5)
        throw new Exception("压缩率不达标");
    
    return new CompressionResult
    {
        M4APath = m4aPath,
        OriginalWavSize = wavSize,
        CompressedSize = fileInfo.Length,
        CompressionRatio = 1.0 - (double)fileInfo.Length / wavSize
    };
}
```

**验证清单**：
- [ ] M4A 文件创建成功
- [ ] 文件可播放
- [ ] 压缩率 > 50%
- [ ] 音质可接受
- [ ] 压缩耗时 < 转写耗时

### 步骤 4：资源并发控制验证

```csharp
public class TranscribeQueueManager
{
    private bool _isRecording = false;
    private CancellationTokenSource? _currentTranscribe;
    
    public async Task StartRecordingWithPreemptAsync()
    {
        _isRecording = true;
        
        // 如果有转写任务在运行，暂停它
        if (_currentTranscribe != null && !_currentTranscribe.IsCancellationRequested)
        {
            _currentTranscribe.Cancel();
            await Task.Delay(100);  // 等待资源释放
        }
        
        // 开始录音（独占资源）
        await _recorderService.StartRecordingAsync();
    }
    
    public async Task StopRecordingAndResumeQueueAsync()
    {
        await _recorderService.StopRecordingAsync();
        _isRecording = false;
        
        // 恢复转写队列
        _ = ProcessQueueAsync();  // fire-and-forget
    }
}
```

**验证清单**：
- [ ] 录音开始瞬间，转写任务暂停
- [ ] 录音期间，CPU 主要供给录音
- [ ] 录音结束后，转写自动恢复
- [ ] 被中断的转写任务正确重启

---

## 预期结果

| 指标 | 目标值 |
|------|--------|
| 录音启动延迟 | < 100ms |
| WAV 文件大小（1 分钟） | ~1.9MB |
| 转写准确率 | > 80% |
| 压缩率 | > 50% |
| M4A 文件大小（1 分钟） | ~0.5MB |
| 压缩耗时 | < 转写耗时 |
| 并发切换时间 | < 50ms |

---

## 问题记录

（在此记录测试中发现的问题）

