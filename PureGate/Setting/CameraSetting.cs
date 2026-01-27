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
            _camType = SettingXml.Inst.CamType;
            switch(_camType){
                case CameraType.WebCam:
                    if (!int.TryParse(tbExposure.Text, out int Webexposure))
                    {
                        MessageBox.Show(
                            "노출 값은 정수로 입력해주세요.\n(-8 ~ -1)",
                            "입력 오류",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        FocusExposure();
                        return;
                    }

                    // 2️⃣ 범위 체크
                    if (Webexposure < -8 || Webexposure > -1)
                    {
                        MessageBox.Show(
                            "노출 값은 -8 ~ 1 사이만 가능합니다.",
                            "입력 오류",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        FocusExposure();
                        return;
                    }

                    SettingXml.Inst.ExposureTime = Webexposure;

                    Global.Inst.InspStage.ApplyCameraSetting();

                    break;

                case CameraType.HikRobot:
                    if (!int.TryParse(tbExposure.Text, out int Hikexposure))
                    {
                        MessageBox.Show(
                            "노출 값은 정수로 입력해주세요.\n(-8 ~ -1)",
                            "입력 오류",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        FocusExposure();
                        return;
                    }

                    // 2️⃣ 범위 체크
                    if (Hikexposure < -8 || Hikexposure > -1)
                    {
                        MessageBox.Show(
                            "노출 값은 -8 ~ 1 사이만 가능합니다.",
                            "입력 오류",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        FocusExposure();
                        return;
                    }

                    SettingXml.Inst.ExposureTime = Hikexposure;

                    Global.Inst.InspStage.ApplyCameraSetting();

                    break;
            }      
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
                tbExposure.Text = "";
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
    }
}
