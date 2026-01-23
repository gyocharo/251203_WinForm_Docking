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

        public MainForm()
        {
            InitializeComponent();

            InitializeUI();
            InitializeEvents();
            InitializeDocking();

            Global.Inst.Initialize();
            LoadDockingWindows();
            LoadSetting();

            this.Shown += (s, e) =>
            {
                bool loaded = Global.Inst.InspStage.LastestModelOpen(); // ← 너가 보여준 그 함수 호출
                if (loaded) ApplyModelToUI();
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
            // 리소스 이미지 불러오기
            Bitmap bmp = new Bitmap(Properties.Resources.이미지);

            // 흰색을 투명 처리
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    if (c.R > 180 && c.G > 180 && c.B > 180) // 거의 흰색
                    {
                        bmp.SetPixel(x, y, Color.Transparent);
                    }
                }
            }

            // PictureBox에 적용
            pictureBox1.Image = bmp;
            // PictureBox 배경색을 SideMenu와 동일하게
            pictureBox1.BackColor = SideMenu.BackColor;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Global.Inst.Dispose();
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

        // Overview 버튼을 위한 DockPanel 초기화 메서드
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
        #endregion

        private void LoadSetting()
        {
            _isCycleMode = SettingXml.Inst.CycleMode;
            UpdateCycleModeButtonUI();
        }

        private void btnOverview_Click(object sender, EventArgs e)
        {
            try
            {
                ResetDockLayout();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"레이아웃 초기화 실패: {ex.Message}");
            }
        }

        private void btnCycleMode_Click(object sender, EventArgs e)
        {
            _isCycleMode = !_isCycleMode;
            SettingXml.Inst.CycleMode = _isCycleMode;

            UpdateCycleModeButtonUI();
        }

        private void btnSetUp_Click(object sender, EventArgs e)
        {
            SLogger.Write($"환경설정창 열기");
            SetupForm setupForm = new SetupForm();
            setupForm.ShowDialog();
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
            NewModel newModel = new NewModel();
            newModel.ShowDialog();

            Model curModel = Global.Inst.InspStage.CurModel;
            if (curModel != null)
            {
                this.Text = GetModelTitle(curModel);
            }
        }

        private void modelOpenMenuItem_Click(object sender, EventArgs e)
        {
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
            Global.Inst.InspStage.SaveModel("");
        }

        private void modelSaveAsMenuItem_Click(object sender, EventArgs e)
        {
            //다른이름으로 모델 파일 저장
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = SettingXml.Inst.ModelDir;
                saveFileDialog.Title = "모델 파일 선택";
                saveFileDialog.Filter = "Model Files|*.xml;";
                saveFileDialog.DefaultExt = "xml";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;
                    Global.Inst.InspStage.SaveModel(filePath);
                }
            }
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
