using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using _251203_WinForm_Docking.Algorithm;
using _251203_WinForm_Docking.Core;
using _251203_WinForm_Docking.Teach;

namespace _251203_WinForm_Docking.Inspect
{
    public class InspResult
    {
        public InspWindow InspObject { get; set; }

        public string GroupID { get; set; }

        public string ObjectID { get; set; }

        public InspWindowType ObjectType { get; set; }

        public InspectType InspType { get; set; }
        
        public int ErrorCode { get; set; }

        public bool IsDefect { get; set; }

        public string ResultValue { get; set; }

        public string ResultInfos { get; set; }

        [XmlIgnore]
        public List<DrawInspectInfo> ResultRectList { get; set; } = null;

        public InspResult()
        {
            InspObject = new InspWindow();
            GroupID = string.Empty;
            ObjectID = string.Empty;
            ObjectType = InspWindowType.None;
            ErrorCode = 0;
            IsDefect = false;
            ResultValue = "";
            ResultInfos = string.Empty;
        }

        public InspResult(InspWindow window, string baseIID, string objectID, InspWindowType objectType)
        {
            InspObject = window;
            GroupID = baseIID;
            ObjectID = objectID;
            ObjectType = objectType;
            ErrorCode = 0;
            IsDefect= false;
            ResultValue = "";
            ResultInfos = string.Empty;
        }
    }
}
