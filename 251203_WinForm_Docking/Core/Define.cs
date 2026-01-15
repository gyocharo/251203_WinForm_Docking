using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _251203_WinForm_Docking.Core
{
    public enum InspectType
    {
        InspNone = -1,
        InspBinary,
        InspMatch,
        InspFilter,
        InspAIModule,
        InspCount
    }

    public enum InspWindowType
    {
        None = 0,
        Base,
        Body,
        Sub,
        ID
    }
    public enum DecisionType
    {
        None = 0,
        Good,
        Defect,
        Info,
        Error,
        Timeout
    }

    public enum WorkingState
    {
        NONE = 0,
        INSPECT,
        LIVE,
        ALARM
    }

    public static class Define
    {
        public static readonly string ROI_IMAGE_NAME = "RoiImage.png";

        public static readonly string PROGRAM_NAME = "Windocking";
    }
}
