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

namespace PureGate.Property
{
    public partial class AIModuleProp : UserControl
    {
        SaigeAI _saigeAI;
        string _modelPath = string.Empty;
        AIEngineType _engineType;
        AIModuleAlgorithm _aiAlgo;
        private bool _isUpdatingUI = false;

        public static AIModuleProp saigeaiprop;

        // 누적 통계 필드
        private int _totalInspections = 0; // 합계(전체 검사 횟수)
        private readonly Dictionary<string, int> _ngCountByClass =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);


        public AIModuleProp()
        {
            InitializeComponent();
            saigeaiprop = this; // 정적 참조 초기화 추가

            cbAIModelType.DataSource = Enum.GetValues(typeof(AIEngineType)).Cast<AIEngineType>().ToList();
            cbAIModelType.SelectedIndex = 0;

            UpdateAreaFilterUI();

            txtMaxArea.TextChanged += (s, e) => { UpdateClassInfoResultUI(); };
            txtMinArea.TextChanged += (s, e) => { UpdateClassInfoResultUI(); };

            this.AutoScroll = true;
            Global.Inst.InspStage.InspectionCompleted += InspStage_InspectionCompleted;
        }

        private void InspStage_InspectionCompleted(bool isOk)
        {
            if (this.IsDisposed) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnInspectionCompleted()));
            }
            else
            {
                OnInspectionCompleted();
            }
        }

        public void OnInspectionCompleted()
        {
            if (_saigeAI == null)
                _saigeAI = Global.Inst.InspStage.AIModule;

            if (_saigeAI == null) return;

            // ListView에 클래스 row가 없으면 모델정보로 채움(안전 방어)
            if (lv_ClassInfos.Items.Count == 0)
            {
                var mi = _saigeAI.GetModelInfo();
                if (mi != null)
                    SetModelInfo(mi, _engineType.ToString());
            }

            var result = _saigeAI.GetResult();
            AccumulateStatsFromResult(result);
            UpdatePercentColumns();
        }


        // 통계 초기화
        private void ResetStats()
        {
            _totalInspections = 0;
            _ngCountByClass.Clear();
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
                MessageBox.Show("모델 파일을 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                _saigeAI.LoadEngine(_modelPath, _engineType);
                var modelInfo = _saigeAI.GetModelInfo();

                if (modelInfo == null)
                {
                    MessageBox.Show("모델 정보가 null입니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                ResetStats();
                UpdateModelInfoUI();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null)
                    msg += "\nInner: " + ex.InnerException.Message;

                MessageBox.Show(msg, "모델 로딩 실패");
            }
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
            {
                lbx_ModelInformation.Items.Add($"{model.InputDataProcessingMode} (Rows X Cols) : {model.CropNumOfRows}x{model.CropNumOfCols}");
            }
            lbx_ModelInformation.Items.Add($"Target Text Shape : {model.TargetTextShape}");

            lv_ClassInfos.Items.Clear();

            for (int i = 0; i < model.ClassInfos.Length; i++)
            {
                // [0]=클래스, [1]=색상, [2]=불량수, [3]=합계, [4]=불량%, [5]=양품%
                string[] row = { model.ClassInfos[i].Name, "", "0", "0", "0.00 %", "0.00 %" };

                var item = new ListViewItem(row);
                item.SubItems[1].BackColor = model.ClassInfos[i].Color;
                item.UseItemStyleForSubItems = false;

                lv_ClassInfos.Items.Add(item);
            }

            Txt_ModuleInfo.Text = module;
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
            UpdatePercentColumns();
        }

        private void UpdateClassInfoResultUI()
        {
            UpdatePercentColumns();
        }

        // 검사 1회 결과에서 NG 클래스 뽑아서 누적
        private void AccumulateStatsFromResult(object result)
        {
            _totalInspections++;

            HashSet<string> ngClassesThisRun = GetNgClassesFromResult(result);

            foreach (var cls in ngClassesThisRun)
            {
                if (_ngCountByClass.ContainsKey(cls)) _ngCountByClass[cls]++;
                else _ngCountByClass[cls] = 1;
            }
        }

        // 결과 객체에서 NG 클래스명 추출
        private HashSet<string> GetNgClassesFromResult(object result)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            switch (_engineType)
            {
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

        // 6컬럼 인덱스 기준으로 누적값을 ListView에 반영
        // [2]=불량수, [3]=합계, [4]=불량%, [5]=양품%
        private void UpdatePercentColumns()
        {
            if (lv_ClassInfos.Items.Count == 0) return;

            int total = _totalInspections;

            for (int i = 0; i < lv_ClassInfos.Items.Count; i++)
            {
                var item = lv_ClassInfos.Items[i];
                string className = item.Text; // [0]

                _ngCountByClass.TryGetValue(className, out int ngCount);

                int okCount = Math.Max(0, total - ngCount);

                double ngPct = (total > 0) ? (ngCount * 100.0 / total) : 0.0;
                double okPct = (total > 0) ? (okCount * 100.0 / total) : 0.0;

                EnsureSubItemCount(item, 6);

                item.SubItems[2].Text = ngCount.ToString();            // 불량수
                item.SubItems[3].Text = total.ToString();              // 합계
                item.SubItems[4].Text = ngPct.ToString("0.00") + " %"; // 불량 %
                item.SubItems[5].Text = okPct.ToString("0.00") + " %"; // 양품 %
            }

            lv_ClassInfos.Refresh();
        }

        // SubItems 개수 안전 보장
        private static void EnsureSubItemCount(ListViewItem item, int count)
        {
            while (item.SubItems.Count < count)
                item.SubItems.Add("");
        }
    }
}