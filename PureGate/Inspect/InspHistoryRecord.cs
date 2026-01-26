using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureGate.Inspect
{
    public class InspHistoryRecord
    {
        public DateTime Time { get; set; }

        public string ModelName { get; set; } = "";
        public string LotNumber { get; set; } = "";
        public string SerialID { get; set; } = "";

        public int Total { get; set; }  // 보통 1
        public int Ok { get; set; }     // 0 or 1
        public int Ng { get; set; }     // 0 or 1

        public string NgClass { get; set; } = ""; // NG일 때 클래스명(예: Crack, Scratch...)
        public float Score { get; set; } = 0f;     // (선택) CLS score
    }
}