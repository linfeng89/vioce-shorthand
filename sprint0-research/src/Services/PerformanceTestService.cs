namespace VoiceDiary.Sprint0.Services;

public class PerformanceTestService
{
    private readonly SpeechRecognizerService _recognizerService;

    public PerformanceTestService(SpeechRecognizerService recognizerService)
    {
        _recognizerService = recognizerService;
    }

    /// <summary>
    /// 测试模型加载性能
    /// </summary>
    public async Task<PerfResult> TestModelLoadingAsync()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _recognizerService.PreloadModelAsync();
        sw.Stop();

        return new PerfResult
        {
            Name = "模型加载",
            DurationMs = sw.ElapsedMilliseconds,
            Success = true,
            Details = $"耗时：{sw.ElapsedMilliseconds}ms"
        };
    }

    /// <summary>
    /// 测试转写性能（多时长）
    /// </summary>
    public async Task<List<PerfResult>> TestTranscriptionAsync()
    {
        var results = new List<PerfResult>();
        var testFiles = new[]
        {
            ("test_30s.wav", 30),
            ("test_1min.wav", 60),
            ("test_3min.wav", 180),
            ("test_5min.wav", 300)
        };

        // 确保模型已加载
        await _recognizerService.GetRecognizerAsync();

        foreach (var (fileName, expectedDuration) in testFiles)
        {
            try
            {
                var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                if (!File.Exists(filePath))
                {
                    results.Add(new PerfResult
                    {
                        Name = $"转写测试 - {fileName}",
                        Success = false,
                        Details = "测试文件不存在"
                    });
                    continue;
                }

                var (text, durationMs) = await _recognizerService.TranscribeFileAsync(filePath);

                var rtf = (double)durationMs / (expectedDuration * 1000);

                results.Add(new PerfResult
                {
                    Name = $"转写测试 - {expectedDuration}秒",
                    DurationMs = durationMs,
                    Success = durationMs < expectedDuration * 1000, // RTF < 1.0
                    Details = $"耗时：{durationMs}ms, RTF: {rtf:F2}"
                });
            }
            catch (Exception ex)
            {
                results.Add(new PerfResult
                {
                    Name = $"转写测试 - {expectedDuration}秒",
                    Success = false,
                    Details = ex.Message
                });
            }
        }

        return results;
    }

    /// <summary>
    /// 测试低内存释放
    /// </summary>
    public async Task<PerfResult> TestLowMemoryHandlingAsync()
    {
        // 先加载模型
        await _recognizerService.PreloadModelAsync();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        _recognizerService.UnloadModel();

        return new PerfResult
        {
            Name = "低内存释放",
            DurationMs = sw.ElapsedMilliseconds,
            Success = !_recognizerService.IsModelLoaded,
            Details = $"模型已{( _recognizerService.IsModelLoaded ? "未" : "")}释放"
        };
    }
}

public class PerfResult
{
    public string Name { get; set; } = "";
    public long DurationMs { get; set; }
    public bool Success { get; set; }
    public string Details { get; set; } = "";
}
