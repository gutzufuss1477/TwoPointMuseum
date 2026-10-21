param([Parameter(Mandatory=$true)][string]$GameDirectory)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$plugin=Join-Path $root 'src/TPMQoL/TPMQoL.csproj'
& dotnet build $plugin -c Release "-p:TPMGameDir=$GameDirectory"
if($LASTEXITCODE){throw 'Plugin build failed'}
$payload=Join-Path $root 'src/TPMQoL/bin/Release/net6.0/TPMQoL.dll'
$trainer=Join-Path $root 'trainer/TPMQoLTrainer/TPMQoLTrainer.csproj'
& dotnet publish $trainer -c Release -r win-x64 --self-contained true "-p:PluginPayload=$payload" -o (Join-Path $root 'dist')
if($LASTEXITCODE){throw 'Trainer build failed'}
