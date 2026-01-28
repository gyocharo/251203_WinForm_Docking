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

            // 1) 검사 입력 데이터 갱신
            foreach (var w in inspWindowList)
            {
                if (w == null) continue;
                UpdateInspData(w);
            }

            // 2) 엔진 검사 수행
            try
            {
                _inspectBoard.InspectWindowList(inspWindowList);
            }
            catch (Exception ex)
            {
                // 엔진/서버 예외면 전체 NG로 처리
                SLogger.Write("Vision Server Error: " + ex.Message, SLogger.LogType.Error);
                isDefect = true;
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

                // 화면에 영역 표시(ROI 등)
                DisplayResult(w, InspectType.InspNone);
            }

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

        // ✅ 윈도우에서 대표 NG 이름 뽑기: area.info 우선, 그 다음 ResultString, 그 다음 InspectType
        private string ExtractNgName(InspWindow inspWindow)
        {
            if (inspWindow?.AlgorithmList == null) return "Unknown";

            foreach (var algo in inspWindow.AlgorithmList)
            {
                if (algo == null) continue;
                if (!algo.IsUse || !algo.IsDefect) continue;

                try
                {
                    List<DrawInspectInfo> areas;
                    int cnt = algo.GetResultRect(out areas);

                    if (cnt > 0 && areas != null && areas.Count > 0 && !string.IsNullOrWhiteSpace(areas[0].info))
                        return areas[0].info;

                    if (algo.ResultString != null && algo.ResultString.Count > 0 && !string.IsNullOrWhiteSpace(algo.ResultString[0]))
                        return algo.ResultString[0];
                }
                catch { }

                return algo.InspectType.ToString().Replace("Insp", "");
            }

            return "Unknown";
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
            inspWindow.PatternLearn();

            foreach (var inspAlgo in inspWindow.AlgorithmList)
            {
                inspAlgo.TeachRect = windowArea;
                inspAlgo.InspRect = windowArea;

                Mat srcImage = Global.Inst.InspStage.GetMat(0, inspAlgo.ImageChannel);
                inspAlgo.SetInspData(srcImage);
            }
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
    }
}