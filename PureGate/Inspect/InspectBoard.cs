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

            SLogger.Write($"========================================");
            SLogger.Write($"[Alignment] 🔍 3단계 검사 시작 - 총 {windowList.Count}개 윈도우");

            // ===== 1단계: Alignment 계산 =====
            Point alignOffset = new Point(0, 0);
            InspWindow alignWindow = windowList.Find(w => w.InspWindowType == Core.InspWindowType.Base);
            
            if (alignWindow == null)
            {
                alignWindow = windowList.Find(w => w.InspWindowType == Core.InspWindowType.Body);
                if (alignWindow != null)
                    SLogger.Write($"[Alignment] Base 없음 → Body를 Alignment 기준으로 사용");
            }
            else
            {
                SLogger.Write($"[Alignment] Base를 Alignment 기준으로 사용");
            }
            
            if (alignWindow != null)
            {
                SLogger.Write($"[Alignment] Alignment 윈도우: {alignWindow.UID} ({alignWindow.InspWindowType})");
                
                MatchAlgorithm matchAlgo = (MatchAlgorithm)alignWindow.FindInspAlgorithm(InspectType.InspMatch);
                if (matchAlgo != null)
                {
                    SLogger.Write($"[Alignment] MatchAlgorithm 발견 - IsUse: {matchAlgo.IsUse}");
                    
                    // ✅ 강제 최적화
                    if (!matchAlgo.IsUse)
                    {
                        matchAlgo.IsUse = true;
                        SLogger.Write($"[Alignment] → 강제 활성화");
                    }
                    
                    if (matchAlgo.ExtSize.Width < 200 || matchAlgo.ExtSize.Height < 200)
                    {
                        int newSize = Math.Max(200, Math.Max(matchAlgo.ExtSize.Width, matchAlgo.ExtSize.Height));
                        matchAlgo.ExtSize = new Size(newSize, newSize);
                        SLogger.Write($"[Alignment] → 검색 범위 확장: {newSize}x{newSize}");
                    }
                    
                    if (matchAlgo.MatchScore > 50)
                    {
                        matchAlgo.MatchScore = 35;  // 더 낮춤
                        SLogger.Write($"[Alignment] → MatchScore 임계값: {matchAlgo.MatchScore}%");
                    }
                    
                    var templates = matchAlgo.GetTemplateImages();
                    if (templates == null || templates.Count == 0)
                    {
                        SLogger.Write($"[Alignment] ❌ Template 이미지 없음!", SLogger.LogType.Error);
                        
                        // Alignment 없이 진행
                        foreach (InspWindow window in windowList)
                        {
                            window.SetInspOffset(new Point(0, 0));
                            
                            // 검사 데이터 갱신
                            foreach (var algo in window.AlgorithmList)
                            {
                                if (!algo.IsUse) continue;
                                algo.TeachRect = window.WindowArea;
                                algo.InspRect = window.WindowArea;
                                Mat algoSrcImage = Global.Inst.InspStage.GetMat(0, algo.ImageChannel);
                                algo.SetInspData(algoSrcImage);
                            }
                            
                            if (!InspectWindow(window))
                                return false;
                        }
                        SLogger.Write($"========================================");
                        return true;
                    }
                    
                    SLogger.Write($"[Alignment] Template: {templates.Count}개, 크기: {templates[0].Width}x{templates[0].Height}");
                    
                    // ✅ 1단계: Alignment만 수행 (RuleBased 제외)
                    SLogger.Write($"[Alignment] === 1단계: Offset 계산 ===");
                    
                    matchAlgo.TeachRect = alignWindow.WindowArea;
                    matchAlgo.InspRect = alignWindow.WindowArea;
                    
                    Mat alignSrcImage = Global.Inst.InspStage.GetMat(0, matchAlgo.ImageChannel);
                    if (alignSrcImage == null || alignSrcImage.Empty())
                    {
                        SLogger.Write($"[Alignment] ❌ 검사 이미지 없음!", SLogger.LogType.Error);
                        SLogger.Write($"========================================");
                        return false;
                    }
                    matchAlgo.SetInspData(alignSrcImage);
                    
                    // ✅ MatchAlgorithm만 실행
                    if (matchAlgo.DoInspect())
                    {
                        if (matchAlgo.IsInspected)
                        {
                            alignOffset = matchAlgo.GetOffset();
                            alignWindow.InspArea = alignWindow.WindowArea + alignOffset;
                            
                            SLogger.Write($"[Alignment] ✅ Offset 계산 완료!");
                            SLogger.Write($"[Alignment] OutPoint: ({matchAlgo.OutPoint.X}, {matchAlgo.OutPoint.Y})");
                            SLogger.Write($"[Alignment] TeachRect: ({matchAlgo.TeachRect.X}, {matchAlgo.TeachRect.Y})");
                            SLogger.Write($"[Alignment] ★★★ Offset: ({alignOffset.X}, {alignOffset.Y}) ★★★");
                            SLogger.Write($"[Alignment] MatchScore: {matchAlgo.OutScore}%");
                            
                            // 오프셋 검증
                            double offsetDist = Math.Sqrt(alignOffset.X * alignOffset.X + alignOffset.Y * alignOffset.Y);
                            if (offsetDist < 5)
                            {
                                SLogger.Write($"[Alignment] ⚠️ 오프셋이 매우 작습니다 (거리: {offsetDist:F1}px)");
                            }
                            else if (offsetDist > 500)
                            {
                                SLogger.Write($"[Alignment] ⚠️ 오프셋이 비정상적으로 큽니다 (거리: {offsetDist:F1}px)");
                            }
                            else
                            {
                                SLogger.Write($"[Alignment] 오프셋 거리: {offsetDist:F1}px (정상 범위)");
                            }
                        }
                        else
                        {
                            SLogger.Write($"[Alignment] ⚠️ MatchAlgorithm 검사 실행 안됨");
                        }
                    }
                    else
                    {
                        SLogger.Write($"[Alignment] ❌ MatchAlgorithm DoInspect 실패!");
                    }
                }
                else
                {
                    SLogger.Write($"[Alignment] ❌ MatchAlgorithm 없음!");
                }
            }
            else
            {
                SLogger.Write($"[Alignment] ⚠️ Base와 Body 윈도우 둘 다 없음!");
            }

            // ===== 2단계: 모든 윈도우에 Offset 적용 =====
            SLogger.Write($"[Alignment] === 2단계: 모든 윈도우에 Offset 적용 ===");
            foreach (InspWindow window in windowList)
            {
                window.SetInspOffset(alignOffset);
                SLogger.Write($"[Alignment] {window.InspWindowType} ({window.UID}): " +
                             $"({window.WindowArea.X}, {window.WindowArea.Y}) → " +
                             $"({window.InspArea.X}, {window.InspArea.Y})");
            }

            // ===== 3단계: 정렬된 위치에서 전체 재검사 =====
            SLogger.Write($"[Alignment] === 3단계: 정렬된 위치에서 전체 재검사 ===");
            
            foreach (InspWindow window in windowList)
            {
                // ✅ 모든 알고리즘의 검사 위치를 InspArea로 업데이트
                foreach (var algo in window.AlgorithmList)
                {
                    if (!algo.IsUse) continue;
                    
                    // TeachRect는 Golden 위치 유지
                    algo.TeachRect = window.WindowArea;
                    
                    // ✅ InspRect는 정렬된 위치로 설정
                    algo.InspRect = window.InspArea;
                    
                    // 검사 이미지 재설정
                    Mat srcImage = Global.Inst.InspStage.GetMat(0, algo.ImageChannel);
                    algo.SetInspData(srcImage);
                }
                
                SLogger.Write($"[Alignment] 재검사: {window.InspWindowType} at ({window.InspArea.X}, {window.InspArea.Y})");
                
                // ✅ 정렬된 위치에서 검사 실행
                if (!InspectWindow(window))
                {
                    SLogger.Write($"[Alignment] ❌ {window.InspWindowType} 재검사 실패!", SLogger.LogType.Error);
                    return false;
                }
            }

            SLogger.Write($"[Alignment] ✅ 3단계 검사 완료!");
            SLogger.Write($"========================================");
            return true;
        }

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
            }

            return string.Empty;
        }
    }
}
