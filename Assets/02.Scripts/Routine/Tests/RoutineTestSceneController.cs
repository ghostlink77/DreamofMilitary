// ========================
// RoutineTestScene에서 일과 루프를 자동 실행하고 결과를 검증하는 테스트 관리자
// 혼합 판정 일과와 전체 퍼펙트 일과를 차례대로 실행해 결과를 Console에 출력한다.
// ========================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DreamOfMilitary.Routine.Tests
{
    public sealed class RoutineTestSceneController : MonoBehaviour
    {
        private const int MixedSeed = 12345;
        private const int PerfectSeed = 67890;
        private const int BasePoints = 10;
        private const int PerfectBonus = 5;
        private const int RoutinePerfectBonus = 25;

        [SerializeField] private RoutineRunner _runner;
        [SerializeField] private RoutineConfig _config;
        [SerializeField] private MinigameCatalog _catalog;
        [SerializeField] private MinigameDef _perfectDefinition;

        private TestPhase _phase;
        private bool _allTestsPassed = true;

        private enum TestPhase
        {
            Mixed = 0,
            AllPerfect = 1,
            Complete = 2
        }

        private void Start()
        {
            if (!ValidateReferences())
            {
                return;
            }

            _runner.StateChanged += OnStateChanged;
            _runner.CommandShown += OnCommandShown;
            _runner.FeedbackShown += OnFeedbackShown;
            _runner.RoutineCompleted += OnRoutineCompleted;

            RunMixedTest();
        }

        private void OnDestroy()
        {
            if (_runner == null)
            {
                return;
            }

            _runner.StateChanged -= OnStateChanged;
            _runner.CommandShown -= OnCommandShown;
            _runner.FeedbackShown -= OnFeedbackShown;
            _runner.RoutineCompleted -= OnRoutineCompleted;
        }

        private void RunMixedTest()
        {
            var selector = new RoutineSelector(_catalog);
            var sequence = selector.SelectRoutine(RoutineStage.PromoteToPrivateFirstClass, _config.MinigameCount, MixedSeed);

            _phase = TestPhase.Mixed;
            Debug.Log($"[RoutineTest] MIXED TEST START: Count={sequence.Count}");
            _runner.StartRoutine(sequence, MixedSeed);
        }

        private IEnumerator RunAllPerfectTest()
        {
            yield return null;

            var sequence = new List<MinigameDef>(_config.MinigameCount);

            for (var index = 0; index < _config.MinigameCount; index++)
            {
                sequence.Add(_perfectDefinition);
            }

            _phase = TestPhase.AllPerfect;
            Debug.Log($"[RoutineTest] ALL PERFECT TEST START: Count={sequence.Count}");
            _runner.StartRoutine(sequence, PerfectSeed);
        }

        private void OnRoutineCompleted(RoutineReport report)
        {
            if (_phase == TestPhase.Mixed)
            {
                var passed = ValidateMixedReport(report);
                _allTestsPassed &= passed;
                LogPhaseResult("MIXED", passed);
                StartCoroutine(RunAllPerfectTest());
                return;
            }

            if (_phase == TestPhase.AllPerfect)
            {
                var passed = ValidatePerfectReport(report);
                _allTestsPassed &= passed;
                LogPhaseResult("ALL PERFECT", passed);
                _phase = TestPhase.Complete;

                if (_allTestsPassed)
                {
                    Debug.Log("[RoutineTest] ALL TESTS PASSED");
                }
                else
                {
                    Debug.LogError("[RoutineTest] TESTS FAILED");
                }
            }
        }

        private bool ValidateMixedReport(RoutineReport report)
        {
            var passed = Expect(report.Entries.Count == _config.MinigameCount, "혼합 일과 결과 개수가 10개가 아닙니다.");
            var seenIds = new HashSet<string>();
            var expectedBaseTotal = 0;
            var expectedBonusTotal = 0;

            for (var index = 0; index < report.Entries.Count; index++)
            {
                var entry = report.Entries[index];
                seenIds.Add(entry.MinigameId);

                if (!TryGetExpectedResult(entry.MinigameId, out var judgement, out var endReason, out var basePoints, out var bonus))
                {
                    passed &= Expect(false, $"알 수 없는 미니게임 ID입니다: {entry.MinigameId}");
                    continue;
                }

                passed &= ValidateEntry(entry, judgement, endReason, basePoints, bonus);
                expectedBaseTotal += basePoints;
                expectedBonusTotal += bonus;
            }

            passed &= Expect(seenIds.Count == 4, "셔플백 결과에 후보 미니게임 4종이 모두 포함되지 않았습니다.");
            passed &= Expect(!report.IsAllPerfect, "혼합 일과가 전체 퍼펙트로 판정되었습니다.");
            passed &= Expect(report.BasePointsTotal == expectedBaseTotal, "혼합 일과 기본 상점 합계가 다릅니다.");
            passed &= Expect(report.PerfectBonusTotal == expectedBonusTotal, "혼합 일과 퍼펙트 보너스 합계가 다릅니다.");
            passed &= Expect(report.RoutinePerfectBonus == 0, "혼합 일과에 전체 퍼펙트 보너스가 지급되었습니다.");
            passed &= Expect(report.TotalPoints == expectedBaseTotal + expectedBonusTotal, "혼합 일과 최종 상점이 다릅니다.");

            return passed;
        }

        private bool ValidatePerfectReport(RoutineReport report)
        {
            var passed = Expect(report.Entries.Count == 10, "전체 퍼펙트 결과 개수가 10개가 아닙니다.");
            passed &= Expect(report.FailureCount == 0, "전체 퍼펙트 결과에 실패가 포함되었습니다.");
            passed &= Expect(report.ClearCount == 0, "전체 퍼펙트 결과에 일반 클리어가 포함되었습니다.");
            passed &= Expect(report.PerfectCount == 10, "퍼펙트 횟수가 10회가 아닙니다.");
            passed &= Expect(report.IsAllPerfect, "전체 퍼펙트 판정이 false입니다.");
            passed &= Expect(report.BasePointsTotal == 100, "전체 퍼펙트 기본 상점이 100점이 아닙니다.");
            passed &= Expect(report.PerfectBonusTotal == 50, "전체 퍼펙트 추가 상점이 50점이 아닙니다.");
            passed &= Expect(report.RoutinePerfectBonus == RoutinePerfectBonus, "일과 퍼펙트 보너스가 25점이 아닙니다.");
            passed &= Expect(report.TotalPoints == 175, "전체 퍼펙트 최종 상점이 175점이 아닙니다.");

            return passed;
        }

        private static bool TryGetExpectedResult(string id, out MinigameJudgement judgement,
            out MinigameEndReason endReason, out int basePoints, out int bonus)
        {
            judgement = MinigameJudgement.Failure;
            endReason = MinigameEndReason.Completed;
            basePoints = 0;
            bonus = 0;

            switch (id)
            {
                case "routine-test-clear":
                    judgement = MinigameJudgement.Clear;
                    basePoints = BasePoints;
                    return true;

                case "routine-test-perfect":
                    judgement = MinigameJudgement.Perfect;
                    basePoints = BasePoints;
                    bonus = PerfectBonus;
                    return true;

                case "routine-test-failure":
                    return true;

                case "routine-test-timeout":
                    endReason = MinigameEndReason.Timeout;
                    return true;

                default:
                    return false;
            }
        }

        private bool ValidateEntry(RoutineEntry entry, MinigameJudgement judgement,
            MinigameEndReason endReason, int basePoints, int bonus)
        {
            var passed = Expect(entry.Judgement == judgement, $"{entry.MinigameId}의 판정이 다릅니다.");
            passed &= Expect(entry.EndReason == endReason, $"{entry.MinigameId}의 종료 사유가 다릅니다.");
            passed &= Expect(entry.Score.BasePoints == basePoints, $"{entry.MinigameId}의 기본 상점이 다릅니다.");
            passed &= Expect(entry.Score.PerfectBonus == bonus, $"{entry.MinigameId}의 퍼펙트 보너스가 다릅니다.");
            return passed;
        }

        private bool ValidateReferences()
        {
            var valid = true;
            valid &= Expect(_runner != null, "RoutineRunner가 연결되지 않았습니다.");
            valid &= Expect(_config != null, "RoutineConfig가 연결되지 않았습니다.");
            valid &= Expect(_catalog != null, "MinigameCatalog가 연결되지 않았습니다.");
            valid &= Expect(_perfectDefinition != null, "퍼펙트 미니게임 정의가 연결되지 않았습니다.");
            return valid;
        }

        private bool Expect(bool condition, string failureMessage)
        {
            if (condition)
            {
                return true;
            }

            Debug.LogError($"[RoutineTest][FAIL] {failureMessage}", this);
            return false;
        }

        private static void LogPhaseResult(string phase, bool passed)
        {
            if (passed)
            {
                Debug.Log($"[RoutineTest] {phase} TEST PASSED");
            }
            else
            {
                Debug.LogError($"[RoutineTest] {phase} TEST FAILED");
            }
        }

        private static void OnStateChanged(RoutineRunState state)
        {
            Debug.Log($"[RoutineTest][State] {state}");
        }

        private static void OnCommandShown(string command, int current, int total)
        {
            Debug.Log($"[RoutineTest][Command] {current}/{total}: {command}");
        }

        private static void OnFeedbackShown(MinigameJudgement judgement, ScoreBreakdown score)
        {
            Debug.Log($"[RoutineTest][Feedback] {judgement}, Points={score.TotalPoints}");
        }
    }
}
