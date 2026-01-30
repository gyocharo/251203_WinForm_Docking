using PureGate.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using PureGate.UIControl;

namespace PureGate
{
    public partial class LoginForm : Form
    {
        public bool LoginSucceeded { get; private set; }

        private bool _dragging;
        private Point _dragStart;

        private TitleBar _title;
        private WindowButton _btnMin;
        private WindowButton _btnClose;

        private SvgLikeLogo _logo;
        private RoundedTextBox _pwBox;
        private LinkLabel _lnkForgot;
        private PillButton _btnLogin;

        // Theme
        private readonly Color _border = Color.FromArgb(210, 220, 230);
        private readonly Color _bgTop = Color.FromArgb(245, 250, 255);
        private readonly Color _bgBottom = Color.White;

        private readonly Color _titleTop = Color.FromArgb(45, 120, 185);
        private readonly Color _titleBottom = Color.FromArgb(25, 95, 165);

        // “컨텐츠”는 여기 위에서만 그라데이션을 그림 (폼 자체는 테두리만 담당)
        private GradientSurface _surface;

        public LoginForm()
        {
            InitializeComponent();
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Login";
            StartPosition = FormStartPosition.CenterScreen;

            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;

            // ✅ 폼은 테두리만 담당 (배경은 surface가 담당)
            Padding = new Padding(1);
            BackColor = _border;
            ClientSize = new Size(520, 260);

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    Close();
                }
            };

            // ✅ 이 안에서만 그라데이션 배경을 그림
            _surface = new GradientSurface(_bgTop, _bgBottom)
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(_surface);

            // TitleBar
            _title = new TitleBar(_titleTop, _titleBottom)
            {
                Dock = DockStyle.Top,
                Height = 28
            };
            _surface.Controls.Add(_title);

            _title.MouseDown += Title_MouseDown;
            _title.MouseMove += Title_MouseMove;
            _title.MouseUp += Title_MouseUp;

            // Window buttons (투명 사용 안 함, 타이틀바와 동일 그라데이션 직접 그림)
            _btnClose = new WindowButton(WindowButtonKind.Close, _titleTop, _titleBottom)
            {
                Size = new Size(40, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnClose.Click += (s, e) => Close();
            _title.Controls.Add(_btnClose);

            _btnMin = new WindowButton(WindowButtonKind.Minimize, _titleTop, _titleBottom)
            {
                Size = new Size(40, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;
            _title.Controls.Add(_btnMin);

            Action layoutTitleButtons = () =>
            {
                _btnClose.Location = new Point(_title.Width - _btnClose.Width, 0);
                _btnMin.Location = new Point(_title.Width - _btnClose.Width - _btnMin.Width, 0);
            };
            layoutTitleButtons();
            _title.SizeChanged += (s, e) => layoutTitleButtons();

            // Logo
            _logo = new SvgLikeLogo
            {
                Size = new Size(115, 125),
                Location = new Point(55, 70)
            };
            _surface.Controls.Add(_logo);

            // Password box
            _pwBox = new RoundedTextBox
            {
                Size = new Size(260, 30),
                Location = new Point(200, 108),
                Radius = 12,
                BorderColor = Color.FromArgb(70, 120, 170),
                BorderWidth = 2f,
                FillColor = Color.White
            };
            _pwBox.SetPlaceholder("Password", true);
            _surface.Controls.Add(_pwBox);

            // Forgot link (LinkLabel은 투명 지원함)
            _lnkForgot = new LinkLabel
            {
                Text = "Forgot Password?",
                AutoSize = true,
                Location = new Point(340, 146),
                Font = new Font("Segoe UI", 8.5f),
                LinkColor = Color.FromArgb(70, 145, 210),
                ActiveLinkColor = Color.FromArgb(40, 110, 175),
                VisitedLinkColor = Color.FromArgb(70, 145, 210),
                LinkBehavior = LinkBehavior.HoverUnderline,
                TabStop = false,
                BackColor = Color.Transparent
            };
            _lnkForgot.LinkClicked += (s, e) => MsgBox.Show("비밀번호 찾기 기능 연결하면 됨");
            _surface.Controls.Add(_lnkForgot);

            // Login button (✅ Button 상속 안 함: 잔상/클리핑 원천 차단)
            _btnLogin = new PillButton
            {
                Text = "LOG IN",
                Size = new Size(150, 34),
                Location = new Point(245, 182),
                Radius = 17,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TabStop = false
            };
            _btnLogin.Click += BtnLogin_Click;
            _surface.Controls.Add(_btnLogin);

            // Enter로 로그인
            AcceptButton = _btnLogin;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!LoginSucceeded)
            {
                var result = MsgBox.Show(
                    this,
                    "정말 종료하시겠습니까?",
                    "종료 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string pw = _pwBox.GetValueOrEmpty();

            if (string.IsNullOrWhiteSpace(pw))
            {
                MsgBox.Show("비밀번호를 입력하세요.");
                return;
            }

            if (ValidatePassword(pw))
            {
                LoginSucceeded = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MsgBox.Show("비밀번호가 틀렸습니다.");
                _pwBox.ClearAndFocus();
            }
        }

        private bool ValidatePassword(string pw)
        {
            return pw == "1234";
        }

        private void Title_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _dragging = true;
            _dragStart = new Point(e.X, e.Y);
        }

        private void Title_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            var screenPos = PointToScreen(e.Location);
            Location = new Point(screenPos.X - _dragStart.X, screenPos.Y - _dragStart.Y);
        }

        private void Title_MouseUp(object sender, MouseEventArgs e)
        {
            _dragging = false;
        }

        private static GraphicsPath CreateRoundPath(Rectangle r, int radius)
        {
            var path = new GraphicsPath();

            int rr = radius;
            int max = Math.Min(r.Width, r.Height) / 2;
            if (rr > max) rr = max;
            if (rr < 0) rr = 0;

            int d = rr * 2;
            if (d == 0)
            {
                path.AddRectangle(r);
                path.CloseFigure();
                return path;
            }

            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // =========================
        //  Background Surface
        // =========================
        private sealed class GradientSurface : Panel
        {
            private readonly Color _top;
            private readonly Color _bottom;

            public GradientSurface(Color top, Color bottom)
            {
                _top = top;
                _bottom = bottom;

                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint |
                         ControlStyles.ResizeRedraw, true);
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                var r = ClientRectangle;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (var br = new LinearGradientBrush(r, _top, _bottom, LinearGradientMode.Vertical))
                    e.Graphics.FillRectangle(br, r);
            }
        }

        // =========================
        //  TitleBar
        // =========================
        private sealed class TitleBar : Panel
        {
            private readonly Color _top;
            private readonly Color _bottom;

            public TitleBar(Color top, Color bottom)
            {
                _top = top;
                _bottom = bottom;

                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint |
                         ControlStyles.ResizeRedraw, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = ClientRectangle;

                using (var br = new LinearGradientBrush(r, _top, _bottom, LinearGradientMode.Vertical))
                    e.Graphics.FillRectangle(br, r);

                using (var pen = new Pen(Color.FromArgb(50, 255, 255, 255)))
                    e.Graphics.DrawLine(pen, 0, 0, r.Width, 0);
            }
        }

        private enum WindowButtonKind { Minimize, Close }

        private sealed class WindowButton : Control
        {
            private readonly WindowButtonKind _kind;
            private readonly Color _top;
            private readonly Color _bottom;
            private bool _hover;

            public WindowButton(WindowButtonKind kind, Color titleTop, Color titleBottom)
            {
                _kind = kind;
                _top = titleTop;
                _bottom = titleBottom;

                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint |
                         ControlStyles.ResizeRedraw, true);

                Cursor = Cursors.Hand;
                TabStop = false;
            }

            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = ClientRectangle;

                // ✅ 타이틀바와 동일한 그라데이션을 버튼에도 직접 그림
                using (var br = new LinearGradientBrush(r, _top, _bottom, LinearGradientMode.Vertical))
                    e.Graphics.FillRectangle(br, r);

                if (_hover)
                {
                    Color hoverColor = (_kind == WindowButtonKind.Close)
                        ? Color.FromArgb(160, 200, 60, 60)
                        : Color.FromArgb(60, 255, 255, 255);

                    using (var brHover = new SolidBrush(hoverColor))
                        e.Graphics.FillRectangle(brHover, r);
                }

                using (var pen = new Pen(Color.White, 2f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;

                    int cx = Width / 2;
                    int cy = Height / 2;

                    if (_kind == WindowButtonKind.Minimize)
                    {
                        e.Graphics.DrawLine(pen, cx - 7, cy + 4, cx + 7, cy + 4);
                    }
                    else
                    {
                        e.Graphics.DrawLine(pen, cx - 6, cy - 5, cx + 6, cy + 5);
                        e.Graphics.DrawLine(pen, cx + 6, cy - 5, cx - 6, cy + 5);
                    }
                }
            }
        }

        // =========================
        //  RoundedTextBox
        // =========================
        private sealed class RoundedTextBox : Panel
        {
            private readonly TextBox _tb;
            private string _placeholder = "";
            private bool _isPassword;
            private bool _showingPlaceholder;
            private char _savedPasswordChar;

            public int Radius { get; set; } = 12;
            public Color BorderColor { get; set; } = Color.FromArgb(185, 205, 225);
            public Color FillColor { get; set; } = Color.White;
            public float BorderWidth { get; set; } = 2f;

            public RoundedTextBox()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint |
                         ControlStyles.ResizeRedraw, true);

                BackColor = Color.White;

                _tb = new TextBox
                {
                    BorderStyle = BorderStyle.None,
                    Font = new Font("Segoe UI", 9f),
                    Multiline = false,
                    BackColor = Color.White
                };
                _savedPasswordChar = '\0';
                Controls.Add(_tb);

                _tb.GotFocus += (s, e) =>
                {
                    if (_showingPlaceholder)
                    {
                        _tb.Text = "";
                        _tb.ForeColor = Color.Black;
                        _showingPlaceholder = false;
                        _tb.PasswordChar = _isPassword ? _savedPasswordChar : '\0';
                    }
                    Invalidate();
                };

                _tb.LostFocus += (s, e) => { ApplyPlaceholderIfNeeded(); Invalidate(); };

                SizeChanged += (s, e) => LayoutTextBox();
            }

            private void LayoutTextBox()
            {
                int padX = 12;
                _tb.BackColor = FillColor;
                _tb.Location = new Point(padX, (Height - _tb.PreferredHeight) / 2);
                _tb.Width = Math.Max(10, Width - padX * 2);
            }

            public void SetPlaceholder(string text, bool isPassword)
            {
                _placeholder = text ?? "";
                _isPassword = isPassword;
                _savedPasswordChar = _isPassword ? '●' : '\0';

                ApplyPlaceholderIfNeeded();
                LayoutTextBox();
            }

            private void ApplyPlaceholderIfNeeded()
            {
                if (string.IsNullOrEmpty(_tb.Text))
                {
                    _showingPlaceholder = true;
                    _tb.ForeColor = Color.Gray;
                    _tb.PasswordChar = '\0';
                    _tb.Text = _placeholder;
                }
            }

            public string GetValueOrEmpty()
            {
                return _showingPlaceholder ? "" : (_tb.Text ?? "");
            }

            public void ClearAndFocus()
            {
                _showingPlaceholder = false;
                _tb.Text = "";
                _tb.ForeColor = Color.Black;
                _tb.PasswordChar = _isPassword ? _savedPasswordChar : '\0';
                _tb.Focus();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                int bw = (int)Math.Ceiling(BorderWidth);
                Rectangle r = new Rectangle(bw, bw, Width - bw * 2 - 1, Height - bw * 2 - 1);
                if (r.Width <= 2 || r.Height <= 2) return;

                using (GraphicsPath path = CreateRoundPath(r, Radius))
                {
                    using (SolidBrush fill = new SolidBrush(FillColor))
                        e.Graphics.FillPath(fill, path);

                    using (Pen pen = new Pen(BorderColor, BorderWidth))
                    {
                        pen.Alignment = PenAlignment.Inset;
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }
        }

        // =========================
        //  Pill Button (NOT Button)
        //  -> 반짤림/검은잔상 원천 차단
        // =========================
        private sealed class PillButton : Control, IButtonControl
        {
            public int Radius { get; set; } = 17;
            private bool _hover;
            private bool _down;


            public PillButton()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint |
                         ControlStyles.ResizeRedraw, true);

                Cursor = Cursors.Hand;
                ForeColor = Color.White;
                SizeChanged += (s, e) => UpdateRegion();
                UpdateRegion();
            }
            private void UpdateRegion()
            {
                if (Width < 2 || Height < 2) return;

                if (Region != null)
                {
                    try { Region.Dispose(); } catch { }
                    Region = null;
                }

                // ✅ 영역을 라운드로 잘라서 "사각형 잔상" 자체 제거
                using (var path = CreateRoundPath(new Rectangle(0, 0, Width, Height), Radius))
                {
                    Region = new Region(path);
                }

                Invalidate();
            }
            public DialogResult DialogResult { get; set; }

            public void NotifyDefault(bool value) { /* no-op */ }

            public void PerformClick()
            {
                OnClick(EventArgs.Empty);
            }

            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    _down = true;
                    Invalidate();
                }
                base.OnMouseDown(e);
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                if (_down && e.Button == MouseButtons.Left)
                {
                    _down = false;
                    Invalidate();
                    OnClick(EventArgs.Empty);
                }
                base.OnMouseUp(e);
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                // ✅ 부모 배경(그라데이션)만 "내 영역"에 다시 그림
                if (Parent == null) return;

                var st = e.Graphics.Save();
                try
                {
                    e.Graphics.TranslateTransform(-Left, -Top);
                    var pea = new PaintEventArgs(e.Graphics, Parent.ClientRectangle);
                    InvokePaintBackground(Parent, pea);
                }
                finally
                {
                    e.Graphics.Restore(st);
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
                if (r.Width <= 2 || r.Height <= 2) return;

                using (GraphicsPath path = CreateRoundPath(r, Radius))
                {
                    Color top = Color.FromArgb(35, 95, 160);
                    Color bottom = Color.FromArgb(20, 75, 140);

                    if (_hover) { top = Color.FromArgb(45, 110, 175); bottom = Color.FromArgb(25, 85, 155); }
                    if (_down) { top = Color.FromArgb(25, 80, 145); bottom = Color.FromArgb(15, 65, 125); }

                    using (LinearGradientBrush br = new LinearGradientBrush(r, top, bottom, LinearGradientMode.Vertical))
                        e.Graphics.FillPath(br, path);

                    Rectangle topHalf = new Rectangle(r.X, r.Y, r.Width, r.Height / 2);
                    using (LinearGradientBrush br2 = new LinearGradientBrush(
                        topHalf,
                        Color.FromArgb(55, 255, 255, 255),
                        Color.FromArgb(0, 255, 255, 255),
                        LinearGradientMode.Vertical))
                    {
                        var st = e.Graphics.Save();
                        try
                        {
                            e.Graphics.SetClip(path);
                            e.Graphics.FillRectangle(br2, topHalf);
                        }
                        finally
                        {
                            e.Graphics.Restore(st);
                        }
                    }

                    using (Pen pen = new Pen(Color.FromArgb(25, 0, 0, 0), 1f))
                    {
                        pen.Alignment = PenAlignment.Inset;
                        e.Graphics.DrawPath(pen, path);
                    }

                    TextRenderer.DrawText(
                        e.Graphics,
                        Text,
                        Font,
                        r,
                        ForeColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }
    }
}
