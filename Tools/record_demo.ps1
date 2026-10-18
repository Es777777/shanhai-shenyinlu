param([string]$BuildDirectory='Builds/H5',[string]$Url='http://127.0.0.1:8766/',[string]$Version='0.6.0')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
$prior=@{}
foreach($key in @('SHANHAI_WEBGL_URL','SHANHAI_DEMO','SHANHAI_DEMO_DIR','SHANHAI_DEMO_VERSION','SHANHAI_DEMO_TAIL','SHANHAI_DEMO_RESUME','SHANHAI_TOUCH')){$prior[$key]=[Environment]::GetEnvironmentVariable($key)}
function ServerHash {
 $client=New-Object Net.WebClient
 try{$bytes=$client.DownloadData($Url.TrimEnd('/')+'/build-manifest.json')}finally{$client.Dispose()}
 $sha=[Security.Cryptography.SHA256]::Create()
 try{return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
}
try{
 $hash=(Get-FileHash -LiteralPath (Join-Path $BuildDirectory 'build-manifest.json') -Algorithm SHA256).Hash.ToLowerInvariant()
 if((ServerHash) -ne $hash){throw 'Recording server does not match the selected build'}
 $env:SHANHAI_WEBGL_URL=$Url;$env:SHANHAI_DEMO='1';$env:SHANHAI_TOUCH='0';$env:SHANHAI_DEMO_TAIL='0';$env:SHANHAI_DEMO_RESUME='0'
 $env:SHANHAI_DEMO_DIR='Builds/Demo-'+$Version;$env:SHANHAI_DEMO_VERSION=$Version
 New-Item -ItemType Directory -Path $env:SHANHAI_DEMO_DIR -Force | Out-Null
 [IO.File]::WriteAllText((Join-Path $projectRoot ($env:SHANHAI_DEMO_DIR+'/recording.pid')),[string]$PID)
 & node Tests/unity-playthrough.mjs
 if($LASTEXITCODE -ne 0){throw 'Recording did not finish successfully'}
 if((ServerHash) -ne $hash){throw 'Recording build changed during capture'}
 $reportPath=Join-Path $projectRoot ($env:SHANHAI_DEMO_DIR+'/results.json')
 $report=Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8 | ConvertFrom-Json
 if($report.status -ne 'passed' -or $report.segments.Count -ne 3){throw 'Recording is incomplete'}
 $report | Add-Member -NotePropertyName buildManifestSha256 -NotePropertyValue $hash -Force
 [IO.File]::WriteAllText($reportPath,($report | ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false))
 & python -X utf8 Tools/export_demo.py
 if($LASTEXITCODE -ne 0){throw 'Video export or validation failed'}
 Write-Output 'PASS: actual gameplay recording exported; visual review remains.'
}finally{foreach($key in $prior.Keys){[Environment]::SetEnvironmentVariable($key,$prior[$key])};Pop-Location}
