# Whisper 模型安装说明

## 模型信息

- **模型名称**: sherpa-onnx-whisper-base.en
- **语言**: 英语（base.en 是英语专用模型）
- **大小**: 约 450MB
- **来源**: https://github.com/k2-fsa/sherpa-onnx

## 快速安装

### 方法一：自动下载（推荐）

在项目根目录执行：

```bash
./VoiceDiary/download-model.sh
```

脚本将：
1. 从 GitHub Releases 下载模型
2. 解压到 `VoiceDiary/Resources/Models/` 目录
3. 自动创建符号链接

### 方法二：手动下载

1. 下载模型：
   ```bash
   wget https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-whisper-base.en.tar.bz2
   ```

2. 解压到项目目录：
   ```bash
   tar -xjf sherpa-onnx-whisper-base.en.tar.bz2 -C VoiceDiary/Resources/Models/
   ```

3. 创建符号链接：
   ```bash
   cd VoiceDiary/Resources/Models/sherpa-onnx-whisper-base.en
   ln -s base.en-encoder.onnx encoder.onnx
   ln -s base.en-decoder.onnx decoder.onnx
   ln -s base.en-tokens.txt tokens.txt
   ```

## 验证安装

安装完成后，目录结构应如下：

```
VoiceDiary/Resources/Models/sherpa-onnx-whisper-base.en/
├── encoder.onnx -> base.en-encoder.onnx
├── decoder.onnx -> base.en-decoder.onnx
├── tokens.txt -> base.en-tokens.txt
├── base.en-encoder.onnx (91MB)
├── base.en-decoder.onnx (188M)
├── base.en-encoder.int8.onnx (28M)  # 量化版本（可选）
├── base.en-decoder.int8.onnx (125M) # 量化版本（可选）
└── test_wavs/  # 测试音频
```

## 注意事项

1. **模型文件未提交到 Git**：由于文件过大（450MB），已通过 `.gitignore` 排除
2. **中文识别**：当前模型 `base.en` 仅支持英语，如需中文识别，请使用中文模型
3. **首次启动**：App 首次启动时会自动检查模型是否存在

## 中文模型（推荐）

如需支持中文，请下载中文模型：

```bash
# Whisper 多语言模型（支持中文）
./VoiceDiary/download-model.sh multi

# 或手动下载：
# https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-whisper-base.tar.bz2
```

中文模型文件结构类似，但支持 99 种语言（包括中文）。

## 更新日志

- 2026-05-02: 添加自动下载脚本
- 2026-05-02: 支持符号链接自动创建

---

参考文档：
- sherpa-onnx GitHub: https://github.com/k2-fsa/sherpa-onnx
- Whisper 模型列表：https://github.com/k2-fsa/sherpa-onnx/releases/tag/asr-models
