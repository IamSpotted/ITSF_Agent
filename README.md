# Device Agent

A cross-platform .NET background service that automatically collects device information and synchronizes it with a SQL Server database. The agent uses smart scheduling with local state tracking to minimize unnecessary database queries.

## Smart Scheduling System

The agent implements an intelligent check-in system:

- **First Run**: Immediately performs device sync and database update
- **Subsequent Runs**: Tracks last check-in time locally using a JSON state file
- **7-Day Interval**: Only performs full sync every 7 days (configurable)
- **Early Exit**: If less than 7 days since last check-in, agent exits without database query
- **Persistent State**: Stores check-in history in `%LOCALAPPDATA%\DeviceAgent\agent-state.json` (Windows) or equivalent on Linux
- **Hourly Checks**: Wakes up every hour to see if it's time for the next check-in

## Cross-Platform Support

The agent supports both **Windows** and **Linux** environments with platform-specific device information collection:

### Windows Features (using WMI)
- Comprehensive hardware detection via Windows Management Instrumentation
- Detailed memory module information (type, speed, manufacturer)  
- Storage drive details (interface type, exact sizes)
- BIOS version and system serial numbers
- Domain/workgroup membership detection

### Linux Features (using /proc, /sys, and system commands)
- Hardware detection via DMI and system files
- Memory information from `/proc/meminfo` and `dmidecode`
- Storage information via `lsblk` command
- OS details from `/etc/os-release` and `uname`
- BIOS and system information from `/sys/class/dmi/id/`

### Shared Features (Cross-Platform)
- Network interface detection and configuration
- IP addresses, MAC addresses, and subnet information
- DNS server configuration
- Multiple network interface support (up to 4 NICs)

## Features

- **Comprehensive Device Information Collection**: Collects detailed system information including:
  - Hardware specs (CPU, RAM, Storage, BIOS)
  - Network interfaces and configuration
  - Operating system details
  - Domain/workgroup membership
  - Serial numbers and asset information

- **Smart Database Synchronization**: 
  - Tracks check-in history locally to minimize database queries
  - Only queries database every 7 days (configurable interval)
  - Compares current device info with database records when sync is due
  - Updates only changed fields when differences are detected
  - Updates only the timestamp when no changes are found
  - Automatically inserts new devices on first discovery
  - Prevents unnecessary network traffic and database load

- **Intelligent Scheduling**:
  - Performs immediate sync on first run (no prior state)
  - Stores last check-in time in local JSON state file
  - Exits early if check-in not due (saves resources)
  - Runs background checks every hour to determine if sync is needed
  - Configurable check-in interval (default: 7 days)
  - Robust error handling with automatic retry logic

## Setup Instructions

### 1. Database Configuration

Update the connection string in `appsettings.json` or `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your_server_name;Database=your_database_name;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### 2. Database Schema

Ensure your SQL Server database has a `devices` table with columns matching the `DeviceInfo` model properties.

## Build Instructions

### Windows
```bash
# Build for Windows (uses WMI for hardware detection)
dotnet build

# Run on Windows
dotnet run
```

### Linux  
```bash
# Build for Linux (uses /proc, /sys, and system commands)
dotnet build

# Run on Linux
dotnet run

# Note: Some hardware detection features may require elevated privileges
sudo dotnet run
```

### Cross-Platform Build
The project automatically detects the target platform and includes the appropriate code:
- `#if WINDOWS` sections use System.Management for WMI calls
- `#if LINUX` sections use file system and command-line tools
- Network detection is cross-platform using .NET NetworkInterface APIs

### 4. Install as Windows Service

To run the agent as a Windows service that starts automatically:

```bash
# Publish the application
dotnet publish -c Release -o C:\DeviceAgent

# Install as Windows service (run as administrator)
sc create DeviceAgent binPath="C:\DeviceAgent\DeviceAgent.exe" start=auto
sc description DeviceAgent "Automated device inventory collection service"
sc start DeviceAgent
```

## Configuration

### Sync Interval
The default check-in interval is 7 days. To change this, modify the `_checkInInterval` in `DeviceSyncService.cs`:

```csharp
private readonly TimeSpan _checkInInterval = TimeSpan.FromDays(7); // Change as needed
```

### Check Frequency  
The agent checks every hour to see if a sync is due. To change this, modify the `_checkInterval` in `Worker.cs`:

```csharp
private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Change as needed
```

### State File Location
The agent stores its check-in state in:
- **Windows**: `%LOCALAPPDATA%\DeviceAgent\agent-state.json`
- **Linux**: `~/.local/share/DeviceAgent/agent-state.json`

To force a fresh sync, delete this file and restart the service.

### Logging
Adjust logging levels in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "DeviceAgent": "Debug"  // For more detailed logging
    }
  }
}
```

## How It Works

1. **Startup Check**: When the service starts, it checks for a local state file
2. **First Run Logic**: 
   - If no state file exists, performs immediate device sync
   - Stores current timestamp as last check-in time
3. **Subsequent Runs**:
   - Reads last check-in time from state file
   - Calculates time since last check-in
   - If less than 7 days, exits without database query
   - If 7+ days, performs full device sync and updates state
4. **Continuous Monitoring**: Wakes up every hour to check if sync is due
5. **Database Operations** (only when sync is due):
   - Collects current device information
   - Queries database for existing device record
   - Compares current vs stored information
   - Updates only changed fields or inserts new device
   - Updates local state file with new check-in time

This approach drastically reduces database load and network traffic while maintaining accurate device inventory.

## Architecture

- **DeviceInfoService**: Collects comprehensive device information using WMI and .NET APIs
- **DatabaseService**: Handles all database operations (select, insert, update)
- **DeviceSyncService**: Orchestrates the comparison and sync logic
- **Worker**: Background service that handles scheduling and coordination

## Dependencies

- .NET 9.0
- Microsoft.Data.SqlClient (for SQL Server connectivity)
- Microsoft.Extensions.Hosting (for background service functionality)

### Platform-Specific Dependencies
- **Windows**: System.Management (for WMI hardware information collection)
- **Linux**: No additional packages required (uses built-in Linux tools)

### Linux System Requirements
For optimal hardware detection on Linux, ensure these tools are available:
- `lsblk` (usually pre-installed)
- `dmidecode` (for detailed memory information - may require sudo)
- Standard `/proc` and `/sys` filesystem access

## Troubleshooting

### Common Issues

1. **WMI Access Denied**: Ensure the service runs with appropriate Windows privileges
2. **Database Connection**: Verify connection string and network connectivity
3. **Missing Columns**: Ensure database schema matches the DeviceInfo model

### Logs
Check Windows Event Logs or application logs for detailed error information when running as a service.

## Security Considerations

- The agent requires local system access to collect hardware information
- Database connection uses Windows Authentication by default
- Consider running the service with a dedicated service account
- Ensure SQL Server access is properly configured for the service account
