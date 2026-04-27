@echo off
setlocal

set SCRIPT_DIR=%~dp0
powershell -ExecutionPolicy Bypass -File "%SCRIPT_DIR%scripts\package-linux.ps1" %*

if errorlevel 1 (
    echo.
    echo Linux packaging failed.
    exit /b %errorlevel%
)

echo.
echo Linux package finished successfully.
