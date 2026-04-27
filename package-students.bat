@echo off
setlocal

set SCRIPT_DIR=%~dp0
powershell -ExecutionPolicy Bypass -File "%SCRIPT_DIR%scripts\package-students.ps1" %*

if errorlevel 1 (
    echo.
    echo Packaging failed.
    exit /b %errorlevel%
)

echo.
echo Student installer finished successfully.
