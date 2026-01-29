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
using System.IO;
using PureGate.Core;
using OpenCvSharp;
using PureGate.Algorithm;
using PureGate.Teach;
using PureGate.UIControl;
using PureGate.Util;

namespace PureGate
{
    public partial class CameraForm : DockContent
    {
        eImageChannel _currentImageChannel = eImageChannel.Color;
        public CameraForm()
        {
            InitializeComponent();

            this.FormClosed += CameraForm_FormClosed;

            imageViewer.DiagramEntityEvent += ImageViewer_DiagramEntityEvent;

            mainViewToolbar.ButtonChanged += Toolbar_ButtonChanged;

        }

        private void ImageViewer_DiagramEntityEvent(object sender, DiagramEntityEventArgs e)
        {
            SLogger.Write($"ImageViewer Action {e.ActionType.ToString()}");
            switch (e.ActionType)
            {
                case EntityActionType.Select:
                    Global.Inst.InspStage.SelectInspWindow(e.InspWindow);
                    imageViewer.Focus();
                    break;
                case EntityActionType.Inspect:
                    UpdateDiagramEntity();
                    Global.Inst.InspStage.TryInspection(e.InspWindow);
                    break;
                case EntityActionType.Add:
                    Global.Inst.InspStage.AddInspWindow(e.WindowType, e.Rect);
                    break;
                case EntityActionType.Copy:
                    Global.Inst.InspStage.AddInspWindow(e.InspWindow, e.OffsetMove);
                    break;
                case EntityActionType.Move:
                    Global.Inst.InspStage.MoveInspWindow(e.InspWindow, e.OffsetMove);
                    break;
                case EntityActionType.Resize:
                    Global.Inst.InspStage.ModifyInspWindow(e.InspWindow, e.Rect);
                    break;
                case EntityActionType.Delete:
                    Global.Inst.InspStage.DelInspWindow(e.InspWindow);
                    break;
                case EntityActionType.DeleteList:
                    Global.Inst.InspStage.DelInspWindow(e.InspWindowList);
                    break;
            }
        }

        public void LoadImage(string filePath)
        {
            if (File.Exists(filePath) == false)
                return;

            Image bitmap = Image.FromFile(filePath);
            imageViewer.LoadBitmap((Bitmap)bitmap);
        }

        public Mat GetDisplayImage()
        {
            return Global.Inst.InspStage.ImageSpace.GetMat(0, _currentImageChannel);
        }

        private void CameraForm_Resize(object sender, EventArgs e)
        {
            int margin = 0;
            imageViewer.Width = this.Width - mainViewToolbar.Width - margin * 2;
            imageViewer.Height = this.Height - margin * 2;

            imageViewer.Location = new System.Drawing.Point(margin, margin);
        }

        public void UpdateDisplay(Bitmap bitmap = null)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateDisplay(bitmap)));
                return;
            }

            if (bitmap == null)
            {
                //#6_INSP_STAGE#3 업데이트시 bitmap이 없다면 InspSpace에서 가져온다
                bitmap = Global.Inst.InspStage.GetBitmap(0, _currentImageChannel);
                if (bitmap == null)
                    return;
            }

            if (imageViewer != null)
                imageViewer.LoadBitmap(bitmap);

        }

        public void UpdateImageViewer()
        {
            imageViewer.UpdateInspParam();
            imageViewer.Invalidate();
        }

        public void UpdateDiagramEntity()
        {
            imageViewer.ResetEntity();

            Model model = Global.Inst.InspStage.CurModel;
            List<DiagramEntity> diagramEntityList = new List<DiagramEntity>();

            foreach (InspWindow window in model.InspWindowList)
            {
                if (window is null)
                    continue;

                DiagramEntity entity = new DiagramEntity()
                {
                    LinkedWindow = window,
                    EntityROI = new Rectangle(
                        window.WindowArea.X, window.WindowArea.Y,
                            window.WindowArea.Width, window.WindowArea.Height),
                    EntityColor = imageViewer.GetWindowColor(window.InspWindowType),
                    IsHold = window.IsTeach
                };
                diagramEntityList.Add(entity);
            }

            imageViewer.SetDiagramEntityList(diagramEntityList);
        }

        public void SelectDiagramEntity(InspWindow window)
        {
            imageViewer.SelectDiagramEntity(window);
        }

        public void ResetDisplay()
        {
            imageViewer.ResetEntity();
        }

        public void AddRect(List<DrawInspectInfo> rectInfos)
        {
            imageViewer.AddRect(rectInfos);
        }

        public void AddRoi(InspWindowType inspWindowType)
        {
            imageViewer.NewRoi(inspWindowType);
        }

        public void SetInspResultCount(int totalArea, int okCnt, int ngCnt)
        {
            imageViewer.SetInspResultCount(new InspectResultCount(totalArea, okCnt, ngCnt));

        }

        public void SetWorkingState(WorkingState workingState)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetWorkingState(workingState)));
                return;
            }

            string state = "";
            switch (workingState)
            {
                case WorkingState.INSPECT: state = "INSPECT"; break;
                case WorkingState.LIVE: state = "LIVE"; break;
                case WorkingState.ALARM: state = "ALARM"; break;
                default: state = ""; break; // NONE
            }

            // 기존 결과(OK/NG) 유지: "STATE\nRESULT" 형태 지원
            string ws = imageViewer.WorkingState ?? "";
            string curState = "";
            string curResult = "";

            var parts = ws.Split('\n');
            if (parts.Length >= 2)
            {
                curState = parts[0] ?? "";
                curResult = parts[1] ?? "";
            }
            else
            {
                if (ws == "OK" || ws == "NG") curResult = ws;
                else curState = ws;
            }

            if (string.IsNullOrEmpty(state))
                imageViewer.WorkingState = curResult;                 // NONE이면 결과만 남김(호환)
            else if (string.IsNullOrEmpty(curResult))
                imageViewer.WorkingState = state;                     // 결과 없으면 상태만
            else
                imageViewer.WorkingState = state + "\n" + curResult;  // 상태+결과

            imageViewer.Invalidate();
        }

        private void Toolbar_ButtonChanged(object sender, ToolbarEventArgs e)
        {
            switch (e.Button)
            {
                case ToolbarButton.ChannelColor:
                    _currentImageChannel = eImageChannel.Color;
                    UpdateDisplay();
                    break;
                case ToolbarButton.ChannelGray:
                    _currentImageChannel = eImageChannel.Gray;
                    UpdateDisplay();
                    break;
                case ToolbarButton.ChannelRed:
                    _currentImageChannel = eImageChannel.Red;
                    UpdateDisplay();
                    break;
                case ToolbarButton.ChannelGreen:
                    _currentImageChannel = eImageChannel.Green;
                    UpdateDisplay();
                    break;
                case ToolbarButton.ChannelBlue:
                    _currentImageChannel = eImageChannel.Blue;
                    UpdateDisplay();
                    break;
            }
        }

        public void SetImageChannel(eImageChannel channel)
        {
            mainViewToolbar.SetSelectButton(channel);
            UpdateDisplay();
        }

        private void CameraForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            mainViewToolbar.ButtonChanged -= Toolbar_ButtonChanged;

            imageViewer.DiagramEntityEvent -= ImageViewer_DiagramEntityEvent;

            this.FormClosed -= CameraForm_FormClosed;
        }

        public void ShowResultOnScreen(bool isOK)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowResultOnScreen(isOK)));
                return;
            }

            string newResult = isOK ? "OK" : "NG";

            // 기존 상태(INSPECT/LIVE/ALARM) 보존 
            SplitWorkingState(imageViewer.WorkingState, out string curState, out string curResult);

            imageViewer.WorkingState = ComposeWorkingState(curState, newResult);
            imageViewer.Invalidate();
        }

        private static void SplitWorkingState(string ws, out string state, out string result)
        {
            state = "";
            result = "";

            if (string.IsNullOrEmpty(ws))
                return;

            var parts = ws.Split('\n');

            if (parts.Length >= 2)
            {
                state = parts[0] ?? "";
                result = parts[1] ?? "";
                return;
            }

            // 기존 호환: "OK" / "NG"만 들어오던 케이스
            if (ws == "OK" || ws == "NG")
                result = ws;
            else
                state = ws;
        }

        private static string ComposeWorkingState(string state, string result)
        {
            state = state ?? "";
            result = result ?? "";

            // 결과만 있으면 예전처럼 "OK"/"NG"로 유지 가능(호환성 ↑)
            if (string.IsNullOrEmpty(state))
                return result;

            // 상태만 있으면 상태만
            if (string.IsNullOrEmpty(result))
                return state;

            // 상태 + 결과 (2줄)
            return state + "\n" + result;
        }
    }
}