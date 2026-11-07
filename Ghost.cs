using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// GHOST
// Compile: csc /target:winexe /out:Ghost.exe Ghost.cs
//
// OPENING WINDOW TEXT - LOCKED:
// Focus:
//   GHOST
//   MODE: FOCUS LOSS
//   F11 = MAXIMIZE / RESTORE
//   RIGHT-CLICK = MENU
// Occlusion:
//   GHOST
//   MODE: OCCLUSION
//   RIGHT-CLICK = MENU = MAXIMIZE / RESTORE
//   WARNING: F11 GOES TO APP BEHIND
// No OR MENU line. That was redundant.
// Max window: no text. Only dot. Dot color = mode. Green = Focus, Orange = Occlusion.
// That is the warning in max without covering the app.
//
// RULES:
// - Default = Focus loss. Menu: Focus on top, Occlusion second, Maximize/Restore, Close.
// - Mode and size separate. No auto-maximize. Occlusion in small is allowed, just not a valid test.
// - Maximize/Restore in menu. Menu is mouse, mouse works in occlusion. F11 is keyboard, fails in occlusion.
// - Border: static rainbow, 2px.
// - Close: Esc or menu Close, instant.

public class GhostWindow : Form
{
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HT_CAPTION = 0x2;
    private const int WM_MOUSEACTIVATE = 0x0021;
    private const int MA_NOACTIVATE = 3;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private bool isMaximized = false;
    private Rectangle normalBounds;
    private bool occlusionMode = false;

    private const int BORDER_THICKNESS = 2;
    private const double TARGET_OPACITY = 0.55;

    private bool dotVisible = true;
    private readonly Timer dotTimer;
    private const int DOT_DIAMETER = 12;
    private const int DOT_MARGIN_RIGHT = 6;
    private const int DOT_MARGIN_TOP = 6;

    private readonly ContextMenuStrip contextMenu;
    private readonly ToolStripMenuItem focusItem;
    private readonly ToolStripMenuItem occlusionItem;
    private readonly ToolStripMenuItem maximizeItem;
    private readonly ToolStripMenuItem closeItem;

    private readonly Font titleFont = new Font("Consolas", 18, FontStyle.Bold);
    private readonly Font infoFont = new Font("Consolas", 13, FontStyle.Regular);
    private readonly Font modeFont = new Font("Consolas", 11, FontStyle.Bold);
    private readonly Font warnFont = new Font("Consolas", 10, FontStyle.Bold);
    private readonly Brush titleBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
    private readonly Brush infoBrush = new SolidBrush(Color.FromArgb(140, 255, 255, 255));
    private readonly Brush modeBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 0));
    private readonly Brush warnBrush = new SolidBrush(Color.FromArgb(255, 255, 120, 120));
    private readonly Brush dotFocusBrush = new SolidBrush(Color.FromArgb(0, 230, 120));
    private readonly Brush dotOcclusionBrush = new SolidBrush(Color.FromArgb(255, 165, 0));
    private readonly Brush backgroundBrush;
    private readonly Pen borderPen = new Pen(Color.White, BORDER_THICKNESS);

    private readonly StringFormat centerFormat = new StringFormat
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };

    public GhostWindow()
    {
        Text = "Ghost - Focus Loss Test";
        Size = new Size(500, 500);
        MinimumSize = new Size(280, 280);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        AutoScaleMode = AutoScaleMode.Dpi;

        BackColor = Color.FromArgb(30, 30, 38);
        backgroundBrush = new SolidBrush(BackColor);

        Opacity = 0;
        ShowInTaskbar = true;
        TopMost = true;
        DoubleBuffered = true;
        KeyPreview = true;

        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.StandardDoubleClick, true);
        UpdateStyles();

        dotTimer = new Timer();
        dotTimer.Interval = 500;
        dotTimer.Tick += (s, e) =>
        {
            if (!isMaximized) return;
            dotVisible = !dotVisible;
            Invalidate(GetDotRect());
        };

        contextMenu = new ContextMenuStrip();

        focusItem = new ToolStripMenuItem("Mode: Focus loss test");
        focusItem.Checked = true;
        focusItem.CheckOnClick = false;
        focusItem.Click += (s, e) => SetMode(false);

        occlusionItem = new ToolStripMenuItem("Mode: Occlusion test (no focus steal)");
        occlusionItem.Checked = false;
        occlusionItem.CheckOnClick = false;
        occlusionItem.Click += (s, e) => SetMode(true);

        maximizeItem = new ToolStripMenuItem("Maximize");
        maximizeItem.Click += (s, e) => ToggleMaximize();

        closeItem = new ToolStripMenuItem("Close");
        closeItem.Click += (s, e) => Close();

        contextMenu.Items.Add(focusItem);
        contextMenu.Items.Add(occlusionItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(maximizeItem);
        contextMenu.Items.Add(closeItem);
    }

    private Rectangle GetDotRect()
    {
        return new Rectangle(Width - DOT_MARGIN_RIGHT - DOT_DIAMETER - 4, DOT_MARGIN_TOP, DOT_DIAMETER + 8, DOT_DIAMETER + 8);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            if (occlusionMode)
                cp.ExStyle |= WS_EX_NOACTIVATE;
            return cp;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_MOUSEACTIVATE && occlusionMode)
        {
            m.Result = (IntPtr)MA_NOACTIVATE;
            return;
        }
        base.WndProc(ref m);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        normalBounds = Bounds;
        Opacity = TARGET_OPACITY;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (dotTimer != null) dotTimer.Stop();
        base.OnFormClosing(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (dotTimer != null) dotTimer.Dispose();
            if (titleFont != null) titleFont.Dispose();
            if (infoFont != null) infoFont.Dispose();
            if (modeFont != null) modeFont.Dispose();
            if (warnFont != null) warnFont.Dispose();
            if (titleBrush != null) titleBrush.Dispose();
            if (infoBrush != null) infoBrush.Dispose();
            if (modeBrush != null) modeBrush.Dispose();
            if (warnBrush != null) warnBrush.Dispose();
            if (dotFocusBrush != null) dotFocusBrush.Dispose();
            if (dotOcclusionBrush != null) dotOcclusionBrush.Dispose();
            if (backgroundBrush != null) backgroundBrush.Dispose();
            if (borderPen != null) borderPen.Dispose();
            if (centerFormat != null) centerFormat.Dispose();
            if (contextMenu != null) contextMenu.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.FillRectangle(backgroundBrush, ClientRectangle);
        DrawStaticRainbowBorder(g);

        if (isMaximized)
        {
            DrawLiveDot(g);
            return;
        }

        float cx = Width / 2f;
        float cy = Height / 2f;
        g.DrawString("GHOST", titleFont, titleBrush, cx, cy - 60, centerFormat);
        string modeText = occlusionMode ? "MODE: OCCLUSION" : "MODE: FOCUS LOSS";
        g.DrawString(modeText, modeFont, modeBrush, cx, cy - 10, centerFormat);

        if (occlusionMode)
        {
            // F11 is dead here, so do not show F11 line. Show menu line instead.
            g.DrawString("RIGHT-CLICK = MENU = MAXIMIZE / RESTORE", infoFont, infoBrush, cx, cy + 25, centerFormat);
            g.DrawString("WARNING: F11 GOES TO APP BEHIND", warnFont, warnBrush, cx, cy + 55, centerFormat);
        }
        else
        {
            g.DrawString("F11 = MAXIMIZE / RESTORE", infoFont, infoBrush, cx, cy + 25, centerFormat);
            g.DrawString("RIGHT-CLICK = MENU", infoFont, infoBrush, cx, cy + 55, centerFormat);
        }
    }

    private void DrawStaticRainbowBorder(Graphics g)
    {
        int w = Width;
        int h = Height;
        int t = BORDER_THICKNESS;
        int seg = 32;
        int alpha = 200;
        float hue = 0f;
        for (int x = 0; x < w; x += seg)
        {
            borderPen.Color = RainbowColor(hue + x * 0.6f, alpha);
            g.DrawLine(borderPen, x, t / 2f, Math.Min(x + seg, w), t / 2f);
        }
        for (int y = 0; y < h; y += seg)
        {
            borderPen.Color = RainbowColor(hue + (w + y) * 0.6f, alpha);
            g.DrawLine(borderPen, w - t / 2f, y, w - t / 2f, Math.Min(y + seg, h));
        }
        for (int x = w; x > 0; x -= seg)
        {
            borderPen.Color = RainbowColor(hue + (w + h + (w - x)) * 0.6f, alpha);
            g.DrawLine(borderPen, x, h - t / 2f, Math.Max(x - seg, 0), h - t / 2f);
        }
        for (int y = h; y > 0; y -= seg)
        {
            borderPen.Color = RainbowColor(hue + (2 * w + h + (h - y)) * 0.6f, alpha);
            g.DrawLine(borderPen, t / 2f, y, t / 2f, Math.Max(y - seg, 0));
        }
    }

    private void DrawLiveDot(Graphics g)
    {
        if (!dotVisible) return;
        Rectangle r = GetDotRect();
        Brush b = occlusionMode ? dotOcclusionBrush : dotFocusBrush;
        g.FillEllipse(b, r.X + 4, r.Y + 4, DOT_DIAMETER, DOT_DIAMETER);
    }

    private static Color RainbowColor(float hueValue, int alpha)
    {
        float h = hueValue % 360f;
        if (h < 0) h += 360f;
        float c = 1f;
        float x = c * (1 - Math.Abs((h / 60f) % 2 - 1));
        float r = 0, gg = 0, b = 0;
        if (h < 60) { r = c; gg = x; b = 0; }
        else if (h < 120) { r = x; gg = c; b = 0; }
        else if (h < 180) { r = 0; gg = c; b = x; }
        else if (h < 240) { r = 0; gg = x; b = c; }
        else if (h < 300) { r = x; gg = 0; b = c; }
        else { r = c; gg = 0; b = x; }
        return Color.FromArgb(alpha, (int)(r * 255), (int)(gg * 255), (int)(b * 255));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Right)
        {
            if (maximizeItem != null) maximizeItem.Text = isMaximized ? "Restore" : "Maximize";
            contextMenu.Show(Cursor.Position);
            return;
        }
        if (e.Button == MouseButtons.Left)
        {
            if (e.Clicks == 2) return;
            if (isMaximized) return;
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.F11) ToggleMaximize();
        if (e.KeyCode == Keys.Escape) Close();
    }

    private void ToggleMaximize()
    {
        if (!isMaximized)
        {
            normalBounds = Bounds;
            Bounds = Screen.FromHandle(Handle).WorkingArea;
            isMaximized = true;
            dotVisible = true;
            if (dotTimer != null) dotTimer.Start();
            if (maximizeItem != null) maximizeItem.Text = "Restore";
        }
        else
        {
            Bounds = normalBounds;
            isMaximized = false;
            if (dotTimer != null) dotTimer.Stop();
            if (maximizeItem != null) maximizeItem.Text = "Maximize";
        }
        Invalidate();
    }

    private void SetMode(bool occlusion)
    {
        if (occlusionMode == occlusion) return;
        occlusionMode = occlusion;
        focusItem.Checked = !occlusion;
        occlusionItem.Checked = occlusion;
        Text = occlusion ? "Ghost - Occlusion Test" : "Ghost - Focus Loss Test";
        Rectangle savedBounds = Bounds;
        double savedOpacity = Opacity;
        bool wasMaximized = isMaximized;
        RecreateHandle();
        Bounds = savedBounds;
        Opacity = savedOpacity;
        isMaximized = wasMaximized;
        Invalidate();
    }
}

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new GhostWindow());
    }
}
