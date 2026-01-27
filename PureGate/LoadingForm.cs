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
        private SvgLikeLogo _logo;
        private Label _lblText;
        private ProgressBar _progress;

        public LoadingForm()
        {
            InitializeComponent();
            BuildUi();
        }

        private void BuildUi()
        {
            // ===== Form Base =====
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(420, 260);
            BackColor = Color.White;
            DoubleBuffered = true;
            ShowInTaskbar = false;
            TopMost = true;

            // ===== Logo =====
            _logo = new SvgLikeLogo
            {
                Size = new Size(110, 120),
                Location = new Point((Width - 110) / 2, 40)
            };
            Controls.Add(_logo);

            // ===== Text =====
            _lblText = new Label
            {
                Text = "Starting...",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 80, 80),
                Size = new Size(Width, 30),
                Location = new Point(0, 165)
            };
            Controls.Add(_lblText);

            // ===== Progress =====
            _progress = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Size = new Size(260, 10),
                Location = new Point((Width - 260) / 2, 205)
            };
            Controls.Add(_progress);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = ClientRectangle;

            using (var br = new LinearGradientBrush(
                r,
                Color.FromArgb(245, 250, 255),
                Color.White,
                LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(br, r);
            }
        }

        public void SetStatus(string text)
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetStatus(text)));
                return;
            }

            _lblText.Text = text;
            _lblText.Refresh();
        }
    }
}
