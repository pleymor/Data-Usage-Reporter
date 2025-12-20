using System.Reflection;
using DataUsageReporter.Core;
using DataUsageReporter.Data;
using DataUsageReporter.Email;

namespace DataUsageReporter.UI;

/// <summary>
/// Options dialog with tabs for graphs, settings, and email configuration.
/// </summary>
public class OptionsForm : Form
{
    private readonly TabControl _tabControl;
    private readonly GraphPanel _graphPanel;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ICredentialManager _credentialManager;
    private readonly IEmailSender? _emailSender;
    private readonly StartupManager _startupManager;
    private readonly IUsageRepository? _usageRepository;
    private readonly IUsageAggregator _usageAggregator;
    private readonly ISpeedFormatter _speedFormatter;
    private readonly IReportScheduler? _reportScheduler;

    // Email settings controls
    private ComboBox? _smtpPresetComboBox;
    private TextBox? _smtpServerTextBox;
    private NumericUpDown? _smtpPortInput;
    private CheckBox? _useSslCheckBox;
    private TextBox? _senderEmailTextBox;
    private TextBox? _recipientEmailTextBox;
    private TextBox? _usernameTextBox;
    private TextBox? _passwordTextBox;
    private Button? _testConnectionButton;
    private Button? _sendNowButton;
    private Label? _testResultLabel;

    // SMTP presets: (Name, Server, Port, UseSsl)
    private static readonly (string Name, string Server, int Port, bool UseSsl)[] SmtpPresets =
    {
        ("Custom", "", 587, true),
        ("Gmail", "smtp.gmail.com", 587, true),
        ("Outlook / Office 365", "smtp.office365.com", 587, true),
        ("Yahoo", "smtp.mail.yahoo.com", 587, true),
        ("iCloud", "smtp.mail.me.com", 587, true),
        ("Zoho", "smtp.zoho.com", 587, true),
        ("SendGrid", "smtp.sendgrid.net", 587, true),
        ("Mailgun", "smtp.mailgun.org", 587, true),
    };

    // Schedule controls
    private CheckBox? _enableScheduleCheckBox;
    private ComboBox? _frequencyComboBox;
    private DateTimePicker? _timePicker;
    private ComboBox? _dayOfWeekComboBox;
    private NumericUpDown? _dayOfMonthInput;
    private Label? _nextRunLabel;

    public OptionsForm(
        IUsageAggregator aggregator,
        ISpeedFormatter formatter,
        ISettingsRepository settingsRepository,
        ICredentialManager? credentialManager = null,
        IEmailSender? emailSender = null,
        IUsageRepository? usageRepository = null,
        IReportScheduler? reportScheduler = null)
    {
        _settingsRepository = settingsRepository;
        _credentialManager = credentialManager ?? new CredentialManager();
        _emailSender = emailSender;
        _startupManager = new StartupManager();
        _usageRepository = usageRepository;
        _usageAggregator = aggregator;
        _speedFormatter = formatter;
        _reportScheduler = reportScheduler;

        // Form setup
        Text = "Data Usage Reporter - Options";
        Size = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(600, 400);

        // Create tab control
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill
        };

        // Usage Graph tab
        var graphTab = new TabPage("Usage Graph");
        _graphPanel = new GraphPanel(aggregator, formatter)
        {
            Dock = DockStyle.Fill
        };
        graphTab.Controls.Add(_graphPanel);
        _tabControl.TabPages.Add(graphTab);

        // Settings tab
        var settingsTab = new TabPage("Settings");
        settingsTab.Controls.Add(CreateSettingsPanel());
        _tabControl.TabPages.Add(settingsTab);

        // Email Settings tab
        var emailTab = new TabPage("Email Settings");
        emailTab.Controls.Add(CreateEmailSettingsPanel());
        _tabControl.TabPages.Add(emailTab);

        // Schedule tab
        var scheduleTab = new TabPage("Schedule");
        scheduleTab.Controls.Add(CreateSchedulePanel());
        _tabControl.TabPages.Add(scheduleTab);

        // About tab
        var aboutTab = new TabPage("About");
        aboutTab.Controls.Add(CreateAboutPanel());
        _tabControl.TabPages.Add(aboutTab);

        Controls.Add(_tabControl);

        LoadEmailSettings();
        LoadScheduleSettings();
    }

    private Panel CreateSettingsPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true,
            Padding = new Padding(10)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var settings = _settingsRepository.Load();

        // Start with Windows checkbox
        var startupCheckbox = new CheckBox
        {
            Text = "Start with Windows",
            Checked = _startupManager.IsStartupEnabled(),
            AutoSize = true
        };
        startupCheckbox.CheckedChanged += (s, e) =>
        {
            _startupManager.SetStartupEnabled(startupCheckbox.Checked);
            settings.StartWithWindows = startupCheckbox.Checked;
            _settingsRepository.Save(settings);
        };
        layout.Controls.Add(startupCheckbox, 0, 0);
        layout.SetColumnSpan(startupCheckbox, 2);

        // Data retention days
        layout.Controls.Add(new Label
        {
            Text = "Data retention (days):",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 1);

        var retentionInput = new NumericUpDown
        {
            Minimum = 30,
            Maximum = 3650,
            Value = settings.DataRetentionDays,
            Width = 100
        };
        retentionInput.ValueChanged += (s, e) =>
        {
            settings.DataRetentionDays = (int)retentionInput.Value;
            _settingsRepository.Save(settings);
        };
        layout.Controls.Add(retentionInput, 1, 1);

        // Update interval
        layout.Controls.Add(new Label
        {
            Text = "Update interval (ms):",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 2);

        var intervalInput = new NumericUpDown
        {
            Minimum = 500,
            Maximum = 5000,
            Value = settings.UpdateIntervalMs,
            Increment = 100,
            Width = 100
        };
        intervalInput.ValueChanged += (s, e) =>
        {
            settings.UpdateIntervalMs = (int)intervalInput.Value;
            _settingsRepository.Save(settings);
        };
        layout.Controls.Add(intervalInput, 1, 2);

        panel.Controls.Add(layout);

        var noteLabel = new Label
        {
            Text = "Note: Some changes require restarting the application to take effect.",
            Dock = DockStyle.Bottom,
            AutoSize = true,
            ForeColor = Color.Gray,
            Padding = new Padding(10)
        };
        panel.Controls.Add(noteLabel);

        return panel;
    }

    private Panel CreateEmailSettingsPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            AutoScroll = true
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 12,
            AutoSize = true,
            Padding = new Padding(10)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;

        // SMTP Preset dropdown
        layout.Controls.Add(new Label { Text = "Provider:", AutoSize = true }, 0, row);
        _smtpPresetComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        foreach (var preset in SmtpPresets)
        {
            _smtpPresetComboBox.Items.Add(preset.Name);
        }
        _smtpPresetComboBox.SelectedIndex = 0;
        _smtpPresetComboBox.SelectedIndexChanged += OnSmtpPresetChanged;
        layout.Controls.Add(_smtpPresetComboBox, 1, row++);

        // SMTP Server
        layout.Controls.Add(new Label { Text = "SMTP Server:", AutoSize = true }, 0, row);
        _smtpServerTextBox = new TextBox { Width = 300 };
        layout.Controls.Add(_smtpServerTextBox, 1, row++);

        // SMTP Port
        layout.Controls.Add(new Label { Text = "Port:", AutoSize = true }, 0, row);
        _smtpPortInput = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 587, Width = 100 };
        layout.Controls.Add(_smtpPortInput, 1, row++);

        // Use SSL
        layout.Controls.Add(new Label { Text = "Security:", AutoSize = true }, 0, row);
        _useSslCheckBox = new CheckBox { Text = "Use TLS/SSL", Checked = true };
        layout.Controls.Add(_useSslCheckBox, 1, row++);

        // Sender Email
        layout.Controls.Add(new Label { Text = "From Email:", AutoSize = true }, 0, row);
        _senderEmailTextBox = new TextBox { Width = 300 };
        layout.Controls.Add(_senderEmailTextBox, 1, row++);

        // Recipient Email (mandatory)
        layout.Controls.Add(new Label { Text = "To Email: *", AutoSize = true, ForeColor = Color.Black }, 0, row);
        _recipientEmailTextBox = new TextBox { Width = 300 };
        layout.Controls.Add(_recipientEmailTextBox, 1, row++);

        // Username
        layout.Controls.Add(new Label { Text = "Username:", AutoSize = true }, 0, row);
        _usernameTextBox = new TextBox { Width = 300 };
        layout.Controls.Add(_usernameTextBox, 1, row++);

        // Password
        layout.Controls.Add(new Label { Text = "Password:", AutoSize = true }, 0, row);
        _passwordTextBox = new TextBox { Width = 300, UseSystemPasswordChar = true };
        layout.Controls.Add(_passwordTextBox, 1, row++);

        // Buttons panel
        var buttonsPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };

        _testConnectionButton = new Button { Text = "Test Connection", Width = 110, Margin = new Padding(0, 0, 5, 0) };
        _testConnectionButton.Click += OnTestConnectionClick;
        buttonsPanel.Controls.Add(_testConnectionButton);

        _sendNowButton = new Button { Text = "Send Report Now", Width = 110, Margin = new Padding(0, 0, 5, 0) };
        _sendNowButton.Click += OnSendNowClick;
        buttonsPanel.Controls.Add(_sendNowButton);

        var saveButton = new Button { Text = "Save Settings", Width = 100 };
        saveButton.Click += OnSaveEmailSettingsClick;
        buttonsPanel.Controls.Add(saveButton);

        layout.Controls.Add(buttonsPanel, 0, row);
        layout.SetColumnSpan(buttonsPanel, 2);
        row++;

        // Result label
        _testResultLabel = new Label { Text = "", AutoSize = true, ForeColor = Color.Gray };
        layout.Controls.Add(_testResultLabel, 0, row);
        layout.SetColumnSpan(_testResultLabel, 2);
        row++;

        // Required fields note
        var requiredNote = new Label
        {
            Text = "* Required field. Most providers require an App Password (not your regular password).",
            AutoSize = true,
            ForeColor = Color.Gray,
            Padding = new Padding(0, 10, 0, 0)
        };
        layout.Controls.Add(requiredNote, 0, row);
        layout.SetColumnSpan(requiredNote, 2);

        panel.Controls.Add(layout);

        return panel;
    }

    private void OnSmtpPresetChanged(object? sender, EventArgs e)
    {
        if (_smtpPresetComboBox == null) return;

        var index = _smtpPresetComboBox.SelectedIndex;
        if (index < 0 || index >= SmtpPresets.Length) return;

        var preset = SmtpPresets[index];

        // Only update if not "Custom"
        if (index > 0)
        {
            _smtpServerTextBox!.Text = preset.Server;
            _smtpPortInput!.Value = preset.Port;
            _useSslCheckBox!.Checked = preset.UseSsl;
        }
    }

    private Panel CreateSchedulePanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 7,
            AutoSize = true,
            Padding = new Padding(10)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;

        // Enable schedule
        _enableScheduleCheckBox = new CheckBox { Text = "Enable Scheduled Reports" };
        _enableScheduleCheckBox.CheckedChanged += OnScheduleEnabledChanged;
        layout.Controls.Add(_enableScheduleCheckBox, 0, row);
        layout.SetColumnSpan(_enableScheduleCheckBox, 2);
        row++;

        // Frequency
        layout.Controls.Add(new Label { Text = "Frequency:", AutoSize = true }, 0, row);
        _frequencyComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
        _frequencyComboBox.Items.AddRange(new object[] { "Daily", "Weekly", "Monthly" });
        _frequencyComboBox.SelectedIndex = 0;
        _frequencyComboBox.SelectedIndexChanged += OnFrequencyChanged;
        layout.Controls.Add(_frequencyComboBox, 1, row++);

        // Time of day
        layout.Controls.Add(new Label { Text = "Time:", AutoSize = true }, 0, row);
        _timePicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Width = 100
        };
        layout.Controls.Add(_timePicker, 1, row++);

        // Day of week (for weekly)
        layout.Controls.Add(new Label { Text = "Day of Week:", AutoSize = true }, 0, row);
        _dayOfWeekComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
        _dayOfWeekComboBox.Items.AddRange(new object[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" });
        _dayOfWeekComboBox.SelectedIndex = 1;
        layout.Controls.Add(_dayOfWeekComboBox, 1, row++);

        // Day of month (for monthly)
        layout.Controls.Add(new Label { Text = "Day of Month:", AutoSize = true }, 0, row);
        _dayOfMonthInput = new NumericUpDown { Minimum = 1, Maximum = 28, Value = 1, Width = 100 };
        layout.Controls.Add(_dayOfMonthInput, 1, row++);

        // Next run label
        layout.Controls.Add(new Label { Text = "Next Run:", AutoSize = true }, 0, row);
        _nextRunLabel = new Label { Text = "Not scheduled", AutoSize = true };
        layout.Controls.Add(_nextRunLabel, 1, row++);

        // Save button
        var saveButton = new Button { Text = "Save Schedule", Width = 120 };
        saveButton.Click += OnSaveScheduleClick;
        layout.Controls.Add(saveButton, 0, row);

        panel.Controls.Add(layout);

        UpdateScheduleVisibility();

        return panel;
    }

    private void LoadEmailSettings()
    {
        var config = _settingsRepository.LoadEmailConfig();
        if (config != null)
        {
            _smtpServerTextBox!.Text = config.SmtpServer;
            _smtpPortInput!.Value = config.SmtpPort;
            _useSslCheckBox!.Checked = config.UseSsl;
            _senderEmailTextBox!.Text = config.SenderEmail;
            _recipientEmailTextBox!.Text = config.RecipientEmail;

            // Select matching preset
            var presetIndex = 0; // Default to "Custom"
            for (int i = 1; i < SmtpPresets.Length; i++)
            {
                if (SmtpPresets[i].Server.Equals(config.SmtpServer, StringComparison.OrdinalIgnoreCase))
                {
                    presetIndex = i;
                    break;
                }
            }
            _smtpPresetComboBox!.SelectedIndex = presetIndex;

            var credentials = _credentialManager.Retrieve(config.CredentialKey);
            if (credentials.HasValue)
            {
                _usernameTextBox!.Text = credentials.Value.Username;
                // Don't populate password for security
            }
        }
    }

    private void LoadScheduleSettings()
    {
        var schedule = _settingsRepository.LoadSchedule();
        if (schedule != null)
        {
            _enableScheduleCheckBox!.Checked = schedule.IsEnabled;
            _frequencyComboBox!.SelectedIndex = (int)schedule.Frequency;
            _timePicker!.Value = DateTime.Today.Add(schedule.TimeOfDay);
            if (schedule.DayOfWeek.HasValue)
                _dayOfWeekComboBox!.SelectedIndex = (int)schedule.DayOfWeek.Value;
            if (schedule.DayOfMonth.HasValue)
                _dayOfMonthInput!.Value = schedule.DayOfMonth.Value;

            UpdateNextRunLabel(schedule);
        }
        UpdateScheduleVisibility();
    }

    private void OnScheduleEnabledChanged(object? sender, EventArgs e)
    {
        UpdateScheduleVisibility();
    }

    private void OnFrequencyChanged(object? sender, EventArgs e)
    {
        UpdateScheduleVisibility();
    }

    private void UpdateScheduleVisibility()
    {
        var enabled = _enableScheduleCheckBox?.Checked ?? false;
        var frequency = _frequencyComboBox?.SelectedIndex ?? 0;

        if (_frequencyComboBox != null) _frequencyComboBox.Enabled = enabled;
        if (_timePicker != null) _timePicker.Enabled = enabled;
        if (_dayOfWeekComboBox != null) _dayOfWeekComboBox.Enabled = enabled && frequency == 1;
        if (_dayOfMonthInput != null) _dayOfMonthInput.Enabled = enabled && frequency == 2;
    }

    private async void OnTestConnectionClick(object? sender, EventArgs e)
    {
        if (_emailSender == null)
        {
            _testResultLabel!.Text = "Email sender not configured";
            _testResultLabel.ForeColor = Color.Red;
            return;
        }

        _testConnectionButton!.Enabled = false;
        _testResultLabel!.Text = "Testing...";
        _testResultLabel.ForeColor = Color.Gray;

        try
        {
            // Save current settings temporarily for testing
            SaveEmailSettingsInternal();

            var result = await _emailSender.TestConnectionAsync();
            if (result.IsValid)
            {
                _testResultLabel.Text = "Connection successful!";
                _testResultLabel.ForeColor = Color.Green;
            }
            else
            {
                _testResultLabel.Text = $"Failed: {result.ErrorMessage}";
                _testResultLabel.ForeColor = Color.Red;
            }
        }
        catch (Exception ex)
        {
            _testResultLabel.Text = $"Error: {ex.Message}";
            _testResultLabel.ForeColor = Color.Red;
        }
        finally
        {
            _testConnectionButton.Enabled = true;
        }
    }

    private async void OnSendNowClick(object? sender, EventArgs e)
    {
        // Validate recipient email
        if (string.IsNullOrWhiteSpace(_recipientEmailTextBox?.Text))
        {
            _testResultLabel!.Text = "Recipient email is required";
            _testResultLabel.ForeColor = Color.Red;
            return;
        }

        _sendNowButton!.Enabled = false;
        _testResultLabel!.Text = "Generating report...";
        _testResultLabel.ForeColor = Color.Gray;

        try
        {
            // Save settings first
            SaveEmailSettingsInternal();

            // Create email sender with current config
            var config = _settingsRepository.LoadEmailConfig();
            if (config == null)
            {
                _testResultLabel.Text = "Failed to load email config";
                _testResultLabel.ForeColor = Color.Red;
                return;
            }

            var emailSender = new EmailSender(config, _credentialManager);

            // Generate report for last 24 hours
            if (_usageRepository == null)
            {
                _testResultLabel.Text = "Usage repository not available";
                _testResultLabel.ForeColor = Color.Red;
                return;
            }

            var reportGenerator = new ReportGenerator(_usageRepository, _usageAggregator, _speedFormatter);
            var now = DateTime.Now;
            var todayStart = now.Date; // Midnight today
            var report = await reportGenerator.GenerateReportAsync(todayStart, now, ReportFrequency.Daily);

            _testResultLabel.Text = "Sending email...";

            // Send with timeout (60 seconds)
            var sendTask = emailSender.SendWithDetailsAsync(report);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(60));
            var completedTask = await Task.WhenAny(sendTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _testResultLabel.Text = "Timeout: Email send took too long. Your ISP may block port 25. Configure SMTP relay.";
                _testResultLabel.ForeColor = Color.Red;
                return;
            }

            var (success, errorMessage) = await sendTask;

            if (success)
            {
                _testResultLabel.Text = "Report sent successfully!";
                _testResultLabel.ForeColor = Color.Green;
            }
            else
            {
                _testResultLabel.Text = $"Failed: {errorMessage ?? "Unknown error. Configure SMTP relay."}";
                _testResultLabel.ForeColor = Color.Red;
            }
        }
        catch (Exception ex)
        {
            _testResultLabel.Text = $"Error: {ex.Message}";
            _testResultLabel.ForeColor = Color.Red;
        }
        finally
        {
            _sendNowButton.Enabled = true;
        }
    }

    private void OnSaveEmailSettingsClick(object? sender, EventArgs e)
    {
        SaveEmailSettingsInternal();
        MessageBox.Show("Email settings saved.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void SaveEmailSettingsInternal()
    {
        var config = new EmailConfig
        {
            SmtpServer = _smtpServerTextBox!.Text,
            SmtpPort = (int)_smtpPortInput!.Value,
            UseSsl = _useSslCheckBox!.Checked,
            SenderEmail = _senderEmailTextBox!.Text,
            RecipientEmail = _recipientEmailTextBox!.Text
        };

        _settingsRepository.SaveEmailConfig(config);

        // Save credentials
        if (!string.IsNullOrEmpty(_usernameTextBox!.Text) && !string.IsNullOrEmpty(_passwordTextBox!.Text))
        {
            _credentialManager.Store(config.CredentialKey, _usernameTextBox.Text, _passwordTextBox.Text);
        }
    }

    private void OnSaveScheduleClick(object? sender, EventArgs e)
    {
        var schedule = new ReportSchedule
        {
            IsEnabled = _enableScheduleCheckBox!.Checked,
            Frequency = (ReportFrequency)_frequencyComboBox!.SelectedIndex,
            TimeOfDay = _timePicker!.Value.TimeOfDay,
            DayOfWeek = (DayOfWeek)_dayOfWeekComboBox!.SelectedIndex,
            DayOfMonth = (int)_dayOfMonthInput!.Value
        };

        // Calculate next run time
        var now = DateTime.Now;
        var nextRun = now.Date.Add(schedule.TimeOfDay);
        if (nextRun <= now) nextRun = nextRun.AddDays(1);
        schedule.NextRunTime = new DateTimeOffset(nextRun).ToUnixTimeSeconds();

        _settingsRepository.SaveSchedule(schedule);
        UpdateNextRunLabel(schedule);

        // Notify the scheduler to recalculate
        _reportScheduler?.RecalculateNextRun();

        MessageBox.Show("Schedule saved.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UpdateNextRunLabel(ReportSchedule schedule)
    {
        if (!schedule.IsEnabled)
        {
            _nextRunLabel!.Text = "Not scheduled";
        }
        else
        {
            var nextRun = DateTimeOffset.FromUnixTimeSeconds(schedule.NextRunTime).LocalDateTime;
            _nextRunLabel!.Text = nextRun.ToString("g");
        }
    }

    private Panel CreateAboutPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20)
        };

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(10)
        };

        // Application name
        var appNameLabel = new Label
        {
            Text = "Data Usage Reporter",
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 5)
        };
        layout.Controls.Add(appNameLabel);

        // Version
        var versionLabel = new Label
        {
            Text = "Version 1.0.0",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 5)
        };
        layout.Controls.Add(versionLabel);

        // Date
        var dateLabel = new Label
        {
            Text = "December 2025",
            AutoSize = true,
            ForeColor = Color.Gray,
            Padding = new Padding(0, 0, 0, 15)
        };
        layout.Controls.Add(dateLabel);

        // Developer
        var developerLabel = new Label
        {
            Text = "Developed by Adrien Laugueux",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 20)
        };
        layout.Controls.Add(developerLabel);

        // Third-party libraries header
        var librariesHeader = new Label
        {
            Text = "Third-Party Libraries",
            Font = new Font(Font.FontFamily, 11, FontStyle.Bold),
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 10)
        };
        layout.Controls.Add(librariesHeader);

        // Library credits
        var libraries = new[]
        {
            ("MailKit", "MIT License"),
            ("DnsClient", "Apache 2.0"),
            ("ScottPlot", "MIT License"),
            ("Microsoft.Data.Sqlite", "MIT License"),
            ("Vanara.PInvoke.IpHlpApi", "MIT License")
        };

        foreach (var (name, license) in libraries)
        {
            var libraryPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(0, 0, 0, 8)
            };

            var nameLabel = new Label
            {
                Text = $"\u2022 {name}",
                AutoSize = true,
                Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
            };
            libraryPanel.Controls.Add(nameLabel);

            var licenseLabel = new Label
            {
                Text = $"   {license}",
                AutoSize = true,
                ForeColor = Color.Gray
            };
            libraryPanel.Controls.Add(licenseLabel);

            layout.Controls.Add(libraryPanel);
        }

        panel.Controls.Add(layout);
        return panel;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }

    public void RefreshGraph()
    {
        _ = _graphPanel.RefreshDataAsync();
    }
}
