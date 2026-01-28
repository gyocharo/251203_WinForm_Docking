using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PureGate.UIControl
{
    public sealed class SvgLikeLogo : Control
    {
        public SvgLikeLogo()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
           ControlStyles.OptimizedDoubleBuffer |
           ControlStyles.UserPaint |
           ControlStyles.ResizeRedraw |
           ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (Width < 20 || Height < 20)
                return;

            // ===== 파란 네모는 항상 정사각형 =====
            int size = Math.Min(Width, Height);
            int offsetX = (Width - size) / 2;
            int offsetY = (Height - size) / 2;

            var square = new Rectangle(offsetX, offsetY, size, size);

            DrawLogo(g, square);
        }

        private void DrawLogo(Graphics g, Rectangle square)
        {
            bool drawText = square.Width >= 70;

            // ===== 파란 배경 =====
            using (var bgPath = CreateRoundPath(square, square.Width * 0.18f))
            using (var bgBr = new LinearGradientBrush(
                square,
                Color.FromArgb(70, 145, 210),
                Color.FromArgb(35, 100, 175),
                LinearGradientMode.Vertical))
            {
                g.FillPath(bgBr, bgPath);
            }

            // ===== 내부 레이아웃 비율 =====
            float iconAreaRatio = drawText ? 0.62f : 1.0f;   // 🔥 접히면 전체 사용
            float textAreaRatio = drawText ? (1f - iconAreaRatio) : 0f;

            RectangleF iconArea = new RectangleF(
                square.X,
                square.Y,
                square.Width,
                square.Height * iconAreaRatio);

            RectangleF textArea = new RectangleF(
                square.X,
                square.Y + square.Height * iconAreaRatio,
                square.Width,
                square.Height * textAreaRatio);

            // ===== Gate 아이콘 =====
            float gateW = iconArea.Width * 0.48f;

            // 🔥 높이를 아이콘 영역 거의 전체로
            float gateH = iconArea.Height * 0.72f;

            // 🔥 위에서 시작 위치를 더 위로
            float gateX = iconArea.X + (iconArea.Width - gateW) / 2f;
            float gateY = drawText
    ? iconArea.Y + iconArea.Height * 0.12f
    : iconArea.Y + (iconArea.Height - gateH) / 2f; // 🔥 접힘 시 정중앙
            using (var br = new SolidBrush(Color.FromArgb(235, 248, 255)))
            using (var pen = new Pen(Color.FromArgb(70, Color.Black), 1f))
            {
                float pillarWidth = gateW * 0.12f;
                float pillarTop = gateY + gateH * 0.18f;
                float pillarHeight = drawText
    ? gateH * 1.0f        // 펼쳤을 때 (기존 그대로)
    : gateH * 0.8f;     // 🔥 접었을 때 다리 짧게

                // 왼쪽 기둥
                g.FillRectangle(br, gateX, pillarTop, pillarWidth, pillarHeight);
                g.DrawRectangle(pen, gateX, pillarTop, pillarWidth, pillarHeight);

                // 오른쪽 기둥
                g.FillRectangle(br, gateX + gateW - pillarWidth, pillarTop, pillarWidth, pillarHeight);
                g.DrawRectangle(pen, gateX + gateW - pillarWidth, pillarTop, pillarWidth, pillarHeight);

                // 상단 바
                g.FillRectangle(br, gateX, gateY, gateW, gateH * 0.18f);
                g.DrawRectangle(pen, gateX, gateY, gateW, gateH * 0.18f);
            }

            // ===== Wave =====
            using (var pen = new Pen(Color.White, square.Width * 0.035f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                float cx = gateX + gateW / 2f;
                float cy = gateY + gateH * 0.6f;

                PointF[] wave =
                {
                    new PointF(cx - gateW * 0.35f, cy),
                    new PointF(cx - gateW * 0.15f, cy),
                    new PointF(cx - gateW * 0.05f, cy - gateH * 0.25f),
                    new PointF(cx + gateW * 0.05f, cy + gateH * 0.25f),
                    new PointF(cx + gateW * 0.2f,  cy),
                    new PointF(cx + gateW * 0.35f, cy)
                };

                g.DrawLines(pen, wave);
            }

            // ===== 체크 뱃지 =====
            float badgeSize = square.Width * 0.2f;
            var badge = new RectangleF(
                square.Right - badgeSize * 1.05f,
                square.Top + badgeSize * 0.15f,
                badgeSize,
                badgeSize);

            using (var path = CreateRoundPath(badge, badgeSize / 2f))
            using (var br = new SolidBrush(Color.FromArgb(35, 210, 120)))
            {
                g.FillPath(br, path);
            }

            using (var pen = new Pen(Color.White, badgeSize * 0.15f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                g.DrawLines(pen, new[]
                {
                    new PointF(badge.X + badge.Width * 0.25f, badge.Y + badge.Height * 0.55f),
                    new PointF(badge.X + badge.Width * 0.45f, badge.Y + badge.Height * 0.75f),
                    new PointF(badge.X + badge.Width * 0.75f, badge.Y + badge.Height * 0.3f),
                });
            }

            // ===== 텍스트: 파란 네모 "아래 남는 영역" 기준 정중앙 =====
            if (square.Width >= 70) // 접히면 바로 안 그림
            {
                using (var f1 = new Font("Segoe UI", square.Width * 0.11f, FontStyle.Bold))
                using (var f2 = new Font("Segoe UI", square.Width * 0.075f, FontStyle.Regular))
                {
                    RectangleF pureGateRect = new RectangleF(
                        textArea.X,
                        textArea.Y,
                        textArea.Width,
                        textArea.Height * 0.55f);

                    RectangleF inspectRect = new RectangleF(
                        textArea.X,
                        textArea.Y + textArea.Height * 0.45f,
                        textArea.Width,
                        textArea.Height * 0.45f);

                    TextRenderer.DrawText(
                        g,
                        "PureGate",
                        f1,
                        Rectangle.Round(pureGateRect),
                        Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    TextRenderer.DrawText(
                        g,
                        "INSPECTION",
                        f2,
                        Rectangle.Round(inspectRect),
                        Color.FromArgb(220, 235, 245),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }

        private static GraphicsPath CreateRoundPath(RectangleF rect, float radius)
        {
            float d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}