@echo off
echo DeviceAgent Service Management
echo =============================
echo.
echo This script must be run as Administrator!
echo.

:menu
echo 1. Install Service
echo 2. Uninstall Service
echo 3. Start Service
echo 4. Stop Service
echo 5. Check Service Status
echo 6. View Service Logs
echo 7. Exit
echo.
set /p choice="Choose an option (1-7): "

if "%choice%"=="1" goto install
if "%choice%"=="2" goto uninstall
if "%choice%"=="3" goto start
if "%choice%"=="4" goto stop
if "%choice%"=="5" goto status
if "%choice%"=="6" goto logs
if "%choice%"=="7" goto exit
echo Invalid choice. Please try again.
goto menu

:install
echo Installing DeviceAgent service...
powershell.exe -ExecutionPolicy Bypass -File "%~dp0install-service.ps1"
pause
goto menu

:uninstall
echo Uninstalling DeviceAgent service...
powershell.exe -ExecutionPolicy Bypass -File "%~dp0install-service.ps1" -Uninstall
pause
goto menu

:start
echo Starting DeviceAgent service...
net start DeviceAgent
echo.
if errorlevel 1 (
    echo Failed to start service. Check if it's installed and not already running.
) else (
    echo Service started successfully!
)
pause
goto menu

:stop
echo Stopping DeviceAgent service...
net stop DeviceAgent
echo.
if errorlevel 1 (
    echo Failed to stop service. Check if it's running.
) else (
    echo Service stopped successfully!
)
pause
goto menu

:status
echo DeviceAgent service status:
sc query DeviceAgent
echo.
pause
goto menu

:logs
echo Opening Event Viewer...
echo Look for DeviceAgent entries under Windows Logs > Application
eventvwr.msc
goto menu

:exit
echo Goodbye!
