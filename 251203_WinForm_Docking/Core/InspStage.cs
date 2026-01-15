using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Deployment.Application;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using _251203_WinForm_Docking.Algorithm;
using _251203_WinForm_Docking.Grab;
using _251203_WinForm_Docking.Setting;
using _251203_WinForm_Docking.Teach;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace _251203_WinForm_Docking.Core
{
    public class InspStage : IDisposable
    {
        SaigeAI _saigeAI;

        public static readonly int MAX_GRAB_BUF = 5;

        private ImageSpace _imageSpace = null;
        //private HikRobotCam _grabManager = null;
        private GrabModel _grabManager = null;
        private CameraType _camType = CameraType.WebCam;

        BlobAlgorithm _blobAlgorithm = null;
        private PreviewImage _previewImage = null;

        private Model _model = null;

        private InspWindow _selectedInspWindow = null;

        public InspStage() { }

        public SaigeAI AIModule
        {
            get
            {
                if (_saigeAI is null)
                    _saigeAI = new SaigeAI();
                return _saigeAI;
            }
        }

        public BlobAlgorithm BlobAlgorithm
        {
            get => _blobAlgorithm;
        }

        public PreviewImage PreView
        {
            get => _previewImage;
        }

        public Model CurModel
        {
            get => _model;
        }

        public ImageSpace ImageSpace
        {
            get => _imageSpace;
        }

        public bool LiveMode { get; set; } = false;


        public bool Initialize()
        {
            _imageSpace = new ImageSpace();

            _blobAlgorithm = new BlobAlgorithm();
            _previewImage = new PreviewImage();

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

            //UpdateProperty();

        }

        private void UpdateProperty(InspWindow inspWindow)
        {
            if (BlobAlgorithm is null)
                return;

            PropertiesForm propertiesForm = MainForm.GetDockForm<PropertiesForm>();
            if (propertiesForm is null)
                return;

            propertiesForm.UpdateProperty(inspWindow);
        }

        public void SetBuffer(int bufferCount)
        {
            if (_grabManager == null)
                return;

            if (_imageSpace.BufferCount == bufferCount)
                return;

            _imageSpace.InitImageSpace(bufferCount);
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

        public void TryInspection(InspWindow inspWindow = null)
        {
            if(inspWindow is null)
            {
                if (_selectedInspWindow is null)
                    return;

                inspWindow = _selectedInspWindow;
            }

            UpdateDiagramEntity();

            List<DrawInspectInfo> totalArea = new List<DrawInspectInfo> ();

            Rect windowArea = inspWindow.WindowArea;

            foreach(var inspAlgo in inspWindow.AlgorithmList)
            {
                inspAlgo.TeachRect = windowArea;
                inspAlgo.InspRect = windowArea;

                InspectType inspType = inspAlgo.InspectType;

                switch (inspType)
                {
                    case InspectType.InspBinary:
                        {
                            BlobAlgorithm blobAlgo = (BlobAlgorithm)inspAlgo;

                            Mat srcImage = Global.Inst.InspStage.GetMat();
                            blobAlgo.SetInspData(srcImage);

                            if (blobAlgo.DoInspect())
                            {
                                List<DrawInspectInfo> resultArea = new List<DrawInspectInfo> ();
                                int resultCnt = blobAlgo.GetResultRect(out resultArea);
                                if(resultCnt > 0)
                                {
                                    totalArea.AddRange(resultArea);
                                }
                            }
                            break;
                        }
                }
                if (inspAlgo.DoInspect())
                {
                    List<DrawInspectInfo> resultArea = new List<DrawInspectInfo>();
                    int resultCnt = inspAlgo.GetResultRect (out resultArea);
                    if(resultCnt > 0)
                    {
                        totalArea.AddRange(resultArea);
                    }
                }
            }

            if(totalArea.Count > 0)
            {
                var cameraForm = MainForm.GetDockForm<CameraForm>();
                if(cameraForm != null)
                {
                    cameraForm.AddRect(totalArea);
                }
            }
        }

        public void SelectInspWindow(InspWindow inspWindow)
        {
            _selectedInspWindow = inspWindow;

            var propForm = MainForm.GetDockForm<PropertiesForm>();
            if(propForm != null)
            {
                if(inspWindow is null){
                    propForm.ResetProperty();
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

        //#MODEL#11 InspWindow 삭제하기
        public void DelInspWindow(InspWindow inspWindow)
        {
            _model.DelInpWindow(inspWindow);
            UpdateDiagramEntity();
        }


        public void DelInspWindow(List<InspWindow> inspWindowList)
        {
            _model.DelInspWindowList(inspWindowList);
            UpdateDiagramEntity();
        }

        public void UpdateDiagramEntity()
        {
            CameraForm cameraForm = MainForm.GetDockForm<CameraForm>();
            if(cameraForm != null)
            {
                cameraForm.UpdateDiagramEntity();
            }

            ModelTreeForm modelTreeForm = MainForm.GetDockForm<ModelTreeForm>();
            if(modelTreeForm != null)
            {
                modelTreeForm.UpdateDiagramEntity();
            }
        }

        private bool DisplayResult()
        {
            if (_blobAlgorithm is null)
                return false;

            List<DrawInspectInfo> resultArea = new List<DrawInspectInfo>();
            int resultCnt = _blobAlgorithm.GetResultRect(out resultArea);
            if (resultCnt > 0)
            {
                //찾은 위치를 이미지상에서 표시
                var cameraForm = MainForm.GetDockForm<CameraForm>();
                if (cameraForm != null)
                {
                    cameraForm.ResetDisplay();
                    cameraForm.AddRect(resultArea);
                }
            }

            return true;
        }


        public void Grab(int bufferIndex)
        {
            if (_grabManager == null)
                return;

            _grabManager.Grab(bufferIndex, true);
        }

        //영상 취득 완료 이벤트 발생시 후처리
        private async void _multiGrab_TransferCompleted(object sender, object e)
        {
            int bufferIndex = (int)e;
            Console.WriteLine($"_multiGrab_TransferCompleted {bufferIndex}");

            _imageSpace.Split(bufferIndex);

            DisplayGrabImage(bufferIndex);

            if (_previewImage != null)
            {
                Bitmap bitmap = ImageSpace.GetBitmap(0);
                _previewImage.SetImage(BitmapConverter.ToMat(bitmap));
            }

            if (LiveMode)
            {
                await Task.Delay(100);
                _grabManager.Grab(bufferIndex, true);
            }
        }

        private void DisplayGrabImage(int bufferIndex)
        {
            var cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.UpdateDisplay();
            }
        }

        public void UpdateDisplay(Bitmap bitmap)
        {
            var cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.UpdateDisplay(bitmap);
            }
        }

        public Bitmap GetCurrentImage()
        {
            Bitmap bitmap = null;
            var cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                bitmap = cameraForm.GetDisplayImage();
            }

            return bitmap;
        }

        public Bitmap GetBitmap(int bufferIndex = -1)
        {
            if (Global.Inst.InspStage.ImageSpace is null)
                return null;

            return Global.Inst.InspStage.ImageSpace.GetBitmap();
        }

        public Mat GetMat()
        {
            return Global.Inst.InspStage.ImageSpace.GetMat();
        }

        public void RedrawMainView()
        {
            CameraForm cameraForm = MainForm.GetDockForm<CameraForm>();
            if(cameraForm != null)
            {
                cameraForm.UpdateImageViewer();
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
