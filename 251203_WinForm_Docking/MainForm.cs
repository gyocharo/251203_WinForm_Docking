using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace _251203_WinForm_Docking
{
    public partial class MainForm : Form
    {
        private static DockPanel _dockPanel;
        public MainForm()
        {
            InitializeComponent();

            _dockPanel = new DockPanel()
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(_dockPanel);

            _dockPanel.Theme = new VS2015BlueTheme();

            LoadDockingWindows();
        }

        private void LoadDockingWindows()
        {
            CameraForm cameraForm = new CameraForm();
            cameraForm.Show(_dockPanel, DockState.Document);

            ResultForm resultForm = new ResultForm();
            resultForm.Show(cameraForm.Pane, DockAlignment.Bottom, 0.3);

            PropertiesForm propForm = new PropertiesForm();
            propForm.Show(_dockPanel, DockState.DockRight);

            StatisticForm statisticForm = new StatisticForm();
            statisticForm.Show(_dockPanel, DockState.DockRight);

            //로그폼 크기 변경
            LogForm logForm = new LogForm();
            logForm.Show(propForm.Pane, DockAlignment.Bottom, 0.5);

        }
    }
}
