namespace PromptManager;

public partial class Form1 : Form
{
    private AppState _state = new();

    private ListBox lstTasks = null!;
    private TextBox txtTaskName = null!;
    private TextBox txtRepoPath = null!;
    private ListBox lstPrompts = null!;
    private ListBox lstAgents = null!;
    private Panel pnlDetail = null!;
    private Panel pnlPrompt = null!;
    private TextBox txtPromptText = null!;
    private ComboBox cboAgent = null!;
    private Label lblSentStatus = null!;

    public Form1()
    {
        InitializeComponent();
        BuildLayout();
        _state = Persistence.Load();
        RefreshTaskList();
    }

    private void BuildLayout()
    {
        Text = "Prompt Manager";
        Size = new Size(1200, 700);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 500);

        var outerSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 200,
            FixedPanel = FixedPanel.Panel1
        };

        var innerSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 340,
            FixedPanel = FixedPanel.Panel1
        };

        outerSplit.Panel1.Controls.Add(BuildTaskPanel());
        innerSplit.Panel1.Controls.Add(BuildDetailPanel());
        innerSplit.Panel2.Controls.Add(BuildPromptPanel());
        outerSplit.Panel2.Controls.Add(innerSplit);

        Controls.Add(outerSplit);
    }

    private Control BuildTaskPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

        var lbl = new Label
        {
            Text = "Tasks",
            Dock = DockStyle.Top,
            Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
            Height = 25
        };

        lstTasks = new ListBox { Dock = DockStyle.Fill };
        lstTasks.SelectedIndexChanged += LstTasks_SelectedIndexChanged;

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 35, FlowDirection = FlowDirection.LeftToRight };
        var btnAdd = new Button { Text = "+ Task", Width = 85 };
        var btnRemove = new Button { Text = "- Task", Width = 85 };
        btnAdd.Click += BtnAddTask_Click;
        btnRemove.Click += BtnRemoveTask_Click;
        buttons.Controls.AddRange(new Control[] { btnAdd, btnRemove });

        panel.Controls.Add(lstTasks);
        panel.Controls.Add(buttons);
        panel.Controls.Add(lbl);
        return panel;
    }

    private Control BuildDetailPanel()
    {
        pnlDetail = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8), Visible = false };

        var lblName = new Label { Text = "Task Name:", Dock = DockStyle.Top, Height = 20 };
        txtTaskName = new TextBox { Dock = DockStyle.Top };
        txtTaskName.Leave += TxtTaskName_Leave;

        var lblRepo = new Label { Text = "Repo Path:", Dock = DockStyle.Top, Height = 20 };
        var pnlRepo = new Panel { Dock = DockStyle.Top, Height = 28 };
        txtRepoPath = new TextBox { Dock = DockStyle.Fill };
        txtRepoPath.Leave += TxtRepoPath_Leave;
        var btnBrowse = new Button { Text = "...", Dock = DockStyle.Right, Width = 35 };
        btnBrowse.Click += BtnBrowseRepo_Click;
        pnlRepo.Controls.Add(txtRepoPath);
        pnlRepo.Controls.Add(btnBrowse);

        var lblPrompts = new Label { Text = "Prompts:", Dock = DockStyle.Top, Height = 20, Font = new Font(Font.FontFamily, 9, FontStyle.Bold) };

        lstPrompts = new ListBox { Dock = DockStyle.Fill };
        lstPrompts.SelectedIndexChanged += LstPrompts_SelectedIndexChanged;

        var promptButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 35, FlowDirection = FlowDirection.LeftToRight };
        var btnAddPrompt = new Button { Text = "+ Prompt", Width = 85 };
        var btnRemovePrompt = new Button { Text = "- Prompt", Width = 85 };
        btnAddPrompt.Click += BtnAddPrompt_Click;
        btnRemovePrompt.Click += BtnRemovePrompt_Click;
        promptButtons.Controls.AddRange(new Control[] { btnAddPrompt, btnRemovePrompt });

        var lblAgents = new Label { Text = "Agents:", Dock = DockStyle.Bottom, Height = 20, Font = new Font(Font.FontFamily, 9, FontStyle.Bold) };
        lstAgents = new ListBox { Dock = DockStyle.Bottom, Height = 100 };

        var agentButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 35, FlowDirection = FlowDirection.LeftToRight };
        var btnAddAgent = new Button { Text = "+ Agent", Width = 85 };
        var btnRemoveAgent = new Button { Text = "- Agent", Width = 85 };
        btnAddAgent.Click += BtnAddAgent_Click;
        btnRemoveAgent.Click += BtnRemoveAgent_Click;
        agentButtons.Controls.AddRange(new Control[] { btnAddAgent, btnRemoveAgent });

        // Add in reverse dock order: bottom items first, then fill, then top
        pnlDetail.Controls.Add(lstPrompts);        // Fill
        pnlDetail.Controls.Add(promptButtons);      // Bottom
        pnlDetail.Controls.Add(agentButtons);       // Bottom
        pnlDetail.Controls.Add(lstAgents);           // Bottom
        pnlDetail.Controls.Add(lblAgents);           // Bottom
        pnlDetail.Controls.Add(lblPrompts);          // Top
        pnlDetail.Controls.Add(pnlRepo);             // Top
        pnlDetail.Controls.Add(lblRepo);             // Top
        pnlDetail.Controls.Add(txtTaskName);          // Top
        pnlDetail.Controls.Add(lblName);              // Top

        return pnlDetail;
    }

    private Control BuildPromptPanel()
    {
        pnlPrompt = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8), Visible = false };

        var lblText = new Label
        {
            Text = "Prompt Text:",
            Dock = DockStyle.Top,
            Height = 20,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };

        txtPromptText = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 10)
        };
        txtPromptText.Leave += TxtPromptText_Leave;

        // Bottom action bar using TableLayoutPanel for clean scaling
        var actionBar = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 70,
            ColumnCount = 4,
            RowCount = 2,
            AutoSize = false
        };
        actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var btnCopy = new Button { Text = "Copy to Clipboard", AutoSize = true, Margin = new Padding(0, 3, 6, 3) };
        btnCopy.Click += BtnCopy_Click;

        var lblAgent = new Label { Text = "Agent:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 3, 3) };
        cboAgent = new ComboBox { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 3, 6, 3) };
        var btnMarkSent = new Button { Text = "Mark Sent", AutoSize = true, Margin = new Padding(0, 3, 6, 3) };
        btnMarkSent.Click += BtnMarkSent_Click;
        lblSentStatus = new Label { AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.Gray, Margin = new Padding(0, 6, 0, 3) };

        actionBar.Controls.Add(btnCopy, 0, 0);
        actionBar.SetColumnSpan(btnCopy, 4);
        actionBar.Controls.Add(lblAgent, 0, 1);
        actionBar.Controls.Add(cboAgent, 1, 1);
        actionBar.Controls.Add(btnMarkSent, 2, 1);
        actionBar.Controls.Add(lblSentStatus, 3, 1);

        pnlPrompt.Controls.Add(txtPromptText);
        pnlPrompt.Controls.Add(actionBar);
        pnlPrompt.Controls.Add(lblText);

        return pnlPrompt;
    }

    // ===== Task logic =====

    private TaskItem? SelectedTask => lstTasks.SelectedIndex >= 0 && lstTasks.SelectedIndex < _state.Tasks.Count
        ? _state.Tasks[lstTasks.SelectedIndex] : null;

    private Prompt? SelectedPrompt
    {
        get
        {
            var task = SelectedTask;
            if (task == null || lstPrompts.SelectedIndex < 0 || lstPrompts.SelectedIndex >= task.Prompts.Count)
                return null;
            return task.Prompts[lstPrompts.SelectedIndex];
        }
    }

    private void RefreshTaskList()
    {
        lstTasks.Items.Clear();
        foreach (var t in _state.Tasks)
            lstTasks.Items.Add(t.Name);
    }

    private void BtnAddTask_Click(object? sender, EventArgs e)
    {
        var name = PromptInput("New Task", "Task name:");
        if (string.IsNullOrWhiteSpace(name)) return;
        _state.Tasks.Add(new TaskItem { Name = name });
        RefreshTaskList();
        lstTasks.SelectedIndex = _state.Tasks.Count - 1;
        Save();
    }

    private void BtnRemoveTask_Click(object? sender, EventArgs e)
    {
        if (SelectedTask == null) return;
        if (MessageBox.Show($"Remove task '{SelectedTask.Name}'?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        _state.Tasks.RemoveAt(lstTasks.SelectedIndex);
        RefreshTaskList();
        pnlDetail.Visible = false;
        pnlPrompt.Visible = false;
        Save();
    }

    private void LstTasks_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var task = SelectedTask;
        pnlDetail.Visible = task != null;
        pnlPrompt.Visible = false;
        if (task == null) return;
        txtTaskName.Text = task.Name;
        txtRepoPath.Text = task.RepoPath;
        RefreshPromptList();
        RefreshAgentList();
    }

    private void TxtTaskName_Leave(object? sender, EventArgs e)
    {
        var task = SelectedTask;
        if (task == null) return;
        task.Name = txtTaskName.Text;
        var idx = lstTasks.SelectedIndex;
        RefreshTaskList();
        lstTasks.SelectedIndex = idx;
        Save();
    }

    private void TxtRepoPath_Leave(object? sender, EventArgs e)
    {
        if (SelectedTask == null) return;
        SelectedTask.RepoPath = txtRepoPath.Text;
        Save();
    }

    private void BtnBrowseRepo_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog();
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            txtRepoPath.Text = dlg.SelectedPath;
            if (SelectedTask != null)
            {
                SelectedTask.RepoPath = dlg.SelectedPath;
                Save();
            }
        }
    }

    // ===== Prompt logic =====

    private void RefreshPromptList()
    {
        lstPrompts.Items.Clear();
        var task = SelectedTask;
        if (task == null) return;
        foreach (var p in task.Prompts)
        {
            var preview = p.Text.Length > 60 ? p.Text[..60] + "..." : p.Text;
            preview = preview.Replace("\r", "").Replace("\n", " ");
            var sent = p.SentToAgent != null ? $" [{p.SentToAgent}]" : "";
            lstPrompts.Items.Add(preview + sent);
        }
    }

    private void BtnAddPrompt_Click(object? sender, EventArgs e)
    {
        var task = SelectedTask;
        if (task == null) return;
        task.Prompts.Add(new Prompt { Text = "" });
        RefreshPromptList();
        lstPrompts.SelectedIndex = task.Prompts.Count - 1;
        Save();
    }

    private void BtnRemovePrompt_Click(object? sender, EventArgs e)
    {
        var task = SelectedTask;
        if (task == null || lstPrompts.SelectedIndex < 0) return;
        task.Prompts.RemoveAt(lstPrompts.SelectedIndex);
        RefreshPromptList();
        pnlPrompt.Visible = false;
        Save();
    }

    private void LstPrompts_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var prompt = SelectedPrompt;
        pnlPrompt.Visible = prompt != null;
        if (prompt == null) return;
        txtPromptText.Text = prompt.Text;
        RefreshAgentCombo();
        UpdateSentStatus();
    }

    private void TxtPromptText_Leave(object? sender, EventArgs e)
    {
        var prompt = SelectedPrompt;
        if (prompt == null) return;
        prompt.Text = txtPromptText.Text;
        RefreshPromptList();
        Save();
    }

    // ===== Agent logic =====

    private void RefreshAgentList()
    {
        lstAgents.Items.Clear();
        var task = SelectedTask;
        if (task == null) return;
        foreach (var a in task.Agents)
            lstAgents.Items.Add(a.Name);
    }

    private void RefreshAgentCombo()
    {
        cboAgent.Items.Clear();
        var task = SelectedTask;
        if (task == null) return;
        foreach (var a in task.Agents)
            cboAgent.Items.Add(a.Name);
    }

    private void BtnAddAgent_Click(object? sender, EventArgs e)
    {
        var task = SelectedTask;
        if (task == null) return;
        var name = PromptInput("New Agent", "Agent name (e.g. 'VS Code session 1'):");
        if (string.IsNullOrWhiteSpace(name)) return;
        task.Agents.Add(new AgentLabel { Name = name });
        RefreshAgentList();
        Save();
    }

    private void BtnRemoveAgent_Click(object? sender, EventArgs e)
    {
        var task = SelectedTask;
        if (task == null || lstAgents.SelectedIndex < 0) return;
        task.Agents.RemoveAt(lstAgents.SelectedIndex);
        RefreshAgentList();
        Save();
    }

    // ===== Clipboard & tracking =====

    private void BtnCopy_Click(object? sender, EventArgs e)
    {
        var prompt = SelectedPrompt;
        if (prompt == null || string.IsNullOrEmpty(txtPromptText.Text)) return;
        prompt.Text = txtPromptText.Text;
        Clipboard.SetText(prompt.Text);
        MessageBox.Show("Copied to clipboard!", "Prompt Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Save();
    }

    private void BtnMarkSent_Click(object? sender, EventArgs e)
    {
        var prompt = SelectedPrompt;
        if (prompt == null) return;
        if (cboAgent.SelectedIndex < 0)
        {
            MessageBox.Show("Select an agent first.", "Prompt Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        prompt.Text = txtPromptText.Text;
        prompt.SentToAgent = cboAgent.Text;
        prompt.SentAt = DateTime.Now;
        UpdateSentStatus();
        RefreshPromptList();
        Save();
    }

    private void UpdateSentStatus()
    {
        var prompt = SelectedPrompt;
        if (prompt?.SentToAgent != null)
            lblSentStatus.Text = $"Sent to {prompt.SentToAgent} at {prompt.SentAt:HH:mm}";
        else
            lblSentStatus.Text = "Not sent yet";
    }

    // ===== Helpers =====

    private void Save() => Persistence.Save(_state);

    private static string? PromptInput(string title, string label)
    {
        var form = new Form
        {
            Text = title,
            Width = 350,
            Height = 150,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };
        var lbl = new Label { Text = label, Left = 10, Top = 10, Width = 310 };
        var txt = new TextBox { Left = 10, Top = 35, Width = 310 };
        var btn = new Button { Text = "OK", Left = 240, Top = 70, Width = 80, DialogResult = DialogResult.OK };
        form.AcceptButton = btn;
        form.Controls.AddRange(new Control[] { lbl, txt, btn });
        return form.ShowDialog() == DialogResult.OK ? txt.Text : null;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Save();
        base.OnFormClosing(e);
    }
}
