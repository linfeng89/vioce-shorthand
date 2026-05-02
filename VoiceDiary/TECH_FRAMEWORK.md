# VoiceDiary 技术框架

## 项目概述

语音日记应用，基于 .NET MAUI 构建，支持 iOS、Android、mac Catalyst 和 Windows 平台。

**核心理念**：按住录音，松开看字。离线、本地、极简的语音日记工具。

## 技术栈

| 层面 | 技术选型 | 说明 |
|------|---------|------|
| 跨平台框架 | .NET MAUI (C#) | 单代码库，多平台支持 |
| 语音转文字 | sherpa-onnx (Whisper) | 完全离线，中文优化 |
| 本地数据库 | SQLite + sqlite-net-pcl | 轻量可靠 |
| 生物识别 | CommunityToolkit.Maui | 跨平台生物识别接口 |
| 音频格式 | WAV → M4A | PCM 录制，异步压缩 |

## 项目结构

```
VoiceDiary/
├── Models/                     # 数据模型层
│   ├── DiaryEntry.cs          # 日记条目
│   └── AudioSegment.cs        # 音频片段（多段合并预留）
├── ViewModels/                 # ViewModel 层
│   ├── BaseViewModel.cs       # MVVM 基类
│   ├── RecordViewModel.cs     # 录音页面
│   ├── DiaryListViewModel.cs  # 列表页面
│   ├── DiaryDetailViewModel.cs # 详情页面
│   ├── SettingsViewModel.cs   # 设置页面
│   └── TrashViewModel.cs      # 回收站页面
├── Views/                      # 视图层
│   ├── RecordPage.xaml        # 录音主页面
│   ├── DiaryListPage.xaml     # 日记列表
│   ├── DiaryDetailPage.xaml   # 详情和回放
│   ├── SettingsPage.xaml      # 设置页面
│   └── TrashPage.xaml         # 回收站
├── Services/                   # 业务服务层
│   ├── IDatabaseService.cs    # 数据库接口
│   │   ├── DatabaseService.cs       # SQLite 实现 + 迁移
│   │   └── Fts5Service.cs         # FTS5 全文搜索
│   ├── IAudioRecorder.cs      # 录音接口
│   │   └── AudioRecorder.cs         # 跨平台录音实现
│   ├── ISpeechRecognizer.cs   # 语音识别接口
│   │   └── WhisperRecognizer.cs     # sherpa-onnx 实现
│   ├── IAudioCompressor.cs    # 音频压缩接口
│   │   └── AudioCompressor.cs       # WAV→M4A 压缩
│   ├── IStorageService.cs     # 存储服务接口
│   │   └── StorageService.cs        # 文件路径管理
│   ├── IBackupService.cs      # 备份恢复接口
│   │   └── BackupService.cs         # ZIP 备份实现
│   ├── IExportService.cs      # 导出接口
│   │   └── ExportService.cs         # TXT/MD/JSON 导出
│   └── IBiometricAuthService.cs # 生物识别接口
│       └── BiometricAuthService.cs  # 指纹/面部识别
└── Resources/                  # 资源文件
    ├── AppIcon/               # 应用图标
    ├── Splash/                # 启动图
    ├── Fonts/                 # 字体文件
    ├── Images/                # 图片资源
    └── Raw/                   # 原始资源
```

## 核心设计

### 1. 录音流程

```
用户按住按钮
  → 启动 PCM 录音（Plugin.Maui.Audio）
  → 实时 WAV 文件写入（边录边写）
  → 检测手势：
    - 短按 (<0.3s)：误触过滤
    - 上滑锁定：进入长录音模式
    - 上滑取消：丢弃录音
    - 正常松开：停止并保存
  → 检查时长 (<1s 丢弃)
  → 保存 WAV 文件 + 数据库记录
  → 异步转写队列
  → 异步压缩队列（WAV→M4A）
```

### 2. 转写流程

```
录音完成
  → 加入转写队列（按 FIFO 排序）
  → 检查模型是否就绪（App 启动预加载）
  → sherpa-onnx 推理（Whisper base）
  → 更新数据库 (TranscribedText, IsTranscribed=true)
  → 触发 FTS5 索引更新
  → UI 自动刷新
```

### 3. 数据模型

```csharp
DiaryEntry {
    RowId (int, SQLite PK)
    Id (string, GUID, 业务主键)
    CreatedAt (DateTime)
    UpdatedAt (DateTime)
    TranscribedText (string)
    AudioFileName (string, 仅文件名，不含路径)
    DurationSeconds (int)
    IsTranscribed (bool)
    TranscribeAttempts (int)
    TranscribeError (string)
    IsCompressed (bool)
    IsDeleted (bool, 软删除)
    DeletedAt (DateTime?)
}
```

### 4. 数据库设计

- **主表**: `DiaryEntry` (日记元数据)
- **附表**: `AudioSegment` (多段录音，MVP 预留)
- **搜索索引**: `DiaryEntry_FTS` (FTS5 虚拟表，CJK 双字符分词)
- **应用设置**: `AppSettings` (键值对)
- **迁移机制**: PRAGMA user_version + 增量脚本

### 5. 文件命名

格式：`{日期}_{时间}_{序号}.{扩展名}`

示例：`20260501_0932_001.wav`, `20260501_1845_002.m4a`

设计要点：
- 文件名本身包含时间信息，肉眼可辨认
- 三位序号解决同一天多条重名问题
- 脱离 App 也能配对音频和文字

## 已实现功能

### MVP (P0)

- ✅ 录音核心功能（按住录音，松开保存）
- ✅ 录音手势（上滑锁定、上滑取消、短按过滤）
- ✅ 最短时长过滤（<1 秒丢弃）
- ✅ 跨平台录音服务（Plugin.Maui.Audio 抽象）
- ✅ WAV 格式录制（PCM 16bit Mono 16kHz）
- ✅ 基础页面框架（5 个页面）
- ✅ MVVM 架构和依赖注入
- ✅ SQLite 数据库 + FTS5 搜索
- ✅ 软删除 + 回收站机制

### 待开发功能

#### Sprint 2：离线转写

- [ ] 集成 sherpa-onnx NuGet 包
- [ ] Whisper base 中文模型集成
- [ ] 转写队列实现
- [ ] 重试机制（最多 3 次）
- [ ] 模型预加载（后台线程）

#### Sprint 3：列表和搜索

- [ ] 日记列表 UI 完善
- [ ] 时间段分组（上午/下午/傍晚/深夜）
- [ ] 全文搜索实现
- [ ] 虚拟滚动（无限加载）
- [ ] 按月分组展示

#### Sprint 4：UI 打磨

- [ ] 手动修正文字功能
- [ ] 音频播放功能
- [ ] 删除回收站交互
- [ ] 触觉反馈
- [ ] 动画效果

#### Sprint 5：隐私保护

- [ ] 生物识别锁
- [ ] 分级解锁逻辑
- [ ] 自动锁定配置

#### Sprint 6：快捷录音

- [ ] 桌面 Widget（Android/iOS）
- [ ] 通知栏快捷按钮
- [ ] 耳机双击触发

#### Sprint 7：导出备份

- [ ] TXT 导出
- [ ] Markdown 导出
- [ ] JSON 导出
- [ ] 音频导出
- [ ] ZIP 打包
- [ ] 备份恢复

## 待填充实现

以下服务当前为占位实现，需要后续补充：

1. **WhisperRecognizer.cs**: sherpa-onnx 集成
2. **AudioCompressor.cs**: 平台原生 M4A 压缩（Android MediaCodec / iOS AVFoundation）
3. **AudioRecorder Platform**: 各平台录音实现
4. **FTS5 初始化**: 触发器自动维护索引
5. **BiometricAuthService**: 真实生物识别接口调用

## 依赖包

```xml
<PackageReference Include="CommunityToolkit.Maui" Version="7.0.1" />
<PackageReference Include="Microsoft.Maui.Controls" Version="8.0.3" />
<PackageReference Include="sqlite-net-pcl" Version="1.9.172" />
<PackageReference Include="SQLitePCLRaw.bundle_green" Version="2.1.7" />
<PackageReference Include="SharpZipLib" Version="1.4.2" />
<PackageReference Include="System.Text.Json" Version="8.0.3" />
<PackageReference Include="XCalendar.Maui" Version="3.0.0" />
<!-- 待添加: sherpa-onnx -->
```

## 构建命令

```bash
# 恢复依赖
dotnet restore VoiceDiary/

# 构建 Android
dotnet build VoiceDiary/ -f net8.0-android

# 构建 iOS (macOS)
dotnet build VoiceDiary/ -f net8.0-ios

# 构建 Windows
dotnet build VoiceDiary/ -f net8.0-windows10.0.19041.0
```

## 下一步计划

按照需求文档中的开发节奏推进：

1. **Sprint 1 (3 天)**: 录音核心功能（已完成）
2. **Sprint 2 (3 天)**: sherpa-onnx 离线转文字
3. **Sprint 3 (3 天)**: 日记列表 + 全文搜索
4. **Sprint 4 (2 天)**: UI 打磨 + 删除回收站
5. **Sprint 5 (1-2 天)**: 隐私保护
6. **Sprint 6 (2-3 天)**: 快捷录音
7. **Sprint 7 (2 天)**: 导出 + 备份

**关键路径**: 3 周完成 MVP 版本

## 注意事项

1. **音频存储优先级**: 录音 > 转写 > 压缩，确保核心功能不受影响
2. **并发控制**: 录音时暂停转写，避免资源冲突
3. **中断安全**: WAV 容器头先写，数据追加式写入，中断后可播放
4. **数据库迁移**: 只允许 ADD COLUMN，不允许 DROP COLUMN
5. **FTS5 维护**: 使用触发器自动更新索引，避免手动维护

---

*最后更新：2026-05-02*
