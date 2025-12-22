using System.Runtime.InteropServices;
using DataUsageReporter.Core;

namespace DataUsageReporter.UI;

/// <summary>
/// Small always-on-top overlay window that displays network speeds near the system tray.
/// </summary>
public class SpeedOverlay : Form
{
    private readonly Label _downloadLabel;
    private readonly Label _uploadLabel;
    private readonly ISpeedFormatter _formatter;
    private readonly System.Windows.Forms.Timer _keepOnTopTimer;

    // Windows API imports for finding system tray position
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public SpeedOverlay(ISpeedFormatter formatter)
    {
        _formatter = formatter;

        // Configure form as a small overlay
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.FromArgb(32, 32, 32); // Dark gray to match taskbar
        ForeColor = Color.White;
        StartPosition = FormStartPosition.Manual;

        // Enable double buffering to prevent flickering
        DoubleBuffered = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint, true);

        // Transparent background
        TransparencyKey = BackColor;

        // Size for two lines of text (compact)
        Width = 85;
        Height = 40;

        // Position dynamically to the left of system tray
        PositionNextToSystemTray();

        // Create labels
        _uploadLabel = new Label
        {
            Text = "↑ 0 bps",
            ForeColor = Color.FromArgb(255, 152, 0), // Orange
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(0, 2)
        };

        _downloadLabel = new Label
        {
            Text = "↓ 0 bps",
            ForeColor = Color.FromArgb(76, 175, 80), // Green
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(0, 20)
        };

        Controls.Add(_uploadLabel);
        Controls.Add(_downloadLabel);

        // Allow clicking through to reposition
        MouseDown += OnMouseDown;
        _uploadLabel.MouseDown += OnMouseDown;
        _downloadLabel.MouseDown += OnMouseDown;

        // Timer to periodically ensure window stays on top (handles edge cases)
        _keepOnTopTimer = new System.Windows.Forms.Timer
        {
            Interval = 2000 // Check every 2 seconds
        };
        _keepOnTopTimer.Tick += OnKeepOnTopTick;
        _keepOnTopTimer.Start();
    }

    private void OnKeepOnTopTick(object? sender, EventArgs e)
    {
        if (!Visible || IsDisposed) return;

        // Silently ensure topmost without causing repaint
        SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOREDRAW);
    }

    private const uint SWP_NOREDRAW = 0x0008;

    public void UpdateSpeed(SpeedReading speed)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateSpeed(speed));
            return;
        }

        _downloadLabel.Text = $"↓ {_formatter.FormatSpeed(speed.DownloadBytesPerSecond)}";
        _uploadLabel.Text = $"↑ {_formatter.FormatSpeed(speed.UploadBytesPerSecond)}";
    }

    private Point _dragStart;

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _dragStart = e.Location;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
        }
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Left += e.X - _dragStart.X;
            Top += e.Y - _dragStart.Y;
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        MouseMove -= OnMouseMove;
        MouseUp -= OnMouseUp;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x80;      // WS_EX_TOOLWINDOW - no taskbar button
            cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE - don't activate when clicked
            cp.ExStyle |= 0x00000008; // WS_EX_TOPMOST
            return cp;
        }
    }

    protected override bool ShowWithoutActivation => true;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Unsubscribe from events to prevent memory leaks
        MouseDown -= OnMouseDown;
        _uploadLabel.MouseDown -= OnMouseDown;
        _downloadLabel.MouseDown -= OnMouseDown;
        _keepOnTopTimer.Tick -= OnKeepOnTopTick;

        _keepOnTopTimer.Stop();
        _keepOnTopTimer.Dispose();
        base.OnFormClosing(e);
    }

    private void PositionNextToSystemTray()
    {
        try
        {
            // Find the taskbar
            var taskbarHandle = FindWindow("Shell_TrayWnd", null);
            if (taskbarHandle == IntPtr.Zero)
            {
                FallbackPosition();
                return;
            }

            // Find the notification area (system tray)
            var trayNotifyHandle = FindWindowEx(taskbarHandle, IntPtr.Zero, "TrayNotifyWnd", null);
            if (trayNotifyHandle == IntPtr.Zero)
            {
                FallbackPosition();
                return;
            }

            // Get the system tray bounds
            if (GetWindowRect(trayNotifyHandle, out RECT trayRect))
            {
                // Position to the left of the system tray, vertically centered on taskbar
                int x = trayRect.Left - Width - 5;
                int y = trayRect.Top + (trayRect.Bottom - trayRect.Top - Height) / 2;
                Location = new Point(x, y);
            }
            else
            {
                FallbackPosition();
            }
        }
        catch
        {
            FallbackPosition();
        }
    }

    private void FallbackPosition()
    {
        // Fallback: position near bottom-right
        var screenBounds = Screen.PrimaryScreen!.Bounds;
        Location = new Point(screenBounds.Right - Width - 200, screenBounds.Bottom - 45);
    }

    /// <summary>
    /// Reposition the overlay (call this if taskbar changes)
    /// </summary>
    public void Reposition()
    {
        PositionNextToSystemTray();
    }
}
