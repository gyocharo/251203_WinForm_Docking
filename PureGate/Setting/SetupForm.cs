using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PureGate.Setting
{
    public enum SettingType
    {
        SettingPath = 0,
        SettingCamera
    }
    public partial class SetupForm : Form
    {
        public SetupForm()
        {
            InitializeComponent();

            InitTabControl();
        }

        private void InitTabControl()
        {
            CameraSetting cameraSetting = new CameraSetting();
            AddTabControl(cameraSetting, "Camera");

            PathSetting pathSetting = new PathSetting();
            AddTabControl(pathSetting, "path");

            CommunicatorSetting commSetting = new CommunicatorSetting();
            AddTabControl(commSetting, "Communicator");

            tabSetting.SelectTab(0);
        }
        private void AddTabControl(UserControl control, string tabName)
        {
            TabPage newTab = new TabPage(tabName)
            {
                Dock = DockStyle.Fill,
            };
            control.Dock = DockStyle.Fill;
            newTab.Controls.Add(control);
            tabSetting.TabPages.Add(newTab);
        }

    }
}
