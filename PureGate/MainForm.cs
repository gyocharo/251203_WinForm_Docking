using PureGate.Core;
using PureGate.Setting;
using PureGate.Teach;
using PureGate.Util;
using SaigeVision.Net.V2;
using SaigeVision.Net.V2.Classification;
using SaigeVision.Net.V2.Detection;
using SaigeVision.Net.V2.IAD;
using SaigeVision.Net.V2.IEN;
using SaigeVision.Net.V2.OCR;
using SaigeVision.Net.V2.Segmentation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using PureGate.UIControl;

namespace PureGate
{
    public partial class MainForm : Form
    {
        // DockPanel을 전역으로 선언
        private static DockPanel _dockPanel;

        // 메인 컨테이너
        private Panel _panelMain;

        // CycleMode 눌림 표시를 위한 상태 변수 선언
        private bool _isCycleMode = false;

        //슬라이딩 메뉴의 최대, 최소 폭 크기, 슬라이딩 메뉴가 보이는/접히는 속도, 최초 슬라이딩 메뉴 크기
        const int MAX_SLIDING_WIDTH = 120;
        const int MIN_SLIDING_WIDTH = 55;
        const int STEP_SLIDING = 2;
        int _posSliding = 120;

        //닫기 창 불타입 선언
        private bool _isClosing = false;

        //로고 필드 선언
        private SvgLikeLogo _logo;
        private const int LOGO_H_EXPANDED = 130; // 펼쳤을 때 로고 영역 높이 (기존 96 -> 130)
        private const int LOGO_H_COLLAPSED = 70; // 접었을 때 로고 영역 높이 (기존 48 -> 70)

        private bool _modelUiAppliedOnce = false;

        public MainForm()
        {
            InitializeComponent();
            InitializeUI();
            InitializeEvents();
            InitializeDocking();
            _dockPanel.Theme = new NoToolWindowCaptionTheme();
            LoadDockingWindows();
            LoadSetting();

            this.Shown += (s, e) =>
            {
                if (_modelUiAppliedOnce) return;
                _modelUiAppliedOnce = true;

                var m = Global.Inst?.InspStage?.CurModel;
                if (m != null && !string.IsNullOrWhiteSpace(m.ModelPath))
                    ApplyModelToUI();
            };
        }

        //초기화 메서드
        #region 
        private void InitializeUI()
        {
            // 메인 패널
            _panelMain = new Panel { Dock = DockStyle.Fill };
            this.Controls.Add(_panelMain);

            // SideMenu
            _panelMain.Controls.Add(SideMenu);
            SideMenu.Dock = DockStyle.Left;
            SideMenu.Width = MAX_SLIDING_WIDTH;
            SideMenu.BringToFront();

            // Setup 버튼 Dock
            SideMenu.Controls.Add(btnSetUp);
            btnSetUp.Dock = DockStyle.Bottom;

            // 슬라이딩 버튼 Dock
            SideMenu.Controls.Add(checkBoxHide);
            checkBoxHide.Dock = DockStyle.Bottom;
            checkBoxHide.Text = "<";
        }

        private void InitializeEvents()
        {
            this.Load += Form1_Load;
            _panelMain.Resize += (s, e) => AdjustDockPanel();
            checkBoxHide.CheckedChanged += checkBoxHide_CheckedChanged;
            this.FormClosing += MainForm_FormClosing;
        }

        private void InitializeDocking()
        {
            _dockPanel = new DockPanel { Theme = new VS2015BlueTheme() };
            _dockPanel.DocumentStyle = DocumentStyle.DockingSdi;
            _panelMain.Controls.Add(_dockPanel);
            AdjustDockPanel();
        }
        #endregion

        //SideMenu 슬라이딩
        #region
        private void checkBoxHide_CheckedChanged(object sender, EventArgs e)
        {
            // 버튼 텍스트/아이콘 매핑
            var btnIconMap = new Dictionary<Button, Image>
        {
            { btnOverview, Properties.Resources.Overview },
            { btnModel, Properties.Resources.Model },
            { btnImage, Properties.Resources.Image },
            { btnCycleMode, Properties.Resources.CycleMode },
            { btnSetUp, Properties.Resources.SetUp }
        };

            if (checkBoxHide.Checked)
            {
                foreach (var kvp in btnIconMap)
                {
                    kvp.Key.Text = "";
                    kvp.Key.Image = kvp.Value;
                }
                checkBoxHide.Text = ">";
            }
            else
            {
                foreach (var kvp in btnIconMap)
                {
                    kvp.Key.Text = kvp.Key == btnCycleMode ? "Cycle Mode" : kvp.Key.Name.Replace("btn", "");
                    kvp.Key.Image = null;
                }
                checkBoxHide.Text = "<";
            }

            timerSliding.Start();
        }
        private void timerSliding_Tick(object sender, EventArgs e)
        {
            if (checkBoxHide.Checked)
            {
                _posSliding -= STEP_SLIDING;
                if (_posSliding <= MIN_SLIDING_WIDTH)
                {
                    _posSliding = MIN_SLIDING_WIDTH;
                    timerSliding.Stop();
                }
            }
            else
            {
                _posSliding += STEP_SLIDING;
                if (_posSliding >= MAX_SLIDING_WIDTH)
                {
                    _posSliding = MAX_SLIDING_WIDTH;
                    timerSliding.Stop();
                }
            }

            SideMenu.Width = _posSliding;

            if (_logo != null)
            {
                _logo.Height = (SideMenu.Width <= MIN_SLIDING_WIDTH + 1)
                    ? LOGO_H_COLLAPSED          // ✅ 접힘 높이
                    : LOGO_H_EXPANDED;          // ✅ 펼침 높이
            }


            _dockPanel.SuspendLayout();
            AdjustDockPanel();
            _dockPanel.ResumeLayout();
        }
        #endregion

        // Form 이벤트
        #region
        // 메인 이미지로고 배경 흰색을 투명 처리하기 위한 기능
        private void Form1_Load(object sender, EventArgs e)
        {
            var parent = pictureBox1.Parent; // SideMenu

            _logo = new SvgLikeLogo
            {
                Dock = DockStyle.Top,
                Height = LOGO_H_EXPANDED,
                BackColor = Color.Transparent
            };

            parent.Controls.Remove(pictureBox1);
            pictureBox1.Dispose();

            parent.Controls.Add(_logo);
            parent.Controls.SetChildIndex(_logo, parent.Controls.Count - 1);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isClosing)
                return;

            DialogResult result = MsgBox.Show(
                "프로그램을 종료하시겠습니까?",
                "종료 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                _isClosing = true;
                Global.Inst.Dispose();
            }
        }
        #endregion

        // DockPanel 관련 기능 함수들
        #region
        private void AdjustDockPanel()
        {
            _dockPanel.Left = SideMenu.Right;
            _dockPanel.Top = 0;
            _dockPanel.Width = _panelMain.ClientSize.Width - SideMenu.Right;
            _dockPanel.Height = _panelMain.ClientSize.Height;
        }

        // UI 위치 선정
        private void LoadDockingWindows()
        {
            // ==========================
            // Document 기준
            // ==========================
            CameraForm cameraForm = new CameraForm();
            cameraForm.Show(_dockPanel, DockState.Document);

            // ==========================
            // 상단 TitleForm 
            // ==========================
            TitleForm titleForm = new TitleForm();
            titleForm.Show(cameraForm.Pane, DockAlignment.Top, 0.07);

            // ==========================
            // CountForm 
            // ==========================
            CountForm countForm = new CountForm();
            countForm.Show(cameraForm.Pane, DockAlignment.Top, 0.1);

            // ==========================
            // 하단 3분할
            // ==========================
            LogForm logForm = new LogForm();
            logForm.Show(cameraForm.Pane, DockAlignment.Bottom, 0.25);

            ResultForm resultForm = new ResultForm();
            resultForm.Show(logForm.Pane, DockAlignment.Right, 0.8);

            RunForm runForm = new RunForm();
            runForm.Show(resultForm.Pane, DockAlignment.Right, 0.37);

            // ==========================
            // 우측 영역
            // ==========================
            PropertiesForm propForm = new PropertiesForm();
            propForm.Show(_dockPanel, DockState.DockRight);

            StatisticForm statisticForm = new StatisticForm();
            statisticForm.Show(propForm.Pane, DockAlignment.Bottom, 0.4);
        }

        public static T GetDockForm<T>() where T : DockContent
        {
            var findForm = _dockPanel.Contents.OfType<T>().FirstOrDefault();
            return findForm;
        }
        #endregion

        private void LoadSetting()
        {
            try
            {
                _isCycleMode = SettingXml.Inst.CycleMode;
                UpdateCycleModeButtonUI();
            }
            catch (Exception ex)
            {
                MsgBox.Show($"로드 세팅 실패: {ex.Message}");
            }
        }

        private void btnOverview_Click(object sender, EventArgs e)
        {
            try
            {
                ResetDockLayout();
                ResetToInitialState();
            }
            catch (Exception ex)
            {
                MsgBox.Show($"레이아웃 초기화 실패: {ex.Message}");
            }
        }

        private void btnCycleMode_Click(object sender, EventArgs e)
        {
            try
            {
                _isCycleMode = !_isCycleMode;
                SettingXml.Inst.CycleMode = _isCycleMode;

                UpdateCycleModeButtonUI();
            }
            catch (Exception ex)
            {
                MsgBox.Show($"사이클모드 실행/취소 실패: {ex.Message}");
            }
        }

        private void btnSetUp_Click(object sender, EventArgs e)
        {
            try
            {
                SLogger.Write($"환경설정창 열기");
                SetupForm setupForm = new SetupForm();
                setupForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MsgBox.Show($"환경설정창 열기 실패: {ex.Message}");
            }
        }

        private void btnModel_Click(object sender, EventArgs e)
        {
            ToolStripDropDown dropDown = new ToolStripDropDown();

            string[] tabNames = { "Model New", "Model Open", "Model Save", "Model Save As" };

            for (int i = 0; i < tabNames.Length; i++)
            {
                Button btn = new Button()
                {
                    Text = tabNames[i],
                    Width = 120,
                    Height = 30,
                    BackColor = Color.FromArgb(224, 224, 224),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.Black,
                    Font = new Font(Font.FontFamily, 9.5f, FontStyle.Regular)
                };

                int index = i;
                btn.Click += (s, ev) =>
                {
                    switch (index)
                    {
                        case 0:
                            modelNewMenuItem_Click(s, EventArgs.Empty);
                            break;
                        case 1:
                            modelOpenMenuItem_Click(s, EventArgs.Empty);
                            break;
                        case 2:
                            modelSaveMenuItem_Click(s, EventArgs.Empty);
                            break;
                        case 3:
                            modelSaveAsMenuItem_Click(s, EventArgs.Empty);
                            break;
                    }

                    dropDown.Close();
                };

                ToolStripControlHost host = new ToolStripControlHost(btn);
                dropDown.Items.Add(host);
            }

            Point location = btnModel.PointToScreen(new Point(btnModel.Width, 0));
            dropDown.Show(location);
        }

        private void btnImage_Click(object sender, EventArgs e)
        {
            ToolStripDropDown dropDown = new ToolStripDropDown();

            string[] tabNames = { "Image Open", "Image Save" };

            for (int i = 0; i < tabNames.Length; i++)
            {
                Button btn = new Button()
                {
                    Text = tabNames[i],
                    Width = 120,
                    Height = 30,
                    BackColor = Color.FromArgb(224, 224, 224),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.Black,
                    Font = new Font(Font.FontFamily, 9.5f, FontStyle.Regular)
                };

                int index = i;
                btn.Click += (s, ev) =>
                {
                    switch (index)
                    {
                        case 0:
                            ImageOpenToolStripMenuItem_Click(s, EventArgs.Empty);
                            break;

                        case 1:
                            ImageSaveToolStripMenuItem_Click(s, EventArgs.Empty);
                            break;
                    }

                    dropDown.Close();
                };

                ToolStripControlHost host = new ToolStripControlHost(btn);
                dropDown.Items.Add(host);
            }

            Point location = btnImage.PointToScreen(new Point(btnImage.Width, 0));
            dropDown.Show(location);
        }

        // Overview 버튼을 위한 초기화 메서드
        #region
        private void ResetDockLayout()
        {
            _dockPanel.SuspendLayout();

            var camera = GetDockForm<CameraForm>();
            var title = GetDockForm<TitleForm>();
            var count = GetDockForm<CountForm>();
            var log = GetDockForm<LogForm>();
            var result = GetDockForm<ResultForm>();
            var run = GetDockForm<RunForm>();
            var prop = GetDockForm<PropertiesForm>();
            var stat = GetDockForm<StatisticForm>();

            // Document 기준
            if (camera != null && camera.DockState != DockState.Document)
                camera.Show(_dockPanel, DockState.Document);

            // 상단 TitleForm
            if (title != null) title.Show(camera?.Pane, DockAlignment.Top, 0.07);

            // CountForm
            if (count != null) count.Show(camera?.Pane, DockAlignment.Top, 0.1);

            // 하단 3분할
            if (log != null) log.Show(camera?.Pane, DockAlignment.Bottom, 0.25);
            if (result != null) result.Show(log?.Pane, DockAlignment.Right, 0.8);
            if (run != null) run.Show(result?.Pane, DockAlignment.Right, 0.37);

            // 우측 영역
            if (prop != null) prop.Show(_dockPanel, DockState.DockRight);
            if (stat != null) stat.Show(prop?.Pane, DockAlignment.Bottom, 0.4);

            _dockPanel.ResumeLayout();
        }

        private void ResetToInitialState()
        {
            // 1) 검사/시퀀스 완전 정지 + 화면 리셋
            try
            {
                var stage = Global.Inst.InspStage;
                if (stage != null)
                {
                    stage.StopCycle();      // InspWorker/Sequence 정지 포함
                    stage.LiveMode = false;
                    stage.ResetDisplay();
                }
            }
            catch { }

            // 2) UI 타이틀/설정 다시 반영 (원하면)
            try
            {
                LoadSetting();       // CycleMode 버튼 UI 반영
                ApplyModelToUI();    // 마지막 모델 로딩되어 있으면 타이틀/이미지 다시 반영
            }
            catch { }
        }
        #endregion

        // Cycle Mode 관련 기능 함수
        #region
        private void UpdateCycleModeButtonUI()
        {
            btnCycleMode.FlatStyle = FlatStyle.Flat;
            btnCycleMode.UseVisualStyleBackColor = false;

            if (_isCycleMode)
            {
                btnCycleMode.BackColor = Color.FromArgb(0, 120, 215);
                btnCycleMode.ForeColor = Color.White;
                btnCycleMode.FlatAppearance.BorderSize = 0;
            }
            else
            {
                btnCycleMode.BackColor = SystemColors.Control;
                btnCycleMode.ForeColor = Color.Black;
                btnCycleMode.FlatAppearance.BorderSize = 1;
            }
        }
        #endregion

        // 모델 버튼 클릭 시 동적으로 생성되는 4가지의 탭 기능들 
        #region 

        private string GetModelTitle(Model curModel)
        {
            if (curModel is null)
                return "";

            string modelName = curModel.ModelName;
            return $"{Define.PROGRAM_NAME} - MODEL : {modelName}";
        }

        // 프로그램 실행 시 모델 로드할 때 UI 갱신을 위한 기능
        private void ApplyModelToUI()
        {
            var m = Global.Inst.InspStage.CurModel;
            if (m == null) return;

            // 타이틀 갱신
            this.Text = GetModelTitle(m);

            // 이미지 즉시 표시(모델에 경로가 있을 때)
            if (!string.IsNullOrWhiteSpace(m.InspectImagePath) && File.Exists(m.InspectImagePath))
            {
                Global.Inst.InspStage.SetImageBuffer(m.InspectImagePath);
            }
        }

        private void modelNewMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // 1️⃣ 모델 디렉터리 존재 보장
                SettingXml.Inst.EnsureModelDir();

                // 2️⃣ 기존 로직
                NewModel newModel = new NewModel();
                newModel.ShowDialog();

                Model curModel = Global.Inst.InspStage.CurModel;
                if (curModel != null)
                {
                    this.Text = GetModelTitle(curModel);
                }
            }
            catch (Exception ex)
            {
                MsgBox.Show(
                    $"모델 디렉터리 준비 실패:\n{ex.Message}",
                    "오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void modelOpenMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // 모델 디렉터리 존재 보장
                SettingXml.Inst.EnsureModelDir();
            }
            catch (Exception ex)
            {
                MsgBox.Show(
                    $"모델 디렉터리 준비 실패:\n{SettingXml.Inst.ModelDir}\n{ex.Message}",
                    "오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "모델 파일 선택";
                openFileDialog.Filter = "Model Files|*.xml;";
                openFileDialog.Multiselect = false;
                openFileDialog.InitialDirectory = SettingXml.Inst.ModelDir;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    if (Global.Inst.InspStage.LoadModel(filePath))
                    {
                        ApplyModelToUI();
                    }
                }
            }
        }

        private void modelSaveMenuItem_Click(object sender, EventArgs e)
        {
            if (!EnsureModelLoadedOrWarn())
                return;

            try
            {
                SettingXml.Inst.EnsureModelDir(); // (선택이지만 권장)
                Global.Inst.InspStage.SaveModel("");
            }
            catch (Exception ex)
            {
                MsgBox.Show(
                    $"모델 저장 실패:\n{ex.Message}",
                    "오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void modelSaveAsMenuItem_Click(object sender, EventArgs e)
        {
            if (!EnsureModelLoadedOrWarn())
                return;

            try
            {
                // 모델 디렉터리 존재 보장
                SettingXml.Inst.EnsureModelDir();
            }
            catch (Exception ex)
            {
                MsgBox.Show(
                    $"모델 디렉터리 준비 실패:\n{SettingXml.Inst.ModelDir}\n{ex.Message}",
                    "오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = SettingXml.Inst.ModelDir;
                saveFileDialog.Title = "모델 파일 선택";
                saveFileDialog.Filter = "Model Files|*.xml;";
                saveFileDialog.DefaultExt = "xml";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string filePath = saveFileDialog.FileName;
                        Global.Inst.InspStage.SaveModel(filePath);
                    }
                    catch (Exception ex)
                    {
                        MsgBox.Show(
                            $"모델 저장 실패:\n{ex.Message}",
                            "오류",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }

        private bool EnsureModelLoadedOrWarn()
        {
            var stage = Global.Inst?.InspStage;

            // 모델 객체 자체가 없거나
            if (stage?.CurModel == null)
            {
                MsgBox.Show("모델을 오픈하거나 새로 만든 다음에 해주세요.", "안내",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            // 핵심: ModelPath가 비어있으면 '진짜로 열린 모델'이 아님
            if (string.IsNullOrWhiteSpace(stage.CurModel.ModelPath))
            {
                MsgBox.Show("모델을 오픈하거나 새로 만든 다음에 해주세요.", "안내",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            return true; ;
        }

        #endregion // 모델 버튼 클릭 시 동적으로 생성되는 4가지의 탭 기능들 

        // 이미지 버튼 클릭 시 동적으로 생성되는 2가지의 탭 기능들
        #region
        private void ImageOpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CameraForm cameraForm = GetDockForm<CameraForm>();
            if (cameraForm is null)
                return;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "이미지 파일 선택";
                openFileDialog.Filter = "Image Files |*.bmp;*.jpg;*.jpeg;*.png;*.gif";
                openFileDialog.InitialDirectory = @"C:\Users\user\Desktop\강의자료\dataset";
                openFileDialog.Multiselect = false;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    Global.Inst.InspStage.SetImageBuffer(filePath);
                    Global.Inst.InspStage.CurModel.InspectImagePath = filePath;
                }
            }
        }

        private void ImageSaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 현재 검사 스테이지
            var inspStage = Global.Inst.InspStage;
            if (inspStage == null)
                return;

            // ImageSpace에서 Bitmap 얻기 (Display 용도)
            Bitmap bitmap = inspStage.ImageSpace.GetBitmap();
            if (bitmap == null)
                return;

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "이미지 저장";
                saveFileDialog.Filter =
                    "Bitmap (*.bmp)|*.bmp|" +
                    "JPEG (*.jpg)|*.jpg|" +
                    "PNG (*.png)|*.png";
                saveFileDialog.DefaultExt = "bmp";
                saveFileDialog.AddExtension = true;

                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                    return;

                string filePath = saveFileDialog.FileName;

                // 확장자에 따라 포맷 결정
                var ext = Path.GetExtension(filePath).ToLower();
                switch (ext)
                {
                    case ".jpg":
                    case ".jpeg":
                        bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                        break;

                    case ".png":
                        bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                        break;

                    default:
                        bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Bmp);
                        break;
                }
            }
        }


        #endregion

    }
}

