using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private InspectBoard _inspectBoard = new InspectBoard();
        public bool IsRunning { get; set; } = false;

        public InspWorker() { }

        public void Stop() { _cts.Cancel(); }

        public void StartCycleInspectImage()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => InspectionLoop(this, _cts.Token));
        }


        private void InspectionLoop(InspWorker inspWorker, CancellationToken token)
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
            Model curMode = Global.Inst.InspStage.CurModel;
            List<InspWindow> inspWindowList = curMode.InspWindowList;

            try
            {
                foreach (var inspWindow in inspWindowList)
                {
                    if (inspWindow is null) continue;
                    UpdateInspData(inspWindow);
                }

                try { _inspectBoard.InspectWindowList(inspWindowList); }
                catch (Exception ex)
                {
                    SLogger.Write("Vision Server Error: " + ex.Message, SLogger.LogType.Error);
                    isDefect = true;
                }
            }
            finally
            {
                // 엔진 결과가 완전히 나올 때까지 아주 잠시 대기
                System.Threading.Thread.Sleep(200);

                int totalCnt = 0; int okCnt = 0; int ngCnt = 0;
                var ngStats = new Dictionary<string, int>();

                foreach (var inspWindow in inspWindowList)
                {
                    if (inspWindow == null) continue;
                    totalCnt++;

                    bool windowIsNG = false;
                    foreach (var algo in inspWindow.AlgorithmList)
                    {
                        if (algo.IsUse && algo.IsDefect) // 불량이 떴다면
                        {
                            windowIsNG = true;
                            isDefect = true;

                            string ngName = "Unknown";
                            List<DrawInspectInfo> areas = new List<DrawInspectInfo>();

                            // 1. 먼저 영역 정보를 시도
                            int resultCnt = algo.GetResultRect(out areas);

                            if (resultCnt > 0 && areas.Count > 0 && !string.IsNullOrEmpty(areas[0].info))
                            {
                                ngName = areas[0].info;
                            }
                            else
                            {
                                // 2. ⭐ [핵심 보강] 영역이 없으면(CLS 모델 등), 알고리즘 객체 자체에서 결과 문자열을 직접 추출 시도
                                // SaigeAI 클래스 내부에서 판정된 클래스 이름을 가져오는 경로를 강제로 지정합니다.
                                try
                                {
                                    // algo 객체가 가지고 있는 마지막 검사 결과 문자열이나 타입명을 활용
                                    // 만약 SaigeAI.cs에 결과 클래스명을 저장하는 변수가 있다면 그걸 참조해야 합니다.
                                    ngName = algo.InspectType.ToString().Replace("Insp", "");
                                }
                                catch
                                {
                                    ngName = "Defect";
                                }
                            }

                            // 딕셔너리에 추가
                            if (ngStats.ContainsKey(ngName)) ngStats[ngName]++;
                            else ngStats[ngName] = 1;
                        }
                    }

                    if (windowIsNG) ngCnt++;
                    else okCnt++;

                    DisplayResult(inspWindow, InspectType.InspNone);
                }

                // UI에 보낼 리스트 생성
                List<NgClassCount> ngDetails = ngStats.Select(kvp => new NgClassCount
                {
                    ClassName = kvp.Key,
                    Count = kvp.Value
                }).ToList();

                // 🔴 [가장 중요] 로그로 데이터가 있는지 먼저 확인 (디버그 콘솔 확인 요망)
                System.Diagnostics.Debug.WriteLine($"[DEBUG] NG Count: {ngCnt}, Details Count: {ngDetails.Count}");
                if (ngDetails.Count > 0)
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] First NG Name: {ngDetails[0].ClassName}");

                // 🔴 UI 업데이트: MainForm뿐만 아니라 StatisticForm을 직접 찾아서 쏴버림
                if (MainForm.Instance != null)
                {
                    // 1. 메인폼 UI 갱신 (전체 카운트 등)
                    MainForm.Instance.UpdateStatisticsUI(okCnt, ngCnt, ngDetails);

                    // 2. 스태티스틱 폼 직접 갱신 (혹시 모르니 이중으로 쏨)
                    var sForm = MainForm.GetDockForm<StatisticForm>();
                    if (sForm != null)
                    {
                        sForm.UpdateStatistics(okCnt, ngCnt, ngDetails);
                    }
                }

                var cameraForm = MainForm.GetDockForm<CameraForm>();
                if (cameraForm != null)
                {
                    cameraForm.SetInspResultCount(totalCnt, okCnt, ngCnt);
                    cameraForm.ShowResultOnScreen(!isDefect);
                }
            }
            return true;
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
            if (inspWindow is null) return false;
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
            if (inspObj is null) return false;
            List<DrawInspectInfo> totalArea = new List<DrawInspectInfo>();
            foreach (var algorithm in inspObj.AlgorithmList)
            {
                if (algorithm.InspectType != inspType && inspType != InspectType.InspNone) continue;
                List<DrawInspectInfo> resultArea = new List<DrawInspectInfo>();
                if (algorithm.GetResultRect(out resultArea) > 0) totalArea.AddRange(resultArea);
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