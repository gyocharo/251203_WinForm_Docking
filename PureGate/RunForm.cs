using PureGate.Core;
using PureGate.Setting;
using PureGate.UIControl;
using PureGate.Util;
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

namespace PureGate
{
    public partial class RunForm : DockContent
    {
        public RunForm()
        {
            InitializeComponent();
            UpdateCameraButtonState();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            UpdateCameraButtonState();
        }

        private void UpdateCameraButtonState()
        {
            bool hasCamera = SettingXml.Inst.CamType != Grab.CameraType.None;

            btnGrab.Enabled = hasCamera;
            btnLive.Enabled = hasCamera;
        }

        public void RefreshCameraButtons()
        {
            UpdateCameraButtonState();
        }

        private void btnGrab_Click_1(object sender, EventArgs e)
        {
            if (SettingXml.Inst.CamType == Grab.CameraType.None)
            {
                MsgBox.Show("카메라가 설정되지 않아 검사를 시작할 수 없습니다.");
                return;
            }

            Global.Inst.InspStage.Grab(0);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (SettingXml.Inst.CamType == Grab.CameraType.WebCam)
                {
                    MsgBox.Show("카메라 세팅을 None으로 변경해주세요.");
                    return;
                }

                string serialID = $"{DateTime.Now:MM-dd HH:mm:ss}";
                Global.Inst.InspStage.InspectReady("LOT_NUMBER", serialID);

                Global.Inst.InspStage.SetWorkingState(WorkingState.INSPECT);

                if (SettingXml.Inst.CamType == Grab.CameraType.None)
                {
                    bool cycleMode = SettingXml.Inst.CycleMode;
                    Global.Inst.InspStage.CycleInspect(cycleMode);
                }
                else
                {
                    Global.Inst.InspStage.StartAutoRun();
                }
            }
            catch (ArgumentException ex)
            {
                // 경로 형식 오류가 대표적으로 여기로 옴
                SLogger.Write(ex.ToString());
                MsgBox.Show("경로 설정이 올바르지 않아 검사를 시작할 수 없습니다.\r\n설정 > Path에서 저장 경로를 확인해주세요.");
            }
            catch (Exception ex)
            {
                // 혹시 다른 예외도 사용자 메시지로 막아두기
                SLogger.Write(ex.ToString());
                MsgBox.Show("검사 시작 중 오류가 발생했습니다.\r\n설정을 확인 후 다시 시도해주세요.");
            }
        }

        private void btnLive_Click(object sender, EventArgs e)
        {
            if (SettingXml.Inst.CamType == Grab.CameraType.None)
            {
                MsgBox.Show("카메라가 설정되지 않아 Live를 실행할 수 없습니다.");
                return;
            }

            Global.Inst.InspStage.LiveMode = !Global.Inst.InspStage.LiveMode;

            if (Global.Inst.InspStage.LiveMode)
            {
                Global.Inst.InspStage.SetWorkingState(WorkingState.LIVE);  // ✅ 추가
                Global.Inst.InspStage.Grab(0);
            }
            else
            {
                Global.Inst.InspStage.SetWorkingState(WorkingState.NONE);  // ✅ 추가
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (SettingXml.Inst.CamType == Grab.CameraType.WebCam)
            {
                MsgBox.Show("카메라 세팅을 None으로 변경해주세요.");
                return;
            }

            Global.Inst.InspStage.StopCycle();
        }


    }
}
