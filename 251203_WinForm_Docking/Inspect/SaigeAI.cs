using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using _251203_WinForm_Docking.Property;
using SaigeVision.Net.V2;
using SaigeVision.Net.V2.Classification;
using SaigeVision.Net.V2.Detection;
using SaigeVision.Net.V2.IAD;
using SaigeVision.Net.V2.IEN;
using SaigeVision.Net.V2.OCR;
using SaigeVision.Net.V2.Segmentation;

namespace _251203_WinForm_Docking
{
    public enum EngineType
    {
        SEG = 0,
        DET = 1,
        IAD = 2,
    }

    public class SaigeAI : IDisposable
    {
        EngineType _engineType;
        SegmentationEngine _segEngine = null;
        SegmentationResult _segResult = null;
        DetectionEngine _detEngine = null;
        DetectionResult _detResult = null;
        IADEngine _iadEngine = null;
        IADResult _iadResult = null;

        Bitmap _bitmap = null;


        public SaigeAI()
        {

        }

        public void LoadEngine(string modelPath, EngineType engineType)
        {
            DisposeMode();

            _engineType = engineType;

            switch (_engineType)
            {
                case EngineType.IAD:
                    RunIAD(modelPath);
                    break;
                case EngineType.SEG:
                    RunSEG(modelPath);
                    break;
                case EngineType.DET:
                    RunDET(modelPath);
                    break;
                default:
                    throw new NotSupportedException("지원하지 않는 엔진 타입입니다.");
            }
        }
        public void RunSEG(string txt)
        {
            DisposeMode();
            string modelPath = Path.GetFullPath(txt);

            _segEngine = new SegmentationEngine(modelPath, 0);

            SegmentationOption option = _segEngine.GetInferenceOption();

            option.CalcTime = true;
            option.CalcObject = true;
            option.CalcScoremap = false;
            option.CalcMask = false;
            option.CalcObjectAreaAndApplyThreshold = true;
            option.CalcObjectScoreAndApplyThreshold = true;
            option.OversizedImageHandling = OverSizeImageFlags.do_not_inspect;

            _segEngine.SetInferenceOption(option);
        }

        public void RunDET(string txt)
        {
            DisposeMode();
            string modelPath = Path.GetFullPath(txt);

            _detEngine = new DetectionEngine(modelPath, 0);

            DetectionOption option = _detEngine.GetInferenceOption();

            option.CalcTime = true;

            _detEngine.SetInferenceOption(option);
        }

        public void RunIAD(string txt)
        {
            DisposeMode();
            string modelPath = Path.GetFullPath(txt);

            _iadEngine = new IADEngine(modelPath, 0);

            IADOption option = _iadEngine.GetInferenceOption();

            option.CalcScoremap = false;
            option.CalcHeatmap = false;
            option.CalcMask = false;
            option.CalcObject = true;
            option.CalcObjectAreaAndApplyThreshold = true;
            option.CalcObjectScoreAndApplyThreshold = true;
            option.CalcTime = true;
            _iadEngine.SetInferenceOption(option);

        }


        

        public bool InspAIModule(Bitmap bmpImage)
        {
            if (bmpImage is null)
            {
                MessageBox.Show("이미지가 없습니다. 유효한 이미지를 입력해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            _bitmap = bmpImage;

            SrImage srImage = new SrImage(bmpImage);

            Stopwatch sw = Stopwatch.StartNew();

            switch (_engineType)
            {
                case EngineType.IAD:
                    if (_iadEngine == null)
                    {
                        MessageBox.Show("엔진이 초기화되지 않았습니다. LoadEngine 메서드를 호출하여 엔진을 초기화하세요.");
                        return false;
                    }
                    _iadResult = _iadEngine.Inspection(srImage);
                    break;
                case EngineType.SEG:
                    if (_segEngine == null)
                    {
                        MessageBox.Show("엔진이 초기화되지 않았습니다. LoadEngine 메서드를 호출하여 엔진을 초기화하세요.");
                        return false;
                    }
                    _segResult = _segEngine.Inspection(srImage);
                    break;
                case EngineType.DET:
                    if (_detEngine == null)
                    {
                        MessageBox.Show("엔진이 초기화되지 않았습니다. LoadEngine 메서드를 호출하여 엔진을 초기화하세요.");
                        return false;
                    }
                    _detResult = _detEngine.Inspection(srImage);
                    break;
            }

            sw.Stop();

            return true;
        }

        private void DrawSEGResult(SegmentedObject[] segmentedObjects, Bitmap bmp, int size)
        {
            Graphics g = Graphics.FromImage(bmp);
            int step = 10;

            foreach (var prediction in segmentedObjects)
            {
                SolidBrush brush = new SolidBrush(Color.FromArgb(127, prediction.ClassInfo.Color));
                using (GraphicsPath gp = new GraphicsPath())
                {
                    if (prediction.Contour.Value.Count < 3) continue;
                    gp.AddPolygon(prediction.Contour.Value.ToArray());
                    foreach (var innerValue in prediction.Contour.InnerValue)
                    {
                        if(prediction.Area > size)
                        {
                            gp.AddPolygon(innerValue.ToArray());
                        }
                        
                    }
                    g.FillPath(brush, gp);
                }
                step += 50;
            }
        }

        private void DrawIADResult(SegmentedObject[] segmentedObjects, Bitmap bmp)
        {
            Graphics g = Graphics.FromImage(bmp);
            int step = 10;

            foreach (var prediction in segmentedObjects)
            {
                SolidBrush brush = new SolidBrush(Color.FromArgb(127, prediction.ClassInfo.Color));
                using (GraphicsPath gp = new GraphicsPath())
                {
                    if (prediction.Contour.Value.Count < 3) continue;
                    gp.AddPolygon(prediction.Contour.Value.ToArray());
                    foreach (var innerValue in prediction.Contour.InnerValue)
                    {
                        gp.AddPolygon(innerValue.ToArray());
                    }
                    g.FillPath(brush, gp);
                }
                step += 50;
            }
        }
        private void DrawDETResult(DetectionResult result, Bitmap bmp)
        {
            Graphics g = Graphics.FromImage(bmp);
            int step = 10;

            foreach (var prediction in result.DetectedObjects)
            {
                SolidBrush brush = new SolidBrush(Color.FromArgb(127, prediction.ClassInfo.Color));
                using (GraphicsPath gp = new GraphicsPath())
                {
                    float x = (float)prediction.BoundingBox.X;
                    float y = (float)prediction.BoundingBox.Y;
                    float width = (float)prediction.BoundingBox.Width;
                    float height = (float)prediction.BoundingBox.Height;
                    gp.AddRectangle(new RectangleF(x, y, width, height));
                    g.DrawPath(new Pen(brush, 10), gp);
                }
                step += 50;
            }
        }

        public Bitmap GetResultImage()
        {
            if (_bitmap is null)
                return null;

            Bitmap resultImage = _bitmap.Clone(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height), System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            int size = int.Parse(AIModuleProp.saigeaiprop.txt_Area.Text);

            switch (_engineType)
            {
                case EngineType.IAD:
                    if (_iadResult == null)
                        return resultImage;
                    DrawIADResult(_iadResult.SegmentedObjects, resultImage);
                    break;
                case EngineType.SEG:
                    if (_segResult == null)
                        return resultImage;
                    DrawSEGResult(_segResult.SegmentedObjects, resultImage, size);
                    break;
                case EngineType.DET:
                    if (_detResult == null)
                        return resultImage;
                    DrawDETResult(_detResult, resultImage);
                    break;
            }

            return resultImage;
        }

        private void DisposeMode()
        {
            if (_segEngine != null)
            {
                _segEngine.Dispose();
            }
            if (_detEngine != null)
            {
                _detEngine.Dispose();
            }
            if (_iadEngine != null)
            {
                _iadEngine.Dispose();
            }
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    DisposeMode();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
