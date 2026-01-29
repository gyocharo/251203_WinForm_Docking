using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PureGate.Algorithm;
using PureGate.Core;

namespace PureGate.Teach
{
    public class InspWindowFactory
    {
        private static readonly Lazy<InspWindowFactory> _instance = new Lazy<InspWindowFactory>(() => new InspWindowFactory());

        public static InspWindowFactory Inst
        {
            get
            {
                return _instance.Value;
            }
        }

        private Dictionary<string, int> _windowTypeNo = new Dictionary<string, int>();

        public InspWindowFactory() { }

        public InspWindow Create(InspWindowType windowType, bool addAlgorithm = true)
        {
            string name, prefix;
            if (!GetWindowName(windowType, out name, out prefix))
                return null;

            InspWindow inspWindow = new InspWindow(windowType, name);
            if (inspWindow is null)
                return null;

            if (!_windowTypeNo.ContainsKey(name))
                _windowTypeNo[name] = 0;

            int curID = _windowTypeNo[name];
            curID++;

            inspWindow.UID = string.Format("{0}_{1:D6}", prefix, curID);

            _windowTypeNo[name] = curID;

            if (addAlgorithm)
                AddInspAlgorithm(inspWindow);

            return inspWindow;
        }

        private bool AddInspAlgorithm(InspWindow inspWindow)
        {
            switch (inspWindow.InspWindowType)
            {
                case InspWindowType.Sub:
                    {
                        // SUB = 미스매칭 담당
                        inspWindow.AddInspAlgorithm(InspectType.InspMatch);

                        // (원하면) SUB 위치불량도 여기서 같이
                        inspWindow.AddInspAlgorithm(InspectType.InspTransistorRule);
                        var tr = inspWindow.FindInspAlgorithm(InspectType.InspTransistorRule) as TransistorRuleAlgorithm;
                        if (tr != null) tr.TargetRole = TransistorRoiRole.Sub;
                        break;
                    }

                case InspWindowType.Base:
                    {
                        // BASE = 케이스파손
                        inspWindow.AddInspAlgorithm(InspectType.InspTransistorRule);
                        var tr = inspWindow.FindInspAlgorithm(InspectType.InspTransistorRule) as TransistorRuleAlgorithm;
                        if (tr != null) tr.TargetRole = TransistorRoiRole.Base;
                        break;
                    }

                case InspWindowType.Body:
                    {
                        // BODY = 다리빠짐/잘림
                        inspWindow.AddInspAlgorithm(InspectType.InspTransistorRule);
                        var tr = inspWindow.FindInspAlgorithm(InspectType.InspTransistorRule) as TransistorRuleAlgorithm;
                        if (tr != null) tr.TargetRole = TransistorRoiRole.Body;
                        break;
                    }
            }
            return true;
        }

        private bool GetWindowName(InspWindowType windowType, out string name, out string prefix)
        {
            name = string.Empty;
            prefix = string.Empty;
            switch (windowType)
            {
                case InspWindowType.Base:
                    name = "Base";
                    prefix = "BAS";
                    break;
                case InspWindowType.Body:
                    name = "Body";
                    prefix = "BDY";
                    break;
                case InspWindowType.Sub:
                    name = "Sub";
                    prefix = "SUB";
                    break;
                case InspWindowType.ID:
                    name = "ID";
                    prefix = "ID";
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
