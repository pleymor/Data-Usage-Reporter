using System.Reflection;
using DataUsageReporter.Core;
using DataUsageReporter.Core.Localization;
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
    private readonly ILocalizationService _localization;
    private readonly ICredentialManager _credentialManager;
    private readonly IEmailSender? _emailSender;
    private readonly StartupManager _startupManager;
    private readonly IUsageRepository? _usageRepository;
    private readonly IUsageAggregator _usageAggregator;
    private readonly ISpeedFormatter _speedFormatter;
    private readonly IReportScheduler? _reportScheduler;

    // Tab references for localization
    private TabPage? _graphTab;
    private TabPage? _settingsTab;
    private TabPage? _emailTab;
    private TabPage? _scheduleTab;
    private TabPage? _aboutTab;

    // Email settings controls
    private ComboBox? _smtpPresetComboBox;
    private TextBox? _smtpServerTextBox;
    private NumericUpDown? _smtpPortInput;
    private CheckBox? _useSslCheckBox;
    private TextBox? _senderEmailTextBox;
    private TextBox? _recipientEmailTextBox;
    private TextBox? _usernameTextBox;
    private TextBox? _passwordTextBox;
    private TextBox? _customSubjectTextBox;
    private Button? _testConnectionButton;
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
    private Button? _sendImmediateButton;
    private Label? _scheduleResultLabel;

    public OptionsForm(
        IUsageAggregator aggregator,
        ISpeedFormatter formatter,
        ISettingsRepository settingsRepository,
        ILocalizationService localization,
        ICredentialManager? credentialManager = null,
        IEmailSender? emailSender = null,
        IUsageRepository? usageRepository = null,
        IReportScheduler? reportScheduler = null)
    {
        _settingsRepository = settingsRepository;
        _localization = localization;
        _credentialManager = credentialManager ?? new CredentialManager();
        _emailSender = emailSender;
        _startupManager = new StartupManager();
        _usageRepository = usageRepository;
        _usageAggregator = aggregator;
        _speedFormatter = formatter;
        _reportScheduler = reportScheduler;

        // Subscribe to language changes
        _localization.LanguageChanged += OnLanguageChanged;

        // Form setup
        Text = _localization.GetString("Options_Title");
        Size = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(600, 400);

        // Set form icon
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "app.ico");
        if (File.Exists(iconPath))
        {
            Icon = new Icon(iconPath);
        }

        // Create tab control
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill
        };

        // Usage Graph tab
        _graphTab = new TabPage(_localization.GetString("Tab_Graph"));
        _graphPanel = new GraphPanel(aggregator, formatter, _localization)
        {
            Dock = DockStyle.Fill
        };
        _graphTab.Controls.Add(_graphPanel);
        _tabControl.TabPages.Add(_graphTab);

        // Settings tab
        _settingsTab = new TabPage(_localization.GetString("Tab_Settings"));
        _settingsTab.Controls.Add(CreateSettingsPanel());
        _tabControl.TabPages.Add(_settingsTab);

        // Email Settings tab
        _emailTab = new TabPage(_localization.GetString("Options_EmailSettings"));
        _emailTab.Controls.Add(CreateEmailSettingsPanel());
        _tabControl.TabPages.Add(_emailTab);

        // Schedule tab
        _scheduleTab = new TabPage(_localization.GetString("Tab_Schedule"));
        _scheduleTab.Controls.Add(CreateSchedulePanel());
        _tabControl.TabPages.Add(_scheduleTab);

        // About tab
        _aboutTab = new TabPage(_localization.GetString("Tab_Credits"));
        _aboutTab.Controls.Add(CreateAboutPanel());
        _tabControl.TabPages.Add(_aboutTab);

        Controls.Add(_tabControl);

        LoadEmailSettings();
        LoadScheduleSettings();
    }

    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        RefreshStrings();
    }

    private void RefreshStrings()
    {
        // Update form title
        Text = _localization.GetString("Options_Title");

        // Update tab titles
        if (_graphTab != null) _graphTab.Text = _localization.GetString("Tab_Graph");
        if (_settingsTab != null) _settingsTab.Text = _localization.GetString("Tab_Settings");
        if (_emailTab != null) _emailTab.Text = _localization.GetString("Options_EmailSettings");
        if (_scheduleTab != null) _scheduleTab.Text = _localization.GetString("Tab_Schedule");
        if (_aboutTab != null) _aboutTab.Text = _localization.GetString("Tab_Credits");

        // Refresh graph panel
        _graphPanel.RefreshStrings();

        // Rebuild Settings tab
        if (_settingsTab != null)
        {
            DisposeTabControls(_settingsTab);
            _settingsTab.Controls.Add(CreateSettingsPanel());
        }

        // Rebuild Email tab
        if (_emailTab != null)
        {
            DisposeTabControls(_emailTab);
            _emailTab.Controls.Add(CreateEmailSettingsPanel());
            LoadEmailSettings();
        }

        // Rebuild Schedule tab
        if (_scheduleTab != null)
        {
            DisposeTabControls(_scheduleTab);
            _scheduleTab.Controls.Add(CreateSchedulePanel());
            LoadScheduleSettings();
        }

        // Rebuild About tab
        if (_aboutTab != null)
        {
            DisposeTabControls(_aboutTab);
            _aboutTab.Controls.Add(CreateAboutPanel());
        }
    }

    private static void DisposeTabControls(TabPage tab)
    {
        while (tab.Controls.Count > 0)
        {
            var control = tab.Controls[0];
            tab.Controls.RemoveAt(0);
            control.Dispose();
        }
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
            RowCount = 4,
            AutoSize = true,
            Padding = new Padding(10)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var settings = _settingsRepository.Load();

        // Language selection
        layout.Controls.Add(new Label
        {
            Text = _localization.GetString("Label_Language") + ":",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 0);

        var languageComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 150
        };

        // Populate language dropdown
        foreach (var lang in _localization.SupportedLanguages)
        {
            languageComboBox.Items.Add(lang.DisplayName);
            if (lang.Code == _localization.CurrentLanguage)
            {
                languageComboBox.SelectedIndex = languageComboBox.Items.Count - 1;
            }
        }

        languageComboBox.SelectedIndexChanged += (s, e) =>
        {
            var selectedIndex = languageComboBox.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < _localization.SupportedLanguages.Count)
            {
                var selectedLang = _localization.SupportedLanguages[selectedIndex];
                _localization.SetLanguage(selectedLang.Code);
            }
        };
        layout.Controls.Add(languageComboBox, 1, 0);

        // Start with Windows checkbox
        var startupCheckbox = new CheckBox
        {
            Text = _localization.GetString("Settings_StartWithWindows"),
            Checked = _startupManager.IsStartupEnabled(),
            AutoSize = true
        };
        startupCheckbox.CheckedChanged += (s, e) =>
        {
            _startupManager.SetStartupEnabled(startupCheckbox.Checked);
            settings.StartWithWindows = startupCheckbox.Checked;
            _settingsRepository.Save(settings);
        };
        layout.Controls.Add(startupCheckbox, 0, 1);
        layout.SetColumnSpan(startupCheckbox, 2);

        // Data retention days
        layout.Controls.Add(new Label
        {
            Text = _localization.GetString("Settings_DataRetention") + ":",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 2);

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
        layout.Controls.Add(retentionInput, 1, 2);

        // Update interval
        layout.Controls.Add(new Label
        {
            Text = _localization.GetString("Settings_UpdateInterval") + ":",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 3);

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
        layout.Controls.Add(intervalInput, 1, 3);

        panel.Controls.Add(layout);

        var noteLabel = new Label
        {
            Text = _localization.GetString("Settings_Note"),
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
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;

        // SMTP Preset dropdown
        layout.Controls.Add(new Label { Text = _localization.GetString("Email_Provider") + ":", AutoSize = true }, 0, row);
        _smtpPresetComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        _smtpPresetComboBox.Items.Add(_localization.GetString("Email_Custom"));
        for (int i = 1; i < SmtpPresets.Length; i++)
        {
            _smtpPresetComboBox.Items.Add(SmtpPresets[i].Name);
        }
        _smtpPresetComboBox.SelectedIndex = 0;
        _smtpPresetComboBox.SelectedIndexChanged += OnSmtpPresetChanged;
        layout.Controls.Add(_smtpPresetComboBox, 1, row++);

        // SMTP Server
        layout.Controls.Add(new Label { Text = _localization.GetString("Email_SmtpServer") + ":", AutoSize = true }, 0, row);
        _smtpServerTextBox = new TextBox { Width = 300 };
        layout.Controls.Add(_smtpServerTextBox, 1, row++);

        // SMTP Port
        layout.Controls.Add(new Label { Text = _localization.GetString("Email_Port") + ":", AutoSize = true }, 0, row);
        _smtpPortInput = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 587, Width = 100 };
        layout.Controls.Add(_smtpPortInput, 1, row++);

        // Use SSL
        layout.Controls.Add(new Label { Text = _localization.GetString("Email_Security") + ":", AutoSize = true }, 0, row);
        _useSslCheckBox = new CheckBox { Text = _localization.GetString("Email_UseTls"), Checked = true };
        layout.Controls.Add(_useSslCheckBox, 1, row++);

        // Sender Email
        layout.Controls.Add(new Label { Text = _localization.GetString("Email_FromEmail") + ":", AutoSize = true }, 0, row);
        _senderEmailTextBox = new TextBox { Width = 300 };
        layout.Controls.Add(_senderEmailTextBox, 1, row++);

        // Recipient Email (mandatory)
        layout.Controls.Add(new Label { Text = _localization.GetString("Email_ToEmail") + ": *", AutoSize = true, ForeColor = Color.Black }, 0, row);
        _recipientEmailTextBox = new TextBox { Width = 300 };
        layout.Controls.Add(_recipientEmailTextBox, 1, row++);

        // Username
        layout.Controls.Add(new Label { Text = _localization.GetString("Email_Username") + ":", AutoSize = true }, 0, row);
        _usernameTextBox = new TextBox { Width = 300 };
        layout.Controls.Add(_usernameTextBox, 1, row++);

        // Password
        layout.Controls.Add(new Label { Text = _localization.GetString("Email_Password") + ":", AutoSize = true }, 0, row);
        _passwordTextBox = new TextBox { Width = 300, UseSystemPasswordChar = true };
        layout.Controls.Add(_passwordTextBox, 1, row++);

        // Custom Subject
        layout.Controls.Add(new Label { Text = _localization.GetString("Email_CustomSubject") + ":", AutoSize = true }, 0, row);
        var subjectPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        _customSubjectTextBox = new TextBox { Width = 250 };
        subjectPanel.Controls.Add(_customSubjectTextBox);
        subjectPanel.Controls.Add(new Label { Text = _localization.GetString("Email_CustomSubjectHint"), AutoSize = true, ForeColor = Color.Gray, Padding = new Padding(5, 3, 0, 0) });
        layout.Controls.Add(subjectPanel, 1, row++);

        // Buttons panel
        var buttonsPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };

        _testConnectionButton = new Button { Text = _localization.GetString("Email_TestConnection"), Width = 130, Margin = new Padding(0, 0, 5, 0) };
        _testConnectionButton.Click += OnTestConnectionClick;
        buttonsPanel.Controls.Add(_testConnectionButton);

        var saveButton = new Button { Text = _localization.GetString("Email_SaveSettings"), Width = 100 };
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
            Text = _localization.GetString("Email_RequiredNote"),
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
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;

        // Enable schedule
        _enableScheduleCheckBox = new CheckBox { Text = _localization.GetString("Schedule_EnableReports") };
        _enableScheduleCheckBox.CheckedChanged += OnScheduleEnabledChanged;
        layout.Controls.Add(_enableScheduleCheckBox, 0, row);
        layout.SetColumnSpan(_enableScheduleCheckBox, 2);
        row++;

        // Frequency
        layout.Controls.Add(new Label { Text = _localization.GetString("Schedule_Frequency") + ":", AutoSize = true }, 0, row);
        _frequencyComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
        _frequencyComboBox.Items.AddRange(new object[] {
            _localization.GetString("Schedule_Daily"),
            _localization.GetString("Schedule_Weekly"),
            _localization.GetString("Schedule_Monthly")
        });
        _frequencyComboBox.SelectedIndex = 0;
        _frequencyComboBox.SelectedIndexChanged += OnFrequencyChanged;
        layout.Controls.Add(_frequencyComboBox, 1, row++);

        // Time of day
        layout.Controls.Add(new Label { Text = _localization.GetString("Schedule_Time") + ":", AutoSize = true }, 0, row);
        _timePicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Width = 100
        };
        layout.Controls.Add(_timePicker, 1, row++);

        // Day of week (for weekly)
        layout.Controls.Add(new Label { Text = _localization.GetString("Schedule_DayOfWeek") + ":", AutoSize = true }, 0, row);
        _dayOfWeekComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
        _dayOfWeekComboBox.Items.AddRange(new object[] {
            _localization.GetString("Day_Sunday"),
            _localization.GetString("Day_Monday"),
            _localization.GetString("Day_Tuesday"),
            _localization.GetString("Day_Wednesday"),
            _localization.GetString("Day_Thursday"),
            _localization.GetString("Day_Friday"),
            _localization.GetString("Day_Saturday")
        });
        _dayOfWeekComboBox.SelectedIndex = 1;
        layout.Controls.Add(_dayOfWeekComboBox, 1, row++);

        // Day of month (for monthly)
        layout.Controls.Add(new Label { Text = _localization.GetString("Schedule_DayOfMonth") + ":", AutoSize = true }, 0, row);
        _dayOfMonthInput = new NumericUpDown { Minimum = 1, Maximum = 28, Value = 1, Width = 100 };
        layout.Controls.Add(_dayOfMonthInput, 1, row++);

        // Next run label
        layout.Controls.Add(new Label { Text = _localization.GetString("Schedule_NextRun") + ":", AutoSize = true }, 0, row);
        _nextRunLabel = new Label { Text = _localization.GetString("Schedule_NotScheduled"), AutoSize = true };
        layout.Controls.Add(_nextRunLabel, 1, row++);

        // Buttons panel
        var buttonsPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };

        var saveButton = new Button { Text = _localization.GetString("Button_SaveSchedule"), Width = 160, Margin = new Padding(0, 0, 10, 0) };
        saveButton.Click += OnSaveScheduleClick;
        buttonsPanel.Controls.Add(saveButton);

        _sendImmediateButton = new Button { Text = _localization.GetString("Schedule_SendImmediate"), Width = 140 };
        _sendImmediateButton.Click += OnSendImmediateClick;
        buttonsPanel.Controls.Add(_sendImmediateButton);

        layout.Controls.Add(buttonsPanel, 0, row);
        layout.SetColumnSpan(buttonsPanel, 2);
        row++;

        // Result label for send feedback
        _scheduleResultLabel = new Label { Text = "", AutoSize = true, ForeColor = Color.Gray };
        layout.Controls.Add(_scheduleResultLabel, 0, row);
        layout.SetColumnSpan(_scheduleResultLabel, 2);

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

            _customSubjectTextBox!.Text = config.CustomSubject ?? string.Empty;
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
            _testResultLabel!.Text = _localization.GetString("Error_EmailNotConfigured");
            _testResultLabel.ForeColor = Color.Red;
            return;
        }

        _testConnectionButton!.Enabled = false;
        _testResultLabel!.Text = _localization.GetString("Email_Testing");
        _testResultLabel.ForeColor = Color.Gray;

        try
        {
            // Save current settings temporarily for testing
            SaveEmailSettingsInternal();

            var result = await _emailSender.TestConnectionAsync();
            if (result.IsValid)
            {
                _testResultLabel.Text = _localization.GetString("Email_ConnectionSuccess");
                _testResultLabel.ForeColor = Color.Green;
            }
            else
            {
                _testResultLabel.Text = _localization.GetString("Error_Failed", result.ErrorMessage ?? "");
                _testResultLabel.ForeColor = Color.Red;
            }
        }
        catch (Exception ex)
        {
            _testResultLabel.Text = _localization.GetString("Error_Generic", ex.Message);
            _testResultLabel.ForeColor = Color.Red;
        }
        finally
        {
            _testConnectionButton.Enabled = true;
        }
    }

    private async void OnSendImmediateClick(object? sender, EventArgs e)
    {
        // Get selected frequency from combo box
        var frequency = (ReportFrequency)(_frequencyComboBox?.SelectedIndex ?? 0);

        // Validate email config exists
        var config = _settingsRepository.LoadEmailConfig();
        if (config == null || string.IsNullOrWhiteSpace(config.RecipientEmail))
        {
            _scheduleResultLabel!.Text = _localization.GetString("Error_ConfigureEmailFirst");
            _scheduleResultLabel.ForeColor = Color.Red;
            return;
        }

        _sendImmediateButton!.Enabled = false;
        _scheduleResultLabel!.Text = _localization.GetString("Email_GeneratingReport");
        _scheduleResultLabel.ForeColor = Color.Gray;

        try
        {
            var emailSender = new EmailSender(config, _credentialManager);

            if (_usageRepository == null)
            {
                _scheduleResultLabel.Text = _localization.GetString("Error_RepositoryNotAvailable");
                _scheduleResultLabel.ForeColor = Color.Red;
                return;
            }

            // Calculate period based on frequency
            var now = DateTime.Now;
            DateTime periodStart;
            switch (frequency)
            {
                case ReportFrequency.Weekly:
                    periodStart = now.Date.AddDays(-7);
                    break;
                case ReportFrequency.Monthly:
                    periodStart = now.Date.AddDays(-31);
                    break;
                case ReportFrequency.Daily:
                default:
                    periodStart = now.Date;
                    break;
            }

            var graphRenderer = new EmailReportGraphRenderer(_localization);
            var reportGenerator = new ReportGenerator(_usageRepository, _usageAggregator, _speedFormatter, _localization, graphRenderer);
            var report = await reportGenerator.GenerateReportAsync(periodStart, now, frequency, config.CustomSubject);

            _scheduleResultLabel.Text = _localization.GetString("Email_SendingEmail");

            // Send with timeout (60 seconds)
            var sendTask = emailSender.SendWithDetailsAsync(report);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(60));
            var completedTask = await Task.WhenAny(sendTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _scheduleResultLabel.Text = _localization.GetString("Error_Timeout");
                _scheduleResultLabel.ForeColor = Color.Red;
                return;
            }

            var (success, errorMessage) = await sendTask;

            if (success)
            {
                _scheduleResultLabel.Text = _localization.GetString("Email_ReportSentSuccess");
                _scheduleResultLabel.ForeColor = Color.Green;
            }
            else
            {
                _scheduleResultLabel.Text = _localization.GetString("Error_Failed", errorMessage ?? _localization.GetString("Error_UnknownSmtp"));
                _scheduleResultLabel.ForeColor = Color.Red;
            }
        }
        catch (Exception ex)
        {
            _scheduleResultLabel.Text = _localization.GetString("Error_Generic", ex.Message);
            _scheduleResultLabel.ForeColor = Color.Red;
        }
        finally
        {
            _sendImmediateButton.Enabled = true;
        }
    }

    private void OnSaveEmailSettingsClick(object? sender, EventArgs e)
    {
        SaveEmailSettingsInternal();
        MessageBox.Show(_localization.GetString("Email_SettingsSaved"), _localization.GetString("Tab_Settings"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void SaveEmailSettingsInternal()
    {
        var config = new EmailConfig
        {
            SmtpServer = _smtpServerTextBox!.Text,
            SmtpPort = (int)_smtpPortInput!.Value,
            UseSsl = _useSslCheckBox!.Checked,
            SenderEmail = _senderEmailTextBox!.Text,
            RecipientEmail = _recipientEmailTextBox!.Text,
            CustomSubject = string.IsNullOrWhiteSpace(_customSubjectTextBox!.Text) ? null : _customSubjectTextBox.Text
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

        MessageBox.Show(_localization.GetString("Schedule_Saved"), _localization.GetString("Tab_Settings"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UpdateNextRunLabel(ReportSchedule schedule)
    {
        if (!schedule.IsEnabled)
        {
            _nextRunLabel!.Text = _localization.GetString("Schedule_NotScheduled");
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
            Text = _localization.GetString("About_AppName"),
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 5)
        };
        layout.Controls.Add(appNameLabel);

        // Version
        var versionLabel = new Label
        {
            Text = _localization.GetString("About_Version", "1.0.0"),
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 5)
        };
        layout.Controls.Add(versionLabel);

        // Date
        var dateLabel = new Label
        {
            Text = _localization.GetString("About_Date"),
            AutoSize = true,
            ForeColor = Color.Gray,
            Padding = new Padding(0, 0, 0, 15)
        };
        layout.Controls.Add(dateLabel);

        // Developer
        var developerLabel = new Label
        {
            Text = _localization.GetString("About_DevelopedBy", "Adrien Laugueux"),
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 20)
        };
        layout.Controls.Add(developerLabel);

        // Third-party libraries header
        var librariesHeader = new Label
        {
            Text = _localization.GetString("About_ThirdPartyLibraries"),
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Unsubscribe from events to prevent memory leaks
            _localization.LanguageChanged -= OnLanguageChanged;
        }
        base.Dispose(disposing);
    }

    public void RefreshGraph()
    {
        _ = _graphPanel.RefreshDataAsync();
    }
}
