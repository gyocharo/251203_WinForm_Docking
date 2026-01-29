using OpenCvSharp;
using PureGate.Algorithm;
using PureGate.Core;
using PureGate.Teach;
using PureGate.Util;
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

            return true;
        }

        public bool InspectWindowList(List<InspWindow> windowList)
        {
            if (windowList.Count <= 0)
                return false;

            // ✅ Base 윈도우를 Alignment 기준으로 사용 (케이스 위치 찾기)
            Point alignOffset = new Point(0, 0);
            InspWindow baseWindow = windowList.Find(w => w.InspWindowType == Core.InspWindowType.Base);
            
            if (baseWindow != null)
            {
                SLogger.Write($"[Alignment] Base 윈도우 발견: {baseWindow.UID}");
                
                MatchAlgorithm matchAlgo = (MatchAlgorithm)baseWindow.FindInspAlgorithm(InspectType.InspMatch);
                if (matchAlgo != null && matchAlgo.IsUse)
                {
                    SLogger.Write($"[Alignment] Base MatchAlgorithm 활성화 상태");
                    
                    // ✅ CRITICAL: Base의 TeachRect를 WindowArea로 먼저 설정 (Golden 위치)
                    matchAlgo.TeachRect = baseWindow.WindowArea;
                    matchAlgo.InspRect = baseWindow.WindowArea;  // 검사 전에는 같은 위치
                    
                    // 검사 이미지 설정
                    Mat srcImage = Global.Inst.InspStage.GetMat(0, matchAlgo.ImageChannel);
                    matchAlgo.SetInspData(srcImage);
                    
                    SLogger.Write($"[Alignment] Base TeachRect: ({matchAlgo.TeachRect.X}, {matchAlgo.TeachRect.Y})");
                    
                    // Base 윈도우의 MatchAlgorithm으로 케이스 위치 찾기
                    if (!InspectWindow(baseWindow))
                    {
                        SLogger.Write($"[Alignment] Base 검사 실패!", SLogger.LogType.Error);
                        return false;
                    }

                    if (matchAlgo.IsInspected)
                    {
                        alignOffset = matchAlgo.GetOffset();
                        baseWindow.InspArea = baseWindow.WindowArea + alignOffset;
                        
                        SLogger.Write($"[Alignment] ✅ 오프셋 계산 완료!");
                        SLogger.Write($"[Alignment] OutPoint: ({matchAlgo.OutPoint.X}, {matchAlgo.OutPoint.Y})");
                        SLogger.Write($"[Alignment] InspRect: ({matchAlgo.InspRect.X}, {matchAlgo.InspRect.Y})");
                        SLogger.Write($"[Alignment] Offset: ({alignOffset.X}, {alignOffset.Y})");
                        SLogger.Write($"[Alignment] Score: {matchAlgo.OutScore}");
                    }
                    else
                    {
                        SLogger.Write($"[Alignment] ⚠️ MatchAlgorithm 검사 실행 안됨", SLogger.LogType.Error);
                    }
                }
                else
                {
                    if (matchAlgo == null)
                        SLogger.Write($"[Alignment] ⚠️ Base에 MatchAlgorithm 없음", SLogger.LogType.Error);
                    else
                        SLogger.Write($"[Alignment] ⚠️ Base MatchAlgorithm 비활성화", SLogger.LogType.Error);
                }
            }
            else
            {
                SLogger.Write($"[Alignment] ⚠️ Base 윈도우 없음", SLogger.LogType.Error);
            }

            // ✅ Base를 제외한 나머지 윈도우에 오프셋 적용
            foreach (InspWindow window in windowList)
            {
                if (window == baseWindow)
                    continue; // Base는 이미 검사했으므로 스킵

                // 모든 윈도우(Body, Sub)에 오프셋 반영
                window.SetInspOffset(alignOffset);
                
                SLogger.Write($"[Alignment] {window.InspWindowType} 오프셋 적용: ({window.WindowArea.X}, {window.WindowArea.Y}) → ({window.InspArea.X}, {window.InspArea.Y})");
                
                if (!InspectWindow(window))
                    return false;
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
    }
}
