# DeviceAgent Remote Management Script

param(
    [Parameter(Mandatory=$false)]
    [string]$ComputerName = $env:COMPUTERNAME,

    [Parameter(Mandatory=$false)]
    [string]$ServiceName = "DeviceAgent",

    [Parameter(Mandatory=$false)]
    [ValidateSet("Status", "Start", "Stop", "Restart", "ForceSync", "UpdateConfig", "ViewLogs")]
    [string]$Action = "Status"
)

function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    } else {
        $input | Write-Output
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

function Get-ServiceStatus {
    param($Computer, $Service)

    try {
        $service = Get-CimInstance -ClassName Win32_Service -ComputerName $Computer -Filter "Name='$Service'" -ErrorAction Stop
        if ($service) {
            Write-Host "Service Status on $($Computer):" -ForegroundColor Cyan
            Write-Host "  Name: $($service.Name)"
            Write-Host "  Display Name: $($service.DisplayName)"
            Write-Host "  Status: $($service.State)" -ForegroundColor $(if ($service.State -eq 'Running') {'Green'} else {'Red'})
            Write-Host "  Start Type: $($service.StartMode)"
            return $service.State
        } else {
            Write-Host "Service '$Service' not found on $Computer" -ForegroundColor Red
            return $null
        }
    } catch {
        Write-Host "Error accessing service on ${Computer}: $_" -ForegroundColor Red
        return $null
    }
}

function Start-RemoteService {
    param($Computer, $Service)

    Write-Host "Starting service '$Service' on $Computer..." -ForegroundColor Yellow
    try {
        Invoke-Command -ComputerName $Computer -ScriptBlock {
            Start-Service -Name $using:Service
        }
        Start-Sleep -Seconds 3
        $status = Get-ServiceStatus -Computer $Computer -Service $Service
        if ($status -eq 'Running') {
            Write-Host "Service started successfully!" -ForegroundColor Green
        }
    } catch {
        Write-Host "Failed to start service: $_" -ForegroundColor Red
    }
}

function Stop-RemoteService {
    param($Computer, $Service)

    Write-Host "Stopping service '$Service' on $Computer..." -ForegroundColor Yellow
    try {
        Invoke-Command -ComputerName $Computer -ScriptBlock {
            Stop-Service -Name $using:Service -Force
        }
        Start-Sleep -Seconds 3
        $status = Get-ServiceStatus -Computer $Computer -Service $Service
        if ($status -eq 'Stopped') {
            Write-Host "Service stopped successfully!" -ForegroundColor Green
        }
    } catch {
        Write-Host "Failed to stop service: $_" -ForegroundColor Red
    }
}

function Restart-RemoteService {
    param($Computer, $Service)

    Write-Host "Restarting service '$Service' on $Computer..." -ForegroundColor Yellow
    Stop-RemoteService -Computer $Computer -Service $Service
    Start-Sleep -Seconds 2
    Start-RemoteService -Computer $Computer -Service $Service
}

function Invoke-ForceSync {
    param($Computer, $Service)

    Write-Host "Creating force sync trigger on $Computer..." -ForegroundColor Yellow

    try {
        $serviceInfo = Get-CimInstance -Class Win32_Service -ComputerName $Computer -Filter "Name='$Service'"
        if ($serviceInfo) {
            $exePath = $serviceInfo.PathName.Trim('"')
            $exeDir = Split-Path $exePath -Parent
            $triggerFile = Join-Path $exeDir "force_sync.trigger"

            $remotePath = $triggerFile -replace '^([A-Z]):', "\\$Computer\`$1$"
            $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

            "Force sync requested at $timestamp" | Out-File -FilePath $remotePath -Encoding UTF8

            Write-Host "Force sync trigger created successfully!" -ForegroundColor Green
        } else {
            Write-Host "Could not find service information." -ForegroundColor Red
        }
    } catch {
        Write-Host "Failed to create force sync trigger: $_" -ForegroundColor Red
    }
}

function Update-RemoteConfig {
    param($Computer, $Service)

    Write-Host "Configuration update options:" -ForegroundColor Cyan
    Write-Host "1. Change sync interval"
    Write-Host "2. Toggle GUI startup"
    Write-Host "3. Change database timeout"

    $choice = Read-Host "Choose option (1-3)"

    try {
        $serviceInfo = Get-CimInstance -Class Win32_Service -ComputerName $Computer -Filter "Name='$Service'"
        if ($serviceInfo) {
            $exePath = $serviceInfo.PathName.Trim('"')
            $exeDir = Split-Path $exePath -Parent
            $configFile = Join-Path $exeDir "appsettings.json"

            $remoteConfigPath = $configFile -replace '^([A-Z]):', "\\$Computer\`$1$"

            if (Test-Path $remoteConfigPath) {
                $config = Get-Content $remoteConfigPath | ConvertFrom-Json

                switch ($choice) {
                    "1" {
                        $newInterval = Read-Host "Enter new sync interval (minutes)"
                        if ($newInterval -match '^\d+$') {
                            $config.DeviceAgent.SyncIntervalMinutes = [int]$newInterval
                            Write-Host "Sync interval updated." -ForegroundColor Green
                        }
                    }
                    "2" {
                        $config.DeviceAgent.ShowGuiAtStartup = !$config.DeviceAgent.ShowGuiAtStartup
                        Write-Host "GUI startup toggled to: $($config.DeviceAgent.ShowGuiAtStartup)" -ForegroundColor Green
                    }
                    "3" {
                        $newTimeout = Read-Host "Enter new database timeout (seconds)"
                        if ($newTimeout -match '^\d+$') {
                            $config.DeviceAgent.DatabaseTimeoutSeconds = [int]$newTimeout
                            Write-Host "Database timeout updated." -ForegroundColor Green
                        }
                    }
                }

                $config | ConvertTo-Json -Depth 10 | Set-Content $remoteConfigPath
                Write-Host "Configuration updated successfully!" -ForegroundColor Green
            } else {
                Write-Host "Configuration file not found: $remoteConfigPath" -ForegroundColor Red
            }
        }
    } catch {
        Write-Host "Failed to update configuration: $_" -ForegroundColor Red
    }
}

function Show-RemoteLogs {
    param($Computer, $Service)

    Write-Host "Retrieving recent logs for '$Service' on $Computer..." -ForegroundColor Yellow

    try {
        $events = Get-WinEvent -ComputerName $Computer -FilterHashtable @{LogName='Application'; StartTime=(Get-Date).AddHours(-24)} -MaxEvents 50 -ErrorAction SilentlyContinue |
                  Where-Object { $_.ProviderName -like "*DeviceAgent*" -or $_.Message -like "*DeviceAgent*" } |
                  Sort-Object TimeCreated -Descending |
                  Select-Object -First 20

        if ($events) {
            Write-Host "`nRecent DeviceAgent logs (last 24 hours):" -ForegroundColor Cyan
            Write-Host "=" * 50

            foreach ($logEvent in $events) {
                $color = switch ($logEvent.LevelDisplayName) {
                    'Error' { 'Red' }
                    'Warning' { 'Yellow' }
                    'Information' { 'White' }
                    default { 'Gray' }
                }

                Write-Host "[$($logEvent.TimeCreated)] [$($logEvent.LevelDisplayName)]" -ForegroundColor $color
                Write-Host $logEvent.Message
                Write-Host "-" * 50
            }
        } else {
            Write-Host "No recent DeviceAgent events found." -ForegroundColor Yellow
        }
    } catch {
        Write-Host "Failed to retrieve logs: $_" -ForegroundColor Red
    }
}

# Main execution
Write-Host "DeviceAgent Remote Management" -ForegroundColor Cyan
Write-Host "Target Computer: $ComputerName" -ForegroundColor White
Write-Host "Service: $ServiceName" -ForegroundColor White
Write-Host "Action: $Action" -ForegroundColor White
Write-Host ""

switch ($Action.ToLower()) {
    "status" {
        Get-ServiceStatus -Computer $ComputerName -Service $ServiceName
    }
    "start" {
        Start-RemoteService -Computer $ComputerName -Service $ServiceName
    }
    "stop" {
        Stop-RemoteService -Computer $ComputerName -Service $ServiceName
    }
    "restart" {
        Restart-RemoteService -Computer $ComputerName -Service $ServiceName
    }
    "forcesync" {
        Invoke-ForceSync -Computer $ComputerName -Service $ServiceName
    }
    "updateconfig" {
        Update-RemoteConfig -Computer $ComputerName -Service $ServiceName
    }
    "viewlogs" {
        Show-RemoteLogs -Computer $ComputerName -Service $ServiceName
    }
    default {
        Write-Host "Invalid action specified." -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Management complete." -ForegroundColor Green
