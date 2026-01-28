using System;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using PureGate.UIControl;

namespace PureGate
{
    public partial class CountForm : DockContent
    {
        private RecentNGimages recentNGimages;

        public CountForm()
        {
            InitializeComponent();
            this.Load += CountForm_Load;
            this.HideOnClose = true;
        }

        private void CountForm_Load(object sender, EventArgs e)
        {
            if (recentNGimages != null) return;

            recentNGimages = new RecentNGimages
            {
                Dock = DockStyle.Fill
            };

            this.Controls.Add(recentNGimages);
            recentNGimages.BringToFront();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            recentNGimages?.Dispose();
            recentNGimages = null;
            base.OnFormClosed(e);
        }
    }
}