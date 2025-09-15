@echo off
setlocal EnableDelayedExpansion

echo DeviceAgent Service Configuration Manager
echo =========================================
echo.

REM Get the directory where this script is located
set SCRIPT_DIR=%~dp0
set CONFIG_FILE=%SCRIPT_DIR%bin\Release\net8.0-windows\win-x64\publish\appsettings.json
set BACKUP_FILE=%SCRIPT_DIR%bin\Release\net8.0-windows\win-x64\publish\appsettings.backup.json

REM Check if config file exists
if not exist "%CONFIG_FILE%" (
    echo Configuration file not found: %CONFIG_FILE%
    echo Please ensure the service is installed and built.
    pause
    exit /b 1
)

:menu
echo Current Configuration:
echo =====================
findstr /C:"SyncIntervalMinutes" "%CONFIG_FILE%" 2>nul
findstr /C:"ShowGuiAtStartup" "%CONFIG_FILE%" 2>nul
findstr /C:"DatabaseTimeoutSeconds" "%CONFIG_FILE%" 2>nul
echo.
echo Options:
echo 1. Change Sync Interval
echo 2. Toggle GUI Startup
echo 3. Change Database Timeout
echo 4. Edit Full Configuration
echo 5. Backup Configuration
echo 6. Restore Configuration
echo 7. View Service Logs
echo 8. Restart Service
echo 9. Force Sync (create trigger file)
echo 0. Exit
echo.
set /p choice="Choose an option (0-9): "

if "%choice%"=="1" goto sync_interval
if "%choice%"=="2" goto toggle_gui
if "%choice%"=="3" goto db_timeout
if "%choice%"=="4" goto edit_config
if "%choice%"=="5" goto backup_config
if "%choice%"=="6" goto restore_config
if "%choice%"=="7" goto view_logs
if "%choice%"=="8" goto restart_service
if "%choice%"=="9" goto force_sync
if "%choice%"=="0" goto exit
echo Invalid choice. Please try again.
goto menu

:sync_interval
echo.
echo Current sync interval (in minutes):
findstr /C:"SyncIntervalMinutes" "%CONFIG_FILE%"
echo.
set /p new_interval="Enter new sync interval (minutes): "
if "%new_interval%"=="" goto menu

REM Create backup before changes
copy "%CONFIG_FILE%" "%BACKUP_FILE%" >nul

REM Use PowerShell to update JSON
powershell -Command "(Get-Content '%CONFIG_FILE%' | ConvertFrom-Json) | ForEach-Object { $_.DeviceAgent.SyncIntervalMinutes = [int]'%new_interval%'; $_ } | ConvertTo-Json -Depth 10 | Set-Content '%CONFIG_FILE%'"

echo Sync interval updated to %new_interval% minutes.
echo Service will pick up the change automatically.
echo.
pause
goto menu

:toggle_gui
echo.
echo Current GUI setting:
findstr /C:"ShowGuiAtStartup" "%CONFIG_FILE%"
echo.
echo 1. Enable GUI
echo 2. Disable GUI
set /p gui_choice="Choose (1-2): "

if "%gui_choice%"=="1" set gui_value=true
if "%gui_choice%"=="2" set gui_value=false
if "%gui_value%"=="" goto menu

copy "%CONFIG_FILE%" "%BACKUP_FILE%" >nul
powershell -Command "(Get-Content '%CONFIG_FILE%' | ConvertFrom-Json) | ForEach-Object { $_.DeviceAgent.ShowGuiAtStartup = [bool]'%gui_value%'; $_ } | ConvertTo-Json -Depth 10 | Set-Content '%CONFIG_FILE%'"

echo GUI startup setting updated.
echo.
pause
goto menu

:db_timeout
echo.
echo Current database timeout (in seconds):
findstr /C:"DatabaseTimeoutSeconds" "%CONFIG_FILE%"
echo.
set /p new_timeout="Enter new timeout (seconds): "
if "%new_timeout%"=="" goto menu

copy "%CONFIG_FILE%" "%BACKUP_FILE%" >nul
powershell -Command "(Get-Content '%CONFIG_FILE%' | ConvertFrom-Json) | ForEach-Object { $_.DeviceAgent.DatabaseTimeoutSeconds = [int]'%new_timeout%'; $_ } | ConvertTo-Json -Depth 10 | Set-Content '%CONFIG_FILE%'"

echo Database timeout updated to %new_timeout% seconds.
echo.
pause
goto menu

:edit_config
echo.
echo Opening configuration file in notepad...
echo Make sure to save the file when done.
echo.
pause
notepad "%CONFIG_FILE%"
goto menu

:backup_config
copy "%CONFIG_FILE%" "%BACKUP_FILE%" >nul
if %errorlevel% == 0 (
    echo Configuration backed up successfully.
    echo Backup location: %BACKUP_FILE%
) else (
    echo Failed to backup configuration.
)
echo.
pause
goto menu

:restore_config
if not exist "%BACKUP_FILE%" (
    echo No backup file found.
    echo.
    pause
    goto menu
)

copy "%BACKUP_FILE%" "%CONFIG_FILE%" >nul
if %errorlevel% == 0 (
    echo Configuration restored from backup.
) else (
    echo Failed to restore configuration.
)
echo.
pause
goto menu

:view_logs
echo Opening Event Viewer...
echo Look for DeviceAgent entries under Windows Logs > Application
start eventvwr.msc
goto menu

:restart_service
echo.
echo Restarting DeviceAgent service...
net stop DeviceAgent 2>nul
timeout /t 3 /nobreak >nul
net start DeviceAgent

if %errorlevel% == 0 (
    echo Service restarted successfully.
) else (
    echo Failed to restart service. Check if it's installed.
)
echo.
pause
goto menu

:force_sync
echo.
echo Creating force sync trigger...
set TRIGGER_FILE=%SCRIPT_DIR%bin\Release\net8.0-windows\win-x64\publish\force_sync.trigger
echo %date% %time% > "%TRIGGER_FILE%"
echo Force sync trigger created.
echo The service will detect this file and perform a sync on the next check.
echo.
pause
goto menu

:exit
echo Goodbye!
pause
