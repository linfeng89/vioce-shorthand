using SherpaOnnx;

namespace VoiceDiary.Sprint0.Services;

public class SpeechRecognizerService
{
    private OnlineRecognizer? _recognizer;
    private Task? _preloadTask;
    private bool _isLowMemory;

    public bool IsModelLoaded => _recognizer != null;
    public DateTime? LoadedAt { get; private set; }

    /// <summary>
    /// 预加载模型（后台线程，不阻塞 UI）
    /// </summary>
    public async Task PreloadModelAsync()
    {
        if (_recognizer != null) return;

        _preloadTask = Task.Run(async () =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 从资源目录加载模型文件
                var modelPath = Path.Combine(
                    FileSystem.AppDataDirectory,
                    "whisper-base"
                );

                // 确保目录存在
                Directory.CreateDirectory(modelPath);

                // 复制模型文件（首次启动时从 Resources/Raw 复制）
                await CopyModelFilesAsync(modelPath);

                // 配置识别器
                var config = new OnlineRecognizerConfig
                {
                    Feats = new FeatureExtractorConfig
                    {
                        SampleRate = 16000,
                        FeatureDim = 80
                    },
                    ModelConfig = new OnlineModelConfig
                    {
                        Whisper = new WhisperConfig
                        {
                            Model = Path.Combine(modelPath, "base-encoder.pt"),
                            Language = "zh",
                            Task = "transcribe"
                        },
                        Tokens = Path.Combine(modelPath, "tokens.txt")
                    },
                    MaxActivePaths = 4,
                    EnableTracing = false
                };

                _recognizer = new OnlineRecognizer(config);
                LoadedAt = DateTime.Now;

                sw.Stop();
                System.Diagnostics.Debug.WriteLine($"✅ 模型加载完成：{sw.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 模型加载失败：{ex}");
                throw;
            }
        });

        await _preloadTask;
    }

    /// <summary>
    /// 获取识别器（如果未加载则等待）
    /// </summary>
    public async Task<OnlineRecognizer> GetRecognizerAsync()
    {
        if (_recognizer != null)
            return _recognizer;

        if (_preloadTask != null)
            await _preloadTask;

        return _recognizer!;
    }

    /// <summary>
    /// 转写音频文件
    /// </summary>
    public async Task<(string Text, int DurationMs)> TranscribeFileAsync(string wavFilePath)
    {
        var recognizer = await GetRecognizerAsync();

        // 读取 WAV 文件
        var audioData = await ReadWavFileAsync(wavFilePath);

        // 创建识别流
        var stream = recognizer.CreateStream();

        // 开始推理
        var sw = System.Diagnostics.Stopwatch.StartNew();
        stream.AcceptWaveform(audioData, 16000);

        while (recognizer.IsReady(stream))
        {
            recognizer.Decode(stream);
        }

        var result = recognizer.GetResult(stream);
        sw.Stop();

        return (result.Text, (int)sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// 低内存时释放模型
    /// </summary>
    public void UnloadModel()
    {
        if (_recognizer != null)
        {
            _recognizer.Dispose();
            _recognizer = null;
            LoadedAt = null;
            GC.Collect();
            System.Diagnostics.Debug.WriteLine("🗑️ 模型已释放");
        }
    }

    #region Helpers

    private async Task CopyModelFilesAsync(string targetPath)
    {
        // 从 Resources/Raw 复制模型文件
        var modelFiles = new[] { "base-encoder.pt", "tokens.txt" };

        foreach (var file in modelFiles)
        {
            var sourcePath = Path.Combine("whisper-base", file);
            var targetFilePath = Path.Combine(targetPath, file);

            if (!File.Exists(targetFilePath))
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(sourcePath);
                using var fileStream = File.Create(targetFilePath);
                await stream.CopyToAsync(fileStream);
            }
        }
    }

    private async Task<float[]> ReadWavFileAsync(string filePath)
    {
        using var fileStream = File.OpenRead(filePath);
        using var reader = new System.IO.BinaryReader(fileStream);

        // 跳过 WAV 头（44 字节）
        reader.ReadBytes(44);

        // 读取 PCM 数据
        var bytes = reader.ReadBytes((int)(fileStream.Length - 44));
        var samples = new float[bytes.Length / 2];

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = BitConverter.ToInt16(bytes, i * 2) / 32768.0f;
        }

        return samples;
    }

    #endregion
}
