$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'Builds') -Force | Out-Null
$compiler = 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$testOutput = Join-Path $projectRoot 'Builds/CoreTests.exe'
$sources = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Scripts/Core') -Filter '*.cs' | ForEach-Object FullName
& $compiler /nologo /r:System.Web.Extensions.dll /r:System.Core.dll "/out:$testOutput" @sources (Join-Path $projectRoot 'Tests/CoreTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Core compilation failed' }
& $testOutput (Join-Path $projectRoot 'Assets/Resources/Story/story.json')
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed' }
