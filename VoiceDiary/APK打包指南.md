# APK 打包和发布指南

**版本**: v0.3.0-sprint3  
**发布日期**: 2026-05-02  
**分支**: `260502-chore-setup-voice-diary-framework`

---

## 快速打包（一键命令）

### Windows (PowerShell)

```powershell
# 1. 切换到项目分支
cd VoiceDiary
git checkout 260502-chore-setup-voice-diary-framework

# 2. 下载模型文件（如果还没有）
./download-model.ps1

# 3. 发布 APK
dotnet publish -f net8.0-android -c Release -o ./publish

# 4. APK 位置
# ./publish/*.apk
```

### macOS / Linux

```bash
# 1. 切换到项目分支
cd VoiceDiary
git checkout 260502-chore-setup-voice-diary-framework

# 2. 下载模型文件
./download-model.sh

# 3. 发布 APK
dotnet publish -f net8.0-android -c Release -o ./publish

# 4. APK 位置
# ./publish/*.apk
```

### Visual Studio 2022

1. 打开 `VoiceDiary/VoiceDiary.csproj`
2. 顶部选择 **Release** 配置
3. 菜单：**生成** → **发布 VoiceDiary**
4. 右侧选择 **APK** → **存档**
5. 点击 **发布**

---

## 环境要求

### 必需软件

| 软件 | 版本 | 安装命令 |
|------|------|----------|
| .NET SDK | 8.0.x | https://dotnet.microsoft.com/download |
| MAUI Workload | 8.0.x | `dotnet workload install maui` |
| Android SDK | API 34+ | Visual Studio 自动安装 |
| Java JDK | 17+ | https://adoptium.net |

### 检查安装

```bash
# 检查 .NET SDK
dotnet --version  # 应该 >= 8.0.100

# 检查 MAUI
dotnet workload list  # 应该显示 maui

# 检查 Android SDK
dotnet build -t:CheckAndroidSdk
```

---

## 详细打包步骤

### 步骤 1：准备工作

```bash
# 克隆仓库（如果还没有）
git clone https://github.com/linfeng89/vioce-shorthand.git
cd vioce-shorthand

# 切换到开发分支
git checkout 260502-chore-setup-voice-diary-framework

# 拉取最新代码
git pull origin 260502-chore-setup-voice-diary-framework
```

### 步骤 2：下载模型文件

模型文件 (~433MB) 已排除在 Git 之外，需要手动下载。

**方式 1：自动下载脚本**

```bash
# Windows (PowerShell)
./VoiceDiary/download-model.ps1

# macOS / Linux (Bash)
./VoiceDiary/download-model.sh
```

**方式 2：手动下载**

1. 访问：https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-whisper-base.tar.bz2
2. 下载并解压到 `VoiceDiary/Resources/Models/`
3. 最终目录结构：
   ```
   VoiceDiary/Resources/Models/sherpa-onnx-whisper-base/
   ├── int8_weights_quant.pt
   ├── tokenizer.json
   ├── melody_encoder.json
   └── tokens.txt
   ```

### 步骤 3：配置签名（可选）

**调试版本**（推荐测试用）：
```bash
# 使用默认的 debug 签名，无需配置
dotnet publish -f net8.0-android -c Debug
```

**发布版本**（需要签名）：

1. 创建 `VoiceDiary/Properties/PublishProfiles/android.pubxml`：

```xml
<?xml version="1.0" encoding="utf-8"?>
<PublishProfile>
  <AndroidCreatePackagePerAbi>false</AndroidCreatePackagePerAbi>
  <AndroidKeyStore>true</AndroidKeyStore>
  <AndroidSigningKeyStore>voicediary.keystore</AndroidSigningKeyStore>
  <AndroidSigningKeyAlias>voicediary</AndroidSigningKeyAlias>
  <AndroidSigningStorePass>YOUR_PASSWORD</AndroidSigningStorePass>
  <AndroidSigningKeyPass>YOUR_PASSWORD</AndroidSigningKeyPass>
</PublishProfile>
```

2. 生成签名密钥（如果还没有）：

```bash
# 生成 keystore
keytool -genkey -v \
  -keystore voicediary.keystore \
  -alias voicediary \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000
```

### 步骤 4：打包 APK

**Debug 版本**（无签名要求，推荐测试）：
```bash
cd VoiceDiary
dotnet publish -f net8.0-android -c Debug -o ./publish-debug
```

**Release 版本**（需要签名）：
```bash
cd VoiceDiary
dotnet publish -f net8.0-android -c Release -o ./publish-release
```

**输出文件**：
```
publish-debug/
└── com.example.voicediary-Signed.apk  # 可直接安装

publish-release/
└── com.example.voicediary.apk         # 需要签名
```

### 步骤 5：验证 APK

```bash
# 检查 APK 信息（需要安装 aapt）
aapt dump badging ./publish-debug/com.example.voicediary-Signed.apk

# 查看权限
aapt dump permissions ./publish-debug/com.example.voicediary-Signed.apk
```

---

## 上传到 GitHub

### 方式 1：作为 Git LFS 附件

```bash
# 1. 创建 releases 目录
mkdir -p releases/v0.3.0-sprint3

# 2. 复制 APK
cp ./publish-debug/*.apk releases/v0.3.0-sprint3/vdtest-sprint3.apk

# 3. 提交
git add releases/v0.3.0-sprint3/vdtest-sprint3.apk
git commit -m "release: v0.3.0 Sprint 3 测试版 APK"
git push origin 260502-chore-setup-voice-diary-framework
```

### 方式 2：创建 GitHub Release

**命令行方式**：

```bash
# 安装 gh 工具（如果还没有）
# https://cli.github.com/

# 1. 创建 tag
git tag v0.3.0-sprint3
git push origin v0.3.0-sprint3

# 2. 创建 Release
gh release create v0.3.0-sprint3 \
  --title "Sprint 3 测试版 v0.3.0" \
  --notes "### 新功能
- 日记列表智能分组（日期 + 时间段）
- FTS5 全文搜索（中文支持）
- 无限滚动加载
- 搜索结果高亮
- UI 美化优化

### 已知问题
- 自定义日期范围选择器待实现
- 搜索历史功能待实现

### 测试重点
- 录音和回放功能
- 中文转写准确率
- 列表分组显示
- 搜索响应速度" \
  ./publish-debug/*.apk
```

**Web 界面方式**：

1. 访问：https://github.com/linfeng89/vioce-shorthand/releases/new
2. Tag version: `v0.3.0-sprint3`
3. Release title: `Sprint 3 测试版`
4. 描述：（参考上面的 release notes）
5. 上传 APK：拖拽 `vdtest-sprint3.apk` 到上传区域
6. 点击 **Publish release**

### 方式 3：GitHub Actions 自动打包（推荐）

创建 `.github/workflows/build-apk.yml`：

```yaml
name: Build Android APK

on:
  push:
    branches: [ "260502-chore-setup-voice-diary-framework" ]
  pull_request:
    branches: [ "main" ]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Setup MAUI
      run: dotnet workload install maui

    - name: Download Model
      run: |
        cd VoiceDiary
        wget https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-whisper-base.tar.bz2
        tar -xf sherpa-onnx-whisper-base.tar.bz2 -C Resources/Models/

    - name: Build APK
      run: |
        cd VoiceDiary
        dotnet publish -f net8.0-android -c Release -o ./publish

    - name: Upload APK
      uses: actions/upload-artifact@v3
      with:
        name: apk-debug
        path: VoiceDiary/publish/*.apk
        
    - name: Upload to Release (tag only)
      if: startsWith(github.ref, 'refs/tags/')
      uses: softprops/action-gh-release@v1
      with:
        files: VoiceDiary/publish/*.apk
```

---

## 测试人员安装指南

### 下载安装

1. 访问 GitHub Release 页面：
   https://github.com/linfeng89/vioce-shorthand/releases

2. 下载最新测试版 APK：
   `vdtest-sprint3.apk`

3. 在 Android 设备上打开下载的文件

### 允许未知来源

首次安装会提示"未知来源"：

1. 点击 **设置**
2. 开启 **允许来自此来源的应用**
3. 返回，点击 **安装**

### 授予权限

首次启动应用需要授予权限：

1. **麦克风权限**（必需）→ 允许
2. **存储权限**（可选）→ 允许

### 测试清单

安装完成后，请按照以下清单测试：

#### 基础功能

- [ ] 点击录音按钮，按住说话
- [ ] 松开按钮，等待处理
- [ ] 查看日记列表，确认新条目出现
- [ ] 点击条目，进入详情页
- [ ] 点击播放按钮，听录音回放

#### 列表功能

- [ ] 查看日记是否按时间段分组（🌅上午、☀️下午、🌆傍晚、🌙深夜）
- [ ] 上下滚动列表，观察是否流畅
- [ ] 快速滚动到底部，观察是否自动加载更多

#### 搜索功能

- [ ] 点击底部 🔍 按钮
- [ ] 输入关键词（如"天气"）
- [ ] 观察搜索结果是否高亮显示
- [ ] 点击清除按钮，返回完整列表
- [ ] 点击 📅 按钮，选择日期范围
- [ ] 在日期范围内搜索

#### 删除功能

- [ ] 在任意条目上向左滑动
- [ ] 出现红色删除按钮
- [ ] 点击删除
- [ ] 确认删除后条目消失

### 问题反馈

如果遇到问题，请提供以下信息：

```
【问题描述】
[简单描述问题]

【设备信息】
- 品牌型号：[如：小米 11]
- Android 版本：[如：Android 13]
- 应用版本：[如：v0.3.0-sprint3]

【复现步骤】
1. 
2. 
3. 

【预期行为】
[应该发生什么]

【实际行为】
[实际发生了什么]

【截图/录屏】
[如有，附上图片]
```

---

## 常见问题

### Q1: APK 安装失败

**错误**: "解析包时出现问题"

**解决**：
- 确保 Android 版本 >= 8.0 (API 26)
- 检查 APK 是否完整下载
- 重新下载 APK 文件

---

### Q2: 应用闪退

**错误**: 启动后立即崩溃

**解决**：
1. 检查是否授予麦克风权限
2. 清除应用数据后重试
3. 查看 logcat 日志：
   ```bash
   adb logcat | grep -i voicediary
   ```

---

### Q3: 模型文件缺失

**错误**: "Model not found" 或转写失败

**解决**：
```bash
# 重新下载模型
cd VoiceDiary
./download-model.sh

# 或手动下载并放入正确位置
# VoiceDiary/Resources/Models/sherpa-onnx-whisper-base/
```

打包时确保模型文件存在：
```bash
ls -la VoiceDiary/Resources/Models/sherpa-onnx-whisper-base/
```

---

### Q4: 打包失败

**错误**: 各种编译错误

**解决**：
1. 更新 .NET SDK：
   ```bash
   dotnet --version
   dotnet SDK update
   ```

2. 清理后重新构建：
   ```bash
   dotnet clean
   dotnet restore
   dotnet publish -f net8.0-android -c Release
   ```

3. 检查 MAUI 版本：
   ```bash
   dotnet workload list
   dotnet workload update maui
   ```

---

### Q5: 签名问题

**错误**: "Installation failed due to: failed to verify signatures"

**解决**：
- 使用 Debug 版本（无需签名）
- 或正确配置 keystore 和密码

---

## 文件清单

打包前检查以下文件是否存在：

```
VoiceDiary/
├── VoiceDiary.csproj          # ✅ 必需
├── download-model.sh          # ✅ 推荐
├── Resources/
│   └── Models/
│       └── sherpa-onnx-whisper-base/  # ✅ 必需（~433MB）
│           ├── int8_weights_quant.pt
│           ├── tokenizer.json
│           ├── melody_encoder.json
│           └── tokens.txt
├── Services/
│   ├── WhisperRecognizer.cs   # ✅ 必需
│   ├── SearchService.cs       # ✅ 必需
│   └── ...
├── Views/
│   ├── DiaryListPage.xaml     # ✅ 必需
│   ├── SearchPage.xaml        # ✅ 必需
│   └── ...
└── Platforms/
    ├── Android/
    │   └── ...                # ✅ 必需
    └── iOS/
        └── ...                # ✅ 必需
```

---

## 发布检查清单

发布前请确认：

- [ ] 所有代码已提交并推送
- [ ] 模型文件已下载（~433MB）
- [ ] 编译无错误和警告
- [ ] APK 签名配置正确（如使用 Release）
- [ ] 测试过 Debug 版本可安装
- [ ] GitHub Release 已创建
- [ ] Release Notes 已撰写
- [ ] 测试人员已收到下载链接

---

## 下一步

打包完成后：

1. 将 APK 上传到 GitHub Release
2. 分享下载链接给测试人员
3. 收集测试反馈
4. 修复发现的问题
5. 进入 Sprint 4 开发

---

**文档版本**: v1.0  
**最后更新**: 2026-05-02  
**负责人**: linfeng89
