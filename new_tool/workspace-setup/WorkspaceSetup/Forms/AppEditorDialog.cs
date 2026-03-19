using WorkspaceSetup.Models;

namespace WorkspaceSetup.Forms;

public class AppEditorDialog : Form
{
    private readonly TextBox _txtName;
    private readonly TextBox _txtExecutable;
    private readonly Button _btnBrowse;
    private readonly TextBox _txtArguments;
    private readonly ComboBox _cboMonitor;
    private readonly NumericUpDown _nudX;
    private readonly NumericUpDown _nudY;
    private readonly NumericUpDown _nudWidth;
    private readonly NumericUpDown _nudHeight;
    private readonly NumericUpDown _nudDelay;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public AppEntry Result { get; private set; } = new();

    public AppEditorDialog(AppEntry? existing, IEnumerable<string> monitorNames)
    {
        Text = existing == null ? "Add Application" : "Edit Application";
        Size = new Size(520, 480);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(32, 32, 32);
        ForeColor = Color.White;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 3,
            RowCount = 9,
            AutoSize = true,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));

        int row = 0;

        Label MakeLabel(string text) => new()
        {
            Text = text,
            ForeColor = Color.FromArgb(200, 200, 200),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 6, 0, 0)
        };

        TextBox MakeTextBox(string val = "") => new()
        {
            Text = val,
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(50, 50, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        // Name
        layout.Controls.Add(MakeLabel("Name:"), 0, row);
        _txtName = MakeTextBox(existing?.Name ?? "");
        layout.Controls.Add(_txtName, 1, row);
        layout.SetColumnSpan(_txtName, 2);
        row++;

        // Executable
        layout.Controls.Add(MakeLabel("Executable:"), 0, row);
        _txtExecutable = MakeTextBox(existing?.Executable ?? "");
        layout.Controls.Add(_txtExecutable, 1, row);
        _btnBrowse = new Button
        {
            Text = "Browse...",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 65),
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
        };
        _btnBrowse.Click += BtnBrowse_Click;
        layout.Controls.Add(_btnBrowse, 2, row);
        row++;

        // Arguments
        layout.Controls.Add(MakeLabel("Arguments:"), 0, row);
        _txtArguments = MakeTextBox(existing?.Arguments ?? "");
        layout.Controls.Add(_txtArguments, 1, row);
        layout.SetColumnSpan(_txtArguments, 2);
        row++;

        // Monitor
        layout.Controls.Add(MakeLabel("Monitor:"), 0, row);
        _cboMonitor = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(50, 50, 55),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };
        foreach (var name in monitorNames) _cboMonitor.Items.Add(name);
        _cboMonitor.SelectedItem = existing?.Monitor ?? monitorNames.FirstOrDefault();
        layout.Controls.Add(_cboMonitor, 1, row);
        layout.SetColumnSpan(_cboMonitor, 2);
        row++;

        // Position panel
        NumericUpDown MakeNud(int val, int max = 10000)
        {
            var nud = new NumericUpDown
            {
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = Color.White,
                Width = 80,
            };
            nud.Minimum = 0;
            nud.Maximum = max;
            nud.Value = Math.Clamp(val, 0, max);
            return nud;
        }

        // X, Y
        layout.Controls.Add(MakeLabel("X:"), 0, row);
        var posPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        _nudX = MakeNud(existing?.X ?? 0);
        posPanel.Controls.Add(_nudX);
        posPanel.Controls.Add(new Label
        {
            Text = "  Y:",
            ForeColor = Color.FromArgb(200, 200, 200),
            AutoSize = true,
            Padding = new Padding(0, 6, 0, 0)
        });
        _nudY = MakeNud(existing?.Y ?? 0);
        posPanel.Controls.Add(_nudY);
        layout.Controls.Add(posPanel, 1, row);
        layout.SetColumnSpan(posPanel, 2);
        row++;

        // Width, Height
        layout.Controls.Add(MakeLabel("Width:"), 0, row);
        var sizePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        _nudWidth = MakeNud(existing?.Width ?? 1280);
        sizePanel.Controls.Add(_nudWidth);
        sizePanel.Controls.Add(new Label
        {
            Text = "  Height:",
            ForeColor = Color.FromArgb(200, 200, 200),
            AutoSize = true,
            Padding = new Padding(0, 6, 0, 0)
        });
        _nudHeight = MakeNud(existing?.Height ?? 1024);
        sizePanel.Controls.Add(_nudHeight);
        layout.Controls.Add(sizePanel, 1, row);
        layout.SetColumnSpan(sizePanel, 2);
        row++;

        // Delay
        layout.Controls.Add(MakeLabel("Delay (sec):"), 0, row);
        _nudDelay = MakeNud(existing?.DelaySeconds ?? 3, 60);
        layout.Controls.Add(_nudDelay, 1, row);
        layout.SetColumnSpan(_nudDelay, 2);
        row++;

        // Spacer
        layout.Controls.Add(new Panel { Height = 10 }, 0, row);
        row++;

        // Buttons
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
        };

        var btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 65),
            ForeColor = Color.White,
            Width = 90,
            Height = 34,
        };

        var btnOk = new Button
        {
            Text = "OK",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(55, 90, 170),
            ForeColor = Color.White,
            Width = 90,
            Height = 34,
        };
        btnOk.Click += BtnOk_Click;

        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnOk);
        layout.Controls.Add(btnPanel, 0, row);
        layout.SetColumnSpan(btnPanel, 3);

        Controls.Add(layout);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Executables (*.exe)|*.exe|All Files (*.*)|*.*",
            Title = "Select Application"
        };
        if (dlg.ShowDialog() == DialogResult.OK)
            _txtExecutable.Text = dlg.FileName;
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text) || string.IsNullOrWhiteSpace(_txtExecutable.Text))
        {
            MessageBox.Show("Name and Executable are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Result = new AppEntry
        {
            Name = _txtName.Text.Trim(),
            Executable = _txtExecutable.Text.Trim(),
            Arguments = _txtArguments.Text.Trim(),
            Monitor = _cboMonitor.SelectedItem?.ToString() ?? "left",
            X = (int)_nudX.Value,
            Y = (int)_nudY.Value,
            Width = (int)_nudWidth.Value,
            Height = (int)_nudHeight.Value,
            DelaySeconds = (int)_nudDelay.Value,
        };

        DialogResult = DialogResult.OK;
        Close();
    }
}
