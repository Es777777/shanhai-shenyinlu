param([switch]$Resume,[string]$BuildDirectory='Builds/H5',[string]$Url='http://127.0.0.1:8766/',[ValidateSet('all','routes','other')][string]$Group='all')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
$previousTouch=$env:SHANHAI_TOUCH
$previousDemo=$env:SHANHAI_DEMO
$previousTail=$env:SHANHAI_DEMO_TAIL
$previousUrl=$env:SHANHAI_WEBGL_URL
$buildRoot=Join-Path $projectRoot $BuildDirectory
$buildTime=(Get-ChildItem -LiteralPath (Join-Path $buildRoot 'Build') -File | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).LastWriteTimeUtc
$manifestHash=(Get-FileHash -LiteralPath (Join-Path $buildRoot 'build-manifest.json') -Algorithm SHA256).Hash.ToLowerInvariant()
$client=New-Object System.Net.WebClient
try {$remoteBytes=$client.DownloadData($Url.TrimEnd('/')+'/build-manifest.json')} finally {$client.Dispose()}
$sha=[System.Security.Cryptography.SHA256]::Create()
try {$remoteHash=([BitConverter]::ToString($sha.ComputeHash($remoteBytes))).Replace('-','').ToLowerInvariant()} finally {$sha.Dispose()}
if($remoteHash -ne $manifestHash){throw 'The test server is serving a different export.'}
function Invoke-ExportCheck([string]$Runner,[string]$CheckScript,[string]$ReportPath) {
    if($Resume -and (Test-Path -LiteralPath $ReportPath)) {
        $existing=Get-Content -LiteralPath $ReportPath -Raw -Encoding UTF8 | ConvertFrom-Json
        if($existing.status -eq 'passed' -and $existing.buildManifestSha256 -eq $manifestHash -and (Get-Item -LiteralPath $ReportPath).LastWriteTimeUtc -ge $buildTime) {
            Write-Output ('PASS (already verified current export): '+$CheckScript)
            return
        }
    }
    & $Runner $CheckScript
    if($LASTEXITCODE -ne 0){throw ('Export verification failed: '+$CheckScript)}
    $result=Get-Content -LiteralPath $ReportPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if($result.status -ne 'passed'){throw ('Export report is not passed: '+$CheckScript)}
    $result | Add-Member -NotePropertyName buildManifestSha256 -NotePropertyValue $manifestHash -Force
    [IO.File]::WriteAllText((Join-Path $projectRoot $ReportPath),($result | ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false))
}
try {
    $env:SHANHAI_WEBGL_URL=$Url
    $env:SHANHAI_DEMO='0'
    $env:SHANHAI_DEMO_TAIL='0'
    $env:SHANHAI_TOUCH='0'
    if($Group -ne 'routes') {
    Invoke-ExportCheck node 'Tests/unity-backups.mjs' 'Builds/QA/Unity/Backups/results.json'
    Invoke-ExportCheck node 'Tests/unity-usability.mjs' 'Builds/QA/Unity/Usability/results.json'
    Invoke-ExportCheck node 'Tests/unity-reading-layout.mjs' 'Builds/QA/Unity/Reading/results.json'
    Invoke-ExportCheck node 'Tests/unity-quality.mjs' 'Builds/QA/Unity/Quality/results.json'
    Invoke-ExportCheck node 'Tests/unity-art-retry.mjs' 'Builds/QA/Unity/ArtRetry/results.json'
    Invoke-ExportCheck node 'Tests/unity-webgl-smoke.mjs' 'Builds/QA/Unity/results.json'
    Invoke-ExportCheck node 'Tests/unity-interruption.mjs' 'Builds/QA/Unity/Interruptions/results.json'
    }
    if($Group -ne 'other') {
    Invoke-ExportCheck node 'Tests/unity-playthrough.mjs' 'Builds/QA/Unity/Playthrough/results.json'
    $env:SHANHAI_TOUCH='1'
    Invoke-ExportCheck node 'Tests/unity-playthrough.mjs' 'Builds/QA/Unity/TouchPlaythrough/results.json'
    }
    $env:SHANHAI_TOUCH='0'
    if($Group -ne 'routes') {
    Invoke-ExportCheck node 'Tests/unity-trace-scaling.mjs' 'Builds/QA/Unity/TraceScaling/results.json'
    Invoke-ExportCheck python 'Tools/check_trace_pixels.py' 'Builds/QA/Unity/TraceScaling/pixel-check.json'
    Invoke-ExportCheck node 'Tests/unity-danxue-scene.mjs' 'Builds/QA/Unity/Danxue/results.json'
    }
    Write-Output ('PASS: current WebGL export checks ('+$Group+'). Review actual screenshots before release.')
}
finally {
    $env:SHANHAI_TOUCH=$previousTouch
    $env:SHANHAI_DEMO=$previousDemo
    $env:SHANHAI_DEMO_TAIL=$previousTail
    $env:SHANHAI_WEBGL_URL=$previousUrl
    Pop-Location
}
