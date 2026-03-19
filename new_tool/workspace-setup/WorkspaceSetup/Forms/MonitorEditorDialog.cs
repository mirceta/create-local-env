using WorkspaceSetup.Models;

namespace WorkspaceSetup.Forms;

public class MonitorEditorDialog : Form
{
    private readonly TextBox _txtName;
    private readonly NumericUpDown _nudOffsetX;
    private readonly NumericUpDown _nudOffsetY;
    private readonly NumericUpDown _nudWidth;
    private readonly NumericUpDown _nudHeight;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string MonitorName => _txtName.Text.Trim().ToLower();
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public MonitorInfo ResultMonitor { get; private set; } = new();

    public MonitorEditorDialog(string? existingName = null, MonitorInfo? existing = null)
    {
        Text = existingName == null ? "Add Monitor" : "Edit Monitor";
        Size = new Size(400, 340);
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
            ColumnCount = 2,
            RowCount = 7,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        Label MakeLabel(string text) => new()
        {
            Text = text,
            ForeColor = Color.FromArgb(200, 200, 200),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 6, 0, 0)
        };

        NumericUpDown MakeNud(int val, int min = -10000, int max = 20000)
        {
            var nud = new NumericUpDown
            {
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
            };
            nud.Minimum = min;
            nud.Maximum = max;
            nud.Value = Math.Clamp(val, min, max);
            return nud;
        }

        int row = 0;

        layout.Controls.Add(MakeLabel("Name:"), 0, row);
        _txtName = new TextBox
        {
            Text = existingName ?? "",
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(50, 50, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Enabled = existingName == null // can't rename existing
        };
        layout.Controls.Add(_txtName, 1, row++);

        layout.Controls.Add(MakeLabel("Offset X:"), 0, row);
        _nudOffsetX = MakeNud(existing?.OffsetX ?? 0);
        layout.Controls.Add(_nudOffsetX, 1, row++);

        layout.Controls.Add(MakeLabel("Offset Y:"), 0, row);
        _nudOffsetY = MakeNud(existing?.OffsetY ?? 0);
        layout.Controls.Add(_nudOffsetY, 1, row++);

        layout.Controls.Add(MakeLabel("Width:"), 0, row);
        _nudWidth = MakeNud(existing?.Width ?? 2560, 100);
        layout.Controls.Add(_nudWidth, 1, row++);

        layout.Controls.Add(MakeLabel("Height:"), 0, row);
        _nudHeight = MakeNud(existing?.Height ?? 1440, 100);
        layout.Controls.Add(_nudHeight, 1, row++);

        // Spacer
        layout.Controls.Add(new Panel { Height = 10 }, 0, row++);

        // Buttons
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
        };

        var btnCancel = new Button
        {
            Text = "Cancel", DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 65),
            ForeColor = Color.White, Width = 90, Height = 34,
        };
        var btnOk = new Button
        {
            Text = "OK", FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(55, 90, 170), ForeColor = Color.White,
            Width = 90, Height = 34,
        };
        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ResultMonitor = new MonitorInfo
            {
                OffsetX = (int)_nudOffsetX.Value,
                OffsetY = (int)_nudOffsetY.Value,
                Width = (int)_nudWidth.Value,
                Height = (int)_nudHeight.Value,
            };
            DialogResult = DialogResult.OK;
            Close();
        };
        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnOk);
        layout.Controls.Add(btnPanel, 0, row);
        layout.SetColumnSpan(btnPanel, 2);

        Controls.Add(layout);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }
}
