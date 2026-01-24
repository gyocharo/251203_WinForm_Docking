using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
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

    public enum AIEngineType
    {
        SEG = 0,
        DET = 1,
        IAD = 2,
        CLS = 3,
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
        IADEngine _iadEngine = null;
        IADResult _iadResult = null;
        ClassificationEngine _clsEngine = null;
        ClassificationResult _clsResult = null;


        Bitmap _inspImage = null;

        private string _lastClsLabel = null;
        private float _lastClsScore = 0f;


        public SaigeAI()
        {

        }

        // 엔진을 로드하는 메서드입니다.
        public void LoadEngine(string modelPath, AIEngineType engineType)
        {
            //GPU에 여러개 모델을 넣을 경우, 메모리가 부족할 수 있으므로, 해제
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
            // 검사하기 위한 엔진에 대한 객체를 생성합니다.
            // 인스턴스 생성 시 모데파일 정보와 GPU Index를 입력해줍니다.
            // 필요에 따라 batch size를 입력합니다
            _iADEngine = new IADEngine(modelPath, 0);

            // 검사 전 option에 대한 설정을 가져옵니다
            IADOption option = _iADEngine.GetInferenceOption();

            option.CalcScoremap = false;

            // 검사 결과에 대한 heatmap 이미지를 가져올 지 선택합니다
            // 약간의 속도차이로 불필요할 경우 false 로 설정합니다
            option.CalcHeatmap = false;

            // 검사 결과에 대한 mask이미지를 가져올 지 선택합니다
            // 약간의 속도차이로 불필요할 경우 false 로 설정합니다
            option.CalcMask = false;

            // 검사 결과에 대한 segmencted object (contour) 에 대한 정보를 가져올 지 선택합니다
            // 약간의 속도차이로 불필요할 경우 false 로 설정합니다
            option.CalcObject = true;

            // Segmented object의 면적이 object area threshold 보다 작으면 최종 결과에서 제외됩니다.
            option.CalcObjectAreaAndApplyThreshold = true;

            // Segmented object의 면적이 object score threshold 보다 작으면 최종 결과에서 제외됩니다.
            option.CalcObjectScoreAndApplyThreshold = true;

            // 추론 API 실행에 소요되는 시간을 세분화하여 출력할지 결정합니다.
            // `true`로 설정하면 이미지를 읽는 시간, 순수 딥러닝 추론 시간, 후처리 시간을 각각 확인할 수 있습니다.
            // `false`로 설정하면 추론 API 실행에 소요된 총 시간만을 확인할 수 있습니다.
            // `true`로 설정하면 전체 추론 시간이 느려질 수 있습니다. 실제 검사 시에는 `false`로 설정하는 것을 권장합니다.
            option.CalcTime = true;

            // option을 적용하여 검사에 대한 조건을 변경할 수 있습니다.
            // 필요에 따라 writeModelFile parameter를 이용하여 모델파일에 정보를 영구적으로 변경할 수 있습니다.
            _iADEngine.SetInferenceOption(option);
        }

        public void LoadSegEngine(string modelPath)
        {
            // 검사하기 위한 엔진에 대한 객체를 생성합니다.
            // 인스턴스 생성 시 모데파일 정보와 GPU Index를 입력해줍니다.
            // 필요에 따라 batch size를 입력합니다
            _segEngine = new SegmentationEngine(modelPath, 0);

            // 검사 전 option에 대한 설정을 가져옵니다
            SegmentationOption option = _segEngine.GetInferenceOption();

            /// 추론 API 실행에 소요되는 시간을 세분화하여 출력할지 결정합니다.
            /// `true`로 설정하면 이미지를 읽는 시간, 순수 딥러닝 추론 시간, 후처리 시간을 각각 확인할 수 있습니다.
            /// `false`로 설정하면 추론 API 실행에 소요된 총 시간만을 확인할 수 있습니다.
            /// `true`로 설정하면 전체 추론 시간이 느려질 수 있습니다. 실제 검사 시에는 `false`로 설정하는 것을 권장합니다.
            option.CalcTime = true;
            option.CalcObject = true;
            option.CalcScoremap = false;
            option.CalcMask = false;
            option.CalcObjectAreaAndApplyThreshold = true;
            option.CalcObjectScoreAndApplyThreshold = true;
            option.OversizedImageHandling = OverSizeImageFlags.do_not_inspect;

            //option.ObjectScoreThresholdPerClass[1] = 0;
            //option.ObjectScoreThresholdPerClass[2] = 0;

            //option.ObjectAreaThresholdPerClass[1] = 0;
            //option.ObjectAreaThresholdPerClass[2] = 0;

            // option을 적용하여 검사에 대한 조건을 변경할 수 있습니다.
            // 필요에 따라 writeModelFile parameter를 이용하여 모델파일에 정보를 영구적으로 변경할 수 있습니다.
            _segEngine.SetInferenceOption(option);
        }

        public void LoadDetEngine(string modelPath)
        {
            // 검사하기 위한 엔진에 대한 객체를 생성합니다.
            // 인스턴스 생성 시 모데파일 정보와 GPU Index를 입력해줍니다.
            // 필요에 따라 batch size, optimaize 사용 여부를 입력합니다.
            _detEngine = new DetectionEngine(modelPath, 0);

            // 검사 전 option에 대한 설정을 가져옵니다
            DetectionOption option = _detEngine.GetInferenceOption();

            option.CalcTime = true;

            //option.ObjectScoreThresholdPerClass[1] = 50;
            //option.ObjectScoreThresholdPerClass[2] = 50;

            //option.ObjectAreaThresholdPerClass[1] = 0;
            //option.ObjectAreaThresholdPerClass[2] = 0;

            //option.MaxNumOfDetectedObjects[1] = -1;
            //option.MaxNumOfDetectedObjects[2] = -1;

            // option을 적용하여 검사에 대한 조건을 변경할 수 있습니다.
            // 필요에 따라 writeModelFile parameter를 이용하여 모델파일에 정보를 영구적으로 변경할 수 있습니다.
            _detEngine.SetInferenceOption(option);
        }

        public void RunCLS(string txt)
        {
            DisposeMode();
            string modelPath = Path.GetFullPath(txt);

            _clsEngine = new ClassificationEngine(modelPath, 0);

            ClassificationOption option = _clsEngine.GetInferenceOption();
            option.CalcTime = true;
            option.CalcClassActivationMap = false;

            _clsEngine.SetInferenceOption(option);
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
                    // IAD 엔진을 이용하여 검사합니다.
                    if (_iADEngine == null)
                    {
                        MessageBox.Show("엔진이 초기화되지 않았습니다. LoadEngine 메서드를 호출하여 엔진을 초기화하세요.");
                        return false;
                    }

                    _iADResult = _iADEngine.Inspection(srImage);
                    _lastResult = _iADResult;
                    break;
                case AIEngineType.Segmentation:
                    if (_segEngine == null)
                    {
                        MessageBox.Show("엔진이 초기화되지 않았습니다. LoadEngine 메서드를 호출하여 엔진을 초기화하세요.");
                        return false;
                    }
                    // Segmentation 엔진을 이용하여 검사합니다.
                    _segResult = _segEngine.Inspection(srImage);
                    break;
                case AIEngineType.Detection:
                    if (_detEngine == null)
                    {
                        MessageBox.Show("엔진이 초기화되지 않았습니다. LoadEngine 메서드를 호출하여 엔진을 초기화하세요.");
                        return false;
                    }
                    // Detection 엔진을 이용하여 검사합니다.
                    _detResult = _detEngine.Inspection(srImage);
                    _lastResult = _detResult;
                    break;
                case EngineType.CLS: // Classification
                    if (_clsEngine == null)
                    {
                        MessageBox.Show("엔진이 초기화되지 않았습니다. LoadEngine 메서드를 호출하여 엔진을 초기화하세요.");
                        return false;
                    }
                    _clsResult = _clsEngine.Inspection(srImage);
                    try
                    {
                        string label;
                        float score;
                        if (TryGetClassificationTop1(_clsResult, out label, out score))
                        {
                            _lastClsLabel = label;
                            _lastClsScore = score;
                        }
                        else
                        {
                            _lastClsLabel = null;
                            _lastClsScore = 0f;
                        }
                    }
                    catch
                    {
                        _lastClsLabel = null;
                        _lastClsScore = 0f;
                    }
                    break;
            }

            return true;
        }


        public object GetResult()
        {
            return _lastResult;
        }

        // IADResult를 이용하여 결과를 이미지에 그립니다.
        private void DrawSegResult(SegmentedObject[] segmentedObjects, Bitmap bmp)
        {
            Graphics g = Graphics.FromImage(bmp);
            int step = 10;

            // outline contour
            foreach (var prediction in segmentedObjects)
            {
                SolidBrush brush = new SolidBrush(Color.FromArgb(127, prediction.ClassInfo.Color));
                //g.DrawString(prediction.ClassInfo.Name + " : " + prediction.Area, new Font(FontFamily.GenericSansSerif, 50), brush, 10, step);
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
                //g.DrawString(prediction.ClassInfo.Name + " : " + prediction.Area, new Font(FontFamily.GenericSansSerif, 50), brush, 10, step);
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

        public bool TryGetLastClsTop1(out string label, out float score)
        {
            label = _lastClsLabel;
            score = _lastClsScore;
            return !string.IsNullOrWhiteSpace(label);
        }

        public Bitmap GetResultImage()
        {
            if (_inspImage is null)
                return null;

            Bitmap resultImage = _inspImage.Clone(new Rectangle(0, 0, _inspImage.Width, _inspImage.Height), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
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
        public ModelInfo GetModelInfo()
        {
            switch (_engineType)
            {
                case AIEngineType.AnomalyDetection:
                    return _iADEngine?.GetModelInfo();

                case AIEngineType.Segmentation:
                    return _segEngine?.GetModelInfo();

                case AIEngineType.Detection:
                    return _detEngine?.GetModelInfo();
            }

        private void DrawCLSResultOverlay(object clsResultObj, Bitmap bmp)
        {
            try
            {
                string label;
                float score;
                bool ok = TryGetClassificationTop1(clsResultObj, out label, out score);

                string text = ok
                    ? $"{label} ({score:0.0})"
                    : "CLS: (result parsed failed)";

                if(label == "Good")
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    using (Font font = new Font("Arial", 80, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (SolidBrush bg = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                    using (SolidBrush fg = new SolidBrush(Color.Green))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;

                        SizeF sz = g.MeasureString(text, font);
                        RectangleF rect = new RectangleF(10, 10, sz.Width + 20, sz.Height + 16);

                        g.FillRectangle(bg, rect);
                        g.DrawString(text, font, fg, 20, 18);
                    }
                }
                else
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    using (Font font = new Font("Arial", 80, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (SolidBrush bg = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                    using (SolidBrush fg = new SolidBrush(Color.Red))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;

                        SizeF sz = g.MeasureString(text, font);
                        RectangleF rect = new RectangleF(10, 10, sz.Width + 20, sz.Height + 16);

                        g.FillRectangle(bg, rect);
                        g.DrawString(text, font, fg, 20, 18);
                    }
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

        #region Disposable

        private bool disposed = false; // to detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.

                    // 검사완료 후 메모리 해제를 합니다.
                    // 엔진 사용이 완료되면 꼭 dispose 해주세요
                    DisposeMode();
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
