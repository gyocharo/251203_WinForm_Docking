using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PureGate.Algorithm;
using PureGate.Core;
using PureGate.Teach;
using PureGate.Util;
using OpenCvSharp;
using System.Windows.Forms;
using System.Reflection;

namespace PureGate.Inspect
{
    public class InspWorker
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly InspectBoard _inspectBoard = new InspectBoard();

        public bool IsRunning { get; private set; } = false;

        public InspWorker() { }

        public void Stop()
        {
            try { _cts?.Cancel(); } catch { }
        }

        public void StartCycleInspectImage()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => InspectionLoop(_cts.Token));
        }

        private void InspectionLoop(CancellationToken token)
        {
            Global.Inst.InspStage.SetWorkingState(WorkingState.INSPECT);
            SLogger.Write("InspectionLoop Start");
            IsRunning = true;

            while (!token.IsCancellationRequested)
            {
                Global.Inst.InspStage.OneCycle();
                Thread.Sleep(50);
            }

            IsRunning = false;
            SLogger.Write("InspectionLoop End");
        }

        private string ExtractNgName(InspWindow w)
        {
            if (w == null) return "NG";

            var all = new List<DrawInspectInfo>();

            if (w.AlgorithmList != null)
            {
                foreach (var algorithm in w.AlgorithmList)
                {
                    if (algorithm == null) continue;
                    if (!algorithm.IsUse) continue;

                    List<DrawInspectInfo> areas;
                    if (algorithm.GetResultRect(out areas) > 0 && areas != null && areas.Count > 0)
                        all.AddRange(areas);
                }
            }

            // 결과Rect가 없으면 ResultString에서라도 하나 뽑기(보강)
            if (all.Count == 0 && w.InspResultList != null && w.InspResultList.Count > 0)
            {
                // InspResultList의 Defect 라벨 같은 걸 쓰고 싶으면 여기서 뽑아도 됨
                // 일단 기본 "NG"
            }

            return ExtractNgName(all);
        }

        public bool RunInspect(out bool isDefect)
        {
            isDefect = false;

            Model curModel = Global.Inst.InspStage.CurModel;
            List<InspWindow> inspWindowList = curModel?.InspWindowList;

            if (curModel == null || inspWindowList == null || inspWindowList.Count == 0)
            {
                SLogger.Write("[InspWorker] RunInspect aborted - model/windows null", SLogger.LogType.Error);
                return false;
            }

            SLogger.Write($"[InspWorker] RunInspect started - Model: {curModel.ModelName}, Windows: {inspWindowList.Count}");

            // 1) Base/Body/Sub 찾기
            var windows = inspWindowList;
            var baseWin = windows.FirstOrDefault(w => w != null && w.InspWindowType == InspWindowType.Base);
            var bodyWin = windows.FirstOrDefault(w => w != null && w.InspWindowType == InspWindowType.Body);
            var subWin = windows.FirstOrDefault(w => w != null && w.InspWindowType == InspWindowType.Sub);

            if (baseWin == null)
            {
                SLogger.Write("[InspWorker] Base window not found. Inspect aborted.", SLogger.LogType.Error);
                return false;
            }

            // 2) 원래 ROI 위치 백업(검사 끝나면 반드시 원복)
            Rect baseOrg = baseWin.WindowArea;
            Rect bodyOrg = default;
            Rect subOrg = default;
            bool hasBody = (bodyWin != null);
            bool hasSub = (subWin != null);
            if (hasBody) bodyOrg = bodyWin.WindowArea;
            if (hasSub) subOrg = subWin.WindowArea;

            // 3) Base에서 Match 먼저 수행해서 offset 얻기
            OpenCvSharp.Point offset = new OpenCvSharp.Point(0, 0);

            try
            {
                // Match가 동작하려면 Base 알고리즘에 InspData가 먼저 들어가야 함
                if (subWin == null)
                {
                    SLogger.Write("[InspWorker] Sub window not found. Inspect aborted.", SLogger.LogType.Error);
                    return false;
                }

                // Match가 동작하려면 Sub 알고리즘에 InspData가 먼저 들어가야 함
                UpdateInspData(subWin);

                var match = subWin.FindInspAlgorithm(InspectType.InspMatch) as MatchAlgorithm;
                if (match != null && match.IsUse)
                {
                    // Match만 먼저 실행 (InspectBoard 전체 실행 전에)
                    // DoInspect()가 public이 아니면 이 줄이 컴파일이 안 될 수 있음 -> 그 경우 알려줘, 다른 방식으로 바꿔줄게.
                    match.DoInspect();

                    double score = TryGetMatchScore(match);
                    SLogger.Write($"[MatchScore(Sub)] score={score}");

                    double minScore = 0.80; // 일단 임시값(튜닝 필요)
                    if (!double.IsNaN(score) && score < minScore)
                    {
                        match.IsDefect = true;   // 강제 NG
                    }
                    if (match.IsDefect) isDefect = true;

                    offset = TryGetMatchOffset(match);
                    SLogger.Write($"[MatchOffset] dx={offset.X}, dy={offset.Y}");
                }
                else
                {
                    SLogger.Write("[InspWorker] MatchAlgorithm not found/disabled on Base. Offset = (0,0)");
                }
            }
            catch (Exception ex)
            {
                // Match가 실패해도 일단 offset=0으로 진행(룰에서 Mismatch 처리 가능)
                SLogger.Write($"[InspWorker] Match pre-run failed: {ex.Message}", SLogger.LogType.Error);
                offset = new OpenCvSharp.Point(0, 0);
            }

            // 4) offset을 Base/Body/Sub에 "검사 1회 동안만" 임시 적용
            try
            {
                baseWin.OffsetMove(offset);
                if (hasBody) bodyWin.OffsetMove(offset);
                if (hasSub) subWin.OffsetMove(offset);

                // 5) 이제 실제 검사 입력 데이터 갱신
                //    - Base는 offset 적용된 rect로 다시 세팅(패턴런은 다시 안 돌리기)
                UpdateInspDataNoPattern(baseWin);

                //    - Body/Sub는 알고리즘이 없더라도 PatternLearn 같은 side-effect를 피하기 위해 NoPattern 권장
                //      (현재 Body/Sub에 알고리즘을 안 붙이는 구조라면 이건 사실상 생략 가능)
                if (hasBody) UpdateInspDataNoPattern(bodyWin);
                if (hasSub) UpdateInspDataNoPattern(subWin);

                // 6) 엔진 검사 수행 (Rule/AI는 Base에만 붙어 있으니 "1번만" 실행됨)
                try
                {
                    _inspectBoard.InspectWindowList(windows);
                    ForceRunLocalAlgorithms(baseWin, ref isDefect);
                    if (hasBody) ForceRunLocalAlgorithms(bodyWin, ref isDefect);
                    if (hasSub) ForceRunLocalAlgorithms(subWin, ref isDefect);
                }
                catch (Exception ex)
                {
                    SLogger.Write("Vision Server Error: " + ex.Message, SLogger.LogType.Error);
                    isDefect = true;
                }
            }
            finally
            {
                // 7) 반드시 원복 (Teach 좌표 안 망가지게)
                baseWin.WindowArea = baseOrg;
                if (hasBody) bodyWin.WindowArea = bodyOrg;
                if (hasSub) subWin.WindowArea = subOrg;
            }

            // 3) 결과가 UI에 반영되기 전에 아주 짧게 대기(기존 코드 유지)
            Thread.Sleep(200);

            // 4) 결과 집계
            int totalCnt = 0;
            int okCnt = 0;
            int ngCnt = 0;

            var ngStats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var w in inspWindowList)
            {
                if (w == null) continue;
                totalCnt++;

                bool windowNg = false;

                // 윈도우 결과 기준(여러 알고리즘 중 하나라도 불량이면 NG)
                if (w.InspResultList != null && w.InspResultList.Count > 0)
                    windowNg = w.InspResultList.Any(r => r.IsDefect);

                // 혹시 InspResultList가 비었거나 믿기 어렵다면 알고리즘도 같이 확인(보강)
                if (!windowNg && w.AlgorithmList != null)
                    windowNg = w.AlgorithmList.Any(a => a.IsUse && a.IsDefect);

                if (windowNg)
                {
                    ngCnt++;
                    isDefect = true;

                    // 대표 NG 이름 1개 뽑기
                    string ngName = ExtractNgName(w);
                    if (string.IsNullOrWhiteSpace(ngName)) ngName = "Unknown";

                    if (ngStats.ContainsKey(ngName)) ngStats[ngName]++;
                    else ngStats[ngName] = 1;
                }
                else
                {
                    okCnt++;
                }
            }
            // 모든 윈도우 결과를 한 번에 그리기(덮어쓰기 방지)
            var allAreas = new List<DrawInspectInfo>();


            // (B) 각 알고리즘 결과 Rect 추가 - 불량만
            foreach (var w in inspWindowList)
            {
                if (w == null) continue;

                foreach (var algorithm in w.AlgorithmList)
                {
                    if (algorithm == null) continue;

                    List<DrawInspectInfo> areas;
                    if (algorithm.GetResultRect(out areas) > 0 && areas != null)
                    {
                        allAreas.AddRange(areas.Where(a => a.decision == DecisionType.Defect));
                    }
                }
            }

            var cameraForm2 = MainForm.GetDockForm<CameraForm>();
            if (cameraForm2 != null && allAreas.Count > 0)
                cameraForm2.AddRect(allAreas);

            // 5) UI 업데이트 데이터 구성
            List<NgClassCount> ngDetails = ngStats
                .Select(kvp => new NgClassCount { ClassName = kvp.Key, Count = kvp.Value })
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[DEBUG] total={totalCnt}, ok={okCnt}, ng={ngCnt}, ngDetails={ngDetails.Count}");

            // 6) ResultForm 업데이트
            ResultForm resultForm = MainForm.GetDockForm<ResultForm>();
            if (resultForm != null)
            {
                SLogger.Write("[InspWorker] ResultForm found -> AddModelResult()");
                resultForm.AddModelResult(curModel);
            }
            else
            {
                SLogger.Write("[InspWorker] ResultForm is null", SLogger.LogType.Error);
            }

            // 7) 통계 업데이트(한 군데로 통일)
            MainForm.Instance?.UpdateStatisticsUI(okCnt, ngCnt, ngDetails);

            var sForm = MainForm.GetDockForm<StatisticForm>();
            if (sForm != null)
                sForm.UpdateStatistics(okCnt, ngCnt, ngDetails);

            // 8) CameraForm 업데이트
            var cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.SetInspResultCount(totalCnt, okCnt, ngCnt);

                string finalResult = isDefect ? "NG" : "OK";
                SLogger.Write($"UI_CHECK: Result is {finalResult}", SLogger.LogType.Info);

                // 형광 OK/NG(화면 상단 표시) 컨트롤(원하면 여기 주석처리 가능)
                cameraForm.ShowResultOnScreen(!isDefect);
            }

            SLogger.Write("[InspWorker] RunInspect completed");
            return true;
        }

        private void ForceRunLocalAlgorithms(InspWindow w, ref bool isDefect)
        {
            if (w == null || w.AlgorithmList == null) return;

            foreach (var a in w.AlgorithmList)
            {
                if (a == null) continue;
                if (!a.IsUse) continue;

                // 로컬(OpenCV)로 만든 알고리즘만 강제 실행
                if (a is TransistorRuleAlgorithm || a is MatchAlgorithm)
                {
                    try
                    {
                        a.DoInspect();

                        SLogger.Write(
                            $"[LocalRun] {w.InspWindowType} {a.GetType().Name} Type={a.InspectType} Defect={a.IsDefect} Result={string.Join(",", a.ResultString)}");

                        if (a.IsDefect) isDefect = true;
                    }
                    catch (Exception ex)
                    {
                        SLogger.Write($"[LocalRun] {w.InspWindowType} {a.GetType().Name} crashed: {ex.Message}", SLogger.LogType.Error);
                        isDefect = true;
                    }
                }
            }
        }

        // ✅ 윈도우에서 대표 NG 이름 뽑기: area.info 우선, 그 다음 ResultString, 그 다음 InspectType
        private string ExtractNgName(List<DrawInspectInfo> areas)
        {
            if (areas == null || areas.Count == 0) return "NG";

            // 1) Defect 라벨 우선
            var defect = areas.FirstOrDefault(a =>
                a.decision == DecisionType.Defect &&
                !string.IsNullOrWhiteSpace(a.info) &&
                a.info != "BodyROI" && a.info != "LegROI" && a.info != "Lead" // 의미없는 라벨 필터(네가 쓰는 라벨에 맞춰 수정)
            );
            if (defect != null) return defect.info;

            // 2) 그 다음 의미있는 info
            var any = areas.FirstOrDefault(a =>
                !string.IsNullOrWhiteSpace(a.info) &&
                a.info != "BodyROI" && a.info != "LegROI" && a.info != "Lead"
            );
            if (any != null) return any.info;

            return "NG";
        }

        public bool TryInspect(InspWindow inspObj, InspectType inspType)
        {
            if (inspObj != null)
            {
                if (!UpdateInspData(inspObj)) return false;
                _inspectBoard.Inspect(inspObj);
                DisplayResult(inspObj, inspType);
            }
            else
            {
                bool isDef = false;
                RunInspect(out isDef);
            }

            ResultForm resultForm = MainForm.GetDockForm<ResultForm>();
            if (resultForm != null)
            {
                if (inspObj != null) resultForm.AddWindowResult(inspObj);
                else resultForm.AddModelResult(Global.Inst.InspStage.CurModel);
            }
            return true;
        }

        private bool UpdateInspData(InspWindow inspWindow)
        {
            if (inspWindow == null) return false;

            Rect windowArea = inspWindow.WindowArea;

            // 1) 먼저 Rect + 이미지 세팅
            foreach (var inspAlgo in inspWindow.AlgorithmList)
            {
                inspAlgo.TeachRect = windowArea;
                inspAlgo.InspRect = windowArea;

                Mat srcImage = Global.Inst.InspStage.GetMat(0, inspAlgo.ImageChannel);
                inspAlgo.SetInspData(srcImage);
            }

            // 2) 그 다음 패턴 학습(윈도우/알고리즘이 올바른 ROI를 보고 만들게)
            inspWindow.PatternLearn();

            return true;
        }

        private bool DisplayResult(InspWindow inspObj, InspectType inspType)
        {
            if (inspObj == null) return false;

            List<DrawInspectInfo> totalArea = new List<DrawInspectInfo>();

            foreach (var algorithm in inspObj.AlgorithmList)
            {
                if (algorithm.InspectType != inspType && inspType != InspectType.InspNone) continue;

                List<DrawInspectInfo> resultArea;
                if (algorithm.GetResultRect(out resultArea) > 0)
                    totalArea.AddRange(resultArea);
            }

            if (totalArea.Count > 0)
            {
                var cameraForm = MainForm.GetDockForm<CameraForm>();
                if (cameraForm != null) cameraForm.AddRect(totalArea);
            }

            return true;
        }

        private void UpdateInspDataNoPattern(InspWindow inspWindow)
        {
            if (inspWindow == null) return;

            Rect windowArea = inspWindow.WindowArea;

            foreach (var inspAlgo in inspWindow.AlgorithmList)
            {
                inspAlgo.TeachRect = windowArea;
                inspAlgo.InspRect = windowArea;

                Mat srcImage = Global.Inst.InspStage.GetMat(0, inspAlgo.ImageChannel);
                inspAlgo.SetInspData(srcImage);
            }
        }

        private OpenCvSharp.Point TryGetMatchOffset(MatchAlgorithm matchAlgo)
        {
            if (matchAlgo == null) return new OpenCvSharp.Point(0, 0);

            // 1) 프로퍼티/필드 후보 이름들 (프로젝트마다 다를 수 있음)
            string[] candidates = new[]
            {
        "Offset", "MatchOffset", "ResultOffset", "FoundOffset", "Delta",
        "OffsetPoint", "GetOffsetPoint", "GetResultOffset", "GetOffset"
    };

            Type t = matchAlgo.GetType();

            // 2) 메서드 형태: GetOffsetPoint() / GetResultOffset() 등
            foreach (var name in candidates)
            {
                var mi = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (mi != null && mi.GetParameters().Length == 0)
                {
                    try
                    {
                        object ret = mi.Invoke(matchAlgo, null);
                        if (ret is OpenCvSharp.Point p) return p;
                        if (ret is System.Drawing.Point dp) return new OpenCvSharp.Point(dp.X, dp.Y);
                    }
                    catch { }
                }
            }

            // 3) 프로퍼티 형태: Offset / MatchOffset 등
            foreach (var name in candidates)
            {
                var pi = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi != null)
                {
                    try
                    {
                        object ret = pi.GetValue(matchAlgo);
                        if (ret is OpenCvSharp.Point p) return p;
                        if (ret is System.Drawing.Point dp) return new OpenCvSharp.Point(dp.X, dp.Y);
                    }
                    catch { }
                }
            }

            // 4) 흔한 숫자 필드/프로퍼티 조합: OffsetX/OffsetY, dx/dy 등
            int TryGetInt(string propOrField)
            {
                var pi = t.GetProperty(propOrField, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi != null)
                {
                    try { return Convert.ToInt32(pi.GetValue(matchAlgo)); } catch { }
                }
                var fi = t.GetField(propOrField, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fi != null)
                {
                    try { return Convert.ToInt32(fi.GetValue(matchAlgo)); } catch { }
                }
                return int.MinValue;
            }

            int ox = TryGetInt("OffsetX");
            int oy = TryGetInt("OffsetY");
            if (ox != int.MinValue && oy != int.MinValue) return new OpenCvSharp.Point(ox, oy);

            ox = TryGetInt("dx");
            oy = TryGetInt("dy");
            if (ox != int.MinValue && oy != int.MinValue) return new OpenCvSharp.Point(ox, oy);

            ox = TryGetInt("DeltaX");
            oy = TryGetInt("DeltaY");
            if (ox != int.MinValue && oy != int.MinValue) return new OpenCvSharp.Point(ox, oy);

            // 못 찾으면 0,0
            return new OpenCvSharp.Point(0, 0);
        }


        private double TryGetMatchScore(MatchAlgorithm matchAlgo)
        {
            if (matchAlgo == null) return double.NaN;

            // 프로젝트마다 이름 다를 수 있으니 후보로 탐색
            string[] candidates = new[]
            {
        "Score", "MatchScore", "ResultScore", "Similarity", "MatchRate",
        "BestScore", "MaxScore"
    };

            var t = matchAlgo.GetType();

            // 프로퍼티
            foreach (var name in candidates)
            {
                var pi = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi != null)
                {
                    try { return Convert.ToDouble(pi.GetValue(matchAlgo)); } catch { }
                }
            }

            // 필드
            foreach (var name in candidates)
            {
                var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fi != null)
                {
                    try { return Convert.ToDouble(fi.GetValue(matchAlgo)); } catch { }
                }
            }

            return double.NaN;
        }
    }
}