using System.Net.NetworkInformation;
using System.Net;
using DeviceAgent.Models;
using System.Net.Sockets;
#if WINDOWS
using System.Management;
using Microsoft.Win32;
#endif
#if LINUX
using System.Text.RegularExpressions;
using System.IO;
#endif

namespace DeviceAgent.Services;

public interface IDeviceInfoService
{
    Task<DeviceInfo> GetCurrentDeviceInfoAsync();
}

public class DeviceInfoService : IDeviceInfoService
{
    private readonly ILogger<DeviceInfoService> _logger;

    public DeviceInfoService(ILogger<DeviceInfoService> logger)
    {
        _logger = logger;
    }

    public async Task<DeviceInfo> GetCurrentDeviceInfoAsync()
    {
        try
        {
            var deviceInfo = new DeviceInfo
            {
                hostname = Environment.MachineName,
                updated_at = DateTime.UtcNow,
                last_discovered = DateTime.UtcNow,
                discovery_method = "Agent",
                device_status = "Active",
                device_type = "Computer"
            };

            // Get domain information
            deviceInfo.domain_name = Environment.UserDomainName;
            deviceInfo.is_domain_joined = !string.Equals(deviceInfo.hostname, deviceInfo.domain_name, StringComparison.OrdinalIgnoreCase);

#if WINDOWS
            // Get detailed system information using WMI (Windows)
            await PopulateWindowsSystemInfoAsync(deviceInfo);
            await PopulateWindowsProcessorInfoAsync(deviceInfo);
            await PopulateWindowsMemoryInfoAsync(deviceInfo);
            await PopulateWindowsStorageInfoAsync(deviceInfo);
            await PopulateWindowsOperatingSystemInfoAsync(deviceInfo);
            await PopulateWindowsBiosInfoAsync(deviceInfo);
#elif LINUX
            // Get detailed system information using Linux commands
            await PopulateLinuxSystemInfoAsync(deviceInfo);
            await PopulateLinuxProcessorInfoAsync(deviceInfo);
            await PopulateLinuxMemoryInfoAsync(deviceInfo);
            await PopulateLinuxStorageInfoAsync(deviceInfo);
            await PopulateLinuxOperatingSystemInfoAsync(deviceInfo);
            await PopulateLinuxBiosInfoAsync(deviceInfo);
#endif
            
            // Network information is cross-platform
            await PopulateNetworkInfoAsync(deviceInfo);

            return deviceInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting device information");
            throw;
        }
    }

#if WINDOWS
    // Windows-specific implementations using WMI
    private Task PopulateWindowsSystemInfoAsync(DeviceInfo deviceInfo)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                deviceInfo.manufacturer = obj["Manufacturer"]?.ToString();
                deviceInfo.model = obj["Model"]?.ToString();
                deviceInfo.total_ram_gb = Convert.ToInt32((ulong)(obj["TotalPhysicalMemory"] ?? 0) / (1024 * 1024 * 1024));
                
                // Determine if domain joined
                var domainRole = Convert.ToInt32(obj["DomainRole"] ?? 0);
                deviceInfo.is_domain_joined = domainRole == 1 || domainRole == 3 || domainRole == 4 || domainRole == 5;
                
                if (deviceInfo.is_domain_joined == true)
                {
                    deviceInfo.domain_name = obj["Domain"]?.ToString();
                }
                break;
            }
            
            // Get Asset Tag from registry
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"{INSERT REGISTRY PATH HERE}");
                if (key != null)
                {
                    deviceInfo.asset_tag = key.GetValue("AssetTag")?.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read asset tag from registry");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Windows system info");
        }
        return Task.CompletedTask;
    }
    
    private Task PopulateWindowsProcessorInfoAsync(DeviceInfo deviceInfo)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                deviceInfo.cpu_info = $"{obj["Name"]} ({obj["NumberOfCores"]} cores, {obj["NumberOfLogicalProcessors"]} threads)";
                break; // Just get the first processor
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Windows processor info");
        }
        return Task.CompletedTask;
    }
    
    private Task PopulateWindowsMemoryInfoAsync(DeviceInfo deviceInfo)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            var memoryModules = searcher.Get().Cast<ManagementObject>().ToList();
            
            if (memoryModules.Any())
            {
                var firstModule = memoryModules.First();
                deviceInfo.ram_type = GetMemoryType(Convert.ToInt32(firstModule["MemoryType"] ?? 0));
                deviceInfo.ram_speed = firstModule["Speed"]?.ToString() + " MHz";
                deviceInfo.ram_manufacturer = firstModule["Manufacturer"]?.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Windows memory info");
        }
        return Task.CompletedTask;
    }
    
    private Task PopulateWindowsStorageInfoAsync(DeviceInfo deviceInfo)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE MediaType='Fixed hard disk media'");
            var drives = searcher.Get().Cast<ManagementObject>().ToList();
            
            for (int i = 0; i < Math.Min(drives.Count, 4); i++)
            {
                var drive = drives[i];
                var model = drive["Model"]?.ToString();
                var size = Convert.ToUInt64(drive["Size"] ?? 0);
                var sizeGB = size / (1024 * 1024 * 1024);
                var interfaceType = drive["InterfaceType"]?.ToString();
                
                if (i == 0)
                {
                    deviceInfo.storage_info = $"{sizeGB}GB {model}";
                    deviceInfo.storage_type = interfaceType;
                    deviceInfo.storage_model = model;
                }
                else
                {
                    switch (i)
                    {
                        case 1:
                            deviceInfo.drive2_name = $"Drive {i + 1}";
                            deviceInfo.drive2_capacity = $"{sizeGB}GB";
                            deviceInfo.drive2_type = interfaceType;
                            deviceInfo.drive2_model = model;
                            break;
                        case 2:
                            deviceInfo.drive3_name = $"Drive {i + 1}";
                            deviceInfo.drive3_capacity = $"{sizeGB}GB";
                            deviceInfo.drive3_type = interfaceType;
                            deviceInfo.drive3_model = model;
                            break;
                        case 3:
                            deviceInfo.drive4_name = $"Drive {i + 1}";
                            deviceInfo.drive4_capacity = $"{sizeGB}GB";
                            deviceInfo.drive4_type = interfaceType;
                            deviceInfo.drive4_model = model;
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Windows storage info");
        }
        return Task.CompletedTask;
    }
    
    private Task PopulateWindowsOperatingSystemInfoAsync(DeviceInfo deviceInfo)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                deviceInfo.os_name = obj["Caption"]?.ToString();
                deviceInfo.os_version = obj["Version"]?.ToString();
                deviceInfo.os_architecture = obj["OSArchitecture"]?.ToString();
                
                var installDateStr = obj["InstallDate"]?.ToString();
                if (!string.IsNullOrEmpty(installDateStr) && installDateStr.Length >= 8)
                {
                    if (DateTime.TryParseExact(installDateStr.Substring(0, 8), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime installDate))
                    {
                        deviceInfo.os_install_date = installDate;
                    }
                }
                break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Windows OS info");
        }
        return Task.CompletedTask;
    }
    
    private Task PopulateWindowsBiosInfoAsync(DeviceInfo deviceInfo)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
            foreach (ManagementObject obj in searcher.Get())
            {
                deviceInfo.bios_version = obj["SMBIOSBIOSVersion"]?.ToString();
                deviceInfo.serial_number = obj["SerialNumber"]?.ToString();
                break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Windows BIOS info");
        }
        return Task.CompletedTask;
    }
#endif

#if LINUX
    // Linux-specific implementations using /proc and other system files
    private async Task PopulateLinuxSystemInfoAsync(DeviceInfo deviceInfo)
    {
        try
        {
            // Get manufacturer and model from DMI
            deviceInfo.Manufacturer = await ReadFileContentAsync("/sys/class/dmi/id/sys_vendor");
            deviceInfo.Model = await ReadFileContentAsync("/sys/class/dmi/id/product_name");
            
            // Get total memory from /proc/meminfo
            var meminfo = await File.ReadAllTextAsync("/proc/meminfo");
            var memTotalMatch = Regex.Match(meminfo, @"MemTotal:\s+(\d+)\s+kB");
            if (memTotalMatch.Success)
            {
                var memKB = long.Parse(memTotalMatch.Groups[1].Value);
                deviceInfo.TotalRamGb = Math.Round((decimal)memKB / (1024 * 1024), 2);
            }
            
            // Domain/workgroup info is less relevant on Linux, but we can check if it's joined to AD
            deviceInfo.IsDomainJoined = File.Exists("/etc/krb5.conf") && File.Exists("/etc/samba/smb.conf");
            if (deviceInfo.IsDomainJoined == true)
            {
                deviceInfo.DomainName = await GetLinuxDomainAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Linux system info");
        }
    }
    
    private async Task PopulateLinuxProcessorInfoAsync(DeviceInfo deviceInfo)
    {
        try
        {
            var cpuinfo = await File.ReadAllTextAsync("/proc/cpuinfo");
            
            var modelMatch = Regex.Match(cpuinfo, @"model name\s*:\s*(.+)");
            var coreCountMatch = Regex.Matches(cpuinfo, @"processor\s*:");
            var physicalIdMatches = Regex.Matches(cpuinfo, @"physical id\s*:\s*(\d+)");
            
            if (modelMatch.Success)
            {
                var cpuModel = modelMatch.Groups[1].Value.Trim();
                var logicalCores = coreCountMatch.Count;
                var physicalCores = physicalIdMatches.Cast<Match>().Select(m => m.Groups[1].Value).Distinct().Count();
                
                deviceInfo.CpuInfo = $"{cpuModel} ({physicalCores} cores, {logicalCores} threads)";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Linux processor info");
        }
    }
    
    private async Task PopulateLinuxMemoryInfoAsync(DeviceInfo deviceInfo)
    {
        try
        {
            // Try to get memory type from dmidecode (requires root)
            try
            {
                var result = await ExecuteCommandAsync("dmidecode -t memory | grep -E 'Type:|Speed:|Manufacturer:'");
                var lines = result.Split('\n');
                
                var typeMatch = lines.FirstOrDefault(l => l.Contains("Type:") && !l.Contains("Error Correction Type") && !l.Contains("Form Factor"));
                var speedMatch = lines.FirstOrDefault(l => l.Contains("Speed:") && !l.Contains("Configured"));
                var manufacturerMatch = lines.FirstOrDefault(l => l.Contains("Manufacturer:"));
                
                if (typeMatch != null) deviceInfo.RamType = typeMatch.Split(':')[1].Trim();
                if (speedMatch != null) deviceInfo.RamSpeed = speedMatch.Split(':')[1].Trim();
                if (manufacturerMatch != null) deviceInfo.RamManufacturer = manufacturerMatch.Split(':')[1].Trim();
            }
            catch
            {
                // dmidecode might not be available or require root
                deviceInfo.RamType = "Unknown";
                _logger.LogInformation("dmidecode not available for detailed memory info");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Linux memory info");
        }
    }
    
    private async Task PopulateLinuxStorageInfoAsync(DeviceInfo deviceInfo)
    {
        try
        {
            // Get storage info from /proc/partitions and lsblk
            var result = await ExecuteCommandAsync("lsblk -d -n -o NAME,SIZE,TYPE,MODEL | grep disk");
            var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < Math.Min(lines.Length, 4); i++)
            {
                var parts = lines[i].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    var deviceName = parts[0];
                    var size = parts[1];
                    var model = parts.Length > 3 ? string.Join(" ", parts.Skip(3)) : "Unknown";
                    
                    if (i == 0)
                    {
                        deviceInfo.StorageInfo = $"{size} {model}";
                        deviceInfo.StorageType = "SATA"; // Default assumption
                        deviceInfo.StorageModel = model;
                    }
                    else
                    {
                        switch (i)
                        {
                            case 1:
                                deviceInfo.Drive2Name = deviceName;
                                deviceInfo.Drive2Capacity = size;
                                deviceInfo.Drive2Type = "SATA";
                                deviceInfo.Drive2Model = model;
                                break;
                            case 2:
                                deviceInfo.Drive3Name = deviceName;
                                deviceInfo.Drive3Capacity = size;
                                deviceInfo.Drive3Type = "SATA";
                                deviceInfo.Drive3Model = model;
                                break;
                            case 3:
                                deviceInfo.Drive4Name = deviceName;
                                deviceInfo.Drive4Capacity = size;
                                deviceInfo.Drive4Type = "SATA";
                                deviceInfo.Drive4Model = model;
                                break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Linux storage info");
        }
    }
    
    private async Task PopulateLinuxOperatingSystemInfoAsync(DeviceInfo deviceInfo)
    {
        try
        {
            // Get OS info from /etc/os-release
            if (File.Exists("/etc/os-release"))
            {
                var osRelease = await File.ReadAllTextAsync("/etc/os-release");
                var prettyNameMatch = Regex.Match(osRelease, @"PRETTY_NAME=""([^""]+)""");
                var versionMatch = Regex.Match(osRelease, @"VERSION=""([^""]+)""");
                
                if (prettyNameMatch.Success)
                    deviceInfo.OsName = prettyNameMatch.Groups[1].Value;
                if (versionMatch.Success)
                    deviceInfo.OSVersion = versionMatch.Groups[1].Value;
            }
            
            // Get kernel version
            var unameResult = await ExecuteCommandAsync("uname -r");
            if (!string.IsNullOrEmpty(unameResult))
            {
                deviceInfo.OSVersion = $"{deviceInfo.OSVersion} (Kernel: {unameResult.Trim()})";
            }
            
            // Get architecture
            var archResult = await ExecuteCommandAsync("uname -m");
            if (!string.IsNullOrEmpty(archResult))
            {
                deviceInfo.OsArchitecture = archResult.Trim();
            }
            
            // Try to get install date from filesystem creation time
            try
            {
                var statResult = await ExecuteCommandAsync("stat -c %W /");
                if (!string.IsNullOrEmpty(statResult) && long.TryParse(statResult.Trim(), out long timestamp))
                {
                    deviceInfo.OsInstallDate = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                }
            }
            catch { /* Install date not critical */ }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Linux OS info");
        }
    }
    
    private async Task PopulateLinuxBiosInfoAsync(DeviceInfo deviceInfo)
    {
        try
        {
            deviceInfo.BiosVersion = await ReadFileContentAsync("/sys/class/dmi/id/bios_version");
            deviceInfo.SerialNumber = await ReadFileContentAsync("/sys/class/dmi/id/product_serial");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Linux BIOS info");
        }
    }
    
    private async Task<string> ReadFileContentAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                return (await File.ReadAllTextAsync(filePath)).Trim();
            }
        }
        catch { }
        return string.Empty;
    }
    
    private async Task<string> ExecuteCommandAsync(string command)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var result = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            return result;
        }
        catch
        {
            return string.Empty;
        }
    }
    
    private async Task<string> GetLinuxDomainAsync()
    {
        try
        {
            // Try to get domain from various sources
            var hostname = await ExecuteCommandAsync("hostname -d");
            if (!string.IsNullOrEmpty(hostname?.Trim()))
                return hostname.Trim();
                
            var dnsdomainname = await ExecuteCommandAsync("dnsdomainname");
            if (!string.IsNullOrEmpty(dnsdomainname?.Trim()))
                return dnsdomainname.Trim();
                
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
#endif

    // Cross-platform network information collection
    private Task PopulateNetworkInfoAsync(DeviceInfo deviceInfo)
    {
        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up && 
                            ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .ToList();
            
            for (int i = 0; i < Math.Min(networkInterfaces.Count, 4); i++)
            {
                var ni = networkInterfaces[i];
                var ipProps = ni.GetIPProperties();
                var ipv4 = ipProps.UnicastAddresses.FirstOrDefault(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork);
                
                if (ipv4 != null)
                {
                    var ip = ipv4.Address.ToString();
                    var mac = ni.GetPhysicalAddress().ToString();
                    var subnet = $"{ipv4.Address}/{ipv4.PrefixLength}";
                    
                    if (i == 0)
                    {
                        deviceInfo.primary_ip = ip;
                        deviceInfo.primary_mac = mac;
                        deviceInfo.primary_subnet = subnet;
                        
                        var dnsServers = ipProps.DnsAddresses.Where(dns => dns.AddressFamily == AddressFamily.InterNetwork).ToList();
                        if (dnsServers.Count > 0) deviceInfo.primary_dns = dnsServers[0].ToString();
                        if (dnsServers.Count > 1) deviceInfo.secondary_dns = dnsServers[1].ToString();
                    }
                    else
                    {
                        switch (i)
                        {
                            case 1:
                                deviceInfo.nic2_name = ni.Name;
                                deviceInfo.nic2_ip = ip;
                                deviceInfo.nic2_mac = mac;
                                deviceInfo.nic2_subnet = subnet;
                                break;
                            case 2:
                                deviceInfo.nic3_name = ni.Name;
                                deviceInfo.nic3_ip = ip;
                                deviceInfo.nic3_mac = mac;
                                deviceInfo.nic3_subnet = subnet;
                                break;
                            case 3:
                                deviceInfo.nic4_name = ni.Name;
                                deviceInfo.nic4_ip = ip;
                                deviceInfo.nic4_mac = mac;
                                deviceInfo.nic4_subnet = subnet;
                                break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting network info");
        }
        return Task.CompletedTask;
    }

#if WINDOWS
    private static string GetMemoryType(int memoryType)
    {
        return memoryType switch
        {
            20 => "DDR",
            21 => "DDR2",
            24 => "DDR3",
            26 => "DDR4",
            34 => "DDR5",
            _ => "Unknown"
        };
    }
#endif
}
