using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using _251203_WinForm_Docking.Grab;

namespace _251203_WinForm_Docking.Setting
{
    public partial class CameraSetting : UserControl
    {
        public CameraSetting()
        {
            InitializeComponent();

            LoadSetting();
        }

        private void LoadSetting()
        {
            cbCameraType.DataSource = Enum.GetValues(typeof(CameraType)).Cast<CameraType>().ToList();

            cbCameraType.SelectedIndex = (int)SettingXml.Inst.CamType;
        }

        private void SaveSetting()
        {
            SettingXml.Inst.CamType = (CameraType)cbCameraType.SelectedIndex;

            SettingXml.Save();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            SaveSetting();
        }
    }
}
