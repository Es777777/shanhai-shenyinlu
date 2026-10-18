@echo off
chcp 65001 >nul
cd /d "%~dp0"
if exist "ShanHai.exe" (
  "ShanHai.exe"
) else if exist "Builds\Launcher\ShanHai.exe" (
  "Builds\Launcher\ShanHai.exe"
) else (
  python Tools\start_preview.py --h5
)
if errorlevel 1 pause
