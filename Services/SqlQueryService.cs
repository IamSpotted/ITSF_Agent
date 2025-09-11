using DeviceAgent.Models;

namespace DeviceAgent.Services;

public interface ISqlQueryService
{
    string GetDeviceByHostnameQuery();
    string GetUpdateDeviceQuery();
    string GetInsertDeviceQuery();
    string GetUpdateLastDiscoveredQuery();
}

public class SqlQueryService : ISqlQueryService
{
    public string GetDeviceByHostnameQuery()
    {
        return @"
            SELECT * FROM devices 
            WHERE Hostname = @hostname";
    }

    public string GetUpdateDeviceQuery()
    {
        return @"
            UPDATE devices SET 
                device_status = @DeviceStatus,
                device_type = @DeviceType,
                serial_number = @SerialNumber,
                asset_tag = @AssetTag,
                equipment_group = @EquipmentGroup,
                domain_name = @DomainName,
                is_domain_joined = @IsDomainJoined,
                manufacturer = @Manufacturer,
                model = @Model,
                cpu_info = @CpuInfo,
                bios_version = @BiosVersion,
                total_ram_gb = @TotalRamGb,
                ram_type = @RamType,
                ram_speed = @RamSpeed,
                ram_manufacturer = @RamManufacturer,
                os_name = @OsName,
                os_version = @OSVersion,
                os_architecture = @OsArchitecture,
                os_install_date = @OsInstallDate,
                storage_info = @StorageInfo,
                storage_type = @StorageType,
                storage_model = @StorageModel,
                drive2_name = @Drive2Name,
                drive2_capacity = @Drive2Capacity,
                drive2_type = @Drive2Type,
                drive2_model = @Drive2Model,
                drive3_name = @Drive3Name,
                drive3_capacity = @Drive3Capacity,
                drive3_type = @Drive3Type,
                drive3_model = @Drive3Model,
                drive4_name = @Drive4Name,
                drive4_capacity = @Drive4Capacity,
                drive4_type = @Drive4Type,
                drive4_model = @Drive4Model,
                primary_ip = @PrimaryIp,
                primary_mac = @PrimaryMac,
                primary_subnet = @PrimarySubnet,
                primary_dns = @PrimaryDns,
                secondary_dns = @SecondaryDns,
                nic2_name = @Nic2Name,
                nic2_ip = @Nic2Ip,
                nic2_mac = @Nic2Mac,
                nic2_subnet = @Nic2Subnet,
                nic3_name = @Nic3Name,
                nic3_ip = @Nic3Ip,
                nic3_mac = @Nic3Mac,
                nic3_subnet = @Nic3Subnet,
                nic4_name = @Nic4Name,
                nic4_ip = @Nic4Ip,
                nic4_mac = @Nic4Mac,
                nic4_subnet = @Nic4Subnet,
                web_interface_url = @WebInterfaceUrl,
                area = @Area,
                zone = @Zone,
                line = @Line,
                pitch = @Pitch,
                floor = @Floor,
                pillar = @Pillar,
                additional_notes = @AdditionalNotes,
                purchase_date = @PurchaseDate,
                service_date = @ServiceDate,
                warranty_date = @WarrantyDate,
                updated_at = @UpdatedAt,
                last_discovered = @LastDiscovered,
                discovery_method = @DiscoveryMethod
            WHERE hostname = @Hostname";
    }

    public string GetInsertDeviceQuery()
    {
        return @"
            INSERT INTO devices (
                hostname, device_status, device_type, serial_number, asset_tag, equipment_group,
                domain_name, is_domain_joined,
                manufacturer, model, cpu_info, bios_version,
                total_ram_gb, ram_type, ram_speed, ram_manufacturer,
                os_name, os_version, os_architecture, os_install_date,
                storage_info, storage_type, storage_model,
                drive2_name, drive2_capacity, drive2_type, drive2_model,
                drive3_name, drive3_capacity, drive3_type, drive3_model,
                drive4_name, drive4_capacity, drive4_type, drive4_model,
                primary_ip, primary_mac, primary_subnet, primary_dns, secondary_dns,
                nic2_name, nic2_ip, nic2_mac, nic2_subnet,
                nic3_name, nic3_ip, nic3_mac, nic3_subnet,
                nic4_name, nic4_ip, nic4_mac, nic4_subnet,
                web_interface_url,
                area, zone, line, pitch, floor, pillar,
                additional_notes,
                purchase_date, service_date, warranty_date,
                created_at, updated_at, last_discovered, discovery_method
            ) VALUES (
                @Hostname, @DeviceStatus, @DeviceType, @SerialNumber, @AssetTag, @EquipmentGroup,
                @DomainName, @IsDomainJoined,
                @Manufacturer, @Model, @CpuInfo, @BiosVersion,
                @TotalRamGb, @RamType, @RamSpeed, @RamManufacturer,
                @OsName, @OSVersion, @OsArchitecture, @OsInstallDate,
                @StorageInfo, @StorageType, @StorageModel,
                @Drive2Name, @Drive2Capacity, @Drive2Type, @Drive2Model,
                @Drive3Name, @Drive3Capacity, @Drive3Type, @Drive3Model,
                @Drive4Name, @Drive4Capacity, @Drive4Type, @Drive4Model,
                @PrimaryIp, @PrimaryMac, @PrimarySubnet, @PrimaryDns, @SecondaryDns,
                @Nic2Name, @Nic2Ip, @Nic2Mac, @Nic2Subnet,
                @Nic3Name, @Nic3Ip, @Nic3Mac, @Nic3Subnet,
                @Nic4Name, @Nic4Ip, @Nic4Mac, @Nic4Subnet,
                @WebInterfaceUrl,
                @Area, @Zone, @Line, @Pitch, @Floor, @Pillar,
                @AdditionalNotes,
                @PurchaseDate, @ServiceDate, @WarrantyDate,
                @CreatedAt, @UpdatedAt, @LastDiscovered, @DiscoveryMethod
            )";
    }

    public string GetUpdateLastDiscoveredQuery()
    {
        return @"
            UPDATE devices 
            SET LastDiscovered = @LastDiscovered, UpdatedAt = @UpdatedAt
            WHERE Hostname = @Hostname";
    }
}
