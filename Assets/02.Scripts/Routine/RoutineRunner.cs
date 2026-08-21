// ========================
// 선정된 일과 미니게임을 순서대로 실행하는 런타임 관리자
// 명령 표시, 제한시간, 판정, 피드백, 결과 보고서 생성을 담당한다.
// HUD에는 이벤트로 상태를 전달하며 GameState 반영과 씬 이동은 처리하지 않는다.
// ========================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DreamOfMilitary.Routine
{
    public enum RoutineRunState
    {
        Idle = 0,
        ShowingCommand = 1,
        Playing = 2,
        ShowingFeedback = 3,
        Completed = 4
    }

    public sealed class RoutineRunner : MonoBehaviour
    {
        [Header("일과 설정")]
        [SerializeField] private RoutineConfig _config;

        [Header("미니게임 생성 위치")]
        [SerializeField] private Transform _playAreaRoot;

        private Coroutine _routineCoroutine;
        private GameObject _activeInstance;
        private IMinigame _activeMinigame;
        private MinigameJudgement _pendingJudgement;
        private bool _isRunning;
        private bool _acceptingCompletion;
        private bool _hasOutcome;
        private int _runToken;

        public bool IsRunning => _isRunning;
        public RoutineRunState State { get; private set; } = RoutineRunState.Idle;

        public event Action<RoutineRunState> StateChanged;
        public event Action<string, int, int> CommandShown;
        public event Action<float> TimeNormalizedChanged;
        public event Action<MinigameJudgement, int> FeedbackShown;
        public event Action<RoutineReport> RoutineCompleted;

        public void StartRoutine(IReadOnlyList<MinigameDef> sequence, int sessionSeed, RoutineRunMode runMode = RoutineRunMode.Routine)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("이미 일과가 진행 중입니다.");
            }

            if (_config == null)
            {
                throw new InvalidOperationException("RoutineConfig가 연결되지 않았습니다.");
            }

            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            if (sequence.Count == 0)
            {
                throw new ArgumentException("실행할 미니게임이 없습니다.", nameof(sequence));
            }

            var copiedSequence = new List<MinigameDef>(sequence.Count);

            for (var index = 0; index < sequence.Count; index++)
            {
                copiedSequence.Add(sequence[index]);
            }

            _runToken++;
            _isRunning = true;
            _routineCoroutine = StartCoroutine(RunRoutine(copiedSequence, sessionSeed, _runToken, runMode));
        }

        public void CancelRoutine()
        {
            if (!_isRunning)
            {
                return;
            }

            _runToken++;
            _acceptingCompletion = false;

            if (_routineCoroutine != null)
            {
                StopCoroutine(_routineCoroutine);
                _routineCoroutine = null;
            }

            AbortActiveMinigame();
            DestroyActiveInstance();

            _isRunning = false;
            SetState(RoutineRunState.Idle);
        }

        private void OnDisable()
        {
            CancelRoutine();
        }

        private IEnumerator RunRoutine(IReadOnlyList<MinigameDef> sequence, int sessionSeed, int runToken, RoutineRunMode runMode)
        {
            var entries = new List<RoutineEntry>(sequence.Count);

            for (var index = 0; index < sequence.Count; index++)
            {
                if (runToken != _runToken)
                {
                    yield break;
                }

                var definition = sequence[index];

                SetState(RoutineRunState.ShowingCommand);
                CommandShown?.Invoke(definition != null ? definition.CommandText : string.Empty, index + 1, sequence.Count);

                if (_config.CommandDisplaySeconds > 0f)
                {
                    yield return new WaitForSeconds(_config.CommandDisplaySeconds);
                }

                yield return RunSingleMinigame(definition, index, sequence.Count, sessionSeed, runToken, entries, runMode);
            }

            var allSuccessful = entries.Count > 0;

            for (var index = 0; index < entries.Count; index++)
            {
                if (entries[index].Judgement != MinigameJudgement.Success)
                {
                    allSuccessful = false;
                    break;
                }
            }

            var routineBonus = runMode == RoutineRunMode.Routine && allSuccessful ? _config.AllSuccessBonusPoints : 0;
            var report = new RoutineReport(entries, routineBonus);

            _routineCoroutine = null;
            _isRunning = false;

            SetState(RoutineRunState.Completed);
            RoutineCompleted?.Invoke(report);
        }

        private IEnumerator RunSingleMinigame(MinigameDef definition, int index, int totalCount,
            int sessionSeed, int runToken, List<RoutineEntry> entries, RoutineRunMode runMode)
        {
            if (definition == null)
            {
                throw new InvalidOperationException($"{index + 1}번째 미니게임 정의가 null입니다.");
            }

            if (definition.Prefab == null)
            {
                throw new InvalidOperationException($"[{definition.Id}] 미니게임 프리팹이 연결되지 않았습니다.");
            }

            var minigameId = GetMinigameId(definition, index);

            _activeInstance = Instantiate(definition.Prefab, _playAreaRoot);

            if (!TryFindMinigame(_activeInstance, out _activeMinigame, out var componentError))
            {
                throw new InvalidOperationException($"[{minigameId}] {componentError}");
            }

            _acceptingCompletion = true;
            _hasOutcome = false;

            var randomSeed = unchecked(sessionSeed + index * 397);
            var context = new MinigameContext(definition.DifficultyTier, definition.TimeLimitSeconds, randomSeed);

            _activeMinigame.Begin(context, judgement => AcceptOutcome(runToken, judgement));

            SetState(RoutineRunState.Playing);
            TimeNormalizedChanged?.Invoke(1f);

            var elapsedSeconds = 0f;

            while (!_hasOutcome && elapsedSeconds < definition.TimeLimitSeconds)
            {
                yield return null;

                if (runToken != _runToken)
                {
                    yield break;
                }

                if (_hasOutcome)
                {
                    break;
                }

                elapsedSeconds = Mathf.Min(elapsedSeconds + Time.deltaTime, definition.TimeLimitSeconds);
                TimeNormalizedChanged?.Invoke(1f - Mathf.Clamp01(elapsedSeconds / definition.TimeLimitSeconds));
            }

            _acceptingCompletion = false;

            MinigameJudgement judgement;
            var feedbackSeconds = _config.FeedbackDisplaySeconds;

            if (_hasOutcome)
            {
                judgement = _pendingJudgement;
            }
            else
            {
                TimeNormalizedChanged?.Invoke(0f);

                judgement = ResolveTimeLimitJudgement(definition);

                AbortActiveMinigame();
                feedbackSeconds = Mathf.Max(feedbackSeconds, _config.AbortCleanupGraceSeconds);
            }

            var score = runMode == RoutineRunMode.Routine && judgement == MinigameJudgement.Success ? 1 : 0;

            entries.Add(new RoutineEntry(minigameId, judgement, score, elapsedSeconds));

            SetState(RoutineRunState.ShowingFeedback);
            FeedbackShown?.Invoke(judgement, score);

            if (feedbackSeconds > 0f)
            {
                yield return new WaitForSeconds(feedbackSeconds);
            }

            DestroyActiveInstance();
        }

        private void AcceptOutcome(int runToken, MinigameJudgement judgement)
        {
            if (!_acceptingCompletion || _hasOutcome || runToken != _runToken)
            {
                return;
            }

            _pendingJudgement = judgement;
            _hasOutcome = true;
        }

        private MinigameJudgement ResolveTimeLimitJudgement(MinigameDef definition)
        {
            switch (definition.TimeLimitRule)
            {
                case MinigameTimeLimitRule.MustCompleteBeforeLimit:
                    return MinigameJudgement.Failure;

                case MinigameTimeLimitRule.SurviveUntilLimit:
                    return MinigameJudgement.Success;

                case MinigameTimeLimitRule.EvaluateAtLimit:
                    if (_activeMinigame is not ITimeLimitResolver resolver)
                    {
                        throw new InvalidOperationException(
                            $"[{definition.Id}] 제한시간 종료 판정을 제공하는 ITimeLimitResolver가 필요합니다.");
                    }

                    return resolver.ResolveAtTimeLimit();

                default:
                    throw new ArgumentOutOfRangeException(nameof(definition.TimeLimitRule));
            }
        }

        private static bool TryFindMinigame(GameObject instance, out IMinigame minigame, out string error)
        {
            minigame = null;
            error = null;
            var behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
            var componentCount = 0;

            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is not IMinigame foundMinigame)
                {
                    continue;
                }

                minigame = foundMinigame;
                componentCount++;
            }

            if (componentCount == 1)
            {
                return true;
            }

            error = componentCount == 0
                ? "프리팹에서 IMinigame 구현체를 찾을 수 없습니다."
                : "프리팹에는 IMinigame 구현체가 하나만 있어야 합니다.";

            minigame = null;
            return false;
        }

        private void AbortActiveMinigame()
        {
            if (_activeMinigame == null)
            {
                return;
            }

            try
            {
                _activeMinigame.Abort();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void DestroyActiveInstance()
        {
            if (_activeInstance != null)
            {
                Destroy(_activeInstance);
            }

            _activeInstance = null;
            _activeMinigame = null;
            _acceptingCompletion = false;
            _hasOutcome = false;
        }

        private static string GetMinigameId(MinigameDef definition, int index)
        {
            return !string.IsNullOrWhiteSpace(definition.Id) ? definition.Id : $"minigame-{index + 1}";
        }

        private void SetState(RoutineRunState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
