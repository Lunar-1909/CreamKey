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
        private static int _animationFps = 60;

        public static bool ReducedMotion { get; set; }

        public static int AnimationFps
        {
            get { return _animationFps; }
            set { _animationFps = value == 30 || value == 120 ? value : 60; }
        }

        public static int AnimationInterval
        {
            get { return Math.Max(8, 1000 / Math.Max(1, _animationFps)); }
        }

        public static int IntervalForFps(int fps)
        {
            return Math.Max(8, 1000 / Math.Max(1, fps));
        }

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

    internal interface IAnimationRateControl
    {
        void SetAnimationFps(int fps);
    }

    internal sealed class ThemeButton : Button, IAnimationRateControl
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
            _cornerRadius = 11;
            _animationTimer = new System.Windows.Forms.Timer();
            _animationTimer.Interval = UiTheme.AnimationInterval;
            _animationTimer.Tick += Animate;
        }

        public void SetAnimationFps(int fps)
        {
            _animationTimer.Interval = UiTheme.IntervalForFps(fps);
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
            UpdateHoverMotion();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _targetHover = 0.0f;
            _pressed = false;
            UpdateHoverMotion();
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
            if (UiTheme.ReducedMotion)
            {
                _hoverAmount = _targetHover;
                _animationTimer.Stop();
                Invalidate();
                return;
            }
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

        private void UpdateHoverMotion()
        {
            if (UiTheme.ReducedMotion)
            {
                _hoverAmount = _targetHover;
                _animationTimer.Stop();
                Invalidate();
                return;
            }
            _animationTimer.Start();
        }
    }

    internal sealed class ThemeToggle : CheckBox, IAnimationRateControl
    {
        private readonly System.Windows.Forms.Timer _animationTimer;
        private float _visualValue;
        private float _targetValue;
        private float _animationVelocity;
        private bool _hovered;
        private bool _pressed;
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
            _animationTimer.Interval = UiTheme.AnimationInterval;
            _animationTimer.Tick += Animate;
        }

        public void SetAnimationFps(int fps)
        {
            _animationTimer.Interval = UiTheme.IntervalForFps(fps);
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
            if (!IsHandleCreated || UiTheme.ReducedMotion)
            {
                _visualValue = _targetValue;
                _animationVelocity = 0.0f;
                _animationTimer.Stop();
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
            _pressed = false;
            Invalidate();
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
            bool narrow = !compact && Width < 140;
            bool micro = narrow && Width <= 104;
            int trackWidth = compact
                ? Math.Max(34, Math.Min(58, Width - 2))
                : (micro ? 36 : (narrow ? 42 : 58));
            Rectangle track = new Rectangle(1, 3, trackWidth, 26);
            Color trackOff = _darkSurface
                ? UiTheme.Blend(UiTheme.Border, UiTheme.SurfaceElevated, 0.34f)
                : UiTheme.Blend(UiTheme.ContentBorder, UiTheme.ContentElevated, 0.22f);
            Color trackColor = UiTheme.Blend(trackOff, UiTheme.ActionActive, _visualValue);
            if (_hovered && Enabled)
            {
                trackColor = UiTheme.Blend(trackColor, Color.White, 0.08f);
            }
            if (_pressed && Enabled)
            {
                trackColor = UiTheme.Blend(trackColor, Color.Black, 0.10f);
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

            int knobWidth = compact && trackWidth < 50 ? 20 : (micro ? 24 : (narrow ? 28 : 30));
            int travel = Math.Max(0, track.Width - knobWidth - 4);
            int knobX = track.X + 2 + (int)Math.Round(travel * _visualValue);
            int pressOffset = _pressed ? 1 : 0;
            Rectangle knobShadow = new Rectangle(knobX, track.Y + 4 + pressOffset, knobWidth, track.Height - 3);
            Rectangle knob = new Rectangle(knobX, track.Y + 2 + pressOffset, knobWidth, track.Height - 4);
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

            if (!compact && knob.Width >= 22)
            {
                string state = Checked ? "ON" : "OFF";
                using (Font stateFont = UiTheme.BodyFont(micro ? 5.2f : 6.4f, FontStyle.Bold))
                {
                    TextRenderer.DrawText(e.Graphics, state, stateFont, knob, UiTheme.Ink,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                }
            }

            int textGap = micro ? 6 : (narrow ? 8 : 12);
            Rectangle textBox = new Rectangle(track.Right + textGap, 0,
                Math.Max(1, Width - track.Right - textGap), Height);
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
            if (UiTheme.ReducedMotion)
            {
                _visualValue = _targetValue;
                _animationVelocity = 0.0f;
                _animationTimer.Stop();
                Invalidate();
                return;
            }
            float dt = _animationTimer.Interval / 1000.0f;
            const float stiffness = 520.0f;
            const float damping = 46.0f;
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

    internal sealed class SoundPulseControl : Control, IAnimationRateControl
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
            _timer.Interval = UiTheme.AnimationInterval;
            _timer.Tick += TickAnimation;
            _timer.Start();
        }

        public void SetAnimationFps(int fps)
        {
            _timer.Interval = UiTheme.IntervalForFps(fps);
        }

        public void Pulse(float strength)
        {
            if (UiTheme.ReducedMotion)
            {
                _energy = 0.0f;
                Array.Clear(_levels, 0, _levels.Length);
                Invalidate();
                return;
            }
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
            if (UiTheme.ReducedMotion)
            {
                if (_energy > 0.0f)
                {
                    _energy = 0.0f;
                    Array.Clear(_levels, 0, _levels.Length);
                    Invalidate();
                }
                return;
            }
            _phase += 0.13f;
            _energy *= 0.88f;
            for (int i = 0; i < _levels.Length; i++)
            {
                _levels[i] *= 0.86f;
            }
            Invalidate();
        }
    }

    internal sealed class SlidingIndicator : Control, IAnimationRateControl
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
            _timer.Interval = UiTheme.AnimationInterval;
            _timer.Tick += Animate;
        }

        public void SetAnimationFps(int fps)
        {
            _timer.Interval = UiTheme.IntervalForFps(fps);
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
            float dt = _timer.Interval / 1000.0f;
            const float stiffness = 410.0f;
            const float damping = 41.0f;
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

    internal sealed class StartupCodeBackdrop : Control
    {
        private static readonly string[] SourceLines = new string[]
        {
            "using CreamyKeys.Audio;",
            "var library = SoundLibrary.Scan(assets);",
            "audio.Prepare(library.KeyboardPacks);",
            "audio.Prepare(library.MousePacks);",
            "keyboard.Connect(globalHook);",
            "mouse.Connect(globalHook);",
            "settings.Normalize();",
            "workspace.RestoreLayout();",
            "engine.SetVariation(volume, pitch);",
            "engine.SetLayers(keyLayers, mouseLayers);",
            "tray.RegisterQuickControls();",
            "CreamyKeys.Open();"
        };

        private readonly System.Windows.Forms.Timer _timer;
        private readonly System.Collections.Generic.List<string> _lines;
        private readonly Font _codeFont;
        private int _sourceIndex;
        private int _characterIndex;
        private int _ticks;
        private float _lineOffset;

        public event EventHandler CharacterTyped;

        public StartupCodeBackdrop()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
            BackColor = Color.FromArgb(12, 16, 14);
            _lines = new System.Collections.Generic.List<string>();
            for (int i = 0; i < 6; i++)
            {
                _lines.Add(SourceLines[i]);
            }
            _sourceIndex = 6;
            _codeFont = new Font("Cascadia Mono", 9.0f, FontStyle.Regular, GraphicsUnit.Point);
            if (!string.Equals(_codeFont.Name, "Cascadia Mono", StringComparison.OrdinalIgnoreCase))
            {
                _codeFont.Dispose();
                _codeFont = new Font("Consolas", 9.0f, FontStyle.Regular, GraphicsUnit.Point);
            }
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 16;
            _timer.Tick += Animate;
            _timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int lineHeight = 24;
            int baseline = Height - 34 + (int)Math.Round(_lineOffset);
            using (SolidBrush numberBrush = new SolidBrush(Color.FromArgb(58, 126, 111, 102)))
            using (SolidBrush codeBrush = new SolidBrush(Color.FromArgb(108, 141, 176, 157)))
            using (SolidBrush accentBrush = new SolidBrush(Color.FromArgb(132, 80, 213, 164)))
            {
                int visible = Math.Min(_lines.Count, Math.Max(1, Height / lineHeight + 2));
                for (int i = 0; i < visible; i++)
                {
                    int lineIndex = _lines.Count - visible + i;
                    int y = baseline - ((_lines.Count - lineIndex) * lineHeight);
                    if (y < -lineHeight || y > Height)
                    {
                        continue;
                    }
                    e.Graphics.DrawString((lineIndex + 1).ToString("00"), _codeFont,
                        numberBrush, 24, y);
                    e.Graphics.DrawString(_lines[lineIndex], _codeFont, codeBrush, 62, y);
                }

                string current = SourceLines[_sourceIndex].Substring(0,
                    Math.Min(_characterIndex, SourceLines[_sourceIndex].Length));
                int currentY = baseline - lineHeight;
                e.Graphics.DrawString((_lines.Count + 1).ToString("00"), _codeFont,
                    numberBrush, 24, currentY);
                e.Graphics.DrawString(current + ((_ticks / 22) % 2 == 0 ? "|" : ""),
                    _codeFont, accentBrush, 62, currentY);
            }

            using (LinearGradientBrush fade = new LinearGradientBrush(ClientRectangle,
                Color.FromArgb(224, BackColor), Color.FromArgb(30, BackColor),
                LinearGradientMode.Vertical))
            {
                ColorBlend blend = new ColorBlend();
                blend.Positions = new float[] { 0.0f, 0.34f, 0.72f, 1.0f };
                blend.Colors = new Color[]
                {
                    Color.FromArgb(235, BackColor), Color.FromArgb(110, BackColor),
                    Color.FromArgb(54, BackColor), Color.FromArgb(220, BackColor)
                };
                fade.InterpolationColors = blend;
                e.Graphics.FillRectangle(fade, ClientRectangle);
            }

            Rectangle loadingSurface = new Rectangle(24, Math.Max(20, Height - 190),
                Math.Max(40, Width - 48), 166);
            using (GraphicsPath surfacePath = UiTheme.RoundedRect(loadingSurface, 12))
            using (SolidBrush surfaceBrush = new SolidBrush(Color.FromArgb(238, 12, 16, 14)))
            using (Pen surfaceBorder = new Pen(Color.FromArgb(52, 67, 59), 1.0f))
            {
                e.Graphics.FillPath(surfaceBrush, surfacePath);
                e.Graphics.DrawPath(surfaceBorder, surfacePath);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
                _codeFont.Dispose();
            }
            base.Dispose(disposing);
        }

        private void Animate(object sender, EventArgs e)
        {
            _ticks++;
            if (_ticks % 2 == 0)
            {
                _characterIndex++;
                EventHandler typed = CharacterTyped;
                if (typed != null)
                {
                    typed(this, EventArgs.Empty);
                }
                if (_characterIndex > SourceLines[_sourceIndex].Length + 5)
                {
                    _lines.Add(SourceLines[_sourceIndex]);
                    if (_lines.Count > 22)
                    {
                        _lines.RemoveAt(0);
                    }
                    _sourceIndex = (_sourceIndex + 1) % SourceLines.Length;
                    _characterIndex = 0;
                    _lineOffset = 18.0f;
                }
            }
            _lineOffset *= 0.78f;
            Invalidate();
        }
    }

    internal sealed class SegmentedLoadingBar : Control
    {
        private readonly System.Windows.Forms.Timer _timer;
        private float _visualValue;
        private float _targetValue;
        private float _velocity;

        public SegmentedLoadingBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
            BackColor = Color.FromArgb(12, 16, 14);
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 16;
            _timer.Tick += Animate;
        }

        public int Value
        {
            get { return (int)Math.Round(_targetValue * 100.0f); }
            set
            {
                _targetValue = Math.Max(0.0f, Math.Min(1.0f, value / 100.0f));
                _timer.Start();
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int percentWidth = 70;
            Rectangle track = new Rectangle(percentWidth, 9,
                Math.Max(20, Width - percentWidth - 2), Math.Max(12, Height - 18));
            using (Font percentFont = UiTheme.BodyFont(9.0f, FontStyle.Bold))
            {
                TextRenderer.DrawText(e.Graphics,
                    ((int)Math.Round(_visualValue * 100.0f)).ToString() + "%",
                    percentFont, new Rectangle(0, 0, percentWidth - 8, Height),
                    UiTheme.TextPrimary, TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine);
            }

            using (GraphicsPath trackPath = UiTheme.RoundedRect(track, track.Height / 2))
            using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(34, 45, 40)))
            using (Pen border = new Pen(Color.FromArgb(62, 78, 69)))
            {
                e.Graphics.FillPath(trackBrush, trackPath);
                e.Graphics.DrawPath(border, trackPath);
            }

            int filledWidth = (int)Math.Round((track.Width - 4) * _visualValue);
            if (filledWidth <= 2)
            {
                return;
            }
            Rectangle fillBounds = new Rectangle(track.X + 2, track.Y + 2,
                filledWidth, track.Height - 4);
            GraphicsState state = e.Graphics.Save();
            using (GraphicsPath clip = UiTheme.RoundedRect(
                new Rectangle(track.X + 2, track.Y + 2, track.Width - 4, track.Height - 4),
                Math.Max(2, (track.Height - 4) / 2)))
            {
                e.Graphics.SetClip(clip);
                int segment = 22;
                for (int x = fillBounds.X - segment; x < fillBounds.Right + segment; x += segment)
                {
                    GraphicsPath stripe = new GraphicsPath();
                    stripe.AddPolygon(new Point[]
                    {
                        new Point(x + 8, fillBounds.Y),
                        new Point(x + segment, fillBounds.Y),
                        new Point(x + segment - 8, fillBounds.Bottom),
                        new Point(x - 1, fillBounds.Bottom)
                    });
                    float t = Math.Max(0.0f, Math.Min(1.0f,
                        (x - fillBounds.X) / (float)Math.Max(1, track.Width)));
                    Color color = UiTheme.Blend(Color.FromArgb(41, 190, 222), UiTheme.Action, t);
                    using (SolidBrush brush = new SolidBrush(color))
                    {
                        e.Graphics.FillPath(brush, stripe);
                    }
                    stripe.Dispose();
                }
            }
            e.Graphics.Restore(state);
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
            float dt = _timer.Interval / 1000.0f;
            const float stiffness = 310.0f;
            const float damping = 35.0f;
            float distance = _targetValue - _visualValue;
            _velocity += (stiffness * distance - damping * _velocity) * dt;
            _visualValue += _velocity * dt;
            if (Math.Abs(distance) < 0.001f && Math.Abs(_velocity) < 0.01f)
            {
                _visualValue = _targetValue;
                _velocity = 0.0f;
                _timer.Stop();
            }
            Invalidate();
        }
    }

    internal sealed class SplashForm : Form
    {
        private readonly Label _stageLabel;
        private readonly SegmentedLoadingBar _progress;
        private readonly System.Media.SoundPlayer _typingSound;
        private int _typedCharacters;

        public SplashForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            ClientSize = new Size(780, 440);
            BackColor = Color.FromArgb(12, 16, 14);
            Font = UiTheme.BodyFont(9.0f, FontStyle.Regular);
            AutoScaleMode = AutoScaleMode.Dpi;
            DoubleBuffered = true;

            StartupCodeBackdrop code = new StartupCodeBackdrop();
            code.Dock = DockStyle.Fill;
            code.CharacterTyped += OnCharacterTyped;
            Controls.Add(code);

            PictureBox mark = new PictureBox();
            mark.Image = LoadExecutableIcon();
            mark.SizeMode = PictureBoxSizeMode.Zoom;
            mark.BackColor = Color.Transparent;
            mark.SetBounds(44, 274, 52, 52);
            code.Controls.Add(mark);

            Label title = new Label();
            title.Text = "CreamKey";
            title.ForeColor = UiTheme.TextPrimary;
            title.BackColor = Color.Transparent;
            title.Font = UiTheme.DisplayFont(20.0f, FontStyle.Bold);
            title.UseCompatibleTextRendering = true;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.SetBounds(112, 268, 360, 58);
            code.Controls.Add(title);

            _stageLabel = new Label();
            _stageLabel.Text = "Loading sound library";
            _stageLabel.ForeColor = UiTheme.TextMuted;
            _stageLabel.BackColor = Color.Transparent;
            _stageLabel.TextAlign = ContentAlignment.MiddleLeft;
            _stageLabel.SetBounds(44, 328, 692, 28);
            code.Controls.Add(_stageLabel);

            _progress = new SegmentedLoadingBar();
            _progress.SetBounds(44, 362, 692, 46);
            _progress.Value = 7;
            code.Controls.Add(_progress);

            string soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "assets", "keyboards", "cherrymx_black_abs", "key_01.wav");
            if (File.Exists(soundPath))
            {
                _typingSound = new System.Media.SoundPlayer(soundPath);
                try { _typingSound.LoadAsync(); } catch { }
            }
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
            SetStage(stage, _progress.Value);
        }

        public void SetStage(string stage, int progress)
        {
            _stageLabel.Text = stage;
            _progress.Value = progress;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen border = new Pen(UiTheme.Border, 1.0f))
            {
                e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
            }
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(12, 16, 14)))
            {
                e.Graphics.FillRectangle(brush, 0, 0, 1, 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _typingSound != null)
            {
                _typingSound.Stop();
                _typingSound.Dispose();
            }
            base.Dispose(disposing);
        }

        private void OnCharacterTyped(object sender, EventArgs e)
        {
            _typedCharacters++;
            if (_typingSound == null || _typedCharacters % 4 != 0)
            {
                return;
            }
            try { _typingSound.Play(); } catch { }
        }
    }

    internal sealed class CreamyKeysApplicationContext : ApplicationContext
    {
        private readonly EventWaitHandle _activationEvent;
        private readonly System.Windows.Forms.Timer _timer;
        private readonly System.Windows.Forms.Timer _activationTimer;
        private SplashForm _splash;
        private MainForm _main;
        private int _ticks;
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
            if (_ticks == 3)
            {
                SetStage("Scanning sound packs", 24);
            }
            else if (_ticks == 6)
            {
                SetStage("Preparing audio engine", 43);
            }
            else if (_ticks == 9)
            {
                SetStage("Connecting keyboard and mouse", 66);
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
            else if (_ticks == 13 && _main != null)
            {
                SetStage("Opening workspace", 88);
            }
            else if (_ticks == 17 && _main != null)
            {
                SetStage("Ready", 100);
            }
            else if (_ticks >= 20 && _main != null)
            {
                FinishStartup();
            }
        }

        private void ShowStartupPresentation()
        {
            _splash = new SplashForm();
            _splash.Show();
        }

        private void SetStage(string stage, int progress)
        {
            if (_splash != null && !_splash.IsDisposed)
            {
                _splash.SetStage(stage, progress);
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
            if (_splash != null && !_splash.IsDisposed)
            {
                _splash.Close();
            }
            _splash = null;
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

    internal sealed class LoopingVideoSurface : Panel
    {
        private readonly PictureBox _poster;
        private readonly System.Windows.Forms.Integration.ElementHost _mediaHost = null;
        private readonly System.Windows.Controls.MediaElement _media = null;
        private readonly bool _captureMode;
        private bool _muted;

        public LoopingVideoSurface(string videoPath, string posterPath)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = UiTheme.Canvas;
            _muted = true;
            _captureMode = IsCaptureMode();

            _poster = new PictureBox();
            _poster.BackColor = UiTheme.Canvas;
            _poster.SizeMode = PictureBoxSizeMode.Zoom;
            _poster.Image = LoadImage(posterPath);
            Controls.Add(_poster);

            if (_captureMode || string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
            {
                return;
            }

            _mediaHost = new System.Windows.Forms.Integration.ElementHost();
            _mediaHost.BackColor = UiTheme.Canvas;
            _mediaHost.Visible = false;

            System.Windows.Controls.Grid root = new System.Windows.Controls.Grid();
            root.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(17, 20, 18));

            _media = new System.Windows.Controls.MediaElement();
            _media.LoadedBehavior = System.Windows.Controls.MediaState.Manual;
            _media.UnloadedBehavior = System.Windows.Controls.MediaState.Stop;
            _media.Stretch = System.Windows.Media.Stretch.Uniform;
            _media.ScrubbingEnabled = true;
            _media.Volume = 0.0;
            _media.Source = new Uri(videoPath, UriKind.Absolute);
            _media.MediaOpened += delegate
            {
                if (IsDisposed)
                {
                    return;
                }
                _poster.Visible = false;
                _mediaHost.Visible = true;
                _mediaHost.BringToFront();
            };
            _media.MediaEnded += delegate
            {
                try
                {
                    _media.Position = TimeSpan.Zero;
                    _media.Play();
                }
                catch
                {
                    ShowPosterFallback();
                }
            };
            _media.MediaFailed += delegate { ShowPosterFallback(); };
            root.Children.Add(_media);

            _mediaHost.Child = root;
            Controls.Add(_mediaHost);
            _mediaHost.BringToFront();
        }

        public bool Muted
        {
            get { return _muted; }
            set
            {
                _muted = value;
                if (_media != null)
                {
                    _media.Volume = value ? 0.0 : 0.72;
                }
            }
        }

        public void StartPlayback()
        {
            if (_media == null || _captureMode)
            {
                return;
            }
            try
            {
                _media.Play();
            }
            catch
            {
                ShowPosterFallback();
            }
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            Rectangle childBounds = new Rectangle(1, 1,
                Math.Max(1, ClientSize.Width - 2), Math.Max(1, ClientSize.Height - 2));
            _poster.Bounds = childBounds;
            if (_mediaHost != null)
            {
                _mediaHost.Bounds = childBounds;
            }

            Region oldRegion = Region;
            using (GraphicsPath path = UiTheme.RoundedRect(ClientRectangle, 12))
            {
                Region = new Region(path);
            }
            if (oldRegion != null)
            {
                oldRegion.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (_media != null)
                    {
                        _media.Stop();
                    }
                }
                catch
                {
                }
                if (_poster.Image != null)
                {
                    _poster.Image.Dispose();
                    _poster.Image = null;
                }
            }
            base.Dispose(disposing);
        }

        private void ShowPosterFallback()
        {
            if (IsDisposed)
            {
                return;
            }
            _mediaHost.Visible = false;
            _poster.Visible = true;
            _poster.BringToFront();
        }

        private static Image LoadImage(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }
            try
            {
                using (Image source = Image.FromFile(path))
                {
                    return new Bitmap(source);
                }
            }
            catch
            {
                return null;
            }
        }

        private static bool IsCaptureMode()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--capture-ui", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal sealed class WelcomeForm : Form
    {
        private readonly SoundPulseControl _pulse;
        private readonly ThemeToggle _doNotShowAgain;
        private readonly ThemeToggle _trailerSound;
        private readonly LoopingVideoSurface _trailer;

        public WelcomeForm(Icon icon, EventHandler testSound)
        {
            Text = "CreamyKeys Trailer";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            KeyPreview = true;
            ClientSize = new Size(1080, 640);
            BackColor = UiTheme.Canvas;
            ForeColor = UiTheme.TextPrimary;
            Font = UiTheme.BodyFont(9.5f, FontStyle.Regular);
            Icon = icon;

            PictureBox mark = new PictureBox();
            mark.Image = icon == null ? SystemIcons.Application.ToBitmap() : icon.ToBitmap();
            mark.SizeMode = PictureBoxSizeMode.Zoom;
            mark.SetBounds(26, 18, 44, 44);
            Controls.Add(mark);

            Label brand = new Label();
            brand.Text = "CreamyKeys";
            brand.ForeColor = UiTheme.TextPrimary;
            brand.Font = UiTheme.DisplayFont(13.0f, FontStyle.Bold);
            brand.SetBounds(84, 14, 240, 30);
            Controls.Add(brand);

            Label brandCaption = new Label();
            brandCaption.Text = "Trailer  /  desktop sound studio";
            brandCaption.ForeColor = UiTheme.TextMuted;
            brandCaption.SetBounds(86, 42, 340, 24);
            Controls.Add(brandCaption);

            Label live = new Label();
            live.Text = "●  TRAILER";
            live.ForeColor = UiTheme.Action;
            live.Font = UiTheme.BodyFont(8.2f, FontStyle.Bold);
            live.TextAlign = ContentAlignment.MiddleRight;
            live.SetBounds(820, 24, 234, 28);
            Controls.Add(live);

            string uiAssets = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "ui");
            _trailer = new LoopingVideoSurface(
                Path.Combine(uiAssets, "welcome-trailer-1080p.mp4"),
                Path.Combine(uiAssets, "welcome-trailer-poster.png"));
            _trailer.SetBounds(26, 88, 650, 366);
            Controls.Add(_trailer);

            Label trailerCaption = new Label();
            trailerCaption.Text = "CREAMYKEYS TRAILER";
            trailerCaption.ForeColor = UiTheme.TextMuted;
            trailerCaption.Font = UiTheme.BodyFont(7.8f, FontStyle.Bold);
            trailerCaption.SetBounds(28, 466, 620, 24);
            Controls.Add(trailerCaption);

            _pulse = new SoundPulseControl();
            _pulse.BackColor = UiTheme.Surface;
            _pulse.SetBounds(26, 500, 650, 68);
            Controls.Add(_pulse);

            Label pulseCaption = new Label();
            pulseCaption.Text = "Press Test sound to hear a shuffled sample and watch the live signal respond.";
            pulseCaption.ForeColor = UiTheme.TextMuted;
            pulseCaption.SetBounds(28, 580, 630, 34);
            Controls.Add(pulseCaption);

            ModernPanel content = new ModernPanel();
            content.FillColor = UiTheme.SurfaceElevated;
            content.BorderColor = UiTheme.Border;
            content.CornerRadius = 12;
            content.SetBounds(702, 88, 352, 526);
            Controls.Add(content);

            Label eyebrow = new Label();
            eyebrow.Text = "START HERE";
            eyebrow.ForeColor = UiTheme.Action;
            eyebrow.Font = UiTheme.BodyFont(8.0f, FontStyle.Bold);
            eyebrow.SetBounds(24, 22, 284, 24);
            content.Controls.Add(eyebrow);

            Label title = new Label();
            title.Text = "Set the feel.";
            title.ForeColor = UiTheme.TextPrimary;
            title.Font = UiTheme.DisplayFont(18.0f, FontStyle.Bold);
            title.UseCompatibleTextRendering = true;
            title.SetBounds(24, 48, 294, 72);
            content.Controls.Add(title);

            Label intro = new Label();
            intro.Text = "Choose a sound pack, tune its variation, then leave CreamyKeys running quietly in the tray.";
            intro.ForeColor = UiTheme.TextMuted;
            intro.SetBounds(24, 126, 296, 58);
            content.Controls.Add(intro);

            AddFeature(content, 24, 194, "01", "Pick a feel", "Separate keyboard and mouse packs.");
            AddFeature(content, 24, 256, "02", "Make it natural", "Shuffle, pitch, volume and layers.");
            AddFeature(content, 24, 318, "03", "Choose where it plays", "Allow games and apps from one list.");

            _doNotShowAgain = new ThemeToggle();
            _doNotShowAgain.Text = "Do not show again";
            _doNotShowAgain.DarkSurface = true;
            _doNotShowAgain.BackColor = UiTheme.SurfaceElevated;
            _doNotShowAgain.SetBounds(24, 384, 296, 32);
            content.Controls.Add(_doNotShowAgain);

            _trailerSound = new ThemeToggle();
            _trailerSound.Text = "Trailer sound";
            _trailerSound.DarkSurface = true;
            _trailerSound.BackColor = UiTheme.SurfaceElevated;
            _trailerSound.SetBounds(24, 422, 296, 32);
            _trailerSound.CheckedChanged += delegate { _trailer.Muted = !_trailerSound.Checked; };
            content.Controls.Add(_trailerSound);

            ThemeButton test = new ThemeButton();
            test.Text = "Test sound";
            test.SetBounds(24, 468, 118, 42);
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
            start.Font = UiTheme.BodyFont(8.0f, FontStyle.Bold);
            start.SetBounds(152, 468, 176, 42);
            start.Click += delegate
            {
                DialogResult = DialogResult.OK;
                Close();
            };
            content.Controls.Add(start);

            Shown += delegate
            {
                BeginInvoke((MethodInvoker)delegate { _trailer.StartPlayback(); });
            };
            KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    Close();
                    e.Handled = true;
                }
            };
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
            index.ForeColor = UiTheme.Action;
            index.Font = UiTheme.BodyFont(8.2f, FontStyle.Bold);
            index.TextAlign = ContentAlignment.TopLeft;
            index.SetBounds(x, y + 1, 38, 24);
            parent.Controls.Add(index);

            Label heading = new Label();
            heading.Text = title;
            heading.ForeColor = UiTheme.TextPrimary;
            heading.Font = UiTheme.BodyFont(9.4f, FontStyle.Bold);
            heading.SetBounds(x + 42, y, 248, 24);
            parent.Controls.Add(heading);

            Label body = new Label();
            body.Text = text;
            body.ForeColor = UiTheme.TextMuted;
            body.SetBounds(x + 42, y + 25, 252, 28);
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
            "Settings and tray"
        };

        private static readonly string[] SectionSubtitles =
        {
            "Get CreamyKeys running in less than a minute.",
            "Choose keyboard and mouse character independently.",
            "Tune repeat handling, layers and input rules.",
            "Limit sound playback to the apps you choose.",
            "Keep CreamyKeys handy without keeping the window open."
        };

        private static readonly string[] SectionBodies =
        {
            "1. Open Sound library and choose a keyboard pack.\n\n2. Choose a separate mouse pack and set both volume controls.\n\n3. Press Test sound, then type or click anywhere.\n\n4. Press Save settings when the result feels right.",
            "Keyboard and mouse use separate shuffled sample pools. Volume variation and pitch variation make repeated input sound less mechanical. Key layers and mouse layers can play several samples together; start with one layer, then increase gradually.",
            "Held key repeat controls whether holding a key continues to make sound. Shift / Ctrl / Alt includes modifier keys. Ignore injected input prevents most synthetic keystrokes from triggering audio. Use the + tile on Dashboard to add a physical key to the custom virtual row.",
            "Turn on Use app allow list, press Scan apps, then tick the apps that may trigger CreamyKeys. Add current captures the foreground app. You can still enter an executable name manually when an app is not discovered automatically.",
            "Hide sends the main window to the notification area. Click the CreamyKeys tray icon to open quick controls. Settings contains animation FPS, reduced motion, Trailer startup, JSON import/export, the user guide and the config folder."
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
        private static PageTransitionOverlay _activeOverlay;
        private readonly Bitmap _oldFrame;
        private readonly Bitmap _newFrame;
        private readonly System.Windows.Forms.Timer _timer;
        private float _progress;
        private float _velocity;

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
            Bitmap oldFrame;
            PageTransitionOverlay active = _activeOverlay;
            if (active != null && !active.IsDisposed && active._target == target &&
                active.Width == target.Width && active.Height == target.Height)
            {
                oldFrame = active.CaptureCurrentFrame();
                active.RemoveOverlay(false);
            }
            else
            {
                if (active != null && !active.IsDisposed)
                {
                    active.RemoveOverlay(true);
                }
                oldFrame = new Bitmap(target.Width, target.Height);
                target.DrawToBitmap(oldFrame, target.ClientRectangle);
            }
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
            _activeOverlay = overlay;
            host.Controls.Add(overlay);
            overlay.BringToFront();
            overlay._timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            float eased = Math.Max(0.0f, Math.Min(1.0f, _progress));
            float opacity = eased * eased * (3.0f - (2.0f * eased));
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
                if (ReferenceEquals(_activeOverlay, this))
                {
                    _activeOverlay = null;
                }
            }
            base.Dispose(disposing);
        }

        private void TickAnimation(object sender, EventArgs e)
        {
            const float dt = 0.016f;
            const float stiffness = 360.0f;
            const float damping = 38.0f;
            float distance = 1.0f - _progress;
            _velocity += (stiffness * distance - damping * _velocity) * dt;
            _progress += _velocity * dt;
            if (Math.Abs(distance) < 0.002f && Math.Abs(_velocity) < 0.025f)
            {
                _progress = 1.0f;
                _velocity = 0.0f;
                RemoveOverlay(true);
                return;
            }
            Invalidate();
        }

        private Bitmap CaptureCurrentFrame()
        {
            Bitmap frame = new Bitmap(Math.Max(1, Width), Math.Max(1, Height));
            DrawToBitmap(frame, ClientRectangle);
            return frame;
        }

        private void RemoveOverlay(bool refreshTarget)
        {
            _timer.Stop();
            if (Parent != null)
            {
                Parent.Controls.Remove(this);
            }
            if (refreshTarget && _target != null && !_target.IsDisposed)
            {
                _target.Invalidate(true);
                _target.Update();
            }
            Dispose();
        }
    }
}
