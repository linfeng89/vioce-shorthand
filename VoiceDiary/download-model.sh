#!/bin/bash

# sherpa-onnx Whisper 模型下载脚本
# 使用方法：./download-model.sh

MODEL_DIR="$(cd "$(dirname "$0")" && pwd)/Resources/Models"
MODEL_NAME="sherpa-onnx-whisper-base.en"
MODEL_URL="https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-whisper-base.en.tar.bz2"

echo "开始下载 Whisper base 模型..."
echo "模型大小：约 150MB"
echo "下载目录：$MODEL_DIR"
echo ""

mkdir -p "$MODEL_DIR"

if [ -f "$MODEL_DIR/$MODEL_NAME/encoder.onnx" ]; then
    echo "✓ 模型已存在，跳过下载"
    exit 0
fi

echo "正在下载模型..."
curl -L -# "$MODEL_URL" -o "/tmp/sherpa-model.tar.bz2"

if [ $? -ne 0 ]; then
    echo "✗ 下载失败，请检查网络连接"
    exit 1
fi

echo ""
echo "正在解压模型..."
tar -xjf "/tmp/sherpa-model.tar.bz2" -C "$MODEL_DIR"
rm -f "/tmp/sherpa-model.tar.bz2"

cd "$MODEL_DIR/$MODEL_NAME"
ln -sf base.en-encoder.onnx encoder.onnx
ln -sf base.en-decoder.onnx decoder.onnx
ln -sf base.en-tokens.txt tokens.txt

echo ""
echo "✓ 模型下载完成！"
ls -lh "$MODEL_DIR/$MODEL_NAME"/*.onnx "$MODEL_DIR/$MODEL_NAME"/*.txt
echo ""
echo "模型总大小：$(du -sh "$MODEL_DIR/$MODEL_NAME" | cut -f1)"
