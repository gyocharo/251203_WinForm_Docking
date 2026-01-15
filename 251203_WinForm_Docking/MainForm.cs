using _251203_WinForm_Docking.Core;
using _251203_WinForm_Docking.Setting;
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

namespace _251203_WinForm_Docking
{
    public partial class MainForm : Form
    {
        private static DockPanel _dockPanel;
        public MainForm()
        {
            InitializeComponent();

            _dockPanel = new DockPanel
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(_dockPanel);

            _dockPanel.Theme = new VS2015BlueTheme();

            LoadDockingWindows();

            Global.Inst.Initialize();
        }

        private void LoadDockingWindows()
        {
            CameraForm cameraForm = new CameraForm();
            cameraForm.Show(_dockPanel, DockState.Document);

            /*ResultForm resultForm = new ResultForm();
            resultForm.Show(cameraForm.Pane, DockAlignment.Bottom, 0.2);*/

            PropertiesForm propForm = new PropertiesForm();
            propForm.Show(_dockPanel, DockState.DockRight);

            StatisticForm statisticForm = new StatisticForm();
            statisticForm.Show(_dockPanel, DockState.DockRight);

            RunForm runForm = new RunForm();
            runForm.Show(cameraForm.Pane, DockAlignment.Bottom, 0.3);

            var modelTreeWindow = new ModelTreeForm();
            modelTreeWindow.Show(cameraForm.Pane, DockAlignment.Right, 0.3);

            //로그폼 크기 변경
            /*LogForm logForm = new LogForm();
            logForm.Show(propForm.Pane, DockAlignment.Bottom, 0.4);*/
        }
        public static T GetDockForm<T>() where T : DockContent
        {
            var findForm = _dockPanel.Contents.OfType<T>().FirstOrDefault();
            return findForm;
        }

        private void imageOpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CameraForm cameraForm = GetDockForm<CameraForm>();
            if (cameraForm is null)
                return;

            using(OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "이미지 파일 선택";
                openFileDialog.Filter = "Image Files |*.bmp;*.jpg;*.jpeg;*.png;*.gif";
                openFileDialog.InitialDirectory = @"C:\Users\user\Desktop\강의자료\dataset";
                openFileDialog.Multiselect = false;
                if(openFileDialog .ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog .FileName;
                    cameraForm.LoadImage(filePath);
                }
            }
        }

        private void SetupMenuItem_Click(object sender, EventArgs e)
        {
            SetupForm setupForm = new SetupForm();
            setupForm.ShowDialog();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Global.Inst.Dispose();
        }
    }
}
