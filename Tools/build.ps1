param([string]$UnityPath='',[ValidateRange(1,64)][int]$BuildJobs=2,[switch]$CleanBuild)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'Builds') -Force | Out-Null
if (-not $UnityPath) {
    $candidates=@(
        'F:/Unity/Tuanjie/1.10.4/Editor/Tuanjie.exe',
        'C:/Users/asd/AppData/Local/Programs/Unity/2022.3.62f1/Editor/Unity.exe',
        'C:/Program Files/Unity/Hub/Editor/6000.3.26f1/Editor/Unity.exe',
        'C:/Program Files/Unity/Hub/Editor/2022.3.62f1/Editor/Unity.exe'
    )
    $UnityPath=$candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $UnityPath -or -not (Test-Path -LiteralPath $UnityPath)) { throw 'Unity Editor was not found. Pass -UnityPath with the full path to Unity.exe.' }
python (Join-Path $PSScriptRoot 'import_story.py')
if($LASTEXITCODE -ne 0){throw 'Story import failed'}
$logPath=Join-Path $projectRoot 'Builds/build.log'
$helper=Join-Path $projectRoot 'Builds/BeeLimited.exe'
$csc=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $csc /nologo /target:exe ('/out:'+$helper) (Join-Path $PSScriptRoot 'BeeLimited.cs')
if($LASTEXITCODE -ne 0){throw 'Build concurrency helper compilation failed'}
$env:SHANHAI_BEE_EXECUTABLE=Join-Path (Split-Path $UnityPath -Parent) 'Data/bee_backend.exe'
$env:SHANHAI_BEE_JOBS=[string]$BuildJobs
$env:SHANHAI_BEE_WRAPPER=$helper
$env:EMCC_CORES=[string]$BuildJobs
$env:SHANHAI_CLEAN_BUILD=if($CleanBuild){'1'}else{'0'}
$buildArguments=@('-batchmode','-nographics','-quit','-job-worker-count',([string]$BuildJobs),'-projectPath',('"'+$projectRoot+'"'),'-buildTarget','WebGL','-executeMethod','ShanHai.Editor.ProjectBuilder.BuildWeb','-logFile',('"'+$logPath+'"'))
$buildProcess=Start-Process -FilePath $UnityPath -ArgumentList $buildArguments -WindowStyle Hidden -PassThru
# Wait only for the editor; licensing/background helper processes may outlive it.
$buildProcess.WaitForExit()
if($buildProcess.ExitCode -ne 0){throw 'Unity build failed. See Builds/build.log.'}
$buildLog=Get-Content -LiteralPath $logPath -Raw -Encoding UTF8
if($buildLog -notmatch 'H5_BUILD_SUCCESS' -or $buildLog -match 'Failed to read data from file.*when adding to the archive'){
    throw 'WebGL export is incomplete. Retry with -CleanBuild; then run actual browser checks.'
}
