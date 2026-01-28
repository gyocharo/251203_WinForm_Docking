using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PureGate.Grab;
using PureGate.Util;
using PureGate.Core;

namespace PureGate.Setting
{
    public partial class CameraSetting : UserControl
    {

        private CameraType _camType = CameraType.WebCam;
        public CameraSetting()
        {
            InitializeComponent();

            LoadSetting();
        }

        private void LoadSetting()
        {
            cbCameraType.DataSource = Enum.GetValues(typeof(CameraType)).Cast<CameraType>().ToList();

            cbCameraType.SelectedIndex = (int)SettingXml.Inst.CamType;

            long exposureTime = SettingXml.Inst.ExposureTime;

            long expTime;
            expTime = exposureTime;

            tbExposure.Text = expTime.ToString();
        }

        private void SaveSetting()
        {
            //환경설정에 카메라 타입 설정
            SettingXml.Inst.CamType = (CameraType)cbCameraType.SelectedIndex;

            long.TryParse(tbExposure.Text, out long expTime);
            SettingXml.Inst.ExposureTime = expTime; //ms → us
            //환경설정 저장
            SettingXml.Save();

            if (SettingXml.Inst.CamType != CameraType.None)
            {
                //카메라 재연결
                Global.Inst.InspStage.SetExposure(SettingXml.Inst.ExposureTime);
            }

            SLogger.Write($"카메라 설정 저장");
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            SaveSetting();
            Global.Inst.InspStage.ApplyCameraSetting();
        }

        private void FocusExposure()
        {
            tbExposure.Focus();
            tbExposure.SelectAll();
        }

        private void cbCameraType_SelectedIndexChanged(object sender, EventArgs e)
        {
            string camType = GetSelectedCameraType();

            if (camType == "HikRobot")
            {
                tbExposure.Enabled = true;
                lb_Exposure.Text = "(0 ~ 1,000,000)";
                tbExposure.Text = "10000";
            }
            else if(camType == "WebCam")
            {
                tbExposure.Enabled = true;
                lb_Exposure.Text = "(-8 ~ 1)";
                tbExposure.Text = "-6";
            }
            else{
                lb_Exposure.Text = "";
                tbExposure.Text = "0";
                tbExposure.Enabled = false;
            }
        }

        private string GetSelectedCameraType()
        {
            // ComboBox 사용 시
            if (cbCameraType.SelectedItem != null)
                return cbCameraType.SelectedItem.ToString();

            // 안전 fallback
            return "WebCam";
        }

        private void tbExposure_Leave(object sender, EventArgs e)
        {
            string camType = GetSelectedCameraType();

            if ((!int.TryParse(tbExposure.Text, out int Webexposure) || Webexposure < -8 || Webexposure > 1) && camType == "WebCam")
            {
                MessageBox.Show(
                    "-8 ~ 1 사이의 정수만 가능합니다.",
                    "입력 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                FocusExposure();
            }
            else if ((!long.TryParse(tbExposure.Text, out long Hikexposure) || Hikexposure < 0 || Hikexposure > 1000000) && camType == "HikRobot")
            {
                MessageBox.Show(
                    "0 ~ 1000000 사이의 정수만 가능합니다.",
                    "입력 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                FocusExposure();
            }
        }
    }
}
