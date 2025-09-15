# DeviceAgent: Comprehensive System Inventory & Monitoring

## Table of Contents
- [🔍 Overview](#-overview)
- [✨ Key Features](#-key-features)
- [🏗️ Architecture](#️-architecture)
- [💾 Device Information Collection](#-device-information-collection)
- [🌐 Network Detection](#-network-detection)
- [⏰ Smart Scheduling](#-smart-scheduling)
- [🛠️ Installation & Setup](#️-installation--setup)
- [⚙️ Configuration](#️-configuration)
- [🗄️ Database Schema](#️-database-schema)
- [🔧 Service Management](#-service-management)
- [🐛 Troubleshooting](#-troubleshooting)
- [🔒 Security](#-security)
- [📋 Recent Enhancements](#-recent-enhancements)

---

## 🔍 Overview

DeviceAgent is an enterprise-grade system inventory service that automatically discovers, collects, and maintains up-to-date information about devices in your environment. It runs as a background service on Windows and Linux systems, periodically gathering comprehensive hardware, software, and network configuration data.

### Primary Use Cases
- **Asset Management**: Automated hardware and software inventory
- **Security Compliance**: Track system configurations and changes
- **Network Management**: Monitor network interface configurations and IP assignments
- **Change Detection**: Identify when device configurations have been modified
- **Reporting & Analytics**: Provide data for business intelligence and reporting systems

### Platform Support
- **Windows**: Full support with GUI management interface
- **Linux**: Headless operation with comprehensive system detection
- **Cross-Platform**: .NET 8.0 multi-targeting for maximum compatibility

---

## ✨ Key Features

### Comprehensive Data Collection
- **Hardware Information**: CPU, memory, storage, BIOS, serial numbers
- **Operating System**: Version, architecture, installation details, domain status
- **Network Configuration**: Up to 4 network interfaces with IP, MAC, subnet, DNS
- **Storage Devices**: Multiple drives with capacity, type, and model information
- **System Identity**: Hostname, domain membership, asset tags

### Intelligent Synchronization
- **Smart Scheduling**: Configurable check-in intervals (default: 7 days)
- **Change Detection**: Only updates database when device information actually changes
- **Local State Tracking**: Minimizes database queries and network traffic
- **Force Update**: Manual sync capability for immediate updates
- **Connection Testing**: Validates database connectivity before operations

### Enterprise Ready
- **Windows Service Integration**: Full service lifecycle management
- **Service Account Support**: Configurable authentication for database access
- **Comprehensive Logging**: Detailed operation logs for troubleshooting
- **Error Recovery**: Automatic retry logic and graceful failure handling
- **Remote Management**: PowerShell scripts for network-wide administration

---

## 🏗️ Architecture

### Core Services
- **DeviceInfoService**: Cross-platform hardware and software detection
- **DatabaseService**: SQL Server connectivity and operations
- **DeviceSyncService**: Orchestrates comparison and synchronization logic
- **Worker**: Background service for scheduling and coordination
- **GUI**: Windows Forms interface for configuration and monitoring

### Data Sources
- **Windows**: Uses WMI (Windows Management Instrumentation)
- **Linux**: Uses `/proc`, `/sys`, and system commands
- **Network**: Cross-platform .NET NetworkInterface APIs

### Data Flow
```
Device Hardware/Software → DeviceInfoService → DeviceSyncService → DatabaseService → SQL Server
                                    ↓
Local State File ← Scheduling Logic ← Configuration Service ← appsettings.json
```

---

- **Smart Scheduling**: Configurable check-in intervals (default: 7 days)

- **Change Detection**: Only updates database when device information actually changes

- **Local State Tracking**: Minimizes database queries and network traffic

- **Force Update**: Manual sync capability for immediate updates

- **Connection Testing**: Validates database connectivity before operations



## 💾 Device Information Collection

### Hardware Information
| Category | Windows Source | Linux Source | Fields Collected |
|----------|---------------|--------------|------------------|
| **CPU** | WMI | `/proc/cpuinfo` | Model, cores, speed, architecture |
| **Memory** | WMI | `/proc/meminfo`, `dmidecode` | Total RAM, type, speed, modules |
| **Storage** | WMI | `lsblk`, `/proc/mounts` | Drives (4), capacity, type, model |
| **BIOS** | WMI | `/sys/class/dmi/id/` | Version, manufacturer, serial |
| **System** | WMI | DMI, `/proc/version` | Manufacturer, model, serial numbers |

### Operating System Details
- **Name & Version**: Full OS identification
- **Architecture**: x86, x64, ARM detection
- **Installation Date**: OS deployment timestamp
- **Domain Status**: Workgroup vs domain membership
- **Service Pack/Updates**: Current patch level information

### Storage Information
- **Multiple Drives**: Up to 4 storage devices tracked
- **Capacity Details**: Total and available space
- **Drive Types**: SSD, HDD, removable media detection
- **Interface Information**: SATA, NVMe, USB connection types

---

## 🌐 Network Detection

### Primary NIC Detection Algorithm
DeviceAgent uses an intelligent algorithm to identify the primary network interface:

1. **Priority 1**: Interfaces with default gateways + lowest metric
2. **Priority 2**: Active Ethernet connections (physical interfaces)
3. **Priority 3**: Any active interface with valid IPv4 address

### Network Data Collected
```json
{
  "primary_nic_name": "Ethernet",
  "primary_ip": "192.168.1.100",
  "primary_mac": "00:11:22:33:44:55",
  "primary_subnet": "255.255.255.0",
  "primary_dns": "192.168.1.1",
  "secondary_dns": "8.8.8.8",
  "nic2_name": "Wi-Fi",
  "nic2_ip": "10.0.0.50",
  "nic2_mac": "AA:BB:CC:DD:EE:FF",
  "nic2_subnet": "255.255.255.0"
}
```

### Network Features
- **Multiple NICs**: Support for up to 4 network interfaces
- **IP Configuration**: IPv4 addresses, subnet masks, DNS servers
- **Physical Details**: MAC addresses (standardized format), interface names
- **Network Type**: Ethernet vs wireless classification
- **Traditional Notation**: Uses dotted decimal (255.255.255.0) instead of CIDR (/24)
- **MAC Address Format**: Standardized colon-separated format (XX:XX:XX:XX:XX:XX)
- **DNS Configuration**: Both primary and secondary DNS servers captured

---

## ⏰ Smart Scheduling

### Scheduling Logic
DeviceAgent implements an intelligent check-in system to minimize database load:

#### First Run Behavior
- Immediately performs device sync and database update
- Creates local state file with timestamp
- Establishes baseline device information

#### Subsequent Runs
- Checks local state file for last sync time
- Compares against configured interval (default: 7 days)
- Exits early if sync not due (saves resources)
- Performs full sync when interval has passed

#### Continuous Monitoring
- Wakes up every hour to check if sync is due
- Monitors for force sync trigger files
- Handles configuration changes without restart
- Maintains persistent state across service restarts

### Sync Process Details
1. **Database Connection Test**: Validates connectivity before operations
2. **Device Information Collection**: Gathers current system state
3. **Change Detection**: Compares with existing database record
4. **Smart Updates**: 
   - **Device Changed**: Updates both `updated_at` and `last_discovered`
   - **No Changes**: Updates only `last_discovered`
   - **New Device**: Inserts complete record
5. **State Update**: Records sync timestamp locally

### Force Update & Debugging Features
- **ForceSyncAsync**: Bypasses interval checks for immediate sync
- **GUI**: Force Update button now always triggers sync
- **Database Connection Test**: Validates DB before sync
- **Logging**: Detailed logs for all DB and sync operations
- **Error Handling**: Clear messages for connection, permission, and config issues
- **Timestamp & Timezone**: User-configurable timezone; all DB times stored as UTC

---

## 🛠️ Installation & Setup

### Prerequisites
- **.NET 8.0 Runtime**: Framework or self-contained deployment
- **SQL Server Access**: Local or network database connectivity
- **Administrative Rights**: Required for hardware information collection
- **Network Connectivity**: For remote database synchronization

### Quick Installation (Windows)

#### Using Install Script
```powershell
# Basic installation (prompts for service account)
.\install-service.ps1

# With service account configuration
.\install-service.ps1 -ServiceAccount "DOMAIN\DeviceAgentSvc"

# With custom executable path
.\install-service.ps1 -ExecutablePath "C:\DeviceAgent\DeviceAgent.exe"
```

#### Manual Installation
```powershell
# Build the application
dotnet publish --framework net8.0-windows --configuration Release --self-contained true --runtime win-x64

# Create Windows service
sc create DeviceAgent binPath="C:\DeviceAgent\DeviceAgent.exe" start=auto
sc description DeviceAgent "Automated device inventory collection service"

# Start the service
sc start DeviceAgent
```

### Linux Installation

#### Build and Deploy
```bash
# Build for Linux
dotnet publish --framework net8.0 --configuration Release --runtime linux-x64 --self-contained true

# Create dedicated service account
sudo useradd -r -s /bin/false -d /opt/deviceagent deviceagent

# Create deployment directory
sudo mkdir -p /opt/deviceagent
sudo cp -r bin/Release/net8.0/linux-x64/publish/* /opt/deviceagent/

# Set ownership and permissions
sudo chown -R deviceagent:deviceagent /opt/deviceagent
sudo chmod +x /opt/deviceagent/DeviceAgent

# Grant sudo access for hardware detection commands (secure alternative to running as root)
sudo tee /etc/sudoers.d/deviceagent > /dev/null <<EOF
# Allow deviceagent user to run specific hardware detection commands
deviceagent ALL=(ALL) NOPASSWD: /usr/bin/dmidecode, /usr/bin/lshw, /bin/lsblk
EOF
```

#### Create systemd Service
```bash
# Create service file
sudo tee /etc/systemd/system/deviceagent.service > /dev/null <<EOF
[Unit]
Description=Device Agent Service
After=network.target

[Service]
Type=notify
ExecStart=/opt/deviceagent/DeviceAgent
Restart=always
RestartSec=30
User=deviceagent
Group=deviceagent
WorkingDirectory=/opt/deviceagent

# Security hardening
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/opt/deviceagent

[Install]
WantedBy=multi-user.target
EOF

# Enable and start service
sudo systemctl enable deviceagent
sudo systemctl start deviceagent
```

---

## ⚙️ Configuration

### Primary Configuration File: `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=it_device_inventory;Integrated Security=true;TrustServerCertificate=true"
  },
  "DeviceAgent": {
    "CheckInInterval": "7.00:00:00",
    "TimeZoneId": "Eastern Standard Time"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "DeviceAgent": "Debug",
      "DeviceAgent.Services": "Debug"
    }
  }
}
```

### Configuration Options

#### Database Connection
- **Integrated Security**: Windows Authentication (recommended)
- **SQL Authentication**: Username/password in connection string
- **Connection Timeout**: Configurable timeout settings
- **SSL/TLS**: Certificate validation options

#### Sync Settings
- **Check-in Interval**: How often to perform full sync (default: 7 days)
- **Check Frequency**: How often to wake up and check if sync is due (default: 1 hour)
- **Timezone**: Configurable timezone for timestamp calculations

#### Logging Configuration
- **Log Levels**: Debug, Information, Warning, Error
- **Categories**: Separate levels for different components
- **Output**: File, Event Log, Console options

### State File Locations
- **Windows**: `%LOCALAPPDATA%\DeviceAgent\agent-state.json`
- **Linux**: `~/.local/share/DeviceAgent/agent-state.json`

---

## 🗄️ Database Schema & Management

### Required Database Table: `devices`

Use the included `create_device_inventory_enhanced.sql` script to create the complete schema with proper indexes and constraints.

#### Key Fields
- **Device Identity**: hostname, manufacturer, model, serial_number
- **Operating System**: os_name, os_version, os_architecture, domain_name
- **Hardware**: cpu_model, cpu_cores, total_memory_gb, bios_version
- **Storage**: drive1-4 with capacity, type, and model information
- **Network**: primary_nic_name, primary_ip, primary_mac, primary_subnet, primary_dns, secondary_dns
- **Additional NICs**: nic2-4 with IP, MAC, and subnet information
- **Timestamps**: created_at, updated_at, last_discovered (all UTC)

#### Database Features
- **UTC Timestamps**: All dates stored in UTC for consistency
- **Flexible Storage**: Support for multiple drives and network interfaces
- **Change Tracking**: Separate timestamps for creation, updates, and discovery
- **Indexing**: Optimized for hostname lookups and reporting queries

---

## 🔧 Service Management

### Windows Service Management
```powershell
# Service status and control
Get-Service DeviceAgent
Start-Service DeviceAgent
Stop-Service DeviceAgent
Restart-Service DeviceAgent

# Interactive management
.\service-manager.bat
.\service-config.bat

# Remote management
.\remote-manage.ps1 -ComputerName "PC001" -Action Status
```

### Linux Service Management
```bash
# Service status and control
sudo systemctl status deviceagent
sudo systemctl start deviceagent
sudo systemctl stop deviceagent
sudo systemctl restart deviceagent

# View logs
sudo journalctl -u deviceagent -f
```

### Force Sync Options
1. **Trigger File**: Create `force_sync.trigger` file in service directory
2. **GUI Interface**: Click "Force Update" button (Windows)
3. **Remote PowerShell**: `.\remote-manage.ps1 -ComputerName "PC001" -Action ForceSync`

---

## 🐛 Troubleshooting

### Common Issues
- **Service Won't Start**: Check Event Viewer, verify database connection, ensure service account has proper permissions
- **Database Connection Failures**: Test connection string, verify network connectivity, check service account SQL permissions
- **No Data Collection**: Check sync interval, use force sync, verify WMI service (Windows), review permission errors

### Logging & Diagnostics
- **Windows Event Log**: Applications and Services Logs → DeviceAgent
- **Debug Logging**: Enable in appsettings.json for detailed troubleshooting
- **SQL Server Profiler**: Monitor database activity
- **Process Monitor**: Track file system access issues

---

## 🔒 Security

### Service Account Best Practices

#### Windows
- **Principle of Least Privilege**: Grant only necessary permissions
- **Local Administrator**: Required for hardware information collection
- **SQL Server Access**: Database read/write permissions only
- **Strong Passwords**: Use complex, regularly rotated passwords

#### Linux
- **Dedicated Service Account**: Use `deviceagent` user instead of root
- **Sudo Access**: Grant specific commands via `/etc/sudoers.d/deviceagent`
- **File Permissions**: Restrict access to service directory only
- **Security Hardening**: Use systemd security features (NoNewPrivileges, PrivateTmp, etc.)
- **Minimal Privileges**: Only grant access to hardware detection commands

### Database Security
- **Connection Encryption**: Use TLS/SSL for database connections
- **Windows Authentication**: Preferred over SQL Authentication (Windows)
- **SQL Authentication**: Use strong credentials for Linux connections
- **Network Security**: Restrict database access to authorized systems
- **Backup Encryption**: Secure database backups containing device information

---

## 📋 Recent Enhancements

### Force Update Fix (September 2025)
- **Problem**: Force Update button didn't work after first use
- **Solution**: Created separate `ForceSyncAsync` method bypassing interval checks
- **Benefits**: Reliable manual sync capability, enhanced debugging

### Timestamp & Timezone Enhancement
- **Problem**: `updated_at` field changed on every check-in, timezone hardcoded
- **Solution**: Fixed update logic, added configurable timezone support
- **Benefits**: Accurate change tracking, proper local time handling

### Network Detection Enhancement
- **Problem**: Basic network interface detection, inconsistent MAC formatting
- **Solution**: Smart primary NIC detection, standardized formats
- **Benefits**: Accurate primary interface identification, comprehensive DNS collection

### Security Improvements
- **Problem**: Plain text password parameters in install scripts
- **Solution**: SecureString implementation, secure credential handling
- **Benefits**: Improved password security, compliance with best practices

---

## License
This project is licensed under the GPL 2.0 License.

## Version
Current Version: 2.0.0 (September 2025)
- Enhanced network detection
- Force update fixes
- Timezone support

- Security improvements

