# 发布输出目录（相对于当前脚本执行目录）
$OutDir = "../../dist/publish-aot-nbvg"

# 发布产物最终复制到的目录及目标文件
$TargetDir = "C:\Lib\ffmpeg"
$TargetExe = Join-Path $TargetDir "nbvg.exe"

# 执行 Windows x64 的 AOT 发布
dotnet publish -r win-x64 -c Release -o $OutDir

# 发布失败时立即退出，避免后续复制旧文件
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish 失败，退出码: $LASTEXITCODE"
    exit $LASTEXITCODE
}

# 递归清理发布目录中的 .pdb 调试符号文件
Get-ChildItem -Path $OutDir -Filter *.pdb -Recurse -ErrorAction SilentlyContinue |
    Remove-Item -Force -ErrorAction SilentlyContinue

# 递归清理发布目录中的 .lib 静态库文件
Get-ChildItem -Path $OutDir -Filter *.lib -Recurse -ErrorAction SilentlyContinue |
    Remove-Item -Force -ErrorAction SilentlyContinue

# 将发布生成的 nbvg.exe 复制到共享 ffmpeg 工具目录
$SourceExe = Join-Path $OutDir "nbvg.exe"
if (-not (Test-Path -LiteralPath $SourceExe -PathType Leaf)) {
    Write-Error "未找到发布产物: $SourceExe"
    exit 1
}

# 目标目录不存在时自动创建，然后覆盖旧文件
New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null
Copy-Item -LiteralPath $SourceExe -Destination $TargetExe -Force

Write-Host "已复制: $SourceExe -> $TargetExe"
