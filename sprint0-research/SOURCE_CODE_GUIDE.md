# 源代码文件说明

本文档说明如何使用 `src/` 目录中的源代码文件。

## 文件清单

```
src/
├── MainPage.xaml                 # 测试界面 UI
├── MainPage.xaml.cs              # 测试界面逻辑
└── Services/
    ├── SpeechRecognizerService.cs    # 语音识别服务（sherpa-onnx 封装）
    └── PerformanceTestService.cs     # 性能测试服务
```

## 使用方法

### 1. 创建 MAUI 项目

```bash
cd /workspace/sprint0-research
dotnet new maui -n VoiceDiary.Sprint0 -f net8.0
cd VoiceDiary.Sprint0
```

### 2. 复制源代码

```bash
# 复制服务类
cp ../src/Services/*.cs Services/

# 复制主页面
cp ../src/MainPage.xaml .
cp ../src/MainPage.xaml.cs .
```

### 3. 修改 App.xaml.cs

编辑 `App.xaml.cs`，添加服务注入:

```csharp
using VoiceDiary.Sprint0.Services;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // 初始化服务
        var recognizerService = new SpeechRecognizerService();
        var testService = new PerformanceTestService(recognizerService);

        // 设置主页面
        MainPage = new MainPage(recognizerService, testService);
    }
}
```

### 4. 准备测试音频文件

在 `Platforms/Android/Resources/raw/` 目录下放置测试音频：

```bash
mkdir -p Platforms/Android/Resources/raw/

# 复制测试音频（需自行录制）
cp /path/to/test_30s.wav Platforms/Android/Resources/raw/
cp /path/to/test_1min.wav Platforms/Android/Resources/raw/
# ... 其他测试音频
```

**测试音频要求**:
- 格式：WAV
- 采样率：16kHz
- 声道：Mono
- 位深：16bit
- 时长：30 秒/1 分钟/3 分钟/5 分钟

### 5. 下载模型文件

```bash
# 在项目根目录执行
mkdir -p Resources/Raw/whisper-base
cd Resources/Raw/whisper-base

# 下载 Whisper base 中文模型
wget https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-whisper-base.tar.bz2

# 解压
tar -xjf sherpa-onnx-whisper-base.tar.bz2

# 清理
rm sherpa-onnx-whisper-base.tar.bz2
```

### 6. 修改项目文件

编辑 `VoiceDiary.Sprint0.csproj`，添加资源引用:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0-android;net8.0-ios</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <RootNamespace>VoiceDiary.Sprint0</RootNamespace>
    <UseMaui>true</UseMaui>

    <SingleProject>true</SingleProject>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="sherpa-onnx" Version="1.8.22" />
    <PackageReference Include="Plugin.Maui.Audio" Version="1.0.2" />
  </ItemGroup>

  <ItemGroup>
    <MauiAsset Include="Resources/Raw/whisper-base/**" />
    <MauiAudio Include="Platforms/Android/Resources/raw/*.wav" />
  </ItemGroup>
</Project>
```

### 7. 构建运行

```bash
# Android
dotnet build -t:Run -f net8.0-android -c Debug

# iOS (需要 Mac)
dotnet build -t:Run -f net8.0-ios -c Debug
```

---

## 代码说明

### SpeechRecognizerService.cs

**核心功能**:
- `PreloadModelAsync()`: 后台预加载模型
- `GetRecognizerAsync()`: 获取识别器实例（自动等待加载）
- `TranscribeFileAsync()`: 转写 WAV 文件
- `UnloadModel()`: 低内存时释放模型

**模型加载流程**:
1. 从 Resources/Raw 复制模型文件到 AppDataDirectory
2. 配置 OnlineRecognizerConfig
3. 创建 OnlineRecognizer 实例
4. 记录加载耗时

### PerformanceTestService.cs

**测试方法**:
- `TestModelLoadingAsync()`: 测试模型加载耗时
- `TestTranscriptionAsync()`: 测试不同时长音频的转写性能
- `TestLowMemoryHandlingAsync()`: 测试低内存释放功能

### MainPage.xaml / MainPage.xaml.cs

**测试界面**:
- 3 个测试按钮（模型加载/转写性能/低内存释放）
- 结果显示区域
- 异常处理和错误提示

---

## 注意事项

1. **模型文件较大** (~80MB)，首次启动复制需要时间
2. **测试音频需自行录制**，确保符合格式要求
3. **真机测试**性能数据才准确，模拟器/仿真器仅供参考
4. **Android 设备**需开启 USB 调试模式
5. **iOS 设备**需要 Mac 和 Xcode

---

## 故障排查

### 问题 1: 找不到模型文件
检查 `Resources/Raw/whisper-base/` 目录是否包含所有模型文件

### 问题 2: 测试文件不存在
确保测试音频文件已放置到正确目录

### 问题 3: 编译失败
确保已安装 .NET 8 SDK 和 MAUI 工作负载

### 问题 4: 运行时崩溃
查看输出日志，常见原因：
- 模型文件路径错误
- WAV 文件格式不正确
- 权限问题（Android 需要录音权限）
