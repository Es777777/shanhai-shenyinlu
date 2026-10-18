$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$output=Join-Path $projectRoot 'Builds/Launcher'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $compiler /nologo /codepage:65001 /target:exe /r:System.Web.Extensions.dll ('/out:'+(Join-Path $output 'ShanHai.exe')) (Join-Path $PSScriptRoot 'GameLauncher.cs')
if($LASTEXITCODE -ne 0){throw 'Launcher compilation failed'}
