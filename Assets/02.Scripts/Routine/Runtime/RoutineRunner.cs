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

    [DisallowMultipleComponent]
    public sealed class RoutineRunner : MonoBehaviour
    {
        [Header("일과 설정")]
        [SerializeField] private RoutineConfig _config;

        [Header("미니게임 생성 위치")]
        [SerializeField] private Transform _playAreaRoot;

        private Coroutine _routineCoroutine;
        private GameObject _activeInstance;
        private IMinigame _activeMinigame;
        private MinigameOutcome _pendingOutcome;
        private bool _isRunning;
        private bool _acceptingCompletion;
        private bool _hasOutcome;
        private int _runToken;

        public bool IsRunning => _isRunning;
        public RoutineRunState State { get; private set; } = RoutineRunState.Idle;

        public event Action<RoutineRunState> StateChanged;
        public event Action<string, int, int> CommandShown;
        public event Action<float> TimeNormalizedChanged;
        public event Action<MinigameJudgement, ScoreBreakdown> FeedbackShown;
        public event Action<RoutineReport> RoutineCompleted;

        public void StartRoutine(IReadOnlyList<MinigameDef> sequence, int sessionSeed)
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
            _routineCoroutine = StartCoroutine(RunRoutine(copiedSequence, sessionSeed, _runToken));
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

        private IEnumerator RunRoutine(IReadOnlyList<MinigameDef> sequence, int sessionSeed, int runToken)
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

                yield return RunSingleMinigame(definition, index, sequence.Count, sessionSeed, runToken, entries);
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

            var routineBonus = allSuccessful ? _config.AllSuccessBonusPoints : 0;
            var report = new RoutineReport(entries, routineBonus);

            _routineCoroutine = null;
            _isRunning = false;

            SetState(RoutineRunState.Completed);
            RoutineCompleted?.Invoke(report);
        }

        private IEnumerator RunSingleMinigame(MinigameDef definition, int index, int totalCount,
            int sessionSeed, int runToken, List<RoutineEntry> entries)
        {
            var minigameId = GetMinigameId(definition, index);

            if (definition == null)
            {
                yield return RecordError(minigameId, "미니게임 정의가 null입니다.", entries);
                yield break;
            }

            if (definition.Prefab == null)
            {
                yield return RecordError(minigameId, "미니게임 프리팹이 연결되지 않았습니다.", entries);
                yield break;
            }

            Exception spawnError = null;

            try
            {
                _activeInstance = Instantiate(definition.Prefab, _playAreaRoot);
            }
            catch (Exception exception)
            {
                spawnError = exception;
            }

            if (spawnError != null)
            {
                Debug.LogException(spawnError, this);
                yield return RecordError(minigameId, "미니게임 프리팹 생성에 실패했습니다.", entries);
                yield break;
            }

            if (!TryFindMinigame(_activeInstance, out _activeMinigame, out var componentError))
            {
                yield return RecordError(minigameId, componentError, entries);
                DestroyActiveInstance();
                yield break;
            }

            _acceptingCompletion = true;
            _hasOutcome = false;

            var randomSeed = unchecked(sessionSeed + index * 397);
            var context = new MinigameContext(definition.DifficultyTier, definition.TimeLimitSeconds, randomSeed);

            Exception beginError = null;

            try
            {
                _activeMinigame.Begin(context, outcome => AcceptOutcome(runToken, outcome));
            }
            catch (Exception exception)
            {
                beginError = exception;
            }

            if (beginError != null)
            {
                Debug.LogException(beginError, this);
                _acceptingCompletion = false;
                AbortActiveMinigame();

                yield return RecordError(minigameId, "미니게임 시작 중 오류가 발생했습니다.", entries);
                DestroyActiveInstance();
                yield break;
            }

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

            var judgement = MinigameJudgement.Failure;
            var endReason = MinigameEndReason.TimeLimitReached;
            var feedbackSeconds = _config.FeedbackDisplaySeconds;
            Exception timeLimitError = null;

            if (_hasOutcome)
            {
                judgement = _pendingOutcome.Judgement;
                endReason = MinigameEndReason.Completed;
            }
            else
            {
                TimeNormalizedChanged?.Invoke(0f);

                try
                {
                    judgement = ResolveTimeLimitJudgement(definition);
                }
                catch (Exception exception)
                {
                    timeLimitError = exception;
                    judgement = MinigameJudgement.Failure;
                    endReason = MinigameEndReason.Error;
                }

                AbortActiveMinigame();
                feedbackSeconds = Mathf.Max(feedbackSeconds, _config.AbortCleanupGraceSeconds);
            }

            if (timeLimitError != null)
            {
                Debug.LogException(timeLimitError, this);
            }

            ScoreBreakdown score;
            Exception scoringError = null;

            try
            {
                score = RoutineScoring.Calculate(definition.BasePoints, judgement);
            }
            catch (Exception exception)
            {
                scoringError = exception;
                judgement = MinigameJudgement.Failure;
                endReason = MinigameEndReason.Error;
                score = new ScoreBreakdown(0);
            }

            if (scoringError != null)
            {
                Debug.LogException(scoringError, this);
                AbortActiveMinigame();
            }

            entries.Add(new RoutineEntry(minigameId, judgement, endReason, score, elapsedSeconds));

            SetState(RoutineRunState.ShowingFeedback);
            FeedbackShown?.Invoke(judgement, score);

            if (feedbackSeconds > 0f)
            {
                yield return new WaitForSeconds(feedbackSeconds);
            }

            DestroyActiveInstance();
        }

        private void AcceptOutcome(int runToken, MinigameOutcome outcome)
        {
            if (!_acceptingCompletion || _hasOutcome || runToken != _runToken)
            {
                return;
            }

            _pendingOutcome = outcome;
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

                    return resolver.ResolveAtTimeLimit().Judgement;

                default:
                    throw new ArgumentOutOfRangeException(nameof(definition.TimeLimitRule));
            }
        }

        private IEnumerator RecordError(string minigameId, string message, List<RoutineEntry> entries)
        {
            Debug.LogError($"[{minigameId}] {message}", this);

            var score = new ScoreBreakdown(0);
            entries.Add(new RoutineEntry(minigameId, MinigameJudgement.Failure, MinigameEndReason.Error, score, 0f));

            SetState(RoutineRunState.ShowingFeedback);
            FeedbackShown?.Invoke(MinigameJudgement.Failure, score);

            if (_config.FeedbackDisplaySeconds > 0f)
            {
                yield return new WaitForSeconds(_config.FeedbackDisplaySeconds);
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
            return definition != null && !string.IsNullOrWhiteSpace(definition.Id)
                ? definition.Id
                : $"invalid-{index + 1}";
        }

        private void SetState(RoutineRunState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
