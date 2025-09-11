using System.ComponentModel.DataAnnotations;

namespace DeviceAgent.Models;

public class DeviceInfo
{
    public int device_id { get; set; } // Unique identifier for the device
    
    // Device Identity - EXACT MATCH to database
    public string hostname { get; set; } = Environment.MachineName;
    public string? serial_number { get; set; }
    public string? asset_tag { get; set; }
    public string? device_type { get; set; }
    public string? equipment_group { get; set; }
    
    // Domain/Workgroup Information
    public string? domain_name { get; set; }
    public bool? is_domain_joined { get; set; }
    
    // Hardware Information
    public string? manufacturer { get; set; }
    public string? model { get; set; }
    public string? cpu_info { get; set; }
    public string? bios_version { get; set; }
    
    // Memory Information - note: total_ram_gb is INT in database
    public int? total_ram_gb { get; set; }
    public string? ram_type { get; set; }
    public string? ram_speed { get; set; }
    public string? ram_manufacturer { get; set; }
    
    // Operating System Information
    public string? os_name { get; set; }
    public string? os_version { get; set; }
    public string? os_architecture { get; set; }
    public DateTime? os_install_date { get; set; }
    
    // Primary Storage Information
    public string? storage_info { get; set; }
    public string? storage_type { get; set; }
    public string? storage_model { get; set; }
    
    // Additional Storage Drives (3 sets)
    public string? drive2_name { get; set; }
    public string? drive2_capacity { get; set; }
    public string? drive2_type { get; set; }
    public string? drive2_model { get; set; }
    
    public string? drive3_name { get; set; }
    public string? drive3_capacity { get; set; }
    public string? drive3_type { get; set; }
    public string? drive3_model { get; set; }
    
    public string? drive4_name { get; set; }
    public string? drive4_capacity { get; set; }
    public string? drive4_type { get; set; }
    public string? drive4_model { get; set; }
    
    // Primary Network Interface
    public string? primary_ip { get; set; }
    public string? primary_mac { get; set; }
    public string? primary_subnet { get; set; }
    public string? primary_dns { get; set; }
    public string? secondary_dns { get; set; }
    
    // Network Interface 2
    public string? nic2_name { get; set; }
    public string? nic2_ip { get; set; }
    public string? nic2_mac { get; set; }
    public string? nic2_subnet { get; set; }
    
    // Network Interface 3
    public string? nic3_name { get; set; }
    public string? nic3_ip { get; set; }
    public string? nic3_mac { get; set; }
    public string? nic3_subnet { get; set; }
    
    // Network Interface 4
    public string? nic4_name { get; set; }
    public string? nic4_ip { get; set; }
    public string? nic4_mac { get; set; }
    public string? nic4_subnet { get; set; }
    
    // Web Interface
    public string? web_interface_url { get; set; }
    
    // Device Status and Location
    public string? device_status { get; set; } = "Active";
    public string? area { get; set; }
    public string? zone { get; set; }
    public string? line { get; set; }
    public string? pitch { get; set; }
    public string? floor { get; set; }
    public string? pillar { get; set; }
    
    // Additional Information
    public string? additional_notes { get; set; }
    
    // Asset Management Dates
    public DateTime? purchase_date { get; set; }
    public DateTime? service_date { get; set; }
    public DateTime? warranty_date { get; set; }
    
    // System Fields
    public DateTime? created_at { get; set; }
    public DateTime? updated_at { get; set; }
    public DateTime? last_discovered { get; set; }
    public string? discovery_method { get; set; }
}
