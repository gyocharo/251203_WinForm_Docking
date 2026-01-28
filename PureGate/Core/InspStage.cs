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

        private string _loadedImageDir = "";

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


        public bool Initialize()
        {
            LoadSetting();

            SLogger.Write("InspStage 초기화!");
            _imageSpace = new ImageSpace();

            _previewImage = new PreviewImage();

            //#15_INSP_WORKER#7 InspWorker 인스턴스 생성
            _inspWorker = new InspWorker();
            _imageLoader = new ImageLoader();

            //#16_LAST_MODELOPEN#2 REGISTRY 키 생성
            _regKey = Registry.CurrentUser.CreateSubKey("Software\\WinDocking");

            _model = new Model();

            LoadSetting();

            switch (_camType)
            {
                case CameraType.WebCam:
                    {
                        _grabManager = new WebCam();
                        break;
                    }
                case CameraType.HikRobot:
                    {
                        _grabManager = new HikRobotCam();
                        break;
                    }
            }

            if (_grabManager != null &&_grabManager.InitGrab() == true)
            {
                _grabManager.TransferCompleted += _multiGrab_TransferCompleted;

                InitModelGrab(MAX_GRAB_BUF);
            }

            VisionSequence.Inst.InitSequence();
            VisionSequence.Inst.SeqCommand += SeqCommand;

            //#16_LAST_MODELOPEN#5 마지막 모델 열기 여부 확인
            if (!LastestModelOpen())
            {
                MessageBox.Show("모델 열기 실패!");
            }

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
            if(_selectedInspWindow is null)
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
            InspWorker.TryInspect(inspWindow, InspectType.InspNone);
        }

        public void SelectInspWindow(InspWindow inspWindow)
        {
            _selectedInspWindow = inspWindow;

            var propForm = MainForm.GetDockForm<PropertiesForm>();
            if(propForm != null)
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
            if(cameraForm != null)
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
            if(cameraForm != null)
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
            if(cameraForm != null)
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
            if(cameraForm != null)
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
            }

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
        }

        public bool LastestModelOpen()
        {
            if (_lastestModelOpen)
                return true;

            _lastestModelOpen = true;

            string lastestModel = (string)_regKey.GetValue("LastestModelPath");
            if (File.Exists(lastestModel) == false)
                return true;

            DialogResult result = MessageBox.Show($"최근 모델을 로딩할까요?\r\n{lastestModel}", "Question", MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
                return true;

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

                // ✅ 폴더가 바뀌었으면 무조건 다시 로드
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


                        TrySaveClsResultImage(resultImage);

                        try
                        {
                            bool ok = true;                 // 기본 OK 처리(최소한 카운트는 증가)
                            string label = "";
                            float score = 0;

                            var modelInfo = AIModule.GetModelInfo();

                            // 1) 원래 방식: Top1 가져오기
                            if (AIModule.TryGetLastClsTop1(out label, out score) && !string.IsNullOrWhiteSpace(label))
                            {
                                // ✅ (중요) label 정규화: 공백/개행 제거 + "(점수)" 같은 꼬리 제거
                                string rawLabel = label;
                                label = label.Trim();

                                int cutIdx = label.IndexOf('(');           // "cut_lead (85.0)" 케이스
                                if (cutIdx >= 0) label = label.Substring(0, cutIdx).Trim();

                                cutIdx = label.IndexOf(' ');               // "cut_lead 85.0" 같이 공백으로 붙는 케이스
                                if (cutIdx >= 0) label = label.Substring(0, cutIdx).Trim();

                                bool isNg = false;

                                if (modelInfo != null && modelInfo.ClassInfos != null && modelInfo.ClassIsNG != null)
                                {
                                    // 1차: 완전일치
                                    int idx = Array.FindIndex(modelInfo.ClassInfos,
                                        c => string.Equals(c.Name?.Trim(), label, StringComparison.OrdinalIgnoreCase));

                                    // 2차: 혹시라도 label이 좀 더 길게 들어오면 StartsWith로 한번 더
                                    if (idx < 0)
                                    {
                                        idx = Array.FindIndex(modelInfo.ClassInfos,
                                            c => (label ?? "").StartsWith(c.Name?.Trim() ?? "", StringComparison.OrdinalIgnoreCase));
                                    }

                                    if (idx >= 0 && idx < modelInfo.ClassIsNG.Length)
                                        isNg = modelInfo.ClassIsNG[idx];

                                    // 🔎 디버그 로그 (이거 꼭 남겨)
                                    SLogger.Write($"[CLS] raw='{rawLabel}' -> norm='{label}', idx={idx}, isNg={isNg}, score={score:0.0}");
                                }
                                else
                                {
                                    // modelInfo가 없으면 fallback
                                    isNg = !string.Equals(label, "Good", StringComparison.OrdinalIgnoreCase);
                                    SLogger.Write($"[CLS] modelInfo null -> label='{label}', isNg={isNg}, score={score:0.0}");
                                }

                                ok = !isNg;
                            }
                            else
                            {
                                label = "Unknown";
                                ok = true; // Unknown을 NG로 잡고 싶으면 false로 바꿔
                                SLogger.Write("[CLS] Top1 FAIL -> Unknown");
                            }

                            // --- 기존 History 저장 로직 ---
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

                            // ✅ 통계 UI 갱신 (무조건 호출)
                            var details = new List<NgClassCount>();
                            if (!ok && !string.IsNullOrWhiteSpace(label) && label != "Unknown")
                                details.Add(new NgClassCount { ClassName = label, Count = 1 });

                            MainForm.Instance?.UpdateStatisticsUI(ok ? 1 : 0, ok ? 0 : 1, details);
                            PushDonutStatsAndUpdateUI(ok, ok ? "" : label); //ROI 없이 CLS만 돌 때도 도넛이 OK + 클래스별로 쌓여요.
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

                if (string.Equals(label, "Good", StringComparison.OrdinalIgnoreCase))
                {
                    root = @"D:\Good";
                    targetDir = Path.Combine(root, dateFolder);
                }
                else
                {
                    root = @"D:\NG";
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

                        string errMsg;

                        if(UseCamera)
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
            // 검사 시작 전 상태 초기화
            ResetDisplay();

            bool isDefect = false;
            // 실제 AI 검사가 실행되는 구간
            if (!_inspWorker.RunInspect(out isDefect))
            {
                SLogger.Write("Failed to inspect", SLogger.LogType.Error);
            }

            // ✅ 핵심: 검사가 끝나자마자 결과 UI 업데이트 호출
            // 결함이 없으면(false) -> OK(true)를 UI에 보냄
            UpdateResultUI(!isDefect);

            // ✅ ROI 검사도 1검사=1레코드로 저장
            try
            {
                string modelName = "";
                if (CurModel != null && !string.IsNullOrWhiteSpace(CurModel.ModelPath))
                    modelName = Path.GetFileNameWithoutExtension(CurModel.ModelPath);

                bool ok = !isDefect;

                // 지금은 원인 클래스가 없으니 임시값
                string ngClass = ok ? "" : "ROI_NG";

                InspHistoryRepo.Append(new InspHistoryRecord
                {
                    Time = DateTime.Now,
                    ModelName = modelName,
                    LotNumber = _lotNumber ?? "",
                    SerialID = _serialID ?? "",
                    Total = 1,
                    Ok = ok ? 1 : 0,
                    Ng = ok ? 0 : 1,
                    NgClass = ngClass
                });
            }
            catch (Exception ex)
            {
                SLogger.Write($"[History Save] Failed: {ex.Message}", SLogger.LogType.Error);
            }

            // 제어기로 결과 전송
            VisionSequence.Inst.VisionCommand(Vision2Mmi.InspDone, isDefect);
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
                MessageBox.Show("열려진 모델이 없습니다!");
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
            MainForm.Instance?.UpdateStatisticsUI(_okCount, _ngCount, donutList);
            System.Diagnostics.Debug.WriteLine("[DONUT_KEYS] " + string.Join(" | ", _donutStats.Keys.Select(k => $"'{k}'")));
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
