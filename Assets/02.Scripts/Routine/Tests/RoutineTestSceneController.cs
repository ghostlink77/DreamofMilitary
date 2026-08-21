// ========================
// RoutineTestScene에서 일과 루프를 자동 실행하고 결과를 검증하는 테스트 관리자
// 혼합 판정 일과와 전체 성공 일과를 차례대로 실행해 결과를 Console에 출력한다.
// ========================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace DreamOfMilitary.Routine.Tests
{
    public sealed class RoutineTestSceneController : MonoBehaviour
    {
        private const int MixedSeed = 12345;
        private const int AllSuccessSeed = 67890;
        private const int PointsPerSuccess = RoutineScoring.PointsPerSuccess;
        private const int RoutinePerfectBonus = 2;

        [SerializeField] private RoutineRunner _runner;
        [SerializeField] private RoutineConfig _config;
        [SerializeField] private MinigameCatalog _catalog;
        [FormerlySerializedAs("_perfectDefinition")]
        [SerializeField] private MinigameDef _allSuccessDefinition;

        private TestPhase _phase;
        private bool _allTestsPassed = true;

        private enum TestPhase
        {
            Mixed = 0,
            AllSuccess = 1,
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

        private IEnumerator RunAllSuccessTest()
        {
            yield return null;

            var sequence = new List<MinigameDef>(_config.MinigameCount);

            for (var index = 0; index < _config.MinigameCount; index++)
            {
                sequence.Add(_allSuccessDefinition);
            }

            _phase = TestPhase.AllSuccess;
            Debug.Log($"[RoutineTest] ALL SUCCESS TEST START: Count={sequence.Count}");
            _runner.StartRoutine(sequence, AllSuccessSeed);
        }

        private void OnRoutineCompleted(RoutineReport report)
        {
            if (_phase == TestPhase.Mixed)
            {
                var passed = ValidateMixedReport(report);
                _allTestsPassed &= passed;
                LogPhaseResult("MIXED", passed);
                StartCoroutine(RunAllSuccessTest());
                return;
            }

            if (_phase == TestPhase.AllSuccess)
            {
                var passed = ValidateAllSuccessReport(report);
                _allTestsPassed &= passed;
                LogPhaseResult("ALL SUCCESS", passed);
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
            var passed = Expect(report.Entries.Count == _config.MinigameCount, "혼합 일과 결과 개수가 설정값과 다릅니다.");
            var seenIds = new HashSet<string>();
            var expectedBaseTotal = 0;

            for (var index = 0; index < report.Entries.Count; index++)
            {
                var entry = report.Entries[index];
                seenIds.Add(entry.MinigameId);

                if (!TryGetExpectedResult(entry.MinigameId, out var judgement, out var basePoints))
                {
                    passed &= Expect(false, $"알 수 없는 미니게임 ID입니다: {entry.MinigameId}");
                    continue;
                }

                passed &= ValidateEntry(entry, judgement, basePoints);
                expectedBaseTotal += basePoints;
            }

            passed &= Expect(seenIds.Count == 4, "셔플백 결과에 후보 미니게임 4종이 모두 포함되지 않았습니다.");
            passed &= Expect(!report.IsAllSuccessful, "혼합 일과가 전체 성공으로 판정되었습니다.");
            passed &= Expect(report.BasePointsTotal == expectedBaseTotal, "혼합 일과 기본 상점 합계가 다릅니다.");
            passed &= Expect(report.RoutinePerfectBonus == 0, "혼합 일과에 전체 퍼펙트 보너스가 지급되었습니다.");
            passed &= Expect(report.TotalPoints == expectedBaseTotal, "혼합 일과 최종 상점이 다릅니다.");

            return passed;
        }

        private bool ValidateAllSuccessReport(RoutineReport report)
        {
            var expectedBaseTotal = _config.MinigameCount * PointsPerSuccess;
            var passed = Expect(report.Entries.Count == _config.MinigameCount, "전체 성공 결과 개수가 설정값과 다릅니다.");
            passed &= Expect(report.FailureCount == 0, "전체 성공 결과에 실패가 포함되었습니다.");
            passed &= Expect(report.SuccessCount == _config.MinigameCount, "성공 횟수가 설정값과 다릅니다.");
            passed &= Expect(report.IsAllSuccessful, "전체 성공 판정이 false입니다.");
            passed &= Expect(report.BasePointsTotal == expectedBaseTotal, "전체 성공 기본 상점이 다릅니다.");
            passed &= Expect(report.RoutinePerfectBonus == RoutinePerfectBonus, "일과 전체 성공 보너스가 2점이 아닙니다.");
            passed &= Expect(report.TotalPoints == expectedBaseTotal + RoutinePerfectBonus, "전체 성공 최종 상점이 다릅니다.");

            return passed;
        }

        private static bool TryGetExpectedResult(string id, out MinigameJudgement judgement, out int basePoints)
        {
            judgement = MinigameJudgement.Failure;
            basePoints = 0;

            switch (id)
            {
                case "routine-test-clear":
                    judgement = MinigameJudgement.Success;
                    basePoints = PointsPerSuccess;
                    return true;

                case "routine-test-survive":
                    judgement = MinigameJudgement.Success;
                    basePoints = PointsPerSuccess;
                    return true;

                case "routine-test-failure":
                    return true;

                case "routine-test-timeout":
                    return true;

                default:
                    return false;
            }
        }

        private bool ValidateEntry(RoutineEntry entry, MinigameJudgement judgement, int basePoints)
        {
            var passed = Expect(entry.Judgement == judgement, $"{entry.MinigameId}의 판정이 다릅니다.");
            passed &= Expect(entry.Score == basePoints, $"{entry.MinigameId}의 기본 상점이 다릅니다.");
            return passed;
        }

        private bool ValidateReferences()
        {
            var valid = true;
            valid &= Expect(_runner != null, "RoutineRunner가 연결되지 않았습니다.");
            valid &= Expect(_config != null, "RoutineConfig가 연결되지 않았습니다.");
            valid &= Expect(_catalog != null, "MinigameCatalog가 연결되지 않았습니다.");
            valid &= Expect(_allSuccessDefinition != null, "전체 성공 테스트용 미니게임 정의가 연결되지 않았습니다.");
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

        private static void OnFeedbackShown(MinigameJudgement judgement, int score)
        {
            Debug.Log($"[RoutineTest][Feedback] {judgement}, Points={score}");
        }
    }
}
