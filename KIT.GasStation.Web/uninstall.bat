@echo off
setlocal

set SERVICE_NAME=KITWeb

net session >nul 2>&1
if %errorlevel% neq 0 (
  echo [ERROR] Run this as Administrator.
  pause
  exit /b 1
)

echo Stopping service...
sc stop "%SERVICE_NAME%" >nul 2>&1
timeout /t 2 /nobreak >nul

echo Deleting service...
sc delete "%SERVICE_NAME%"

echo Removed.
pause
endlocal
