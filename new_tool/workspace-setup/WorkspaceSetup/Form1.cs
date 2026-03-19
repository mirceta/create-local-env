using System.Text.Json;
using WorkspaceSetup.Controls;
using WorkspaceSetup.Forms;
using WorkspaceSetup.Models;
using WorkspaceSetup.Services;

namespace WorkspaceSetup;

public partial class Form1 : Form
{
    private WorkspaceConfig _config = new();
    private string? _configPath;
    private CancellationTokenSource? _launchCts;
    private WorkspaceLauncher? _launcher;
    private int _selectedDesktopIndex;

    // UI controls
    private readonly MonitorPreviewPanel _preview;
    private readonly ListBox _lstApps;
    private readonly ListBox _lstMonitors;
    private readonly RichTextBox _txtLog;
    private readonly Button _btnLaunch;
    private readonly Button _btnStop;
    private readonly TabControl _tabs;
    private readonly RichTextBox _txtEditor;
    private readonly Label _lblEditorStatus;
    private readonly ComboBox _cboDesktop;
    private readonly Label _lblDesktopInfo;
    private readonly FlowLayoutPanel _variablesPanel;

    /// <summary>Config lives in AppData so it's the same for the user regardless of where the exe runs from.</summary>
    private static string AppDataDir
    {
        get
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WorkspaceSetup");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
    private static string ConfigFilePath => Path.Combine(AppDataDir, "workspace-config.json");
    private static string HistoryFilePath => Path.Combine(AppDataDir, "workspace-history.json");

    private DesktopConfig? SelectedDesktop =>
        _selectedDesktopIndex >= 0 && _selectedDesktopIndex < _config.Desktops.Count
            ? _config.Desktops[_selectedDesktopIndex] : null;

    private static List<string> LoadPathHistory()
    {
        try
        {
            if (File.Exists(HistoryFilePath))
            {
                var json = File.ReadAllText(HistoryFilePath);
                return JsonSerializer.Deserialize<List<string>>(json) ?? [];
            }
        }
        catch { }
        return [];
    }

    private static void SavePathHistory(List<string> paths)
    {
        try
        {
            var json = JsonSerializer.Serialize(paths, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(HistoryFilePath, json);
        }
        catch { }
    }

    public Form1()
    {
        InitializeComponent();

        Text = "Workspace Setup";
        Size = new Size(1400, 1000);
        MinimumSize = new Size(900, 600);
        BackColor = Color.FromArgb(28, 28, 30);
        ForeColor = Color.White;
        StartPosition = FormStartPosition.CenterScreen;

        // ── Menu strip ──
        var menu = new MenuStrip { BackColor = Color.FromArgb(40, 40, 45), ForeColor = Color.White };
        var fileMenu = new ToolStripMenuItem("File");
        fileMenu.DropDownItems.Add("New Config", null, (_, _) => NewConfig());
        fileMenu.DropDownItems.Add("Open Config...", null, (_, _) => OpenConfig());
        fileMenu.DropDownItems.Add("Save Config", null, (_, _) => SaveConfig());
        fileMenu.DropDownItems.Add("Save Config As...", null, (_, _) => SaveConfigAs());
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("Exit", null, (_, _) => Close());
        menu.Items.Add(fileMenu);

        var toolsMenu = new ToolStripMenuItem("Tools");
        toolsMenu.DropDownItems.Add("Detect My Monitors", null, (_, _) => DetectMonitors());
        toolsMenu.DropDownItems.Add("Install Login Task...", null, (_, _) => InstallLoginTask());
        toolsMenu.DropDownItems.Add("Remove Login Task", null, (_, _) => RemoveLoginTask());
        menu.Items.Add(toolsMenu);
        MainMenuStrip = menu;
        Controls.Add(menu);

        // ══════════════════════════════════════════════
        // TAB CONTROL — Workspace + Config Editor
        // ══════════════════════════════════════════════
        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10f),
        };
        _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabs.DrawItem += TabControl_DrawItem;

        // ── Tab 1: Workspace ──
        var workspaceTab = new TabPage("  Workspace  ")
        {
            BackColor = Color.FromArgb(28, 28, 30),
            ForeColor = Color.White,
        };

        // ── Desktop selector bar (above preview) ──
        var desktopBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(8, 6, 8, 6),
            BackColor = Color.FromArgb(35, 35, 40),
        };

        var desktopBarFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
        };

        desktopBarFlow.Controls.Add(new Label
        {
            Text = "Virtual Desktop:",
            ForeColor = Color.FromArgb(180, 180, 185),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            AutoSize = true,
            Padding = new Padding(0, 6, 4, 0),
        });

        _cboDesktop = new ComboBox
        {
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(50, 50, 55),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f),
        };
        _cboDesktop.SelectedIndexChanged += (_, _) =>
        {
            _selectedDesktopIndex = _cboDesktop.SelectedIndex;
            RefreshAppList();
        };
        desktopBarFlow.Controls.Add(_cboDesktop);

        desktopBarFlow.Controls.Add(MakeSmallButton("Add Desktop", (_, _) => AddDesktop()));
        desktopBarFlow.Controls.Add(MakeSmallButton("Rename", (_, _) => RenameDesktop()));
        desktopBarFlow.Controls.Add(MakeSmallButton("Remove", (_, _) => RemoveDesktop()));

        _lblDesktopInfo = new Label
        {
            Text = "",
            ForeColor = Color.FromArgb(120, 120, 125),
            Font = new Font("Segoe UI", 8.5f),
            AutoSize = true,
            Padding = new Padding(8, 7, 0, 0),
        };
        desktopBarFlow.Controls.Add(_lblDesktopInfo);

        desktopBar.Controls.Add(desktopBarFlow);

        // ── Top: monitor preview ──
        _preview = new MonitorPreviewPanel
        {
            Dock = DockStyle.Top,
            Height = 200,
        };

        // ── Left: monitors list ──
        var monPanel = new Panel { Dock = DockStyle.Left, Width = 220, Padding = new Padding(8) };

        _lstMonitors = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(40, 40, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5f),
        };
        _lstMonitors.DoubleClick += (_, _) => EditMonitor();

        var monBtnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            FlowDirection = FlowDirection.LeftToRight,
        };
        monBtnPanel.Controls.Add(MakeSmallButton("Add", (_, _) => AddMonitor()));
        monBtnPanel.Controls.Add(MakeSmallButton("Edit", (_, _) => EditMonitor()));
        monBtnPanel.Controls.Add(MakeSmallButton("Remove", (_, _) => RemoveMonitor()));
        monBtnPanel.Controls.Add(MakeSmallButton("Detect", (_, _) => DetectMonitors()));

        monPanel.Controls.Add(_lstMonitors);
        monPanel.Controls.Add(monBtnPanel);
        monPanel.Controls.Add(new Label
        {
            Text = "MONITORS",
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.FromArgb(150, 150, 155),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
        });

        // ── Center: apps list ──
        var appPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

        _lstApps = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(40, 40, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5f),
        };
        _lstApps.SelectedIndexChanged += (_, _) => _preview.SetSelectedApp(_lstApps.SelectedIndex);
        _lstApps.DoubleClick += (_, _) => EditApp();

        var appBtnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            FlowDirection = FlowDirection.LeftToRight,
        };
        appBtnPanel.Controls.Add(MakeSmallButton("Add", (_, _) => AddApp()));
        appBtnPanel.Controls.Add(MakeSmallButton("Edit", (_, _) => EditApp()));
        appBtnPanel.Controls.Add(MakeSmallButton("Remove", (_, _) => RemoveApp()));
        appBtnPanel.Controls.Add(MakeSmallButton("Move Up", (_, _) => MoveApp(-1)));
        appBtnPanel.Controls.Add(MakeSmallButton("Move Down", (_, _) => MoveApp(1)));

        appPanel.Controls.Add(_lstApps);
        appPanel.Controls.Add(appBtnPanel);
        appPanel.Controls.Add(new Label
        {
            Text = "APPLICATIONS (for selected desktop)",
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.FromArgb(150, 150, 155),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
        });

        // ── Bottom: log + launch ──
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 350,
            Padding = new Padding(8),
        };

        _txtLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(20, 20, 22),
            ForeColor = Color.FromArgb(180, 220, 180),
            Font = new Font("Cascadia Mono", 9f),
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
        };
        // ── Variables panel (editable before launch) ──
        _variablesPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 4),
        };

        var launchPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 4, 0, 4),
        };
        _btnLaunch = new Button
        {
            Text = "Launch Desktop",
            Width = 160,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 140, 70),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
        };
        _btnLaunch.Click += BtnLaunch_Click;

        _btnStop = new Button
        {
            Text = "Stop",
            Width = 80,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(180, 50, 50),
            ForeColor = Color.White,
            Enabled = false,
        };
        _btnStop.Click += (_, _) => { _launchCts?.Cancel(); };

        var btnBringToFront = new Button
        {
            Text = "Bring to Front",
            Width = 110,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(55, 90, 170),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        };
        btnBringToFront.Click += BtnBringToFront_Click;

        var btnVerify = new Button
        {
            Text = "Verify",
            Width = 80,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(140, 120, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        };
        btnVerify.Click += BtnVerify_Click;

        var btnReposition = new Button
        {
            Text = "Reposition",
            Width = 100,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(160, 90, 30),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        };
        btnReposition.Click += BtnReposition_Click;

        launchPanel.Controls.Add(_btnLaunch);
        launchPanel.Controls.Add(btnBringToFront);
        launchPanel.Controls.Add(btnVerify);
        launchPanel.Controls.Add(btnReposition);
        launchPanel.Controls.Add(_btnStop);

        bottomPanel.Controls.Add(_txtLog);
        bottomPanel.Controls.Add(_variablesPanel);
        bottomPanel.Controls.Add(launchPanel);
        bottomPanel.Controls.Add(new Label
        {
            Text = "LOG",
            Dock = DockStyle.Top,
            Height = 20,
            ForeColor = Color.FromArgb(150, 150, 155),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
        });

        // Assemble workspace tab (reverse dock order)
        workspaceTab.Controls.Add(appPanel);    // Fill
        workspaceTab.Controls.Add(monPanel);    // Left
        workspaceTab.Controls.Add(_preview);    // Top (below desktop bar)
        workspaceTab.Controls.Add(desktopBar);  // Top (topmost)
        workspaceTab.Controls.Add(bottomPanel); // Bottom

        // ── Tab 2: Config Editor ──
        var editorTab = new TabPage("  Config Editor  ")
        {
            BackColor = Color.FromArgb(28, 28, 30),
            ForeColor = Color.White,
            Padding = new Padding(8),
        };

        var editorToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 6, 0, 6),
        };

        var btnApply = new Button
        {
            Text = "Apply && Reload",
            Width = 140, Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(55, 90, 170),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
        };
        btnApply.Click += BtnApplyConfig_Click;

        var btnSaveEditor = new Button
        {
            Text = "Save to File",
            Width = 110, Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 140, 70),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
        };
        btnSaveEditor.Click += BtnSaveEditor_Click;

        var btnFormatJson = new Button
        {
            Text = "Format JSON",
            Width = 110, Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(55, 55, 60),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f),
        };
        btnFormatJson.Click += BtnFormatJson_Click;

        var btnRevert = new Button
        {
            Text = "Revert",
            Width = 80, Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(55, 55, 60),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f),
        };
        btnRevert.Click += (_, _) => SyncConfigToEditor();

        _lblEditorStatus = new Label
        {
            Text = "",
            AutoSize = true,
            ForeColor = Color.FromArgb(180, 220, 180),
            Font = new Font("Segoe UI", 9f),
            Padding = new Padding(12, 8, 0, 0),
        };

        editorToolbar.Controls.Add(btnApply);
        editorToolbar.Controls.Add(btnSaveEditor);
        editorToolbar.Controls.Add(btnFormatJson);
        editorToolbar.Controls.Add(btnRevert);
        editorToolbar.Controls.Add(_lblEditorStatus);

        _txtEditor = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(20, 20, 22),
            ForeColor = Color.FromArgb(212, 212, 212),
            Font = new Font("Cascadia Mono", 10.5f),
            BorderStyle = BorderStyle.None,
            AcceptsTab = true,
            WordWrap = false,
            DetectUrls = false,
        };
        _txtEditor.TextChanged += (_, _) => EditorTextChanged();

        editorTab.Controls.Add(_txtEditor);
        editorTab.Controls.Add(editorToolbar);

        // Assemble tabs
        _tabs.TabPages.Add(workspaceTab);
        _tabs.TabPages.Add(editorTab);
        _tabs.SelectedIndexChanged += Tabs_SelectedIndexChanged;
        Controls.Add(_tabs);

        // Handle --auto-launch command line arg
        var args = Environment.GetCommandLineArgs();
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--auto-launch" && i + 1 < args.Length)
            {
                LoadConfig(args[i + 1]);
                _ = AutoLaunchAsync();
                return;
            }
        }

        // Load config from AppData; if missing, try to migrate from next to exe
        var appDataConfig = ConfigFilePath;
        if (!File.Exists(appDataConfig))
        {
            // Look for config next to exe or up the directory tree (dev scenario)
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string? legacyPath = null;
            var dir = exeDir;
            for (int i = 0; i < 6 && dir != null; i++)
            {
                var candidate = Path.Combine(dir, "workspace-config.json");
                if (File.Exists(candidate)) { legacyPath = candidate; break; }
                dir = Path.GetDirectoryName(dir);
            }

            if (legacyPath != null)
            {
                File.Copy(legacyPath, appDataConfig);
                Log($"Migrated config from {legacyPath} to {appDataConfig}");
            }
        }

        if (File.Exists(appDataConfig))
            LoadConfig(appDataConfig);
        else
            RefreshUI();
    }

    // ── Tab drawing (dark theme) ──

    private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
    {
        var tab = _tabs.TabPages[e.Index];
        var bounds = _tabs.GetTabRect(e.Index);
        bool selected = _tabs.SelectedIndex == e.Index;

        var bgColor = selected ? Color.FromArgb(45, 45, 50) : Color.FromArgb(32, 32, 36);
        var fgColor = selected ? Color.White : Color.FromArgb(160, 160, 165);

        using var bgBrush = new SolidBrush(bgColor);
        using var fgBrush = new SolidBrush(fgColor);

        e.Graphics!.FillRectangle(bgBrush, bounds);
        var textSize = e.Graphics.MeasureString(tab.Text, _tabs.Font);
        var textX = bounds.X + (bounds.Width - textSize.Width) / 2;
        var textY = bounds.Y + (bounds.Height - textSize.Height) / 2;
        e.Graphics.DrawString(tab.Text, _tabs.Font, fgBrush, textX, textY);

        if (selected)
        {
            using var accentPen = new Pen(Color.FromArgb(70, 130, 230), 3);
            e.Graphics.DrawLine(accentPen, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
        }
    }

    private void Tabs_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_tabs.SelectedIndex == 1) SyncConfigToEditor();
    }

    // ── Desktop management ──

    private void RefreshDesktopCombo()
    {
        _cboDesktop.Items.Clear();
        for (int i = 0; i < _config.Desktops.Count; i++)
        {
            var d = _config.Desktops[i];
            _cboDesktop.Items.Add($"Desktop {i + 1}: {d.Name} ({d.Apps.Count} apps)");
        }

        if (_selectedDesktopIndex >= _config.Desktops.Count)
            _selectedDesktopIndex = Math.Max(0, _config.Desktops.Count - 1);

        if (_cboDesktop.Items.Count > 0)
            _cboDesktop.SelectedIndex = _selectedDesktopIndex;

        _lblDesktopInfo.Text = _config.Desktops.Count > 1
            ? $"Ctrl+Win+Arrow to switch  |  {_config.Desktops.Count} desktops configured"
            : "Add more desktops for multi-workspace setup";
    }

    private void AddDesktop()
    {
        var name = PromptText("Add Desktop", "Desktop name:", $"Desktop {_config.Desktops.Count + 1}");
        if (name == null) return;
        _config.Desktops.Add(new DesktopConfig { Name = name });
        _selectedDesktopIndex = _config.Desktops.Count - 1;
        RefreshUI();
    }

    private void RenameDesktop()
    {
        if (SelectedDesktop == null) return;
        var name = PromptText("Rename Desktop", "New name:", SelectedDesktop.Name);
        if (name == null) return;
        SelectedDesktop.Name = name;
        RefreshDesktopCombo();
    }

    private void RemoveDesktop()
    {
        if (_config.Desktops.Count <= 1)
        {
            MessageBox.Show("You need at least one desktop.", "Cannot Remove", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (SelectedDesktop == null) return;

        var result = MessageBox.Show(
            $"Remove desktop \"{SelectedDesktop.Name}\" and its {SelectedDesktop.Apps.Count} apps?",
            "Remove Desktop", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        _config.Desktops.RemoveAt(_selectedDesktopIndex);
        _selectedDesktopIndex = Math.Min(_selectedDesktopIndex, _config.Desktops.Count - 1);
        RefreshUI();
    }

    private static string? PromptText(string title, string label, string defaultValue)
    {
        using var dlg = new Form
        {
            Text = title,
            Size = new Size(360, 160),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            StartPosition = FormStartPosition.CenterParent,
            BackColor = Color.FromArgb(32, 32, 32),
            ForeColor = Color.White,
        };
        var lbl = new Label { Text = label, Left = 16, Top = 16, AutoSize = true, ForeColor = Color.White };
        var txt = new TextBox
        {
            Text = defaultValue, Left = 16, Top = 40, Width = 310,
            BackColor = Color.FromArgb(50, 50, 55), ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
        };
        var btnOk = new Button
        {
            Text = "OK", DialogResult = DialogResult.OK,
            Left = 160, Top = 78, Width = 80, Height = 30,
            FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(55, 90, 170), ForeColor = Color.White,
        };
        var btnCancel = new Button
        {
            Text = "Cancel", DialogResult = DialogResult.Cancel,
            Left = 246, Top = 78, Width = 80, Height = 30,
            FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 65), ForeColor = Color.White,
        };
        dlg.Controls.AddRange([lbl, txt, btnOk, btnCancel]);
        dlg.AcceptButton = btnOk;
        dlg.CancelButton = btnCancel;
        return dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text) ? txt.Text.Trim() : null;
    }

    // ── Config Editor ──

    private bool _editorDirty;

    private void SyncConfigToEditor()
    {
        var opts = new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
        _txtEditor.Text = JsonSerializer.Serialize(_config, opts);
        _editorDirty = false;
        _lblEditorStatus.Text = "";
        _lblEditorStatus.ForeColor = Color.FromArgb(180, 220, 180);
    }

    private void EditorTextChanged()
    {
        _editorDirty = true;
        try
        {
            JsonSerializer.Deserialize<WorkspaceConfig>(_txtEditor.Text,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            _lblEditorStatus.Text = "Valid JSON";
            _lblEditorStatus.ForeColor = Color.FromArgb(100, 200, 100);
        }
        catch (JsonException ex)
        {
            _lblEditorStatus.Text = $"JSON error: {ex.Message[..Math.Min(ex.Message.Length, 80)]}";
            _lblEditorStatus.ForeColor = Color.FromArgb(240, 80, 80);
        }
    }

    private void BtnApplyConfig_Click(object? sender, EventArgs e)
    {
        try
        {
            var newConfig = JsonSerializer.Deserialize<WorkspaceConfig>(_txtEditor.Text,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (newConfig == null)
            {
                MessageBox.Show("Config parsed as null.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            _config = newConfig;
            _editorDirty = false;
            _selectedDesktopIndex = 0;
            RefreshUI();
            _lblEditorStatus.Text = "Applied!";
            _lblEditorStatus.ForeColor = Color.FromArgb(100, 200, 100);
            Log("Config applied from editor.");
            _tabs.SelectedIndex = 0;
        }
        catch (JsonException ex)
        {
            MessageBox.Show($"Invalid JSON:\n{ex.Message}", "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnSaveEditor_Click(object? sender, EventArgs e)
    {
        BtnApplyConfig_Click(sender, e);
        if (_editorDirty) return;
        SaveConfig();
    }

    private void BtnFormatJson_Click(object? sender, EventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(_txtEditor.Text);
            _txtEditor.Text = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
            _lblEditorStatus.Text = "Formatted";
            _lblEditorStatus.ForeColor = Color.FromArgb(100, 200, 100);
        }
        catch (JsonException ex)
        {
            MessageBox.Show($"Cannot format — invalid JSON:\n{ex.Message}", "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task AutoLaunchAsync()
    {
        await Task.Delay(500);
        BtnLaunch_Click(this, EventArgs.Empty);
    }

    // ── Config IO ──

    private void NewConfig()
    {
        _config = new WorkspaceConfig();
        _configPath = null;
        _selectedDesktopIndex = 0;
        RefreshUI();
        Log("New config created.");
    }

    private void OpenConfig()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            Title = "Open Workspace Config"
        };
        if (dlg.ShowDialog() == DialogResult.OK) LoadConfig(dlg.FileName);
    }

    private void LoadConfig(string path)
    {
        try
        {
            _config = WorkspaceConfig.Load(path);
            _configPath = path;
            _selectedDesktopIndex = 0;
            RefreshUI();
            Log($"Loaded: {path}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load config:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveConfig()
    {
        if (_configPath == null) _configPath = ConfigFilePath;
        _config.Save(_configPath);
        Log($"Saved: {_configPath}");
    }

    private void SaveConfigAs()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            Title = "Save Workspace Config",
            FileName = "workspace-config.json"
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _configPath = dlg.FileName;
            _config.Save(_configPath);
            Log($"Saved: {_configPath}");
        }
    }

    // ── UI Refresh ──

    private void RefreshUI()
    {
        RefreshDesktopCombo();
        RefreshMonitorList();
        RefreshAppList();
        RefreshVariablesPanel();
        UpdateTitle();
    }

    private void RefreshVariablesPanel()
    {
        _variablesPanel.Controls.Clear();

        if (_config.Variables.Count == 0) return;

        var history = LoadPathHistory();

        foreach (var (key, defaultValue) in _config.Variables)
        {
            var row = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Height = 30,
                AutoSize = true,
            };

            var lbl = new Label
            {
                Text = $"{{{{{key}}}}}:",
                ForeColor = Color.FromArgb(180, 180, 185),
                Font = new Font("Cascadia Mono", 9f),
                AutoSize = true,
                Padding = new Padding(0, 5, 4, 0),
            };

            var cbo = new ComboBox
            {
                Width = 500,
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                DropDownStyle = ComboBoxStyle.DropDown, // editable
                Tag = key,
            };

            // Populate with history + current default
            var allPaths = new List<string>(history);
            if (!string.IsNullOrEmpty(defaultValue) && !allPaths.Contains(defaultValue))
                allPaths.Insert(0, defaultValue);
            foreach (var p in allPaths)
                cbo.Items.Add(p);

            cbo.Text = defaultValue;
            cbo.TextChanged += (_, _) => _config.Variables[key] = cbo.Text;

            var btnBrowse = new Button
            {
                Text = "...",
                Width = 32,
                Height = 24,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(55, 55, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f),
            };
            btnBrowse.Click += (_, _) =>
            {
                using var dlg = new FolderBrowserDialog { SelectedPath = cbo.Text };
                if (dlg.ShowDialog() == DialogResult.OK)
                    cbo.Text = dlg.SelectedPath;
            };

            row.Controls.Add(lbl);
            row.Controls.Add(cbo);
            row.Controls.Add(btnBrowse);
            _variablesPanel.Controls.Add(row);
        }
    }

    private void RefreshMonitorList()
    {
        _lstMonitors.Items.Clear();
        foreach (var (name, mon) in _config.Monitors)
            _lstMonitors.Items.Add($"{name}: {mon}");
    }

    private void RefreshAppList()
    {
        _lstApps.Items.Clear();
        var desktop = SelectedDesktop;
        if (desktop != null)
        {
            foreach (var app in desktop.Apps)
                _lstApps.Items.Add($"{app.Name}  [{app.Monitor}]  ({app.X},{app.Y}) {app.Width}x{app.Height}");
        }

        // Update preview to show this desktop's apps
        _preview.SetConfig(_config, _selectedDesktopIndex);
    }

    private void UpdateTitle()
    {
        Text = _configPath != null
            ? $"Workspace Setup - {Path.GetFileName(_configPath)}"
            : "Workspace Setup - (unsaved)";
    }

    // ── Monitor management ──

    private void DetectMonitors()
    {
        var screens = Screen.AllScreens.OrderBy(s => s.Bounds.X).ToArray();
        _config.Monitors.Clear();

        for (int i = 0; i < screens.Length; i++)
        {
            var s = screens[i];
            var name = screens.Length == 1 ? "main"
                : i == 0 ? "left"
                : i == screens.Length - 1 ? "right"
                : $"center{(screens.Length > 3 ? (i).ToString() : "")}";

            _config.Monitors[name] = new MonitorInfo
            {
                OffsetX = s.Bounds.X,
                OffsetY = s.Bounds.Y,
                Width = s.Bounds.Width,
                Height = s.Bounds.Height,
            };
            Log($"Detected {name}: {s.Bounds.Width}x{s.Bounds.Height} at ({s.Bounds.X},{s.Bounds.Y})");
        }
        RefreshUI();
    }

    private void AddMonitor()
    {
        using var dlg = new MonitorEditorDialog();
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _config.Monitors[dlg.MonitorName] = dlg.ResultMonitor;
            RefreshUI();
        }
    }

    private void EditMonitor()
    {
        if (_lstMonitors.SelectedIndex < 0) return;
        var name = _config.Monitors.Keys.ElementAt(_lstMonitors.SelectedIndex);
        var mon = _config.Monitors[name];
        using var dlg = new MonitorEditorDialog(name, mon);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _config.Monitors[name] = dlg.ResultMonitor;
            RefreshUI();
        }
    }

    private void RemoveMonitor()
    {
        if (_lstMonitors.SelectedIndex < 0) return;
        var name = _config.Monitors.Keys.ElementAt(_lstMonitors.SelectedIndex);
        _config.Monitors.Remove(name);
        RefreshUI();
    }

    // ── App management (scoped to selected desktop) ──

    private void AddApp()
    {
        if (SelectedDesktop == null) return;
        using var dlg = new AppEditorDialog(null, _config.Monitors.Keys);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            SelectedDesktop.Apps.Add(dlg.Result);
            RefreshUI();
        }
    }

    private void EditApp()
    {
        if (SelectedDesktop == null || _lstApps.SelectedIndex < 0) return;
        var app = SelectedDesktop.Apps[_lstApps.SelectedIndex];
        using var dlg = new AppEditorDialog(app, _config.Monitors.Keys);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            SelectedDesktop.Apps[_lstApps.SelectedIndex] = dlg.Result;
            RefreshUI();
        }
    }

    private void RemoveApp()
    {
        if (SelectedDesktop == null || _lstApps.SelectedIndex < 0) return;
        SelectedDesktop.Apps.RemoveAt(_lstApps.SelectedIndex);
        RefreshUI();
    }

    private void MoveApp(int direction)
    {
        if (SelectedDesktop == null) return;
        int idx = _lstApps.SelectedIndex;
        int newIdx = idx + direction;
        if (idx < 0 || newIdx < 0 || newIdx >= SelectedDesktop.Apps.Count) return;

        (SelectedDesktop.Apps[idx], SelectedDesktop.Apps[newIdx]) = (SelectedDesktop.Apps[newIdx], SelectedDesktop.Apps[idx]);
        RefreshAppList();
        RefreshDesktopCombo();
        _lstApps.SelectedIndex = newIdx;
    }

    // ── Launch ──

    private async void BtnLaunch_Click(object? sender, EventArgs e)
    {
        if (SelectedDesktop == null || SelectedDesktop.Apps.Count == 0)
        {
            MessageBox.Show("No apps configured on the selected desktop.", "Nothing to launch", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _btnLaunch.Enabled = false;
        _btnStop.Enabled = true;
        _txtLog.Clear();
        _launchCts = new CancellationTokenSource();

        // Save any variable values to history for next time
        foreach (var (key, value) in _config.Variables)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                var history = LoadPathHistory();
                if (!history.Contains(value))
                {
                    history.Insert(0, value);
                    if (history.Count > 20) history.RemoveAt(history.Count - 1);
                    SavePathHistory(history);
                }
            }
        }

        _launcher = new WorkspaceLauncher();
        _launcher.Progress += (_, args) =>
        {
            BeginInvoke(() => Log(args.IsError
                ? $"[ERROR] [{args.AppName}] {args.Message}"
                : $"[{args.AppName}] {args.Message}"));
        };

        try
        {
            await _launcher.LaunchDesktopAsync(SelectedDesktop, _config.Monitors, _config.Variables, _launchCts.Token);
        }
        catch (OperationCanceledException) { Log("Launch cancelled."); }
        catch (Exception ex) { Log($"[ERROR] {ex.Message}"); }
        finally
        {
            _btnLaunch.Enabled = true;
            _btnStop.Enabled = false;
        }
    }

    private void BtnBringToFront_Click(object? sender, EventArgs e)
    {
        if (SelectedDesktop == null || SelectedDesktop.Apps.Count == 0)
        {
            Log("No apps to bring to front.");
            return;
        }

        int total = 0;
        foreach (var app in SelectedDesktop.Apps)
        {
            var procName = Path.GetFileNameWithoutExtension(app.Executable);
            int count = Win32Helper.BringAllToFrontByProcessName(procName);
            if (count > 0)
                Log($"[{app.Name}] Brought {count} window(s) to front.");
            total += count;
        }

        if (total == 0)
            Log("No running windows found for this desktop's apps.");
    }

    private void BtnVerify_Click(object? sender, EventArgs e)
    {
        if (_launcher == null || _launcher.TrackedWindows.Count == 0)
        {
            Log("Nothing to verify — launch a desktop first.");
            return;
        }

        Log("── Verifying window positions ──");
        var problems = _launcher.VerifyAll();

        foreach (var tw in _launcher.TrackedWindows)
        {
            var rect = tw.Found ? Win32Helper.GetWindowPosition(tw.Handle) : null;
            var problem = problems.Find(p => p.Window == tw);

            if (problem.Problem != null)
            {
                Log($"[ERROR] [{tw.App.Name}] {problem.Problem}");
            }
            else if (rect != null)
            {
                var r = rect.Value;
                Log($"[{tw.App.Name}] OK at ({r.Left},{r.Top}) {r.Width}x{r.Height}");
            }
        }

        if (problems.Count == 0)
            Log($"All {_launcher.TrackedWindows.Count} window(s) are correctly positioned.");
        else
            Log($"{problems.Count} problem(s) found. Use 'Reposition' to fix.");
    }

    private void BtnReposition_Click(object? sender, EventArgs e)
    {
        if (_launcher == null || _launcher.TrackedWindows.Count == 0)
        {
            Log("Nothing to reposition — launch a desktop first.");
            return;
        }

        Log("── Repositioning all windows ──");
        int count = _launcher.RepositionAll();
        Log($"Repositioned {count}/{_launcher.TrackedWindows.Count} window(s).");

        // Auto-verify after reposition
        var problems = _launcher.VerifyAll();
        if (problems.Count > 0)
        {
            foreach (var (tw, problem) in problems)
                Log($"[ERROR] [{tw.App.Name}] Still wrong: {problem}");
        }
        else
        {
            Log("All windows now correctly positioned.");
        }
    }

    // ── Scheduled task ──

    private void InstallLoginTask()
    {
        if (_configPath == null)
        {
            MessageBox.Show("Save your config first.", "No config", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var exePath = Application.ExecutablePath;
        var result = MessageBox.Show(
            $"This will create a Windows Scheduled Task to run this app at login.\n\nApp: {exePath}\nConfig: {_configPath}\n\nContinue?",
            "Install Login Task", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/Create /TN \"WorkspaceSetup\" /TR \"\\\"{exePath}\\\" --auto-launch \\\"{_configPath}\\\"\" /SC ONLOGON /DELAY 0000:15 /F",
                UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
            };
            var proc = System.Diagnostics.Process.Start(psi)!;
            proc.WaitForExit(10000);
            var output = proc.StandardOutput.ReadToEnd() + proc.StandardError.ReadToEnd();
            Log($"schtasks: {output.Trim()}");
            if (proc.ExitCode == 0)
                MessageBox.Show("Login task installed.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show($"Failed:\n{output}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void RemoveLoginTask()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = "/Delete /TN \"WorkspaceSetup\" /F",
                UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
            };
            var proc = System.Diagnostics.Process.Start(psi)!;
            proc.WaitForExit(10000);
            Log("Login task removed.");
            MessageBox.Show("Login task removed.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    // ── Helpers ──

    private void Log(string msg)
    {
        _txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
        _txtLog.ScrollToCaret();
    }

    private static Button MakeSmallButton(string text, EventHandler onClick)
    {
        var btn = new Button
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(4, 0, 4, 0),
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(55, 55, 60),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5f),
        };
        btn.Click += onClick;
        return btn;
    }
}
