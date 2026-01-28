using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using PureGate.Util;
using PureGate.UIControl;

namespace PureGate
{
    public partial class CountForm : DockContent
    {
        private RecentNGimages recentNGimages;

        public CountForm()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            // RecentNGimages UserControl 생성 및 추가
            recentNGimages = new RecentNGimages
            {
                Dock = DockStyle.Fill
            };

            this.Controls.Add(recentNGimages);
        }
    }
}
