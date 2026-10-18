param([switch]$Resume)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
$previousTouch=$env:SHANHAI_TOUCH
$previousDemo=$env:SHANHAI_DEMO
$previousTail=$env:SHANHAI_DEMO_TAIL
$buildTime=(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Builds/H5/Build') -File | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).LastWriteTimeUtc
function Invoke-ExportCheck([string]$Runner,[string]$CheckScript,[string]$ReportPath) {
    if($Resume -and (Test-Path -LiteralPath $ReportPath)) {
        $existing=Get-Content -LiteralPath $ReportPath -Raw -Encoding UTF8 | ConvertFrom-Json
        if($existing.status -eq 'passed' -and (Get-Item -LiteralPath $ReportPath).LastWriteTimeUtc -ge $buildTime) {
            Write-Output ('PASS (already verified current export): '+$CheckScript)
            return
        }
    }
    & $Runner $CheckScript
    if($LASTEXITCODE -ne 0){throw ('Export verification failed: '+$CheckScript)}
}
try {
    $env:SHANHAI_DEMO='0'
    $env:SHANHAI_DEMO_TAIL='0'
    $env:SHANHAI_TOUCH='0'
    Invoke-ExportCheck node 'Tests/unity-webgl-smoke.mjs' 'Builds/QA/Unity/results.json'
    Invoke-ExportCheck node 'Tests/unity-interruption.mjs' 'Builds/QA/Unity/Interruptions/results.json'
    Invoke-ExportCheck node 'Tests/unity-playthrough.mjs' 'Builds/QA/Unity/Playthrough/results.json'
    $env:SHANHAI_TOUCH='1'
    Invoke-ExportCheck node 'Tests/unity-playthrough.mjs' 'Builds/QA/Unity/TouchPlaythrough/results.json'
    $env:SHANHAI_TOUCH='0'
    Invoke-ExportCheck node 'Tests/unity-trace-scaling.mjs' 'Builds/QA/Unity/TraceScaling/results.json'
    Invoke-ExportCheck python 'Tools/check_trace_pixels.py' 'Builds/QA/Unity/TraceScaling/pixel-check.json'
    Invoke-ExportCheck node 'Tests/unity-danxue-scene.mjs' 'Builds/QA/Unity/Danxue/results.json'
    Write-Output 'PASS: all current WebGL export checks. Review actual screenshots before release.'
}
finally {
    $env:SHANHAI_TOUCH=$previousTouch
    $env:SHANHAI_DEMO=$previousDemo
    $env:SHANHAI_DEMO_TAIL=$previousTail
    Pop-Location
}
