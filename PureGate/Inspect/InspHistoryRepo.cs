using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Common.Util.Helpers;

namespace PureGate.Inspect
{
    public enum InspHistoryCategory
    {
        Rule,
        AI
    }

    public static class InspHistoryRepo
    {
        private const string DIR = @"Setup\History";

        private static string BaseDir
            => Path.Combine(Environment.CurrentDirectory, DIR);

        private static string GetFilePath(DateTime day, InspHistoryCategory cat)
        {
            Directory.CreateDirectory(BaseDir);

            string suffix = (cat == InspHistoryCategory.AI) ? "AI" : "RULE";
            return Path.Combine(BaseDir, $"{day:yyyyMMdd}_{suffix}.xml");
        }

        // ✅ 기존 API 유지: 기본은 Rule로 저장(ROI 검사 쪽)
        public static void Append(InspHistoryRecord item)
            => Append(InspHistoryCategory.Rule, item);

        // ✅ 신규: 카테고리 지정 저장
        public static void Append(InspHistoryCategory category, InspHistoryRecord item)
        {
            string path = GetFilePath(item.Time.Date, category);

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

        // ✅ 기존 API 유지: 기본은 Rule 로드
        public static List<InspHistoryRecord> LoadRange(DateTime fromDate, DateTime toDateInclusive, string modelName = "")
            => LoadRange(InspHistoryCategory.Rule, fromDate, toDateInclusive, modelName);

        // ✅ 신규: 카테고리 지정 로드
        public static List<InspHistoryRecord> LoadRange(InspHistoryCategory category, DateTime fromDate, DateTime toDateInclusive, string modelName = "")
        {
            var result = new List<InspHistoryRecord>();

            DateTime d = fromDate.Date;
            DateTime end = toDateInclusive.Date;

            while (d <= end)
            {
                string path = GetFilePath(d, category);
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

            DateTime from = fromDate.Date;
            DateTime toExclusive = toDateInclusive.Date.AddDays(1);

            var q = result.Where(x => x.Time >= from && x.Time < toExclusive);

            if (!string.IsNullOrWhiteSpace(modelName))
                q = q.Where(x => string.Equals(x.ModelName, modelName, StringComparison.OrdinalIgnoreCase));

            return q.OrderByDescending(x => x.Time).ToList();
        }
    }
}
