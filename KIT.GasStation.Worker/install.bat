@echo off
chcp 65001
setlocal EnableExtensions

set "SERVICE_NAME=KITWorker"
set "DISPLAY_NAME=KITWorker"
set "EXE_FULL=%~dp0КИТ-АЗС Сервер обмена.exe"

echo ===============================
echo Installing service: %SERVICE_NAME%
echo EXE: "%EXE_FULL%"
echo ===============================

:: Проверка прав админа
net session >nul 2>&1
if %errorlevel% neq 0 (
  echo [ERROR] Run this as Administrator.
  pause
  exit /b 1
)

:: Проверка, что exe существует
if not exist "%EXE_FULL%" (
  echo [ERROR] EXE not found: "%EXE_FULL%"
  pause
  exit /b 1
)

:: Создать/обновить службу (start=demand чтобы НЕ автозапускалась; если хочешь auto — поменяй обратно)
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorlevel%==0 (
  echo Service exists. Stopping...
  sc stop "%SERVICE_NAME%" >nul 2>&1
  timeout /t 2 /nobreak >nul

  echo Updating service config...
  sc config "%SERVICE_NAME%" binPath= "\"%EXE_FULL%\"" start= demand DisplayName= "%DISPLAY_NAME%"
) else (
  echo Creating service...
  sc create "%SERVICE_NAME%" binPath= "\"%EXE_FULL%\"" start= demand DisplayName= "%DISPLAY_NAME%"
)

:: Описание
sc description "%SERVICE_NAME%" "KIT worker service"

:: Автовосстановление
sc failure "%SERVICE_NAME%" reset= 86400 actions= restart/5000/restart/5000/restart/5000

echo ===============================
echo Grant Start/Stop rights to ALL USERS (BU)
echo ===============================

:: Даём обычным пользователям право START/STOP этой службы
:: ВАЖНО: это делается один раз админом, после этого любой юзер сможет sc start/stop
sc sdset "%SERVICE_NAME%" D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWRPWPDTLOCRRC;;;BU)

echo Starting service...
sc start "%SERVICE_NAME%"

echo Done.
pause
endlocal
