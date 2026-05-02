#!/bin/bash

# sherpa-onnx Whisper 模型下载脚本（中文多语言版）
# 使用方法：./download-model.sh

MODEL_DIR="$(cd "$(dirname "$0")" && pwd)/Resources/Models"
MODEL_NAME="sherpa-onnx-whisper-base"
MODEL_URL="https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-whisper-base.tar.bz2"

echo "开始下载 Whisper base 多语言模型（支持中文）..."
echo "模型大小：约 300MB（压缩后）"
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
ln -sf base-encoder.int8.onnx encoder.onnx
ln -sf base-decoder.int8.onnx decoder.onnx
ln -sf base-tokens.txt tokens.txt

echo ""
echo "✓ 模型下载完成！"
echo ""
echo "模型文件："
ls -lh "$MODEL_DIR/$MODEL_NAME"/*.onnx "$MODEL_DIR/$MODEL_NAME"/*.txt
echo ""
echo "模型总大小：$(du -sh "$MODEL_DIR/$MODEL_NAME" | cut -f1)"
echo ""
echo "支持的语言：99 种（包括中文、英语、粤语等）"
