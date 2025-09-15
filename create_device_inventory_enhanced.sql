-- Enhanced IT Device Inventory Database
-- Updated to match DatabaseAdmin fields with multiple NICs and drives
-- Created: September 1, 2025
-- SQL Server Version for SSMS

-- Create the database (run this first, then connect to the new database)
CREATE DATABASE it_device_inventory;
GO

USE it_device_inventory;
GO

-- Main devices table with enhanced support for multiple NICs and drives
CREATE TABLE devices (
    device_id INT IDENTITY(1,1) PRIMARY KEY,
    
    -- Device Identity
    hostname NVARCHAR(255) UNIQUE NOT NULL,
    serial_number NVARCHAR(100),
    asset_tag NVARCHAR(50),
    device_type NVARCHAR(30),
    equipment_group NVARCHAR(50),

    -- Domain/Workgroup Information
    domain_name NVARCHAR(100),
    is_domain_joined BIT DEFAULT 0,
    
    -- Hardware Information
    manufacturer NVARCHAR(100),
    model NVARCHAR(100),
    cpu_info NVARCHAR(255),
    bios_version NVARCHAR(100),
    
    -- Memory Information
    total_ram_gb INT,
    ram_type NVARCHAR(50),
    ram_speed NVARCHAR(50),
    ram_manufacturer NVARCHAR(100),
    
    -- Operating System Information
    os_name NVARCHAR(100),
    os_version NVARCHAR(50),
    os_architecture NVARCHAR(50),
    os_install_date DATETIME2,
    
    -- Primary Storage Information
    storage_info NVARCHAR(255),
    storage_type NVARCHAR(50),
    storage_model NVARCHAR(100),
    
    -- Additional Storage Drives (3 sets)
    drive2_name NVARCHAR(50),
    drive2_capacity NVARCHAR(50),
    drive2_type NVARCHAR(50),
    drive2_model NVARCHAR(100),
    
    drive3_name NVARCHAR(50),
    drive3_capacity NVARCHAR(50),
    drive3_type NVARCHAR(50),
    drive3_model NVARCHAR(100),
    
    drive4_name NVARCHAR(50),
    drive4_capacity NVARCHAR(50),
    drive4_type NVARCHAR(50),
    drive4_model NVARCHAR(100),
    
    -- Primary Network Interface
    primary_ip NVARCHAR(45),
    primary_mac NVARCHAR(17),
    primary_subnet NVARCHAR(45),
    primary_dns NVARCHAR(45),
    secondary_dns NVARCHAR(45),
    
    -- Network Interface 2
    nic2_name NVARCHAR(100),
    nic2_ip NVARCHAR(45),
    nic2_mac NVARCHAR(17),
    nic2_subnet NVARCHAR(45),
    
    -- Network Interface 3
    nic3_name NVARCHAR(100),
    nic3_ip NVARCHAR(45),
    nic3_mac NVARCHAR(17),
    nic3_subnet NVARCHAR(45),
    
    -- Network Interface 4
    nic4_name NVARCHAR(100),
    nic4_ip NVARCHAR(45),
    nic4_mac NVARCHAR(17),
    nic4_subnet NVARCHAR(45),
    
    -- Web Interface
    web_interface_url NVARCHAR(255),
    
    -- Device Status and Location
    device_status NVARCHAR(20) DEFAULT 'Active' CHECK (device_status IN ('Active', 'Inactive', 'Retired', 'Missing', 'Maintenance')),
    area NVARCHAR(100),
    zone NVARCHAR(100),
    line NVARCHAR(100),
    pitch NVARCHAR(100),
    floor NVARCHAR(50),
    pillar NVARCHAR(50),
    
    -- Additional Information
    additional_notes NVARCHAR(MAX),
    
    -- Asset Management Dates
    purchase_date DATETIME2,
    service_date DATETIME2,
    warranty_date DATETIME2,
    
    -- System Fields
    created_at DATETIME2 DEFAULT GETDATE(),
    updated_at DATETIME2 DEFAULT GETDATE(),
    last_discovered DATETIME2,
    discovery_method NVARCHAR(50) -- 'Computer Scan', 'Manual Entry', etc.
);
GO

-- Create indexes for fast searches
CREATE INDEX IX_devices_hostname ON devices(hostname);
CREATE INDEX IX_devices_primary_ip ON devices(primary_ip);
CREATE INDEX IX_devices_nic2_ip ON devices(nic2_ip);
CREATE INDEX IX_devices_nic3_ip ON devices(nic3_ip);
CREATE INDEX IX_devices_nic4_ip ON devices(nic4_ip);
CREATE INDEX IX_devices_primary_mac ON devices(primary_mac);
CREATE INDEX IX_devices_nic2_mac ON devices(nic2_mac);
CREATE INDEX IX_devices_nic3_mac ON devices(nic3_mac);
CREATE INDEX IX_devices_nic4_mac ON devices(nic4_mac);
CREATE INDEX IX_devices_domain_name ON devices(domain_name);
CREATE INDEX IX_devices_area_zone ON devices(area, zone);
CREATE INDEX IX_devices_last_discovered ON devices(last_discovered);
CREATE INDEX IX_devices_device_status ON devices(device_status);
CREATE INDEX IX_devices_manufacturer_model ON devices(manufacturer, model);
CREATE INDEX IX_devices_serial_number ON devices(serial_number);
CREATE INDEX IX_devices_asset_tag ON devices(asset_tag);
CREATE INDEX IX_devices_purchase_date ON devices(purchase_date);
CREATE INDEX IX_devices_warranty_date ON devices(warranty_date);
CREATE INDEX IX_devices_service_date ON devices(service_date);
GO

-- Audit log table for 1-year tracking (immutable records)
CREATE TABLE device_audit_log (
    log_id INT IDENTITY(1,1) PRIMARY KEY,
    device_id INT,
    action_type NVARCHAR(20) NOT NULL CHECK (action_type IN ('CREATE', 'UPDATE', 'DELETE', 'DISCOVER')),
    field_name NVARCHAR(100),
    old_value NVARCHAR(MAX),
    new_value NVARCHAR(MAX),
    performed_at DATETIME2 DEFAULT GETDATE(),
    performed_by NVARCHAR(100) DEFAULT SUSER_SNAME(), -- Windows User ID who made the change
    application_user NVARCHAR(100), -- Application user if different from Windows user
    discovery_session_id UNIQUEIDENTIFIER,
    change_reason NVARCHAR(255) -- Optional reason for the change
    
    -- NOTE: NO FOREIGN KEY CONSTRAINT to devices table
    -- This allows audit logs to remain immutable even after devices are deleted
    -- device_id is kept for reference but not enforced by constraint
    -- Audit logs should remain intact until they age out (1-year retention)
);
GO

-- Create indexes for audit log
CREATE INDEX IX_audit_device_id ON device_audit_log(device_id);
CREATE INDEX IX_audit_performed_at ON device_audit_log(performed_at);
CREATE INDEX IX_audit_performed_by ON device_audit_log(performed_by);
CREATE INDEX IX_audit_session_id ON device_audit_log(discovery_session_id);
GO

-- Deleted devices archive table (1-year retention)
CREATE TABLE deleted_devices (
    deleted_device_id INT IDENTITY(1,1) PRIMARY KEY,
    original_device_id INT NOT NULL,
    
    -- All original device fields preserved
    hostname NVARCHAR(255) NOT NULL,
    serial_number NVARCHAR(100),
    asset_tag NVARCHAR(50),
    device_type NVARCHAR(30),
    equipment_group NVARCHAR(50),

    -- Domain/Workgroup Information
    domain_name NVARCHAR(100),
    is_domain_joined BIT,
    
    -- Hardware Information
    manufacturer NVARCHAR(100),
    model NVARCHAR(100),
    cpu_info NVARCHAR(255),
    bios_version NVARCHAR(100),
    
    -- Memory Information
    total_ram_gb INT,
    ram_type NVARCHAR(50),
    ram_speed NVARCHAR(50),
    ram_manufacturer NVARCHAR(100),
    
    -- Operating System Information
    os_name NVARCHAR(100),
    os_version NVARCHAR(50),
    os_architecture NVARCHAR(50),
    os_install_date DATETIME2,
    
    -- Primary Storage Information
    storage_info NVARCHAR(255),
    storage_type NVARCHAR(50),
    storage_model NVARCHAR(100),
    
    -- Additional Storage Drives (3 sets)
    drive2_name NVARCHAR(50),
    drive2_capacity NVARCHAR(50),
    drive2_type NVARCHAR(50),
    drive2_model NVARCHAR(100),
    
    drive3_name NVARCHAR(50),
    drive3_capacity NVARCHAR(50),
    drive3_type NVARCHAR(50),
    drive3_model NVARCHAR(100),
    
    drive4_name NVARCHAR(50),
    drive4_capacity NVARCHAR(50),
    drive4_type NVARCHAR(50),
    drive4_model NVARCHAR(100),
    
    -- Primary Network Interface
    primary_ip NVARCHAR(45),
    primary_mac NVARCHAR(17),
    primary_subnet NVARCHAR(45),
    primary_dns NVARCHAR(45),
    secondary_dns NVARCHAR(45),
    
    -- Network Interface 2
    nic2_name NVARCHAR(100),
    nic2_ip NVARCHAR(45),
    nic2_mac NVARCHAR(17),
    nic2_subnet NVARCHAR(45),
    
    -- Network Interface 3
    nic3_name NVARCHAR(100),
    nic3_ip NVARCHAR(45),
    nic3_mac NVARCHAR(17),
    nic3_subnet NVARCHAR(45),
    
    -- Network Interface 4
    nic4_name NVARCHAR(100),
    nic4_ip NVARCHAR(45),
    nic4_mac NVARCHAR(17),
    nic4_subnet NVARCHAR(45),
    
    -- Web Interface
    web_interface_url NVARCHAR(255),
    
    -- Device Status and Location
    device_status NVARCHAR(20),
    area NVARCHAR(100),
    zone NVARCHAR(100),
    line NVARCHAR(100),
    pitch NVARCHAR(100),
    floor NVARCHAR(50),
    pillar NVARCHAR(50),
    
    -- Additional Information
    additional_notes NVARCHAR(MAX),
    
    -- Asset Management Dates
    purchase_date DATETIME2,
    service_date DATETIME2,
    warranty_date DATETIME2,
    
    -- Original System Fields
    original_created_at DATETIME2,
    original_updated_at DATETIME2,
    last_discovered DATETIME2,
    discovery_method NVARCHAR(50),
    
    -- Deletion tracking fields
    deleted_at DATETIME2 DEFAULT GETDATE(),
    deleted_by NVARCHAR(100) DEFAULT SUSER_SNAME(),
    deletion_reason NVARCHAR(255)
);
GO

-- Create indexes for deleted devices table
CREATE INDEX IX_deleted_devices_hostname ON deleted_devices(hostname);
CREATE INDEX IX_deleted_devices_original_device_id ON deleted_devices(original_device_id);
CREATE INDEX IX_deleted_devices_deleted_at ON deleted_devices(deleted_at);
CREATE INDEX IX_deleted_devices_deleted_by ON deleted_devices(deleted_by);
CREATE INDEX IX_deleted_devices_primary_ip ON deleted_devices(primary_ip);
CREATE INDEX IX_deleted_devices_primary_mac ON deleted_devices(primary_mac);
CREATE INDEX IX_deleted_devices_serial_number ON deleted_devices(serial_number);
CREATE INDEX IX_deleted_devices_asset_tag ON deleted_devices(asset_tag);
GO

-- Discovery sessions tracking
CREATE TABLE discovery_sessions (
    session_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    hostname_target NVARCHAR(255),
    started_at DATETIME2 DEFAULT GETDATE(),
    completed_at DATETIME2,
    status NVARCHAR(20) DEFAULT 'Running' CHECK (status IN ('Running', 'Completed', 'Failed')),
    results_summary NVARCHAR(MAX) -- JSON summary of what was discovered/changed
);
GO

-- Create indexes for discovery sessions
CREATE INDEX IX_sessions_started_at ON discovery_sessions(started_at);
CREATE INDEX IX_sessions_hostname_target ON discovery_sessions(hostname_target);
GO

-- Create a stored procedure to auto-purge old audit logs (1 year) and deleted devices (1 year)
CREATE PROCEDURE CleanupAuditLogs
AS
BEGIN
    -- Clean up audit logs older than 1 year
    DELETE FROM device_audit_log 
    WHERE performed_at < DATEADD(YEAR, -1, GETDATE());
    
    -- Clean up deleted devices older than 1 year
    DELETE FROM deleted_devices 
    WHERE deleted_at < DATEADD(YEAR, -1, GETDATE());
END;
GO

-- Stored procedure to archive device to deleted_devices table
CREATE PROCEDURE ArchiveDeletedDevice
    @DeviceId INT,
    @DeletionReason NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Archive the device to deleted_devices table
    INSERT INTO deleted_devices (
        original_device_id, hostname, serial_number, asset_tag, device_type, equipment_group,
        domain_name, is_domain_joined, manufacturer, model, cpu_info, bios_version,
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
        web_interface_url, device_status, area, zone, line, pitch, floor, pillar,
        additional_notes, purchase_date, service_date, warranty_date,
        original_created_at, original_updated_at, last_discovered, discovery_method,
        deleted_by, deletion_reason
    )
    SELECT 
        device_id, hostname, serial_number, asset_tag, device_type, equipment_group,
        domain_name, is_domain_joined, manufacturer, model, cpu_info, bios_version,
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
        web_interface_url, device_status, area, zone, line, pitch, floor, pillar,
        additional_notes, purchase_date, service_date, warranty_date,
        created_at, updated_at, last_discovered, discovery_method,
        SUSER_SNAME(), @DeletionReason
    FROM devices 
    WHERE device_id = @DeviceId;
    
    -- Now delete from main table (triggers will handle audit logging)
    DELETE FROM devices WHERE device_id = @DeviceId;
END;
GO

-- Stored procedure to manually log custom audit entries (for application use)
CREATE PROCEDURE LogCustomAuditEntry
    @DeviceId INT,
    @ActionType NVARCHAR(20),
    @FieldName NVARCHAR(100) = NULL,
    @OldValue NVARCHAR(MAX) = NULL,
    @NewValue NVARCHAR(MAX) = NULL,
    @ApplicationUser NVARCHAR(100) = NULL,
    @ChangeReason NVARCHAR(255) = NULL,
    @DiscoverySessionId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO device_audit_log (
        device_id, 
        action_type, 
        field_name, 
        old_value, 
        new_value, 
        performed_by, 
        application_user, 
        change_reason, 
        discovery_session_id
    )
    VALUES (
        @DeviceId,
        @ActionType,
        @FieldName,
        @OldValue,
        @NewValue,
        SUSER_SNAME(),
        @ApplicationUser,
        @ChangeReason,
        @DiscoverySessionId
    );
END;
GO

-- Stored procedure to restore a device from deleted_devices table
CREATE PROCEDURE RestoreDeletedDevice
    @DeletedDeviceId INT,
    @RestoreReason NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @RestoredDeviceId INT;
    
    -- Restore device to main devices table
    INSERT INTO devices (
        hostname, serial_number, asset_tag, device_type, equipment_group,
        domain_name, is_domain_joined, manufacturer, model, cpu_info, bios_version,
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
        web_interface_url, device_status, area, zone, line, pitch, floor, pillar,
        additional_notes, purchase_date, service_date, warranty_date,
        last_discovered, discovery_method
    )
    SELECT 
        hostname, serial_number, asset_tag, device_type, equipment_group,
        domain_name, is_domain_joined, manufacturer, model, cpu_info, bios_version,
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
        web_interface_url, device_status, area, zone, line, pitch, floor, pillar,
        additional_notes, purchase_date, service_date, warranty_date,
        last_discovered, discovery_method
    FROM deleted_devices 
    WHERE deleted_device_id = @DeletedDeviceId;
    
    SET @RestoredDeviceId = SCOPE_IDENTITY();
    
    -- Log the restoration
    EXEC LogCustomAuditEntry 
        @DeviceId = @RestoredDeviceId,
        @ActionType = 'RESTORE',
        @FieldName = 'DEVICE_RESTORED',
        @NewValue = 'Device restored from deleted_devices archive',
        @ChangeReason = @RestoreReason;
    
    -- Remove from deleted_devices table
    DELETE FROM deleted_devices WHERE deleted_device_id = @DeletedDeviceId;
    
    SELECT @RestoredDeviceId as restored_device_id;
END;
GO

-- Stored procedure to get deleted devices history
CREATE PROCEDURE GetDeletedDevicesHistory
    @DaysBack INT = 30,
    @DeletedBy NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        deleted_device_id,
        original_device_id,
        hostname,
        manufacturer,
        model,
        serial_number,
        asset_tag,
        device_status,
        area,
        zone,
        deleted_at,
        deleted_by,
        deletion_reason,
        original_created_at,
        last_discovered
    FROM deleted_devices
    WHERE deleted_at >= DATEADD(DAY, -@DaysBack, GETDATE())
        AND (@DeletedBy IS NULL OR deleted_by LIKE '%' + @DeletedBy + '%')
    ORDER BY deleted_at DESC;
END;
GO

-- Stored procedure to get audit history for a specific device
CREATE PROCEDURE GetDeviceAuditHistory
    @DeviceId INT = NULL,
    @Hostname NVARCHAR(255) = NULL,
    @DaysBack INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @TargetDeviceId INT = @DeviceId;
    
    -- If hostname provided instead of device_id, look it up
    IF @TargetDeviceId IS NULL AND @Hostname IS NOT NULL
    BEGIN
        SELECT @TargetDeviceId = device_id FROM devices WHERE hostname = @Hostname;
    END
    
    SELECT 
        dal.log_id,
        dal.device_id,
        d.hostname,
        dal.action_type,
        dal.field_name,
        dal.old_value,
        dal.new_value,
        dal.performed_at,
        dal.performed_by,
        dal.application_user,
        dal.change_reason,
        dal.discovery_session_id
    FROM device_audit_log dal
    LEFT JOIN devices d ON dal.device_id = d.device_id
    WHERE (@TargetDeviceId IS NULL OR dal.device_id = @TargetDeviceId)
        AND dal.performed_at >= DATEADD(DAY, -@DaysBack, GETDATE())
    ORDER BY dal.performed_at DESC, dal.log_id DESC;
END;
GO

-- Stored procedure to get recent changes across all devices
CREATE PROCEDURE GetRecentAuditActivity
    @DaysBack INT = 7,
    @ActionType NVARCHAR(20) = NULL,
    @PerformedBy NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        dal.log_id,
        dal.device_id,
        d.hostname,
        dal.action_type,
        dal.field_name,
        dal.old_value,
        dal.new_value,
        dal.performed_at,
        dal.performed_by,
        dal.application_user,
        dal.change_reason
    FROM device_audit_log dal
    LEFT JOIN devices d ON dal.device_id = d.device_id
    WHERE dal.performed_at >= DATEADD(DAY, -@DaysBack, GETDATE())
        AND (@ActionType IS NULL OR dal.action_type = @ActionType)
        AND (@PerformedBy IS NULL OR dal.performed_by LIKE '%' + @PerformedBy + '%')
    ORDER BY dal.performed_at DESC, dal.log_id DESC;
END;
GO

-- Stored procedure for audit log retention management (1-year retention policy)
CREATE PROCEDURE CleanupOldAuditLogs
    @RetentionDays INT = 365,  -- Default 1 year retention
    @DryRun BIT = 1           -- Default to dry run (preview only)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@RetentionDays, GETDATE());
    DECLARE @RecordsToDelete INT;
    
    -- Count records that would be deleted
    SELECT @RecordsToDelete = COUNT(*)
    FROM device_audit_log 
    WHERE performed_at < @CutoffDate;
    
    PRINT 'Audit Log Cleanup Analysis:';
    PRINT '- Cutoff Date: ' + CONVERT(NVARCHAR(20), @CutoffDate, 120);
    PRINT '- Records older than ' + CAST(@RetentionDays AS NVARCHAR(10)) + ' days: ' + CAST(@RecordsToDelete AS NVARCHAR(10));
    
    IF @RecordsToDelete = 0
    BEGIN
        PRINT '- No old audit records to clean up.';
        RETURN;
    END
    
    IF @DryRun = 1
    BEGIN
        PRINT '- DRY RUN MODE: No records will be deleted.';
        PRINT '- To actually delete these records, run: EXEC CleanupOldAuditLogs @RetentionDays = ' + CAST(@RetentionDays AS NVARCHAR(10)) + ', @DryRun = 0';
        
        -- Show sample of records that would be deleted
        PRINT '';
        PRINT 'Sample of records that would be deleted:';
        SELECT TOP 10 
            log_id,
            device_id,
            action_type,
            performed_at,
            performed_by,
            DATEDIFF(DAY, performed_at, GETDATE()) AS days_old
        FROM device_audit_log 
        WHERE performed_at < @CutoffDate
        ORDER BY performed_at;
    END
    ELSE
    BEGIN
        -- Actually delete the old records
        DELETE FROM device_audit_log 
        WHERE performed_at < @CutoffDate;
        
        PRINT '- DELETED ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' old audit log records.';
        PRINT '- Audit log cleanup completed successfully.';
    END
END;
GO

-- Create comprehensive audit triggers for all device changes

-- Trigger for INSERT operations (device creation)
CREATE TRIGGER tr_devices_audit_insert
ON devices
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO device_audit_log (
        device_id, 
        action_type, 
        field_name, 
        old_value, 
        new_value, 
        performed_at, 
        performed_by, 
        application_user, 
        change_reason
    )
    SELECT 
        i.device_id,
        'CREATE',
        'DEVICE_CREATED',
        NULL,
        'Device created: ' + i.hostname + ' (' + ISNULL(i.manufacturer, 'Unknown') + ' ' + ISNULL(i.model, 'Unknown') + ')',
        GETDATE(),
        SUSER_SNAME(),
        SUSER_SNAME(), -- Application user same as Windows user for device creation
        'New device added to inventory'
    FROM inserted i;
END;
GO

-- Trigger for UPDATE operations (comprehensive field-by-field logging)
CREATE TRIGGER tr_devices_audit_update
ON devices
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Update the updated_at timestamp
    UPDATE devices 
    SET updated_at = GETDATE()
    WHERE device_id IN (SELECT DISTINCT device_id FROM inserted);
    
    -- Log detailed field changes
    INSERT INTO device_audit_log (device_id, action_type, field_name, old_value, new_value, performed_at, performed_by, application_user)
    SELECT 
        device_id,
        'UPDATE',
        field_name,
        old_value,
        new_value,
        GETDATE(),
        SUSER_SNAME(),
        SUSER_SNAME() -- Application user same as Windows user for updates
    FROM (
        -- Compare all relevant fields
        SELECT i.device_id, 'hostname' as field_name, d.hostname as old_value, i.hostname as new_value FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.hostname, '') != ISNULL(i.hostname, '')
        UNION ALL
        SELECT i.device_id, 'serial_number', d.serial_number, i.serial_number FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.serial_number, '') != ISNULL(i.serial_number, '')
        UNION ALL
        SELECT i.device_id, 'asset_tag', d.asset_tag, i.asset_tag FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.asset_tag, '') != ISNULL(i.asset_tag, '')
        UNION ALL
        SELECT i.device_id, 'device_type', d.device_type, i.device_type FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.device_type, '') != ISNULL(i.device_type, '')
        UNION ALL
        SELECT i.device_id, 'equipment_group', d.equipment_group, i.equipment_group FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.equipment_group, '') != ISNULL(i.equipment_group, '')
        UNION ALL
        SELECT i.device_id, 'domain_name', d.domain_name, i.domain_name FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.domain_name, '') != ISNULL(i.domain_name, '')
        UNION ALL
        SELECT i.device_id, 'is_domain_joined', CAST(d.is_domain_joined as NVARCHAR), CAST(i.is_domain_joined as NVARCHAR) FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.is_domain_joined, 0) != ISNULL(i.is_domain_joined, 0)
        UNION ALL
        SELECT i.device_id, 'manufacturer', d.manufacturer, i.manufacturer FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.manufacturer, '') != ISNULL(i.manufacturer, '')
        UNION ALL
        SELECT i.device_id, 'model', d.model, i.model FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.model, '') != ISNULL(i.model, '')
        UNION ALL
        SELECT i.device_id, 'cpu_info', d.cpu_info, i.cpu_info FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.cpu_info, '') != ISNULL(i.cpu_info, '')
        UNION ALL
        SELECT i.device_id, 'total_ram_gb', CAST(d.total_ram_gb as NVARCHAR), CAST(i.total_ram_gb as NVARCHAR) FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.total_ram_gb, 0) != ISNULL(i.total_ram_gb, 0)
        UNION ALL
        SELECT i.device_id, 'ram_type', d.ram_type, i.ram_type FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.ram_type, '') != ISNULL(i.ram_type, '')
        UNION ALL
        SELECT i.device_id, 'ram_speed', d.ram_speed, i.ram_speed FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.ram_speed, '') != ISNULL(i.ram_speed, '')
        UNION ALL
        SELECT i.device_id, 'ram_manufacturer', d.ram_manufacturer, i.ram_manufacturer FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.ram_manufacturer, '') != ISNULL(i.ram_manufacturer, '')
        UNION ALL
        SELECT i.device_id, 'os_name', d.os_name, i.os_name FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.os_name, '') != ISNULL(i.os_name, '')
        UNION ALL
        SELECT i.device_id, 'os_version', d.os_version, i.os_version FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.os_version, '') != ISNULL(i.os_version, '')
        UNION ALL
        SELECT i.device_id, 'os_architecture', d.os_architecture, i.os_architecture FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.os_architecture, '') != ISNULL(i.os_architecture, '')
        UNION ALL
        SELECT i.device_id, 'primary_ip', d.primary_ip, i.primary_ip FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.primary_ip, '') != ISNULL(i.primary_ip, '')
        UNION ALL
        SELECT i.device_id, 'primary_mac', d.primary_mac, i.primary_mac FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.primary_mac, '') != ISNULL(i.primary_mac, '')
        UNION ALL
        SELECT i.device_id, 'primary_subnet', d.primary_subnet, i.primary_subnet FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.primary_subnet, '') != ISNULL(i.primary_subnet, '')
        UNION ALL
        SELECT i.device_id, 'primary_dns', d.primary_dns, i.primary_dns FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.primary_dns, '') != ISNULL(i.primary_dns, '')
        UNION ALL
        SELECT i.device_id, 'secondary_dns', d.secondary_dns, i.secondary_dns FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.secondary_dns, '') != ISNULL(i.secondary_dns, '')
        UNION ALL
        SELECT i.device_id, 'device_status', d.device_status, i.device_status FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.device_status, '') != ISNULL(i.device_status, '')
        UNION ALL
        SELECT i.device_id, 'area', d.area, i.area FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.area, '') != ISNULL(i.area, '')
        UNION ALL
        SELECT i.device_id, 'zone', d.zone, i.zone FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.zone, '') != ISNULL(i.zone, '')
        UNION ALL
        SELECT i.device_id, 'line', d.line, i.line FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE ISNULL(d.line, '') != ISNULL(i.line, '')
        UNION ALL
        SELECT i.device_id, 'purchase_date', CONVERT(NVARCHAR, d.purchase_date, 120), CONVERT(NVARCHAR, i.purchase_date, 120) FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE d.purchase_date != i.purchase_date OR (d.purchase_date IS NULL AND i.purchase_date IS NOT NULL) OR (d.purchase_date IS NOT NULL AND i.purchase_date IS NULL)
        UNION ALL
        SELECT i.device_id, 'service_date', CONVERT(NVARCHAR, d.service_date, 120), CONVERT(NVARCHAR, i.service_date, 120) FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE d.service_date != i.service_date OR (d.service_date IS NULL AND i.service_date IS NOT NULL) OR (d.service_date IS NOT NULL AND i.service_date IS NULL)
        UNION ALL
        SELECT i.device_id, 'warranty_date', CONVERT(NVARCHAR, d.warranty_date, 120), CONVERT(NVARCHAR, i.warranty_date, 120) FROM inserted i INNER JOIN deleted d ON i.device_id = d.device_id WHERE d.warranty_date != i.warranty_date OR (d.warranty_date IS NULL AND i.warranty_date IS NOT NULL) OR (d.warranty_date IS NOT NULL AND i.warranty_date IS NULL)
    ) changes;
END;
GO

-- Trigger for DELETE operations (device removal with archiving)
CREATE TRIGGER tr_devices_audit_delete
ON devices
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Archive each deleted device to deleted_devices table
    INSERT INTO deleted_devices (
        original_device_id, hostname, serial_number, asset_tag, device_type, equipment_group,
        domain_name, is_domain_joined, manufacturer, model, cpu_info, bios_version,
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
        web_interface_url, device_status, area, zone, line, pitch, floor, pillar,
        additional_notes, purchase_date, service_date, warranty_date,
        original_created_at, original_updated_at, last_discovered, discovery_method,
        deleted_by, deletion_reason
    )
    SELECT 
        d.device_id, d.hostname, d.serial_number, d.asset_tag, d.device_type, d.equipment_group,
        d.domain_name, d.is_domain_joined, d.manufacturer, d.model, d.cpu_info, d.bios_version,
        d.total_ram_gb, d.ram_type, d.ram_speed, d.ram_manufacturer,
        d.os_name, d.os_version, d.os_architecture, d.os_install_date,
        d.storage_info, d.storage_type, d.storage_model,
        d.drive2_name, d.drive2_capacity, d.drive2_type, d.drive2_model,
        d.drive3_name, d.drive3_capacity, d.drive3_type, d.drive3_model,
        d.drive4_name, d.drive4_capacity, d.drive4_type, d.drive4_model,
        d.primary_ip, d.primary_mac, d.primary_subnet, d.primary_dns, d.secondary_dns,
        d.nic2_name, d.nic2_ip, d.nic2_mac, d.nic2_subnet,
        d.nic3_name, d.nic3_ip, d.nic3_mac, d.nic3_subnet,
        d.nic4_name, d.nic4_ip, d.nic4_mac, d.nic4_subnet,
        d.web_interface_url, d.device_status, d.area, d.zone, d.line, d.pitch, d.floor, d.pillar,
        d.additional_notes, d.purchase_date, d.service_date, d.warranty_date,
        d.created_at, d.updated_at, d.last_discovered, d.discovery_method,
        SUSER_SNAME(), 'Device deleted via application'
    FROM deleted d;
    
    -- Log the deletion in audit trail (single comprehensive entry)
    INSERT INTO device_audit_log (
        device_id, 
        action_type, 
        field_name, 
        old_value, 
        new_value, 
        performed_at, 
        performed_by, 
        application_user, 
        change_reason
    )
    SELECT 
        d.device_id,
        'DELETE',
        'DEVICE_DELETED',
        'Device removed: ' + d.hostname + ' (' + ISNULL(d.manufacturer, 'Unknown') + ' ' + ISNULL(d.model, 'Unknown') + ') - IP: ' + ISNULL(d.primary_ip, 'N/A'),
        'Device archived to deleted_devices table',
        GETDATE(),
        SUSER_SNAME(),
        SUSER_SNAME(), -- Application user same as Windows user for deletion
        'Device deleted via application' -- This will be updated by the application with user-provided reason
    FROM deleted d;
    
    -- Now actually delete from the main table
    DELETE FROM devices WHERE device_id IN (SELECT device_id FROM deleted);
END;
GO

-- Insert sample device for testing
INSERT INTO devices (
    hostname, 
    domain_name, 
    is_domain_joined,
    manufacturer, 
    model,
    cpu_info,
    total_ram_gb,
    ram_type,
    os_name,
    os_version,
    os_architecture,
    primary_ip,
    primary_mac,
    primary_subnet,
    device_status, 
    area, 
    zone, 
    discovery_method,
    additional_notes
) 
VALUES (
    'SAMPLE-WORKSTATION', 
    'CHATTPROD1.NA.VWG',
    1,
    'Dell Inc.', 
    'OptiPlex 7090',
    'Intel(R) Core(TM) i7-10700 CPU @ 2.90GHz',
    16,
    'DDR4',
    'Microsoft Windows 11 Enterprise',
    '10.0.22631',
    '64-bit',
    '192.168.1.100',
    '00:1B:21:12:34:56',
    '255.255.255.0',
    'Active', 
    'Production', 
    'A1',
    'Manual Entry',
    'Sample device for testing enhanced database structure with multiple NICs and drives'
);
GO

-- Sample queries for testing the new structure

-- Find devices by any network interface IP
/*
SELECT hostname, primary_ip, nic2_ip, nic3_ip, nic4_ip, manufacturer, model
FROM devices 
WHERE primary_ip LIKE '192.168.1.%' 
   OR nic2_ip LIKE '192.168.1.%' 
   OR nic3_ip LIKE '192.168.1.%' 
   OR nic4_ip LIKE '192.168.1.%';
*/

-- Find devices by MAC address across all NICs
/*
SELECT hostname, primary_mac, nic2_mac, nic3_mac, nic4_mac
FROM devices 
WHERE primary_mac = '00:1B:21:12:34:56'
   OR nic2_mac = '00:1B:21:12:34:56'
   OR nic3_mac = '00:1B:21:12:34:56'
   OR nic4_mac = '00:1B:21:12:34:56';
*/

-- Domain vs Workgroup breakdown
/*
SELECT 
    CASE WHEN is_domain_joined = 1 THEN 'Domain' ELSE 'Workgroup' END as join_type,
    COUNT(*) as device_count
FROM devices 
GROUP BY is_domain_joined;
*/

-- Storage summary across all drives
/*
SELECT 
    hostname,
    storage_info as primary_drive,
    drive2_name + ' (' + COALESCE(drive2_capacity, 'Unknown') + ')' as drive2,
    drive3_name + ' (' + COALESCE(drive3_capacity, 'Unknown') + ')' as drive3,
    drive4_name + ' (' + COALESCE(drive4_capacity, 'Unknown') + ')' as drive4
FROM devices 
WHERE storage_info IS NOT NULL;
*/

-- Devices by location hierarchy
/*
SELECT area, zone, line, COUNT(*) as device_count
FROM devices 
WHERE area IS NOT NULL 
GROUP BY area, zone, line
ORDER BY area, zone, line;
*/

-- Network interface utilization report
/*
SELECT 
    COUNT(*) as total_devices,
    COUNT(CASE WHEN primary_ip IS NOT NULL THEN 1 END) as primary_nic_used,
    COUNT(CASE WHEN nic2_ip IS NOT NULL THEN 1 END) as nic2_used,
    COUNT(CASE WHEN nic3_ip IS NOT NULL THEN 1 END) as nic3_used,
    COUNT(CASE WHEN nic4_ip IS NOT NULL THEN 1 END) as nic4_used
FROM devices;
*/

-- Storage drive utilization report
/*
SELECT 
    COUNT(*) as total_devices,
    COUNT(CASE WHEN storage_info IS NOT NULL THEN 1 END) as primary_drive_used,
    COUNT(CASE WHEN drive2_name IS NOT NULL THEN 1 END) as drive2_used,
    COUNT(CASE WHEN drive3_name IS NOT NULL THEN 1 END) as drive3_used,
    COUNT(CASE WHEN drive4_name IS NOT NULL THEN 1 END) as drive4_used
FROM devices;
*/

-- Recent discoveries
/*
SELECT hostname, manufacturer, model, domain_name, last_discovered, discovery_method
FROM devices 
WHERE last_discovered >= DATEADD(DAY, -7, GETDATE())
ORDER BY last_discovered DESC;
*/

-- Memory analysis
/*
SELECT 
    ram_type,
    AVG(total_ram_gb) as avg_ram_gb,
    MIN(total_ram_gb) as min_ram_gb,
    MAX(total_ram_gb) as max_ram_gb,
    COUNT(*) as device_count
FROM devices 
WHERE total_ram_gb > 0
GROUP BY ram_type
ORDER BY avg_ram_gb DESC;
*/

-- Show the enhanced table structure
SELECT 
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.IS_NULLABLE,
    c.COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME = 'devices'
ORDER BY c.ORDINAL_POSITION;

-- ========================================
-- AUDIT TRAIL SAMPLE QUERIES
-- ========================================

-- Get audit history for a specific device (last 30 days)
/*
EXEC GetDeviceAuditHistory @Hostname = 'SAMPLE-WORKSTATION', @DaysBack = 30;
*/

-- Get all recent audit activity (last 7 days)
/*
EXEC GetRecentAuditActivity @DaysBack = 7;
*/

-- Get only UPDATE actions in the last 7 days
/*
EXEC GetRecentAuditActivity @DaysBack = 7, @ActionType = 'UPDATE';
*/

-- Get changes made by a specific user
/*
EXEC GetRecentAuditActivity @DaysBack = 30, @PerformedBy = 'username';
*/

-- Manual custom audit log entry (for application use)
/*
EXEC LogCustomAuditEntry 
    @DeviceId = 1, 
    @ActionType = 'DISCOVER', 
    @FieldName = 'SCAN_COMPLETED', 
    @NewValue = 'Computer scan completed successfully',
    @ApplicationUser = 'IT-Admin',
    @ChangeReason = 'Automated inventory scan';
*/

-- View recent changes with device details
/*
SELECT 
    d.hostname,
    d.manufacturer,
    d.model,
    dal.action_type,
    dal.field_name,
    dal.old_value,
    dal.new_value,
    dal.performed_at,
    dal.performed_by,
    dal.change_reason
FROM device_audit_log dal
INNER JOIN devices d ON dal.device_id = d.device_id
WHERE dal.performed_at >= DATEADD(DAY, -7, GETDATE())
ORDER BY dal.performed_at DESC;
*/

-- Audit summary by action type
/*
SELECT 
    action_type,
    COUNT(*) as change_count,
    COUNT(DISTINCT device_id) as devices_affected,
    MIN(performed_at) as earliest_change,
    MAX(performed_at) as latest_change
FROM device_audit_log
WHERE performed_at >= DATEADD(DAY, -30, GETDATE())
GROUP BY action_type
ORDER BY change_count DESC;
*/

-- Most active users (by audit entries)
/*
SELECT 
    performed_by,
    COUNT(*) as total_changes,
    COUNT(DISTINCT device_id) as devices_modified,
    MIN(performed_at) as first_activity,
    MAX(performed_at) as last_activity
FROM device_audit_log
WHERE performed_at >= DATEADD(DAY, -30, GETDATE())
GROUP BY performed_by
ORDER BY total_changes DESC;
*/

-- Most frequently changed fields
/*
SELECT 
    field_name,
    COUNT(*) as change_count,
    COUNT(DISTINCT device_id) as devices_affected
FROM device_audit_log
WHERE action_type = 'UPDATE' 
    AND performed_at >= DATEADD(DAY, -30, GETDATE())
GROUP BY field_name
ORDER BY change_count DESC;
*/

-- Devices with the most changes
/*
SELECT 
    d.hostname,
    d.manufacturer,
    d.model,
    COUNT(dal.log_id) as total_changes,
    MAX(dal.performed_at) as last_change
FROM devices d
INNER JOIN device_audit_log dal ON d.device_id = dal.device_id
WHERE dal.performed_at >= DATEADD(DAY, -30, GETDATE())
GROUP BY d.device_id, d.hostname, d.manufacturer, d.model
ORDER BY total_changes DESC;
*/

-- Audit trail for warranty date changes specifically
/*
SELECT 
    d.hostname,
    dal.old_value as old_warranty_date,
    dal.new_value as new_warranty_date,
    dal.performed_at,
    dal.performed_by,
    dal.change_reason
FROM device_audit_log dal
INNER JOIN devices d ON dal.device_id = d.device_id
WHERE dal.field_name = 'warranty_date'
ORDER BY dal.performed_at DESC;
*/

-- Clean up old audit logs (run periodically)
/*
EXEC CleanupAuditLogs;
*/

-- ========================================
-- DELETED DEVICES SAMPLE QUERIES
-- ========================================

-- Get recently deleted devices (last 30 days)
/*
EXEC GetDeletedDevicesHistory @DaysBack = 30;
*/

-- Get devices deleted by specific user
/*
EXEC GetDeletedDevicesHistory @DaysBack = 90, @DeletedBy = 'username';
*/

-- Manually archive a device (alternative to DELETE)
/*
EXEC ArchiveDeletedDevice @DeviceId = 1, @DeletionReason = 'Device decommissioned due to hardware failure';
*/

-- Restore a device from archive
/*
EXEC RestoreDeletedDevice @DeletedDeviceId = 1, @RestoreReason = 'Device was accidentally deleted';
*/

-- View all deleted devices with original creation info
/*
SELECT 
    hostname,
    manufacturer,
    model,
    serial_number,
    original_created_at,
    deleted_at,
    deleted_by,
    deletion_reason,
    DATEDIFF(DAY, original_created_at, deleted_at) as days_in_service
FROM deleted_devices
ORDER BY deleted_at DESC;
*/

-- Find deleted device by hostname or IP for recovery
/*
SELECT 
    deleted_device_id,
    hostname,
    primary_ip,
    manufacturer,
    model,
    deleted_at,
    deleted_by,
    deletion_reason
FROM deleted_devices
WHERE hostname LIKE '%WORKSTATION%' 
   OR primary_ip = '192.168.1.100'
ORDER BY deleted_at DESC;
*/

-- Deletion statistics by user
/*
SELECT 
    deleted_by,
    COUNT(*) as devices_deleted,
    MIN(deleted_at) as first_deletion,
    MAX(deleted_at) as last_deletion
FROM deleted_devices
WHERE deleted_at >= DATEADD(DAY, -90, GETDATE())
GROUP BY deleted_by
ORDER BY devices_deleted DESC;
*/

-- Devices approaching permanent deletion (older than 11 months)
/*
SELECT 
    hostname,
    manufacturer,
    model,
    deleted_at,
    deleted_by,
    DATEDIFF(DAY, deleted_at, DATEADD(YEAR, 1, deleted_at)) as days_until_permanent_deletion
FROM deleted_devices
WHERE deleted_at < DATEADD(MONTH, -11, GETDATE())
ORDER BY deleted_at ASC;
*/

-- Compare active vs deleted device counts
/*
SELECT 
    'Active Devices' as status,
    COUNT(*) as device_count
FROM devices
UNION ALL
SELECT 
    'Deleted Devices (Archive)',
    COUNT(*)
FROM deleted_devices;
*/

-- Audit Log Retention Management Examples:
/*
-- Preview what audit logs would be cleaned up (DRY RUN):
EXEC CleanupOldAuditLogs @RetentionDays = 365, @DryRun = 1;

-- Actually clean up audit logs older than 1 year:
EXEC CleanupOldAuditLogs @RetentionDays = 365, @DryRun = 0;

-- Clean up audit logs older than 2 years:
EXEC CleanupOldAuditLogs @RetentionDays = 730, @DryRun = 0;

-- Set up SQL Server Agent job for automatic cleanup (recommended):
-- 1. Create a new SQL Server Agent Job
-- 2. Add a step with this command: EXEC CleanupOldAuditLogs @RetentionDays = 365, @DryRun = 0
-- 3. Schedule to run monthly or quarterly
-- 4. Set up alerts for job success/failure

-- Check audit log statistics:
SELECT 
    COUNT(*) as total_audit_records,
    MIN(performed_at) as oldest_record,
    MAX(performed_at) as newest_record,
    COUNT(CASE WHEN performed_at < DATEADD(YEAR, -1, GETDATE()) THEN 1 END) as records_older_than_1_year
FROM device_audit_log;
*/
