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
        InspRuleBased,  // ✅ 추가: Rule-Based 검사
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

    // ✅ NG 타입 정의
    public enum NgType
    {
        None = 0,
        Good,           // 정상
        BentLead,       // 리드 휨/벌어짐 (Sub)
        CutLead,        // 리드 잘림/짧음 (Sub)
        DamagedCase,    // 케이스 손상 (Body)
        Misplaced       // 위치 불량/리드 빠짐 (Base)
    }

    public static class Define
    {
        public static readonly string ROI_IMAGE_NAME = "RoiImage.png";

        public static readonly string PROGRAM_NAME = "PureGate";
    
         // ✅ NG 타입 한글명
        public static readonly Dictionary<NgType, string> NgTypeKorean = new Dictionary<NgType, string>
        {
            { NgType.Good, "양품" },
            { NgType.BentLead, "리드 휨" },
            { NgType.CutLead, "리드 잘림" },
            { NgType.DamagedCase, "케이스 파손" },
            { NgType.Misplaced, "위치 불량" }
        };

        // ✅ AI 클래스명 → NgType 매핑
        public static readonly Dictionary<string, NgType> ClassNameToNgType =
            new Dictionary<string, NgType>(StringComparer.OrdinalIgnoreCase)
        {
            { "Good", NgType.Good },
            { "bent_lead", NgType.BentLead },
            { "cut_lead", NgType.CutLead },
            { "damaged_case", NgType.DamagedCase },
            { "misplaced", NgType.Misplaced }
        };

        // ✅ NgType → AI 클래스명
        public static readonly Dictionary<NgType, string> NgTypeToClassName = new Dictionary<NgType, string>
        {
            { NgType.Good, "Good" },
            { NgType.BentLead, "bent_lead" },
            { NgType.CutLead, "cut_lead" },
            { NgType.DamagedCase, "damaged_case" },
            { NgType.Misplaced, "misplaced" }
        };

        // ✅ WindowType별 담당 NG 타입
        public static readonly Dictionary<InspWindowType, List<NgType>> WindowTypeToNgTypes =
            new Dictionary<InspWindowType, List<NgType>>
        {
            { InspWindowType.Base, new List<NgType> { NgType.Misplaced } },
            { InspWindowType.Body, new List<NgType> { NgType.DamagedCase } },
            { InspWindowType.Sub, new List<NgType> { NgType.BentLead, NgType.CutLead } }
        };
    }
}
