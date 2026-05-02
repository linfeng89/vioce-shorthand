# Task 2: Whisper base 性能实测

## 测试音频准备

准备 4 段标准测试音频（中文普通话）：

```bash
# 使用 Audacity 或其他工具录制
1. 30 秒朗读（约 100-120 字）
2. 1 分钟朗读（约 200-240 字）
3. 3 分钟朗读（约 600-720 字）
4. 5 分钟朗读（约 1000-1200 字）

# 格式要求：WAV, 16kHz, Mono, 16bit
```

## 测试步骤

### 步骤 1：部署测试 App

1. 在 Android 设备上启用开发者模式
2. 通过 Visual Studio 部署到真机
3. 确保 Release 模式（性能更接近生产环境）

### 步骤 2：加载测试音频

```csharp
// 从文件读取音频
private async Task<byte[]> LoadTestAudioAsync(string fileName)
{
    using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
    using var memory = new MemoryStream();
    await stream.CopyToAsync(memory);
    return memory.ToArray();
}

// 测试方法
private async Task TestPerformanceAsync()
{
    var testFiles = new[] 
    { 
        ("test_30s.wav", 30), 
        ("test_1min.wav", 60), 
        ("test_3min.wav", 180), 
        ("test_5min.wav", 300) 
    };
    
    bool result)
    {
        foreach (var (file, expectedDuration) in testFiles)
        {
            // 加载音频
            var audioData = await LoadTestAudioAsync(file);
            
            // 创建识别流
            var stream = _recognizer.CreateStream();
            
            // 开始推理
            var sw = Stopwatch.StartNew();
            stream.AcceptWaveform(audioData, 16000);
            
            while (_recognizer.IsReady(stream))
            {
                _recognizer.Decode(stream);
            }
            
            var result = _recognizer.GetResult(stream);
            sw.Stop();
            
            // 记录结果
            Log($"音频：{file}");
            Log($"时长：{expectedDuration}秒");
            Log($"推理耗时：{sw.ElapsedMilliseconds}ms");
            Log($"RTF (实时率): {sw.ElapsedMilliseconds / (expectedDuration * 1000):F2}");
            Log($"识别结果：{result.Text}");
            Log("---");
        }
    }
}
```

### 步骤 3：监控资源占用

**Android**:
```bash
# CPU 占用
adb shell dumpsys cpuinfo | grep VoiceDiary

# 内存占用
adb shell dumpsys meminfo VoiceDiary

# 或使用 Android Studio Profiler
```

**iOS**:
```bash
# 使用 Xcode Instruments
# - Time Profiler (CPU)
# - Allocations (内存)
```

### 步骤 4：多设备测试

| 设备 | 完成时间 | 1 分钟音频耗时 | CPU 峰值 | 内存占用 |
|------|----------|----------------|----------|----------|
| iPhone 14 Pro | | | | |
| Redmi Note 11 | | | | |
| 华为 P30 | | | | |

---

## 性能评估标准

| 实时率 (RTF) | 评价 |
|-------------|------|
| < 0.3 | 优秀（30 秒音频 < 10 秒完成） |
| 0.3-0.5 | 良好 |
| 0.5-1.0 | 可接受 |
| > 1.0 | 需优化 |

---

## 预期结果

**Whisper base 模型**：
- 30 秒 音频：< 8 秒
- 1 分钟 音频：< 15 秒
- 3 分钟 音频：< 45 秒
- 5 分钟 音频：< 75 秒

---

## 问题记录

（在此记录测试中发现的问题）

