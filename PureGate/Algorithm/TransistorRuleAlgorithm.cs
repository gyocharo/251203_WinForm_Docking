using OpenCvSharp;
using PureGate.Algorithm;
using PureGate.Core;
using SaigeVision.Net.V2;
using System;
using System.Collections.Generic;

namespace PureGate.Algorithm
{
    public class TransistorRuleAlgorithm : InspAlgorithm
    {
        public TransistorRuleAlgorithm()
        {
            InspectType = InspectType.InspTransistorRule;
        }

        public TransistorRoiRole TargetRole { get; set; } = TransistorRoiRole.Base;

        // =========================================================
        // ✅ TransistorRuleProp(UI) 호환용 기존 프로퍼티들 (컴파일 에러 해결)
        // =========================================================
        public int BodyDarkThresholdPercentile { get; set; } = 30;   // Base 케이스(검정) 분리에도 사용
        public int LeadBrightThresholdPercentile { get; set; } = 80; // Body 다리(밝음) 분리에도 사용
        public int ExpectedLeadCount { get; set; } = 3;

        public double MinBodyWidthRatio { get; set; } = 0.25;        // (현재 로직에서는 미사용, 호환 유지)
        public double MinBodyHeightRatio { get; set; } = 0.20;       // (현재 로직에서는 미사용, 호환 유지)

        public int MinLeadArea { get; set; } = 250;
        public double MinLeadAspectRatio { get; set; } = 2.0;
        public double MaxLeadWidthRatioToBody { get; set; } = 0.35;  // (현재 로직에서는 미사용, 호환 유지)
        public int LeadAttachMaxGapPx { get; set; } = 35;            // (현재 로직에서는 미사용, 호환 유지)

        // Sub(위치불량) 호환용
        public int SubBodyMinAreaPx { get; set; } = 2000;
        public double SubAngleTolDeg { get; set; } = 8.0;
        public double SubOffsetXTolPx { get; set; } = 25.0;
        public double SubOffsetYTolPx { get; set; } = 25.0;
        public int SubOpenK { get; set; } = 3;
        public int SubCloseK { get; set; } = 15;

        private readonly List<DrawInspectInfo> _drawInfos = new List<DrawInspectInfo>();

        // =========================================================
        // 추가 파라미터(필요시 조절)
        // =========================================================
        public int BodyBlurK { get; set; } = 5;
        public double MinLeadLenRatioToRoiH { get; set; } = 0.35; // 다리 잘림(길이 짧음) 판정

        public int BaseOpenK { get; set; } = 3;
        public double BaseDefectBrightRatioTol { get; set; } = 0.020; // 케이스 내부 밝은 결함 비율

        // Sub 존재판정(없음)
        public int SubMinObjectAreaPx { get; set; } = 25000; // "트랜지스터가 있다" 최소 면적(큰 컨투어)
        public int SubBlurK { get; set; } = 5;

        // Prop에서 호출될 수 있는 훅
        public void NotifyParamsChanged() { }

        public override InspAlgorithm Clone()
        {
            var c = new TransistorRuleAlgorithm();
            c.CopyFrom(this);
            return c;
        }

        public override bool CopyFrom(InspAlgorithm src)
        {
            var s = src as TransistorRuleAlgorithm;
            if (s == null) return false;

            TargetRole = s.TargetRole;

            BodyDarkThresholdPercentile = s.BodyDarkThresholdPercentile;
            LeadBrightThresholdPercentile = s.LeadBrightThresholdPercentile;
            ExpectedLeadCount = s.ExpectedLeadCount;

            MinBodyWidthRatio = s.MinBodyWidthRatio;
            MinBodyHeightRatio = s.MinBodyHeightRatio;

            MinLeadArea = s.MinLeadArea;
            MinLeadAspectRatio = s.MinLeadAspectRatio;
            MaxLeadWidthRatioToBody = s.MaxLeadWidthRatioToBody;
            LeadAttachMaxGapPx = s.LeadAttachMaxGapPx;

            SubBodyMinAreaPx = s.SubBodyMinAreaPx;
            SubAngleTolDeg = s.SubAngleTolDeg;
            SubOffsetXTolPx = s.SubOffsetXTolPx;
            SubOffsetYTolPx = s.SubOffsetYTolPx;
            SubOpenK = s.SubOpenK;
            SubCloseK = s.SubCloseK;

            BodyBlurK = s.BodyBlurK;
            MinLeadLenRatioToRoiH = s.MinLeadLenRatioToRoiH;

            BaseOpenK = s.BaseOpenK;
            BaseDefectBrightRatioTol = s.BaseDefectBrightRatioTol;

            SubMinObjectAreaPx = s.SubMinObjectAreaPx;
            SubBlurK = s.SubBlurK;

            return true;
        }

        public override bool DoInspect()
        {
            ResetResult();
            ResultString.Clear();
            _drawInfos.Clear();

            if (_srcImage == null) return false;

            Rect roi = ToCvRect(InspRect);
            roi = ClampRect(roi, _srcImage.Width, _srcImage.Height);
            if (roi.Width <= 0 || roi.Height <= 0) return false;

            // (선택) 윈도우 ROI도 항상 그리고 싶으면 Info로 추가
            _drawInfos.Add(new DrawInspectInfo(roi, TargetRole.ToString() + "ROI", InspectType, DecisionType.Info));

            bool ng;
            string label;

            if (TargetRole == TransistorRoiRole.Sub)
            {
                var r = Inspect_Sub(roi);
                ng = r.Item1; label = r.Item2;
            }
            else if (TargetRole == TransistorRoiRole.Base)
            {
                var r = InspectCaseDamage_Base(roi);
                ng = r.Item1; label = r.Item2;
            }
            else
            {
                var r = InspectLead_Body(roi);
                ng = r.Item1; label = r.Item2;
            }

            IsInspected = true;
            IsDefect = ng;
            ResultString.Add(label);
            return true;
        }

        // =========================================================
        // SUB: 트랜지스터 존재/각도/위치(중심) 검사
        // =========================================================
        private Tuple<bool, string> Inspect_Sub(Rect roi)
        {
            Mat roiMat = null, gray = null, blur = null, bin = null, morph = null, kOpen = null, kClose = null;

            try
            {
                roiMat = new Mat(_srcImage, roi);

                gray = new Mat();
                if (roiMat.Channels() == 1) roiMat.CopyTo(gray);
                else Cv2.CvtColor(roiMat, gray, ColorConversionCodes.BGR2GRAY);

                blur = new Mat();
                Cv2.GaussianBlur(gray, blur, new Size(SubBlurK, SubBlurK), 0);

                // 어두운 물체(트랜지스터)를 흰색으로: Otsu + Invert
                bin = new Mat();
                Cv2.Threshold(blur, bin, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                morph = bin.Clone();
                kOpen = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(SubOpenK, SubOpenK));
                kClose = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(SubCloseK, SubCloseK));
                Cv2.MorphologyEx(morph, morph, MorphTypes.Open, kOpen, iterations: 1);
                Cv2.MorphologyEx(morph, morph, MorphTypes.Close, kClose, iterations: 1);

                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(morph, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours == null || contours.Length == 0)
                    return Tuple.Create(true, "미스매칭(없음)");

                int bestIdx = -1;
                double bestArea = 0;
                for (int i = 0; i < contours.Length; i++)
                {
                    double a = Cv2.ContourArea(contours[i]);
                    if (a > bestArea) { bestArea = a; bestIdx = i; }
                }

                // 존재 판정(없음)
                if (bestIdx < 0 || bestArea < Math.Max(SubBodyMinAreaPx, SubMinObjectAreaPx))
                    return Tuple.Create(true, "미스매칭(없음)");

                RotatedRect rr = Cv2.MinAreaRect(contours[bestIdx]);
                double angle = NormalizeAngle(rr);

                // 각도 불량
                if (Math.Abs(angle) > SubAngleTolDeg)
                    return Tuple.Create(true, "미스매칭(각도)");

                // 위치 불량(중심 이탈)
                Point2f c = rr.Center;
                Point2f roiCenter = new Point2f(roiMat.Width / 2f, roiMat.Height / 2f);
                double dx = c.X - roiCenter.X;
                double dy = c.Y - roiCenter.Y;

                if (Math.Abs(dx) > SubOffsetXTolPx || Math.Abs(dy) > SubOffsetYTolPx)
                    return Tuple.Create(true, "미스매칭(위치)");

                return Tuple.Create(false, "양품");
            }
            finally
            {
                if (kClose != null) kClose.Dispose();
                if (kOpen != null) kOpen.Dispose();
                if (morph != null) morph.Dispose();
                if (bin != null) bin.Dispose();
                if (blur != null) blur.Dispose();
                if (gray != null) gray.Dispose();
                if (roiMat != null) roiMat.Dispose();
            }
        }

        private double NormalizeAngle(RotatedRect rr)
        {
            double angle = rr.Angle;
            if (rr.Size.Width >= rr.Size.Height) angle += 90.0;

            while (angle > 90) angle -= 180;
            while (angle < -90) angle += 180;
            return angle;
        }

        // =========================================================
        // BASE: 검은 케이스 파손 검사
        // - 케이스(어두운 영역) 마스크를 만들고
        // - 그 안에서 비정상적으로 밝은 픽셀(깨짐/스크래치/이물) 비율이 크면 NG
        // =========================================================
        private Tuple<bool, string> InspectCaseDamage_Base(Rect roi)
        {
            Mat roiMat = null, gray = null, blur = null, caseMask = null, k = null, caseOnly = null, defectMask = null;
            try
            {
                roiMat = new Mat(_srcImage, roi);

                gray = new Mat();
                if (roiMat.Channels() == 1) roiMat.CopyTo(gray);
                else Cv2.CvtColor(roiMat, gray, ColorConversionCodes.BGR2GRAY);

                blur = new Mat();
                Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);

                // 케이스(검정) 분리: 낮은 percentile을 임계로 잡고, 그보다 어두운 것을 케이스로
                int thr = PercentileThreshold(blur, BodyDarkThresholdPercentile);

                caseMask = new Mat();
                Cv2.Threshold(blur, caseMask, thr, 255, ThresholdTypes.BinaryInv);

                k = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(BaseOpenK, BaseOpenK));
                Cv2.MorphologyEx(caseMask, caseMask, MorphTypes.Open, k, iterations: 1);

                int casePx = Cv2.CountNonZero(caseMask);
                if (casePx < 800)
                    return Tuple.Create(true, "케이스 파손"); // ROI가 잘못되었거나 대상 없음

                // 케이스 내부만 복사
                caseOnly = new Mat(blur.Size(), blur.Type(), Scalar.All(0));
                blur.CopyTo(caseOnly, caseMask);

                // 케이스 내부에서 상위 밝기 픽셀 비율을 결함으로 카운트
                int defectThr = PercentileThreshold(caseOnly, 90);

                defectMask = new Mat();
                Cv2.Threshold(caseOnly, defectMask, defectThr, 255, ThresholdTypes.Binary);

                int defectPx = Cv2.CountNonZero(defectMask);
                double ratio = defectPx / (double)Math.Max(1, casePx);

                if (ratio > BaseDefectBrightRatioTol)
                {
                    _drawInfos.Add(new DrawInspectInfo(roi, "케이스 파손", InspectType.InspTransistorRule, DecisionType.Defect));
                    return Tuple.Create(true, "케이스 파손");
                }

                return Tuple.Create(false, "양품");
            }
            finally
            {
                if (defectMask != null) defectMask.Dispose();
                if (caseOnly != null) caseOnly.Dispose();
                if (k != null) k.Dispose();
                if (caseMask != null) caseMask.Dispose();
                if (blur != null) blur.Dispose();
                if (gray != null) gray.Dispose();
                if (roiMat != null) roiMat.Dispose();
            }
        }

        // =========================================================
        // BODY: 다리 빠짐 / 다리 잘림 검사
        // - 다리는 밝음: LeadBrightThresholdPercentile로 threshold
        // - 컨투어 중 "긴 막대"만 리드 후보로 카운트
        // - 개수 부족 -> 다리 빠짐
        // - 후보 중 길이가 ROI 대비 너무 짧은 게 있으면 -> 다리 잘림
        // =========================================================
        private Tuple<bool, string> InspectLead_Body(Rect roi)
        {
            Mat roiMat = null, gray = null, blur = null, bin = null, k = null;
            try
            {
                roiMat = new Mat(_srcImage, roi);

                gray = new Mat();
                if (roiMat.Channels() == 1) roiMat.CopyTo(gray);
                else Cv2.CvtColor(roiMat, gray, ColorConversionCodes.BGR2GRAY);

                blur = new Mat();
                Cv2.GaussianBlur(gray, blur, new Size(BodyBlurK, BodyBlurK), 0);

                // 밝은 리드 분리: percentile 기반 threshold
                int thr = PercentileThreshold(blur, LeadBrightThresholdPercentile);

                bin = new Mat();
                Cv2.Threshold(blur, bin, thr, 255, ThresholdTypes.Binary);

                k = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
                Cv2.MorphologyEx(bin, bin, MorphTypes.Open, k, iterations: 1);

                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(bin, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours == null || contours.Length == 0)
                    return Tuple.Create(true, "다리 빠짐");

                int leadCnt = 0;
                bool anyTooShort = false;

                int roiH = roiMat.Height;
                double minLen = roiH * MinLeadLenRatioToRoiH;

                for (int i = 0; i < contours.Length; i++)
                {
                    double area = Cv2.ContourArea(contours[i]);
                    if (area < MinLeadArea) continue;

                    Rect r = Cv2.BoundingRect(contours[i]);
                    int longSide = Math.Max(r.Width, r.Height);
                    int shortSide = Math.Max(1, Math.Min(r.Width, r.Height));
                    double ar = longSide / (double)shortSide;

                    if (ar < MinLeadAspectRatio) continue;

                    // 리드 후보 1개
                    leadCnt++;

                    var decision = DecisionType.Info;
                    string info = "Lead";

                    if (longSide < minLen)
                    {
                        anyTooShort = true;
                        decision = DecisionType.Defect;
                        info = "다리 잘림";
                    }

                    // roiMat 좌표를 원본 좌표로 보정
                    var abs = new Rect(roi.X + r.X, roi.Y + r.Y, r.Width, r.Height);
                    _drawInfos.Add(new DrawInspectInfo(abs, info, InspectType.InspTransistorRule, decision));
                }

                if (leadCnt < ExpectedLeadCount)
                    return Tuple.Create(true, "다리 빠짐");

                if (anyTooShort)
                    return Tuple.Create(true, "다리 잘림");

                return Tuple.Create(false, "양품");
            }
            finally
            {
                if (k != null) k.Dispose();
                if (bin != null) bin.Dispose();
                if (blur != null) blur.Dispose();
                if (gray != null) gray.Dispose();
                if (roiMat != null) roiMat.Dispose();
            }
        }

        public override int GetResultRect(out List<DrawInspectInfo> areas)
        {
            areas = new List<DrawInspectInfo>(_drawInfos);
            return areas.Count;
        }

        // =========================================================
        // Utils
        // =========================================================
        private Rect ClampRect(Rect r, int w, int h)
        {
            int x = Math.Max(0, r.X);
            int y = Math.Max(0, r.Y);
            int rw = Math.Min(r.Width, w - x);
            int rh = Math.Min(r.Height, h - y);
            if (rw < 0) rw = 0;
            if (rh < 0) rh = 0;
            return new Rect(x, y, rw, rh);
        }

        private Rect ToCvRect(object inspRect)
        {
            if (inspRect is Rect) return (Rect)inspRect;

            if (inspRect is System.Drawing.Rectangle)
            {
                var dr = (System.Drawing.Rectangle)inspRect;
                return new Rect(dr.X, dr.Y, dr.Width, dr.Height);
            }

            try
            {
                var t = inspRect.GetType();
                int x = Convert.ToInt32(t.GetProperty("X") != null ? t.GetProperty("X").GetValue(inspRect) : 0);
                int y = Convert.ToInt32(t.GetProperty("Y") != null ? t.GetProperty("Y").GetValue(inspRect) : 0);

                object wObj = null;
                if (t.GetProperty("Width") != null) wObj = t.GetProperty("Width").GetValue(inspRect);
                if (wObj == null && t.GetProperty("W") != null) wObj = t.GetProperty("W").GetValue(inspRect);
                int w = Convert.ToInt32(wObj ?? 0);

                object hObj = null;
                if (t.GetProperty("Height") != null) hObj = t.GetProperty("Height").GetValue(inspRect);
                if (hObj == null && t.GetProperty("H") != null) hObj = t.GetProperty("H").GetValue(inspRect);
                int h = Convert.ToInt32(hObj ?? 0);

                return new Rect(x, y, w, h);
            }
            catch
            {
                return new Rect(0, 0, 0, 0);
            }
        }

        private int PercentileThreshold(Mat gray8u, int percentile0to100)
        {
            int p = Math.Max(0, Math.Min(100, percentile0to100));

            Mat src = gray8u;
            Mat tmp = null;

            // 단일 채널 8U로 맞춤
            if (gray8u.Type() != MatType.CV_8UC1)
            {
                tmp = new Mat();
                gray8u.ConvertTo(tmp, MatType.CV_8UC1);
                src = tmp;
            }

            try
            {
                int[] hist = new int[256];

                // 포인터(unsafe) 없이 픽셀 읽기
                // OpenCvSharp MatIndexer 사용
                var idx = src.GetGenericIndexer<byte>();

                for (int y = 0; y < src.Rows; y++)
                {
                    for (int x = 0; x < src.Cols; x++)
                    {
                        byte v = idx[y, x];
                        hist[v]++;
                    }
                }

                int total = src.Rows * src.Cols;
                int target = (int)Math.Round(total * (p / 100.0));

                int acc = 0;
                for (int i = 0; i < 256; i++)
                {
                    acc += hist[i];
                    if (acc >= target) return i;
                }

                return 127;
            }
            finally
            {
                if (tmp != null) tmp.Dispose();
            }
        }
    }
}
