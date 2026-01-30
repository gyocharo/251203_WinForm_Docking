using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Deployment.Application;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PureGate.Algorithm;
using PureGate.Grab;
using PureGate.Setting;
using PureGate.Teach;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.IO;
using PureGate.Inspect;
using Microsoft.Win32;
using PureGate.Util;
using System.Windows.Forms;
using PureGate.Sequence;
using PureGate.UIControl;
using MessagingLibrary;

namespace PureGate.Core
{
    public class InspStage : IDisposable
    {
        SaigeAI _saigeAI;

        public static readonly int MAX_GRAB_BUF = 5;

        private ImageSpace _imageSpace = null;
        //private HikRobotCam _grabManager = null;
        private GrabModel _grabManager = null;
        private CameraType _camType = CameraType.WebCam;

        private PreviewImage _previewImage = null;

        private Model _model = null;

        private InspWindow _selectedInspWindow = null;

        //#15_INSP_WORKER#5 InspWorker 클래스 선언
        private InspWorker _inspWorker = null;
        private ImageLoader _imageLoader = null;

        //#16_LAST_MODELOPEN#1 가장 최근 모델 파일 경로와 저장할 REGISTRY 키 변수 선언

        // 레지스트리 키 생성 또는 열기
        RegistryKey _regKey = null;

        //가장 최근 모델 파일 경로를 저장하는 변수
        private bool _lastestModelOpen = false;

        public bool UseCamera { get; set; } = false;

        public bool SaveCamImage { get; set; } = false;
        public int SaveImageIndex { get; set; } = 0;

        private string _capturePath = "";

        private string _lotNumber;
        private string _serialID;

        private bool _isInspectMode = false;

        // ✅ 추가: public 프로퍼티로 노출
        public bool IsInspectMode
        {
            get => _isInspectMode;
            set => _isInspectMode = value;
        }

        private string _loadedImageDir = "";

        //NG 이미지가 저장되면, 저장된 파일 경로(string)를 알려주는 이벤트
        public static event Action<string> NGImageSaved;

        private int _okCount = 0;
        private int _ngCount = 0;

        private List<NgClassCount> _ngClassList = new List<NgClassCount>();

        public InspStage() { }

        public ImageSpace ImageSpace
        {
            get => _imageSpace;
        }

        private readonly Dictionary<string, int> _donutStats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public SaigeAI AIModule
        {
            get
            {
                if (_saigeAI is null)
                    _saigeAI = new SaigeAI();
                return _saigeAI;
            }
        }

        public PreviewImage PreView
        {
            get => _previewImage;
        }

        //#15_INSP_WORKER#6 InspWorker 프로퍼티
        public InspWorker InspWorker
        {
            get => _inspWorker;
        }

        public Model CurModel
        {
            get => _model;
        }

        public bool LiveMode { get; set; } = false;

        public int SelBufferIndex { get; set; } = 0;

        public eImageChannel SelImageChannel { get; set; } = eImageChannel.Gray;

        public event Action<bool> InspectionCompleted;

        private readonly object _wcfMetaLock = new object();
        private bool _pendingWcfSave = false;

        private string _wcfPart = "";
        private string _wcfSerial = "";
        private string _wcfCam = "";
        private string _wcfLine = "";
        private string _wcfStation = "";
        private string _wcfLotTime = "";

        private readonly object _wcfLock = new object();
        // 고정/기본값(요청 예시 기준)
        private const string WCF_DEFAULT_PART = "TRN001";
        private const string WCF_DEFAULT_LINE = "02";
        private const string WCF_DEFAULT_STATION = "02";
        private const string WCF_DEFAULT_CAM = "01";

        public void SetSaigeModelInfo(string saigeModelPath, AIEngineType engineType)
        {
            if (_model == null) return;
            _model.SaigeModelPath = saigeModelPath ?? string.Empty;
            _model.SaigeEngineType = engineType;
        }


        /// <summary>
        /// 모델(.xml)에 저장된 Saige AI 정보를 기반으로 엔진을 자동 로드합니다.
        /// (모델 재시작/최근 모델 로딩 시 AI도 함께 복구)
        /// </summary>
        private void TryAutoLoadSaigeFromModel()
        {
            if (_model == null) return;

            string path = _model.SaigeModelPath;
            if (string.IsNullOrWhiteSpace(path)) return;
            if (!File.Exists(path)) return;

            try
            {
                AIModule.LoadEngine(path, _model.SaigeEngineType);
                try
                {
                    if (PureGate.Property.AIModuleProp.saigeaiprop != null)
                        PureGate.Property.AIModuleProp.saigeaiprop.SyncFromCurrentModelAndUpdateUI();
                }
                catch { /* UI 갱신 실패는 무시 */ }
            }
            catch (Exception ex)
            {
                // AI 자동 로드는 실패해도 모델 자체 로드는 계속 진행되어야 함
                SLogger.Write($"Saige AI 자동 로드 실패: {ex.Message}", SLogger.LogType.Error);
            }
        }


        private void RaiseInspectionCompleted(bool isOk)
        {
            try
            {
                InspectionCompleted?.Invoke(isOk);
            }
            catch (Exception ex)
            {
                SLogger.Write($"[InspectionCompleted] handler error: {ex.Message}", SLogger.LogType.Error);
            }
        }

        public bool Initialize(Action<double, string> progress = null)
        {
            void Report(double p, string s = null)
            {
                progress?.Invoke(p, s);
            }

            Report(3, "Loading settings...");
            LoadSetting();

            Report(8, "Initializing image buffers...");
            _imageSpace = new ImageSpace();

            Report(13, "Preparing preview...");
            _previewImage = new PreviewImage();

            Report(18, "Starting inspection worker...");
            _inspWorker = new InspWorker();
            _imageLoader = new ImageLoader();

            Report(24, "Loading registry...");
            _regKey = Registry.CurrentUser.CreateSubKey("Software\\WinDocking");

            Report(30, "Creating model instance...");
            _model = new Model();

            Report(33, "Loading settings...");
            LoadSetting();

            Report(38, "Initializing camera...");
            switch (_camType)
            {
                case CameraType.WebCam:
                    _grabManager = new WebCam();
                    break;
                case CameraType.HikRobot:
                    _grabManager = new HikRobotCam();
                    break;
            }

            Report(45, "Allocating grab buffers...");
            if (_grabManager != null && _grabManager.InitGrab() == true)
            {
                _grabManager.TransferCompleted += _multiGrab_TransferCompleted;
                InitModelGrab(MAX_GRAB_BUF);
            }

            Report(52, "Initializing sequence...");
            VisionSequence.Inst.InitSequence();
            VisionSequence.Inst.SeqCommand += SeqCommand;

            Report(55, "Core services ready");
            return true;
        }

        private void LoadSetting()
        {
            _camType = SettingXml.Inst.CamType;
        }

        public void InitModelGrab(int bufferCount)
        {
            if (_grabManager == null)
                return;

            int pixelBpp = 8;
            _grabManager.GetPixelBpp(out pixelBpp);

            int inspectionWidth;
            int inspectionHeight;
            int inspectionStride;
            _grabManager.GetResolution(out inspectionWidth, out inspectionHeight, out inspectionStride);

            if (_imageSpace != null)
            {
                _imageSpace.SetImageInfo(pixelBpp, inspectionWidth, inspectionHeight, inspectionStride);
            }

            SetBuffer(bufferCount);

            eImageChannel imageChannel = (pixelBpp == 24) ? eImageChannel.Color : eImageChannel.Gray;
            SetImageChannel(imageChannel);

        }

        public void SetImageBuffer(string filePath)
        {
            SLogger.Write($"Load Image : {filePath}");

            Mat matImage = Cv2.ImRead(filePath);

            int pixelBpp = 8;
            int imageWidth;
            int imageHeight;
            int imageStride;

            if (matImage.Type() == MatType.CV_8UC3)
                pixelBpp = 24;

            imageWidth = (matImage.Width + 3) / 4 * 4;
            imageHeight = matImage.Height;

            // 4바이트 정렬된 새로운 Mat 생성
            Mat alignedMat = new Mat();
            Cv2.CopyMakeBorder(matImage, alignedMat, 0, 0, 0, imageWidth - matImage.Width, BorderTypes.Constant, Scalar.Black);

            imageStride = imageWidth * matImage.ElemSize();

            if (_imageSpace != null)
            {
                if (_imageSpace.ImageSize.Width != imageWidth || _imageSpace.ImageSize.Height != imageHeight)
                {
                    _imageSpace.SetImageInfo(pixelBpp, imageWidth, imageHeight, imageStride);
                    SetBuffer(_imageSpace.BufferCount);
                }
            }

            int bufferIndex = 0;

            // Mat의 데이터를 byte 배열로 복사
            int bufSize = (int)(alignedMat.Total() * alignedMat.ElemSize());
            Marshal.Copy(alignedMat.Data, ImageSpace.GetInspectionBuffer(bufferIndex), 0, bufSize);

            _imageSpace.Split(bufferIndex);

            DisplayGrabImage(bufferIndex);
        }

        public void CheckImageBuffer()
        {
            if (_grabManager != null && SettingXml.Inst.CamType != CameraType.None)
            {
                int imageWidth;
                int imageHeight;
                int imageStride;
                _grabManager.GetResolution(out imageWidth, out imageHeight, out imageStride);

                if (_imageSpace.ImageSize.Width != imageWidth || _imageSpace.ImageSize.Height != imageHeight)
                {
                    int pixelBpp = 8;
                    _grabManager.GetPixelBpp(out pixelBpp);

                    _imageSpace.SetImageInfo(pixelBpp, imageWidth, imageHeight, imageStride);
                    SetBuffer(_imageSpace.BufferCount);
                }
            }
        }

        private void UpdateProperty(InspWindow inspWindow)
        {
            if (inspWindow is null)
                return;

            PropertiesForm propertiesForm = MainForm.GetDockForm<PropertiesForm>();
            if (propertiesForm is null)
                return;

            propertiesForm.UpdateProperty(inspWindow);
        }

        public void UpdateTeachingImage(int index)
        {
            if (_selectedInspWindow is null)
                return;

            SetTeachingImage(_selectedInspWindow, index);
        }

        public void DelTeachingImage(int index)
        {
            if (_selectedInspWindow is null)
                return;

            InspWindow inspWindow = _selectedInspWindow;

            inspWindow.DelWindowImage(index);

            MatchAlgorithm matchAlgo = (MatchAlgorithm)inspWindow.FindInspAlgorithm(InspectType.InspMatch);
            if (matchAlgo != null)
            {
                UpdateProperty(inspWindow);
            }
        }

        public void SetTeachingImage(InspWindow inspWindow, int index = -1)
        {
            if (inspWindow is null)
                return;

            CameraForm cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm is null)
                return;

            Mat curImage = cameraForm.GetDisplayImage();
            if (curImage is null)
                return;

            if (inspWindow.WindowArea.Right >= curImage.Width ||
                inspWindow.WindowArea.Bottom >= curImage.Height)
            {
                SLogger.Write("ROI 영역이 잘못되었습니다!");
                return;
            }

            Mat windowImage = curImage[inspWindow.WindowArea];

            if (index < 0)
                inspWindow.AddWindowImage(windowImage);
            else
                inspWindow.SetWindowImage(windowImage, index);

            inspWindow.IsPatternLearn = false;

            MatchAlgorithm matchAlgo = (MatchAlgorithm)inspWindow.FindInspAlgorithm(InspectType.InspMatch);
            if (matchAlgo != null)
            {
                matchAlgo.ImageChannel = SelImageChannel;
                if (matchAlgo.ImageChannel == eImageChannel.Color)
                    matchAlgo.ImageChannel = eImageChannel.Gray;
                UpdateProperty(inspWindow);
            }
        }

        public bool SaveGoldenImages(bool selectedOnly = true)
        {
            if (CurModel == null)
            {
                SLogger.Write("[Golden] 모델이 없습니다.");
                return false;
            }

            CameraForm cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm == null)
            {
                SLogger.Write("[Golden] CameraForm을 찾을 수 없습니다.");
                return false;
            }

            Mat curImage = cameraForm.GetDisplayImage();
            if (curImage == null || curImage.Empty())
            {
                SLogger.Write("[Golden] 현재 이미지가 없습니다.");
                return false;
            }

            // 저장 대상 ROI 목록 구성
            var targets = new List<InspWindow>();

            if (selectedOnly && _selectedInspWindow != null)
            {
                targets.Add(_selectedInspWindow);
            }
            else
            {
                if (CurModel.InspWindowList != null)
                    targets.AddRange(CurModel.InspWindowList.Where(w => w != null));
            }

            if (targets.Count == 0)
            {
                SLogger.Write("[Golden] 저장할 ROI가 없습니다.");
                return false;
            }

            // SaveGoldenImagesFromSelected 재사용
            return SaveGoldenImagesFromSelected(targets);
        }

        public void SetBuffer(int bufferCount)
        {
            _imageSpace.InitImageSpace(bufferCount);

            if (_grabManager != null)
            {
                _grabManager.InitBuffer(bufferCount);

                for (int i = 0; i < bufferCount; i++)
                {
                    _grabManager.SetBuffer(
                        _imageSpace.GetInspectionBuffer(i),
                        _imageSpace.GetnspectionBufferPtr(i),
                        _imageSpace.GetInspectionBufferHandle(i),
                        i);
                }
            }
            SLogger.Write("버퍼 초기화 성공!");
        }

        public void TryInspection(InspWindow inspWindow)
        {
            UpdateDiagramEntity();

            // ROI가 없거나 선택된 ROI가 없을 때도 검사 + OK/NG 표시
            if (inspWindow == null)
            {
                // 1) 모델에 ROI 자체가 0개면: CLS(전체 이미지) 검사로 처리
                if (CurModel != null && (CurModel.InspWindowList == null || CurModel.InspWindowList.Count == 0))
                {
                    if (AIModule != null && AIModule.IsEngineLoaded)
                    {
                        Bitmap bitmap = GetBitmap();
                        if (bitmap != null)
                        {
                            AIModule.InspAIModule(bitmap);

                            Bitmap resultImage = AIModule.GetResultImage();
                            UpdateDisplay(resultImage);

                            // Top1 기반으로 OK/NG 통일 판정 (실패는 NG)
                            string label = "";
                            float score = 0f;
                            bool hasCls = AIModule.TryGetLastClsTop1(out label, out score) && !string.IsNullOrWhiteSpace(label);

                            bool ok = hasCls && DecideOkFromClsTop1(label, score);
                            if (!hasCls)
                            {
                                label = "Unknown";
                                ok = false;
                                SLogger.Write("[CLS] Top1 FAIL (TryInspection) -> Unknown => NG", SLogger.LogType.Error);
                            }
                            else
                            {
                                SLogger.Write($"[CLS] (TryInspection) label='{label}', score={score:0.000}, ok={ok}");
                            }

                            UpdateResultUI(ok);
                            RaiseInspectionCompleted(ok);
                        }
                    }
                    return; // 여기서 끝
                }

                // 2) ROI는 있는데 "선택만 안 된 상태"면: ROI 전체 검사로 처리
                bool isDefect;
                if (_inspWorker.RunInspect(out isDefect))
                {
                    bool ok = !isDefect;
                    UpdateResultUI(ok);
                    RaiseInspectionCompleted(ok);
                }

                return;
            }

            // ROI 하나 선택되어 있으면 기존 로직 유지
            InspWorker.TryInspect(inspWindow, InspectType.InspNone);
        }

        public void SelectInspWindow(InspWindow inspWindow)
        {
            _selectedInspWindow = inspWindow;

            var propForm = MainForm.GetDockForm<PropertiesForm>();
            if (propForm != null)
            {
                if (inspWindow is null)
                {
                    // ROI 선택 전에는 AIModuleProp만 표시
                    propForm.ShowAIModuleOnly();
                    return;
                }

                propForm.ShowProperty(inspWindow);
            }

            UpdateProperty(inspWindow);

            Global.Inst.InspStage.PreView.SetInspWindow(inspWindow);
        }

        public void AddInspWindow(InspWindowType windowType, Rect rect)
        {
            InspWindow inspWindow = _model.AddInspWindow(windowType);
            if (inspWindow is null)
                return;

            inspWindow.WindowArea = rect;
            inspWindow.IsTeach = false;

            SetTeachingImage(inspWindow);
            UpdateProperty(inspWindow);
            UpdateDiagramEntity();

            CameraForm cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.SelectDiagramEntity(inspWindow);
                SelectInspWindow(inspWindow);
            }
        }

        public bool AddInspWindow(InspWindow sourceWindow, OpenCvSharp.Point offset)
        {
            InspWindow cloneWindow = sourceWindow.Clone(offset);
            if (cloneWindow is null)
                return false;

            if (!_model.AddInspWindow(cloneWindow))
                return false;

            UpdateProperty(cloneWindow);
            UpdateDiagramEntity();

            CameraForm cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.SelectDiagramEntity(cloneWindow);
                SelectInspWindow(cloneWindow);
            }
            return true;
        }

        public void MoveInspWindow(InspWindow inspWindow, OpenCvSharp.Point offset)
        {
            if (inspWindow == null)
                return;

            inspWindow.OffsetMove(offset);
            UpdateProperty(inspWindow);
        }

        public void ModifyInspWindow(InspWindow inspWindow, Rect rect)
        {
            if (inspWindow == null)
                return;

            inspWindow.WindowArea = rect;
            inspWindow.IsTeach = false;

            UpdateProperty(inspWindow);
        }

        public void DelInspWindow(InspWindow inspWindow)
        {
            _model.DelInspWindow(inspWindow);
            UpdateDiagramEntity();
        }


        public void DelInspWindow(List<InspWindow> inspWindowList)
        {
            _model.DelInspWindowList(inspWindowList);
            UpdateDiagramEntity();
        }

        public bool Grab(int bufferIndex)
        {
            if (_grabManager == null)
                return false;

            CheckImageBuffer();

            if (!_grabManager.Grab(bufferIndex, true))
                return false;

            return true;
        }

        public bool ApplyCameraSetting()
        {
            StopCycle(); // 실행중 정지

            // 최신 설정 반영
            _camType = SettingXml.Inst.CamType;

            // 기존 카메라 정리
            if (_grabManager != null)
            {
                try { _grabManager.TransferCompleted -= _multiGrab_TransferCompleted; } catch { }
                _grabManager.Dispose();
                _grabManager = null;
            }

            // 새 카메라 생성
            switch (_camType)
            {
                case CameraType.WebCam:
                    _grabManager = new WebCam();
                    break;
                case CameraType.HikRobot:
                    _grabManager = new HikRobotCam();
                    break;
                case CameraType.None:
                default:
                    _grabManager = null;
                    break;
            }

            // 카메라 있으면 초기화 + 버퍼 세팅
            if (_grabManager != null && _grabManager.InitGrab())
            {
                _grabManager.TransferCompleted += _multiGrab_TransferCompleted;
                InitModelGrab(MAX_GRAB_BUF);
            }

            // 런타임 상태 갱신
            UseCamera = (SettingXml.Inst.CamType != CameraType.None);
            CheckImageBuffer();
            ResetDisplay();

            return true;
        }

        //영상 취득 완료 이벤트 발생시 후처리
        private async void _multiGrab_TransferCompleted(object sender, object e)
        {
            int bufferIndex = (int)e;
            SLogger.Write($"TransferCompleted {bufferIndex}");

            _imageSpace.Split(bufferIndex);

            if (_isInspectMode && SettingXml.Inst.CommType == CommunicatorType.WCF)
            {
                bool doSave = false;
                lock (_wcfLock) doSave = _pendingWcfSave;

                if (doSave)
                    SaveWcfGrabImageAndSetInspectPath(bufferIndex);
            }

            if (SettingXml.Inst.CommType == CommunicatorType.WCF)
            {
                bool doSave = false;
                lock (_wcfMetaLock) doSave = _pendingWcfSave;

                if (doSave)
                    SaveWcfGrabImage(bufferIndex);
            }

            if (SaveCamImage && Directory.Exists(_capturePath))
            {
                Mat curImage = GetMat(0, eImageChannel.Color);

                if (curImage != null)
                {
                    string imageName = $"{++SaveImageIndex:D4}.png";
                    string savePath = Path.Combine(_capturePath, imageName);
                    curImage.SaveImage(savePath);
                }
            }

            DisplayGrabImage(bufferIndex);

            //#8_LIVE#2 LIVE 모드일때, Grab을 계속 실행하여, 반복되도록 구현
            //이 함수는 await를 사용하여 비동기적으로 실행되어, 함수를 async로 선언해야 합니다.
            if (LiveMode)
            {
                SLogger.Write("Grab");
                await Task.Delay(100);
                _grabManager.Grab(bufferIndex, true);
            }

            if (_isInspectMode)
                RunInspect();
        }

        private void DisplayGrabImage(int bufferIndex)
        {
            var cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                // UI 스레드에서 실행되도록 보장
                if (cameraForm.InvokeRequired)
                {
                    cameraForm.Invoke(new Action(() => cameraForm.UpdateDisplay()));
                }
                else
                {
                    cameraForm.UpdateDisplay();
                }
            }

        }

        public void UpdateDisplay(Bitmap bitmap)
        {
            var cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                // UI 스레드에서 실행되도록 보장
                if (cameraForm.InvokeRequired)
                {
                    cameraForm.Invoke(new Action(() => cameraForm.UpdateDisplay(bitmap)));
                }
                else
                {
                    cameraForm.UpdateDisplay(bitmap);
                }
            }

        }

        public void SetPreviewImage(eImageChannel channel)
        {
            if (_previewImage is null)
                return;

            Bitmap bitmap = ImageSpace.GetBitmap(0, channel);
            _previewImage.SetImage(BitmapConverter.ToMat(bitmap));

            SetImageChannel(channel);
        }

        public void SetImageChannel(eImageChannel channel)
        {
            var cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.SetImageChannel(channel);
            }
        }

        public Bitmap GetBitmap(int bufferIndex = -1, eImageChannel imageChannel = eImageChannel.None)
        {
            if (bufferIndex >= 0)
                SelBufferIndex = bufferIndex;

            //#BINARY FILTER#13 채널 정보가 유지되도록, eImageChannel.None 타입을 추가
            if (imageChannel != eImageChannel.None)
                SelImageChannel = imageChannel;

            if (Global.Inst.InspStage.ImageSpace is null)
                return null;

            return Global.Inst.InspStage.ImageSpace.GetBitmap(SelBufferIndex, SelImageChannel);
        }

        //#7_BINARY_PREVIEW#4 이진화 프리뷰를 위해, ImageSpace에서 이미지 가져오기
        public Mat GetMat(int bufferIndex = -1, eImageChannel imageChannel = eImageChannel.None)
        {
            if (bufferIndex >= 0)
                SelBufferIndex = bufferIndex;

            return Global.Inst.InspStage.ImageSpace.GetMat(SelBufferIndex, imageChannel);
        }

        //#10_INSPWINDOW#14 변경된 모델 정보 갱신하여, ImageViewer와 모델트리에 반영
        public void UpdateDiagramEntity()
        {
            CameraForm cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.UpdateDiagramEntity();
            }

            ModelTreeForm modelTreeForm = MainForm.GetDockForm<ModelTreeForm>();
            if (modelTreeForm != null)
            {
                modelTreeForm.UpdateDiagramEntity();
            }
        }

        //#7_BINARY_PREVIEW#5 이진화 임계값 변경시, 프리뷰 갱신
        public void RedrawMainView()
        {
            CameraForm cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.UpdateImageViewer();
            }
        }

        public void ResetDisplay()
        {
            CameraForm cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.ResetDisplay();
            }
        }

        public bool LoadModel(string filePath)
        {
            SLogger.Write($"모델 로딩:{filePath}");

            _model = _model.Load(filePath);

            if (_model is null)
            {
                SLogger.Write($"모델 로딩 실패:{filePath}");
                return false;
            }

            string inspImagePath = _model.InspectImagePath;
            if (File.Exists(inspImagePath))
            {
                Global.Inst.InspStage.SetImageBuffer(inspImagePath);

                UpdateDisplay((System.Drawing.Bitmap)null);
            }

            TryAutoLoadSaigeFromModel();
            UpdateDiagramEntity();

            //#16_LAST_MODELOPEN#3 마지막 저장 모델 경로를 레지스트리에 저장
            _regKey.SetValue("LastestModelPath", filePath);

            return true;
        }

        public void SaveModel(string filePath)
        {
            SLogger.Write($"모델 저장:{filePath}");

            //입력 경로가 없으면 현재 모델 저장
            if (string.IsNullOrEmpty(filePath))
                Global.Inst.InspStage.CurModel.Save();
            else
                Global.Inst.InspStage.CurModel.SaveAs(filePath);

            // ✅ 저장이 끝난 "최종 경로"로 최근 모델 갱신
            if (_regKey != null && CurModel != null && !string.IsNullOrWhiteSpace(CurModel.ModelPath))
                _regKey.SetValue("LastestModelPath", CurModel.ModelPath);
        }

        public bool LastestModelOpen(IWin32Window owner = null)
        {
            if (_lastestModelOpen) return true;
            _lastestModelOpen = true;

            if (_regKey == null) return true;

            string lastestModel = _regKey.GetValue("LastestModelPath") as string;
            if (string.IsNullOrWhiteSpace(lastestModel) || !File.Exists(lastestModel))
                return true;

            var result = MsgBox.Show(
                owner,
                $"최근 모델을 로딩할까요?\r\n{lastestModel}",
                "Question",
                MessageBoxButtons.YesNo);

            if (result == DialogResult.No) return true;
            return LoadModel(lastestModel);
        }

        //#15_INSP_WORKER#9 자동 연속 검사 함수
        public void CycleInspect(bool isCycle)
        {
            if (InspWorker.IsRunning)
                return;

            if (!UseCamera)
            {
                string inspImagePath = CurModel.InspectImagePath;
                string inspImageDir = Path.GetDirectoryName(inspImagePath);

                if (!Directory.Exists(inspImageDir))
                    return;

                // 폴더가 바뀌었으면 무조건 다시 로드
                if (_loadedImageDir != inspImageDir)
                {
                    _imageLoader.LoadImages(inspImageDir);
                    _imageLoader.Reset();          // ← 이전 인덱스 제거
                    _loadedImageDir = inspImageDir;
                }
                else
                {
                    // 같은 폴더지만 아직 로드 안 된 경우
                    if (!_imageLoader.IsLoadedImages())
                        _imageLoader.LoadImages(inspImageDir);
                }
            }

            if (isCycle)
                _inspWorker.StartCycleInspectImage();
            else
                OneCycle();
        }

        public bool OneCycle()
        {
            if (UseCamera)
            {
                if (!Grab(0)) return false;
            }
            else
            {
                if (!VirtualGrab()) return false;
            }

            ResetDisplay();

            // ROI(검사 윈도우)가 하나도 없으면: AIModuleProp의 "적용"과 동일 흐름으로 검사
            //    (검사 버튼 누를 때마다 다음 이미지로 넘어가는 VirtualGrab/Grab 흐름은 그대로 유지됨)
            // 1. ROI가 없을 때 (AI 모듈 직접 실행 케이스)
            if (CurModel != null && (CurModel.InspWindowList == null || CurModel.InspWindowList.Count == 0))
            {
                if (AIModule != null && AIModule.IsEngineLoaded)
                {
                    Bitmap bitmap = GetBitmap();
                    if (bitmap != null)
                    {
                        AIModule.InspAIModule(bitmap);

                        Bitmap resultImage = AIModule.GetResultImage();
                        UpdateDisplay(resultImage);

                        // CLS 분류 결과 이미지 저장(있으면)
                        TrySaveClsResultImage(resultImage);

                        try
                        {
                            // Top1 기반 OK/NG 통일 판정 (실패는 NG)
                            string label = "";
                            float score = 0f;
                            bool hasCls = AIModule.TryGetLastClsTop1(out label, out score) && !string.IsNullOrWhiteSpace(label);

                            bool ok = hasCls && DecideOkFromClsTop1(label, score);
                            if (!hasCls)
                            {
                                label = "Unknown";
                                ok = false;
                                SLogger.Write("[CLS] Top1 FAIL (OneCycle) -> Unknown => NG", SLogger.LogType.Error);
                            }
                            else
                            {
                                SLogger.Write($"[CLS] (OneCycle) label='{label}', score={score:0.000}, ok={ok}");
                            }

                            UpdateResultUI(ok);
                            RaiseInspectionCompleted(ok);

                            string modelName = "";
                            if (CurModel != null && !string.IsNullOrWhiteSpace(CurModel.ModelPath))
                                modelName = Path.GetFileNameWithoutExtension(CurModel.ModelPath);

                            InspHistoryRepo.Append(new InspHistoryRecord
                            {
                                Time = DateTime.Now,
                                ModelName = modelName,
                                LotNumber = _lotNumber ?? "",
                                SerialID = _serialID ?? "",
                                Total = 1,
                                Ok = ok ? 1 : 0,
                                Ng = ok ? 0 : 1,
                                NgClass = ok ? "" : label,
                                Score = score
                            });

                            // 도넛(OK + NG 클래스)=

                            // ResultForm에도 반영
                            TryUpdateResultFormForAIModuleOnly();

                            var cForm = MainForm.GetDockForm<CameraForm>();
                            if (cForm != null) cForm.ShowResultOnScreen(ok);
                        }
                        catch (Exception ex)
                        {
                            SLogger.Write($"[History Save] Failed: {ex.Message}", SLogger.LogType.Error);
                        }

                        return true;
                    }
                }
                return true;
            }

            // 2. ROI가 있는 기존 검사 로직 케이스
            bool isDefect;
            if (!_inspWorker.RunInspect(out isDefect))
                return false;

            // ✅ 추가된 코드: ROI 검사 결과도 UI에 전송
            // isDefect가 true이면 불량이므로, ok는 false가 되어야 함 (!isDefect)
            UpdateResultUI(!isDefect);

            return true;
        }

        private void TryUpdateResultFormForAIModuleOnly()
        {
            try
            {
                ResultForm resultForm = MainForm.GetDockForm<ResultForm>();
                if (resultForm == null)
                {
                    SLogger.Write("[InspStage] ResultForm is null (cannot update).", SLogger.LogType.Error);
                    return;
                }

                string imageFileName = "";
                if (CurModel != null && !string.IsNullOrEmpty(CurModel.InspectImagePath))
                    imageFileName = Path.GetFileName(CurModel.InspectImagePath);

                string label = "";
                float score = 0f;
                bool hasCls = (AIModule != null) && AIModule.TryGetLastClsTop1(out label, out score) && !string.IsNullOrWhiteSpace(label);

                bool isOk = hasCls && DecideOkFromClsTop1(label, score);
                bool isDefect = !isOk; // Top1 없으면 NG 처리(안전)

                var window = new InspWindow(InspWindowType.None, "AIModule");
                window.UID = "AIModule";

                var res = new InspResult();
                res.ObjectID = "AIModule";
                res.ObjectType = InspWindowType.None;
                res.InspType = InspectType.InspAIModule;
                res.IsDefect = isDefect;

                // NG면 라벨을 남겨서 ResultForm에서 상태가 보이게
                res.ResultValue = hasCls ? label : "Unknown";

                if (hasCls)
                    res.ResultInfos = $"CLS: {(isDefect ? "NG" : "OK")}, Label={label}, Score={score:0.000}";
                else
                    res.ResultInfos = "CLS: NG (no top1 info)";

                res.ParseImageFileName(imageFileName);

                window.AddInspResult(res);
                resultForm.AddWindowResult(window);
            }
            catch (Exception ex)
            {
                SLogger.Write($"[InspStage] TryUpdateResultFormForAIModuleOnly failed: {ex.Message}", SLogger.LogType.Error);
            }
        }

        private void TrySaveClsResultImage(Bitmap resultImage)
        {
            if (resultImage == null) return;
            if (AIModule == null) return;

            // CLS가 아닐 수도 있으니 라벨이 없으면 그냥 종료
            if (!AIModule.TryGetLastClsTop1(out string label, out float score)) return;
            if (string.IsNullOrWhiteSpace(label)) return;

            try
            {
                string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");

                // 라벨명 폴더 안전 처리
                string safeLabel = SanitizePathSegment(label);

                string root;
                string targetDir;

                // ✅ exe 폴더 기준 상위 4단계(=프로젝트 루트쪽) 경로 구하기
                string baseDir = AppContext.BaseDirectory; // exe 있는 폴더
                var di = new DirectoryInfo(baseDir);
                for (int i = 0; i < 4 && di.Parent != null; i++)
                    di = di.Parent;

                string projectRoot = di.FullName;

                if (string.Equals(label, "Good", StringComparison.OrdinalIgnoreCase))
                {
                    root = Path.Combine(projectRoot, "Good");
                    targetDir = Path.Combine(root, dateFolder);
                }
                else
                {
                    root = Path.Combine(projectRoot, "NG");
                    targetDir = Path.Combine(root, safeLabel, dateFolder);
                }

                // 폴더 없으면 생성 (Good/NG 모두)
                Directory.CreateDirectory(targetDir);

                // 파일명: 시간_라벨.jpg (충돌 방지)
                string ts = DateTime.Now.ToString("HH.mm.ss.ff");
                string fileName = $"{ts}_{safeLabel}.jpg";
                string fullPath = Path.Combine(targetDir, fileName);

                // 이미지 저장 (resultImage는 화면 표시용일 수 있으니 Clone해서 저장)
                using (Bitmap clone = (Bitmap)resultImage.Clone())
                {
                    clone.Save(fullPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                SLogger.Write($"[AI-CLS Save] {label}({score:0.000}) -> {fullPath}", SLogger.LogType.Info);

                // ✅ NG 이미지인 경우에만 이벤트 발생
                if (!string.Equals(label, "Good", StringComparison.OrdinalIgnoreCase))
                {
                    NGImageSaved?.Invoke(fullPath);
                }

            }
            catch (Exception ex)
            {
                // 저장 실패해도 기존 검사/넘김 흐름은 그대로 유지
                SLogger.Write($"[AI-CLS Save] Failed: {ex.Message}", SLogger.LogType.Error);
            }
        }

        private string SanitizePathSegment(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unknown";

            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            // 너무 길면 잘라서 Windows 경로 문제 방지
            if (name.Length > 80) name = name.Substring(0, 80);

            return name.Trim();
        }

        public void StopCycle()
        {
            if (_inspWorker != null)
                _inspWorker.Stop();

            VisionSequence.Inst.StopAutoRun();
            _isInspectMode = false;

            SetWorkingState(WorkingState.NONE);
        }

        public bool VirtualGrab()
        {
            if (_imageLoader is null)
                return false;

            string imagePath = _imageLoader.GetNextImagePath();
            if (imagePath == "")
                return false;

            if (_model != null)
            {
                _model.InspectImagePath = imagePath;
                SLogger.Write($"[InspStage] Updated InspectImagePath: {Path.GetFileName(imagePath)}");
            }

            Global.Inst.InspStage.SetImageBuffer(imagePath);

            _imageSpace.Split(0);

            DisplayGrabImage(0);

            return true;
        }

        private void SeqCommand(object sender, SeqCmd seqCmd, object Param)
        {
            switch (seqCmd)
            {
                case SeqCmd.InspStart:
                    {
                        SLogger.Write("MMI : InspStart", SLogger.LogType.Info);

                        if (SettingXml.Inst.CommType == CommunicatorType.WCF)
                            PrepareWcfSaveMeta(Param);

                        string errMsg;

                        if (UseCamera)
                        {
                            if (!Grab(0))
                            {
                                errMsg = string.Format("Failed to grab");
                                SLogger.Write(errMsg, SLogger.LogType.Error);
                            }
                        }
                        else
                        {
                            if (!VirtualGrab())
                            {
                                errMsg = string.Format("Failed to virtual grab");
                                SLogger.Write(errMsg, SLogger.LogType.Error);
                            }
                        }
                    }
                    break;
                case SeqCmd.InspEnd:
                    {
                        SLogger.Write("MMI : InspEnd", SLogger.LogType.Info);

                        string errMsg = "";

                        SLogger.Write("검사 종료");

                        VisionSequence.Inst.VisionCommand(Vision2Mmi.InspEnd, errMsg);
                    }
                    break;
            }
        }

        private void RunInspect()
        {
            SetWorkingState(WorkingState.INSPECT);

            // 검사 시작 전 상태 초기화
            ResetDisplay();

            // ✅ 1) ROI가 없으면: Saige AI(CLS)로 검사하고 결과이미지(오버레이)를 표시
            if (CurModel != null && (CurModel.InspWindowList == null || CurModel.InspWindowList.Count == 0))
            {
                if (AIModule != null && AIModule.IsEngineLoaded)
                {
                    // LIVE가 켜져있으면, 결과 표시가 바로 덮일 수 있으니 필요시 끄는 것을 권장
                    // LiveMode = false;

                    Bitmap bitmap = GetBitmap();
                    if (bitmap != null)
                    {
                        AIModule.InspAIModule(bitmap);

                        // ✅ 여기서 Good(100.0) 오버레이가 그려진 이미지가 만들어짐
                        Bitmap resultImage = AIModule.GetResultImage();
                        UpdateDisplay(resultImage);

                        TrySaveClsResultImage(resultImage);

                        // 판정(OK/NG) 결정
                        bool ok = true;
                        string label = "";
                        float score = 0;

                        var modelInfo = AIModule.GetModelInfo();

                        if (AIModule.TryGetLastClsTop1(out label, out score) && !string.IsNullOrWhiteSpace(label))
                        {
                            label = label.Trim();

                            // 혹시 label에 부가 텍스트가 붙는 경우 대비 (예: "Good(…)" or "Good …")
                            int cutIdx = label.IndexOf('(');
                            if (cutIdx >= 0) label = label.Substring(0, cutIdx).Trim();

                            cutIdx = label.IndexOf(' ');
                            if (cutIdx >= 0) label = label.Substring(0, cutIdx).Trim();

                            bool isNg = false;

                            // 가능하면 모델의 ClassIsNG 매핑 사용
                            if (modelInfo != null && modelInfo.ClassInfos != null && modelInfo.ClassIsNG != null)
                            {
                                int idx = Array.FindIndex(modelInfo.ClassInfos,
                                    c => string.Equals(c.Name?.Trim(), label, StringComparison.OrdinalIgnoreCase));

                                if (idx < 0)
                                {
                                    idx = Array.FindIndex(modelInfo.ClassInfos,
                                        c => (label ?? "").StartsWith(c.Name?.Trim() ?? "", StringComparison.OrdinalIgnoreCase));
                                }

                                if (idx >= 0 && idx < modelInfo.ClassIsNG.Length)
                                    isNg = modelInfo.ClassIsNG[idx];
                            }
                            else
                            {
                                // fallback: Good이면 OK, 아니면 NG
                                isNg = !string.Equals(label, "Good", StringComparison.OrdinalIgnoreCase);
                            }

                            ok = !isNg;
                        }
                        else
                        {
                            // Top1 못 얻으면 일단 OK로 두거나 정책에 맞게 변경
                            ok = true;
                        }

                        UpdateResultUI(ok);
                        RaiseInspectionCompleted(ok);

                        // 제어기로 결과 전송 (isDefect = !ok)
                        VisionSequence.Inst.VisionCommand(Vision2Mmi.InspDone, !ok);
                        SetWorkingState(WorkingState.NONE);
                        return;
                    }
                }

                // 엔진/이미지 없으면 기존처럼 종료
                VisionSequence.Inst.VisionCommand(Vision2Mmi.InspDone, false);
                SetWorkingState(WorkingState.NONE);
                return;
            }

            // ✅ 2) ROI가 있으면: 기존 ROI 검사 경로 그대로
            bool isDefect = false;
            if (!_inspWorker.RunInspect(out isDefect))
            {
                SLogger.Write("Failed to inspect", SLogger.LogType.Error);
            }

            UpdateResultUI(!isDefect);

            TrySaveRoiResultImage(isDefect);

            RaiseInspectionCompleted(!isDefect);

            VisionSequence.Inst.VisionCommand(Vision2Mmi.InspDone, isDefect);
            SetWorkingState(WorkingState.NONE);
        }

        private void TrySaveRoiResultImage(bool isDefect, string ngFolderName = "ROI_NG")
        {
            if (!isDefect) return;

            try
            {
                string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
                string safeLabel = SanitizePathSegment(ngFolderName);

                // ✅ exe 폴더 기준 상위 4단계(=프로젝트 루트쪽) 경로 구하기 (CLS 저장 방식과 동일)
                string baseDir = AppContext.BaseDirectory;
                var di = new DirectoryInfo(baseDir);
                for (int i = 0; i < 4 && di.Parent != null; i++)
                    di = di.Parent;
                string projectRoot = di.FullName;

                string targetDir = Path.Combine(projectRoot, "NG", safeLabel, dateFolder);
                Directory.CreateDirectory(targetDir);

                string ts = DateTime.Now.ToString("HH.mm.ss.ff");
                string fullPath = Path.Combine(targetDir, $"{ts}_{safeLabel}.jpg");

                // ✅ 현재 버퍼 이미지를 JPG로 저장
                using (Bitmap bmp = GetBitmap(0, eImageChannel.Color))
                {
                    if (bmp != null)
                    {
                        bmp.Save(fullPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                    else
                    {
                        // Color가 없으면 Gray라도 저장 시도
                        using (Bitmap gray = GetBitmap(0, eImageChannel.Gray))
                        {
                            if (gray == null) return;
                            gray.Save(fullPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                        }
                    }
                }

                // ✅ CountForm(RecentNGimages) 썸네일 갱신 트리거
                NGImageSaved?.Invoke(fullPath);

                SLogger.Write($"[ROI Save] NG -> {fullPath}", SLogger.LogType.Info);
            }
            catch (Exception ex)
            {
                SLogger.Write($"[ROI Save] Failed: {ex.Message}", SLogger.LogType.Error);
            }
        }

        //검사를 위한 준비 작업
        public bool InspectReady(string lotNumber, string serialID)
        {
            _lotNumber = lotNumber;
            _serialID = serialID;

            LiveMode = false;
            UseCamera = SettingXml.Inst.CamType != CameraType.None ? true : false;

            Global.Inst.InspStage.CheckImageBuffer();

            ResetDisplay();

            return true;
        }

        public bool StartAutoRun()
        {
            SLogger.Write("Action : StartAutoRun");

            if (SaveCamImage && _model != null)
            {
                SaveImageIndex = 0;

                _capturePath = Path.Combine(Path.GetDirectoryName(_model.ModelPath), "Capture");
                if (!Directory.Exists(_capturePath))
                {
                    Directory.CreateDirectory(_capturePath);
                }
                else
                {
                    string[] files = Directory.GetFiles(_capturePath);
                    foreach (string file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            SLogger.Write($"Failed to delete file: {file}. Exception: {ex.Message}", SLogger.LogType.Error);
                        }
                    }
                }
            }

            string modelPath = CurModel.ModelPath;
            if (modelPath == "")
            {
                SLogger.Write("열려진 모델이 없습니다!", SLogger.LogType.Error);
                MsgBox.Show("열려진 모델이 없습니다!");
                return false;
            }

            LiveMode = false;
            UseCamera = SettingXml.Inst.CamType != CameraType.None ? true : false;

            SetWorkingState(WorkingState.INSPECT);

            string modelName = Path.GetFileNameWithoutExtension(modelPath);
            VisionSequence.Inst.StartAutoRun(modelName);
            _isInspectMode = true;

            return true;
        }

        //#17_WORKING_STATE#2 작업 상태 설정
        public void SetWorkingState(WorkingState workingState)
        {
            var cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.SetWorkingState(workingState);
            }
        }

        public void SetExposure(long exposureTime)
        {
            if (_grabManager != null)
            {
                _grabManager.SetExposureTime(exposureTime);
            }
        }

        private void UpdateResultUI(bool isOK)
        {
            if (isOK) _okCount++;
            else _ngCount++;

            // ✅ NG일 때 대표 클래스명 1개 추출 (CLS면 label, ROI면 areas/info)
            string ngName = "";

            if (!isOK)
            {
                // ROI 검사 케이스: 모든 윈도우/알고리즘에서 IsDefect인 것들 클래스명 수집
                foreach (var window in _model.InspWindowList)
                {
                    foreach (var algo in window.AlgorithmList)
                    {
                        if (algo.IsUse && algo.IsDefect)
                        {
                            List<DrawInspectInfo> areas;
                            if (algo.GetResultRect(out areas) > 0 && areas != null && areas.Count > 0 && !string.IsNullOrWhiteSpace(areas[0].info))
                            {
                                ngName = areas[0].info; // ✅ 가장 우선
                                break;
                            }

                            // fallback
                            if (algo.ResultString != null && algo.ResultString.Count > 0)
                            {
                                ngName = algo.ResultString[0];
                                break;
                            }

                            ngName = algo.InspectType.ToString().Replace("Insp", "");
                            break;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(ngName)) break;
                }
            }

            // ✅ 도넛 통계 누적 + 폼 갱신 (OK도 도넛 데이터에 포함됨)
            PushDonutStatsAndUpdateUI(isOK, ngName);

            // 카메라 화면 알림(이건 아래 2번에서 “지울거면” 여기 호출을 꺼도 됨)
            var cForm = MainForm.GetDockForm<CameraForm>();
            if (cForm != null) cForm.ShowResultOnScreen(isOK);
        }

        private void PushDonutStatsAndUpdateUI(bool isOk, string ngClassName)
        {
            // 1) 누적 카운트 업데이트
            if (isOk)
            {
                if (_donutStats.ContainsKey("OK")) _donutStats["OK"]++;
                else _donutStats["OK"] = 1;
            }
            else
            {
                // NG인데 클래스명이 비었으면 Unknown으로
                string key = string.IsNullOrWhiteSpace(ngClassName) ? "Unknown" : ngClassName;

                if (_donutStats.ContainsKey(key)) _donutStats[key]++;
                else _donutStats[key] = 1;
            }

            // 2) StatisticForm에 보낼 리스트 구성 (OK 포함!)
            List<NgClassCount> donutList = _donutStats
                .Select(kvp => new NgClassCount { ClassName = kvp.Key, Count = kvp.Value })
                .ToList();

            // 3) 폼 갱신
            var sForm = MainForm.GetDockForm<StatisticForm>();
            if (sForm != null)
            {
                // ok/ng는 기존대로 전체 카운트(라벨표시용)로 보내고,
                // donutList는 도넛 분할 데이터(OK + NG 클래스별)로 사용
                sForm.UpdateStatistics(_okCount, _ngCount, donutList);
            }

            // (선택) 메인폼에서도 갱신하고 싶으면
            //MainForm.Instance?.UpdateStatisticsUI(_okCount, _ngCount, donutList);
            System.Diagnostics.Debug.WriteLine("[DONUT_KEYS] " + string.Join(" | ", _donutStats.Keys.Select(k => $"'{k}'")));
        }

        public bool LastestModelOpenWithProgress(LoadingForm loading, double startPercent, double endPercent)
        {
            if (_lastestModelOpen) return true;
            _lastestModelOpen = true;

            if (_regKey == null) return true;

            string lastestModel = _regKey.GetValue("LastestModelPath") as string;
            if (string.IsNullOrWhiteSpace(lastestModel) || !File.Exists(lastestModel))
            {
                loading?.SetProgress(endPercent);
                return true;
            }

            loading?.SetProgress(startPercent + (endPercent - startPercent) * 0.15);
            loading?.SetStatus("Checking latest model...");
            loading?.Refresh();
            Application.DoEvents();

            var result = MsgBox.Show(
                loading,
                $"최근 모델을 로딩할까요?\r\n{lastestModel}",
                "Question",
                MessageBoxButtons.YesNo);

            if (result == DialogResult.No)
            {
                loading?.SetProgress(endPercent);
                return true;
            }

            loading?.SetProgress(startPercent + (endPercent - startPercent) * 0.35);
            loading?.SetStatus("Reading model file...");
            loading?.Refresh();
            Application.DoEvents();

            bool ok = LoadModel(lastestModel);

            loading?.SetProgress(startPercent + (endPercent - startPercent) * 0.75);
            loading?.SetStatus("Applying model & AI...");
            loading?.Refresh();
            Application.DoEvents();

            loading?.SetProgress(endPercent);
            return ok;
        }
        // 선택된 InspWindow 목록에 대해 Golden 이미지를 저장하고 RuleBasedAlgorithm에 주입
        public bool SaveGoldenImagesFromSelected(List<InspWindow> selectedWindows)
        {
            if (selectedWindows == null || selectedWindows.Count == 0)
            {
                SLogger.Write("[Golden] 선택된 ROI가 없습니다.");
                return false;
            }

            CameraForm cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm == null)
            {
                SLogger.Write("[Golden] CameraForm을 찾을 수 없습니다.");
                return false;
            }

            Mat curImage = cameraForm.GetDisplayImage();
            if (curImage == null || curImage.Empty())
            {
                SLogger.Write("[Golden] 현재 이미지가 없습니다.");
                return false;
            }

            // 모델 경로 기준 저장 폴더
            string baseDir = AppContext.BaseDirectory;
            try
            {
                if (CurModel != null && !string.IsNullOrWhiteSpace(CurModel.ModelPath))
                {
                    string dir = Path.GetDirectoryName(CurModel.ModelPath);
                    if (!string.IsNullOrWhiteSpace(dir))
                        baseDir = dir;
                }
            }
            catch { }

            string goldenDir = Path.Combine(baseDir, "GoldenImages");
            Directory.CreateDirectory(goldenDir);

            int savedCount = 0;

            foreach (var window in selectedWindows)
            {
                if (window == null) continue;

                // ROI 유효성 체크
                if (window.WindowArea.Right >= curImage.Width ||
                    window.WindowArea.Bottom >= curImage.Height)
                {
                    SLogger.Write($"[Golden] ROI 범위 오류: {window.UID}");
                    continue;
                }

                try
                {
                    // ROI 영역 추출
                    using (Mat roiImage = curImage[window.WindowArea])
                    {
                        // 파일명 생성 (UID + WindowType)
                        string uid = string.IsNullOrWhiteSpace(window.UID)
                            ? window.InspWindowType.ToString()
                            : window.UID;
                        uid = SanitizePathSegment(uid);

                        string filename = $"{uid}_{window.InspWindowType}.png";
                        string savePath = Path.Combine(goldenDir, filename);

                        // 이미지 파일로 저장
                        roiImage.SaveImage(savePath);

                        // RuleBasedAlgorithm 찾아서 Golden 이미지 주입
                        var ruleAlgo = window.FindInspAlgorithm(InspectType.InspRuleBased) as RuleBasedAlgorithm;
                        if (ruleAlgo != null)
                        {
                            ruleAlgo.WindowType = window.InspWindowType;
                            ruleAlgo.IsUse = true; // RuleBased 활성화

                            if (ruleAlgo.SetGoldenImage(roiImage))
                            {
                                SLogger.Write($"[Golden] Saved & Injected: {savePath}");
                                savedCount++;
                            }
                            else
                            {
                                SLogger.Write($"[Golden] 파일 저장 성공했으나 알고리즘 주입 실패: {savePath}");
                            }
                        }
                        else
                        {
                            // RuleBasedAlgorithm이 없으면 새로 생성해서 추가
                            ruleAlgo = new RuleBasedAlgorithm
                            {
                                WindowType = window.InspWindowType,
                                IsUse = true
                            };

                            if (ruleAlgo.SetGoldenImage(roiImage))
                            {
                                window.AlgorithmList.Add(ruleAlgo);
                                SLogger.Write($"[Golden] Saved & New Algorithm Created: {savePath}");
                                savedCount++;
                            }
                            else
                            {
                                SLogger.Write($"[Golden] 새 알고리즘 생성 실패: {savePath}");
                            }
                        }

                        // 다른 알고리즘들은 비활성화
                        foreach (var algo in window.AlgorithmList)
                        {
                            if (algo.InspectType != InspectType.InspRuleBased)
                            {
                                algo.IsUse = false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    SLogger.Write($"[Golden] 저장 중 오류 ({window.UID}): {ex.Message}");
                }
            }

            if (savedCount > 0)
            {
                SLogger.Write($"[Golden] 완료: {savedCount}개 저장 및 주입 -> {goldenDir}");
                return true;
            }

            return false;
        }

        private bool DecideOkFromClsTop1(string rawLabel, float score)
        {
            if (AIModule == null) return false;
            if (string.IsNullOrWhiteSpace(rawLabel)) return false;

            string label = rawLabel.Trim();

            // "Good (0.98)" 같은 케이스 정리
            int cutIdx = label.IndexOf('(');
            if (cutIdx >= 0) label = label.Substring(0, cutIdx).Trim();

            // 혹시 공백으로 뒤에 더 붙는 형태도 정리
            cutIdx = label.IndexOf(' ');
            if (cutIdx >= 0) label = label.Substring(0, cutIdx).Trim();

            var modelInfo = AIModule.GetModelInfo();

            // modelInfo 기준이 가장 신뢰도 높음
            if (modelInfo?.ClassInfos != null && modelInfo.ClassIsNG != null)
            {
                int idx = Array.FindIndex(modelInfo.ClassInfos,
                    c => string.Equals(c.Name?.Trim(), label, StringComparison.OrdinalIgnoreCase));

                if (idx < 0)
                {
                    idx = Array.FindIndex(modelInfo.ClassInfos,
                        c => (label ?? "").StartsWith(c.Name?.Trim() ?? "", StringComparison.OrdinalIgnoreCase));
                }

                if (idx >= 0 && idx < modelInfo.ClassIsNG.Length)
                {
                    bool isNg = modelInfo.ClassIsNG[idx];
                    return !isNg;
                }

                // modelInfo에 없는 라벨이면 안전하게 NG
                SLogger.Write($"[CLS] label '{label}' not found in modelInfo.ClassInfos -> NG (score={score:0.000})", SLogger.LogType.Error);
                return false;
            }

            // fallback: modelInfo 없을 때만 임시로 Good=OK 처리
            bool okFallback = string.Equals(label, "Good", StringComparison.OrdinalIgnoreCase);
            SLogger.Write(
     $"[CLS] modelInfo null -> fallback label='{label}', ok={okFallback}, score={score:0.000}",
     okFallback ? SLogger.LogType.Info : SLogger.LogType.Error
 );
            return okFallback;
        }

        // ✅ 파일명 토큰에서 '_' 들어가면 ResultForm 정규식( [^_]+ ) 파싱이 깨짐
        private string SafeToken(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return "NA";

            // 파일명에 위험한 문자 제거/치환
            foreach (char c in Path.GetInvalidFileNameChars())
                v = v.Replace(c, '-');

            // ParseImageFileName()이 '_'를 구분자로 쓰므로, 값 내부 '_'는 치환
            v = v.Replace('_', '-').Trim();

            if (v.Length > 80) v = v.Substring(0, 80);
            return v;
        }

        // ✅ WCF 메시지에서 Lot/Serial 등 메타 갱신 (Part/Cam/Line/Station은 메시지에 없을 수도 있어 fallback 포함)
        private void UpdateWcfMetaFromMessage(object param)
        {
            // 기본값(fallback)
            string fallbackPart = "";
            if (CurModel != null && !string.IsNullOrWhiteSpace(CurModel.ModelPath))
                fallbackPart = Path.GetFileNameWithoutExtension(CurModel.ModelPath);

            string fallbackCam = SettingXml.Inst.MachineName; // 예: VISION02
            string fallbackLine = "0";
            string fallbackStation = "0";

            string lot = "";
            string serial = "";

            try
            {
                // WCF에서 넘어오는 타입: MessagingLibrary.Message
                if (param is MessagingLibrary.Message msg)
                {
                    lot = msg.LotNumber ?? "";
                    serial = msg.SerialID ?? "";
                }
            }
            catch { /* 무시 */ }

            lock (_wcfMetaLock)
            {
                // 프로그램 내부 history용(기존 코드가 _lotNumber/_serialID 사용)
                _lotNumber = lot;
                _serialID = serial;

                // 요청 파일명 구성용
                _wcfSerial = !string.IsNullOrWhiteSpace(serial) ? serial : "NA";

                // PART/CAM/LINE/ST 는 WCF 메시지에 필드가 없어서 기본값 사용(필요 시 나중에 확장 가능)
                _wcfPart = !string.IsNullOrWhiteSpace(_wcfPart) ? _wcfPart : fallbackPart;
                if (string.IsNullOrWhiteSpace(_wcfPart)) _wcfPart = fallbackPart;

                _wcfCam = fallbackCam;
                _wcfLine = fallbackLine;
                _wcfStation = fallbackStation;

                _pendingWcfSave = true;
            }
        }

        // ✅ Grab 완료 시점(TransferCompleted)에서 버퍼 이미지를 지정 파일명으로 저장하고 ResultForm 표시용 InspectImagePath도 세팅
        private void SaveWcfGrabImage(int bufferIndex)
        {
            try
            {
                string dir = GetWcfSaveDir();
                string fileName = BuildWcfFileName();
                string savePath = Path.Combine(dir, fileName);

                Mat curImage = GetMat(bufferIndex, eImageChannel.Color);
                if (curImage == null || curImage.Empty())
                {
                    // Color가 없으면 Gray라도 저장 시도
                    curImage = GetMat(bufferIndex, eImageChannel.Gray);
                }

                if (curImage == null || curImage.Empty())
                {
                    SLogger.Write("[WCF Save] No image in buffer", SLogger.LogType.Error);
                    return;
                }

                curImage.SaveImage(savePath);

                // ✅ ResultForm이 파일명에서 LOT/PART/SN/CAM/LINE/ST 파싱하도록 경로 주입
                if (CurModel != null)
                    CurModel.InspectImagePath = savePath;

                SLogger.Write($"[WCF Save] Saved: {savePath}", SLogger.LogType.Info);
            }
            catch (Exception ex)
            {
                SLogger.Write($"[WCF Save] Failed: {ex.Message}", SLogger.LogType.Error);
            }
            finally
            {
                lock (_wcfMetaLock) { _pendingWcfSave = false; }
            }
        }

        // 파일명에 들어갈 토큰 정리(정규식 파싱이 '_' 기준이므로 '_'는 제거)
        private string SafeToken(string v, string fallback = "NA")
        {
            if (string.IsNullOrWhiteSpace(v)) v = fallback;

            foreach (char c in Path.GetInvalidFileNameChars())
                v = v.Replace(c, '-');

            v = v.Replace('_', '-').Trim();
            return v;
        }

        private string Pad2(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "00";
            // 숫자만 남기기
            string digits = new string(s.Where(char.IsDigit).ToArray());
            if (digits.Length == 0) return "00";
            if (digits.Length == 1) return "0" + digits;
            return digits.Substring(digits.Length - 2, 2);
        }

        private string ExtractCamFromMachineName()
        {
            // 예: VISION02 -> "02"
            string machine = Setting.SettingXml.Inst.MachineName ?? "";
            string digits = new string(machine.Where(char.IsDigit).ToArray());
            if (digits.Length == 0) return WCF_DEFAULT_CAM;
            if (digits.Length == 1) return "0" + digits;
            return digits.Substring(digits.Length - 2, 2);
        }

        private string GetWcfSaveDir()
        {
            // ✅ 사용자가 설정한 ImageDir 우선, 없으면 모델폴더\WcfCapture
            string dir = Setting.SettingXml.Inst.ImageDir;

            if (string.IsNullOrWhiteSpace(dir))
            {
                string modelDir = "";
                if (CurModel != null && !string.IsNullOrWhiteSpace(CurModel.ModelPath))
                    modelDir = Path.GetDirectoryName(CurModel.ModelPath);

                dir = string.IsNullOrWhiteSpace(modelDir)
                    ? Path.Combine(AppContext.BaseDirectory, "WcfCapture")
                    : Path.Combine(modelDir, "WcfCapture");
            }

            Directory.CreateDirectory(dir);
            return dir;
        }

        // ✅ 요청 형식: LOT-현재날짜_PART-TRN001_SN-000031_CAM-01_LINE-02_ST-02.png
        private string BuildWcfFileName()
        {
            string lotDate = DateTime.Now.ToString("yyyyMMdd"); // "현재날짜"

            string part = WCF_DEFAULT_PART;
            string serial;
            lock (_wcfLock)
                serial = _wcfSerial;

            // serial이 숫자라면 6자리 0패딩(예: 31 -> 000031)
            string digits = new string((serial ?? "").Where(char.IsDigit).ToArray());
            if (!string.IsNullOrWhiteSpace(digits))
                serial = digits.PadLeft(6, '0');

            string cam = ExtractCamFromMachineName();
            string line = WCF_DEFAULT_LINE;
            string st = WCF_DEFAULT_STATION;

            part = SafeToken(part, WCF_DEFAULT_PART);
            serial = SafeToken(serial, "000000");
            cam = SafeToken(Pad2(cam), WCF_DEFAULT_CAM);
            line = SafeToken(Pad2(line), WCF_DEFAULT_LINE);
            st = SafeToken(Pad2(st), WCF_DEFAULT_STATION);

            return $"LOT-{lotDate}_PART-{part}_SN-{serial}_CAM-{cam}_LINE-{line}_ST-{st}.png";
        }

        // ✅ WCF InspStart 들어왔을 때 Serial 메타 저장 + (기존 통계용 lot/serial도 갱신)
        private void PrepareWcfSaveMeta(object param)
        {
            string serial = "";

            try
            {
                if (param is MessagingLibrary.Message msg)
                {
                    // control에서 SerialID를 준다고 가정(현재 VisionSequence가 InspStart에 e를 넘김)
                    serial = msg.SerialID ?? "";
                    // 통계/히스토리용 필드도 같이 갱신
                    _lotNumber = msg.LotNumber ?? "";
                    _serialID = msg.SerialID ?? "";
                }
            }
            catch { /* 무시 */ }

            lock (_wcfLock)
            {
                _wcfSerial = serial;
                _pendingWcfSave = true;
            }
        }

        // ✅ Grab 완료(TransferCompleted)에서 실제 저장 + InspectImagePath 세팅
        private void SaveWcfGrabImageAndSetInspectPath(int bufferIndex)
        {
            try
            {
                string dir = GetWcfSaveDir();
                string fileName = BuildWcfFileName();
                string savePath = Path.Combine(dir, fileName);

                Mat img = GetMat(bufferIndex, eImageChannel.Color);
                if (img == null || img.Empty())
                    img = GetMat(bufferIndex, eImageChannel.Gray);

                if (img == null || img.Empty())
                {
                    SLogger.Write("[WCF Save] Buffer image is empty", SLogger.LogType.Error);
                    return;
                }

                img.SaveImage(savePath);

                // ✅ ResultForm이 파일명 파싱하도록 InspectImagePath를 “저장된 파일”로 교체
                if (CurModel != null)
                    CurModel.InspectImagePath = savePath;

                SLogger.Write($"[WCF Save] Saved: {Path.GetFileName(savePath)}", SLogger.LogType.Info);
            }
            catch (Exception ex)
            {
                SLogger.Write($"[WCF Save] Failed: {ex.Message}", SLogger.LogType.Error);
            }
            finally
            {
                lock (_wcfLock) { _pendingWcfSave = false; }
            }
        }







        #region Disposable

        private bool disposed = false; // to detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.

                    VisionSequence.Inst.SeqCommand -= SeqCommand;

                    if (_grabManager != null)
                    {
                        _grabManager.Dispose();
                        _grabManager = null;
                    }

                    if (_saigeAI != null)
                    {
                        _saigeAI.Dispose();
                        _saigeAI = null;
                    }
                }

                //#16_LAST_MODELOPEN#4 registry 키를 닫습니다.
                _regKey.Close();

                // Dispose unmanaged managed resources.

                disposed = true;
            }
        }


        public void Dispose()
        {
            Dispose(true);
        }

        #endregion //Disposable
    }
}
