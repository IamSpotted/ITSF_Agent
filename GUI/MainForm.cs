#if WINDOWS
using DeviceAgent.Models;
using DeviceAgent.Services;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;

namespace DeviceAgent.GUI;

public partial class MainForm : Form
{
    private readonly IDeviceInfoService _deviceInfoService;
    private readonly IDeviceSyncService _deviceSyncService;
    private readonly IConfigurationService _configService;
    private readonly ILogger<MainForm> _logger;
    
    private NotifyIcon? _notifyIcon;
    private bool _isClosing = false;
    private System.Windows.Forms.Timer? _statusUpdateTimer;

    // Controls
    private TabControl _tabControl = null!;
    private TextBox _serverNameTextBox = null!;
    private TextBox _databaseNameTextBox = null!;
    private TextBox _connectionStringTextBox = null!;
    private NumericUpDown _intervalNumeric = null!;
    private TextBox _deviceInfoTextBox = null!;
    private TextBox _logTextBox = null!;
    private Button _forceUpdateButton = null!;
    private Button _saveConfigButton = null!;
    private Label _statusLabel = null!;
    private Label _lastSyncLabel = null!;
    private Label _nextSyncLabel = null!;
    private CheckBox _showGuiCheckBox = null!;
    private CheckBox _minimizeToTrayCheckBox = null!;
    private ComboBox _timeZoneComboBox = null!;

    public MainForm(
        IDeviceInfoService deviceInfoService,
        IDeviceSyncService deviceSyncService,
        IConfigurationService configService,
        ILogger<MainForm> logger)
    {
        _deviceInfoService = deviceInfoService;
        _deviceSyncService = deviceSyncService;
        _configService = configService;
        _logger = logger;
        
        InitializeComponent();
        InitializeNotifyIcon();
        LoadConfiguration();
        
        // Start status update timer
        _statusUpdateTimer = new System.Windows.Forms.Timer();
        _statusUpdateTimer.Interval = 5000; // Update every 5 seconds
        _statusUpdateTimer.Tick += StatusUpdateTimer_Tick;
        _statusUpdateTimer.Start();
        
        // Subscribe to configuration changes
        _configService.ConfigurationChanged += OnConfigurationChanged;
    }

    private void InitializeComponent()
    {
        Text = "Device Agent Configuration";
        Size = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = SystemIcons.Application;
        MinimumSize = new Size(600, 450);

        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(10, 10)
        };

        // Configuration Tab
        var configTab = new TabPage("Configuration");
        var configPanel = CreateConfigurationPanel();
        configTab.Controls.Add(configPanel);
        _tabControl.TabPages.Add(configTab);

        // Device Info Tab
        var deviceTab = new TabPage("Device Information");
        var devicePanel = CreateDeviceInfoPanel();
        deviceTab.Controls.Add(devicePanel);
        _tabControl.TabPages.Add(deviceTab);

        // Status & Logs Tab
        var statusTab = new TabPage("Status & Logs");
        var statusPanel = CreateStatusPanel();
        statusTab.Controls.Add(statusPanel);
        _tabControl.TabPages.Add(statusTab);

        Controls.Add(_tabControl);
    }

    private Panel CreateConfigurationPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            AutoSize = true
        };
        
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));

        // Server Name
        layout.Controls.Add(new Label { Text = "Server Name:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
        _serverNameTextBox = new TextBox { Dock = DockStyle.Fill };
        layout.Controls.Add(_serverNameTextBox, 1, 0);

        // Database Name
        layout.Controls.Add(new Label { Text = "Database Name:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
        _databaseNameTextBox = new TextBox { Dock = DockStyle.Fill };
        layout.Controls.Add(_databaseNameTextBox, 1, 1);

        // Connection String (read-only for reference)
        layout.Controls.Add(new Label { Text = "Connection String:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
        _connectionStringTextBox = new TextBox 
        { 
            Dock = DockStyle.Fill, 
            Multiline = true, 
            Height = 60, 
            ReadOnly = true,
            BackColor = SystemColors.Control
        };
        layout.Controls.Add(_connectionStringTextBox, 1, 2);

        // Check-in Interval
        layout.Controls.Add(new Label { Text = "Check-in Interval (days):", TextAlign = ContentAlignment.MiddleRight }, 0, 3);
        _intervalNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 365, Value = 7 };
        layout.Controls.Add(_intervalNumeric, 1, 3);

        // GUI Options
        layout.Controls.Add(new Label { Text = "Show GUI at startup:", TextAlign = ContentAlignment.MiddleRight }, 0, 4);
        _showGuiCheckBox = new CheckBox { Dock = DockStyle.Fill, Checked = true };
        layout.Controls.Add(_showGuiCheckBox, 1, 4);

        layout.Controls.Add(new Label { Text = "Minimize to system tray:", TextAlign = ContentAlignment.MiddleRight }, 0, 5);
        _minimizeToTrayCheckBox = new CheckBox { Dock = DockStyle.Fill, Checked = true };
        layout.Controls.Add(_minimizeToTrayCheckBox, 1, 5);

        // Timezone Selection
        layout.Controls.Add(new Label { Text = "Time Zone:", TextAlign = ContentAlignment.MiddleRight }, 0, 6);
        _timeZoneComboBox = new ComboBox 
        { 
            Dock = DockStyle.Fill, 
            DropDownStyle = ComboBoxStyle.DropDownList 
        };
        PopulateTimeZoneComboBox();
        layout.Controls.Add(_timeZoneComboBox, 1, 6);

        // Save button
        layout.Controls.Add(new Label(), 0, 7); // Spacer
        _saveConfigButton = new Button { Text = "Save Configuration", Dock = DockStyle.Fill };
        _saveConfigButton.Click += SaveConfigButton_Click;
        layout.Controls.Add(_saveConfigButton, 1, 7);

        // Force update button
        layout.Controls.Add(new Label(), 0, 8); // Spacer
        _forceUpdateButton = new Button { Text = "Force Update Now", Dock = DockStyle.Fill, BackColor = Color.LightGreen };
        _forceUpdateButton.Click += ForceUpdateButton_Click;
        layout.Controls.Add(_forceUpdateButton, 1, 8);

        // Event handlers to update connection string display
        _serverNameTextBox.TextChanged += (s, e) => UpdateConnectionString();
        _databaseNameTextBox.TextChanged += (s, e) => UpdateConnectionString();

        panel.Controls.Add(layout);
        return panel;
    }

    private void PopulateTimeZoneComboBox()
    {
        try
        {
            var timeZones = TimeZoneInfo.GetSystemTimeZones()
                .Select(tz => new { DisplayName = $"({tz.BaseUtcOffset:hh\\:mm}) {tz.DisplayName}", Id = tz.Id })
                .OrderBy(tz => tz.DisplayName)
                .ToList();

            _timeZoneComboBox.DisplayMember = "DisplayName";
            _timeZoneComboBox.ValueMember = "Id";
            _timeZoneComboBox.DataSource = timeZones;

            // Set default to Eastern Standard Time
            var defaultTimeZone = timeZones.FirstOrDefault(tz => tz.Id == "Eastern Standard Time") 
                                ?? timeZones.FirstOrDefault();
            if (defaultTimeZone != null)
            {
                _timeZoneComboBox.SelectedValue = defaultTimeZone.Id;
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error populating timezone list: {ex.Message}");
            // Add a fallback option
            _timeZoneComboBox.Items.Add("System Default");
            _timeZoneComboBox.SelectedIndex = 0;
        }
    }

    private void UpdateConnectionString()
    {
        if (string.IsNullOrWhiteSpace(_serverNameTextBox?.Text) || string.IsNullOrWhiteSpace(_databaseNameTextBox?.Text))
        {
            _connectionStringTextBox.Text = "Enter server and database name to see connection string";
            return;
        }

        var connectionString = $"Server={_serverNameTextBox.Text};Database={_databaseNameTextBox.Text};Integrated Security=true;TrustServerCertificate=true;";
        _connectionStringTextBox.Text = connectionString;
    }

    private Panel CreateDeviceInfoPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var refreshButton = new Button { Text = "Refresh Device Information", Dock = DockStyle.Fill };
        refreshButton.Click += RefreshDeviceInfo_Click;
        layout.Controls.Add(refreshButton, 0, 0);

        _deviceInfoTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            ReadOnly = true,
            Font = new Font(FontFamily.GenericMonospace, 9F)
        };
        layout.Controls.Add(_deviceInfoTextBox, 0, 1);

        panel.Controls.Add(layout);
        return panel;
    }

    private Panel CreateStatusPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            AutoSize = true
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _statusLabel = new Label { Text = "Status: Starting...", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        layout.Controls.Add(_statusLabel, 0, 0);

        _lastSyncLabel = new Label { Text = "Last Sync: Unknown", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        layout.Controls.Add(_lastSyncLabel, 0, 1);

        _nextSyncLabel = new Label { Text = "Next Sync: Calculating...", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        layout.Controls.Add(_nextSyncLabel, 0, 2);

        var clearLogsButton = new Button { Text = "Clear Logs", Dock = DockStyle.Fill };
        clearLogsButton.Click += (s, e) => _logTextBox.Clear();
        layout.Controls.Add(clearLogsButton, 0, 3);

        _logTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            ReadOnly = true,
            Font = new Font(FontFamily.GenericMonospace, 8F)
        };
        layout.Controls.Add(_logTextBox, 0, 4);

        panel.Controls.Add(layout);
        return panel;
    }

    private void InitializeNotifyIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Device Agent",
            Visible = true
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Show", null, (s, e) => ShowForm());
        contextMenu.Items.Add("Force Update", null, async (s, e) => await ForceUpdate());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());
        
        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => ShowForm();
    }

    private void LoadConfiguration()
    {
        var config = _configService.GetConfiguration();
        
        // Parse connection string to extract server and database
        ParseConnectionString(config.ConnectionString);
        
        _intervalNumeric.Value = config.CheckInIntervalDays;
        _showGuiCheckBox.Checked = config.ShowGuiAtStartup;
        _minimizeToTrayCheckBox.Checked = config.MinimizeToTray;
        
        // Set timezone
        if (!string.IsNullOrEmpty(config.TimeZoneId))
        {
            _timeZoneComboBox.SelectedValue = config.TimeZoneId;
        }
    }

    private void ParseConnectionString(string connectionString)
    {
        _serverNameTextBox.Text = "";
        _databaseNameTextBox.Text = "";
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            UpdateConnectionString();
            return;
        }

        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            var keyValue = part.Split('=');
            if (keyValue.Length == 2)
            {
                var key = keyValue[0].Trim().ToLower();
                var value = keyValue[1].Trim();
                
                if (key == "server")
                {
                    _serverNameTextBox.Text = value;
                }
                else if (key == "database")
                {
                    _databaseNameTextBox.Text = value;
                }
            }
        }
        
        UpdateConnectionString();
    }

    private async void SaveConfigButton_Click(object? sender, EventArgs e)
    {
        try
        {
            // Validate server and database inputs
            if (string.IsNullOrWhiteSpace(_serverNameTextBox.Text))
            {
                MessageBox.Show("Please enter a server name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _serverNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_databaseNameTextBox.Text))
            {
                MessageBox.Show("Please enter a database name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _databaseNameTextBox.Focus();
                return;
            }

            // Build connection string from separate fields
            var connectionString = $"Server={_serverNameTextBox.Text.Trim()};Database={_databaseNameTextBox.Text.Trim()};Integrated Security=true;TrustServerCertificate=true;";

            var config = new AppConfiguration
            {
                ConnectionString = connectionString,
                CheckInIntervalDays = (int)_intervalNumeric.Value,
                ShowGuiAtStartup = _showGuiCheckBox.Checked,
                MinimizeToTray = _minimizeToTrayCheckBox.Checked,
                TimeZoneId = _timeZoneComboBox.SelectedValue?.ToString() ?? "Eastern Standard Time"
            };

            await _configService.SaveConfigurationAsync(config);
            MessageBox.Show("Configuration saved successfully!", "Configuration", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LogMessage("Configuration saved successfully");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            LogMessage($"Error saving configuration: {ex.Message}");
        }
    }

    private async void ForceUpdateButton_Click(object? sender, EventArgs e)
    {
        await ForceUpdate();
    }

    private async Task ForceUpdate()
    {
        try
        {
            _forceUpdateButton.Enabled = false;
            _forceUpdateButton.Text = "Updating...";
            _statusLabel.Text = "Status: Performing forced update...";
            LogMessage("Force update initiated by user");
            
            await _deviceSyncService.ForceSyncAsync();
            
            LogMessage("Force update completed successfully");
            _statusLabel.Text = "Status: Force update completed";
        }
        catch (Exception ex)
        {
            LogMessage($"Force update failed: {ex.Message}");
            _statusLabel.Text = "Status: Force update failed";
            MessageBox.Show($"Force update failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _forceUpdateButton.Enabled = true;
            _forceUpdateButton.Text = "Force Update Now";
        }
    }

    private async void RefreshDeviceInfo_Click(object? sender, EventArgs e)
    {
        try
        {
            var deviceInfo = await _deviceInfoService.GetCurrentDeviceInfoAsync();
            var sb = new StringBuilder();
            
            // Device Identity
            sb.AppendLine($"Hostname: {deviceInfo.hostname}");
            sb.AppendLine($"Serial Number: {deviceInfo.serial_number ?? "Unknown"}");
            sb.AppendLine($"Asset Tag: {deviceInfo.asset_tag ?? "Not Set"}");
            sb.AppendLine($"Device Type: {deviceInfo.device_type ?? "Unknown"}");
            sb.AppendLine($"Equipment Group: {deviceInfo.equipment_group ?? "Not Set"}");
            sb.AppendLine();
            
            // Domain Information
            sb.AppendLine($"Domain Name: {deviceInfo.domain_name ?? "Not domain joined"}");
            sb.AppendLine($"Domain Joined: {(deviceInfo.is_domain_joined == true ? "Yes" : "No")}");
            sb.AppendLine();
            
            // Hardware Information
            sb.AppendLine($"Manufacturer: {deviceInfo.manufacturer ?? "Unknown"}");
            sb.AppendLine($"Model: {deviceInfo.model ?? "Unknown"}");
            sb.AppendLine($"CPU Info: {deviceInfo.cpu_info ?? "Unknown"}");
            sb.AppendLine($"BIOS Version: {deviceInfo.bios_version ?? "Unknown"}");
            sb.AppendLine();
            
            // Memory Information
            sb.AppendLine($"Total RAM: {deviceInfo.total_ram_gb ?? 0} GB");
            sb.AppendLine($"RAM Type: {deviceInfo.ram_type ?? "Unknown"}");
            sb.AppendLine($"RAM Speed: {deviceInfo.ram_speed ?? "Unknown"}");
            sb.AppendLine($"RAM Manufacturer: {deviceInfo.ram_manufacturer ?? "Unknown"}");
            sb.AppendLine();
            
            // Operating System
            sb.AppendLine($"OS Name: {deviceInfo.os_name ?? "Unknown"}");
            sb.AppendLine($"OS Version: {deviceInfo.os_version ?? "Unknown"}");
            sb.AppendLine($"OS Architecture: {deviceInfo.os_architecture ?? "Unknown"}");
            sb.AppendLine($"OS Install Date: {deviceInfo.os_install_date?.ToString("yyyy-MM-dd") ?? "Unknown"}");
            sb.AppendLine();
            
            // Storage Information
            sb.AppendLine($"Primary Storage: {deviceInfo.storage_info ?? "Unknown"}");
            sb.AppendLine($"Storage Type: {deviceInfo.storage_type ?? "Unknown"}");
            sb.AppendLine($"Storage Model: {deviceInfo.storage_model ?? "Unknown"}");
            
            // Additional drives
            if (!string.IsNullOrEmpty(deviceInfo.drive2_name))
            {
                sb.AppendLine($"Drive 2: {deviceInfo.drive2_name} ({deviceInfo.drive2_capacity}) - {deviceInfo.drive2_type}");
            }
            if (!string.IsNullOrEmpty(deviceInfo.drive3_name))
            {
                sb.AppendLine($"Drive 3: {deviceInfo.drive3_name} ({deviceInfo.drive3_capacity}) - {deviceInfo.drive3_type}");
            }
            if (!string.IsNullOrEmpty(deviceInfo.drive4_name))
            {
                sb.AppendLine($"Drive 4: {deviceInfo.drive4_name} ({deviceInfo.drive4_capacity}) - {deviceInfo.drive4_type}");
            }
            sb.AppendLine();
            
            // Network Information
            sb.AppendLine($"Primary IP: {deviceInfo.primary_ip ?? "Unknown"}");
            sb.AppendLine($"Primary MAC: {deviceInfo.primary_mac ?? "Unknown"}");
            sb.AppendLine($"Primary Subnet: {deviceInfo.primary_subnet ?? "Unknown"}");
            sb.AppendLine($"Primary DNS: {deviceInfo.primary_dns ?? "Unknown"}");
            sb.AppendLine($"Secondary DNS: {deviceInfo.secondary_dns ?? "Unknown"}");
            
            // Additional NICs
            if (!string.IsNullOrEmpty(deviceInfo.nic2_name))
            {
                sb.AppendLine($"NIC 2: {deviceInfo.nic2_name} - {deviceInfo.nic2_ip} ({deviceInfo.nic2_mac})");
            }
            if (!string.IsNullOrEmpty(deviceInfo.nic3_name))
            {
                sb.AppendLine($"NIC 3: {deviceInfo.nic3_name} - {deviceInfo.nic3_ip} ({deviceInfo.nic3_mac})");
            }
            if (!string.IsNullOrEmpty(deviceInfo.nic4_name))
            {
                sb.AppendLine($"NIC 4: {deviceInfo.nic4_name} - {deviceInfo.nic4_ip} ({deviceInfo.nic4_mac})");
            }
            sb.AppendLine();
            
            // Status and Location
            sb.AppendLine($"Device Status: {deviceInfo.device_status ?? "Unknown"}");
            sb.AppendLine($"Area: {deviceInfo.area ?? "Not Set"}");
            sb.AppendLine($"Zone: {deviceInfo.zone ?? "Not Set"}");
            sb.AppendLine($"Line: {deviceInfo.line ?? "Not Set"}");
            sb.AppendLine($"Floor: {deviceInfo.floor ?? "Not Set"}");
            sb.AppendLine();
            
            // Timestamps
            sb.AppendLine($"Last Discovered: {deviceInfo.last_discovered?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never"}");
            sb.AppendLine($"Discovery Method: {deviceInfo.discovery_method ?? "Unknown"}");
            sb.AppendLine($"Created: {deviceInfo.created_at?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown"}");
            sb.AppendLine($"Updated: {deviceInfo.updated_at?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown"}");

            _deviceInfoTextBox.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            _deviceInfoTextBox.Text = $"Error collecting device information: {ex.Message}";
            LogMessage($"Error refreshing device info: {ex.Message}");
        }
    }

    private async void StatusUpdateTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            var timeUntilNext = await _deviceSyncService.GetTimeUntilNextCheckInAsync();
            
            if (timeUntilNext <= TimeSpan.Zero)
            {
                _nextSyncLabel.Text = "Next Sync: Due now";
                _statusLabel.Text = "Status: Sync due";
            }
            else
            {
                var nextSyncTime = DateTime.Now.Add(timeUntilNext);
                _nextSyncLabel.Text = $"Next Sync: {nextSyncTime:yyyy-MM-dd HH:mm:ss} (in {FormatTimeSpan(timeUntilNext)})";
                _statusLabel.Text = "Status: Running normally";
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Status: Error checking sync status";
            LogMessage($"Error updating status: {ex.Message}");
        }
    }

    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
            return $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
        else if (timeSpan.TotalHours >= 1)
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
        else
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
    }

    private void OnConfigurationChanged(object? sender, AppConfiguration config)
    {
        LogMessage($"Configuration updated - Check-in interval: {config.CheckInIntervalDays} days");
    }

    public void LogMessage(string message)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string>(LogMessage), message);
            return;
        }

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _logTextBox.AppendText($"[{timestamp}] {message}\r\n");
        _logTextBox.SelectionStart = _logTextBox.Text.Length;
        _logTextBox.ScrollToCaret();
    }

    private void ShowForm()
    {
        Show();
        WindowState = FormWindowState.Normal;
        BringToFront();
        Activate();
    }

    private void ExitApplication()
    {
        _isClosing = true;
        Application.Exit();
    }

    protected override void SetVisibleCore(bool value)
    {
        var config = _configService.GetConfiguration();
        base.SetVisibleCore(config.ShowGuiAtStartup && value);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_isClosing && _minimizeToTrayCheckBox.Checked)
        {
            e.Cancel = true;
            Hide();
            _notifyIcon!.ShowBalloonTip(2000, "Device Agent", "Application minimized to system tray", ToolTipIcon.Info);
        }
        else
        {
            base.OnFormClosing(e);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _statusUpdateTimer?.Dispose();
            _notifyIcon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
#endif
