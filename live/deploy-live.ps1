param()

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $PSScriptRoot 'TPMQoLLive\TPMQoLLive.csproj'
$built = Join-Path $PSScriptRoot 'TPMQoLLive\bin\Release\net6.0\TPMQoLLive.dll'
$liveDir = 'C:\Program Files (x86)\Steam\steamapps\common\Two Point Museum\BepInEx\plugins\TPMQoL\live'
$target = Join-Path $liveDir 'TPMQoLLive.dll'
$tempTarget = Join-Path $liveDir 'TPMQoLLive.dll.new'
$dotnet = Join-Path $env:TEMP 'TPMQoL-dotnet6\dotnet.exe'

if (-not (Test-Path $dotnet)) {
    $dotnet = 'dotnet'
}

& $dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

New-Item -ItemType Directory -Force -Path $liveDir | Out-Null
Copy-Item -LiteralPath $built -Destination $tempTarget -Force
Move-Item -LiteralPath $tempTarget -Destination $target -Force

$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $target).Hash
Write-Output "LIVE_DEPLOYED $target"
Write-Output "SHA256 $hash"
