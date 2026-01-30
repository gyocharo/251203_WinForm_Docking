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
        // ✅ 추가: 선택된 ROI 타입 저장
        private InspWindowType _selectedRoiType = InspWindowType.Base;
        public CameraForm()
        {
            InitializeComponent();

            this.FormClosed += CameraForm_FormClosed;

            imageViewer.DiagramEntityEvent += ImageViewer_DiagramEntityEvent;

            mainViewToolbar.ButtonChanged += Toolbar_ButtonChanged;

            // ✅ 추가: ROI 타입 선택 이벤트 연결
            mainViewToolbar.RoiTypeSelected += Toolbar_RoiTypeSelected;

            imageViewer.NewRoiCanceled += (s, e) => { mainViewToolbar.SetSetRoiChecked(false); };

        }
        public List<InspWindow> GetSelectedWindows()
        {
            return imageViewer?.GetSelectedWindows() ?? new List<InspWindow>();
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

                // ✅ InspArea가 설정되어 있으면 InspArea, 아니면 WindowArea
                // InspArea는 Alignment 후에만 값이 있고, 평소에는 Empty
                Rect roiToDisplay = (window.InspArea.Width > 0 && window.InspArea.Height > 0)
                    ? window.InspArea
                    : window.WindowArea;
                // ✅ 디버깅 로그 추가 (임시)
                SLogger.Write($"[UpdateDiagram] {window.InspWindowType}: " +
                             $"WindowArea=({window.WindowArea.X},{window.WindowArea.Y}), " +
                             $"InspArea=({window.InspArea.X},{window.InspArea.Y}), " +
                             $"Display=({roiToDisplay.X},{roiToDisplay.Y})");
                DiagramEntity entity = new DiagramEntity()
                {
                    LinkedWindow = window,
                    EntityROI = new Rectangle(
                        roiToDisplay.X, roiToDisplay.Y,
                        roiToDisplay.Width, roiToDisplay.Height),
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
                case ToolbarButton.ShowROI:
                    {
                        bool show = e.IsChecked;
                        imageViewer.ShowROI = show;
                        imageViewer.Invalidate();
                    }
                    break;
                case ToolbarButton.SetROI:
                    {
                        bool isDrawMode = e.IsChecked;

                        if (isDrawMode)
                        {
                            // ROI 그리기 모드 활성화
                            imageViewer.NewRoi(_selectedRoiType);
                        }
                        else
                        {
                            // ROI 그리기 모드 비활성화
                            imageViewer.CancelNewRoi();
                        }
                    }
                    break;
                case ToolbarButton.SetGolden:  // ✅ 추가
                    {
                        OnSetGolden();
                    }
                    break;
                case ToolbarButton.ChannelColor:
                case ToolbarButton.ChannelGray:
                case ToolbarButton.ChannelRed:
                case ToolbarButton.ChannelGreen:
                case ToolbarButton.ChannelBlue:
                    {
                        eImageChannel channel = eImageChannel.Color;
                        if (e.Button == ToolbarButton.ChannelGray)
                            channel = eImageChannel.Gray;
                        else if (e.Button == ToolbarButton.ChannelRed)
                            channel = eImageChannel.Red;
                        else if (e.Button == ToolbarButton.ChannelGreen)
                            channel = eImageChannel.Green;
                        else if (e.Button == ToolbarButton.ChannelBlue)
                            channel = eImageChannel.Blue;

                        SetImageChannel(channel);
                        _currentImageChannel = channel;
                    }
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

        // ✅ ROI 타입 선택 이벤트 핸들러
        private void Toolbar_RoiTypeSelected(object sender, RoiTypeSelectedEventArgs e)
        {
            _selectedRoiType = e.WindowType;

            // ROI 그리기 모드 시작
            imageViewer.NewRoi(_selectedRoiType);
            imageViewer.Focus();

            SLogger.Write($"ROI 타입 선택: {_selectedRoiType}");
        }

        // 1) 다중 선택된 InspWindow 목록을 반환하는 메서드 추가
        public List<InspWindow> GetSelectedInspWindows()
        {
            return imageViewer?.GetSelectedWindows() ?? new List<InspWindow>();
        }

        // 3) SetGolden 버튼 클릭 시 다중 선택된 ROI들의 Golden Reference 설정
        private void OnSetGolden()
        {
            try
            {
                // 다중 선택된 InspWindow 목록 가져오기
                List<InspWindow> selectedWindows = GetSelectedInspWindows();

                if (selectedWindows == null || selectedWindows.Count == 0)
                {
                    MsgBox.Show("Golden Reference로 설정할 ROI를 선택해주세요.");
                    return;
                }

                // 현재 표시 중인 이미지 가져오기
                Mat currentImage = GetDisplayImage();
                if (currentImage == null || currentImage.Empty())
                {
                    MsgBox.Show("현재 이미지가 없습니다. 먼저 이미지를 로드해주세요.");
                    return;
                }

                // 선택된 각 ROI에 대해 Golden Reference 설정
                int successCount = 0;
                foreach (var window in selectedWindows)
                {
                    if (window == null) continue;

                    // ROI 유효성 체크
                    if (window.WindowArea.Right >= currentImage.Width ||
                        window.WindowArea.Bottom >= currentImage.Height)
                    {
                        SLogger.Write($"[SetGolden] ROI 범위 오류: {window.UID}");
                        continue;
                    }

                    // RuleBasedAlgorithm 찾기
                    RuleBasedAlgorithm ruleAlgo = null;
                    foreach (var algo in window.AlgorithmList)
                    {
                        if (algo.InspectType == InspectType.InspRuleBased)
                        {
                            ruleAlgo = algo as RuleBasedAlgorithm;
                            break;
                        }
                    }

                    // RuleBasedAlgorithm이 없으면 새로 생성
                    if (ruleAlgo == null)
                    {
                        ruleAlgo = new RuleBasedAlgorithm
                        {
                            WindowType = window.InspWindowType,
                            IsUse = true
                        };
                        window.AlgorithmList.Add(ruleAlgo);
                        SLogger.Write($"[SetGolden] RuleBasedAlgorithm 새로 생성: {window.UID}");
                    }
                    else
                    {
                        ruleAlgo.IsUse = true;
                    }

                    // ✅ Base 또는 Body 윈도우인 경우: MatchAlgorithm도 함께 설정 (Alignment용)
                    MatchAlgorithm matchAlgo = null;
                    if (window.InspWindowType == InspWindowType.Base ||
                        window.InspWindowType == InspWindowType.Body)
                    {
                        matchAlgo = window.FindInspAlgorithm(InspectType.InspMatch) as MatchAlgorithm;
                        if (matchAlgo != null)
                        {
                            matchAlgo.IsUse = true; // MatchAlgorithm 활성화
                            SLogger.Write($"[SetGolden] {window.InspWindowType} 윈도우의 MatchAlgorithm 활성화: {window.UID}");
                        }
                    }

                    // ✅ 중요: 다른 알고리즘은 비활성화 (RuleBased와 Match는 제외!)
                    foreach (var algo in window.AlgorithmList)
                    {
                        if (algo.InspectType != InspectType.InspRuleBased &&
                            algo.InspectType != InspectType.InspMatch)
                        {
                            algo.IsUse = false;
                        }
                    }

                    // ROI 영역 추출 및 Golden 이미지 설정
                    using (Mat roiImage = currentImage[window.WindowArea])
                    {
                        // WindowType 설정 (Base/Body/Sub 구분)
                        ruleAlgo.WindowType = window.InspWindowType;

                        // Golden 이미지 주입
                        bool ruleSuccess = ruleAlgo.SetGoldenImage(roiImage);

                        // ✅ Base 또는 Body 윈도우면 MatchAlgorithm에도 Template 설정
                        bool matchSuccess = true;
                        if ((window.InspWindowType == InspWindowType.Base ||
                             window.InspWindowType == InspWindowType.Body) &&
                            matchAlgo != null)
                        {
                            // Gray 변환
                            Mat grayImage = new Mat();
                            if (roiImage.Channels() == 3)
                            {
                                Cv2.CvtColor(roiImage, grayImage, ColorConversionCodes.BGR2GRAY);
                            }
                            else
                            {
                                grayImage = roiImage.Clone();
                            }

                            // MatchAlgorithm의 Template 초기화 후 추가
                            matchAlgo.ResetTemplateImages();
                            matchAlgo.AddTemplateImage(grayImage);

                            grayImage.Dispose();

                            SLogger.Write($"[SetGolden] {window.InspWindowType} 윈도우의 MatchAlgorithm Template 설정: {window.UID}");
                        }

                        if (ruleSuccess && matchSuccess)
                        {
                            SLogger.Write($"[SetGolden] Success: {window.UID} ({window.InspWindowType})");
                            successCount++;
                        }
                        else
                        {
                            SLogger.Write($"[SetGolden] Failed to set golden: {window.UID}");
                            // 에러 상세 로그
                            if (ruleAlgo.ResultString != null && ruleAlgo.ResultString.Count > 0)
                            {
                                string lastError = ruleAlgo.ResultString[ruleAlgo.ResultString.Count - 1];
                                SLogger.Write($"[SetGolden] Error detail: {lastError}");
                            }
                        }
                    }
                }

                // Golden 이미지 파일로도 저장 (선택 사항)
                if (successCount > 0)
                {
                    // InspStage의 SaveGoldenImages 호출하여 파일로도 저장
                    Global.Inst.InspStage.SaveGoldenImagesFromSelected(selectedWindows);

                    MsgBox.Show($"{successCount}개의 ROI에 Golden Reference가 설정되었습니다.");
                    SLogger.Write($"[SetGolden] 총 {successCount}개 ROI의 Golden Reference 설정 완료");
                }
                else
                {
                    MsgBox.Show("Golden Reference 설정에 실패했습니다.");
                }
            }
            catch (Exception ex)
            {
                SLogger.Write($"[SetGolden] 오류: {ex.Message}");
                MsgBox.Show($"Golden Reference 설정 중 오류가 발생했습니다.\n{ex.Message}");
            }
        }
    }
}