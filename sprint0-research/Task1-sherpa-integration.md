# Task 1: sherpa-onnx 集成测试

## 步骤

### 1.1 创建测试项目

```bash
# 安装 .NET MAUI 工作负载
dotnet workload install maui

# 创建测试项目
dotnet new maui -n VoiceDiary.Sprint0 -o VoiceDiary.Sprint0
cd VoiceDiary.Sprint0
```

### 1.2 安装 sherpa-onnx NuGet 包

```bash
# 添加 sherpa-onnx NuGet
dotnet add package sherpa-onnx --version 1.8.0

# 或使用 NuGet 包管理器安装最新版
```

### 1.3 集成验证代码

在 `MainPage.xaml.cs` 中添加：

```csharp
using SherpaOnnx;

public partial class MainPage : ContentPage
{
    private OnlineRecognizer? _recognizer;
    private OnlineStream? _stream;
    
    public MainPage()
    {
        InitializeComponent();
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        try
        {
            // 记录加载开始时间
            var startTime = DateTime.Now;
            
            // 初始化识别器（Whisper base 模型）
            var config = new OnlineRecognizerConfig
            {
                Feats = new FeatureExtractorConfig { SampleRate = 16000 },
                ModelConfig = new OnlineModelConfig
                {
                    Whisper = new WhisperConfig
                    {
                        Model = "whisper-base.en.txt",  // 模型路径
                        Language = "zh",
                        Task = "transcribe"
                    },
                    Tokens = "tokens.txt"  // 词表文件
                },
                MaxActivePaths = 4
            };
            
            _recognizer = new OnlineRecognizer(config);
            
            var elapsed = DateTime.Now - startTime;
            
            // 显示结果
            ResultLabel.Text = $"✅ 模型加载成功\n耗时：{elapsed.TotalMilliseconds:F0}ms";
        }
        catch (Exception ex)
        {
            ResultLabel.Text = $"❌ 加载失败\n{ex.Message}\n{ex.StackTrace}";
        }
    }
}
```

### 1.4 下载测试模型

从 sherpa-onnx 官方下载 Whisper base 中文模型：

```bash
# 模型下载链接（示例）
wget https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-whisper-base.en.tar.bz2

# 解压
tar -xjf sherpa-onnx-whisper-base.en.tar.bz2

# 将模型文件复制到 Resources/Raw/ 目录
```

### 1.5 验证清单

- [ ] NuGet 包成功安装
- [ ] 模型文件正确放置
- [ ] App 启动后模型成功加载
- [ ] 记录加载耗时（目标 < 2 秒）
- [ ] 记录模型文件大小
- [ ] 记录内存占用

---

## 预期结果

- ✅ 模型加载耗时：< 2 秒
- ✅ 模型文件大小：~80MB
- ✅ 内存占用：< 200MB

---

## 问题记录

（在此记录遇到的问题）

