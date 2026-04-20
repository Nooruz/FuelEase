@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion
title КИТ-АЗС — Установка KIT.GasStation.Web

:: ══════════════════════════════════════════════════════════════════════════
::  Самоповышение до прав администратора через UAC.
::  Если скрипт запущен без прав — PowerShell перезапускает его от имени
::  администратора и ждёт завершения. Окно исходного процесса закрывается.
:: ══════════════════════════════════════════════════════════════════════════
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo  Запрос прав администратора (UAC)...
    powershell -NoProfile -Command ^
        "Start-Process -FilePath '%~f0' -Verb RunAs -Wait"
    exit /b
)

cd /d "%~dp0"

set "SERVICE_NAME=KIT.GasStation.Web"
set "SERVICE_DISPLAY=КИТ-АЗС Веб-сервер"
set "SERVICE_DESC=HTTP API и SignalR хаб для управления ТРК (КИТ-АЗС)"
set "SERVICE_EXE=%~dp0KIT.GasStation.Web.exe"

echo.
echo  ============================================================
echo   Установка службы  :  %SERVICE_DISPLAY%
echo   Имя в SCM         :  %SERVICE_NAME%
echo   Автозапуск        :  Да (при старте Windows)
echo  ============================================================
echo.

:: ── Проверка исполняемого файла ───────────────────────────────────────────
if not exist "%SERVICE_EXE%" (
    echo  [ОШИБКА] Исполняемый файл не найден:
    echo           %SERVICE_EXE%
    echo.
    echo  Убедитесь, что скрипт находится рядом с опубликованным приложением.
    goto :done
)
echo  [OK]  Файл найден: %SERVICE_EXE%

:: ── Переустановка, если служба уже существует ─────────────────────────────
sc query "%SERVICE_NAME%" >nul 2>&1
if !errorlevel!==0 (
    echo.
    echo  [ИНФО] Служба уже установлена — выполняю переустановку.
    echo  [ИНФО] Останавливаю службу...
    net stop "%SERVICE_NAME%"
    echo  [ИНФО] Жду завершения остановки...
    timeout /t 4 /nobreak >nul
    echo  [ИНФО] Удаляю старую службу...
    sc delete "%SERVICE_NAME%"
    if !errorlevel! neq 0 (
        echo  [ОШИБКА] Не удалось удалить службу. Код: !errorlevel!
        echo  [ИНФО]   Попробуйте удалить вручную: sc delete %SERVICE_NAME%
        goto :done
    )
    echo  [ИНФО] Жду регистрации удаления в SCM...
    timeout /t 3 /nobreak >nul
)

:: ── Создание службы ───────────────────────────────────────────────────────
echo.
echo  [ИНФО] Создаю службу...
sc create "%SERVICE_NAME%" ^
    binPath= "\"%SERVICE_EXE%\"" ^
    start= auto ^
    DisplayName= "%SERVICE_DISPLAY%"
if %errorlevel% neq 0 (
    echo  [ОШИБКА] sc create завершился с кодом: %errorlevel%
    goto :done
)

:: Описание службы
sc description "%SERVICE_NAME%" "%SERVICE_DESC%" >nul

:: Авто-перезапуск при сбое: через 5с, 10с, 30с; счётчик сбросится через 24ч
sc failure "%SERVICE_NAME%" reset= 86400 actions= restart/5000/restart/10000/restart/30000 >nul

echo  [OK]  Служба создана.

:: ── Запуск службы ─────────────────────────────────────────────────────────
echo  [ИНФО] Запускаю службу...
sc start "%SERVICE_NAME%"
set "START_ERR=!errorlevel!"

echo.
if !START_ERR!==0 (
    echo  [OK]  Служба запущена успешно.
) else (
    echo  [ПРЕДУПРЕЖДЕНИЕ] sc start вернул код: !START_ERR!
    echo  [ИНФО] Служба зарегистрирована, но могла не запуститься.
    echo  [ИНФО] Проверьте Журнал событий Windows ^(eventvwr.msc^).
)

:: ── Итоговый статус ───────────────────────────────────────────────────────
echo.
echo  ── Статус службы ──────────────────────────────────────────
sc query "%SERVICE_NAME%"
echo  ───────────────────────────────────────────────────────────

:done
echo.
pause
