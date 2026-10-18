param([string]$EditorRoot='')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
if(-not $EditorRoot){
    $EditorRoot='F:/Unity/Tuanjie/1.10.4/Editor'
    if(-not (Test-Path -LiteralPath $EditorRoot)){$EditorRoot='C:/Users/asd/AppData/Local/Programs/Unity/2022.3.62f1/Editor'}
}
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'Builds') -Force | Out-Null
$data=Join-Path $EditorRoot 'Data'
$mono=Join-Path $data 'MonoBleedingEdge/bin/mono.exe'
$compiler=Join-Path $data 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$response=Join-Path $projectRoot 'Builds/unity-compile.rsp'
$arguments=@('/nologo','/target:library','/langversion:latest','/define:UNITY_EDITOR,UNITY_EDITOR_WIN',('/out:"'+(Join-Path $projectRoot 'Builds/UnityCompileCheck.dll')+'"'))
$arguments+=Get-ChildItem -LiteralPath (Join-Path $data 'Managed/UnityEngine') -Filter '*.dll' | ForEach-Object { '/r:"'+$_.FullName+'"' }
$arguments+='/r:"'+(Join-Path $data 'MonoBleedingEdge/lib/mono/4.5/Facades/netstandard.dll')+'"'
$arguments+=Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Scripts'),(Join-Path $projectRoot 'Assets/Editor') -Recurse -Filter '*.cs' | ForEach-Object { '"'+$_.FullName+'"' }
$arguments | Set-Content -LiteralPath $response -Encoding UTF8
& $mono $compiler ('@'+$response)
if($LASTEXITCODE -ne 0){throw 'Unity reference compilation failed'}
Write-Output 'PASS: All runtime and editor scripts compile against installed Unity assemblies.'
