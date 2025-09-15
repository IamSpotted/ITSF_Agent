# DeviceAgent Windows Service Installation Script
# Run this script as Administrator
#
# Examples:
#   .\install-service.ps1
#   .\install-service.ps1 -ExecutablePath "C:\DeviceAgent\DeviceAgent.exe"
#   .\install-service.ps1 -ServiceAccount "DOMAIN\ServiceUser"
#   .\install-service.ps1 -ServiceAccount "DOMAIN\ServiceUser" -ServicePassword (Read-Host "Password" -AsSecureString)
#   .\install-service.ps1 -ExecutablePath "C:\DeviceAgent\DeviceAgent.exe" -ServiceAccount "DOMAIN\ServiceUser"
#   .\install-service.ps1 -Uninstall

param(
    [Parameter(Mandatory=$false)]
    [string]$ServiceName = "DeviceAgent",
    
    [Parameter(Mandatory=$false)]
    [string]$DisplayName = "Device Agent Service",
    
    [Parameter(Mandatory=$false)]
    [string]$Description = "Collects device information and syncs with database",
    
    [Parameter(Mandatory=$false)]
    [string]$ExecutablePath = "",
    
    [Parameter(Mandatory=$false)]
    [string]$ServiceAccount = "",
    
    [Parameter(Mandatory=$false)]
    [SecureString]$ServicePassword,
    
    [Parameter(Mandatory=$false)]
    [switch]$Uninstall
)

# Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator. Right-click PowerShell and select 'Run as Administrator'"
    exit 1
}

# Set default executable path if not provided
if ([string]::IsNullOrEmpty($ExecutablePath)) {
    # Try multiple common locations
    $PossiblePaths = @(
        "C:\DeviceAgent\DeviceAgent.exe",
        (Join-Path $PSScriptRoot "DeviceAgent.exe"),
        (Join-Path $PSScriptRoot "bin\Release\net8.0-windows\win-x64\publish\DeviceAgent.exe"),
        (Join-Path $PSScriptRoot "bin\Release\net8.0-windows\DeviceAgent.exe")
    )
    
    foreach ($Path in $PossiblePaths) {
        if (Test-Path $Path) {
            $ExecutablePath = $Path
            Write-Host "Found DeviceAgent.exe at: $ExecutablePath" -ForegroundColor Green
            break
        }
    }
}

# Check if executable exists
if ([string]::IsNullOrEmpty($ExecutablePath) -or -not (Test-Path $ExecutablePath)) {
    Write-Error "DeviceAgent.exe not found."
    Write-Host "Searched locations:" -ForegroundColor Yellow
    Write-Host "  - C:\DeviceAgent\DeviceAgent.exe"
    Write-Host "  - $PSScriptRoot\DeviceAgent.exe"
    Write-Host "  - $PSScriptRoot\bin\Release\net8.0-windows\win-x64\publish\DeviceAgent.exe"
    Write-Host "  - $PSScriptRoot\bin\Release\net8.0-windows\DeviceAgent.exe"
    Write-Host ""
    Write-Host "Please specify the path using -ExecutablePath parameter or build the project:" -ForegroundColor Cyan
    Write-Host "  dotnet publish --framework net8.0-windows --configuration Release --self-contained true --runtime win-x64"
    exit 1
}

if ($Uninstall) {
    # Uninstall the service
    Write-Host "Uninstalling $ServiceName service..."
    
    # Stop the service if it's running
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service) {
        if ($service.Status -eq 'Running') {
            Write-Host "Stopping service..."
            Stop-Service -Name $ServiceName -Force
            Start-Sleep -Seconds 3
        }
        
        Write-Host "Removing service..."
        sc.exe delete $ServiceName
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Service '$ServiceName' has been successfully uninstalled." -ForegroundColor Green
        } else {
            Write-Error "Failed to uninstall service. Exit code: $LASTEXITCODE"
        }
    } else {
        Write-Host "Service '$ServiceName' is not installed." -ForegroundColor Yellow
    }
} else {
    # Install the service
    Write-Host "Installing $ServiceName service..."
    Write-Host "Executable: $ExecutablePath"
    
    # Check if service already exists
    $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-Error "Service '$ServiceName' already exists. Use -Uninstall to remove it first."
        exit 1
    }
    
    # Create the service
    sc.exe create $ServiceName binPath= "`"$ExecutablePath`"" DisplayName= $DisplayName start= auto
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Service created successfully." -ForegroundColor Green
        
        # Set service description
        sc.exe description $ServiceName $Description | Out-Null
        
        # Configure service to restart on failure
        sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
        
        # Configure service account if specified
        if (-not [string]::IsNullOrEmpty($ServiceAccount)) {
            Write-Host "Configuring service to run as: $ServiceAccount" -ForegroundColor Yellow
            
            if ($null -eq $ServicePassword) {
                $SecurePassword = Read-Host "Enter password for $ServiceAccount" -AsSecureString
            } else {
                $SecurePassword = $ServicePassword
            }
            
            $Credential = New-Object System.Management.Automation.PSCredential($ServiceAccount, $SecurePassword)
            
            try {
                # Use WMI to configure the service account
                $service = Get-WmiObject -Class Win32_Service -Filter "Name='$ServiceName'"
                $result = $service.Change($null, $null, $null, $null, $null, $null, $ServiceAccount, $Credential.GetNetworkCredential().Password)
                
                if ($result.ReturnValue -eq 0) {
                    Write-Host "Service account configured successfully." -ForegroundColor Green
                } else {
                    Write-Warning "Failed to configure service account. Return code: $($result.ReturnValue)"
                    Write-Host "You can manually configure the service account in services.msc" -ForegroundColor Yellow
                }
            } catch {
                Write-Warning "Error configuring service account: $($_.Exception.Message)"
                Write-Host "You can manually configure the service account in services.msc" -ForegroundColor Yellow
            }
            
            # Clear the credential from memory
            $SecurePassword = $null
            $Credential = $null
        } else {
            Write-Host ""
            Write-Warning "Service will run as Local System by default."
            Write-Host "For better security and database access, consider configuring a service account:" -ForegroundColor Yellow
            Write-Host "  1. Create a domain/local user with local admin rights and SQL access"
            Write-Host "  2. Run: services.msc -> DeviceAgent -> Properties -> Log On tab"
            Write-Host "  3. Select 'This account' and enter the service account credentials"
            Write-Host "  Or re-run this script with -ServiceAccount and -ServicePassword parameters"
        }
        
        Write-Host "Service configuration:"
        Write-Host "  Name: $ServiceName"
        Write-Host "  Display Name: $DisplayName"
        Write-Host "  Description: $Description"
        Write-Host "  Executable: $ExecutablePath"
        Write-Host "  Startup Type: Automatic"
        Write-Host "  Restart on Failure: Yes"
        if (-not [string]::IsNullOrEmpty($ServiceAccount)) {
            Write-Host "  Service Account: $ServiceAccount"
        } else {
            Write-Host "  Service Account: Local System (NT AUTHORITY\SYSTEM)"
        }
        
        # Ask if user wants to start the service now
        $startNow = Read-Host "Start the service now? (y/n)"
        if ($startNow -eq 'y' -or $startNow -eq 'Y') {
            Write-Host "Starting service..."
            Start-Service -Name $ServiceName
            Start-Sleep -Seconds 2
            
            $service = Get-Service -Name $ServiceName
            if ($service.Status -eq 'Running') {
                Write-Host "Service started successfully!" -ForegroundColor Green
            } else {
                Write-Warning "Service failed to start. Check Event Viewer for details."
            }
        }
        
        Write-Host ""
        Write-Host "Service Management Commands:" -ForegroundColor Cyan
        Write-Host "  Start:   Start-Service -Name $ServiceName"
        Write-Host "  Stop:    Stop-Service -Name $ServiceName"
        Write-Host "  Status:  Get-Service -Name $ServiceName"
        Write-Host "  Logs:    Check Windows Event Viewer under Applications and Services Logs"
        
    } else {
        Write-Error "Failed to create service. Exit code: $LASTEXITCODE"
        exit 1
    }
}
