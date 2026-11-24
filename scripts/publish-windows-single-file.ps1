[CmdletBinding()]
param(
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectPath = Join-Path $repoRoot 'PCL.Launcher\PCL.Launcher.csproj'

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot 'artifacts\windows-x64-single-file'
}
else {
    $OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
}

$normalizedOutput = $OutputDirectory.TrimEnd([char[]]"\/")
$normalizedRepoRoot = $repoRoot.TrimEnd([char[]]"\/")
$driveRoot = [System.IO.Path]::GetPathRoot($normalizedOutput).TrimEnd([char[]]"\/")
if ($normalizedOutput -eq $driveRoot -or $normalizedOutput -eq $normalizedRepoRoot) {
    throw "拒绝使用危险的发布目录：$OutputDirectory"
}

if (Test-Path -LiteralPath $OutputDirectory) {
    Remove-Item -LiteralPath $OutputDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

& dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $OutputDirectory `
    --nologo `
    --verbosity minimal `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:IncludeSatelliteAssemblies=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=false `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    throw "单文件发布失败，dotnet publish 返回 $LASTEXITCODE。"
}

$publishedItems = @(Get-ChildItem -LiteralPath $OutputDirectory -Force -Recurse)
$unexpectedFiles = @(
    $publishedItems |
        Where-Object { -not $_.PSIsContainer -and $_.Name -ne 'PCL.Launcher.exe' }
)
$unexpectedDirectories = @($publishedItems | Where-Object { $_.PSIsContainer })
if ($unexpectedFiles.Count -gt 0 -or $unexpectedDirectories.Count -gt 0) {
    $unexpected = ($publishedItems | Select-Object -ExpandProperty FullName) -join [Environment]::NewLine
    throw "单文件发布目录包含附属文件：$([Environment]::NewLine)$unexpected"
}

$executable = Get-Item -LiteralPath (Join-Path $OutputDirectory 'PCL.Launcher.exe')
$hash = (Get-FileHash -LiteralPath $executable.FullName -Algorithm SHA256).Hash
Write-Host "单文件发布完成：$($executable.FullName)"
Write-Host "文件大小：$($executable.Length) 字节"
Write-Host "SHA256：$hash"
