using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureGate.Core
{
    public enum InspectType
    {
        InspNone = -1,
        InspBinary,
        InspMatch,
        InspFilter,
        InspAIModule,
        InspTransistorRule, 
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


    public enum TransistorRoiRole
    {
        Base,   // 케이스 파손
        Body,   // 다리 빠짐 / 다리 잘림
        Sub     // 위치 불량
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

        public static readonly string PROGRAM_NAME = "PureGate";
    }
}
