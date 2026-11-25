@echo off
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build_SingleExe.ps1"
set "RC=%ERRORLEVEL%"
if not "%RC%"=="0" echo Build failed. See output above.
pause
exit /b %RC%
