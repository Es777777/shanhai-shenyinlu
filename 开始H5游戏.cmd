@echo off
chcp 65001 >nul
cd /d "%~dp0"
python Tools\start_preview.py --h5
if errorlevel 1 pause
