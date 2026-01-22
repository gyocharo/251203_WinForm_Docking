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
using PureGate.Property;
using SaigeVision.Net.V2;
using SaigeVision.Net.V2.Classification;
using SaigeVision.Net.V2.Detection;
using SaigeVision.Net.V2.IAD;
using SaigeVision.Net.V2.IEN;
using SaigeVision.Net.V2.OCR;
using SaigeVision.Net.V2.Segmentation;

namespace PureGate
{
    public enum EngineType
    {
        SEG = 0,
        DET = 1,
        IAD = 2,
        CLS = 3,
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
        ClassificationEngine _clsEngine = null;
        ClassificationResult _clsResult = null;


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
                case EngineType.CLS:
                    RunCLS(modelPath);
                    break;
                default:
                    throw new NotSupportedException("지원하지 않는 엔진 타입입니다.");
            }
        }

        public bool IsEngineLoaded
        {
            get
            {
                switch (_engineType)
                {
                    case EngineType.IAD:
                        return _iadEngine != null;
                    case EngineType.SEG:
                        return _segEngine != null;
                    case EngineType.DET:
                        return _detEngine != null;
                    case EngineType.CLS:
                        return _clsEngine != null;
                    default:
                        return false;
                }
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

        public void RunCLS(string txt)
        {
            DisposeMode();
            string modelPath = Path.GetFullPath(txt);

            _clsEngine = new ClassificationEngine(modelPath, 0);

            ClassificationOption option = _clsEngine.GetInferenceOption();
            option.CalcTime = true;

            _clsEngine.SetInferenceOption(option);
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
                case EngineType.CLS: // Classification
                    if (_clsEngine == null)
                    {
                        MessageBox.Show("엔진이 초기화되지 않았습니다. LoadEngine 메서드를 호출하여 엔진을 초기화하세요.");
                        return false;
                    }
                    _clsResult = _clsEngine.Inspection(srImage);
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

            int size = 0; // 기본값
            var text = AIModuleProp.saigeaiprop?.txt_Area?.Text?.Trim();

            if (!int.TryParse(text, out size))
                size = 0;

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
                case EngineType.CLS: // classification
                    if (_clsResult == null) return resultImage;
                    DrawCLSResultOverlay(_clsResult, resultImage);
                    break;
            }

            return resultImage;
        }

        private void DrawCLSResultOverlay(object clsResultObj, Bitmap bmp)
        {
            try
            {
                string label;
                float score;
                bool ok = TryGetClassificationTop1(clsResultObj, out label, out score);

                string text = ok
                    ? $"CLS: {label} ({score:0.000})"
                    : "CLS: (result parsed failed)";

                using (Graphics g = Graphics.FromImage(bmp))
                using (Font font = new Font("Arial", 24, FontStyle.Bold, GraphicsUnit.Pixel))
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                using (SolidBrush fg = new SolidBrush(Color.White))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    SizeF sz = g.MeasureString(text, font);
                    RectangleF rect = new RectangleF(10, 10, sz.Width + 20, sz.Height + 16);

                    g.FillRectangle(bg, rect);
                    g.DrawString(text, font, fg, 20, 18);
                }
            }
            catch
            {
                // 결과 표시 실패해도 다른 기능에 영향 없게 무시
            }
        }

        // ✅ SaigeVision ClassificationResult 구조가 버전에 따라 다를 수 있어 Reflection으로 최대한 안전하게 Top1 추출
        private bool TryGetClassificationTop1(object result, out string label, out float score)
        {
            label = null;
            score = 0f;

            if (result == null)
                return false;

            // ✅ 1) 강타입(정확한 프로퍼티명) 우선
            if (result is ClassificationResult cr)
                return TryGetClassificationTop1Strong(cr, out label, out score);

            // ✅ 2) 강타입이 아니면(참조 어셈블리 mismatch 등) 리플렉션 fallback
            return TryGetClassificationTop1ByReflection(result, out label, out score);
        }

        private bool TryGetClassificationTop1ByReflection(object result, out string label, out float score)
        {
            label = null;
            score = 0f;

            if (result == null)
                return false;

            var t = result.GetType();
            var props = t.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            object TryGetProp(string name)
                => props.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.GetValue(result);

            // ✅ DLL 문자열 기반으로 가장 유력한 후보를 우선 시도
            // BestScoreClassInfo / ClassScoreInfos / ClassInfo / Name / Score
            foreach (var cand in new[] { "BestScoreClassInfo", "Top1", "Best", "BestClass", "TopClass", "ClassInfo", "Class" })
            {
                var v = TryGetProp(cand);
                if (v != null)
                {
                    if (TryExtractLabelScoreFromObject(v, out label, out score))
                        return true;

                    if (v is string s && !string.IsNullOrWhiteSpace(s))
                    {
                        label = s;
                        score = 0f;
                        return true;
                    }
                }
            }

            // IEnumerable(ClassScoreInfos 등)에서 첫 요소/최대값 시도
            foreach (var p in props)
            {
                if (p.PropertyType == typeof(string)) continue;

                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType))
                {
                    var enumerable = p.GetValue(result) as System.Collections.IEnumerable;
                    if (enumerable == null) continue;

                    // 최대 스코어 찾기(가능하면)
                    object bestItem = null;
                    float bestScore = float.MinValue;

                    foreach (var item in enumerable)
                    {
                        if (item == null) continue;

                        if (TryExtractLabelScoreFromObject(item, out var l, out var sc))
                        {
                            if (sc > bestScore)
                            {
                                bestScore = sc;
                                bestItem = item;
                                label = l;
                                score = sc;
                            }
                        }
                    }

                    if (bestItem != null && !string.IsNullOrWhiteSpace(label))
                        return true;
                }
            }

            return false;
        }

        private bool TryGetClassificationTop1Strong(ClassificationResult cr, out string label, out float score)
        {
            label = null;
            score = 0f;

            if (cr == null)
                return false;

            // 1) DLL에 실제 존재: BestScoreClassInfo
            ClassScoreInfo best = cr.BestScoreClassInfo;

            if (best != null)
            {
                label = best.ClassInfo?.Name;   // ✅ DLL 문자열에 <Name>k__BackingField / get_Name 존재
                score = best.Score;             // ✅ get_Score 존재

                if (!string.IsNullOrWhiteSpace(label))
                    return true;
            }

            // 2) 없으면 ClassScoreInfos 에서 최대 Score 찾기
            var list = cr.ClassScoreInfos;      // ✅ get_ClassScoreInfos 존재
            if (list != null && list.Count() > 0)
            {
                best = list.OrderByDescending(x => x?.Score ?? float.MinValue).FirstOrDefault();

                if (best != null)
                {
                    label = best.ClassInfo?.Name;
                    score = best.Score;

                    if (!string.IsNullOrWhiteSpace(label))
                        return true;
                }
            }

            // 3) 마지막 보정: 라벨이 비었으면 클래스 인덱스/기타 정보가 있는지 (버전에 따라 다름)
            // 여기서는 강타입으로 보이는 정보가 "Name" 중심이라, 비면 실패 처리하고 fallback로 넘김
            return false;
        }

        private bool TryExtractLabelScoreFromObject(object obj, out string label, out float score)
        {
            label = null;
            score = 0f;

            if (obj == null) return false;

            // obj 안에 ClassInfo + Score 조합이 있을 수 있음
            var t = obj.GetType();
            var props = t.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            object Get(string name)
                => props.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.GetValue(obj);

            // 1) ClassInfo 내부에서 Name/Label 찾기
            var classInfo = Get("ClassInfo");
            if (classInfo != null)
            {
                var ciT = classInfo.GetType();
                var ciProps = ciT.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                object GetCI(string name)
                    => ciProps.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.GetValue(classInfo);

                label = (GetCI("Name") as string) ?? (GetCI("Label") as string) ?? (GetCI("ClassName") as string);

                // Score는 obj쪽일 수도 classInfo쪽일 수도
                if (!TryGetFloat(Get("Score"), out score))
                    TryGetFloat(GetCI("Score"), out score);

                if (!string.IsNullOrWhiteSpace(label))
                    return true;
            }

            // 2) obj 자체에 Label/Name/Score가 있는 케이스
            label = (Get("Name") as string) ?? (Get("Label") as string) ?? (Get("ClassName") as string);
            if (!TryGetFloat(Get("Score"), out score))
                TryGetFloat(Get("Confidence"), out score);

            if (!string.IsNullOrWhiteSpace(label))
                return true;

            return false;
        }

        private bool TryGetFloat(object v, out float f)
{
    f = 0f;
    if (v == null) return false;

    if (v is float ff) { f = ff; return true; }
    if (v is double dd) { f = (float)dd; return true; }
    if (v is int ii) { f = ii; return true; }
    if (v is long ll) { f = ll; return true; }

    if (float.TryParse(v.ToString(), out f))
        return true;

    return false;
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
            if(_clsEngine != null)
            {
                _clsEngine.Dispose();
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
