using WorkspaceSetup.Models;

namespace WorkspaceSetup.Controls;

public class MonitorPreviewPanel : Panel
{
    private WorkspaceConfig? _config;
    private int _desktopIndex;
    private int _selectedAppIndex = -1;

    public MonitorPreviewPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(30, 30, 30);
    }

    public void SetConfig(WorkspaceConfig config, int desktopIndex = 0)
    {
        _config = config;
        _desktopIndex = desktopIndex;
        Invalidate();
    }

    public void SetSelectedApp(int index)
    {
        _selectedAppIndex = index;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_config == null || _config.Monitors.Count == 0) return;

        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Calculate the total virtual desktop bounds
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var mon in _config.Monitors.Values)
        {
            minX = Math.Min(minX, mon.OffsetX);
            minY = Math.Min(minY, mon.OffsetY);
            maxX = Math.Max(maxX, mon.OffsetX + mon.Width);
            maxY = Math.Max(maxY, mon.OffsetY + mon.Height);
        }

        int totalW = maxX - minX;
        int totalH = maxY - minY;
        if (totalW == 0 || totalH == 0) return;

        float pad = 20f;
        float scaleX = (Width - pad * 2) / totalW;
        float scaleY = (Height - pad * 2) / totalH;
        float scale = Math.Min(scaleX, scaleY);
        float offsetX = (Width - totalW * scale) / 2;
        float offsetY = (Height - totalH * scale) / 2;

        RectangleF ToScreen(int x, int y, int w, int h) =>
            new(offsetX + (x - minX) * scale, offsetY + (y - minY) * scale, w * scale, h * scale);

        // Draw monitors
        using var monBrush = new SolidBrush(Color.FromArgb(50, 50, 55));
        using var monBorder = new Pen(Color.FromArgb(100, 100, 110), 2);
        using var monFont = new Font("Segoe UI", 10f, FontStyle.Bold);

        foreach (var (name, mon) in _config.Monitors)
        {
            var rect = ToScreen(mon.OffsetX, mon.OffsetY, mon.Width, mon.Height);
            g.FillRectangle(monBrush, rect);
            g.DrawRectangle(monBorder, rect.X, rect.Y, rect.Width, rect.Height);

            var label = $"{name.ToUpper()}\n{mon.Width}x{mon.Height}";
            var labelSize = g.MeasureString(label, monFont);
            g.DrawString(label, monFont, Brushes.Gray,
                rect.X + (rect.Width - labelSize.Width) / 2,
                rect.Y + 8);
        }

        // Get apps for the selected desktop
        var apps = _desktopIndex >= 0 && _desktopIndex < _config.Desktops.Count
            ? _config.Desktops[_desktopIndex].Apps
            : [];

        var appColors = new[] {
            Color.FromArgb(140, 66, 133, 244),
            Color.FromArgb(140, 234, 67, 53),
            Color.FromArgb(140, 52, 168, 83),
            Color.FromArgb(140, 251, 188, 4),
            Color.FromArgb(140, 171, 71, 188),
            Color.FromArgb(140, 255, 112, 67),
        };

        using var appFont = new Font("Segoe UI", 8.5f);
        using var selectedPen = new Pen(Color.White, 2.5f);

        for (int i = 0; i < apps.Count; i++)
        {
            var app = apps[i];
            if (!_config.Monitors.TryGetValue(app.Monitor, out var mon)) continue;

            int absX = mon.OffsetX + app.X;
            int absY = mon.OffsetY + app.Y;
            var rect = ToScreen(absX, absY, app.Width, app.Height);

            var color = appColors[i % appColors.Length];
            using var fillBrush = new SolidBrush(color);
            using var borderPen = new Pen(Color.FromArgb(200, color.R, color.G, color.B), 1.5f);

            g.FillRectangle(fillBrush, rect);
            g.DrawRectangle(i == _selectedAppIndex ? selectedPen : borderPen,
                rect.X, rect.Y, rect.Width, rect.Height);

            var nameSize = g.MeasureString(app.Name, appFont);
            if (nameSize.Width < rect.Width && nameSize.Height < rect.Height)
            {
                g.DrawString(app.Name, appFont, Brushes.White,
                    rect.X + (rect.Width - nameSize.Width) / 2,
                    rect.Y + (rect.Height - nameSize.Height) / 2);
            }
        }

        // Draw desktop label in corner
        if (_config.Desktops.Count > 0 && _desktopIndex < _config.Desktops.Count)
        {
            var desktopName = _config.Desktops[_desktopIndex].Name;
            using var labelFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var labelBg = new SolidBrush(Color.FromArgb(180, 40, 40, 45));
            var text = $"Desktop {_desktopIndex + 1}: {desktopName}";
            var size = g.MeasureString(text, labelFont);
            var labelRect = new RectangleF(8, Height - size.Height - 8, size.Width + 12, size.Height + 4);
            g.FillRectangle(labelBg, labelRect);
            g.DrawString(text, labelFont, Brushes.White, labelRect.X + 6, labelRect.Y + 2);
        }
    }
}
