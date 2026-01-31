using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using OpenCvSharp;
using PureGate.Algorithm;
using PureGate.Core;

namespace PureGate.RuleBasedBatch
{
    internal class Program
    {

        static int Main(string[] args)
        {
            try
            {
                var opt = Options.Parse(args);

                if (!Directory.Exists(opt.Root))
                    throw new DirectoryNotFoundException("Root not found: " + opt.Root);

                // 1) 골든 이미지 선택 (먼저!)
                string goldenPath = opt.GoldenPath;
                if (string.IsNullOrWhiteSpace(goldenPath))
                {
                    goldenPath = FindFirstImage(Path.Combine(opt.Root, "test", "good"))
                              ?? FindFirstImage(Path.Combine(opt.Root, "train", "good"));

                    if (goldenPath == null)
                        throw new Exception("Golden image not found. Provide --golden or ensure test/good or train/good exists.");
                }

                // 2) rect 결정 (골든 이미지 크기 기반: --rect 없으면 전체 이미지)
                Rect rect;
                using (var goldenFull = Cv2.ImRead(goldenPath))
                {
                    if (goldenFull.Empty())
                        throw new Exception("Golden image read failed: " + goldenPath);

                    if (opt.W <= 0 || opt.H <= 0)
                        rect = new Rect(0, 0, goldenFull.Width, goldenFull.Height);
                    else
                        rect = new Rect(opt.X, opt.Y, opt.W, opt.H);

                    ValidateRect(goldenFull, rect, "Golden");
                }

                // 3) 알고리즘 준비 (rect 확정 후)
                var algo = new RuleBasedAlgorithm
                {
                    IsUse = true,
                    WindowType = opt.WindowType,
                    InspRect = rect
                };

                // 4) 골든 세팅 (⭐ 반드시 ROI 크롭해서 넣는다)
                using (var goldenFull = Cv2.ImRead(goldenPath))
                {
                    using (var goldenRoi = new Mat(goldenFull, rect))
                    {
                        if (!algo.SetGoldenImage(goldenRoi))
                            throw new Exception("SetGoldenImage failed: " + string.Join(" | ", algo.ResultString));
                    }
                }

                // 5) 평가할 이미지 목록 수집
                var items = CollectLabeledImages(opt.Root);

                // 6) 실행 + CSV 기록
                string outCsv = Path.Combine(opt.Root,
                    $"rulebased_{opt.WindowType}_{rect.X}_{rect.Y}_{rect.Width}_{rect.Height}.csv"
);


                using (var sw = new StreamWriter(outCsv, false, new UTF8Encoding(true)))
                {
                    sw.WriteLine("file,label,pred,isDefect,score,holePixels,holeDelta,bodyDiffPixels,bodyRatio,subArea,subAreaRatio,subCx,subCxShift,log");

                    int ok = 0, total = 0;
                    foreach (var it in items)
                    {
                        total++;
                        algo.ResetResult();

                        using (var src = Cv2.ImRead(it.FilePath))
                        {
                            if (src.Empty())
                            {
                                sw.WriteLine($"\"{it.FilePath}\",{it.Label},read_fail,true,0,0,0,0,0,0,0,0,0,\"imread failed\"");
                                continue;
                            }

                            ValidateRect(src, rect, "Src");

                            algo.SetInspData(src);
                            bool inspOk = algo.DoInspect();

                            var pred = algo.DetectedNgType.ToString();
                            var isDefect = algo.IsDefect;

                            var m = ParseMetrics(algo.ResultString);

                            string log = string.Join(" / ", algo.ResultString).Replace("\"", "'");

                            sw.WriteLine(
                                $"\"{it.FilePath}\",{it.Label},{pred},{isDefect}," +
                                $"{m.Score},{m.HolePixels},{m.HoleDelta},{m.BodyDiffPixels},{m.BodyRatio}," +
                                $"{m.SubArea},{m.SubAreaRatio},{m.SubCx},{m.SubCxShift}," +
                                $"\"{log}\""
                            );

                            if (inspOk) ok++;
                        }
                    }

                    Console.WriteLine($"Done. Total={total}, DoInspectOk={ok}");
                    Console.WriteLine("CSV: " + outCsv);
                } // ✅ 여기서 파일이 닫힘!

                // ✅ 이제 읽어도 안 터짐
                PrintRecommendations(outCsv, opt.WindowType);
                Console.WriteLine("완료. 아무 키나 누르면 종료합니다...");
                Console.ReadKey();


                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] " + ex);
                return -1;
            }
        }


        // -------------------- Helpers --------------------

        private static string FindFirstImage(string dir)
        {
            if (!Directory.Exists(dir)) return null;
            return Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
                            .FirstOrDefault(f => IsImage(f));
        }

        private static bool IsImage(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp";
        }

        private static void ValidateRect(Mat img, Rect rect, string tag)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                throw new Exception($"{tag}: Invalid rect size: {rect}");

            if (rect.Right > img.Width || rect.Bottom > img.Height)
                throw new Exception($"{tag}: Rect out of range. Img={img.Width}x{img.Height}, Rect={rect}");
        }

        private class LabeledImage
        {
            public string FilePath;
            public string Label;
        }

        private static List<LabeledImage> CollectLabeledImages(string root)
        {
            // label은 폴더명에서 추출: test/good, test/misplaced, ...
            // root 아래 모든 이미지 수집하되, ground_truth는 제외
            var all = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                               .Where(IsImage)
                               .Where(p => !p.Replace('\\', '/').Contains("/ground_truth/"))
                               .ToList();

            var list = new List<LabeledImage>(all.Count);

            foreach (var f in all)
            {
                string norm = f.Replace('\\', '/');
                string label = "unknown";

                // transistor_samples 구조에 맞춰 label 추출 (test/<label>/..., train/good/.. 등)
                var parts = norm.Split('/');
                int testIdx = Array.IndexOf(parts, "test");
                int trainIdx = Array.IndexOf(parts, "train");
                if (testIdx >= 0 && testIdx + 1 < parts.Length) label = parts[testIdx + 1];
                else if (trainIdx >= 0 && trainIdx + 1 < parts.Length) label = parts[trainIdx + 1];

                list.Add(new LabeledImage { FilePath = f, Label = label });
            }

            return list;
        }

        private struct Metrics
        {
            public double Score;
            public int HolePixels;
            public int HoleDelta;
            public int BodyDiffPixels;
            public double BodyRatio;
            public double SubArea;
            public double SubAreaRatio;
            public double SubCx;
            public double SubCxShift;
        }

        private static Metrics ParseMetrics(List<string> logs)
        {
            // RuleBasedAlgorithm.ResultString에서 지표 파싱 (없으면 0)
            // 예시:
            // [Base] MatchScore=0.812
            // [Base] HolePixels=12345, Golden=12000, Delta=345
            // [Body] DiffPixels=1234, Ratio=0.0123
            // [Sub] Area=..., AreaRatio=..., CX=..., Shift=...
            var m = new Metrics();

            foreach (var line in logs)
            {
                if (line.StartsWith("[Base] MatchScore="))
                {
                    double.TryParse(line.Split('=').Last(), out m.Score);
                }
                else if (line.StartsWith("[Base] HolePixels="))
                {
                    // HolePixels=..., Golden=..., Delta=...
                    var hp = Regex.Match(line, @"HolePixels=(\d+)");
                    var hd = Regex.Match(line, @"Delta=(\d+)");
                    if (hp.Success) int.TryParse(hp.Groups[1].Value, out m.HolePixels);
                    if (hd.Success) int.TryParse(hd.Groups[1].Value, out m.HoleDelta);
                }
                else if (line.StartsWith("[Body]"))
                {
                    var dp = Regex.Match(line, @"DiffPixels=(\d+)");
                    var rr = Regex.Match(line, @"Ratio=([0-9.]+)");
                    if (dp.Success) int.TryParse(dp.Groups[1].Value, out m.BodyDiffPixels);
                    if (rr.Success) double.TryParse(rr.Groups[1].Value, out m.BodyRatio);
                }
                else if (line.StartsWith("[Sub]"))
                {
                    var area = Regex.Match(line, @"Area=([0-9.]+)");
                    var ar = Regex.Match(line, @"AreaRatio=([0-9.]+)");
                    var cx = Regex.Match(line, @"CX=([0-9.]+)");
                    var sh = Regex.Match(line, @"Shift=([0-9.]+)");
                    if (area.Success) double.TryParse(area.Groups[1].Value, out m.SubArea);
                    if (ar.Success) double.TryParse(ar.Groups[1].Value, out m.SubAreaRatio);
                    if (cx.Success) double.TryParse(cx.Groups[1].Value, out m.SubCx);
                    if (sh.Success) double.TryParse(sh.Groups[1].Value, out m.SubCxShift);
                }
            }

            return m;
        }

        private static double Percentile(List<double> values, double p)
        {
            if (values == null || values.Count == 0) return 0;
            values.Sort();
            double idx = (values.Count - 1) * p;
            int lo = (int)Math.Floor(idx);
            int hi = (int)Math.Ceiling(idx);
            if (lo == hi) return values[lo];
            double w = idx - lo;
            return values[lo] * (1 - w) + values[hi] * w;
        }

        private static void PrintRecommendations(string csvPath, InspWindowType type)
        {
            // CSV를 다시 읽어서 good의 지표 분포 기반 추천값 출력
            var lines = File.ReadAllLines(csvPath, Encoding.UTF8).Skip(1).ToList();
            if (lines.Count == 0)
            {
                Console.WriteLine("[RECOMMEND] CSV empty: " + csvPath);
                return;
            }

            // columns:
            // file,label,pred,isDefect,score,holePixels,holeDelta,bodyDiffPixels,bodyRatio,subArea,subAreaRatio,subCx,subCxShift,log
            List<double> scores = new List<double>();
            List<double> holeDeltas = new List<double>();
            List<double> bodyRatios = new List<double>();
            List<double> subAreaRatios = new List<double>();
            List<double> subCxShifts = new List<double>();

            foreach (var raw in lines)
            {
                // 매우 단순 CSV 파서(따옴표 포함) - 여기서는 마지막 log에 쉼표가 있을 수 있어 split 제한
                var parts = raw.Split(new[] { ',' }, 14);
                if (parts.Length < 14) continue;

                string label = parts[1].Trim().Trim('"').ToLowerInvariant();
                if (label != "good") continue;

                double.TryParse(parts[4], out var score);
                double.TryParse(parts[6], out var holeDelta);
                double.TryParse(parts[8], out var bodyRatio);
                double.TryParse(parts[10], out var subAreaRatio);
                double.TryParse(parts[12], out var subCxShift);

                scores.Add(score);
                holeDeltas.Add(holeDelta);
                bodyRatios.Add(bodyRatio);
                subAreaRatios.Add(subAreaRatio);
                subCxShifts.Add(subCxShift);
            }

            Console.WriteLine();
            Console.WriteLine("========== [RECOMMEND] " + type + " ==========");

            if (type == InspWindowType.Base)
            {
                double p99HoleDelta = Percentile(holeDeltas, 0.99);
                double p01Score = Percentile(scores, 0.01); // good 하위 1% 점수
                Console.WriteLine($"good holeDelta p99 = {p99HoleDelta:0}");
                Console.WriteLine($"good score   p01 = {p01Score:0.000}");
                Console.WriteLine($"추천 MisplacedHolePixelThreshold ≈ {Math.Ceiling(p99HoleDelta / 1000.0) * 1000:0} (p99 올림)");
                Console.WriteLine($"참고 MisplacedMatchScoreThreshold는 {p01Score:0.000} 근처부터 시작해서 조정");
            }
            else if (type == InspWindowType.Body)
            {
                double p99Ratio = Percentile(bodyRatios, 0.99);
                Console.WriteLine($"good bodyRatio p99 = {p99Ratio:0.0000}");
                Console.WriteLine($"추천 DamagedCase ratio threshold ≈ {p99Ratio + 0.002:0.0000} (p99 + 여유 0.002)");
            }
            else if (type == InspWindowType.Sub)
            {
                double p01AreaRatio = Percentile(subAreaRatios, 0.01); // good 하위 1%
                double p99CxShift = Percentile(subCxShifts, 0.99);
                Console.WriteLine($"good subAreaRatio p01 = {p01AreaRatio:0.000}");
                Console.WriteLine($"good subCxShift  p99 = {p99CxShift:0.0}");
                Console.WriteLine($"추천 CutLeadAreaRatioThreshold ≈ {p01AreaRatio - 0.05:0.000} (p01 - 여유 0.05)");
                Console.WriteLine($"추천 BentLeadCentroidXThreshold ≈ {p99CxShift + 2.0:0.0} (p99 + 여유 2.0)");
            }
        }

        private class Options
        {
            public string Root;
            public InspWindowType WindowType;
            public string GoldenPath;
            public int X, Y, W, H;

            public static Options Parse(string[] args)
            {
                // 기본값
                var opt = new Options
                {
                    Root = "",
                    WindowType = InspWindowType.Base,
                    GoldenPath = null,
                    X = 0,
                    Y = 0,
                    W = 0,
                    H = 0
                };

                // 아주 단순 파서
                for (int i = 0; i < args.Length; i++)
                {
                    string a = args[i].ToLowerInvariant();
                    if (a == "--root" && i + 1 < args.Length) opt.Root = args[++i];
                    else if (a == "--type" && i + 1 < args.Length)
                    {
                        string t = (args[++i] ?? "").Trim().ToLowerInvariant();

                        // 사용자가 흔히 입력하는 값들을 전부 허용
                        switch (t)
                        {
                            case "base":
                                opt.WindowType = InspWindowType.Base;
                                break;

                            case "body":
                                opt.WindowType = InspWindowType.Body;
                                break;

                            case "sub":
                                opt.WindowType = InspWindowType.Sub;
                                break;

                            // 혹시 enum 이름 그대로 넣는 경우도 허용
                            case "inspwindowtype.base":
                                opt.WindowType = InspWindowType.Base;
                                break;
                            case "inspwindowtype.body":
                                opt.WindowType = InspWindowType.Body;
                                break;
                            case "inspwindowtype.sub":
                                opt.WindowType = InspWindowType.Sub;
                                break;

                            default:
                                throw new Exception($"Unknown --type: {t}  (use base|body|sub)");
                        }
                    }

                    else if (a == "--golden" && i + 1 < args.Length) opt.GoldenPath = args[++i];
                    else if (a == "--rect" && i + 4 < args.Length)
                    {
                        opt.X = int.Parse(args[++i]);
                        opt.Y = int.Parse(args[++i]);
                        opt.W = int.Parse(args[++i]);
                        opt.H = int.Parse(args[++i]);
                    }
                }

                if (string.IsNullOrWhiteSpace(opt.Root))
                    throw new Exception("Usage: --root <dataset_root> --type base|body|sub [--rect x y w h] [--golden path]");




                return opt;
            }

           

        }
    }
}
