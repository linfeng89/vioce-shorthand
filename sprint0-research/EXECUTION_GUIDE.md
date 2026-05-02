# Sprint 0 执行手册

## 前置条件

1. **安装 .NET 8 SDK**: https://dotnet.microsoft.com/download/dotnet/8.0
2. **安装 Visual Studio 2022** (推荐) 或 VS Code + C# Dev Kit
3. **安装 MAUI 工作负载**:
   ```bash
   dotnet workload install maui
   ```

4. **测试设备**:
   - Android 手机（启用开发者模式 + USB 调试）
   - iPhone（可选，需要 Mac + Xcode）

---

## 执行步骤

### 步骤 1: 创建测试项目

```bash
cd /workspace/sprint0-research
dotnet new maui -n VoiceDiary.Sprint0 -f net8.0
cd VoiceDiary.Sprint0
```

### 步骤 2: 添加依赖

```bash
# sherpa-onnx (语音识别)
dotnet add package sherpa-onnx --version 1.8.22

# 音频录制
dotnet add package Plugin.Maui.Audio --version 1.0.2

# ZIP 压缩 (用于模型文件)
dotnet add package SharpZipLib --version 1.4.2
```

### 步骤 3: 下载模型文件

**Whisper base 中文模型**:

```bash
# 下载模型 (约 80MB)
wget https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-whisper-base.tar.bz2

# 解压
tar -xjf sherpa-onnx-whisper-base.tar.bz2

# 复制模型文件到项目资源目录
mkdir -p Resources/Raw/whisper-base
cp sherpa-onnx-whisper-base/* Resources/Raw/whisper-base/
```

**模型文件结构**:
```
Resources/Raw/whisper-base/
├── base.pt          # 编码器模型
├── base-encoder.pt   # 编码器
├── tokens.txt       # 词表
└── README.txt       # 说明
```

### 步骤 4: 替换代码

将以下文件复制到项目中：

1. `Services/SpeechRecognizerService.cs` - 语音识别服务
2. `Services/AudioRecorderService.cs` - 录音服务  
3. `Services/PerformanceTestService.cs` - 性能测试服务
4. `MainPage.xaml` - 主界面
5. `MainPage.xaml.cs` - 主界面逻辑

（代码文件见本目录下的 `src/` 文件夹）

### 步骤 5: 配置项目文件

编辑 `VoiceDiary.Sprint0.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0-android;net8.0-ios</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <RootNamespace>VoiceDiary.Sprint0</RootNamespace>
    <UseMaui>true</UseMaui>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="sherpa-onnx" Version="1.8.22" />
    <PackageReference Include="Plugin.Maui.Audio" Version="1.0.2" />
  </ItemGroup>

  <ItemGroup>
    <MauiAsset Include="Resources/Raw/whisper-base/**" />
  </ItemGroup>
</Project>
```

### 步骤 6: 构建并部署

**Android**:
```bash
# Build
dotnet build -f net8.0-android -c Debug

# Deploy to device
dotnet build -t:Run -f net8.0-android -c Debug
```

或在 Visual Studio 中:
1. 选择 Android 设备
2. 按 F5 运行

**iOS** (需要 Mac):
```bash
dotnet build -t:Run -f net8.0-ios -c Debug
```

### 步骤 7: 执行测试

App 启动后，界面会显示测试菜单：

```
┌─────────────────────────────┐
│   Sprint 0 性能测试          │
├─────────────────────────────┤
│ [1] 模型加载测试             │
│ [2] 转写性能测试             │
│ [3] 全流程测试               │
│ [4] 模型加载时机测试          │
│                             │
│ ▶ 测试结果:                 │
│                             │
│ └──────────────────────────┘
```

点击按钮执行对应测试，结果会显示在下方。

---

## 测试结果记录

执行完所有测试后，填写 `performance-results-template.md` 文件。

---

## 常见问题

### Q1: sherpa-onnx 找不到模型文件
**A**: 检查 `Resources/Raw/whisper-base/` 目录是否包含所有模型文件，确保 `.csproj` 中配置了 `MauiAsset`。

### Q2: Android 部署失败
**A**: 
1. 确保 USB 调试已启用
2. `adb devices` 检查设备连接
3. Visual Studio 中选择正确的设备

### Q3: iOS 编译失败
**A**: 
1. 确保在 Mac 上执行
2. 安装最新 Xcode
3. 配置签名证书

### Q4: 内存占用过高
**A**: 这是正常现象，Whisper base 模型需要 ~200MB。低内存设备建议使用方案 C（按需加载 + 低内存释放）。

---

## 后续步骤

1. 完成所有测试
2. 填写性能结果模板
3. 根据结果评估:
   - ✅ 全部达标 → 进入 Sprint 1
   - ⚠️ 部分达标 → 优化后重测
   - ❌ 不达标 → 调整技术方案（降级到 tiny 模型或云端 API）
