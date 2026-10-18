$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$compiler='C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$sources=Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Scripts/Core') -Filter '*.cs' | ForEach-Object FullName
$output=Join-Path $projectRoot 'Builds/SaveTests.exe'
& $compiler /nologo /r:System.Web.Extensions.dll /r:System.Core.dll ('/out:'+$output) @sources (Join-Path $projectRoot 'Assets/Scripts/Runtime/LocalSaves.cs') (Join-Path $projectRoot 'Assets/Scripts/Runtime/SaveArchive.cs') (Join-Path $projectRoot 'Tests/SaveTests.cs')
if($LASTEXITCODE -ne 0){throw 'Save test compilation failed'}
& $output (Join-Path $projectRoot 'Assets/Resources/Story/story.json')
if($LASTEXITCODE -ne 0){throw 'Save integrity tests failed'}
