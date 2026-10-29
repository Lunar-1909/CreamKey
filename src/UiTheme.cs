using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace CreamyKeysDesktop
{
    internal static class UiTheme
    {
        public static readonly Color Canvas = Color.FromArgb(17, 20, 18);
        public static readonly Color Surface = Color.FromArgb(27, 32, 29);
        public static readonly Color SurfaceElevated = Color.FromArgb(36, 42, 38);
        public static readonly Color Border = Color.FromArgb(58, 68, 62);
        public static readonly Color Content = Color.FromArgb(238, 243, 240);
        public static readonly Color ContentElevated = Color.FromArgb(248, 250, 249);
        public static readonly Color ContentBorder = Color.FromArgb(210, 219, 214);
        public static readonly Color TextPrimary = Color.FromArgb(247, 249, 248);
        public static readonly Color TextMuted = Color.FromArgb(170, 182, 175);
        public static readonly Color Ink = Color.FromArgb(31, 38, 34);
        public static readonly Color InkMuted = Color.FromArgb(101, 115, 107);
        public static readonly Color Action = Color.FromArgb(80, 213, 164);
        public static readonly Color ActionActive = Color.FromArgb(53, 185, 138);
        public static readonly Color Warning = Color.FromArgb(246, 200, 95);
        public static readonly Color Danger = Color.FromArgb(255, 111, 97);
        public static readonly Color Focus = Color.FromArgb(113, 232, 188);

        public static Font DisplayFont(float size, FontStyle style)
        {
            return CreateFont("Segoe UI Variable Display", "Segoe UI", size, style);
        }

        public static Font BodyFont(float size, FontStyle style)
        {
            return CreateFont("Segoe UI Variable Text", "Segoe UI", size, style);
        }

        public static GraphicsPath RoundedRect(Rectangle rectangle, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = Math.Max(2, radius * 2);
            if (rectangle.Width <= diameter || rectangle.Height <= diameter)
            {
                path.AddRectangle(rectangle);
                path.CloseFigure();
                return path;
            }

            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Color Blend(Color from, Color to, float amount)
        {
            amount = Math.Max(0.0f, Math.Min(1.0f, amount));
            return Color.FromArgb(
                (int)(from.A + ((to.A - from.A) * amount)),
                (int)(from.R + ((to.R - from.R) * amount)),
                (int)(from.G + ((to.G - from.G) * amount)),
                (int)(from.B + ((to.B - from.B) * amount)));
        }

        public static Bitmap TintIcon(Image source, Color color)
        {
            Bitmap result = new Bitmap(source.Width, source.Height,
                PixelFormat.Format32bppArgb);
            ColorMatrix matrix = new ColorMatrix(new float[][]
            {
                new float[] { 0, 0, 0, 0, 0 },
                new float[] { 0, 0, 0, 0, 0 },
                new float[] { 0, 0, 0, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[]
                {
                    color.R / 255.0f,
                    color.G / 255.0f,
                    color.B / 255.0f,
                    0,
                    1
                }
            });
            using (Graphics graphics = Graphics.FromImage(result))
            using (ImageAttributes attributes = new ImageAttributes())
            {
                attributes.SetColorMatrix(matrix);
                graphics.DrawImage(source, new Rectangle(0, 0, result.Width, result.Height),
                    0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            }
            return result;
        }

        public static Color ResolveSurface(Control control, Color fallback)
        {
            Control current = control == null ? null : control.Parent;
            while (current != null)
            {
                ModernPanel modern = current as ModernPanel;
                if (modern != null && modern.FillColor != Color.Empty)
                {
                    return modern.FillColor;
                }

                if (current.BackColor != Color.Transparent && current.BackColor.A == 255)
                {
                    return current.BackColor;
                }
                current = current.Parent;
            }
            return fallback;
        }

        private static Font CreateFont(string preferred, string fallback, float size, FontStyle style)
        {
            try
            {
                Font candidate = new Font(preferred, size, style, GraphicsUnit.Point);
                if (string.Equals(candidate.Name, preferred, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
                candidate.Dispose();
            }
            catch
            {
            }
            return new Font(fallback, size, style, GraphicsUnit.Point);
        }
    }

    internal sealed class ThemeButton : Button
    {
        private readonly System.Windows.Forms.Timer _animationTimer;
        private float _hoverAmount;
        private float _targetHover;
        private bool _pressed;
        private bool _accent;
        private bool _danger;
        private int _cornerRadius;

        public ThemeButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = UiTheme.BodyFont(9.0f, FontStyle.Bold);
            Cursor = Cursors.Hand;
            TabStop = true;
            Height = 40;
            _cornerRadius = 8;
            _animationTimer = new System.Windows.Forms.Timer();
            _animationTimer.Interval = 16;
            _animationTimer.Tick += Animate;
        }

        public bool Accent
        {
            get { return _accent; }
            set { _accent = value; Invalidate(); }
        }

        public bool Danger
        {
            get { return _danger; }
            set { _danger = value; Invalidate(); }
        }

        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = Math.Max(3, Math.Min(16, value)); Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _targetHover = 1.0f;
            _animationTimer.Start();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _targetHover = 0.0f;
            _pressed = false;
            _animationTimer.Start();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && Enabled)
            {
                _pressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _pressed = false;
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(UiTheme.ResolveSurface(this, UiTheme.Surface));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle box = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            if (_pressed)
            {
                box.Offset(0, 1);
            }

            Color baseColor = _danger ? UiTheme.Danger : (_accent ? UiTheme.Action : UiTheme.SurfaceElevated);
            Color hoverColor = _danger
                ? UiTheme.Blend(UiTheme.Danger, Color.White, 0.12f)
                : (_accent ? UiTheme.Blend(UiTheme.Action, Color.White, 0.13f) : UiTheme.Blend(UiTheme.SurfaceElevated, UiTheme.Border, 0.65f));
            Color fill = UiTheme.Blend(baseColor, hoverColor, _hoverAmount);
            if (_pressed)
            {
                fill = UiTheme.Blend(fill, Color.Black, 0.13f);
            }
            if (!Enabled)
            {
                fill = UiTheme.Blend(UiTheme.Surface, UiTheme.Border, 0.38f);
            }

            using (GraphicsPath path = UiTheme.RoundedRect(box, _cornerRadius))
            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen border = new Pen(_accent || _danger ? fill : UiTheme.Border, 1.0f))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(border, path);
            }

            Color textColor = _accent || _danger ? UiTheme.Canvas : UiTheme.TextPrimary;
            if (!Enabled)
            {
                textColor = UiTheme.TextMuted;
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, box, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);

            if (Focused && ShowFocusCues)
            {
                Rectangle focus = Rectangle.Inflate(box, -3, -3);
                using (GraphicsPath path = UiTheme.RoundedRect(focus, Math.Max(3, _cornerRadius - 2)))
                using (Pen pen = new Pen(UiTheme.Focus, 1.4f))
                {
                    pen.DashStyle = DashStyle.Dot;
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer.Stop();
                _animationTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        private void Animate(object sender, EventArgs e)
        {
            float distance = _targetHover - _hoverAmount;
            if (Math.Abs(distance) < 0.02f)
            {
                _hoverAmount = _targetHover;
                _animationTimer.Stop();
            }
            else
            {
                _hoverAmount += distance * 0.28f;
            }
            Invalidate();
        }
    }

    internal sealed class ThemeToggle : CheckBox
    {
        private readonly System.Windows.Forms.Timer _animationTimer;
        private float _visualValue;
        private float _targetValue;
        private float _animationVelocity;
        private bool _hovered;
        private bool _darkSurface;

        public ThemeToggle()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.Opaque, true);
            AutoSize = false;
            Height = 32;
            Width = 230;
            Font = UiTheme.BodyFont(9.0f, FontStyle.Regular);
            Cursor = Cursors.Hand;
            BackColor = UiTheme.ContentElevated;
            _animationTimer = new System.Windows.Forms.Timer();
            _animationTimer.Interval = 16;
            _animationTimer.Tick += Animate;
        }

        public bool DarkSurface
        {
            get { return _darkSurface; }
            set
            {
                _darkSurface = value;
                BackColor = value ? UiTheme.Surface : UiTheme.ContentElevated;
                Invalidate();
            }
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            _targetValue = Checked ? 1.0f : 0.0f;
            if (!IsHandleCreated)
            {
                _visualValue = _targetValue;
            }
            else
            {
                _animationTimer.Start();
            }
            base.OnCheckedChanged(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color labelColor = _darkSurface ? UiTheme.TextPrimary : UiTheme.Ink;
            if (!Enabled)
            {
                labelColor = _darkSurface ? UiTheme.TextMuted : UiTheme.InkMuted;
            }

            bool compact = string.IsNullOrWhiteSpace(Text);
            int trackWidth = compact ? Math.Max(34, Math.Min(58, Width - 2)) : 58;
            Rectangle track = new Rectangle(1, 3, trackWidth, 26);
            Color trackOff = _darkSurface
                ? UiTheme.Blend(UiTheme.Border, UiTheme.SurfaceElevated, 0.34f)
                : UiTheme.Blend(UiTheme.ContentBorder, UiTheme.ContentElevated, 0.22f);
            Color trackColor = UiTheme.Blend(trackOff, UiTheme.ActionActive, _visualValue);
            if (_hovered && Enabled)
            {
                trackColor = UiTheme.Blend(trackColor, Color.White, 0.08f);
            }
            if (!Enabled)
            {
                trackColor = UiTheme.Blend(trackOff, UiTheme.Surface, 0.25f);
            }

            using (GraphicsPath trackPath = UiTheme.RoundedRect(track, 13))
            using (LinearGradientBrush trackBrush = new LinearGradientBrush(track,
                UiTheme.Blend(trackColor, Color.White, 0.14f),
                UiTheme.Blend(trackColor, Color.Black, 0.10f), LinearGradientMode.Vertical))
            using (Pen trackBorder = new Pen(_darkSurface
                ? UiTheme.Blend(UiTheme.Border, Color.White, 0.10f)
                : UiTheme.Blend(UiTheme.ContentBorder, UiTheme.InkMuted, 0.20f), 1.0f))
            {
                e.Graphics.FillPath(trackBrush, trackPath);
                e.Graphics.DrawPath(trackBorder, trackPath);
            }

            int knobWidth = compact && trackWidth < 50 ? 20 : 30;
            int travel = Math.Max(0, track.Width - knobWidth - 4);
            int knobX = track.X + 2 + (int)Math.Round(travel * _visualValue);
            Rectangle knobShadow = new Rectangle(knobX, track.Y + 4, knobWidth, track.Height - 3);
            Rectangle knob = new Rectangle(knobX, track.Y + 2, knobWidth, track.Height - 4);
            using (GraphicsPath shadowPath = UiTheme.RoundedRect(knobShadow, knobShadow.Height / 2))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(52, 0, 0, 0)))
            {
                e.Graphics.FillPath(shadowBrush, shadowPath);
            }
            using (GraphicsPath knobPath = UiTheme.RoundedRect(knob, knob.Height / 2))
            using (LinearGradientBrush knobBrush = new LinearGradientBrush(knob,
                Color.FromArgb(255, 255, 255), Color.FromArgb(198, 205, 201), LinearGradientMode.Vertical))
            using (Pen knobBorder = new Pen(Color.FromArgb(122, 135, 128), 1.0f))
            {
                e.Graphics.FillPath(knobBrush, knobPath);
                e.Graphics.DrawPath(knobBorder, knobPath);
            }

            if (!compact && knob.Width >= 28)
            {
                string state = Checked ? "ON" : "OFF";
                using (Font stateFont = UiTheme.BodyFont(6.4f, FontStyle.Bold))
                {
                    TextRenderer.DrawText(e.Graphics, state, stateFont, knob, UiTheme.Ink,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                }
            }

            Rectangle textBox = new Rectangle(track.Right + 12, 0,
                Math.Max(1, Width - track.Right - 12), Height);
            TextRenderer.DrawText(e.Graphics, Text, Font, textBox, labelColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                TextFormatFlags.SingleLine);

            if (Focused && ShowFocusCues)
            {
                using (Pen pen = new Pen(UiTheme.Focus, 1.2f))
                {
                    pen.DashStyle = DashStyle.Dot;
                    e.Graphics.DrawRectangle(pen, track.X, track.Y, track.Width, track.Height);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer.Stop();
                _animationTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        private void Animate(object sender, EventArgs e)
        {
            const float dt = 0.016f;
            const float stiffness = 520.0f;
            const float damping = 36.0f;
            float distance = _targetValue - _visualValue;
            _animationVelocity += (stiffness * distance - damping * _animationVelocity) * dt;
            _visualValue += _animationVelocity * dt;
            if (Math.Abs(distance) < 0.002f && Math.Abs(_animationVelocity) < 0.02f)
            {
                _visualValue = _targetValue;
                _animationVelocity = 0.0f;
                _animationTimer.Stop();
            }
            Invalidate();
        }
    }

    internal sealed class SoundPulseControl : Control
    {
        private readonly System.Windows.Forms.Timer _timer;
        private readonly float[] _levels;
        private readonly Random _random;
        private float _energy;
        private float _phase;

        public SoundPulseControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.Opaque, true);
            BackColor = UiTheme.Surface;
            _levels = new float[30];
            _random = new Random();
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 33;
            _timer.Tick += TickAnimation;
            _timer.Start();
        }

        public void Pulse(float strength)
        {
            _energy = Math.Max(_energy, Math.Max(0.25f, Math.Min(1.0f, strength)));
            for (int i = 0; i < _levels.Length; i++)
            {
                float shape = (float)Math.Sin((i / (float)(_levels.Length - 1)) * Math.PI);
                _levels[i] = Math.Max(_levels[i], shape * _energy * (0.55f + ((float)_random.NextDouble() * 0.45f)));
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int mid = Height / 2;
            int usableHeight = Math.Max(10, Height - 12);
            float step = Width / (float)(_levels.Length - 1);
            PointF[] points = new PointF[_levels.Length];
            for (int i = 0; i < _levels.Length; i++)
            {
                float ambient = 0.045f + ((float)Math.Sin(_phase + (i * 0.38f)) * 0.020f);
                float value = Math.Max(ambient, _levels[i]);
                float direction = (float)Math.Sin(_phase + (i * 0.72f));
                points[i] = new PointF(i * step, mid + (direction * value * usableHeight * 0.34f));
            }

            using (Pen baseline = new Pen(Color.FromArgb(75, UiTheme.Action), 1.0f))
            using (Pen wave = new Pen(UiTheme.Action, 2.2f))
            {
                e.Graphics.DrawLine(baseline, 0, mid, Width, mid);
                if (points.Length > 1)
                {
                    e.Graphics.DrawCurve(wave, points, 0.34f);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
            }
            base.Dispose(disposing);
        }

        private void TickAnimation(object sender, EventArgs e)
        {
            _phase += 0.13f;
            _energy *= 0.88f;
            for (int i = 0; i < _levels.Length; i++)
            {
                _levels[i] *= 0.86f;
            }
            Invalidate();
        }
    }

    internal sealed class SlidingIndicator : Control
    {
        private readonly System.Windows.Forms.Timer _timer;
        private int _targetTop;
        private float _visualTop;
        private float _velocity;
        private bool _initialized;

        public SlidingIndicator()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque |
                ControlStyles.ResizeRedraw, true);
            BackColor = UiTheme.Surface;
            Size = new Size(5, 24);
            _targetTop = Top;
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 16;
            _timer.Tick += Animate;
        }

        public void MoveTo(int top, bool reducedMotion)
        {
            _targetTop = top;
            if (!_initialized)
            {
                _visualTop = Top;
                _initialized = true;
            }
            if (reducedMotion)
            {
                Top = _targetTop;
                _visualTop = _targetTop;
                _velocity = 0.0f;
                _timer.Stop();
                Invalidate();
            }
            else
            {
                _timer.Start();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle box = new Rectangle(0, 1, Math.Max(1, Width - 1), Math.Max(1, Height - 2));
            using (GraphicsPath path = UiTheme.RoundedRect(box, 3))
            using (SolidBrush brush = new SolidBrush(UiTheme.Action))
            {
                e.Graphics.FillPath(brush, path);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
            }
            base.Dispose(disposing);
        }

        private void Animate(object sender, EventArgs e)
        {
            const float dt = 0.016f;
            const float stiffness = 410.0f;
            const float damping = 30.0f;
            float distance = _targetTop - _visualTop;
            _velocity += (stiffness * distance - damping * _velocity) * dt;
            _visualTop += _velocity * dt;
            Top = (int)Math.Round(_visualTop);
            if (Math.Abs(distance) < 0.18f && Math.Abs(_velocity) < 0.4f)
            {
                Top = _targetTop;
                _visualTop = _targetTop;
                _velocity = 0.0f;
                _timer.Stop();
            }
            Invalidate();
        }
    }

    internal sealed class SplashForm : Form
    {
        private readonly Label _stageLabel;
        private readonly SoundPulseControl _pulse;

        public SplashForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            ClientSize = new Size(520, 312);
            BackColor = UiTheme.Canvas;
            Font = UiTheme.BodyFont(9.0f, FontStyle.Regular);
            AutoScaleMode = AutoScaleMode.Dpi;
            DoubleBuffered = true;

            PictureBox mark = new PictureBox();
            mark.Image = LoadExecutableIcon();
            mark.SizeMode = PictureBoxSizeMode.Zoom;
            mark.SetBounds(36, 34, 50, 50);
            Controls.Add(mark);

            Label title = new Label();
            title.Text = "CreamyKeys";
            title.ForeColor = UiTheme.TextPrimary;
            title.Font = UiTheme.DisplayFont(21.0f, FontStyle.Bold);
            title.UseCompatibleTextRendering = true;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.SetBounds(104, 20, 360, 62);
            Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "Desktop sound studio";
            subtitle.ForeColor = UiTheme.TextMuted;
            subtitle.SetBounds(106, 78, 320, 28);
            Controls.Add(subtitle);

            _pulse = new SoundPulseControl();
            _pulse.SetBounds(36, 130, 448, 72);
            Controls.Add(_pulse);

            _stageLabel = new Label();
            _stageLabel.Text = "Loading sound library";
            _stageLabel.ForeColor = UiTheme.TextMuted;
            _stageLabel.TextAlign = ContentAlignment.MiddleLeft;
            _stageLabel.SetBounds(36, 232, 448, 34);
            Controls.Add(_stageLabel);
        }

        private static Image LoadExecutableIcon()
        {
            try
            {
                using (Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath))
                {
                    if (icon != null)
                    {
                        return icon.ToBitmap();
                    }
                }
            }
            catch
            {
            }
            return SystemIcons.Application.ToBitmap();
        }

        public void SetStage(string stage)
        {
            _stageLabel.Text = stage;
            _pulse.Pulse(0.75f);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen border = new Pen(UiTheme.Border, 1.0f))
            {
                e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
            }
            using (SolidBrush brush = new SolidBrush(UiTheme.Action))
            {
                e.Graphics.FillRectangle(brush, 36, 284, 94, 3);
            }
        }
    }

    internal sealed class StartupTrailerForm : Form
    {
        private readonly System.Windows.Controls.MediaElement _media;
        private readonly Label _stageLabel;
        private bool _completionRaised;
        private bool _failureRaised;

        public event EventHandler Finished;
        public event EventHandler PlaybackFailed;

        public StartupTrailerForm(string videoPath)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            KeyPreview = true;
            ClientSize = new Size(960, 600);
            MinimumSize = ClientSize;
            MaximumSize = ClientSize;
            BackColor = UiTheme.Canvas;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = UiTheme.BodyFont(9.0f, FontStyle.Regular);

            System.Windows.Forms.Integration.ElementHost mediaHost =
                new System.Windows.Forms.Integration.ElementHost();
            mediaHost.SetBounds(0, 0, 960, 540);
            mediaHost.BackColor = UiTheme.Canvas;

            System.Windows.Controls.Grid mediaSurface = new System.Windows.Controls.Grid();
            mediaSurface.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(17, 20, 18));
            _media = new System.Windows.Controls.MediaElement();
            _media.LoadedBehavior = System.Windows.Controls.MediaState.Manual;
            _media.UnloadedBehavior = System.Windows.Controls.MediaState.Stop;
            _media.Stretch = System.Windows.Media.Stretch.Uniform;
            _media.Volume = 0.72;
            _media.ScrubbingEnabled = true;
            _media.Source = new Uri(videoPath, UriKind.Absolute);
            _media.MediaEnded += delegate { RaiseFinished(); };
            _media.MediaFailed += delegate { RaisePlaybackFailed(); };
            mediaSurface.Children.Add(_media);
            mediaHost.Child = mediaSurface;
            Controls.Add(mediaHost);

            Panel footer = new Panel();
            footer.BackColor = UiTheme.Surface;
            footer.SetBounds(0, 540, 960, 60);
            Controls.Add(footer);

            _stageLabel = new Label();
            _stageLabel.Text = "Loading sound library";
            _stageLabel.ForeColor = UiTheme.TextMuted;
            _stageLabel.TextAlign = ContentAlignment.MiddleLeft;
            _stageLabel.SetBounds(24, 12, 690, 36);
            footer.Controls.Add(_stageLabel);

            ThemeButton skip = new ThemeButton();
            skip.Text = "Skip intro";
            skip.SetBounds(808, 10, 128, 40);
            skip.Click += delegate { RaiseFinished(); };
            footer.Controls.Add(skip);

            KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    RaiseFinished();
                    e.Handled = true;
                }
            };
            Shown += delegate
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    try
                    {
                        _media.Play();
                    }
                    catch
                    {
                        RaisePlaybackFailed();
                    }
                });
            };
        }

        public void SetStage(string stage)
        {
            _stageLabel.Text = stage;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen border = new Pen(UiTheme.Border, 1.0f))
            {
                e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                _media.Stop();
            }
            catch
            {
            }
            base.OnFormClosed(e);
        }

        private void RaiseFinished()
        {
            if (_completionRaised)
            {
                return;
            }
            _completionRaised = true;
            EventHandler handler = Finished;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void RaisePlaybackFailed()
        {
            if (_failureRaised)
            {
                return;
            }
            _failureRaised = true;
            EventHandler handler = PlaybackFailed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }

    internal sealed class CreamyKeysApplicationContext : ApplicationContext
    {
        private readonly EventWaitHandle _activationEvent;
        private readonly System.Windows.Forms.Timer _timer;
        private readonly System.Windows.Forms.Timer _activationTimer;
        private Form _startupForm;
        private SplashForm _splash;
        private StartupTrailerForm _trailer;
        private MainForm _main;
        private int _ticks;
        private bool _trailerFinished;
        private bool _mainShown;
        private bool _activationPending;

        public CreamyKeysApplicationContext(EventWaitHandle activationEvent)
        {
            _activationEvent = activationEvent;
            ShowStartupPresentation();
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 45;
            _timer.Tick += Advance;
            _timer.Start();

            _activationTimer = new System.Windows.Forms.Timer();
            _activationTimer.Interval = 120;
            _activationTimer.Tick += CheckActivationRequest;
            _activationTimer.Start();
        }

        private void Advance(object sender, EventArgs e)
        {
            _ticks++;
            if (_ticks == 4)
            {
                SetStage("Preparing audio engine");
            }
            else if (_ticks == 9)
            {
                SetStage("Connecting keyboard and mouse");
                try
                {
                    _main = new MainForm();
                }
                catch (Exception ex)
                {
                    _timer.Stop();
                    CloseStartupPresentation();
                    MessageBox.Show("CreamyKeys could not start:\r\n" + ex.Message,
                        "CreamyKeys", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ExitThread();
                }
            }
            else if (_ticks == 16 && _main != null)
            {
                SetStage("Opening workspace");
            }
            else if (_ticks >= 20 && _main != null &&
                (_trailer == null || _trailerFinished || _ticks >= 180))
            {
                FinishStartup();
            }
        }

        private void ShowStartupPresentation()
        {
            string videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "assets", "ui", "startup-intro.mp4");
            if (File.Exists(videoPath))
            {
                try
                {
                    _trailer = new StartupTrailerForm(videoPath);
                    _trailer.Finished += delegate
                    {
                        _trailerFinished = true;
                        if (_main != null && _ticks >= 20)
                        {
                            FinishStartup();
                        }
                    };
                    _trailer.PlaybackFailed += delegate { SwitchToFallbackSplash(); };
                    _startupForm = _trailer;
                    _startupForm.Show();
                    return;
                }
                catch
                {
                    _trailer = null;
                }
            }

            SwitchToFallbackSplash();
        }

        private void SwitchToFallbackSplash()
        {
            if (_mainShown)
            {
                return;
            }
            if (_startupForm != null && !_startupForm.IsDisposed)
            {
                _startupForm.Close();
            }
            _trailer = null;
            _splash = new SplashForm();
            _startupForm = _splash;
            _splash.Show();
        }

        private void SetStage(string stage)
        {
            if (_trailer != null && !_trailer.IsDisposed)
            {
                _trailer.SetStage(stage);
            }
            else if (_splash != null && !_splash.IsDisposed)
            {
                _splash.SetStage(stage);
            }
        }

        private void FinishStartup()
        {
            if (_mainShown || _main == null)
            {
                return;
            }
            _mainShown = true;
            _timer.Stop();
            _timer.Dispose();
            MainForm = _main;
            _main.FormClosed += delegate { ExitThread(); };
            _main.Show();
            CloseStartupPresentation();
            if (_activationPending)
            {
                _activationPending = false;
                _main.ShowFromExternalLaunch();
            }
        }

        private void CloseStartupPresentation()
        {
            if (_startupForm != null && !_startupForm.IsDisposed)
            {
                _startupForm.Close();
            }
            _startupForm = null;
        }

        private void CheckActivationRequest(object sender, EventArgs e)
        {
            if (_activationEvent == null || !_activationEvent.WaitOne(0))
            {
                return;
            }
            if (_mainShown && _main != null && !_main.IsDisposed)
            {
                _main.ShowFromExternalLaunch();
            }
            else
            {
                _activationPending = true;
            }
        }

        protected override void ExitThreadCore()
        {
            _activationTimer.Stop();
            _activationTimer.Dispose();
            CloseStartupPresentation();
            base.ExitThreadCore();
        }
    }

    internal sealed class WelcomeForm : Form
    {
        private readonly SoundPulseControl _pulse;
        private readonly ThemeToggle _doNotShowAgain;

        public WelcomeForm(Icon icon, EventHandler testSound)
        {
            Text = "Welcome to CreamyKeys";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(860, 570);
            BackColor = UiTheme.Canvas;
            ForeColor = UiTheme.TextPrimary;
            Font = UiTheme.BodyFont(9.5f, FontStyle.Regular);
            Icon = icon;

            Panel visual = new Panel();
            visual.BackColor = UiTheme.Surface;
            visual.Dock = DockStyle.Left;
            visual.Width = 320;
            Controls.Add(visual);

            PictureBox mark = new PictureBox();
            mark.Image = icon == null ? SystemIcons.Application.ToBitmap() : icon.ToBitmap();
            mark.SizeMode = PictureBoxSizeMode.Zoom;
            mark.SetBounds(30, 34, 60, 60);
            visual.Controls.Add(mark);

            Label visualTitle = new Label();
            visualTitle.Text = "Every key\nfeels different.";
            visualTitle.ForeColor = UiTheme.TextPrimary;
            visualTitle.Font = UiTheme.DisplayFont(19.0f, FontStyle.Bold);
            visualTitle.SetBounds(30, 126, 258, 116);
            visual.Controls.Add(visualTitle);

            _pulse = new SoundPulseControl();
            _pulse.SetBounds(30, 260, 258, 82);
            visual.Controls.Add(_pulse);

            Label caption = new Label();
            caption.Text = "Randomized samples, pitch, volume and layers create natural variation.";
            caption.ForeColor = UiTheme.TextMuted;
            caption.SetBounds(30, 368, 254, 92);
            visual.Controls.Add(caption);

            Panel content = new Panel();
            content.BackColor = UiTheme.Content;
            content.Dock = DockStyle.Fill;
            content.Padding = new Padding(42, 34, 42, 30);
            Controls.Add(content);
            content.BringToFront();

            Label title = new Label();
            title.Text = "Meet CreamyKeys";
            title.ForeColor = UiTheme.Ink;
            title.Font = UiTheme.DisplayFont(18.0f, FontStyle.Bold);
            title.SetBounds(42, 34, 430, 54);
            content.Controls.Add(title);

            Label intro = new Label();
            intro.Text = "Choose a sound pack, tune how much it varies, then keep CreamyKeys quietly in the tray.";
            intro.ForeColor = UiTheme.InkMuted;
            intro.SetBounds(44, 92, 414, 72);
            content.Controls.Add(intro);

            AddFeature(content, 44, 172, "01", "Pick a feel", "Use separate sound packs for keyboard and mouse.");
            AddFeature(content, 44, 260, "02", "Make it natural", "No-repeat shuffle, pitch, volume and layers add variation.");
            AddFeature(content, 44, 348, "03", "Control where it plays", "Choose which games and apps can trigger sound.");

            _doNotShowAgain = new ThemeToggle();
            _doNotShowAgain.Text = "Do not show again";
            _doNotShowAgain.DarkSurface = false;
            _doNotShowAgain.SetBounds(44, 442, 260, 32);
            content.Controls.Add(_doNotShowAgain);

            ThemeButton test = new ThemeButton();
            test.Text = "Test a sound";
            test.SetBounds(44, 496, 154, 42);
            test.Click += delegate(object sender, EventArgs e)
            {
                _pulse.Pulse(1.0f);
                if (testSound != null)
                {
                    testSound(sender, e);
                }
            };
            content.Controls.Add(test);

            ThemeButton start = new ThemeButton();
            start.Text = "Open CreamyKeys";
            start.Accent = true;
            start.SetBounds(218, 496, 210, 42);
            start.Click += delegate(object sender, EventArgs e)
            {
                DialogResult = DialogResult.OK;
                Close();
            };
            content.Controls.Add(start);

            AcceptButton = start;
            CancelButton = null;
        }

        public bool DoNotShowAgain
        {
            get { return _doNotShowAgain != null && _doNotShowAgain.Checked; }
        }

        private static void AddFeature(Control parent, int x, int y, string number, string title, string text)
        {
            Label index = new Label();
            index.Text = number;
            index.ForeColor = UiTheme.ActionActive;
            index.Font = UiTheme.BodyFont(8.5f, FontStyle.Bold);
            index.SetBounds(x, y + 2, 38, 24);
            parent.Controls.Add(index);

            Label heading = new Label();
            heading.Text = title;
            heading.ForeColor = UiTheme.Ink;
            heading.Font = UiTheme.BodyFont(10.0f, FontStyle.Bold);
            heading.SetBounds(x + 44, y, 360, 26);
            parent.Controls.Add(heading);

            Label body = new Label();
            body.Text = text;
            body.ForeColor = UiTheme.InkMuted;
            body.SetBounds(x + 44, y + 28, 378, 58);
            parent.Controls.Add(body);
        }
    }

    internal sealed class UserGuideForm : Form
    {
        private readonly Label _title;
        private readonly Label _subtitle;
        private readonly Label _body;
        private readonly ThemeButton[] _sectionButtons;

        private static readonly string[] SectionTitles =
        {
            "Quick start",
            "Sound library",
            "Behavior",
            "Allowed apps",
            "Tray and settings"
        };

        private static readonly string[] SectionSubtitles =
        {
            "Get CreamyKeys running in less than a minute.",
            "Choose keyboard and mouse character independently.",
            "Tune repeat handling, layers and motion.",
            "Limit sound playback to the apps you choose.",
            "Keep CreamyKeys handy without keeping the window open."
        };

        private static readonly string[] SectionBodies =
        {
            "1. Open Sound library and choose a keyboard pack.\n\n2. Choose a separate mouse pack and set both volume controls.\n\n3. Press Test sound, then type or click anywhere.\n\n4. Press Save settings when the result feels right.",
            "Keyboard and mouse use separate shuffled sample pools. Volume variation and pitch variation make repeated input sound less mechanical. Key layers and mouse layers can play several samples together; start with one layer, then increase gradually.",
            "Held key repeat controls whether holding a key continues to make sound. Shift / Ctrl / Alt includes modifier keys. Ignore injected input prevents most synthetic keystrokes from triggering audio. Reduce animations keeps the interface still while preserving every feature.",
            "Turn on Use app allow list, press Scan apps, then tick the apps that may trigger CreamyKeys. Add current captures the foreground app. You can still enter an executable name manually when an app is not discovered automatically.",
            "Hide sends the main window to the notification area. Click the CreamyKeys tray icon to open quick controls. Import settings restores a JSON settings file; Export settings creates one you can move to another computer. Welcome pop-up can be enabled again from Behavior."
        };

        public UserGuideForm(Icon icon)
        {
            Text = "CreamyKeys user guide";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(900, 610);
            BackColor = UiTheme.Canvas;
            ForeColor = UiTheme.TextPrimary;
            Font = UiTheme.BodyFont(9.5f, FontStyle.Regular);
            Icon = icon;

            Panel navigation = new Panel();
            navigation.Dock = DockStyle.Left;
            navigation.Width = 244;
            navigation.BackColor = UiTheme.Surface;
            navigation.Padding = new Padding(24);
            Controls.Add(navigation);

            PictureBox mark = new PictureBox();
            mark.Image = icon == null ? SystemIcons.Application.ToBitmap() : icon.ToBitmap();
            mark.SizeMode = PictureBoxSizeMode.Zoom;
            mark.SetBounds(24, 24, 48, 48);
            navigation.Controls.Add(mark);

            Label brand = new Label();
            brand.Text = "User guide";
            brand.Font = UiTheme.DisplayFont(14.0f, FontStyle.Bold);
            brand.ForeColor = UiTheme.TextPrimary;
            brand.SetBounds(84, 26, 136, 38);
            navigation.Controls.Add(brand);

            _sectionButtons = new ThemeButton[SectionTitles.Length];
            for (int i = 0; i < SectionTitles.Length; i++)
            {
                int sectionIndex = i;
                ThemeButton button = new ThemeButton();
                button.Text = SectionTitles[i];
                button.TextAlign = ContentAlignment.MiddleLeft;
                button.Padding = new Padding(14, 0, 0, 0);
                button.SetBounds(24, 112 + (i * 52), 196, 42);
                button.Click += delegate { SelectSection(sectionIndex); };
                navigation.Controls.Add(button);
                _sectionButtons[i] = button;
            }

            Panel content = new Panel();
            content.Dock = DockStyle.Fill;
            content.BackColor = UiTheme.Content;
            Controls.Add(content);
            content.BringToFront();

            _title = new Label();
            _title.Font = UiTheme.DisplayFont(20.0f, FontStyle.Bold);
            _title.ForeColor = UiTheme.Ink;
            _title.SetBounds(46, 42, 560, 54);
            content.Controls.Add(_title);

            _subtitle = new Label();
            _subtitle.Font = UiTheme.BodyFont(10.0f, FontStyle.Regular);
            _subtitle.ForeColor = UiTheme.InkMuted;
            _subtitle.SetBounds(48, 104, 560, 42);
            content.Controls.Add(_subtitle);

            _body = new Label();
            _body.AutoSize = false;
            _body.BackColor = UiTheme.Content;
            _body.ForeColor = UiTheme.Ink;
            _body.Font = UiTheme.BodyFont(10.0f, FontStyle.Regular);
            _body.SetBounds(48, 174, 558, 322);
            _body.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _body.TextAlign = ContentAlignment.TopLeft;
            content.Controls.Add(_body);

            ThemeButton close = new ThemeButton();
            close.Text = "Close";
            close.Accent = true;
            close.DialogResult = DialogResult.OK;
            close.SetBounds(470, 536, 136, 42);
            close.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            content.Controls.Add(close);
            AcceptButton = close;
            CancelButton = close;

            SelectSection(0);
        }

        private void SelectSection(int index)
        {
            if (index < 0 || index >= SectionTitles.Length)
            {
                return;
            }
            _title.Text = SectionTitles[index];
            _subtitle.Text = SectionSubtitles[index];
            _body.Text = SectionBodies[index];
            for (int i = 0; i < _sectionButtons.Length; i++)
            {
                _sectionButtons[i].Accent = i == index;
            }
        }
    }

    internal sealed class PageTransitionOverlay : Control
    {
        private readonly Bitmap _oldFrame;
        private readonly Bitmap _newFrame;
        private readonly System.Windows.Forms.Timer _timer;
        private float _progress;

        private readonly Control _target;
        private readonly int _direction;

        private PageTransitionOverlay(Control target, Control host, Rectangle bounds,
            Bitmap oldFrame, Bitmap newFrame, int direction)
        {
            _target = target;
            _oldFrame = oldFrame;
            _newFrame = newFrame;
            _direction = direction < 0 ? -1 : 1;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
            BackColor = UiTheme.ContentElevated;
            Bounds = bounds;
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 16;
            _timer.Tick += TickAnimation;
        }

        public static void Run(Control target, Action switchPage, bool reducedMotion, int direction)
        {
            if (target == null || target.Parent == null || switchPage == null)
            {
                if (switchPage != null)
                {
                    switchPage();
                }
                return;
            }

            if (reducedMotion || target.Width < 2 || target.Height < 2)
            {
                switchPage();
                return;
            }

            target.Refresh();
            Bitmap oldFrame = new Bitmap(target.Width, target.Height);
            target.DrawToBitmap(oldFrame, target.ClientRectangle);
            switchPage();
            target.Refresh();
            Bitmap newFrame = new Bitmap(target.Width, target.Height);
            target.DrawToBitmap(newFrame, target.ClientRectangle);

            Control host = target.FindForm();
            if (host == null)
            {
                host = target.Parent;
            }
            Point origin = host.PointToClient(target.PointToScreen(Point.Empty));
            Rectangle bounds = new Rectangle(origin, target.Size);
            PageTransitionOverlay overlay = new PageTransitionOverlay(target, host, bounds,
                oldFrame, newFrame, direction);
            host.Controls.Add(overlay);
            overlay.BringToFront();
            overlay._timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            float eased = SpringProgress(_progress);
            float opacity = 1.0f - (float)Math.Pow(1.0f - _progress, 3.0f);
            int travel = Math.Max(24, Math.Min(82, Width / 10));
            int oldX = (int)Math.Round(-_direction * eased * travel);
            int newX = (int)Math.Round(_direction * (1.0f - eased) * travel);

            DrawFrame(e.Graphics, _oldFrame, oldX, 1.0f - opacity);
            DrawFrame(e.Graphics, _newFrame, newX, opacity);
        }

        private void DrawFrame(Graphics graphics, Bitmap frame, int x, float opacity)
        {
            ColorMatrix matrix = new ColorMatrix();
            matrix.Matrix33 = Math.Max(0.0f, Math.Min(1.0f, opacity));
            using (ImageAttributes attributes = new ImageAttributes())
            {
                attributes.SetColorMatrix(matrix);
                graphics.DrawImage(frame, new Rectangle(x, 0, frame.Width, frame.Height),
                    0, 0, frame.Width, frame.Height, GraphicsUnit.Pixel, attributes);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
                _oldFrame.Dispose();
                _newFrame.Dispose();
            }
            base.Dispose(disposing);
        }

        private void TickAnimation(object sender, EventArgs e)
        {
            _progress += 0.0625f;
            if (_progress >= 1.0f)
            {
                _progress = 1.0f;
                _timer.Stop();
                if (Parent != null)
                {
                    Parent.Controls.Remove(this);
                }
                if (_target != null && !_target.IsDisposed)
                {
                    _target.Invalidate(true);
                    _target.Update();
                }
                Dispose();
                return;
            }
            Invalidate();
        }

        private static float SpringProgress(float progress)
        {
            float t = Math.Max(0.0f, Math.Min(1.0f, progress));
            double value = 1.0 - Math.Exp(-7.2 * t) * Math.Cos(10.5 * t);
            return Math.Max(0.0f, Math.Min(1.035f, (float)value));
        }
    }
}
