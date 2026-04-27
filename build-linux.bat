@echo off
setlocal

set SCRIPT_DIR=%~dp0
powershell -ExecutionPolicy Bypass -File "%SCRIPT_DIR%scripts\build-linux.ps1" %*

if errorlevel 1 (
    echo.
    echo Linux build failed.
    exit /b %errorlevel%
)

echo.
echo Linux build finished successfully.
