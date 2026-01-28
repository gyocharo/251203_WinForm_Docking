using PureGate.Algorithm;
using PureGate.Core;
using PureGate.Inspect;
using SaigeVision.Net.V2;
using SaigeVision.Net.V2.Classification;
using SaigeVision.Net.V2.Detection;
using SaigeVision.Net.V2.IAD;
using SaigeVision.Net.V2.Segmentation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Xml.Serialization;

namespace PureGate.Algorithm
{
    public class AIModuleAlgorithm : InspAlgorithm
    {
        public string ModelPath { get; set; }
        public AIEngineType EngineType { get; set; }

        // CLS 결과 저장용 (Status에 NG 클래스 표기용)
        [XmlIgnore]  // ✅ 추가
        public string LastClsLabel { get; private set; } = "";

        [XmlIgnore]  // ✅ 추가
        public float LastClsScore { get; private set; } = 0f;

        private SaigeAI _saigeAI;
        private List<DrawInspectInfo> _resultAreas;

        public AIModuleAlgorithm()
        {
            InspectType = InspectType.InspAIModule;
        }

        public override bool DoInspect()
        {
            ResetResult();

            LastClsLabel = "";
            LastClsScore = 0f;
            LastClsLabel = "";
            LastClsScore = 0f;

            if (!IsUse)
                return false;

            if (string.IsNullOrEmpty(ModelPath))
                return false;

            if (_srcImage == null)
                return false;

            // 검사 영역이 이미지 범위를 벗어나는지 체크
            if (InspRect.Right > _srcImage.Width || InspRect.Bottom > _srcImage.Height)
                return false;

            if (_saigeAI == null)
                _saigeAI = Global.Inst.InspStage.AIModule;

            // ROI 추출 및 검사
            using (Mat roiMat = new Mat(_srcImage, InspRect))
            using (Bitmap roiBmp = BitmapConverter.ToBitmap(roiMat))
            {
                _saigeAI.InspAIModule(roiBmp);
            }

            var result = _saigeAI.GetResult();
            if (result == null)
                return false;

            _resultAreas = new List<DrawInspectInfo>();

            // Enum 명칭 수정 (AnomalyDetection -> IAD, Detection -> DET 등)
            if (EngineType == AIEngineType.IAD)
                HandleIAD(result as IADResult);
            else if (EngineType == AIEngineType.DET)
                HandleDetection(result as DetectionResult);
            else if (EngineType == AIEngineType.SEG)
                HandleSegmentation(result as SegmentationResult);
            else if (EngineType == AIEngineType.CLS)
                HandleClassification();

            IsInspected = true;
            return true;
        }

        private void HandleIAD(IADResult iad)
        {
            if (iad == null) return;

            IsDefect = iad.IsNG;
            ResultString.Add($"IAD Result : {(IsDefect ? "NG" : "OK")}");
            ResultString.Add($"Score : {iad.AnomalyScore.Score:N3}");

            _resultAreas.Add(
                new DrawInspectInfo(
                    InspRect,
                    $"Score:{iad.AnomalyScore.Score:N3}",
                    InspectType.InspAIModule,
                    IsDefect ? DecisionType.Defect : DecisionType.Info
                )
            );
        }

        private void HandleDetection(DetectionResult det)
        {
            if (det == null) return;

            IsDefect = det.DetectedObjects.Length > 0;

            foreach (var obj in det.DetectedObjects)
            {
                // ROI 상대 좌표를 전체 이미지 절대 좌표로 변환
                Rect rect = new Rect(
                    (int)obj.BoundingBox.X + InspRect.X,
                    (int)obj.BoundingBox.Y + InspRect.Y,
                    (int)obj.BoundingBox.Width,
                    (int)obj.BoundingBox.Height
                );

                _resultAreas.Add(new DrawInspectInfo(rect, obj.ClassInfo.Name, InspectType.InspAIModule, DecisionType.Defect));

                // ⭐ 추가: 불량 명칭을 ResultString에 직접 추가합니다.
                ResultString.Add(obj.ClassInfo.Name);

            }
            ResultString.Add($"Detection Count : {det.DetectedObjects.Length}");
        }

        private void HandleSegmentation(SegmentationResult seg)
        {
            if (seg == null) return;

            IsDefect = seg.SegmentedObjects.Length > 0;

            foreach (var obj in seg.SegmentedObjects)
            {
                var box = obj.BoundingRotBox;

                // ROI 상대 좌표를 전체 이미지 절대 좌표로 변환
                Rect rect = new Rect(
                    (int)(box.Center.X - box.Width / 2) + InspRect.X,
                    (int)(box.Center.Y - box.Height / 2) + InspRect.Y,
                    (int)box.Width,
                    (int)box.Height
                );

                _resultAreas.Add(new DrawInspectInfo(rect, obj.ClassInfo.Name, InspectType.InspAIModule, DecisionType.Defect));

                // ⭐ 추가: 불량 명칭을 ResultString에 직접 추가합니다.
                ResultString.Add(obj.ClassInfo.Name);
            }
            ResultString.Add($"Segment Count : {seg.SegmentedObjects.Length}");
        }

        

        private void HandleClassification()
        {
            // SaigeAI.InspAIModule()가 호출되면 SaigeAI 내부에 CLS Top1(label/score)이 저장됩니다.
            if (_saigeAI == null)
                return;

            // SaigeAI에서 Top1(label/score) 추출
            if (!_saigeAI.TryGetLastClsTop1(out string label, out float score))
            {
                LastClsLabel = "";
                LastClsScore = 0f;
                IsDefect = false;
                ResultString.Add("CLS Result : (no label)");
                return;
            }

            LastClsLabel = label ?? "";
            LastClsScore = score;

            bool isOk = IsOkLabel(LastClsLabel);
            IsDefect = !isOk;

            ResultString.Add($"CLS Result : {(IsDefect ? "NG" : "OK")}");
            ResultString.Add($"Label : {LastClsLabel}");
            ResultString.Add($"Score : {LastClsScore:N3}");

            string overlay = $"{LastClsLabel} ({LastClsScore:N3})";
            _resultAreas.Add(
                new DrawInspectInfo(
                    InspRect,
                    overlay,
                    InspectType.InspAIModule,
                    IsDefect ? DecisionType.Defect : DecisionType.Info
                )
            );
        }

        private bool IsOkLabel(string label)
        {
            // SaigeAI.DrawCLSResultOverlay() 기준: label이 "Good"이면 OK로 취급
            // (대소문자 차이는 허용)
            if (string.IsNullOrWhiteSpace(label))
                return false;

            return label.Trim().Equals("Good", System.StringComparison.OrdinalIgnoreCase);
        }

        public override int GetResultRect(out List<DrawInspectInfo> resultArea)
        {
            resultArea = null;
            if (!IsInspected || _resultAreas == null || _resultAreas.Count == 0)
                return -1;

            resultArea = _resultAreas;
            return resultArea.Count;
        }

        public override bool CopyFrom(InspAlgorithm sourceAlgo)
        {
            var src = sourceAlgo as AIModuleAlgorithm;
            if (src == null) return false;

            CopyBaseTo(this);
            ModelPath = src.ModelPath;
            EngineType = src.EngineType;
            return true;
        }

        public override InspAlgorithm Clone()
        {
            var clone = new AIModuleAlgorithm();
            clone.CopyFrom(this);
            return clone;
        }
    }
}