using OpenCvSharp;
using PureGate.Algorithm;
using PureGate.Core;
using PureGate.Teach;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureGate.Inspect
{
    public class InspectBoard
    {
        public InspectBoard()
        {
        }

        public bool Inspect(InspWindow window)
        {
            if (window is null)
                return false;

            if (!InspectWindow(window))
                return false;

            return true;
        }

        private bool InspectWindow(InspWindow window)
        {
            window.ResetInspResult();

            // 현재 검사 이미지 파일명 가져오기
            string imageFileName = GetCurrentImageFileName();

            foreach (InspAlgorithm algo in window.AlgorithmList)
            {
                if (algo.IsUse == false)
                    continue;

                if (!algo.DoInspect())
                    return false;

                string resultInfo = string.Join("\r\n", algo.ResultString);

                InspResult inspResult = new InspResult
                {
                    ObjectID = window.UID,
                    InspType = algo.InspectType,
                    IsDefect = algo.IsDefect,
                    ResultInfos = resultInfo
                };

                // 이미지 파일명 정보 파싱
                inspResult.ParseImageFileName(imageFileName);


                switch (algo.InspectType)
                {
                    case InspectType.InspMatch:
                        MatchAlgorithm matchAlgo = algo as MatchAlgorithm;
                        inspResult.ResultValue = $"{matchAlgo.OutScore}";
                        break;
                    case InspectType.InspBinary:
                        BlobAlgorithm blobAlgo = algo as BlobAlgorithm;
                        int min = blobAlgo.BlobFilters[blobAlgo.FILTER_COUNT].min;
                        int max = blobAlgo.BlobFilters[blobAlgo.FILTER_COUNT].max;

                        inspResult.ResultValue = $"{blobAlgo.OutBlobCount}/{min}~{max}";
                        break;
                    case InspectType.InspAIModule:
                        {
                            AIModuleAlgorithm AIModuleAlgo = algo as AIModuleAlgorithm;

                            // ✅ CLS는 NG 클래스(라벨)를 Status에 표시하기 위해 ResultValue에 저장
                            if (AIModuleAlgo != null && AIModuleAlgo.EngineType == AIEngineType.CLS && AIModuleAlgo.IsDefect)
                            {
                                if (!string.IsNullOrWhiteSpace(AIModuleAlgo.LastClsLabel))
                                    inspResult.ResultValue = AIModuleAlgo.LastClsLabel;
                            }
                            break;
                        }
                }

                List<DrawInspectInfo> resultArea = new List<DrawInspectInfo>();
                int resultCnt = algo.GetResultRect(out resultArea);
                inspResult.ResultRectList = resultArea;

                window.AddInspResult(inspResult);
            }
            if (Global.Inst.InspStage.CurrentDetectMode == DetectMode.MATCH)
            {
                bool ng = JudgeByMatch(window);
                if (ng)
                    window.InspResultList.Last().IsDefect = true;
            }

            return true;
        }

        public bool InspectWindowList(List<InspWindow> windowList)
        {
            if (windowList.Count <= 0)
                return false;

            //ID 윈도우가 매칭알고리즘이 있고, 검사가 되었다면, 오프셋을 얻는다.
            Point alignOffset = new Point(0, 0);
            InspWindow idWindow = windowList.Find(w => w.InspWindowType == Core.InspWindowType.ID);
            if (idWindow != null)
            {
                MatchAlgorithm matchAlgo = (MatchAlgorithm)idWindow.FindInspAlgorithm(InspectType.InspMatch);
                if (matchAlgo != null && matchAlgo.IsUse)
                {
                    if (!InspectWindow(idWindow))
                        return false;

                    if (matchAlgo.IsInspected)
                    {
                        alignOffset = matchAlgo.GetOffset();
                        idWindow.InspArea = idWindow.WindowArea + alignOffset;
                    }
                }
            }

            foreach (InspWindow window in windowList)
            {
                //모든 윈도우에 오프셋 반영
                window.SetInspOffset(alignOffset);
                if (!InspectWindow(window))
                    return false;
            }

            if (Global.Inst.InspStage.CurrentDetectMode == DetectMode.MATCH)
            {
                bool legNg = JudgeLegGroup(windowList);
                if (legNg)
                {
                    // 다리 하나라도 NG면 전체 NG
                    foreach (var w in windowList.Where(w => w.InspWindowType == InspWindowType.Sub))
                    {
                        var last = w.InspResultList.LastOrDefault();
                        if (last != null)
                            last.IsDefect = true;
                    }
                }
            }

            return true;
        }

        // 현재 검사 중인 이미지 파일명 가져오기
        private string GetCurrentImageFileName()
        {
            try
            {
                var curModel = Global.Inst?.InspStage?.CurModel;
                if (curModel != null && !string.IsNullOrEmpty(curModel.InspectImagePath))
                {
                    return Path.GetFileName(curModel.InspectImagePath);
                }
            }
            catch (Exception)
            {
                // 예외 발생 시 빈 문자열 반환
            }

            return string.Empty;
        }

        bool JudgeByMatch(InspWindow window)
        {
            switch (window.InspWindowType)
            {
                case InspWindowType.Body:
                    return JudgeBodyDefect(window);

                case InspWindowType.Base:
                    return JudgeBaseExist(window);

                // ❌ Sub는 여기서 판단하지 않음
                default:
                    return false;
            }
        }

        bool JudgeLegGroup(List<InspWindow> windows)
        {
            var legs = windows
                .Where(w => w.InspWindowType == InspWindowType.Sub)
                .ToList();

            if (legs.Count < 3)
                return true; // 다리 부족 = NG

            foreach (var leg in legs)
            {
                // Match 결과 가져오기
                var match = leg.FindInspAlgorithm(InspectType.InspMatch) as MatchAlgorithm;
                if (match == null || !match.IsInspected)
                    return true;

                // 1️⃣ 매칭 점수 부족
                if (match.OutScore < match.MatchScore)
                    return true;

                // 2️⃣ Binary 결과 NG
                var binary = leg.FindInspAlgorithm(InspectType.InspBinary);
                if (binary != null && binary.IsDefect)
                    return true;
            }

            return false; // 3개 모두 OK
        }

        bool JudgeBodyDefect(InspWindow window)
        {
            var match = window.FindInspAlgorithm(InspectType.InspMatch) as MatchAlgorithm;
            if (match == null || !match.IsInspected)
                return true;

            // 1️⃣ 위치/존재 NG
            if (match.OutScore < match.MatchScore)
                return true;

            // 2️⃣ Binary로 깨짐/스크래치
            var binary = window.FindInspAlgorithm(InspectType.InspBinary);
            if (binary != null && binary.IsDefect)
                return true;

            return false;
        }

        bool JudgeBaseExist(InspWindow window)
        {
            var match = window.FindInspAlgorithm(InspectType.InspMatch) as MatchAlgorithm;
            if (match == null || !match.IsInspected)
                return true;

            return match.OutScore < match.MatchScore;
        }
    }
}
