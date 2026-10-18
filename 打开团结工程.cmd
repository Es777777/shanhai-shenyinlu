@echo off
setlocal
set "SHANHAI_EDITOR=F:\Unity\Tuanjie\1.10.4\Editor\Tuanjie.exe"
if not exist "%SHANHAI_EDITOR%" set "SHANHAI_EDITOR=F:\2022.3.62t16\Editor\Tuanjie.exe"
if not exist "%SHANHAI_EDITOR%" (
  echo Tuanjie Editor was not found. Open this folder from Tuanjie Hub.
  pause
  exit /b 1
)
start "" "%SHANHAI_EDITOR%" -projectPath "%~dp0."
