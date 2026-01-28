using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PureGate.Core;
using PureGate.Setting;
using PureGate.UIControl;
using WeifenLuo.WinFormsUI.Docking;

namespace PureGate
{
    public partial class RunForm : DockContent
    {
        public RunForm()
        {
            InitializeComponent();


        }

        private void btnGrab_Click_1(object sender, EventArgs e)
        {
            Global.Inst.InspStage.Grab(0);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //#15_INSP_WORKER#10 카메라 타입에 따라 자동 검사 모드 설정

            //진짜 제품 시리얼”이 없으니까 시간을 시리얼처럼 임시로 쓰는 것.
            string serialID = $"{DateTime.Now:MM-dd HH:mm:ss}";
            Global.Inst.InspStage.InspectReady("LOT_NUMBER", serialID);

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

        private void btnLive_Click(object sender, EventArgs e)
        {
            Global.Inst.InspStage.LiveMode = !Global.Inst.InspStage.LiveMode;

            if (Global.Inst.InspStage.LiveMode)
            {
                Global.Inst.InspStage.Grab(0);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            Global.Inst.InspStage.StopCycle();
        }


    }
}
