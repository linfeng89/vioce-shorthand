using K2.Fsa.SherpaOnnx;

namespace VoiceDiary.Services;

public class WhisperRecognizer : ISpeechRecognizer
{
    private bool _isReady;
    private bool _isInitialized;
    private OnlineRecognizer? _recognizer;
    private string? _modelPath;
    private readonly IStorageService _storageService;
    private readonly object _lock = new();

    public WhisperRecognizer(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public bool IsReady => _isReady;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        await Task.Run(() =>
        {
            lock (_lock)
            {
                if (_isInitialized)
                    return;

                try
                {
                    var modelDir = Path.Combine(_storageService.AppDatabasePath, "models");
                    Directory.CreateDirectory(modelDir);

                    _modelPath = Path.Combine(modelDir, "sherpa-onnx-whisper-base");
                    
                    if (!Directory.Exists(_modelPath))
                    {
                        DownloadModel(_modelPath);
                    }

                    var config = new OnlineRecognizerConfig
                    {
                        FeatsConfig = new FeatureExtractorConfig
                        {
                            SampleRate = 16000,
                            FeatureDim = 80
                        },
                        ModelConfig = new OnlineModelConfig
                        {
                            Whisper = new WhisperConfig
                            {
                                Encoder = Path.Combine(_modelPath, "encoder.onnx"),
                                Decoder = Path.Combine(_modelPath, "decoder.onnx"),
                                Language = "auto",  // 自动检测语言（支持 99 种）
                                Task = "transcribe",
                                Multilingual = true  // 启用多语言模式
                            },
                            BpeVocab = Path.Combine(_modelPath, "tokens.txt")
                        },
                        DecodingMethod = "greedy_search",
                        MaxActivePaths = 4,
                        EnableTruncation = false
                    };

                    _recognizer = new OnlineRecognizer(config);
                    _isReady = true;
                }

                    var config = new OnlineRecognizerConfig
                    {
                        FeatsConfig = new FeatureExtractorConfig
                        {
                            SampleRate = 16000,
                            FeatureDim = 80
                        },
                        ModelConfig = new OnlineModelConfig
                        {
                            Whisper = new WhisperConfig
                            {
                                Encoder = Path.Combine(_modelPath, "encoder.onnx"),
                                Decoder = Path.Combine(_modelPath, "decoder.onnx"),
                                Language = "zh",
                                Task = "transcribe"
                            },
                            BpeVocab = Path.Combine(_modelPath, "tokens.txt")
                        },
                        DecodingMethod = "greedy_search",
                        MaxActivePaths = 4,
                        EnableTruncation = false
                    };

                    _recognizer = new OnlineRecognizer(config);
                    _isReady = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"模型加载失败：{ex.Message}");
                    throw;
                }
            }
        });

        _isInitialized = true;
    }

    public async Task<string?> RecognizeAsync(string audioFilePath)
    {
        if (!_isReady)
            await InitializeAsync();

        return await Task.Run(() =>
        {
            if (_recognizer == null)
                return null;

            try
            {
                var stream = _recognizer.CreateStream();

                var samples = ReadWavFile(audioFilePath);
                stream.AcceptWaveform(16000, samples);

                var tailPadding = new float[16000];
                stream.AcceptWaveform(16000, tailPadding);

                while (_recognizer.IsReady(stream))
                {
                    _recognizer.Decode(stream);
                }

                var text = _recognizer.GetResult(stream).Text;
                return string.IsNullOrEmpty(text) ? null : text.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"转写失败：{ex.Message}");
                throw;
            }
        });
    }

    public void Release()
    {
        lock (_lock)
        {
            _recognizer?.Dispose();
            _recognizer = null;
            _isReady = false;
        }
    }

    private float[] ReadWavFile(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fs);

        if (new string(reader.ReadChars(4)) != "RIFF")
            throw new ArgumentException("无效的 WAV 文件");

        reader.ReadInt32();
        if (new string(reader.ReadChars(4)) != "WAVE")
            throw new ArgumentException("无效的 WAV 文件");

        while (true)
        {
            var chunkId = new string(reader.ReadChars(4));
            var chunkSize = reader.ReadInt32();

            if (chunkId == "fmt ")
            {
                var format = reader.ReadUInt16();
                var channels = reader.ReadUInt16();
                var sampleRate = reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadUInt16();
                var bitsPerSample = reader.ReadUInt16();

                if (format != 1)
                    throw new ArgumentException("仅支持 PCM 格式");
                if (channels != 1)
                    throw new ArgumentException("仅支持单声道");
                if (sampleRate != 16000)
                    throw new ArgumentException("仅支持 16kHz 采样率");
                if (bitsPerSample != 16)
                    throw new ArgumentException("仅支持 16bit");

                if (chunkSize > 16)
                    reader.ReadBytes(chunkSize - 16);
            }
            else if (chunkId == "data")
            {
                var data = reader.ReadBytes(chunkSize);
                var samples = new float[chunkSize / 2];
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] = BitConverter.ToInt16(data, i * 2) / 32768.0f;
                }
                return samples;
            }
            else
            {
                reader.ReadBytes(chunkSize);
            }

            if (fs.Position >= fs.Length)
                break;
        }

        return Array.Empty<float>();
    }

    private void DownloadModel(string modelPath)
    {
        Console.WriteLine($"模型不存在，需要手动下载 Whisper base 中文模型到：{modelPath}");
        Console.WriteLine("下载地址：https://github.com/k2-fsa/sherpa-onnx/releases");
        throw new FileNotFoundException("Whisper 模型未找到，请手动下载并放置到指定目录");
    }
}
