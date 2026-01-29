using System;
using System.Collections.Generic;
using OpenCvSharp;
using PureGate.Algorithm;
using PureGate.Core;

public class TransistorRuleAlgorithm : InspAlgorithm
{
    public TransistorRoiRole TargetRole { get; set; } = TransistorRoiRole.Base;

    // 네가 말한 그대로
    protected Mat _srcImage = null;

    /* ---------- Sub(위치불량) 파라미터 ---------- */
    public int SubBodyMinAreaPx { get; set; } = 2000;
    public double SubAngleTolDeg { get; set; } = 8.0;
    public double SubOffsetXTolPx { get; set; } = 25.0;
    public double SubOffsetYTolPx { get; set; } = 25.0;

    public int SubOpenK { get; set; } = 3;
    public int SubCloseK { get; set; } = 15;

    // ===== 기존 Prop(UI)가 쓰던 파라미터들(호환용) =====
    public int BodyDarkThresholdPercentile { get; set; } = 30;
    public int LeadBrightThresholdPercentile { get; set; } = 80;
    public int ExpectedLeadCount { get; set; } = 3;

    public double MinBodyWidthRatio { get; set; } = 0.25;
    public double MinBodyHeightRatio { get; set; } = 0.20;

    public int MinLeadArea { get; set; } = 250;
    public double MinLeadAspectRatio { get; set; } = 2.0;
    public double MaxLeadWidthRatioToBody { get; set; } = 0.35;
    public int LeadAttachMaxGapPx { get; set; } = 35;

    // Prop에서 호출하는 메서드(없어서 에러났음) - 일단 빈 구현으로라도 추가
    public void NotifyParamsChanged()
    {
        // 필요하면 여기서 파라미터 변경 이벤트/플래그 처리
    }

    // ---------- Clone / CopyFrom : 네 InspAlgorithm 시그니처에 맞춰야 함 ----------
    // 만약 네 프로젝트 시그니처가 다르면 여기 2개만 네 선언에 맞게 고치면 됨.
    public override InspAlgorithm Clone()
    {
        TransistorRuleAlgorithm c = new TransistorRuleAlgorithm();
        c.CopyFrom(this);
        return c;
    }

    public override bool CopyFrom(InspAlgorithm src)
    {
        TransistorRuleAlgorithm s = src as TransistorRuleAlgorithm;
        if (s == null) return false;

        TargetRole = s.TargetRole;

        SubBodyMinAreaPx = s.SubBodyMinAreaPx;
        SubAngleTolDeg = s.SubAngleTolDeg;
        SubOffsetXTolPx = s.SubOffsetXTolPx;
        SubOffsetYTolPx = s.SubOffsetYTolPx;

        SubOpenK = s.SubOpenK;
        SubCloseK = s.SubCloseK;

        BodyDarkThresholdPercentile = s.BodyDarkThresholdPercentile;
        LeadBrightThresholdPercentile = s.LeadBrightThresholdPercentile;
        ExpectedLeadCount = s.ExpectedLeadCount;

        MinBodyWidthRatio = s.MinBodyWidthRatio;
        MinBodyHeightRatio = s.MinBodyHeightRatio;

        MinLeadArea = s.MinLeadArea;
        MinLeadAspectRatio = s.MinLeadAspectRatio;
        MaxLeadWidthRatioToBody = s.MaxLeadWidthRatioToBody;
        LeadAttachMaxGapPx = s.LeadAttachMaxGapPx;

        return true;
    }
    // -----------------------------------------------------------------------

    public override bool DoInspect()
    {
        ResetResult();
        ResultString.Clear();

        if (_srcImage == null) return false;

        // InspRect 타입이 프로젝트마다 다를 수 있어 변환
        Rect roi = ToCvRect(InspRect);
        roi = ClampRect(roi, _srcImage.Width, _srcImage.Height);
        if (roi.Width <= 0 || roi.Height <= 0) return false;

        bool ng = false;
        string label = "양품";

        if (TargetRole == TransistorRoiRole.Sub)
        {
            Tuple<bool, string> r = InspectPose_Sub(roi);
            ng = r.Item1;
            label = r.Item2;
        }
        else if (TargetRole == TransistorRoiRole.Base)
        {
            Tuple<bool, string> r = InspectCaseDamage_Base(roi);
            ng = r.Item1;
            label = r.Item2;
        }
        else // Body
        {
            Tuple<bool, string> r = InspectLead_Body(roi);
            ng = r.Item1;
            label = r.Item2;
        }

        IsInspected = true;
        IsDefect = ng;
        ResultString.Add(label);

        // ===== 결과 Rect 기록 =====
        // 기존 프로젝트가 GetResultRect()를 갖고 있고,
        // InspWorker.ExtractNgName()이 첫 번째 info를 쓰는 구조라면,
        // "대표 라벨"을 반드시 첫번째로 넣어준다.
        //List<DrawInspectInfo> areas;
        //GetResultRect(out areas);

        //if (areas != null)
        //{
        //    areas.Insert(0, new DrawInspectInfo(
        //        roi,
        //        label,
        //        InspectType.InspTransistorRule,
        //        ng ? DecisionType.Defect : DecisionType.Info));
        //}

        return true;
    }

    // ===================== Sub: 위치 불량 (바디 기준 + OffsetX 포함) =====================

    private Tuple<bool, string> InspectPose_Sub(Rect roi)
    {
        Mat roiMat = null;
        Mat gray = null;
        Mat blur = null;
        Mat bin = null;
        Mat morph = null;
        Mat kOpen = null;
        Mat kClose = null;

        try
        {
            roiMat = new Mat(_srcImage, roi);

            gray = new Mat();
            if (roiMat.Channels() == 1) roiMat.CopyTo(gray);
            else Cv2.CvtColor(roiMat, gray, ColorConversionCodes.BGR2GRAY);

            blur = new Mat();
            Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);

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
                return Tuple.Create(true, "위치 불량");

            int bestIdx = -1;
            double bestArea = 0.0;
            for (int i = 0; i < contours.Length; i++)
            {
                double area = Cv2.ContourArea(contours[i]);
                if (area > bestArea)
                {
                    bestArea = area;
                    bestIdx = i;
                }
            }

            if (bestIdx < 0 || bestArea < SubBodyMinAreaPx)
                return Tuple.Create(true, "위치 불량");

            RotatedRect rr = Cv2.MinAreaRect(contours[bestIdx]);
            Point2f bodyCenter = rr.Center;

            double angle = NormalizeAngle(rr);

            Point2f roiCenter = new Point2f(roiMat.Width / 2f, roiMat.Height / 2f);
            double offsetX = bodyCenter.X - roiCenter.X;
            double offsetY = bodyCenter.Y - roiCenter.Y;

            if (Math.Abs(angle) > SubAngleTolDeg) return Tuple.Create(true, "위치 불량");
            if (Math.Abs(offsetX) > SubOffsetXTolPx) return Tuple.Create(true, "위치 불량");
            if (Math.Abs(offsetY) > SubOffsetYTolPx) return Tuple.Create(true, "위치 불량");

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
        // 주의: MinAreaRect 각도 보정은 샘플로 튜닝 필요
        double angle = rr.Angle;
        if (rr.Size.Width >= rr.Size.Height) angle += 90.0;

        while (angle > 90) angle -= 180;
        while (angle < -90) angle += 180;
        return angle;
    }

    // ===================== Base: 케이스 파손 =====================
    // 여기만 네 기존 로직으로 교체하면 됨
    private Tuple<bool, string> InspectCaseDamage_Base(Rect roi)
    {
        // TODO: 기존 Base 로직 붙여넣기
        return Tuple.Create(false, "양품");
    }

    // ===================== Body: 다리 빠짐 / 잘림 =====================
    // 여기만 네 기존 로직으로 교체하면 됨
    private Tuple<bool, string> InspectLead_Body(Rect roi)
    {
        // TODO: 기존 Body 로직 붙여넣기
        return Tuple.Create(false, "양품");
    }

    // ===================== Utils =====================

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
        // InspRect가 OpenCvSharp.Rect인 경우
        if (inspRect is Rect) return (Rect)inspRect;

        // InspRect가 System.Drawing.Rectangle인 경우
        if (inspRect is System.Drawing.Rectangle)
        {
            System.Drawing.Rectangle dr = (System.Drawing.Rectangle)inspRect;
            return new Rect(dr.X, dr.Y, dr.Width, dr.Height);
        }

        // 그 외: 안전하게 0
        return new Rect(0, 0, 0, 0);
    }
}
