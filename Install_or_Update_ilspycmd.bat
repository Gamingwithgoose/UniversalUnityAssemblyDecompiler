@echo off
setlocal

echo ============================================================
echo Install / Update ilspycmd
echo ============================================================
echo.

dotnet tool update --global ilspycmd
if %errorlevel% neq 0 (
    echo.
    echo Update failed or ilspycmd is not installed yet. Trying install...
    dotnet tool install --global ilspycmd
)

echo.
echo Done. Close and reopen the dumper if it was already running.
pause
