using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Common.Util.Helpers;

namespace PureGate.Inspect
{
    public static class InspHistoryRepo
    {
        private const string DIR = @"Setup\History";

        private static string BaseDir
            => Path.Combine(Environment.CurrentDirectory, DIR);

        private static string GetFilePath(DateTime day)
        {
            Directory.CreateDirectory(BaseDir);
            return Path.Combine(BaseDir, day.ToString("yyyyMMdd") + ".xml");
        }

        public static void Append(InspHistoryRecord item)
        {
            string path = GetFilePath(item.Time.Date);

            List<InspHistoryRecord> list;
            if (File.Exists(path))
            {
                try { list = XmlHelper.LoadXml<List<InspHistoryRecord>>(path) ?? new List<InspHistoryRecord>(); }
                catch { list = new List<InspHistoryRecord>(); }
            }
            else
            {
                list = new List<InspHistoryRecord>();
            }

            list.Add(item);

            try { XmlHelper.SaveXml(path, list); }
            catch { /* 저장 실패해도 검사 흐름은 유지 */ }
        }

        public static List<InspHistoryRecord> LoadRange(DateTime fromDate, DateTime toDateInclusive, string modelName = "")
        {
            var result = new List<InspHistoryRecord>();

            DateTime d = fromDate.Date;
            DateTime end = toDateInclusive.Date;

            while (d <= end)
            {
                string path = GetFilePath(d);
                if (File.Exists(path))
                {
                    try
                    {
                        var list = XmlHelper.LoadXml<List<InspHistoryRecord>>(path);
                        if (list != null) result.AddRange(list);
                    }
                    catch { }
                }
                d = d.AddDays(1);
            }

            // 시간 포함 범위 필터 (from 00:00 ~ to 다음날 00:00 전)
            DateTime from = fromDate.Date;
            DateTime toExclusive = toDateInclusive.Date.AddDays(1);

            var q = result.Where(x => x.Time >= from && x.Time < toExclusive);

            if (!string.IsNullOrWhiteSpace(modelName))
                q = q.Where(x => string.Equals(x.ModelName, modelName, StringComparison.OrdinalIgnoreCase));

            return q.OrderByDescending(x => x.Time).ToList();
        }
    }
}
