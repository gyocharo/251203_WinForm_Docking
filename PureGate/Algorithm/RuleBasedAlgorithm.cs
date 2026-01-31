using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using PureGate.Core;
using System.Xml.Serialization;

namespace PureGate.Algorithm
{
    public class RuleBasedAlgorithm : InspAlgorithm
    {// ===== Golden (Teach) =====
        [XmlIgnore] private Mat _goldenImage = null;      // gray
        [XmlIgnore] private Mat _goldenTemplate = null;   // base/body compare

        // Sub feature (lead)
        private double _goldenMetalArea = 0;
        private double _goldenCentroidX = 0;

        // ===== Result =====
        [XmlIgnore]
        public NgType DetectedNgType { get; private set; } = NgType.None;

        public InspWindowType WindowType { get; set; } = InspWindowType.None;

        // ===== Thresholds =====
        public double MisplacedMatchScoreThreshold { get; set; } = 0.30;
        public int MisplacedHolePixelThreshold { get; set; } = 40000;
        public int DamagedCaseDiffPixelThreshold { get; set; } = 5000;

        public double CutLeadAreaRatioThreshold { get; set; } = 0.50;
        public double BentLeadCentroidXThreshold { get; set; } = 15.0;

        public int MetalThreshold { get; set; } = 120;
        public int CaseDiffThreshold { get; set; } = 45;
        public int HoleDarkThreshold { get; set; } = 80;
        public double DamagedCaseRatioThreshold { get; set; } = 0.36;


        private int _goldenHolePixels = 0;

        public RuleBasedAlgorithm()
        {
            InspectType = InspectType.InspRuleBased;
        }

        [XmlIgnore]
        public string ParentWindowUid { get; set; } = "";


        public override InspAlgorithm Clone()
        {
            var clone = new RuleBasedAlgorithm();
            this.CopyBaseTo(clone);
            clone.CopyFrom(this);
            return clone;
        }

        public override bool CopyFrom(InspAlgorithm sourceAlgo)
        {
            var src = sourceAlgo as RuleBasedAlgorithm;
            if (src == null) return false;

            WindowType = src.WindowType;

            MisplacedMatchScoreThreshold = src.MisplacedMatchScoreThreshold;
            MisplacedHolePixelThreshold = src.MisplacedHolePixelThreshold;
            DamagedCaseDiffPixelThreshold = src.DamagedCaseDiffPixelThreshold;

            CutLeadAreaRatioThreshold = src.CutLeadAreaRatioThreshold;
            BentLeadCentroidXThreshold = src.BentLeadCentroidXThreshold;

            MetalThreshold = src.MetalThreshold;
            CaseDiffThreshold = src.CaseDiffThreshold;
            HoleDarkThreshold = src.HoleDarkThreshold;

            // Golden Mat은 런타임 리소스: 모델 저장/복제 대상으로 안 봄
            return true;
        }

        public override void ResetResult()
        {
            base.ResetResult();
            DetectedNgType = NgType.None;
        }

        public bool SetGoldenImage(Mat goldenImage)
        {
            if (goldenImage == null || goldenImage.Empty())
                return false;

            Mat gray = null;

            try
            {
                // gray 변환
                if (goldenImage.Type() == MatType.CV_8UC3)
                {
                    gray = new Mat();
                    Cv2.CvtColor(goldenImage, gray, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    gray = goldenImage.Clone();
                }

                if (_goldenImage != null) _goldenImage.Dispose();
                _goldenImage = gray.Clone();

                if (WindowType == InspWindowType.Base || WindowType == InspWindowType.Body)
                {
                    if (_goldenTemplate != null) _goldenTemplate.Dispose();
                    _goldenTemplate = gray.Clone();
                }

                if (WindowType == InspWindowType.Base)
                {
                    _goldenHolePixels = CountHolePixels(gray);
                    ResultString.Add("[Golden] BaseHolePixels=" + _goldenHolePixels);
                }

                if (WindowType == InspWindowType.Sub)
                {
                    double area, cx;
                    ExtractSubFeatures(gray, out area, out cx);
                    _goldenMetalArea = area;
                    _goldenCentroidX = cx;
                }

                ResultString.Add("[Golden Set] " + WindowType + " OK");
                return true;
            }
            catch (Exception ex)
            {
                ResultString.Add("골든 이미지 설정 실패: " + ex.Message);
                return false;
            }
            finally
            {
                if (gray != null) gray.Dispose();
            }
        }

        public override bool DoInspect()
        {
            ResetResult();

            if (!IsUse)
                return false;

            if (_srcImage == null || _srcImage.Empty())
            {
                ResultString.Add("검사 이미지 없음");
                return false;
            }

            // Golden 필요 조건
            if (WindowType == InspWindowType.Base || WindowType == InspWindowType.Body)
            {
                if (_goldenTemplate == null || _goldenTemplate.Empty())
                {
                    ResultString.Add("골든 이미지 없음(Base/Body)");
                    return false;
                }
            }
            if (WindowType == InspWindowType.Sub)
            {
                if (_goldenMetalArea <= 0)
                {
                    ResultString.Add("골든 특징값 없음(Sub) - 골든 Set 필요");
                    return false;
                }
            }

            // ROI 체크
            if (InspRect.Width <= 0 || InspRect.Height <= 0)
            {
                ResultString.Add("ROI 크기 이상: " + InspRect);
                return false;
            }
            if (InspRect.Right > _srcImage.Width || InspRect.Bottom > _srcImage.Height)
            {
                ResultString.Add("ROI 범위 초과: " + InspRect);
                return false;
            }

            Mat roi = null;
            Mat gray = null;

            try
            {
                roi = new Mat(_srcImage, InspRect);

                if (roi.Type() == MatType.CV_8UC3)
                {
                    gray = new Mat();
                    Cv2.CvtColor(roi, gray, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    gray = roi.Clone();
                }

                switch (WindowType)
                {
                    case InspWindowType.Base:
                        InspectBase(gray);
                        break;

                    case InspWindowType.Body:
                        InspectBody(gray);
                        break;

                    case InspWindowType.Sub:
                        InspectSub(gray);
                        break;

                    default:
                        ResultString.Add("지원하지 않는 WindowType: " + WindowType);
                        DetectedNgType = NgType.None;
                        break;
                }

                IsInspected = true;
                IsDefect = (DetectedNgType != NgType.None && DetectedNgType != NgType.Good);

                if (!IsDefect)
                {
                    DetectedNgType = NgType.Good;
                    ResultString.Add("[OK] RuleBased OK");
                }

                return true;
            }
            catch (Exception ex)
            {
                ResultString.Add("검사 오류: " + ex.Message);
                return false;
            }
            finally
            {
                if (gray != null) gray.Dispose();
                if (roi != null) roi.Dispose();
            }
        }

        public override int GetResultRect(out List<DrawInspectInfo> resultArea)
        {
            resultArea = null;

            if (!IsInspected)
                return -1;

            if (!IsDefect)
                return 0;

            resultArea = new List<DrawInspectInfo>();
            string info = GetNgKoreanName(DetectedNgType);

            resultArea.Add(new DrawInspectInfo(InspRect, info, InspectType.InspRuleBased, DecisionType.Defect));
            return resultArea.Count;
        }

        // ===================== Inspect =====================

        private void InspectBase(Mat gray)
        {
            double score = CalcTemplateMatchScore(gray, _goldenTemplate);
            ResultString.Add("[Base] MatchScore=" + score.ToString("F3"));

            int holePixels = CountHolePixels(gray);
            int holeDelta = Math.Abs(holePixels - _goldenHolePixels);
            ResultString.Add($"[Base] HolePixels={holePixels}, Golden={_goldenHolePixels}, Delta={holeDelta}");

            bool byHole = (holeDelta > MisplacedHolePixelThreshold);

            // ✅ 오탐 방지 결합 규칙
            bool byMatchWeak = (score < MisplacedMatchScoreThreshold);
            bool byMatchStrong = (score < 0.25); // 강한 불일치(고정값/추가 튜닝 가능)

            if (byMatchStrong || (byMatchWeak && byHole))
            {
                DetectedNgType = NgType.Misplaced;
                ResultString.Add("[NG] Misplaced");
            }
            else
            {
                DetectedNgType = NgType.Good;
            }
        }


        private void InspectBody(Mat gray)
        {
            Mat diff = null;
            Mat bin = null;

            try
            {
                diff = new Mat();
                Cv2.Absdiff(gray, _goldenTemplate, diff);
                Cv2.GaussianBlur(diff, diff, new Size(3, 3), 0);

                bin = new Mat();
                Cv2.Threshold(diff, bin, CaseDiffThreshold, 255, ThresholdTypes.Binary);

                int diffPixels = Cv2.CountNonZero(bin);
                double ratio = (double)diffPixels / (bin.Rows * bin.Cols);
                ResultString.Add($"[Body] DiffPixels={diffPixels}, Ratio={ratio:F4}");

                if (ratio > DamagedCaseRatioThreshold)
                    DetectedNgType = NgType.DamagedCase;
                else
                    DetectedNgType = NgType.Good;

            }
            finally
            {
                if (bin != null) bin.Dispose();
                if (diff != null) diff.Dispose();
            }
        }

        private void InspectSub(Mat gray)
        {
            double curArea, curCx;
            ExtractSubFeatures(gray, out curArea, out curCx);

            double areaRatio = (_goldenMetalArea > 0) ? (curArea / _goldenMetalArea) : 1.0;
            double cxShift = Math.Abs(curCx - _goldenCentroidX);

            // ✅ UID별 threshold 가져오기
            double cutTh, bentTh;
            GetSubThresholdsByUid(out cutTh, out bentTh);

            ResultString.Add("[Sub] UID=" + ParentWindowUid +
                             ", Area=" + curArea.ToString("F0") +
                             ", AreaRatio=" + areaRatio.ToString("F3") +
                             ", CX=" + curCx.ToString("F1") +
                             ", Shift=" + cxShift.ToString("F1") +
                             $", CutTh={cutTh:F3}, BentTh={bentTh:F1}");

            if (areaRatio < cutTh)
            {
                DetectedNgType = NgType.CutLead;
                ResultString.Add("[NG] CutLead");
                return;
            }

            if (cxShift > bentTh)
            {
                DetectedNgType = NgType.BentLead;
                ResultString.Add("[NG] BentLead");
                return;
            }

            DetectedNgType = NgType.Good;
        }


        // ===================== Helpers =====================

        private void ExtractSubFeatures(Mat gray, out double metalArea, out double centroidX)
        {
            Mat bin = null;
            Mat kernel = null;

            try
            {
                bin = new Mat();
                Cv2.Threshold(gray, bin, MetalThreshold, 255, ThresholdTypes.Binary);

                kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
                Cv2.MorphologyEx(bin, bin, MorphTypes.Open, kernel);

                metalArea = Cv2.CountNonZero(bin);

                Moments m = Cv2.Moments(bin, true);
                if (m.M00 > 0)
                    centroidX = m.M10 / m.M00;
                else
                    centroidX = gray.Width / 2.0;
            }
            finally
            {
                if (kernel != null) kernel.Dispose();
                if (bin != null) bin.Dispose();
            }
        }

        private double CalcTemplateMatchScore(Mat gray, Mat templ)
        {
            if (templ == null || templ.Empty())
                return 0.0;

            if (gray.Width != templ.Width || gray.Height != templ.Height)
            {
                ResultString.Add("[Base] Template size mismatch: cur=" +
                                 gray.Width + "x" + gray.Height +
                                 ", golden=" + templ.Width + "x" + templ.Height);
                return 0.0;
            }

            Mat result = null;
            try
            {
                result = new Mat();
                Cv2.MatchTemplate(gray, templ, result, TemplateMatchModes.CCoeffNormed);
                double minVal, maxVal;
                Point minLoc, maxLoc;
                Cv2.MinMaxLoc(result, out minVal, out maxVal, out minLoc, out maxLoc);
                return maxVal;
            }
            finally
            {
                if (result != null) result.Dispose();
            }
        }

        private int CountHolePixels(Mat gray)
        {
            Mat bin = null;
            Mat kernel = null;

            try
            {
                bin = new Mat();
                Cv2.Threshold(gray, bin, HoleDarkThreshold, 255, ThresholdTypes.BinaryInv);

                kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
                Cv2.MorphologyEx(bin, bin, MorphTypes.Open, kernel);

                return Cv2.CountNonZero(bin);
            }
            finally
            {
                if (kernel != null) kernel.Dispose();
                if (bin != null) bin.Dispose();
            }
        }

        private string GetNgKoreanName(NgType ng)
        {
            if (ng == NgType.None) return "Unknown";

            if (Define.NgTypeKorean != null)
            {
                string name;
                if (Define.NgTypeKorean.TryGetValue(ng, out name))
                    return name;
            }

            return ng.ToString();
        }

        private void GetSubThresholdsByUid(out double cutAreaRatioTh, out double bentCxShiftTh)
        {
            // 기본값(혹시 UID 못 받으면 이 값 사용)
            cutAreaRatioTh = CutLeadAreaRatioThreshold;
            bentCxShiftTh = BentLeadCentroidXThreshold;

            string uid = (ParentWindowUid ?? "").Trim();
            if (uid.Length == 0) return;

            switch (uid)
            {
                case "SUB_000001":
                    cutAreaRatioTh = 0.282;
                    bentCxShiftTh = 71.6;
                    break;

                case "SUB_000002":
                    cutAreaRatioTh = 0.575;
                    bentCxShiftTh = 28.4;
                    break;

                case "SUB_000003":
                    cutAreaRatioTh = 0.370;
                    bentCxShiftTh = 40.3;
                    break;
            }
        }

    }
}

