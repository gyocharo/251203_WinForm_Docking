using PureGate.Algorithm;
using PureGate.Core;
using PureGate.Inspect;
using SaigeVision.Net.V2;
using SaigeVision.Net.V2.Classification;
using SaigeVision.Net.V2.Detection;
using SaigeVision.Net.V2.IAD;
using SaigeVision.Net.V2.IEN;
using SaigeVision.Net.V2.OCR;
using SaigeVision.Net.V2.Segmentation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;


namespace PureGate.Algorithm
{
    public class AIModuleAlgorithm : InspAlgorithm
    {
        public string ModelPath { get; set; }
        public AIEngineType EngineType { get; set; }

        private SaigeAI _saigeAI;
        private List<DrawInspectInfo> _resultAreas;



        public AIModuleAlgorithm()
        {
            InspectType = InspectType.InspAIModule;
        }

        public override bool DoInspect()
        {
            ResetResult();

            if (!IsUse)
                return false;

            if (string.IsNullOrEmpty(ModelPath))
                return false;

            if (_srcImage == null)
                return false;

            if (InspRect.Right > _srcImage.Width ||
                InspRect.Bottom > _srcImage.Height)
                return false;

            if (_saigeAI == null)
                _saigeAI = Global.Inst.InspStage.AIModule;

            Bitmap roiBmp;
            using (Mat roiMat = new Mat(_srcImage, InspRect))
            {
                roiBmp = BitmapConverter.ToBitmap(roiMat);
            }

            _saigeAI.InspAIModule(roiBmp);

            var result = _saigeAI.GetResult();
            if (result == null)
                return false;

            _resultAreas = new List<DrawInspectInfo>();

            if (EngineType == AIEngineType.AnomalyDetection)
                HandleIAD(result as IADResult);
            else if (EngineType == AIEngineType.Detection)
                HandleDetection(result as DetectionResult);
            else if (EngineType == AIEngineType.Segmentation)
                HandleSegmentation(result as SegmentationResult);

            IsInspected = true;
            return true;
        }

        private void HandleIAD(IADResult iad)
        {
            if (iad == null)
                return;

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
            if (det == null)
                return;

            IsDefect = det.DetectedObjects.Length > 0;

            foreach (var obj in det.DetectedObjects)
            {
                Rect rect = new Rect(
                    (int)obj.BoundingBox.X,
                    (int)obj.BoundingBox.Y,
                    (int)obj.BoundingBox.Width,
                    (int)obj.BoundingBox.Height
                ) + InspRect.TopLeft;

                _resultAreas.Add(
                    new DrawInspectInfo(
                        rect,
                        obj.ClassInfo.Name,
                        InspectType.InspAIModule,
                        DecisionType.Defect
                    )
                );
            }

            ResultString.Add($"Detection Count : {det.DetectedObjects.Length}");
        }

        private void HandleSegmentation(SegmentationResult seg)
        {
            if (seg == null)
                return;

            IsDefect = seg.SegmentedObjects.Length > 0;

            foreach (var obj in seg.SegmentedObjects)
            {
                var box = obj.BoundingRotBox;

                Rect rect = new Rect(
                    (int)(box.Center.X - box.Width / 2),
                    (int)(box.Center.Y - box.Height / 2),
                    (int)box.Width,
                    (int)box.Height
                ) + InspRect.TopLeft;

                _resultAreas.Add(
                    new DrawInspectInfo(
                        rect,
                        obj.ClassInfo.Name,
                        InspectType.InspAIModule,
                        DecisionType.Defect
                    )
                );
            }

            ResultString.Add($"Segment Count : {seg.SegmentedObjects.Length}");
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
            if (src == null)
                return false;

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
