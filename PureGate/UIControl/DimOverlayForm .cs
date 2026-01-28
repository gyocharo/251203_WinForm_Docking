using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PureGate.UIControl
{
    public partial class DimOverlayForm : Form
    {
        private readonly Form _owner;

        public DimOverlayForm(Form owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));

            // 폼 기본
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;

            BackColor = Color.Black;
            Opacity = 0.35; // 딤 강도
            Bounds = _owner.Bounds;

            Owner = _owner;
            TopMost = _owner.TopMost;

            // owner 따라다니기
            _owner.LocationChanged += OwnerChanged;
            _owner.SizeChanged += OwnerChanged;
        }

        private void OwnerChanged(object sender, EventArgs e)
        {
            if (!_owner.IsDisposed)
                Bounds = _owner.Bounds;
        }

        protected override bool ShowWithoutActivation => true;
    }
}