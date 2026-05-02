# VoiceDiary

.NET MAUI 语音日记应用

## 技术栈

- **跨平台框架**: .NET MAUI (C#)
- **语音转文字**: sherpa-onnx (Whisper base 中文模型)
- **本地数据库**: SQLite + sqlite-net-pcl
- **生物识别**: CommunityToolkit.Maui
- **UI 组件**: .NET MAUI Community Toolkit

## 快速开始

### 环境要求

- Visual Studio 2022 / Rider / VS Code
- .NET 8.0 SDK
- MAUI 工作负载

### 安装依赖

```bash
dotnet restore
```

### 运行应用

```bash
# Android
dotnet build -t:Run -f net8.0-android

# iOS (macOS only)
dotnet build -t:Run -f net8.0-ios

# Windows
dotnet build -t:Run -f net8.0-windows10.0.19041.0
```

## 项目结构

```
VoiceDiary/
├── Models/           # 数据模型
├── Views/            # 页面视图
├── ViewModels/       # ViewModel
├── Services/         # 业务服务
└── Resources/        # 资源文件
```

## 功能特性

- ✅ 按住录音，松开保存
- ✅ 离线语音转文字
- ✅ 日记列表和时间分组
- ✅ 全文搜索
- ✅ 回收站管理
- ✅ 备份恢复
- ✅ 多格式导出

## 许可证

MIT
