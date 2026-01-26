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
                string[] row = { model.ClassInfos[i].Name, "", model.ClassIsNG[i].ToString() };
                var listViewItem = new ListViewItem(row);
                listViewItem.SubItems[1].BackColor = model.ClassInfos[i].Color;
                listViewItem.UseItemStyleForSubItems = false;
                lv_ClassInfos.Items.Add(listViewItem);
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
        }

        private void UpdateClassInfoResultUI()
        {
            if (_saigeAI == null) return;

            var result = _saigeAI.GetResult();
            if (result == null) return;

            switch (_engineType)
            {
                case AIEngineType.IAD:
                    UpdateIADClassInfo(result as IADResult);
                    break;
                case AIEngineType.DET:
                    UpdateDetectionClassInfo(result as DetectionResult);
                    break;
                case AIEngineType.SEG:
                    UpdateSegmentationClassInfo(result as SegmentationResult);
                    break;
            }
        }

        private void UpdateDetectionClassInfo(DetectionResult detResult)
        {
            if (detResult == null || lv_ClassInfos.Items.Count == 0) return;

            for (int i = 0; i < lv_ClassInfos.Items.Count; i++)
            {
                var item = lv_ClassInfos.Items[i];
                string className = item.Text;
                bool hasNG = detResult.DetectedObjects.Any(o =>
                    string.Equals(o.ClassInfo.Name, className, StringComparison.OrdinalIgnoreCase));

                item.SubItems[2].Text = hasNG ? "True" : "False";
            }
        }

        private void UpdateSegmentationClassInfo(SegmentationResult segResult)
        {
            if (segResult == null || lv_ClassInfos.Items.Count == 0) return;

            for (int i = 0; i < lv_ClassInfos.Items.Count; i++)
            {
                var item = lv_ClassInfos.Items[i];
                string className = item.Text;
                bool hasNG = segResult.SegmentedObjects.Any(o =>
                    string.Equals(o.ClassInfo.Name, className, StringComparison.OrdinalIgnoreCase));

                item.SubItems[2].Text = hasNG ? "True" : "False";
            }
        }

        private void UpdateIADClassInfo(IADResult iadResult)
        {
            if (iadResult == null || lv_ClassInfos.Items.Count == 0) return;

            for (int i = 0; i < lv_ClassInfos.Items.Count; i++)
            {
                var item = lv_ClassInfos.Items[i];
                string className = item.Text;
                bool hasNG = iadResult.SegmentedObjects.Any(o =>
                    string.Equals(o.ClassInfo.Name, className, StringComparison.OrdinalIgnoreCase));

                item.SubItems[2].Text = hasNG ? "True" : "False";
            }
        }

    }
}