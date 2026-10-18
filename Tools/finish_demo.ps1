param([Parameter(Mandatory=$true)][int]$RecordingProcessId)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $projectRoot
$recordingStart=(Get-Item -LiteralPath Builds/Demo/recording.pid).LastWriteTimeUtc
Write-Output 'Waiting for the complete real-game recording...'
while($true) {
    $recordingReport=Get-Item -LiteralPath Builds/Demo/results.json -ErrorAction SilentlyContinue
    if($recordingReport -and $recordingReport.LastWriteTimeUtc -ge $recordingStart) {
        $recorded=Get-Content -LiteralPath $recordingReport.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
        if($recorded.status -ne 'passed'){throw 'The recording failed. See Builds/Demo/results.json.'}
        if($recorded.segments.Count -ne 3){throw 'The recording is incomplete.'}
        break
    }
    if(-not (Get-Process -Id $RecordingProcessId -ErrorAction SilentlyContinue)){throw 'Recording process stopped without a complete report.'}
    Start-Sleep -Seconds 2
}
Write-Output 'Recording complete. Exporting MP4 with original game audio and chapters.'
& python -X utf8 Tools/export_demo.py
if($LASTEXITCODE -ne 0){throw 'MP4 export or video validation failed.'}
Write-Output 'MP4 export complete. Resuming only the remaining game checks.'
& powershell -NoProfile -ExecutionPolicy Bypass -File Tools/verify_export.ps1 -Resume
if($LASTEXITCODE -ne 0){throw 'Remaining export checks failed.'}
Write-Output 'PASS: movie export and all game checks completed. Visual review and packaging remain.'
