using System;
using System.Collections.Generic;
using DreamOfMilitary.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DreamOfMilitary.Routine
{
    [DisallowMultipleComponent]
    public sealed class RoutineFlowController : MonoBehaviour
    {
        public static RoutineFlowController Instance { get; private set; }

        [SerializeField] private ProgressionConfig progressionConfig;
        [SerializeField] private RoutineConfig routineConfig;
        [SerializeField] private MinigameCatalog minigameCatalog;
        [SerializeField] private string endingSceneName;

        private RoutineSelector _routineSelector;
        private IReadOnlyList<MinigameDef> _selectedRoutine;
        private RoutineRunner _activeRunner;
        private RoutineResultView _activeResultView;
        private RoutineRunMode _runMode;
        private string _lobbySceneName;
        private string _minigameSceneName;
        private int _sessionSeed;
        private int _examRequiredSuccessCount;
        private int _examSuccessCount;
        private int _examFailureCount;
        private bool _isTransitioning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (progressionConfig == null)
            {
                throw new InvalidOperationException("ProgressionConfig가 연결되지 않았습니다.");
            }

            if (routineConfig == null)
            {
                throw new InvalidOperationException("RoutineConfig가 연결되지 않았습니다.");
            }

            if (minigameCatalog == null)
            {
                throw new InvalidOperationException("MinigameCatalog가 연결되지 않았습니다.");
            }

            _routineSelector = new RoutineSelector(minigameCatalog);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnsubscribeActiveRunner();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void BeginRoutine(string minigameSceneName)
        {
            if (_isTransitioning || _activeRunner != null)
            {
                return;
            }

            var snapshot = ValidateAndCaptureSnapshot(minigameSceneName);
            var stage = progressionConfig.GetRoutineStage(snapshot.Rank);

            _sessionSeed = Environment.TickCount;
            _selectedRoutine = _routineSelector.SelectRoutine(stage, routineConfig.MinigameCount, _sessionSeed);
            _runMode = RoutineRunMode.Routine;
            _examRequiredSuccessCount = 0;

            BeginTransition(minigameSceneName);
        }

        public void BeginExam(string minigameSceneName)
        {
            if (_isTransitioning || _activeRunner != null)
            {
                return;
            }

            var snapshot = ValidateAndCaptureSnapshot(minigameSceneName);

            if (!progressionConfig.CanTakeExam(snapshot))
            {
                throw new InvalidOperationException("현재 심사 자격 조건을 충족하지 않았습니다.");
            }

            var stage = progressionConfig.GetRoutineStage(snapshot.Rank);
            var examMinigameCount = progressionConfig.GetExamMinigameCount(snapshot.Rank);

            _sessionSeed = Environment.TickCount;
            _selectedRoutine = _routineSelector.SelectRoutine(stage, examMinigameCount, _sessionSeed);
            _runMode = RoutineRunMode.Exam;
            _examRequiredSuccessCount = progressionConfig.GetExamRequiredSuccessCount(snapshot.Rank);
            _examSuccessCount = 0;
            _examFailureCount = 0;

            BeginTransition(minigameSceneName);
        }

        public bool IsDischargeExam()
        {
            return progressionConfig.IsDischargeExam(GameState.Instance.CaptureSnapshot());
        }

        public bool RefreshLobbyPointUI(Slider pointSlider, Text pointText)
        {
            var state = GameState.Instance.CaptureSnapshot();
            var requiredPoints = progressionConfig.GetRequiredCumulativePoints(state.Rank);

            pointSlider.minValue = 0;
            pointSlider.maxValue = requiredPoints;
            pointSlider.value = state.TotalPoints;
            pointSlider.interactable = false;
            pointText.text = $"{state.TotalPoints} / {requiredPoints}";
            return state.TotalPoints >= requiredPoints;
        }

        private GameStateSnapshot ValidateAndCaptureSnapshot(string minigameSceneName)
        {
            if (string.IsNullOrWhiteSpace(minigameSceneName))
            {
                throw new ArgumentException("미니게임 씬 이름이 비어 있습니다.", nameof(minigameSceneName));
            }

            if (GameState.Instance == null)
            {
                throw new InvalidOperationException("GameState가 존재하지 않습니다.");
            }

            return GameState.Instance.CaptureSnapshot();
        }

        private void BeginTransition(string minigameSceneName)
        {
            _lobbySceneName = SceneManager.GetActiveScene().name;
            _minigameSceneName = minigameSceneName;
            _isTransitioning = true;

            SceneManager.LoadScene(_minigameSceneName);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (!_isTransitioning || scene.name != _minigameSceneName)
            {
                return;
            }

            _isTransitioning = false;
            _activeRunner = FindFirstObjectByType<RoutineRunner>();

            if (_activeRunner == null)
            {
                Debug.LogError($"{scene.name} 씬에서 RoutineRunner를 찾지 못했습니다.");
                ReturnToLobby();
                return;
            }

            if (_runMode == RoutineRunMode.Routine)
            {
                _activeResultView = FindFirstObjectByType<RoutineResultView>(FindObjectsInactive.Include);

                if (_activeResultView == null)
                {
                    Debug.LogError($"{scene.name} 씬에서 RoutineResultView를 찾지 못했습니다.");
                    ReturnToLobby();
                    return;
                }
            }
            else
            {
                _activeResultView = null;
            }

            _activeRunner.RoutineCompleted += OnRoutineCompleted;

            if (_runMode == RoutineRunMode.Exam)
            {
                _activeRunner.FeedbackShown += OnExamFeedbackShown;
            }

            _activeRunner.StartRoutine(_selectedRoutine, _sessionSeed, _runMode);
        }

        private void OnExamFeedbackShown(MinigameJudgement judgement, int score)
        {
            if (judgement == MinigameJudgement.Success)
            {
                _examSuccessCount++;
            }
            else
            {
                _examFailureCount++;
            }

            var remaining = _selectedRoutine.Count - (_examSuccessCount + _examFailureCount);
            var confirmedPass = _examSuccessCount >= _examRequiredSuccessCount;
            var confirmedFail = _examSuccessCount + remaining < _examRequiredSuccessCount;

            if (confirmedPass || confirmedFail)
            {
                _activeRunner.RequestEarlyStop();
            }
        }

        private void OnRoutineCompleted(RoutineReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            if (GameState.Instance == null)
            {
                throw new InvalidOperationException("GameState가 존재하지 않습니다.");
            }

            var runMode = _runMode;
            var examRequiredSuccessCount = _examRequiredSuccessCount;

            UnsubscribeActiveRunner();

            if (runMode == RoutineRunMode.Exam)
            {
                CompleteExam(report, examRequiredSuccessCount);
            }
            else
            {
                CompleteRoutine(report);
            }
        }

        private void CompleteRoutine(RoutineReport report)
        {
            if (_activeResultView == null)
            {
                Debug.LogError("일과 결과 화면을 표시할 RoutineResultView가 없습니다.");
                ReturnToLobby();
                return;
            }

            var beforeSettlement = GameState.Instance.CaptureSnapshot();
            GameState.Instance.ApplyRoutineSettlement(report);
            var afterSettlement = GameState.Instance.CaptureSnapshot();
            var requiredPoints = progressionConfig.GetRequiredCumulativePoints(afterSettlement.Rank);

            var resultData = new RoutineResultData(
                report.SuccessCount,
                report.FailureCount,
                report.BasePointsTotal,
                report.RoutinePerfectBonus,
                beforeSettlement.TotalPoints,
                afterSettlement.TotalPoints,
                requiredPoints);

            _activeResultView.Show(resultData, ContinueAfterRoutineResult);
        }

        private void ContinueAfterRoutineResult()
        {
            var lobbySceneName = _lobbySceneName;
            ClearSession();
            SceneManager.LoadScene(lobbySceneName);
        }

        private void CompleteExam(RoutineReport report, int requiredSuccessCount)
        {
            var passed = report.SuccessCount >= requiredSuccessCount;
            var isDischargeExam = progressionConfig.IsDischargeExam(GameState.Instance.CaptureSnapshot());

            GameState.Instance.AdvanceMonth();

            if (passed && isDischargeExam)
            {
                if (string.IsNullOrWhiteSpace(endingSceneName))
                {
                    throw new InvalidOperationException("endingSceneName이 연결되지 않았습니다.");
                }

                ClearSession();
                SceneManager.LoadScene(endingSceneName);
                return;
            }

            if (passed)
            {
                GameState.Instance.TryPromote();
            }

            var lobbySceneName = _lobbySceneName;
            ClearSession();
            SceneManager.LoadScene(lobbySceneName);
        }

        private void ReturnToLobby()
        {
            var lobbySceneName = _lobbySceneName;
            ClearSession();

            if (!string.IsNullOrWhiteSpace(lobbySceneName))
            {
                SceneManager.LoadScene(lobbySceneName);
            }
        }

        private void UnsubscribeActiveRunner()
        {
            if (_activeRunner == null)
            {
                return;
            }

            _activeRunner.RoutineCompleted -= OnRoutineCompleted;
            _activeRunner.FeedbackShown -= OnExamFeedbackShown;
            _activeRunner = null;
        }

        private void ClearSession()
        {
            UnsubscribeActiveRunner();

            _selectedRoutine = null;
            _activeResultView = null;
            _lobbySceneName = null;
            _minigameSceneName = null;
            _sessionSeed = 0;
            _runMode = RoutineRunMode.Routine;
            _examRequiredSuccessCount = 0;
            _examSuccessCount = 0;
            _examFailureCount = 0;
            _isTransitioning = false;
        }
    }
}
