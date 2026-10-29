# 友達に渡す配布用 zip を作る。
# .NET ランタイムを含めた単一 exe（圧縮あり）と「はじめにお読みください.txt」を
# DesktopTimer フォルダに入れて zip にする。
#
# 使い方（リポジトリのルートで）：
#   powershell -ExecutionPolicy Bypass -File tools\make-dist.ps1
# 出力先を変える場合は -OutDir で指定する（既定はリポジトリの隣の DesktopTimer-dist）。

param(
    [string]$OutDir = (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'DesktopTimer-dist')
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot 'src\DesktopTimer'
$readme = Join-Path $PSScriptRoot 'dist\はじめにお読みください.txt'
$zipName = 'DesktopTimer_' + (Get-Date -Format 'yyyy-MM-dd') + '.zip'

# 作業用フォルダは毎回作り直す（前回の残りが zip に混ざらないように）
$work = Join-Path $OutDir 'work'
$publishDir = Join-Path $work 'publish'
$stageDir = Join-Path $work 'DesktopTimer'
if (Test-Path $work) { Remove-Item $work -Recurse -Force }
New-Item -ItemType Directory -Force $publishDir, $stageDir | Out-Null

# 単一 exe を出力する。EnableCompressionInSingleFile でサイズを半分以下にし、pdb は出さない
dotnet publish $project -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish に失敗しました。' }

Copy-Item (Join-Path $publishDir 'DesktopTimer.exe') $stageDir
Copy-Item $readme $stageDir

# 展開すると DesktopTimer フォルダができるよう、フォルダごと zip にする。
# Windows PowerShell の Compress-Archive は日本語のファイル名を Shift-JIS で格納するため、
# UTF-8 を指定して ZipFile で作る（展開する環境によらず文字化けしないように）
$zipPath = Join-Path $OutDir $zipName
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $stageDir, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $true, [System.Text.Encoding]::UTF8)

Remove-Item $work -Recurse -Force

$sizeMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
Write-Host ''
Write-Host "作成しました：$zipPath（$sizeMB MB）"
