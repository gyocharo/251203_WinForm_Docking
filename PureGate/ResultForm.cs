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
using PureGate.Inspect;
using PureGate.Teach;
using BrightIdeasSoftware;
using WeifenLuo.WinFormsUI.Docking;
using PureGate.Util;
using PureGate.Core;

namespace PureGate
{
    public partial class ResultForm : DockContent
    {
        private SplitContainer _splitContainer;
        private TreeListView _treeListView;
        private TextBox _txtDetails;
        
        // 검사 이력을 누적하기 위한 리스트
        private List<InspWindow> _inspectionHistory = new List<InspWindow>();
        
        // 최대 표시 개수
        private const int MAX_HISTORY_COUNT = 30;
        
        
        /// 최대 이력 개수 (기본값: 30)        
        public int MaxHistoryCount { get; set; } = MAX_HISTORY_COUNT;
        
        public ResultForm()
        {
            InitializeComponent();

            InitTreeListView();
        }

        private void InitTreeListView()
        {
            // SplitContainer 사용하여 상하 분할 레이아웃 구성
            _splitContainer = new SplitContainer()
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 120,
                Panel1MinSize = 70,
                Panel2MinSize = 40
            };

            //TreeListView 검사 결과 트리 생성
            _treeListView = new TreeListView()
            {
                Dock = DockStyle.Fill,
                FullRowSelect = true,
                ShowGroups = false,
                UseFiltering = true,
                OwnerDraw = true,
                MultiSelect = false,
                GridLines = true
            };
            _treeListView.SelectionChanged += TreeListView_SelectionChanged;

            _treeListView.CanExpandGetter = x => x is InspWindow;

            _treeListView.ChildrenGetter = x =>
            {
                if (x is InspWindow w)
                    return w.InspResultList;
                return new List<InspResult>();
            };

            //컬럼 추가
            var colLotNumber = new OLVColumn("Lot Number", "")
            {
                Width = 150,
                IsEditable = false,
                AspectGetter = obj =>
                {
                    if (obj is InspWindow win)
                        return win.UID;
                    if (obj is InspResult res)
                        return res.LotNumber ?? "";
                    return "";
                }
            };

            var colProductInfo = new OLVColumn("제품/Part ID, 바코드/시리얼", "")
            {
                Width = 200,
                IsEditable = false,
                AspectGetter = obj =>
                {
                    if (obj is InspWindow win)
                        return ""; // InspWindow 행에는 빈 값
                    if (obj is InspResult res)
                        return res.GetProductInfo();
                    return "";
                }
            };

            var colStatus = new OLVColumn("Status", "IsDefect")
            {
                Width = 120,
                TextAlign = HorizontalAlignment.Center,
                AspectGetter = obj =>
                {
                    if (obj is InspWindow win)
                        return ""; // InspWindow 행에는 빈 값
                    if (obj is InspResult res)
                        return res.GetStatusString();
                    return "";
                }
            };

            var colLocationInfo = new OLVColumn("카메라/라인/스테이션 번호", "")
            {
                Width = 220,
                TextAlign = HorizontalAlignment.Center,
                AspectGetter = obj =>
                {
                    if (obj is InspWindow win)
                        return ""; // InspWindow 행에는 빈 값
                    if (obj is InspResult res)
                        return res.GetLocationInfo();
                    return "";
                }
            };

            // 컬럼 추가
            _treeListView.Columns.AddRange(new OLVColumn[] { colLotNumber, colProductInfo, colStatus, colLocationInfo });


            // 검사 상세 정보 텍스트박스 생성
            _txtDetails = new TextBox()
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Arial", 10),
                ReadOnly = true
            };

            // 컨테이너에 컨트롤 추가
            _splitContainer.Panel1.Controls.Add(_treeListView);
            _splitContainer.Panel2.Controls.Add(_txtDetails);
            Controls.Add(_splitContainer);
        }

        
        /// 검사 결과를 ResultForm에 추가하고 표시합니다.
        /// ⚠️ 중요: 이 메서드는 이미지가 NG/OK 폴더에 저장된 후 호출되어야 합니다.
        /// 저장된 파일명(LOT-<lot>_PART-<part>_SN-<serial>_CAM-<cam>_LINE-<line>_ST-<station>.png)에서
        /// 정보를 파싱하여 컬럼에 표시합니다.
        
        /// <param name="savedImagePath">저장된 이미지의 전체 경로</param>
        /// <param name="inspWindow">검사 윈도우 객체 (복사본 전달 권장)</param>
        public void AddInspectionResult(string savedImagePath, InspWindow inspWindow)
        {
            // ✅ UI 스레드 체크
            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action(() => AddInspectionResult(savedImagePath, inspWindow)));
                }
                catch (ObjectDisposedException)
                {
                    SLogger.Write("[ResultForm] Form disposed, skipping update");
                }
                return;
            }

            if (inspWindow == null)
            {
                SLogger.Write("[ResultForm] AddInspectionResult - inspWindow is null", SLogger.LogType.Error);
                return;
            }

            SLogger.Write($"[ResultForm] AddInspectionResult - SavedPath: {savedImagePath}, Window: {inspWindow.UID}, Results: {inspWindow.InspResultList.Count}");

            // 저장된 파일명에서 파일명만 추출
            string imageFileName = string.Empty;
            if (!string.IsNullOrEmpty(savedImagePath))
            {
                imageFileName = Path.GetFileName(savedImagePath);
                SLogger.Write($"[ResultForm] Image FileName from saved path: {imageFileName}");
            }

            // 검사 시간 추가
            string timestamp = DateTime.Now.ToString("HH:mm:ss");

            // 이력에 추가할 윈도우 복사본 생성
            var historyWindow = new InspWindow(inspWindow.InspWindowType, inspWindow.Name);
            historyWindow.UID = $"{timestamp} - {inspWindow.UID}";
            historyWindow.WindowArea = inspWindow.WindowArea;
            historyWindow.InspArea = inspWindow.InspArea;

            // InspResult 복사 및 파일명 파싱
            foreach (var result in inspWindow.InspResultList)
            {
                var historyResult = new InspResult
                {
                    ObjectID = result.ObjectID,
                    InspType = result.InspType,
                    IsDefect = result.IsDefect,
                    ResultValue = result.ResultValue,
                    ResultInfos = result.ResultInfos,
                    ResultRectList = result.ResultRectList,
                    GroupID = result.GroupID
                };

                // ✅ 저장된 파일명에서 정보 파싱 (LOT-<lot>_PART-<part>_SN-<serial>_CAM-<cam>_LINE-<line>_ST-<station>.png)
                historyResult.ParseImageFileName(imageFileName);
                historyWindow.AddInspResult(historyResult);
            }

            // 이력에 추가 (맨 앞에 추가하여 최신 항목이 위에 표시되도록)
            _inspectionHistory.Insert(0, historyWindow);

            // 최대 개수까지만 유지 (오래된 것부터 제거 - 마지막 항목 제거)
            while (_inspectionHistory.Count > MaxHistoryCount)
            {
                var oldest = _inspectionHistory[_inspectionHistory.Count - 1];
                _inspectionHistory.RemoveAt(_inspectionHistory.Count - 1);
                SLogger.Write($"[ResultForm] Removed oldest history: {oldest.UID}");
            }

            // TreeListView 업데이트
            _treeListView.SetObjects(_inspectionHistory);
            _treeListView.Expand(historyWindow);

            if (historyWindow.InspResultList.Count > 0)
            {
                InspResult inspResult = historyWindow.InspResultList[0];
                ShowDetail(inspResult);
            }

            SLogger.Write($"[ResultForm] Total inspection history: {_inspectionHistory.Count}/{MaxHistoryCount} windows");
        }        

        //실제 검사가 되었을때, 검사 결과를 추가하는 함수
        public void AddInspResult(InspResult inspResult)
        {
            if (inspResult == null)
            {
                SLogger.Write("[ResultForm] AddInspResult - inspResult is null", SLogger.LogType.Error);
                return;
            }

            SLogger.Write($"[ResultForm] AddInspResult - ObjectID: {inspResult.ObjectID}");

            // 이 메서드는 원래 용도대로 사용 (현재는 사용되지 않을 수 있음)
            // 필요시 구현
        }

        // 이력 초기화 메서드 (필요시 사용)
        public void ClearHistory()
        {
            _inspectionHistory.Clear();
            _treeListView.ClearObjects();
            _txtDetails.Clear();
            SLogger.Write("[ResultForm] Inspection history cleared");
        }

        //해당 트리 리스트 뷰 선택시, 상세 정보 텍스트 박스에 표시
        private void TreeListView_SelectionChanged(object sender, EventArgs e)
        {
            if (_treeListView.SelectedObject == null)
            {
                _txtDetails.Text = string.Empty;
                return;
            }

            if (_treeListView.SelectedObject is InspResult result)
            {
                ShowDetail(result);
            }
            else if (_treeListView.SelectedObject is InspWindow window)
            {
                var infos = window.InspResultList.Select(r => $" -{r.ObjectID}: {r.ResultInfos}").ToList();
                _txtDetails.Text = $"{window.UID}\r\n" +
                    string.Join("\r\n", infos);
            }
        }

        private void ShowDetail(InspResult result)
        {
            if (result == null)
                return;

            // 상세 정보 구성
            StringBuilder details = new StringBuilder();

            // 이미지 파일명 정보
            if (!string.IsNullOrEmpty(result.ImageFileName))
            {
                details.AppendLine($"=== 이미지 정보 ===");
                details.AppendLine($"파일명: {result.ImageFileName}");
                details.AppendLine($"Lot Number: {result.LotNumber}");
                details.AppendLine($"Part ID: {result.PartID}");
                details.AppendLine($"Serial Number: {result.SerialNumber}");
                details.AppendLine($"Camera: {result.CameraNumber}");
                details.AppendLine($"Line: {result.LineNumber}");
                details.AppendLine($"Station: {result.StationNumber}");
                details.AppendLine();
            }

            // 검사 결과 정보
            details.AppendLine($"=== 검사 결과 ===");
            details.AppendLine(result.ResultInfos.ToString());

            _txtDetails.Text = details.ToString();

            if (result.ResultRectList != null)
            {
                CameraForm cameraForm = MainForm.GetDockForm<CameraForm>();
                if (cameraForm != null)
                {
                    cameraForm.AddRect(result.ResultRectList);
                }
            }
        }
    }
}
