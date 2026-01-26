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

namespace PureGate.Inspect
{
    public class InspWorker
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();

        private InspectBoard _inspectBoard = new InspectBoard();

        public bool IsRunning { get; set; } = false;

        public InspWorker()
        {
        }

        public void Stop()
        {
            _cts.Cancel();
        }

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

                Thread.Sleep(50); // 주기 설정
            }

            IsRunning = false;

            SLogger.Write("InspectionLoop End");
        }

        //InspStage내의 모든 InspWindow들을 검사하는 함수
        public bool RunInspect(out bool isDefect)
        {
            isDefect = false;
            Model curMode = Global.Inst.InspStage.CurModel;
            List<InspWindow> inspWindowList = curMode.InspWindowList;

            try
            {
                // 1. 데이터 업데이트
                foreach (var inspWindow in inspWindowList)
                {
                    if (inspWindow is null) continue;
                    UpdateInspData(inspWindow);
                }

                // 2. 비전 엔진 실행 (여기서 에러가 나도 튕기지 않게 개별 감쌈)
                try
                {
                    _inspectBoard.InspectWindowList(inspWindowList);
                }
                catch (Exception ex)
                {
                    // 서버 연결 끊김 로그만 찍고 넘어감
                    SLogger.Write("Vision Server Error: " + ex.Message, SLogger.LogType.Error);
                    isDefect = true; // 에러가 났으므로 결과는 NG로 간주
                }
            }
            finally
            {
                // 3. [핵심] 상단에서 에러가 나도 이곳은 '무조건' 실행됩니다.
                int totalCnt = 0; int okCnt = 0; int ngCnt = 0;
                foreach (var inspWindow in inspWindowList)
                {
                    totalCnt++;
                    if (inspWindow.IsDefect()) { isDefect = true; ngCnt++; }
                    else { okCnt++; }
                    DisplayResult(inspWindow, InspectType.InspNone);
                }

                var cameraForm = MainForm.GetDockForm<CameraForm>();
                if (cameraForm != null)
                {
                    cameraForm.SetInspResultCount(totalCnt, okCnt, ngCnt);

                    // 이 로그가 뜨는지 다시 확인하세요. 무조건 떠야 합니다.
                    string finalResult = isDefect ? "NG" : "OK";
                    SLogger.Write($"UI_CHECK: Result is {finalResult}", SLogger.LogType.Info);

                    cameraForm.ShowResultOnScreen(!isDefect);
                }
            }
            return true;
        }

        //특정 InspWindow에 대한 검사 진행
        //inspType이 있다면 그것만을 검사하고, 없다면 InpsWindow내의 모든 알고리즘 검사
        public bool TryInspect(InspWindow inspObj, InspectType inspType)
        {
            if (inspObj != null)
            {
                if (!UpdateInspData(inspObj))
                    return false;

                _inspectBoard.Inspect(inspObj);

                DisplayResult(inspObj, inspType);
            }
            else
            {
                bool isDefect = false;
                RunInspect(out isDefect);
            }

            ResultForm resultForm = MainForm.GetDockForm<ResultForm>();
            if (resultForm != null)
            {
                if (inspObj != null)
                    resultForm.AddWindowResult(inspObj);
                else
                {
                    Model curMode = Global.Inst.InspStage.CurModel;
                    resultForm.AddModelResult(curMode);
                }
            }

            return true;
        }

        //각 알고리즘 타입 별로 검사에 필요한 데이터를 입력하는 함수
        private bool UpdateInspData(InspWindow inspWindow)
        {
            if (inspWindow is null)
                return false;

            Rect windowArea = inspWindow.WindowArea;

            inspWindow.PatternLearn();

            foreach (var inspAlgo in inspWindow.AlgorithmList)
            {
                //검사 영역 초기화
                inspAlgo.TeachRect = windowArea;
                inspAlgo.InspRect = windowArea;

                Mat srcImage = Global.Inst.InspStage.GetMat(0, inspAlgo.ImageChannel);
                inspAlgo.SetInspData(srcImage);
            }

            return true;
        }

        //InspWindow내의 알고리즘 중에서, 인자로 입력된 알고리즘과 같거나,
        //인자가 None이면 모든 알고리즘의 검사 결과(Rect 영역)를 얻어, cameraForm에 출력한다.
        private bool DisplayResult(InspWindow inspObj, InspectType inspType)
        {
            if (inspObj is null)
                return false;

            List<DrawInspectInfo> totalArea = new List<DrawInspectInfo>();

            List<InspAlgorithm> inspAlgorithmList = inspObj.AlgorithmList;
            foreach (var algorithm in inspAlgorithmList)
            {
                if (algorithm.InspectType != inspType && inspType != InspectType.InspNone)
                    continue;

                List<DrawInspectInfo> resultArea = new List<DrawInspectInfo>();
                int resultCnt = algorithm.GetResultRect(out resultArea);
                if (resultCnt > 0)
                {
                    totalArea.AddRange(resultArea);
                }
            }

            if (totalArea.Count > 0)
            {
                //찾은 위치를 이미지상에서 표시
                var cameraForm = MainForm.GetDockForm<CameraForm>();
                if (cameraForm != null)
                {
                    cameraForm.AddRect(totalArea);
                }
            }

            return true;
        }
    }
}
