using PureGate.Algorithm;
using PureGate.Core;
using SaigeVision.Net.Core.V2;
using SaigeVision.Net.V2;
using SaigeVision.Net.V2.Detection;
using SaigeVision.Net.V2.IAD;
using SaigeVision.Net.V2.Segmentation;
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
using PureGate.UIControl;
using System.Runtime.InteropServices;

namespace PureGate.Property
{
    public partial class AIModuleProp : UserControl
    {
        // ✅ ListView 플리커 제거용: Win32 더블버퍼 스타일 적용
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int LVM_SETEXTENDEDLISTVIEWSTYLE = 0x1036;
        private const int LVS_EX_DOUBLEBUFFER = 0x00010000;

        private static void EnableListViewDoubleBuffer(ListView lv)
        {
            if (lv == null) return;

            // Handle이 아직 없으면 HandleCreated 때 적용
            if (!lv.IsHandleCreated)
            {
                lv.HandleCreated += (s, e) => EnableListViewDoubleBuffer(lv);
                return;
            }

            // 기존 확장 스타일을 덮어쓰지 않고 DOUBLEBUFFER 비트만 켠다(마스크 사용)
            try
            {
                SendMessage(lv.Handle, LVM_SETEXTENDEDLISTVIEWSTYLE,
                    (IntPtr)LVS_EX_DOUBLEBUFFER,
                    (IntPtr)LVS_EX_DOUBLEBUFFER);
            }
            catch
            {
                // UI 개선 목적이라 실패해도 기능엔 영향 없게 무시
            }
        }


        SaigeAI _saigeAI;
        string _modelPath = string.Empty;
        AIEngineType _engineType;
        AIModuleAlgorithm _aiAlgo;
        private bool _isUpdatingUI = false;

        public static AIModuleProp saigeaiprop;

        // 요약용(진짜 OK/NG) 카운트
        private int _okInspections = 0;
        private int _ngInspections = 0;

        // 누적 통계 필드
        private int _totalInspections = 0; // 합계(전체 검사 횟수)
        private readonly Dictionary<string, int> _countByClass =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // 요약 "아이템" 방식 필드
        private const string SUMMARY_KEY = "__SUMMARY__";
        private int _summaryIndex = -1;
        private string _okClassName = "OK"; // 기본값 (모델에 OK 없으면 자동 추정)

        // 첫 행(요약행) "병합 렌더링"용 필드
        private string _summaryText = "";                 // 병합행에 그릴 텍스트
        private bool _enableMergedSummaryRow = true;      // 필요시 끌 수 있게

        private readonly Dictionary<string, string> _classNameKo =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Good",          "양품" },
        { "bent_lead",     "다리 빠짐" },
        { "cut_lead",      "다리 잘림" },
        { "damaged_case",  "케이스 파손" },
        { "misplaced",     "위치 불량" }
    };

        // 표시 텍스트 -> 원본(영어) 키 매핑(역매핑)
        private readonly Dictionary<string, string> _displayToRaw =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public AIModuleProp()
        {
            InitializeComponent();
            saigeaiprop = this;

            cbAIModelType.DataSource = Enum.GetValues(typeof(AIEngineType)).Cast<AIEngineType>().ToList();
            cbAIModelType.SelectedIndex = 0;
            UpdateAreaFilterUI();

            InitClassListView();
            EnableListViewDoubleBuffer(lv_ClassInfos);

            // OwnerDraw 활성화 + 이벤트 연결
            lv_ClassInfos.OwnerDraw = true;
            lv_ClassInfos.DrawColumnHeader += Lv_ClassInfos_DrawColumnHeader;
            lv_ClassInfos.DrawItem += Lv_ClassInfos_DrawItem;
            lv_ClassInfos.DrawSubItem += Lv_ClassInfos_DrawSubItem;

            txtMaxArea.TextChanged += (s, e) => { UpdateClassInfoResultUI(); };
            txtMinArea.TextChanged += (s, e) => { UpdateClassInfoResultUI(); };

            this.AutoScroll = true;
            Global.Inst.InspStage.InspectionCompleted += InspStage_InspectionCompleted;

            // 초기 요약 줄 업데이트
            UpdateSummaryRow();

            this.Load += (s, e) => SyncFromCurrentModelAndUpdateUI();
            this.VisibleChanged += (s, e) =>
            {
                if (this.Visible) SyncFromCurrentModelAndUpdateUI();
            };
        }

        private void InspStage_InspectionCompleted(bool isOk)
        {
            if (this.IsDisposed) return;

            if (this.InvokeRequired)
                this.BeginInvoke(new Action(() => OnInspectionCompleted(isOk)));
            else
                OnInspectionCompleted(isOk);
        }

        public void OnInspectionCompleted(bool isOk)
        {
            if (_saigeAI == null)
                _saigeAI = Global.Inst.InspStage.AIModule;
            if (_saigeAI == null) return;

            // ✅ 요약 카운트는 isOk 기준으로 누적(여기가 핵심)
            if (isOk) _okInspections++;
            else _ngInspections++;

            // ✅ total은 여기서 계산 (또는 _totalInspections++ 없애기)
            _totalInspections = _okInspections + _ngInspections;

            // 클래스 분포는 기존처럼 result로 누적해도 됨
            var result = _saigeAI.GetResult();
            AccumulateStatsFromResult(result);

            UpdateSummaryRow();
            UpdateDistributionColumns_VisibleOnly();
        }

        // ListView 초기 구성: "클래스 | 색상 | 수량 | 비율(%)"
        private void InitClassListView()
        {
            lv_ClassInfos.BeginUpdate();
            try
            {
                lv_ClassInfos.View = View.Details;
                lv_ClassInfos.FullRowSelect = true;
                lv_ClassInfos.GridLines = true;
                lv_ClassInfos.ShowGroups = false;
                lv_ClassInfos.Groups.Clear();

                // 컬럼 4개
                lv_ClassInfos.Columns.Clear();
                lv_ClassInfos.Columns.Add("클래스", 85, HorizontalAlignment.Left); // 요약문이 길어서 폭 조금 늘림
                lv_ClassInfos.Columns.Add("색상", 70, HorizontalAlignment.Left);
                lv_ClassInfos.Columns.Add("수량", 100, HorizontalAlignment.Right);
                lv_ClassInfos.Columns.Add("비율(%)", 100, HorizontalAlignment.Right);
            }
            finally
            {
                lv_ClassInfos.EndUpdate();
            }
        }

        // 통계 초기화
        private void ResetStats()
        {
            _totalInspections = 0;
            _countByClass.Clear();

            UpdateSummaryRow();
            UpdateDistributionColumns_VisibleOnly();
        }

        private void btnSelAIModel_Click(object sender, EventArgs e)
        {
            string filter = "AI Files|*.*;";

            switch (_engineType)
            {
                case AIEngineType.CLS: // EngineType -> AIEngineType 오타 수정
                    filter = "Classification Files|*.saigecls;";
                    break;
                case AIEngineType.IAD: // Enum 이름 일치 (AnomalyDetection -> IAD)
                    filter = "Anomaly Detection Files|*.saigeiad;";
                    break;
                case AIEngineType.SEG: // Enum 이름 일치 (Segmentation -> SEG)
                    filter = "Segmentation Files|*.saigeseg;";
                    break;
                case AIEngineType.DET: // Enum 이름 일치 (Detection -> DET)
                    filter = "Detection Files|*.saigedet;";
                    break;
            }

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "AI 모델 파일 선택";
                openFileDialog.Filter = filter;
                openFileDialog.Multiselect = false;
                openFileDialog.InitialDirectory = @"C:\Saige\SaigeVision\engine\Examples\data\sfaw2023\models";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _modelPath = openFileDialog.FileName;
                    txtAIModelPath.Text = _modelPath;

                    if (_aiAlgo != null)
                    {
                        _aiAlgo.ModelPath = _modelPath;
                        _aiAlgo.EngineType = _engineType;
                    }
                }
            }
        }

        private void btnLoadModel_Click(object sender, EventArgs e)
        {
            if (_saigeAI == null)
            {
                _saigeAI = Global.Inst.InspStage.AIModule;
            }

            if (string.IsNullOrEmpty(_modelPath))
            {
                MsgBox.Show("모델 파일을 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                _saigeAI.LoadEngine(_modelPath, _engineType);
                var modelInfo = _saigeAI.GetModelInfo();

                if (modelInfo == null)
                {
                    MsgBox.Show("모델 정보가 null입니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (_aiAlgo != null)
                {
                    _aiAlgo.ModelPath = _modelPath;
                    _aiAlgo.EngineType = _engineType;
                }
                Global.Inst.InspStage.SetSaigeModelInfo(_modelPath, _engineType);

                ResetStats();
                UpdateModelInfoUI();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null)
                    msg += "\nInner: " + ex.InnerException.Message;

                MsgBox.Show(msg, "모델 로딩 실패");
            }
        }

        private void btnInspAI_Click(object sender, EventArgs e)
        {
            if (_saigeAI == null)
            {
                MessageBox.Show("AI 모듈이 초기화되지 않았습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Bitmap bitmap = Global.Inst.InspStage.GetBitmap();
            if (bitmap is null)
            {
                MessageBox.Show("현재 이미지가 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
           

            _saigeAI.InspAIModule(bitmap);
            Bitmap resultImage = _saigeAI.GetResultImage();

            Global.Inst.InspStage.UpdateDisplay(resultImage);
            UpdateClassInfoResultUI();
        }

        private void cbAIModelType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUI) return;

            AIEngineType engineType = (AIEngineType)cbAIModelType.SelectedItem;

            if (engineType != _engineType)
            {
                if (_saigeAI != null)
                {
                    _saigeAI.DisposeMode(); // Dispose -> DisposeMode (SaigeAI 클래스 메서드명)
                    // _saigeAI = null; // 인스턴스를 유지하려면 주석 처리, 새로 생성하려면 유지
                }

                _modelPath = string.Empty;
                txtAIModelPath.Clear();
                lbx_ModelInformation.Items.Clear();
                lv_ClassInfos.Items.Clear();
                Txt_ModuleInfo.Clear();
                ResetStats();
            }
            _engineType = engineType;
            if (_aiAlgo != null)
            {
                _aiAlgo.EngineType = _engineType;

                // 엔진 타입 바꾸면 기존 모델 확장자가 달라질 수 있으니,
                // 정책에 따라 ModelPath를 유지할지/초기화할지 선택.
                // 보통은 초기화가 안전:
                // _aiAlgo.ModelPath = string.Empty;
            }
            UpdateAreaFilterUI();
        }

        public void SetAlgorithm(AIModuleAlgorithm algo)
        {
            _aiAlgo = algo;
            if (_aiAlgo == null) return;

            _isUpdatingUI = true;
            try
            {
                txtAIModelPath.Text = _aiAlgo.ModelPath;
                _modelPath = _aiAlgo.ModelPath; // 내부 경로 변수도 업데이트
                cbAIModelType.SelectedItem = _aiAlgo.EngineType;
                _engineType = _aiAlgo.EngineType;
            }
            finally
            {
                _isUpdatingUI = false;
            }
        }

        private void SetModelInfo(ModelInfo model, string module)
        {
            if (model.InputDataProcessingMode != null)
                lbx_ModelInformation.Items.Add($"{model.InputDataProcessingMode} (Rows X Cols) : {model.CropNumOfRows}x{model.CropNumOfCols}");
            lbx_ModelInformation.Items.Add($"Target Text Shape : {model.TargetTextShape}");

            lv_ClassInfos.BeginUpdate();
            try
            {
                lv_ClassInfos.Items.Clear();

                // 요약행
                var summary = new ListViewItem(new[] { "", "", "", "" })
                {
                    Name = SUMMARY_KEY,
                    Tag = SUMMARY_KEY,
                    Font = new Font(lv_ClassInfos.Font, FontStyle.Bold)
                };
                summary.UseItemStyleForSubItems = false;
                summary.BackColor = Color.Gainsboro;
                lv_ClassInfos.Items.Add(summary);
                _summaryIndex = 0;

                // 역매핑 초기화(모델 다시 로드 시 중복 방지)
                _displayToRaw.Clear();

                // 클래스 아이템들(표시는 한국어, 통계키는 영어 Tag)
                for (int i = 0; i < model.ClassInfos.Length; i++)
                {
                    string raw = model.ClassInfos[i].Name;   // 영어 원본
                    string disp = ToDisplayName(raw);        // 한국어 표시

                    string[] row = { disp, "", "0", "0.00 %" };
                    var item = new ListViewItem(row);

                    // 통계/OK판정용 원본명 보관
                    item.Tag = raw;

                    // 표시명 -> 원본명 역매핑(혹시 필요할 때 대비)
                    _displayToRaw[disp] = raw;

                    item.SubItems[1].BackColor = model.ClassInfos[i].Color;
                    item.UseItemStyleForSubItems = false;

                    lv_ClassInfos.Items.Add(item);
                }
            }
            finally
            {
                lv_ClassInfos.EndUpdate();
            }

            Txt_ModuleInfo.Text = module;

            // OK 클래스명 추정(영어 원본 기준)
            GuessOkClassName();

            UpdateSummaryRow();
            UpdateDistributionColumns_VisibleOnly();
        }

        private bool TryGetAreaFilter(out double minArea, out double maxArea)
        {
            minArea = double.MinValue;
            maxArea = double.MaxValue;

            // 로직상 txtMinArea가 minArea로, txtMaxArea가 maxArea로 가야할 것 같아 순서 조정 권장
            if (double.TryParse(txtMinArea.Text, out double min)) minArea = min;
            if (double.TryParse(txtMaxArea.Text, out double max)) maxArea = max;

            return true;
        }

        private void UpdateAreaFilterUI()
        {
            bool enable = _engineType == AIEngineType.DET || _engineType == AIEngineType.SEG;

            txtMaxArea.Enabled = enable;
            txtMinArea.Enabled = enable;
            lblAreaFilter.Enabled = enable;
        }

        private void UpdateModelInfoUI()
        {
            if (_saigeAI == null) _saigeAI = Global.Inst.InspStage.AIModule;
            if (_saigeAI == null) return;
            lbx_ModelInformation.Items.Clear();
            lv_ClassInfos.Items.Clear();
            Txt_ModuleInfo.Clear();

            var modelInfo = _saigeAI.GetModelInfo();
            if (modelInfo == null)
            {
                lbx_ModelInformation.Items.Add("모델 정보가 없습니다.");
                return;
            }

            lbx_ModelInformation.Items.Add($"EngineType : {_engineType}");
            lbx_ModelInformation.Items.Add($"ModelPath : {_modelPath}");

            SetModelInfo(modelInfo, _engineType.ToString());

            UpdateSummaryRow();
            UpdateDistributionColumns_VisibleOnly();
        }

        private void UpdateClassInfoResultUI()
        {
            UpdateSummaryRow();
            UpdateDistributionColumns_VisibleOnly();
        }

        // 검사 1회 결과에서 NG 클래스 뽑아서 누적
        private void AccumulateStatsFromResult(object result)
        {
            var classesThisRun = GetClassesFromResult(result);
            foreach (var cls in classesThisRun)
            {
                if (_countByClass.ContainsKey(cls)) _countByClass[cls]++;
                else _countByClass[cls] = 1;
            }
        }

        private HashSet<string> GetClassesFromResult(object result)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            switch (_engineType)
            {
                case AIEngineType.CLS:
                    {
                        // SaigeAI 쪽 결과 타입/필드명이 프로젝트마다 다를 수 있어서
                        // 1) result 객체에서 라벨을 뽑아보고
                        // 2) 안 되면 SaigeAI에 “마지막 CLS 라벨”을 보관하도록 만들어서 그걸 가져오는 순서로 가는 게 안전함

                        // 우선: result.ToString() 같은 추측은 위험하니, 여기서는 "SaigeAI가 라벨을 제공하는 메서드"를 쓰는 형태로 권장
                        if (_saigeAI != null && _saigeAI.TryGetLastClsTop1(out string label, out float score))
                        {
                            if (!string.IsNullOrWhiteSpace(label))
                                set.Add(label);
                        }
                        break;
                    }

                case AIEngineType.DET:
                    {
                        var det = result as DetectionResult;
                        if (det?.DetectedObjects == null) break;

                        foreach (var o in det.DetectedObjects)
                        {
                            var name = o?.ClassInfo?.Name;
                            if (!string.IsNullOrWhiteSpace(name)) set.Add(name);
                        }
                        break;
                    }

                case AIEngineType.SEG:
                    {
                        var seg = result as SegmentationResult;
                        if (seg?.SegmentedObjects == null) break;

                        foreach (var o in seg.SegmentedObjects)
                        {
                            var name = o?.ClassInfo?.Name;
                            if (!string.IsNullOrWhiteSpace(name)) set.Add(name);
                        }
                        break;
                    }

                case AIEngineType.IAD:
                    {
                        var iad = result as IADResult;
                        if (iad?.SegmentedObjects == null) break;

                        foreach (var o in iad.SegmentedObjects)
                        {
                            var name = o?.ClassInfo?.Name;
                            if (!string.IsNullOrWhiteSpace(name)) set.Add(name);
                        }
                        break;
                    }
            }

            return set;
        }

        // 요약 "아이템" 1줄 업데이트 (검사 중 계속 업데이트 가능)
        private void UpdateSummaryRow()
        {
            if (_summaryIndex < 0 || _summaryIndex >= lv_ClassInfos.Items.Count) return;

            int total = _okInspections + _ngInspections;
            int okCount = _okInspections;
            int ngCount = _ngInspections;

            double okPct = total > 0 ? okCount * 100.0 / total : 0.0;
            double ngPct = total > 0 ? ngCount * 100.0 / total : 0.0;

            _summaryText =
                $"합계: {total} / OK: {okCount} ({okPct:0.00}%) / NG: {ngCount} ({ngPct:0.00}%)";

            try
            {
                var r = lv_ClassInfos.GetItemRect(_summaryIndex, ItemBoundsPortion.Entire);
                lv_ClassInfos.Invalidate(r);   // ✅ 요약행만 다시 그림
            }
            catch
            {
                lv_ClassInfos.Invalidate();    // 예외 시만 전체
            }
        }

        private void UpdateDistributionColumns_VisibleOnly()
        {
            if (lv_ClassInfos.Items.Count == 0) return;

            int total = _totalInspections;

            int first = 0;
            int last = lv_ClassInfos.Items.Count - 1;

            try
            {
                if (lv_ClassInfos.TopItem != null)
                {
                    first = lv_ClassInfos.TopItem.Index;

                    int itemHeight = lv_ClassInfos.GetItemRect(first).Height;
                    int visibleCount = itemHeight > 0 ? (lv_ClassInfos.ClientSize.Height / itemHeight) + 2 : 30;

                    last = Math.Min(lv_ClassInfos.Items.Count - 1, first + visibleCount);
                }
            }
            catch
            {
                first = 0;
                last = lv_ClassInfos.Items.Count - 1;
            }

            lv_ClassInfos.BeginUpdate();
            try
            {
                for (int i = first; i <= last; i++)
                {
                    if (i == _summaryIndex) continue;

                    var item = lv_ClassInfos.Items[i];

                    // 통계 key는 Tag(영어 원본) 사용
                    string rawName = item.Tag as string;
                    if (string.IsNullOrWhiteSpace(rawName))
                        rawName = item.Text; // Tag 없을 때만 fallback

                    _countByClass.TryGetValue(rawName, out int count);
                    double pct = (total > 0) ? (count * 100.0 / total) : 0.0;

                    EnsureSubItemCount(item, 4);
                    item.SubItems[2].Text = count.ToString();
                    item.SubItems[3].Text = pct.ToString("0.00") + " %";
                }
            }
            finally
            {
                lv_ClassInfos.EndUpdate();
            }
        }

        // OK 클래스명 자동 추정 (OK > Good > 첫번째)
        private void GuessOkClassName()
        {
            // 표시명(Text)이 아니라 Tag(영어 원본) 기준으로 OK/Good 찾기
            var raws = lv_ClassInfos.Items
                .Cast<ListViewItem>()
                .Where(it => !string.Equals(it.Name, SUMMARY_KEY, StringComparison.Ordinal))
                .Select(it => it.Tag as string)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (raws.Count == 0)
            {
                _okClassName = "OK";
                return;
            }

            if (raws.Any(n => string.Equals(n, "OK", StringComparison.OrdinalIgnoreCase)))
                _okClassName = raws.First(n => string.Equals(n, "OK", StringComparison.OrdinalIgnoreCase));
            else if (raws.Any(n => string.Equals(n, "Good", StringComparison.OrdinalIgnoreCase)))
                _okClassName = raws.First(n => string.Equals(n, "Good", StringComparison.OrdinalIgnoreCase));
            else
                _okClassName = raws[0];
        }

        // OwnerDraw: 첫 행(요약행)만 1~4열 병합처럼 그리기
        private void Lv_ClassInfos_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void Lv_ClassInfos_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // Details 모드에서는 SubItem에서 그리지만,
            // 요약행은 배경만 먼저 칠해줌
            if (_enableMergedSummaryRow && e.ItemIndex == _summaryIndex)
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.Gainsboro), e.Bounds);
                return;
            }

            // 일반행은 기본 흐름대로 (SubItem에서 DrawDefault=true로 처리)
            e.DrawDefault = false;
        }

        private void Lv_ClassInfos_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            // 요약행 병합 렌더링
            if (_enableMergedSummaryRow && e.Item.Index == _summaryIndex)
            {
                if (e.ColumnIndex == 0)
                {
                    // 0~마지막 컬럼 Bounds 합치기
                    Rectangle merged = e.Bounds;
                    for (int i = 1; i < lv_ClassInfos.Columns.Count; i++)
                        merged = Rectangle.Union(merged, e.Item.SubItems[i].Bounds);

                    using (var bg = new SolidBrush(Color.Gainsboro))
                        e.Graphics.FillRectangle(bg, merged);

                    // 텍스트 영역 패딩
                    Rectangle textRect = new Rectangle(merged.X + 6, merged.Y + 2, merged.Width - 12, merged.Height - 4);

                    TextRenderer.DrawText(
                        e.Graphics,
                        _summaryText ?? "",
                        new Font(lv_ClassInfos.Font, FontStyle.Bold),
                        textRect,
                        SystemColors.ControlText,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
                    );

                    // 구분선
                    e.Graphics.DrawLine(SystemPens.ControlDark, merged.Left, merged.Bottom - 1, merged.Right, merged.Bottom - 1);
                }
                return; // 나머지 컬럼은 안 그림(병합 효과)
            }

            // 일반행은 기본 렌더링(색상칸 BackColor 포함)
            e.DrawDefault = true;
        }

        // SubItems 개수 안전 보장
        private static void EnsureSubItemCount(ListViewItem item, int count)
        {
            while (item.SubItems.Count < count)
                item.SubItems.Add("");
        }

        // 영어 원본명을 한국어 표시명으로
        private string ToDisplayName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return rawName;
            return _classNameKo.TryGetValue(rawName, out var ko) ? ko : rawName;
        }

        // ListView 표시명으로부터 원본(영어) 키 복원
        private string ToRawName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return displayName;
            return _displayToRaw.TryGetValue(displayName, out var raw) ? raw : displayName;
        }

        public void SyncFromCurrentModelAndUpdateUI()
        {
            // UI 스레드 보장
            if (this.IsHandleCreated && this.InvokeRequired)
            {
                this.BeginInvoke(new Action(SyncFromCurrentModelAndUpdateUI));
                return;
            }

            if (Global.Inst?.InspStage == null) return;

            // Stage에 있는 SaigeAI 인스턴스 사용
            _saigeAI = Global.Inst.InspStage.AIModule;
            if (_saigeAI == null) return;

            // 현재 모델(.xml)에 저장된 Saige 정보 가져오기
            var model = Global.Inst.InspStage.CurModel;
            if (model == null) return;

            // 저장된 값이 없으면 굳이 UI를 채우지 않아도 됨
            if (string.IsNullOrWhiteSpace(model.SaigeModelPath))
                return;

            _isUpdatingUI = true;
            try
            {
                _modelPath = model.SaigeModelPath ?? string.Empty;
                _engineType = model.SaigeEngineType;

                // UI 반영
                txtAIModelPath.Text = _modelPath;

                // DataSource가 Enum 목록이므로 SelectedItem으로 맞추는 게 안전
                cbAIModelType.SelectedItem = _engineType;
            }
            finally
            {
                _isUpdatingUI = false;
            }

            // 엔진이 이미 로드된 상태이므로 모델 정보/클래스 리스트 갱신
            UpdateModelInfoUI();
        }
    }
}