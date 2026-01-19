using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using _251203_WinForm_Docking.Sequence;
using _251203_WinForm_Docking.Util;

namespace _251203_WinForm_Docking.Setting
{
    public partial class CommunicatorSetting : UserControl
    {
        public CommunicatorSetting()
        {
            InitializeComponent();

            LoadSetting();
        }

        private void LoadSetting()
        {
            cmbCommType.DataSource = Enum.GetValues(typeof(CommunicatorType)).Cast<CommunicatorType>().ToArray();

            txtMachine.Text = SettingXml.Inst.MachineName;

            cmbCommType.SelectedIndex = (int)SettingXml.Inst.CommType;

            txtIpAddr.Text = SettingXml.Inst.CommIP;
        }

        private void SaveSetting()
        {
            SettingXml.Inst.MachineName = txtMachine.Text;

            SettingXml.Inst.CommType = (CommunicatorType)cmbCommType.SelectedIndex;

            SettingXml.Inst.CommIP = txtIpAddr.Text;

            SettingXml.Save();

            SLogger.Write($"통신 설정 저장");
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            SaveSetting();
        }
    }
}
