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
using System.Runtime.InteropServices;

namespace PureGate.UIControl
{
    public sealed class CustomMessageBox : Form
    {
        public enum MsgKind
        {
            Notice,   // 안내
            Info,     // 정보
            Warning,  // 주의
            Error,    // 오류
            Confirm   // 확인(예/아니오)
        }

        // >>> ADD: 버튼 종류(OK 단일 / YesNo 2개)
        public enum MsgButtons
        {
            Ok,
            YesNo
        }

        // ===== Public API =====

        // >>> ADD: MsgKind 기반 호출(권장)
        public static DialogResult Show(
            IWin32Window owner,
            MsgKind kind,
            string message,
            string detail = null,
            string title = null,
            string okText = "확인",
            string yesText = "예",
            string noText = "아니오",
            Image leftIcon = null)
        {
            // kind별 기본 타이틀
            if (string.IsNullOrWhiteSpace(title))
            {
                if (kind == MsgKind.Notice) title = "안내";
                else if (kind == MsgKind.Info) title = "정보";
                else if (kind == MsgKind.Warning) title = "주의";
                else if (kind == MsgKind.Error) title = "오류";
                else if (kind == MsgKind.Confirm) title = "확인";
                else title = "안내";
            }

            // kind별 버튼 구성
            var buttons = (kind == MsgKind.Confirm) ? MsgButtons.YesNo : MsgButtons.Ok;

            // >>> ADD: 아이콘 자동(원치 않으면 leftIcon 넘기면 그걸 사용)
            if (leftIcon == null)
                leftIcon = GetDefaultIcon(kind);

            // OK일 때는 yesText에 okText를 넣고, noText는 null로
            string finalYes = (buttons == MsgButtons.Ok) ? okText : yesText;
            string finalNo = (buttons == MsgButtons.Ok) ? null : noText;

            using (var dlg = new CustomMessageBox(owner, title, message, detail, buttons, finalYes, finalNo, leftIcon))
            {
                return owner != null ? dlg.ShowDialog(owner) : dlg.ShowDialog();
            }
        }

        // >>> CHG: 기존 Show는 유지하되, 버튼 제어가 가능하게 확장
        // - noText를 null로 넘기면 "확인(1개)" 형태로 동작하게 함
        public static DialogResult Show(
            IWin32Window owner,
            string title,
            string message,
            string detail = null,
            string yesText = "예",
            string noText = "아니오",
            Image leftIcon = null)
        {
            // >>> ADD: noText가 null/공백이면 OK 단일로 간주
            var buttons = string.IsNullOrWhiteSpace(noText) ? MsgButtons.Ok : MsgButtons.YesNo;

            using (var dlg = new CustomMessageBox(owner, title, message, detail, buttons, yesText, noText, leftIcon))
            {
                return owner != null ? dlg.ShowDialog(owner) : dlg.ShowDialog();
            }
        }

        // ===== Style =====
        private const int Radius = 18;

        private readonly Color C_Back = Color.FromArgb(250, 252, 255);
        private readonly Color C_Border = Color.FromArgb(225, 232, 242);
        private readonly Color C_Title = Color.FromArgb(18, 55, 90);
        private readonly Color C_Text = Color.FromArgb(55, 65, 81);
        private readonly Color C_Sub = Color.FromArgb(120, 130, 145);
        private readonly Color C_Primary = Color.FromArgb(34, 117, 255);

        // ===== Owner =====
        private readonly IWin32Window _owner;

        // ===== UI =====
        private readonly Panel _header = new Panel();
        private readonly PictureBox _pic = new PictureBox();
        private readonly Label _lblTitle = new Label();

        private readonly Label _lblMsg = new Label();
        private readonly Label _lblDetail = new Label();

        private readonly Panel _buttons = new Panel();
        private readonly FlowLayoutPanel _btnFlow = new FlowLayoutPanel();
        private readonly Button _btnYes = new Button();
        private readonly Button _btnNo = new Button();

        // >>> ADD: 버튼 모드 저장
        private readonly MsgButtons _buttonsMode;

        // ===== Ctor =====
        // >>> CHG: buttonsMode 파라미터 추가
        private CustomMessageBox(
            IWin32Window owner,
            string title,
            string message,
            string detail,
            MsgButtons buttonsMode,
            string yesText,
            string noText,
            Image leftIcon)
        {
            _owner = owner;
            _buttonsMode = buttonsMode;

            // Form basics
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            BackColor = C_Back;

            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // >>> CHG: 높이는 detail/버튼에 따라 더 안전하게
            int baseH = string.IsNullOrWhiteSpace(detail) ? 190 : 220;
            ClientSize = new Size(440, baseH);

            // ===== Header =====
            _header.Dock = DockStyle.Top;
            _header.Height = 52;
            _header.BackColor = C_Back;
            _header.Padding = new Padding(18, 10, 18, 10);
            _header.Paint += Header_Paint;

            // >>> ADD: 헤더 드래그 이벤트 연결(기존 메서드가 있었는데 미연결 상태였음)
            _header.MouseDown += Header_MouseDown;
            _header.MouseMove += Header_MouseMove;
            _header.MouseUp += Header_MouseUp;

            // Icon
            _pic.Size = new Size(32, 32);
            _pic.SizeMode = PictureBoxSizeMode.CenterImage;
            _pic.Image = leftIcon;
            _pic.Visible = (leftIcon != null);
            _pic.Location = new Point(0, 0);
            _header.Controls.Add(_pic);

            _lblTitle.Text = string.IsNullOrWhiteSpace(title) ? "Question" : title;
            _lblTitle.AutoSize = false;
            _lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            _lblTitle.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            _lblTitle.ForeColor = C_Title;
            _header.Controls.Add(_lblTitle);

            // ===== Buttons panel =====
            _buttons.Dock = DockStyle.Bottom;
            _buttons.Height = 56;
            _buttons.BackColor = C_Back;
            _buttons.Padding = new Padding(18, 10, 18, 12);
            _buttons.Paint += Buttons_Paint;

            _btnFlow.Dock = DockStyle.Right;
            _btnFlow.FlowDirection = FlowDirection.LeftToRight;
            _btnFlow.WrapContents = false;
            _btnFlow.AutoSize = true;
            _btnFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _btnFlow.BackColor = Color.Transparent;
            _btnFlow.Margin = new Padding(0);
            _btnFlow.Padding = new Padding(0);
            _buttons.Controls.Add(_btnFlow);

            // >>> ADD: 버튼 스타일 함수
            void StylePrimary(Button b)
            {
                b.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
                b.BackColor = C_Primary;
                b.ForeColor = Color.White;
                b.Size = new Size(112, 34);
                b.Margin = new Padding(0);
            }
            void StyleSecondary(Button b)
            {
                b.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.BorderColor = C_Border;
                b.BackColor = Color.White;
                b.ForeColor = C_Text;
                b.Size = new Size(112, 34);
                b.Margin = new Padding(8, 0, 0, 0);
            }

            // >>> CHG: 기존처럼 무조건 2개 추가하지 말고, 모드에 따라 구성
            _btnFlow.Controls.Clear();

            if (_buttonsMode == MsgButtons.Ok)
            {
                // OK 단일 버튼: _btnYes 재사용
                _btnYes.Text = string.IsNullOrWhiteSpace(yesText) ? "확인" : yesText;
                StylePrimary(_btnYes);

                // >>> CHG: OK는 DialogResult.OK
                _btnYes.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

                _btnFlow.Controls.Add(_btnYes);

                AcceptButton = _btnYes;
                CancelButton = _btnYes; // ESC도 닫힘

                // >>> ADD: No 버튼은 사용 안 함
                _btnNo.Visible = false;
            }
            else
            {
                // YesNo
                _btnYes.Text = string.IsNullOrWhiteSpace(yesText) ? "예" : yesText;
                StylePrimary(_btnYes);
                _btnYes.Click += (s, e) => { DialogResult = DialogResult.Yes; Close(); };

                _btnNo.Text = string.IsNullOrWhiteSpace(noText) ? "아니오" : noText;
                StyleSecondary(_btnNo);
                _btnNo.Click += (s, e) => { DialogResult = DialogResult.No; Close(); };

                _btnFlow.Controls.Add(_btnYes);
                _btnFlow.Controls.Add(_btnNo);

                AcceptButton = _btnYes;
                CancelButton = _btnNo;

                _btnNo.Visible = true;
            }

            // ===== Body (fill) =====
            var body = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = C_Back,
                Padding = new Padding(18, 14, 18, 12)
            };

            _lblMsg.Text = message ?? "";
            _lblMsg.AutoSize = false;
            _lblMsg.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            _lblMsg.ForeColor = C_Text;
            _lblMsg.Dock = DockStyle.Top;
            _lblMsg.MaximumSize = new Size(ClientSize.Width - body.Padding.Left - body.Padding.Right, 0);
            _lblMsg.Height = _lblMsg.PreferredHeight;
            body.Controls.Add(_lblMsg);

            _lblDetail.Text = detail ?? "";
            _lblDetail.Visible = !string.IsNullOrWhiteSpace(detail);
            _lblDetail.AutoSize = false;
            _lblDetail.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            _lblDetail.ForeColor = C_Sub;
            _lblDetail.Dock = DockStyle.Top;
            _lblDetail.MaximumSize = new Size(ClientSize.Width - body.Padding.Left - body.Padding.Right, 0);
            _lblDetail.Height = _lblDetail.PreferredHeight + 6;
            _lblDetail.Padding = new Padding(0, 6, 0, 0);

            if (_lblDetail.Visible)
                body.Controls.Add(_lblDetail);

            Controls.Add(body);
            Controls.Add(_buttons);
            Controls.Add(_header);

            Layout += (s, e) => ReflowHeader();

            Shown += (s, e) =>
            {
                int w = ClientSize.Width - body.Padding.Left - body.Padding.Right;

                _lblMsg.MaximumSize = new Size(w, 0);
                _lblMsg.Height = _lblMsg.PreferredHeight;

                _lblDetail.MaximumSize = new Size(w, 0);
                _lblDetail.Height = _lblDetail.PreferredHeight + 6;

                if (_owner is Control c && c.FindForm() != null)
                {
                    var f = c.FindForm();
                    Location = new Point(
                        f.Left + (f.Width - Width) / 2,
                        f.Top + (f.Height - Height) / 2
                    );
                }
                else
                {
                    CenterToScreen();
                }
            };

            ApplyRoundedRegion();
            SizeChanged += (s, e) => ApplyRoundedRegion();
        }

        private void ReflowHeader()
        {
            int left = _header.Padding.Left;
            int top = _header.Padding.Top;

            if (_pic.Visible)
            {
                _pic.Location = new Point(left, top);
                _lblTitle.Location = new Point(_pic.Right + 10, top + 2);
                _lblTitle.Size = new Size(_header.ClientSize.Width - (_pic.Right + 10) - _header.Padding.Right, 28);
            }
            else
            {
                _lblTitle.Location = new Point(left, top + 2);
                _lblTitle.Size = new Size(_header.ClientSize.Width - left - _header.Padding.Right, 28);
            }
        }

        private void Header_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Color.FromArgb(235, 240, 246), 1f))
            {
                e.Graphics.DrawLine(pen, 0, _header.Height - 1, _header.Width, _header.Height - 1);
            }
        }

        private void Buttons_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Color.FromArgb(235, 240, 246), 1f))
            {
                e.Graphics.DrawLine(pen, 0, 0, _buttons.Width, 0);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = ClientRectangle;
            rect.Inflate(-1, -1);

            using (var path = CreateRoundRectPath(rect, Radius))
            using (var pen = new Pen(C_Border, 1f))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        private void ApplyRoundedRegion()
        {
            var rect = ClientRectangle;
            if (rect.Width <= 0 || rect.Height <= 0) return;

            rect.Inflate(-1, -1);

            using (var path = CreateRoundRectPath(rect, Radius))
            {
                Region = new Region(path);
            }
            Invalidate();
        }

        private static GraphicsPath CreateRoundRectPath(Rectangle rect, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // >>> ADD: kind별 기본 아이콘(원하면 여기서 바꿔)
        private static Image GetDefaultIcon(MsgKind kind)
        {
            return IconCache.Get(kind);
        }

        private static class IconCache
        {
            private static readonly Dictionary<MsgKind, Image> _cache = new Dictionary<MsgKind, Image>();

            public static Image Get(MsgKind kind)
            {
                Image img;
                if (_cache.TryGetValue(kind, out img)) return img;

                img = CreateModernIcon(kind, 32);
                _cache[kind] = img;
                return img;
            }
        }

        private static Image CreateModernIcon(MsgKind kind, int size)
        {
            // kind별 컬러(네 UI 톤에 맞춘 계열)
            Color baseColor;
            switch (kind)
            {
                case MsgKind.Info: baseColor = Color.FromArgb(59, 130, 246); break; // blue
                case MsgKind.Notice: baseColor = Color.FromArgb(34, 197, 94); break; // green
                case MsgKind.Warning: baseColor = Color.FromArgb(245, 158, 11); break; // amber
                case MsgKind.Error: baseColor = Color.FromArgb(239, 68, 68); break; // red
                case MsgKind.Confirm: baseColor = Color.FromArgb(99, 102, 241); break; // indigo
                default: baseColor = Color.FromArgb(59, 130, 246); break;
            }

            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                var rect = new Rectangle(1, 1, size - 2, size - 2);

                // 살짝 밝은 톤(위쪽) + 기본 톤(아래쪽) 그라데이션
                var light = Blend(baseColor, Color.White, 0.28f);
                var dark = Blend(baseColor, Color.Black, 0.08f);

                using (var br = new LinearGradientBrush(rect, light, dark, 90f))
                using (var ring = new Pen(Color.FromArgb(210, 255, 255, 255), 1.2f))
                using (var shadow = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                {
                    // 아주 약한 그림자
                    var shadowRect = rect;
                    shadowRect.Offset(0, 1);
                    using (var sp = CreateEllipsePath(shadowRect))
                        g.FillPath(shadow, sp);

                    // 원형 배경
                    using (var p = CreateEllipsePath(rect))
                        g.FillPath(br, p);

                    // 얇은 링(하이라이트)
                    using (var p = CreateEllipsePath(rect))
                        g.DrawPath(ring, p);
                }

                // 중앙 심볼(흰색)
                using (var sym = new Pen(Color.FromArgb(245, 255, 255, 255), Math.Max(2.2f, size * 0.075f)))
                using (var fill = new SolidBrush(Color.FromArgb(245, 255, 255, 255)))
                {
                    sym.StartCap = LineCap.Round;
                    sym.EndCap = LineCap.Round;
                    sym.LineJoin = LineJoin.Round;

                    // 심볼 도형은 “폰트” 대신 “도형”으로 그려서 PC마다 일관되게 보이게 함
                    switch (kind)
                    {
                        case MsgKind.Info:
                            DrawInfoGlyph(g, size, fill, sym);
                            break;

                        case MsgKind.Notice:
                            DrawCheckGlyph(g, size, sym);
                            break;

                        case MsgKind.Warning:
                            DrawWarningGlyph(g, size, sym, fill);
                            break;

                        case MsgKind.Error:
                            DrawErrorGlyph(g, size, sym);
                            break;

                        case MsgKind.Confirm:
                            DrawQuestionGlyph(g, size, fill, sym);
                            break;

                        default:
                            DrawInfoGlyph(g, size, fill, sym);
                            break;
                    }
                }
            }

            return bmp;
        }

        private static Color Blend(Color a, Color b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;

            int r = (int)(a.R + (b.R - a.R) * t);
            int g = (int)(a.G + (b.G - a.G) * t);
            int bl = (int)(a.B + (b.B - a.B) * t);
            return Color.FromArgb(255, r, g, bl);
        }

        // >>> ADD: 원형 path
        private static GraphicsPath CreateEllipsePath(Rectangle r)
        {
            var p = new GraphicsPath();
            p.AddEllipse(r);
            p.CloseFigure();
            return p;
        }


        // ===== Glyphs =====

        // i (정보)
        private static void DrawInfoGlyph(Graphics g, int size, Brush fill, Pen pen)
        {
            float cx = size / 2f;
            float top = size * 0.28f;

            // 점
            float dot = size * 0.09f;
            g.FillEllipse(fill, cx - dot / 2f, top, dot, dot);

            // 막대
            float w = size * 0.10f;
            float h = size * 0.36f;
            float y = size * 0.42f;

            using (var br = new SolidBrush(Color.FromArgb(245, 255, 255, 255)))
                FillRoundedRect(g, br, cx - w / 2f, y, w, h, w / 2f);
        }

        // 체크(안내)
        private static void DrawCheckGlyph(Graphics g, int size, Pen pen)
        {
            var p1 = new PointF(size * 0.30f, size * 0.54f);
            var p2 = new PointF(size * 0.44f, size * 0.66f);
            var p3 = new PointF(size * 0.71f, size * 0.38f);

            g.DrawLines(pen, new[] { p1, p2, p3 });
        }

        // 경고(삼각 + 느낌표)
        private static void DrawWarningGlyph(Graphics g, int size, Pen pen, Brush fill)
        {
            // 삼각형 외곽
            var a = new PointF(size * 0.50f, size * 0.22f);
            var b = new PointF(size * 0.78f, size * 0.72f);
            var c = new PointF(size * 0.22f, size * 0.72f);

            using (var path = new GraphicsPath())
            {
                path.AddPolygon(new[] { a, b, c });
                using (var triFill = new SolidBrush(Color.FromArgb(30, 255, 255, 255)))
                    g.FillPath(triFill, path);
                g.DrawPath(pen, path);
            }

            // 느낌표
            float cx = size * 0.50f;
            g.DrawLine(pen, cx, size * 0.38f, cx, size * 0.56f);

            float dot = size * 0.08f;
            g.FillEllipse(fill, cx - dot / 2f, size * 0.62f, dot, dot);
        }

        // 에러(X)
        private static void DrawErrorGlyph(Graphics g, int size, Pen pen)
        {
            var p1 = new PointF(size * 0.33f, size * 0.33f);
            var p2 = new PointF(size * 0.67f, size * 0.67f);
            var p3 = new PointF(size * 0.67f, size * 0.33f);
            var p4 = new PointF(size * 0.33f, size * 0.67f);

            g.DrawLine(pen, p1, p2);
            g.DrawLine(pen, p3, p4);
        }

        // 물음표(확인)
        private static void DrawQuestionGlyph(Graphics g, int size, Brush fill, Pen pen)
        {
            // 상단 곡선(간단한 ? 느낌)
            using (var path = new GraphicsPath())
            {
                path.StartFigure();
                path.AddArc(size * 0.30f, size * 0.26f, size * 0.40f, size * 0.34f, 200, 220);
                g.DrawPath(pen, path);
            }

            // 아래 짧은 줄
            g.DrawLine(pen, size * 0.50f, size * 0.56f, size * 0.50f, size * 0.60f);

            // 점
            float dot = size * 0.08f;
            g.FillEllipse(fill, size * 0.50f - dot / 2f, size * 0.66f, dot, dot);
        }

        // >>> ADD: 둥근 사각형 Fill 확장 (C# 7.3 OK)
        private static void FillRoundedRect(Graphics g, Brush brush, float x, float y, float w, float h, float r)
        {
            using (var path = new GraphicsPath())
            {
                float d = r * 2f;
                path.AddArc(x, y, d, d, 180, 90);
                path.AddArc(x + w - d, y, d, d, 270, 90);
                path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
                path.AddArc(x, y + h - d, d, d, 90, 90);
                path.CloseFigure();
                g.FillPath(brush, path);
            }
        }

        // ===== Dragging header =====
        private bool _drag;
        private Point _dragStart;

        private void Header_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _drag = true;
            _dragStart = e.Location;
        }

        private void Header_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_drag) return;
            var dx = e.X - _dragStart.X;
            var dy = e.Y - _dragStart.Y;
            Location = new Point(Location.X + dx, Location.Y + dy);
        }

        private void Header_MouseUp(object sender, MouseEventArgs e) => _drag = false;

        // ===== Shadow =====
        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x00020000;
                var cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }
    }
}
