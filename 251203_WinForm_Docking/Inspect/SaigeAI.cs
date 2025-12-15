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
using SaigeVision.Net.V2;
using SaigeVision.Net.V2.Detection;
using SaigeVision.Net.V2.IAD;
using SaigeVision.Net.V2.Segmentation;

namespace _251203_WinForm_Docking
{
    public enum AIEngineType
    {
        [Description("Anomaly Detection")]
        AnomalyDetection = 0,
        [Description("Segmentation")]
        Segmentation,
        [Description("Detection")]
        Detection
    }
    public class SaigeAI : IDisposable
    {
        AIEngineType _engineType;
        IADEngine _iADEngine = null;
        IADResult _iADResult = null;
        SegmentationEngine _segEngine = null;
        SegmentationResult _segResult = null;
        DetectionEngine _detEngine = null;
        DetectionResult _detResult = null;

        Bitmap _inspImage = null;

        public SaigeAI()
        {
        }

        public void LoadEngine(string modelPath, AIEngineType engineType)
        {
            DisposeMode();

            _engineType = engineType;

            switch (_engineType)
            {
                case AIEngineType.AnomalyDetection:
                    LoadIADEngine(modelPath);
                    break;
                case AIEngineType.Segmentation:
                    LoadSegEngine(modelPath);
                    break;
                case AIEngineType.Detection:
                    LoadDetEngine(modelPath);
                    break;
                default:
                    throw new NotSupportedException("지원하지 않는 엔진 타입입니다.");
            }
        }

        public void LoadIADEngine(string modelPath)
        {
            _iADEngine = new IADEngine(modelPath, 0);

            IADOption option = _iADEngine.GetInferenceOption();

            option.CalcScoremap = false;
            option.CalcHeatmap = false;
            option.CalcMask = false;
            option.CalcObject = true;
            option.CalcObjectAreaAndApplyThreshold = true;
            option.CalcObjectScoreAndApplyThreshold = true;
            option.CalcTime = true;
            _iADEngine.SetInferenceOption(option);
        }

        public void LoadSegEngine(string modelPath)
        {
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

        public void LoadDetEngine(string modelPath)
        {
            _detEngine = new DetectionEngine(modelPath, 0);

            DetectionOption option = _detEngine.GetInferenceOption();

            option.CalcTime = true;
            _detEngine.SetInferenceOption(option);
        }


        // 입력된 이미지에서 IAD 검사 진행
        public bool InspAIModule(Bitmap bmpImage)
        {
            if (bmpImage is null)
            {
                MessageBox.Show("이미지가 없습니다. 유효한 이미지를 입력해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            _inspImage = bmpImage;

            SrImage srImage = new SrImage(bmpImage);

            Stopwatch sw = Stopwatch.StartNew();

            switch (_engineType)
            {
                case AIEngineType.AnomalyDetection:
                    if (_iADEngine == null)
                    {
                        MessageBox.Show("엔진이 초기화되지 않았습니다. LoadEngine 메서드를 호출하여 엔진을 초기화하세요.");
                        return false;
                    }

                    _iADResult = _iADEngine.Inspection(srImage);
                    break;
                case AIEngineType.Segmentation:
                    if (_segEngine == null)
                    {
                        MessageBox.Show("엔진이 초기화되지 않았습니다. LoadEngine 메서드를 호출하여 엔진을 초기화하세요.");
                        return false;
                    }
                    _segResult = _segEngine.Inspection(srImage);
                    break;
                case AIEngineType.Detection:
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
        private void DrawSegResult(SegmentedObject[] segmentedObjects, Bitmap bmp)
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
        private void DrawDetectionResult(DetectionResult result, Bitmap bmp)
        {
            Graphics g = Graphics.FromImage(bmp);
            int step = 10;

            // outline contour
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
            if (_inspImage is null)
                return null;

            Bitmap resultImage = _inspImage.Clone(new Rectangle(0, 0, _inspImage.Width, _inspImage.Height), System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            switch (_engineType)
            {
                case AIEngineType.AnomalyDetection:
                    if (_iADResult == null)
                        return resultImage;
                    DrawSegResult(_iADResult.SegmentedObjects, resultImage);
                    break;
                case AIEngineType.Segmentation:
                    if (_segResult == null)
                        return resultImage;
                    DrawSegResult(_segResult.SegmentedObjects, resultImage);
                    break;
                case AIEngineType.Detection:
                    if (_detResult == null)
                        return resultImage;
                    DrawDetectionResult(_detResult, resultImage);
                    break;
            }

            return resultImage;
        }

        private void DisposeMode()
        {
            if (_iADEngine != null)
                _iADEngine.Dispose();

            if (_segEngine != null)
                _segEngine.Dispose();

            if (_detEngine != null)
                _detEngine.Dispose();
        }

        #region Disposable

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

        #endregion
    }
}
