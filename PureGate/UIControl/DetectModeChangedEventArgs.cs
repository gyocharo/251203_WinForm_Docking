using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PureGate.Core;

namespace PureGate.UIControl
{
    public class DetectModeChangedEventArgs : EventArgs
    {
        public DetectMode Mode { get; }
        public DetectModeChangedEventArgs(DetectMode mode) => Mode = mode;
    }
}

