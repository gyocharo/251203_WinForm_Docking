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
        #region Fields

        public bool LoginSucceeded { get; private set; }

        private bool _dragging;
        private Point _dragStart;

        private TitleBar pnlTitle;
        private WindowButton btnMin;
        private WindowButton btnClose;

        private PureGate.UIControl.SvgLikeLogo logo;

        private RoundedTextBox pwBox;
        private LinkLabel lnkForgot;
        private GradientPillButton btnLogin;
        #endregion

        #region Ctor

        public LoginForm()
        {
            InitializeComponent();
            BuildUi();
        }

        #endregion

        #region UI Build

        private void BuildUi()
        {
            #region Form Base

            Text = "Login";
            StartPosition = FormStartPosition.CenterScreen;

            // 1번 느낌은 상단 파란 타이틀바가 있음(커스텀)
            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;

            // 1번 비율에 맞춤
            ClientSize = new Size(520, 260);

            BackColor = Color.White;

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) Close();
            };

            #endregion

            #region TitleBar (Top blue bar + window buttons)

            pnlTitle = new TitleBar
            {
                Dock = DockStyle.Top,
                Height = 28
            };
            Controls.Add(pnlTitle);

            // 드래그 이동
            pnlTitle.MouseDown += Title_MouseDown;
            pnlTitle.MouseMove += Title_MouseMove;
            pnlTitle.MouseUp += Title_MouseUp;

            // 최소화
            btnMin = new WindowButton(WindowButtonKind.Minimize)
            {
                Size = new Size(40, 28),
                Location = new Point(0, 0), // ✅ 초기값 (SizeChanged에서 재배치)
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;
            pnlTitle.Controls.Add(btnMin);

            // 닫기
            btnClose = new WindowButton(WindowButtonKind.Close)
            {
                Size = new Size(40, 28),
                Location = new Point(0, 0), // ✅ 초기값 (SizeChanged에서 재배치)
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.Click += (s, e) => Close();
            pnlTitle.Controls.Add(btnClose);

            // ✅ 타이틀바 폭 기준으로 버튼 위치를 항상 재계산 (겹침/hover 이상 방지)
            Action layoutTitleButtons = () =>
            {
                // 오른쪽 끝부터 닫기, 그 왼쪽에 최소화
                btnClose.Location = new Point(pnlTitle.Width - btnClose.Width, 0);
                btnMin.Location = new Point(pnlTitle.Width - btnClose.Width - btnMin.Width, 0);
            };

            // 최초 1회 배치
            layoutTitleButtons();

            // 타이틀바 크기 바뀔 때마다 재배치
            pnlTitle.SizeChanged += (s, e) => layoutTitleButtons();

            #endregion

            #region Logo (SVG-like)

            logo = new SvgLikeLogo
            {
                Size = new Size(115, 125),
                Location = new Point(55, 70)
            };
            Controls.Add(logo);

            #endregion

            #region Password Only Input (match 1st image style)

            // 1번: 입력창은 얇은 테두리 + 라운드, 높이 낮음
            pwBox = new RoundedTextBox
            {
                Size = new Size(250, 28),
                Location = new Point(200, 110),
                Radius = 10,
                BorderColor = Color.FromArgb(70, 120, 170), // 더 진하게
                BorderWidth = 2f,                           // 🔥 추가
                FillColor = Color.White
            };
            pwBox.SetPlaceholder("Password", isPassword: true);
            Controls.Add(pwBox);

            #endregion

            #region Forgot Password link

            lnkForgot = new LinkLabel
            {
                Text = "Forgot Password?",
                AutoSize = true,
                Location = new Point(340, 145),
                Font = new Font("Segoe UI", 8.5f),
                LinkColor = Color.FromArgb(70, 145, 210),
                ActiveLinkColor = Color.FromArgb(40, 110, 175),
                VisitedLinkColor = Color.FromArgb(70, 145, 210),
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            lnkForgot.LinkClicked += (s, e) => MsgBox.Show("비밀번호 찾기 기능 연결하면 됨");
            Controls.Add(lnkForgot);

            #endregion

            #region Login Button (pill, smaller, dark navy)

            btnLogin = new GradientPillButton
            {
                Text = "LOG IN",
                Size = new Size(130, 32),
                Location = new Point(255, 185),
                Radius = 16,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnLogin.Click += BtnLogin_Click;
            Controls.Add(btnLogin);

            AcceptButton = btnLogin;

            #endregion
        }

        #endregion

        #region Paint (Background)

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // 기본 배경 지우기 막고 우리가 직접 그린다
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = this.ClientRectangle;

            using (var br = new LinearGradientBrush(
                r,
                Color.FromArgb(245, 250, 255),
                Color.White,
                LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(br, r);
            }
        }

        #endregion

        #region Login Logic

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string pw = pwBox.GetValueOrEmpty();

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
                pwBox.ClearAndFocus();
            }
        }

        private bool ValidatePassword(string pw)
        {
            // TODO 실제 검증 연결
            return pw == "1234";
        }

        #endregion

        #region Drag Move

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

        #endregion

        #region Round Utils

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

        #endregion

        #region Custom Controls - TitleBar & Buttons

        private sealed class TitleBar : Panel
        {
            public TitleBar()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint |
                         ControlStyles.ResizeRedraw, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = ClientRectangle;

                // 1번의 상단 파란바 느낌: 위쪽이 약간 더 밝음
                using (var br = new LinearGradientBrush(
                    r,
                    Color.FromArgb(45, 120, 185),
                    Color.FromArgb(25, 95, 165),
                    LinearGradientMode.Vertical))
                {
                    e.Graphics.FillRectangle(br, r);
                }

                // 아주 약한 하이라이트 라인
                using (var pen = new Pen(Color.FromArgb(50, 255, 255, 255)))
                {
                    e.Graphics.DrawLine(pen, 0, 0, r.Width, 0);
                }
            }
        }

        private enum WindowButtonKind { Minimize, Close }

        private sealed class WindowButton : Control
        {
            private readonly WindowButtonKind _kind;
            private bool _hover;

            public WindowButton(WindowButtonKind kind)
            {
                _kind = kind;

                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint |
                         ControlStyles.ResizeRedraw |
                         ControlStyles.SupportsTransparentBackColor, true);

                Cursor = Cursors.Hand;

                BackColor = Color.Transparent;   // Control 상속이라 OK
            }

            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); }

            protected override void OnPaintBackground(PaintEventArgs pevent)
            {
                base.OnPaintBackground(pevent);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // hover 배경
                if (_hover)
                {
                    Color hoverColor = (_kind == WindowButtonKind.Close)
                        ? Color.FromArgb(160, 200, 60, 60)
                        : Color.FromArgb(60, 255, 255, 255);

                    using (var br = new SolidBrush(hoverColor))
                    {
                        e.Graphics.FillRectangle(br, ClientRectangle);
                    }
                }

                // 아이콘
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

        #endregion

        #region Custom Controls - RoundedTextBox

        private sealed class RoundedTextBox : Panel
        {
            private readonly TextBox _tb;
            private string _placeholder = "";
            private bool _isPassword;
            private bool _showingPlaceholder;
            private char _savedPasswordChar;

            public int Radius { get; set; } = 10;
            public Color BorderColor { get; set; } = Color.FromArgb(185, 205, 225);
            public Color FillColor { get; set; } = Color.White;
            public float BorderWidth { get; set; } = 1f;

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
                    Location = new Point(10, 6),
                    Width = 10
                };
                _savedPasswordChar = '\0'; // 기본값
                Controls.Add(_tb);

                _tb.GotFocus += (s, e) =>
                {
                    if (_showingPlaceholder)
                    {
                        _tb.Text = "";
                        _tb.ForeColor = Color.Black;
                        _showingPlaceholder = false;

                        // ✅ 비밀번호면 PasswordChar로 가림 (핸들 재생성 없음)
                        _tb.PasswordChar = _isPassword ? _savedPasswordChar : '\0';
                    }
                    Invalidate();
                };

                _tb.LostFocus += (s, e) =>
                {
                    ApplyPlaceholderIfNeeded();
                    Invalidate();
                };

                SizeChanged += (s, e) => LayoutTextBox();
            }

            private void LayoutTextBox()
            {
                // 슬림 입력창 느낌
                _tb.Location = new Point(10, (Height - _tb.PreferredHeight) / 2);
                _tb.Width = Math.Max(10, Width - 20);
                ApplyRegionSafe();
                Invalidate();
            }

            private void ApplyRegionSafe()
            {
                if (Width <= 2 || Height <= 2) return;

                if (Region != null)
                {
                    try { Region.Dispose(); } catch { }
                    Region = null;
                }

                using (var path = CreateRoundPath(new Rectangle(0, 0, Width - 1, Height - 1), Radius))
                {
                    Region = new Region(path);
                }
            }

            public void SetPlaceholder(string text, bool isPassword)
            {
                _placeholder = text ?? "";
                _isPassword = isPassword;

                // ✅ 비밀번호 표시 문자 설정(원하면 '*'로 바꿔도 됨)
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

                    // ✅ placeholder는 가리면 안 되므로 PasswordChar 해제
                    _tb.PasswordChar = '\0';

                    _tb.Text = _placeholder;
                }
            }

            public string GetValueOrEmpty()
            {
                if (_showingPlaceholder) return "";
                return _tb.Text ?? "";
            }

            public void ClearAndFocus()
            {
                _showingPlaceholder = false;
                _tb.Text = "";
                _tb.ForeColor = Color.Black;

                // ✅ 비밀번호면 PasswordChar로 가림
                _tb.PasswordChar = _isPassword ? _savedPasswordChar : '\0';

                _tb.Focus();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, Width - 1, Height - 1);

                using (var path = CreateRoundPath(r, Radius))
                using (var fill = new SolidBrush(FillColor))
                using (var pen = new Pen(BorderColor, BorderWidth))   // 🔥 두께 적용
                {
                    pen.Alignment = PenAlignment.Inset;               // 🔥 안쪽으로 그려서 또렷하게
                    e.Graphics.FillPath(fill, path);
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        #endregion

        #region Custom Controls - Login Button

        private sealed class GradientPillButton : Button
        {
            public int Radius { get; set; } = 16;
            private bool _hover;
            private bool _down;

            public GradientPillButton()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint |
                         ControlStyles.ResizeRedraw, true);

                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;

                ForeColor = Color.White;
                BackColor = Color.Empty;               // Transparent 금지
                UseVisualStyleBackColor = false;

                Cursor = Cursors.Hand;

                MouseEnter += (s, e) => { _hover = true; Invalidate(); };
                MouseLeave += (s, e) => { _hover = false; _down = false; Invalidate(); };
                MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { _down = true; Invalidate(); } };
                MouseUp += (s, e) => { _down = false; Invalidate(); };

                SizeChanged += (s, e) => ApplyRegionSafe();
            }

            private void ApplyRegionSafe()
            {
                if (Width <= 2 || Height <= 2) return;

                if (Region != null)
                {
                    try { Region.Dispose(); } catch { }
                    Region = null;
                }

                using (var path = CreateRoundPath(new Rectangle(0, 0, Width - 1, Height - 1), Radius))
                {
                    Region = new Region(path);
                }
            }

            protected override void OnPaintBackground(PaintEventArgs pevent)
            {
                // 배경 기본 칠 없음(우리가 직접 그림)
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, Width - 1, Height - 1);

                using (var path = CreateRoundPath(r, Radius))
                {
                    // 1번처럼 진한 남색 pill (hover/down은 아주 미세)
                    Color top = Color.FromArgb(35, 95, 160);
                    Color bottom = Color.FromArgb(20, 75, 140);

                    if (_hover) { top = Color.FromArgb(45, 110, 175); bottom = Color.FromArgb(25, 85, 155); }
                    if (_down) { top = Color.FromArgb(25, 80, 145); bottom = Color.FromArgb(15, 65, 125); }

                    using (var br = new LinearGradientBrush(r, top, bottom, LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillPath(br, path);
                    }

                    // 은은한 상단 하이라이트
                    var topHalf = new Rectangle(0, 0, Width - 1, Height / 2);
                    using (var br2 = new LinearGradientBrush(topHalf,
                        Color.FromArgb(60, 255, 255, 255),
                        Color.FromArgb(0, 255, 255, 255),
                        LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillPath(br2, path);
                    }

                    // 테두리 아주 약하게
                    using (var pen = new Pen(Color.FromArgb(30, 0, 0, 0)))
                    {
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

        #endregion
    }
}