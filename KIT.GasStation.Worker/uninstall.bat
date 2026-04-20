@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion
title КИТ-АЗС — Удаление KIT.GasStation.Worker

:: ══════════════════════════════════════════════════════════════════════════
::  Самоповышение до прав администратора через UAC.
:: ══════════════════════════════════════════════════════════════════════════
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo  Запрос прав администратора (UAC)...
    powershell -NoProfile -Command ^
        "Start-Process -FilePath '%~f0' -Verb RunAs -Wait"
    exit /b
)

set "SERVICE_NAME=KIT.GasStation.Worker"
set "SERVICE_DISPLAY=КИТ-АЗС Сервер обмена"

echo.
echo  ============================================================
echo   Удаление службы  :  %SERVICE_DISPLAY%
echo   Имя в SCM        :  %SERVICE_NAME%
echo  ============================================================
echo.

:: ── Проверка существования ────────────────────────────────────────────────
sc query "%SERVICE_NAME%" >nul 2>&1
if !errorlevel! neq 0 (
    echo  [ИНФО] Служба "%SERVICE_NAME%" не найдена — ничего не удалено.
    goto :done
)

:: ── Остановка службы ──────────────────────────────────────────────────────
echo  [ИНФО] Останавливаю службу...
net stop "%SERVICE_NAME%"
echo  [ИНФО] Жду завершения остановки...
timeout /t 4 /nobreak >nul

:: ── Удаление службы ───────────────────────────────────────────────────────
echo  [ИНФО] Удаляю службу...
sc delete "%SERVICE_NAME%"
if %errorlevel% neq 0 (
    echo  [ОШИБКА] Не удалось удалить службу. Код: %errorlevel%
    echo  [ИНФО]   Попробуйте вручную: sc delete %SERVICE_NAME%
    goto :done
)

echo.
echo  [OK]  Служба "%SERVICE_NAME%" успешно удалена.

:done
echo.
pause
