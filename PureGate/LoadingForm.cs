using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PureGate.UIControl;

namespace PureGate
{
    public partial class LoadingForm : Form
    {
        private readonly LoadingSurface _surface;

        public LoadingForm()
        {
            InitializeComponent();

            // ===== Form Base =====
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(640, 300);   // 가로 넉넉하게 (산업용 느낌)
            BackColor = Color.Magenta;         // TransparencyKey용 (실제 색은 Surface가 그림)
            TransparencyKey = Color.Magenta;   // 폼 라운드 가장자리 “바깥” 보이게
            DoubleBuffered = true;
            ShowInTaskbar = false;
            TopMost = true;

            _surface = new LoadingSurface
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(_surface);

            // 기본 문구
            _surface.SetTitle("PureGate INSPECTION");
            _surface.SetMeta("Inspection Runtime • Secured Session");
            _surface.SetStatus("Initializing");
            _surface.SetSteps(
                "Loading inspection model",
                "Initializing camera pipeline",
                "Preparing runtime environment"
            );
            _surface.SetProgress(null);

            // 폼 라운드 적용
            ApplyRoundRegion();
            SizeChanged += (s, e) => ApplyRoundRegion();
        }

        // 외부에서 상태 갱신
        public void SetStatus(string text) => _surface.SetStatus(text);

        // 외부에서 진행률(0~100) 넣고 싶으면 사용, null이면 인디터미넌트(마퀴)
        public void SetProgress(double? percent0to100) => _surface.SetProgress(percent0to100);

        // 단계 텍스트 3줄 변경
        public void SetSteps(params string[] steps3) => _surface.SetSteps(steps3);

        // (2번) 단계 지정 래퍼
        public void SetActiveStep(int index) => _surface.SetActiveStep(index);

        // 둥근 모서리 폼 Region
        private void ApplyRoundRegion()
        {
            int radius = 22;
            if (Width <= 2 || Height <= 2) return;

            using (var path = CreateRoundPath(new Rectangle(0, 0, Width, Height), radius))
            {
                Region = new Region(path);
            }
        }

        // 약한 드롭섀도우(환경에 따라 차이 있음)
        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x00020000;
                var cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;

                // 깜빡임 완화(로딩폼에만)
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
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

            // r.Width/Height 그대로 쓰면 경계가 깔끔하게 잘림
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private sealed class LoadingSurface : Panel
        {
            // ===== Animation =====
            private readonly Timer _timer;
            private int _tick;
            private int _dots;

            // ===== UI State =====
            private string _title = "PureGate INSPECTION";
            private string _meta = "Inspection Runtime • Secured Session";
            private string _statusBase = "Initializing";
            private string[] _steps = new[] { "", "", "" };
            private double? _progress; // null => indeterminate

            private double? _progressTarget;
            private double _progressDisplay;

            private int _activeStep = 0; // 0~2 현재 강조 단계

            // ===== Child =====
            private readonly SvgLikeLogo _logo;

            // ===== Fonts (캐싱) =====
            private readonly Font _fTitle = new Font("Segoe UI", 15.0f, FontStyle.Bold);
            private readonly Font _fMeta = new Font("Segoe UI", 9.2f, FontStyle.Regular);
            private readonly Font _fStatus = new Font("Segoe UI", 10.2f, FontStyle.Regular);
            private readonly Font _fStep = new Font("Segoe UI", 9.4f, FontStyle.Regular);
            private readonly Font _fPercent = new Font("Segoe UI", 8.6f, FontStyle.Regular);

            // ===== Drag Move =====
            private bool _dragging;
            private Point _dragStart;

            public void SetActiveStep(int index)
            {
                if (index < 0) index = 0;
                if (index > 2) index = 2;

                if (_activeStep == index) return; // 불필요한 redraw 방지
                _activeStep = index;
                Invalidate();
            }

            // 현재 단계 얻고 싶으면
            public int GetActiveStep()
            {
                return _activeStep;
            }

            public LoadingSurface()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint |
                         ControlStyles.ResizeRedraw, true);

                BackColor = Color.Transparent;

                _logo = new SvgLikeLogo
                {
                    Size = new Size(110, 120)
                };
                Controls.Add(_logo);

                // 드래그 이동 (산업용 현장 PC에서 유용)
                MouseDown += Surface_MouseDown;
                MouseMove += Surface_MouseMove;
                MouseUp += (s, e) => _dragging = false;

                _timer = new Timer { Interval = 16 }; // ~60fps
                _timer.Tick += (s, e) =>
                {
                    _tick++;
                    if (_tick % 14 == 0) _dots = (_dots + 1) % 4;

                    if (_progressTarget.HasValue)
                    {
                        double target = _progressTarget.Value;

                        // 남은 거리 비례로 이동(프레임마다 부드럽게)
                        double delta = (target - _progressDisplay) * 0.12;

                        // 너무 느려서 멈춘 것처럼 보이지 않게 최소 이동량 보장
                        if (Math.Abs(delta) < 0.06)
                            delta = (target > _progressDisplay) ? 0.06 : -0.06;

                        // 목표를 넘지 않도록 클램프
                        if ((delta > 0 && _progressDisplay + delta > target) ||
                            (delta < 0 && _progressDisplay + delta < target))
                            _progressDisplay = target;
                        else
                            _progressDisplay += delta;
                    }

                    Invalidate();
                };
                _timer.Start();
            }

            private void Surface_MouseDown(object sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left) return;
                _dragging = true;
                _dragStart = e.Location;
            }

            private void Surface_MouseMove(object sender, MouseEventArgs e)
            {
                if (!_dragging) return;
                var f = FindForm();
                if (f == null) return;

                var screenPos = PointToScreen(e.Location);
                f.Location = new Point(screenPos.X - _dragStart.X, screenPos.Y - _dragStart.Y);
            }

            // ===== API =====
            public void SetTitle(string title)
            {
                _title = string.IsNullOrWhiteSpace(title) ? "PureGate INSPECTION" : title.Trim();
                Invalidate();
            }

            public void SetMeta(string meta)
            {
                _meta = string.IsNullOrWhiteSpace(meta) ? "" : meta.Trim();
                Invalidate();
            }

            public void SetStatus(string text)
            {
                _statusBase = string.IsNullOrWhiteSpace(text) ? "Initializing" : text.Trim();
                Invalidate();
            }

            public void SetSteps(params string[] steps3)
            {
                var a = new string[] { "", "", "" };
                if (steps3 != null)
                {
                    for (int i = 0; i < Math.Min(3, steps3.Length); i++)
                        a[i] = steps3[i] ?? "";
                }
                _steps = a;
                Invalidate();
            }

            public void SetProgress(double? percent0to100)
            {
                if (percent0to100.HasValue)
                {
                    var v = percent0to100.Value;
                    if (v < 0) v = 0;
                    if (v > 100) v = 100;

                    _progressTarget = v;

                    // 처음 determinate로 전환되는 순간 표시값 초기화
                    if (!_progress.HasValue)
                        _progressDisplay = v;

                    _progress = v; // determinate 모드 표시용 플래그 역할
                }
                else
                {
                    _progress = null;
                    _progressTarget = null;
                }
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
                    return;

                base.OnPaint(e);

                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // ===== 전체 라운드 폼 안쪽 배경 =====
                var r = ClientRectangle;

                // “TransparencyKey = Magenta”로 폼 바깥이 비어 보이게 했으니
                // 여기서는 실제 UI 배경을 그려준다.
                using (var br = new LinearGradientBrush(
                    r,
                    Color.FromArgb(246, 250, 255),
                    Color.White,
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(br, r);
                }

                // 얇은 인셋 테두리(끊김 방지: Inset)
                using (var pen = new Pen(Color.FromArgb(35, 0, 0, 0), 1f))
                {
                    pen.Alignment = PenAlignment.Inset;
                    g.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);
                }

                // ===== 레이아웃 =====
                int padX = 26;
                int padY = 22;

                // 좌측 로고 영역
                int logoX = padX;
                int logoY = padY + 6;
                _logo.Location = new Point(logoX, logoY);

                // 우측 텍스트 영역 시작
                int textLeft = logoX + _logo.Width + 22;
                int top = padY + 4;

                Color cPrimary = Color.FromArgb(25, 95, 165);
                Color cText = Color.FromArgb(45, 45, 45);
                Color cSub = Color.FromArgb(105, 105, 105);
                Color cMute = Color.FromArgb(150, 150, 150);

                // ===== Title / Meta =====
                DrawText(g, _title, _fTitle, new Rectangle(textLeft, top, Width - textLeft - padX, 30), cPrimary);

                if (!string.IsNullOrEmpty(_meta))
                    DrawText(g, _meta, _fMeta, new Rectangle(textLeft, top + 32, Width - textLeft - padX, 18), cMute);

                // 구분선
                int sepY = top + 60;
                DrawHairLine(g, padX, sepY, Width - padX, sepY, Color.FromArgb(30, 0, 0, 0));

                // ===== Status (점 애니메이션) =====
                string dots = new string('.', _dots);
                string statusLine = _statusBase + dots;
                DrawText(g, statusLine, _fStatus, new Rectangle(textLeft, sepY + 12, Width - textLeft - padX, 20), cText);

                // ===== Steps =====
                int stepTop = sepY + 42;
                for (int i = 0; i < 3; i++)
                {
                    bool active = (i == _activeStep);

                    DrawStepLine(
                        g,
                        x: textLeft,
                        y: stepTop + i * 22,
                        width: Width - textLeft - padX,
                        text: _steps[i] ?? "",
                        font: _fStep,
                        textColor: active ? cPrimary : cSub,
                        dotColor: active ? Color.FromArgb(200, cPrimary) : Color.FromArgb(110, 180, 190, 205)
                    );
                }

                // ===== Progress Bar =====
                int barH = 12;
                int barY = Height - padY - barH;
                var barRect = new Rectangle(padX, barY, Width - padX * 2, barH);
                DrawProgressBar(g, barRect, _progress, _tick,
                    track: Color.FromArgb(225, 235, 245),
                    accent: cPrimary);

                // 퍼센트는 determinate일 때만
                if (_progress.HasValue)
                {
                    string p = string.Format("{0:0}%", _progress.Value);
                    DrawTextRight(g, p, _fPercent, new Rectangle(padX, barY - 18, Width - padX * 2, 16), cMute);
                }
            }

            private static void DrawProgressBar(Graphics g, Rectangle r, double? percent, int tick, Color track, Color accent)
            {
                // 그릴 공간이 없으면 종료
                if (r.Width <= 0 || r.Height <= 0)
                    return;

                // percent 방어 + 0~100 클램프 (double로 처리)
                double p = percent ?? 0.0;
                if (double.IsNaN(p) || double.IsInfinity(p)) p = 0.0;
                if (p < 0.0) p = 0.0;
                if (p > 100.0) p = 100.0;

                using (var path = CreateRoundPath(r, radius: r.Height))
                using (var brTrack = new SolidBrush(track))
                {
                    g.FillPath(brTrack, path);
                }

                using (var clipPath = CreateRoundPath(r, radius: r.Height))
                {
                    var old = g.Clip;
                    g.SetClip(clipPath);

                    if (percent.HasValue)
                    {
                        int w = (int)Math.Round(r.Width * (p / 100.0));
                        if (w < 0) w = 0;
                        if (w > r.Width) w = r.Width;

                        // 폭이 0이면 fill 관련 브러시/그라데이션을 만들지 않음 (핵심)
                        if (w > 0)
                        {
                            var fillRect2 = new Rectangle(r.X, r.Y, w, r.Height); // 이름 변경
                            using (var brFill = new SolidBrush(Color.FromArgb(220, accent)))
                                g.FillRectangle(brFill, fillRect2);

                            var hi = new Rectangle(r.X, r.Y, w, Math.Max(1, r.Height / 2));
                            // hi는 height를 1 이상 보장, w도 >0 보장이라 예외 안 남
                            using (var br2 = new LinearGradientBrush(
                                hi,
                                Color.FromArgb(70, Color.White),
                                Color.FromArgb(0, Color.White),
                                LinearGradientMode.Vertical))
                            {
                                g.FillRectangle(br2, hi);
                            }
                        }
                    }
                    else
                    {
                        int chunkW = Math.Max(30, (int)(r.Width * 0.18));
                        int speed = 7;
                        int x = r.X + (tick * speed) % (r.Width + chunkW) - chunkW;

                        DrawChunk(g, r, x, chunkW, accent);
                        DrawChunk(g, r, x - (chunkW + 44), chunkW, accent);
                    }

                    g.Clip = old;
                }

                using (var outline = CreateRoundPath(r, radius: r.Height))
                using (var pen = new Pen(Color.FromArgb(55, 0, 0, 0), 1f))
                {
                    pen.Alignment = PenAlignment.Inset;
                    g.DrawPath(pen, outline);
                }
            }

            private static void DrawChunk(Graphics g, Rectangle r, int x, int w, Color accent)
            {
                var chunk = new Rectangle(x, r.Y, w, r.Height);
                using (var br = new LinearGradientBrush(
                    chunk,
                    Color.FromArgb(0, accent),
                    Color.FromArgb(230, accent),
                    LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(br, chunk);
                }

                var inner = new Rectangle(x + w / 4, r.Y, w / 2, r.Height);
                using (var br2 = new LinearGradientBrush(
                    inner,
                    Color.FromArgb(0, Color.White),
                    Color.FromArgb(120, Color.White),
                    LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(br2, inner);
                }
            }

            private static void DrawStepLine(Graphics g, int x, int y, int width, string text, Font font, Color textColor, Color dotColor)
            {
                int dotSize = 7;
                int dotY = y + 6;

                using (var br = new SolidBrush(dotColor))
                    g.FillEllipse(br, x, dotY, dotSize, dotSize);

                var rect = new Rectangle(x + dotSize + 12, y, width - (dotSize + 12), 20);
                DrawText(g, text, font, rect, textColor);
            }

            private static void DrawHairLine(Graphics g, int x1, int y1, int x2, int y2, Color color)
            {
                using (var pen = new Pen(color, 1f))
                {
                    pen.Alignment = PenAlignment.Inset;
                    g.DrawLine(pen, x1, y1, x2, y2);
                }
            }

            private static void DrawText(Graphics g, string text, Font font, Rectangle rect, Color color)
            {
                TextRenderer.DrawText(
                    g,
                    text ?? "",
                    font,
                    rect,
                    color,
                    TextFormatFlags.Left |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPadding);
            }

            private static void DrawTextRight(Graphics g, string text, Font font, Rectangle rect, Color color)
            {
                TextRenderer.DrawText(
                    g,
                    text ?? "",
                    font,
                    rect,
                    color,
                    TextFormatFlags.Right |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPadding);
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

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    try { _timer.Stop(); _timer.Dispose(); } catch { }

                    // Fonts dispose
                    try { _fTitle.Dispose(); } catch { }
                    try { _fMeta.Dispose(); } catch { }
                    try { _fStatus.Dispose(); } catch { }
                    try { _fStep.Dispose(); } catch { }
                    try { _fPercent.Dispose(); } catch { }
                }
                base.Dispose(disposing);
            }
        }
    }
}
