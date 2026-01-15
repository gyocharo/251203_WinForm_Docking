using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _251203_WinForm_Docking.Algorithm;
using _251203_WinForm_Docking.Core;
using OpenCvSharp;

namespace _251203_WinForm_Docking.Teach
{
    public class InspWindow
    {
        public InspWindowType InspWindowType { get; set; }

        public string Name { get; set; }
        public string UID { get; set; }

        public Rect WindowArea { get; set; }
        public Rect InspArea { get; set; }
        public bool IsTeach { get; set; } = false;

        public List<InspAlgorithm> AlgorithmList { get; set; } = new List<InspAlgorithm>();
        
        public InspWindow()
        {

        }

        public InspWindow(InspWindowType windowType, string name)
        {
            InspWindowType = windowType;
            Name = name;
        }

        public InspWindow Clone(OpenCvSharp.Point offset, bool includeChildern = true)
        {
            InspWindow cloneWindow = InspWindowFactory.Inst.Create(this.InspWindowType, false);
            cloneWindow.WindowArea = this.WindowArea + offset;
            cloneWindow.IsTeach = false;

            foreach(InspAlgorithm algo in AlgorithmList)
            {
                var cloneAlgo = algo.Clone();
                cloneWindow.AlgorithmList.Add(cloneAlgo);
            }

            return cloneWindow;
        }

        public bool AddInspAlgorithm(InspectType inspType)
        {
            InspAlgorithm inspAlgo = null;

            switch (inspType)
            {
                case InspectType.InspBinary:
                    inspAlgo = new BlobAlgorithm();
                    break;
            }

            if (inspAlgo is null)
                return false;

            AlgorithmList.Add(inspAlgo);

            return true;
        }

        public InspAlgorithm FindInspAlgorithm(InspectType inspType)
        {
            return AlgorithmList.Find(algo => algo.InspectType == inspType);
        }

        public virtual bool DoInspect(InspectType inspType)
        {
            foreach(var inspAlgo in AlgorithmList)
            {
                if (inspAlgo.InspectType == inspType || inspType == InspectType.InspNone)
                    inspAlgo.DoInspect();
            }
            return true;
        }

        public bool IsDefect()
        {
            foreach(InspAlgorithm algo in AlgorithmList)
            {
                if (!algo.IsInspected)
                    continue;
                if (algo.IsDefect)
                    return true;
            }
            return true;
        }

        public virtual bool OffsetMove(OpenCvSharp.Point offset)
        {
            Rect windowRect = WindowArea;
            windowRect.X += offset.X;
            windowRect.Y += offset.Y;
            WindowArea = windowRect;
            return true;
        }

        public bool SetInspOffset(OpenCvSharp.Point offset)
        {
            InspArea = WindowArea + offset;
            AlgorithmList.ForEach(algo => algo.InspRect = algo.TeachRect + offset);
            return true;
        }
    }
}
