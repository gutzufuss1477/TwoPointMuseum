using System.Diagnostics;
using System.Reflection;

namespace TPMQoLTrainer;

internal sealed class MainForm : Form
{
    private const string DefaultConfig =
        @"C:\Program Files (x86)\Steam\steamapps\common\Two Point Museum\BepInEx\config\TPMQoL.cfg";

    private readonly string _languageFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TPMQoLTrainer",
        "language.txt");

    private readonly Color _bg = Color.FromArgb(24, 27, 32);
    private readonly Color _panel = Color.FromArgb(34, 38, 45);
    private readonly Color _accent = Color.FromArgb(82, 166, 255);
    private readonly Color _text = Color.FromArgb(235, 238, 242);
    private readonly Color _muted = Color.FromArgb(160, 168, 178);

    private readonly Label _statusLabel = new();
    private readonly Label _pathLabel = new();
    private readonly Label _modLabel = new();
    private readonly Label _languageLabel = new();
    private readonly ComboBox _languageBox = new();
    private readonly ComboBox _quality = new();
    private readonly System.Windows.Forms.Timer _statusTimer = new();

    private readonly NumericUpDown _workshop = Num(1, 20, 0.5m);
    private readonly NumericUpDown _training = Num(1, 20, 0.5m);
    private readonly NumericUpDown _analysis = Num(1, 20, 0.5m);
    private readonly NumericUpDown _wildlife = Num(1, 20, 0.5m);
    private readonly NumericUpDown _expeditions = Num(1, 20, 0.5m);
    private readonly NumericUpDown _staffMovement = Num(1, 5, 0.25m);

    private readonly CheckBox _analysisMax = Check("analysis_max");
    private readonly CheckBox _wildlifeMax = Check("vet_max");

    private readonly NumericUpDown _minimumRank = Num(1, 20, 1);
    private readonly CheckBox _emptySkills = Check("base_skill");

    private readonly CheckBox _maxSurvey = Check("max_survey");
    private readonly CheckBox _forceQuality = Check("force_quality");

    private readonly NumericUpDown _securityRadius = Num(0, 5000, 50);
    private readonly CheckBox _firstAid = Check("first_aid");

    private string _configPath = DefaultConfig;
    private bool _loaded;
    private bool _changingLanguage;

    internal MainForm()
    {
        Text = "Two Point Museum QoL Trainer";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 700);
        Size = new Size(980, 790);
        BackColor = _bg;
        ForeColor = _text;
        Font = new Font("Segoe UI", 9.5f);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;

        _configPath = DetectConfigPath();
        L.SetLanguage(LoadInitialLanguage());
        BuildUi();
        SelectLanguageInCombo();
        ApplyLanguage();
        LoadConfig();

        _statusTimer.Interval = 1000;
        _statusTimer.Tick += (_, _) => RefreshStatus();
        _statusTimer.Start();
        RefreshStatus();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 4,
            BackColor = _bg
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(), 0, 0);

        var columns = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = _bg,
            Padding = new Padding(0, 8, 0, 4)
        };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        columns.Controls.Add(BuildLeftColumn(), 0, 0);
        columns.Controls.Add(BuildRightColumn(), 1, 0);
        root.Controls.Add(columns, 0, 1);

        root.Controls.Add(BuildButtons(), 0, 2);

        var footer = new Label
        {
            Tag = "footer",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = _muted
        };
        root.Controls.Add(footer, 0, 3);
    }

    private Control BuildHeader()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = _panel, Padding = new Padding(14) };

        var title = new Label
        {
            Text = "Two Point Museum QoL Trainer",
            Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
            AutoSize = true,
            ForeColor = _text,
            Location = new Point(14, 8)
        };
        panel.Controls.Add(title);

        _statusLabel.AutoSize = true;
        _statusLabel.Location = new Point(18, 53);
        panel.Controls.Add(_statusLabel);

        _modLabel.AutoSize = true;
        _modLabel.Location = new Point(220, 53);
        _modLabel.ForeColor = _muted;
        panel.Controls.Add(_modLabel);

        _languageLabel.Tag = "language";
        _languageLabel.AutoSize = true;
        _languageLabel.ForeColor = _muted;
        _languageLabel.TextAlign = ContentAlignment.MiddleRight;
        panel.Controls.Add(_languageLabel);

        _languageBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _languageBox.Width = 170;
        _languageBox.BackColor = Color.FromArgb(48, 53, 62);
        _languageBox.ForeColor = _text;

        foreach (var language in L.Languages)
            _languageBox.Items.Add(language.Name);

        _languageBox.SelectedIndexChanged += (_, _) => ChangeLanguageFromCombo();
        panel.Controls.Add(_languageBox);

        var browse = MakeButton("choose_config", 128);
        browse.Click += (_, _) => ChooseConfig();
        panel.Controls.Add(browse);

        _pathLabel.AutoEllipsis = true;
        _pathLabel.ForeColor = _muted;
        panel.Controls.Add(_pathLabel);

        void LayoutHeader()
        {
            browse.Left = panel.ClientSize.Width - browse.Width - 14;
            browse.Top = 10;

            _languageBox.Left = browse.Left - _languageBox.Width - 12;
            _languageBox.Top = 11;

            _languageLabel.Left = _languageBox.Left - _languageLabel.PreferredWidth - 8;
            _languageLabel.Top = 17;

            _pathLabel.Left = 390;
            _pathLabel.Top = 51;
            _pathLabel.Width = Math.Max(150, panel.ClientSize.Width - 410);
            _pathLabel.Height = 24;
        }

        panel.Resize += (_, _) => LayoutHeader();
        LayoutHeader();

        return panel;
    }

    private Control BuildLeftColumn()
    {
        var flow = NewColumn();

        var speed = NewGroup("speed");
        AddNumericRow(speed, "workshop", _workshop, "x");
        AddNumericRow(speed, "training", _training, "x");
        AddNumericRow(speed, "analysis", _analysis, "x");
        AddNumericRow(speed, "vet", _wildlife, "x");
        AddNumericRow(speed, "expeditions", _expeditions, "x");
        AddNumericRow(speed, "staff", _staffMovement, "x");
        flow.Controls.Add(speed);

        var knowledge = NewGroup("knowledge");
        knowledge.Controls.Add(_analysisMax);
        knowledge.Controls.Add(_wildlifeMax);
        flow.Controls.Add(knowledge);

        return flow;
    }

    private Control BuildRightColumn()
    {
        var flow = NewColumn();

        var applicants = NewGroup("applicants");
        AddNumericRow(applicants, "minimum_rank", _minimumRank, "");
        applicants.Controls.Add(_emptySkills);
        flow.Controls.Add(applicants);

        var expedition = NewGroup("expeditions");
        expedition.Controls.Add(_maxSurvey);

        var qualityRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(3, 4, 3, 4)
        };

        qualityRow.Controls.Add(RowLabel("min_quality", 190));

        _quality.DropDownStyle = ComboBoxStyle.DropDownList;
        _quality.Width = 150;
        _quality.BackColor = Color.FromArgb(48, 53, 62);
        _quality.ForeColor = _text;
        qualityRow.Controls.Add(_quality);

        expedition.Controls.Add(qualityRow);
        expedition.Controls.Add(_forceQuality);
        flow.Controls.Add(expedition);

        var security = NewGroup("security");
        AddNumericRow(security, "coverage_radius", _securityRadius, "");
        flow.Controls.Add(security);

        var health = NewGroup("health");
        health.Controls.Add(_firstAid);
        flow.Controls.Add(health);

        return flow;
    }

    private Control BuildButtons()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 12, 0, 6),
            BackColor = _bg
        };

        var apply = MakeButton("apply", 125, _accent);
        apply.Click += (_, _) => SaveConfig();
        panel.Controls.Add(apply);

        var reload = MakeButton("reload", 115);
        reload.Click += (_, _) => LoadConfig();
        panel.Controls.Add(reload);

        var vanilla = MakeButton("vanilla", 125);
        vanilla.Click += (_, _) => SetVanilla();
        panel.Controls.Add(vanilla);

        var start = MakeButton("start_game", 120);
        start.Click += (_, _) => StartGame();
        panel.Controls.Add(start);

        return panel;
    }

    private FlowLayoutPanel NewColumn()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoScroll = true,
            WrapContents = false,
            Padding = new Padding(6),
            BackColor = _bg
        };
    }

    private FlowLayoutPanel NewGroup(string titleKey)
    {
        var box = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            BackColor = _panel,
            Padding = new Padding(14, 12, 14, 14),
            Margin = new Padding(4, 4, 8, 12),
            MinimumSize = new Size(420, 0)
        };

        box.Controls.Add(new Label
        {
            Tag = titleKey,
            Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
            ForeColor = _text,
            AutoSize = true,
            Margin = new Padding(3, 0, 3, 8)
        });

        return box;
    }

    private void AddNumericRow(FlowLayoutPanel parent, string labelKey, NumericUpDown control, string suffix)
    {
        var row = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(3, 3, 3, 3)
        };

        row.Controls.Add(RowLabel(labelKey, 210));
        row.Controls.Add(control);

        if (!string.IsNullOrEmpty(suffix))
        {
            row.Controls.Add(new Label
            {
                Text = suffix,
                ForeColor = _muted,
                AutoSize = true,
                Margin = new Padding(4, 7, 0, 0)
            });
        }

        parent.Controls.Add(row);
    }

    private Label RowLabel(string key, int width)
    {
        return new Label
        {
            Tag = key,
            Width = width,
            Height = 28,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = _text
        };
    }

    private static NumericUpDown Num(decimal min, decimal max, decimal increment)
    {
        var decimals = increment < 1m ? (increment < 0.1m ? 2 : 1) : 0;

        return new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Increment = increment,
            DecimalPlaces = decimals,
            Width = 105,
            TextAlign = HorizontalAlignment.Right,
            BackColor = Color.FromArgb(48, 53, 62),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private static CheckBox Check(string key)
    {
        return new CheckBox
        {
            Tag = key,
            AutoSize = true,
            MaximumSize = new Size(400, 0),
            ForeColor = Color.White,
            Margin = new Padding(3, 6, 3, 6)
        };
    }

    private Button MakeButton(string key, int width, Color? backColor = null)
    {
        return new Button
        {
            Tag = key,
            Width = width,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor ?? Color.FromArgb(52, 58, 68),
            ForeColor = Color.White,
            FlatAppearance = { BorderColor = Color.FromArgb(78, 86, 99), BorderSize = 1 },
            Margin = new Padding(6, 0, 0, 0)
        };
    }

    private void ApplyLanguage()
    {
        ApplyLanguageRecursive(this);
        RefreshQualityItems();
        RefreshStatus();
    }

    private void ApplyLanguageRecursive(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (control.Tag is string key && !string.IsNullOrWhiteSpace(key))
                control.Text = L.T(key);

            if (control.HasChildren)
                ApplyLanguageRecursive(control);
        }
    }

    private void RefreshQualityItems()
    {
        var selected = _quality.SelectedIndex;
        if (selected < 0)
            selected = 0;

        _quality.BeginUpdate();
        _quality.Items.Clear();
        _quality.Items.AddRange(new object[]
        {
            L.T("q_vanilla"),
            L.T("q_normal"),
            L.T("q_great"),
            L.T("q_epic"),
            L.T("q_pristine")
        });
        _quality.SelectedIndex = Math.Clamp(selected, 0, 4);
        _quality.EndUpdate();
    }

    private void ChangeLanguageFromCombo()
    {
        if (_changingLanguage || _languageBox.SelectedIndex < 0 || _languageBox.SelectedIndex >= L.Languages.Length)
            return;

        var code = L.Languages[_languageBox.SelectedIndex].Code;
        L.SetLanguage(code);
        SaveLanguagePreference(code);
        ApplyLanguage();
    }

    private void SelectLanguageInCombo()
    {
        _changingLanguage = true;

        var index = Array.FindIndex(
            L.Languages,
            x => x.Code.Equals(L.CurrentCode, StringComparison.OrdinalIgnoreCase));

        _languageBox.SelectedIndex = index >= 0 ? index : 0;
        _changingLanguage = false;
    }

    private string LoadInitialLanguage()
    {
        try
        {
            if (File.Exists(_languageFile))
            {
                var code = File.ReadAllText(_languageFile).Trim();
                if (L.Languages.Any(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
                    return code;
            }
        }
        catch
        {
        }

        var steamLanguage = DetectSteamGameLanguage();
        return !string.IsNullOrWhiteSpace(steamLanguage)
            ? steamLanguage
            : L.DetectLanguage();
    }

    private string DetectSteamGameLanguage()
    {
        try
        {
            var configDir = Path.GetDirectoryName(_configPath);
            var bepInExDir = configDir == null ? null : Directory.GetParent(configDir)?.FullName;
            var gameDir = bepInExDir == null ? null : Directory.GetParent(bepInExDir)?.FullName;
            var commonDir = gameDir == null ? null : Directory.GetParent(gameDir)?.FullName;
            var steamAppsDir = commonDir == null ? null : Directory.GetParent(commonDir)?.FullName;
            var manifest = steamAppsDir == null
                ? null
                : Path.Combine(steamAppsDir, "appmanifest_2185060.acf");

            if (manifest == null || !File.Exists(manifest))
                return string.Empty;

            foreach (var raw in File.ReadLines(manifest))
            {
                var line = raw.Trim();
                if (!line.StartsWith("\"language\"", StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = line.Split('"', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length < 2)
                    continue;

                return MapSteamLanguage(parts[^1]);
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    private static string MapSteamLanguage(string steamLanguage)
    {
        return steamLanguage.Trim().ToLowerInvariant() switch
        {
            "english" => "en",
            "french" => "fr",
            "italian" => "it",
            "german" => "de",
            "spanish" => "es-ES",
            "japanese" => "ja",
            "koreana" => "ko",
            "korean" => "ko",
            "brazilian" => "pt-BR",
            "schinese" => "zh-CN",
            "tchinese" => "zh-TW",
            "turkish" => "tr",
            "polish" => "pl",
            "russian" => "ru",
            "latam" => "es-419",
            "thai" => "th",
            _ => string.Empty
        };
    }

    private void SaveLanguagePreference(string code)
    {
        try
        {
            var dir = Path.GetDirectoryName(_languageFile);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(_languageFile, code);
        }
        catch
        {
            // Language persistence is optional; the trainer remains fully usable.
        }
    }

    private void LoadConfig()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                _loaded = false;
                MessageBox.Show(
                    this,
                    L.T("config_missing"),
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var cfg = IniConfig.Load(_configPath);

            _workshop.Value = ClampDecimal(cfg.GetFloat("Speed", "Workshop", 1), _workshop);
            _training.Value = ClampDecimal(cfg.GetFloat("Speed", "Training", 1), _training);
            _analysis.Value = ClampDecimal(cfg.GetFloat("Speed", "Analysis", 1), _analysis);
            _wildlife.Value = ClampDecimal(cfg.GetFloat("Speed", "Wildlife", 1), _wildlife);
            _expeditions.Value = ClampDecimal(cfg.GetFloat("Speed", "Expeditions", 1), _expeditions);
            _staffMovement.Value = ClampDecimal(cfg.GetFloat("Speed", "StaffMovement", 1), _staffMovement);

            _analysisMax.Checked = cfg.GetBool("Knowledge", "AnalysisMaxAfterOne", false);
            _wildlifeMax.Checked = cfg.GetBool("Knowledge", "WildlifeMaxAfterOne", false);

            _minimumRank.Value = ClampDecimal(cfg.GetInt("Applicants", "MinimumRank", 1), _minimumRank);
            _emptySkills.Checked = cfg.GetBool("Applicants", "EmptySkills", false);

            _maxSurvey.Checked = cfg.GetBool("Expeditions", "MaxSurveyAfterOne", false);
            _quality.SelectedIndex = Math.Clamp(cfg.GetInt("Expeditions", "Quality", -1) + 1, 0, 4);
            _forceQuality.Checked = cfg.GetBool("Expeditions", "ForceQuality", false);

            _securityRadius.Value = ClampDecimal(cfg.GetFloat("Security", "CoverageRadius", 0), _securityRadius);
            _firstAid.Checked = cfg.GetBool("Health", "FirstAidCuresExpeditionAilments", false);

            _loaded = true;
            _pathLabel.Text = _configPath;
            RefreshStatus();
        }
        catch (Exception ex)
        {
            _loaded = false;
            MessageBox.Show(this, ex.Message, L.T("load_error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveConfig()
    {
        if (!_loaded || !File.Exists(_configPath))
        {
            ChooseConfig();
            if (!_loaded)
                return;
        }

        try
        {
            var backup = _configPath + ".trainer.bak";
            if (!File.Exists(backup))
                File.Copy(_configPath, backup, false);

            var cfg = IniConfig.Load(_configPath);

            cfg.SetFloat("Speed", "Workshop", _workshop.Value);
            cfg.SetFloat("Speed", "Training", _training.Value);
            cfg.SetFloat("Speed", "Analysis", _analysis.Value);
            cfg.SetFloat("Speed", "Wildlife", _wildlife.Value);
            cfg.SetFloat("Speed", "Expeditions", _expeditions.Value);
            cfg.SetFloat("Speed", "StaffMovement", _staffMovement.Value);

            cfg.SetBool("Knowledge", "AnalysisMaxAfterOne", _analysisMax.Checked);
            cfg.SetBool("Knowledge", "WildlifeMaxAfterOne", _wildlifeMax.Checked);

            cfg.SetInt("Applicants", "MinimumRank", (int)_minimumRank.Value);
            cfg.SetBool("Applicants", "EmptySkills", _emptySkills.Checked);

            cfg.SetBool("Expeditions", "MaxSurveyAfterOne", _maxSurvey.Checked);
            cfg.SetInt("Expeditions", "Quality", _quality.SelectedIndex - 1);
            cfg.SetBool("Expeditions", "ForceQuality", _forceQuality.Checked);

            cfg.SetFloat("Security", "CoverageRadius", _securityRadius.Value);
            cfg.SetBool("Health", "FirstAidCuresExpeditionAilments", _firstAid.Checked);

            cfg.Save(_configPath);

            var running = IsGameRunning();
            _statusLabel.Text = running ? L.T("saved_live") : L.T("saved_stopped");
            _statusLabel.ForeColor = running ? Color.LightGreen : Color.Gold;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, L.T("save_error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetVanilla()
    {
        _workshop.Value = 1;
        _training.Value = 1;
        _analysis.Value = 1;
        _wildlife.Value = 1;
        _expeditions.Value = 1;
        _staffMovement.Value = 1;

        _analysisMax.Checked = false;
        _wildlifeMax.Checked = false;

        _minimumRank.Value = 1;
        _emptySkills.Checked = false;

        _maxSurvey.Checked = false;
        _quality.SelectedIndex = 0;
        _forceQuality.Checked = false;

        _securityRadius.Value = 0;
        _firstAid.Checked = false;

        try
        {
            if (File.Exists(_configPath))
            {
                var cfg = IniConfig.Load(_configPath);
                cfg.SetFloat("Expeditions", "SurveyXPMultiplier", 1);
                cfg.Save(_configPath);
            }
        }
        catch
        {
        }

        SaveConfig();
    }

    private void ChooseConfig()
    {
        using var dialog = new OpenFileDialog
        {
            Title = L.T("choose_config"),
            Filter = "TPM QoL Config (TPMQoL.cfg)|TPMQoL.cfg|Config (*.cfg)|*.cfg|All files (*.*)|*.*",
            CheckFileExists = true,
            FileName = "TPMQoL.cfg",
            InitialDirectory = File.Exists(_configPath)
                ? Path.GetDirectoryName(_configPath)
                : Path.GetDirectoryName(DefaultConfig)
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        _configPath = dialog.FileName;
        LoadConfig();
    }

    private void RefreshStatus()
    {
        var running = IsGameRunning();

        _statusLabel.Text = running ? L.T("game_running") : L.T("game_stopped");
        _statusLabel.ForeColor = running ? Color.LightGreen : Color.Gold;

        _pathLabel.Text = _configPath;

        var version = GetModVersion();
        _modLabel.Text = string.IsNullOrWhiteSpace(version)
            ? $"{L.T("mod")}: —"
            : $"{L.T("mod")}: v{version}";
    }

    private void StartGame()
    {
        try
        {
            if (IsGameRunning())
                return;

            Process.Start(new ProcessStartInfo("steam://rungameid/2185060") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, L.T("start_error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string DetectConfigPath()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var besideTrainer = Path.Combine(baseDir, "BepInEx", "config", "TPMQoL.cfg");
            if (File.Exists(besideTrainer))
                return besideTrainer;

            var parentDir = Directory.GetParent(baseDir)?.FullName;
            if (!string.IsNullOrWhiteSpace(parentDir))
            {
                var oneLevelUp = Path.Combine(parentDir, "BepInEx", "config", "TPMQoL.cfg");
                if (File.Exists(oneLevelUp))
                    return oneLevelUp;
            }

            var process = Process.GetProcessesByName("TPM").FirstOrDefault();
            var exe = process?.MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(exe))
            {
                var candidate = Path.Combine(Path.GetDirectoryName(exe)!, "BepInEx", "config", "TPMQoL.cfg");
                if (File.Exists(candidate))
                    return candidate;
            }
        }
        catch
        {
        }

        return DefaultConfig;
    }

    private string GetModVersion()
    {
        try
        {
            var configDir = Path.GetDirectoryName(_configPath);
            if (string.IsNullOrWhiteSpace(configDir))
                return string.Empty;

            var bepInEx = Directory.GetParent(configDir)?.FullName;
            if (string.IsNullOrWhiteSpace(bepInEx))
                return string.Empty;

            var dll = Path.Combine(bepInEx, "plugins", "TPMQoL", "TPMQoL.dll");
            return File.Exists(dll)
                ? AssemblyName.GetAssemblyName(dll).Version?.ToString() ?? string.Empty
                : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool IsGameRunning()
    {
        return Process.GetProcessesByName("TPM").Length > 0;
    }

    private static decimal ClampDecimal(float value, NumericUpDown control)
    {
        var decimalValue = (decimal)value;
        return Math.Min(control.Maximum, Math.Max(control.Minimum, decimalValue));
    }

    private static decimal ClampDecimal(int value, NumericUpDown control)
    {
        var decimalValue = (decimal)value;
        return Math.Min(control.Maximum, Math.Max(control.Minimum, decimalValue));
    }
}
