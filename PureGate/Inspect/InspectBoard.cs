using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using PureGate.Algorithm;
using PureGate.Core;
using PureGate.Teach;
using PureGate.Util;

namespace PureGate.Inspect
{

    public class InspectBoard
    {
        public InspectBoard()
        {
        }

        private OpenCvSharp.Point _lastGoodAlignOffset = new OpenCvSharp.Point(0, 0);
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

                BindAlgoContext(window, algo);

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

        private static void SetPropIfExists(object obj, string propName, object value)
        {
            if (obj == null) return;

            var p = obj.GetType().GetProperty(propName);
            if (p == null || !p.CanWrite) return;

            // 타입이 안 맞으면 변환 가능한 경우만 처리
            if (value != null && !p.PropertyType.IsAssignableFrom(value.GetType()))
            {
                try { value = Convert.ChangeType(value, p.PropertyType); }
                catch { return; }
            }

            p.SetValue(obj, value, null);
        }

        private static void BindAlgoContext(InspWindow window, InspAlgorithm algo)
        {
            // ✅ UID 계열(프로젝트마다 이름이 달라도 대응)
            SetPropIfExists(algo, "UID", window.UID);
            SetPropIfExists(algo, "ObjectID", window.UID);
            SetPropIfExists(algo, "WindowUID", window.UID);

            // ✅ window 참조/타입(있으면 넣어줌)
            SetPropIfExists(algo, "ParentWindow", window);
            SetPropIfExists(algo, "InspWindow", window);
            SetPropIfExists(algo, "InspWindowType", window.InspWindowType);

            // ✅ ROI 정보(프로퍼티가 존재하면 세팅)
            SetPropIfExists(algo, "WindowArea", window.WindowArea);
            SetPropIfExists(algo, "InspArea", window.InspArea);
            SetPropIfExists(algo, "InspRect", window.InspArea);
        }


        public bool InspectWindowList(List<InspWindow> windowList)
        {
            System.Diagnostics.Debug.WriteLine("[ALGO_DUMP] InspectWindowList ENTER");
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

                                // ✅ RuleBasedAlgorithm이면 UID 주입 (Sub ROI별 Threshold용)
                                if (algo is RuleBasedAlgorithm rbAlgo)
                                    rbAlgo.ParentWindowUid = window.UID;

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

                            Debug.WriteLine($"[ALIGN] FINAL offset=({alignOffset.X},{alignOffset.Y}), outScore={matchAlgo.OutScore}");

                            const double MIN_ALIGN_SCORE = 75.0;
                            const double MAX_ALIGN_DIST = 60.0;

                            double dist = Math.Sqrt(alignOffset.X * alignOffset.X + alignOffset.Y * alignOffset.Y);

                            if (matchAlgo.OutScore < MIN_ALIGN_SCORE || dist > MAX_ALIGN_DIST)
                            {
                                Debug.WriteLine($"[ALIGN] WEAK (score={matchAlgo.OutScore}, dist={dist:F1}) -> use LAST ({_lastGoodAlignOffset.X},{_lastGoodAlignOffset.Y})");
                                alignOffset = _lastGoodAlignOffset;
                                alignWindow.InspArea = alignWindow.WindowArea + alignOffset;
                            }
                            else
                            {
                                _lastGoodAlignOffset = alignOffset; // ✅ 이번 오프셋을 저장
                            }

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

            // ✅ [DEBUG] RuleBased용 최종 ROI 좌표 출력 (Alignment 적용된 InspArea 기준)
            foreach (var window in windowList)
            {
                if (window == null) continue;

                // 현재 window의 알고리즘 중 RuleBased 찾기
                var rb = window.FindInspAlgorithm(InspectType.InspRuleBased) as RuleBasedAlgorithm;
                if (rb == null || !rb.IsUse) continue;

                // 지금 3단계에서 algo.InspRect = window.InspArea 로 덮어쓰니까,
                // 실제로 배치툴에 넣을 ROI는 "window.InspArea"가 정답임.
                var r = window.InspArea;

                SLogger.Write($"[RB_ROI] Type={rb.WindowType}, Rect={r.X},{r.Y},{r.Width},{r.Height}, UID={window.UID}");
            }

            // ✅ 여기 추가: alignWindow 외에는 MatchAlgorithm 비활성화(임시)
            foreach (var w in windowList)
            {
                var m = w.FindInspAlgorithm(InspectType.InspMatch) as MatchAlgorithm;
                if (m == null) continue;

                if (alignWindow != null && w.UID != alignWindow.UID)
                {
                    m.IsUse = false;
                    SLogger.Write($"[Alignment] MatchAlgorithm OFF (judge용 차단): {w.InspWindowType} {w.UID}");
                }
                else
                {
                    // alignWindow는 alignment에 필요하니 켜두기 + 임계값 보정
                    m.IsUse = true;
                    if (m.MatchScore > 35) m.MatchScore = 35;
                    if (m.ExtSize.Width < 200 || m.ExtSize.Height < 200)
                        m.ExtSize = new Size(Math.Max(200, m.ExtSize.Width), Math.Max(200, m.ExtSize.Height));
                }
            }


            // ===== 3단계: 정렬된 위치에서 전체 재검사 =====
            SLogger.Write($"[Alignment] === 3단계: 정렬된 위치에서 전체 재검사 ===");
            
            foreach (InspWindow window in windowList)
            {
                System.Diagnostics.Debug.WriteLine($"[ALGO_DUMP] window={window.UID}");
                SLogger.Write($"[ALGO_DUMP] window={window.UID}");

                SLogger.Write($"[ALGO_DUMP] {window.InspWindowType} {window.UID} WindowArea={window.WindowArea.X},{window.WindowArea.Y},{window.WindowArea.Width},{window.WindowArea.Height} " +
                $"InspArea={window.InspArea.X},{window.InspArea.Y},{window.InspArea.Width},{window.InspArea.Height}");

                foreach (var algo in window.AlgorithmList)
                {
                    SLogger.Write($"[ALGO_DUMP]   - {algo.GetType().Name} IsUse={algo.IsUse} Ch={algo.ImageChannel}");

                    var m = algo as MatchAlgorithm;
                    if (m != null)
                    {
                        int tmplCount = m.GetTemplateImages()?.Count ?? 0;
                        SLogger.Write($"[ALGO_DUMP]     Match: ScoreLimit={m.MatchScore}, Ext={m.ExtSize.Width}x{m.ExtSize.Height}, Templates={tmplCount}");
                    }

                    var rb = algo as RuleBasedAlgorithm;
                    if (rb != null)
                    {
                        SLogger.Write($"[ALGO_DUMP]     RuleBased: ParentWindowUid='{rb.ParentWindowUid}' WindowType={rb.WindowType}");
                    }
                }

                // ✅ 모든 알고리즘의 검사 위치를 InspArea로 업데이트
                foreach (var algo in window.AlgorithmList)
                {
                    if (!algo.IsUse) continue;
                    
                    // TeachRect는 Golden 위치 유지
                    algo.TeachRect = window.WindowArea;
                    
                    // ✅ InspRect는 정렬된 위치로 설정
                    algo.InspRect = window.InspArea;

                    // ✅ (추가) RuleBasedAlgorithm이면 UID 주입 (Sub ROI별)
                    if (algo is RuleBasedAlgorithm rbAlgo)
                        rbAlgo.ParentWindowUid = window.UID;

                    // 검사 이미지 재설정
                    Mat srcImage = Global.Inst.InspStage.GetMat(0, algo.ImageChannel);
                    algo.SetInspData(srcImage);
                    if (algo is RuleBasedAlgorithm rb)
                    {
                        rb.ParentWindowUid = window.UID;  // <-- 이거 반드시! (너 지금 정상경로엔 없음)

                        // 인스턴스 공유 확인용: "객체 identity hash"
                        // GetHashCode()는 override될 수 있으니 RuntimeHelpers가 더 안전
                        int objId = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(rb);

                        SLogger.Write(
                            $"[RB_DBG] WinType={window.InspWindowType}, UID={window.UID}, ObjId={objId}, " +
                            $"Teach=({algo.TeachRect.X},{algo.TeachRect.Y},{algo.TeachRect.Width},{algo.TeachRect.Height}), " +
                            $"Insp=({algo.InspRect.X},{algo.InspRect.Y},{algo.InspRect.Width},{algo.InspRect.Height})"
                        );
                    }
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
