@echo off
setlocal

set SCRIPT_DIR=%~dp0
powershell -ExecutionPolicy Bypass -File "%SCRIPT_DIR%scripts\build-mac.ps1" %*

if errorlevel 1 (
    echo.
    echo Mac build failed.
    exit /b %errorlevel%
)

echo.
echo Mac build finished successfully.
