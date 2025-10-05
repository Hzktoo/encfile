#!/bin/bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
mkdir -p "$HOME/.local/bin"
cp bin/Release/net9.0/linux-x64/publish/AesFileEncryptor "$HOME/.local/bin/encfile"
chmod +x "$HOME/.local/bin/encfile"
echo "✓ Installation complete! Usage: encfile <file>"