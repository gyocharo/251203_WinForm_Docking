using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SaigeVision.Net.V2;
using SaigeVision.Net.Core.V2;
using System.IO;
using _251203_WinForm_Docking.Core;

namespace _251203_WinForm_Docking.Property
{
    public partial class AIModuleProp : UserControl
    {
        SaigeAI _saigeAI;
        string _modelPath;
        EngineType _engineType;

        public static AIModuleProp saigeaiprop;
        public AIModuleProp()
        {
            InitializeComponent();

            saigeaiprop = this;

            cmb_Model.DataSource = Enum.GetValues(typeof(EngineType)).Cast<EngineType>().ToList();
            cmb_Model.SelectedIndex = 0;
        }

        private void btn_Apply_Click(object sender, EventArgs e)
        {
            if (_saigeAI == null)
            {
                MessageBox.Show("AI 모듈이 초기화되지 않았습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Bitmap bitmap = Global.Inst.InspStage.GetCurrentImage();
            if (bitmap is null)
            {
                MessageBox.Show("현재 이미지가 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _saigeAI.InspAIModule(bitmap);

            Bitmap resultImage = _saigeAI.GetResultImage();

            Global.Inst.InspStage.UpdateDisplay(resultImage);
        }

        private void btn_Model_Click(object sender, EventArgs e)
        {
            string filter = "AI Files|*.*;";

            switch (_engineType)
            {
                case EngineType.IAD:
                    filter = "Anomaly Detection Files|*.saigeiad;";
                    break;
                case EngineType.SEG:
                    filter = "Segmentation Files|*.saigeseg;";
                    break;
                case EngineType.DET:
                    filter = "Detection Files|*.saigedet;";
                    break;
            }

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "AI 모델 파일 선택";
                openFileDialog.Filter = filter;
                openFileDialog.Multiselect = false;
                openFileDialog.InitialDirectory = @"D:\Saige_Model";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _modelPath = openFileDialog.FileName;
                    txt_Model_Path.Text = _modelPath;
                }
            }
        }

        private void cmb_Model_SelectedIndexChanged(object sender, EventArgs e)
        {
            EngineType engineType = (EngineType)cmb_Model.SelectedItem;

            if (engineType != _engineType)
            {
                if (_saigeAI != null)
                    _saigeAI.Dispose();
            }

            _engineType = engineType;
        }

        private void btn_Model_Load_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_modelPath))
            {
                MessageBox.Show("모델 파일을 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_saigeAI == null)
            {
                _saigeAI = Global.Inst.InspStage.AIModule;
            }

            _saigeAI.LoadEngine(_modelPath, _engineType);
            MessageBox.Show("모델이 성공적으로 로드되었습니다.", "정보", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
