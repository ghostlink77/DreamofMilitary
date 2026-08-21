using System;
using System.Collections.Generic;
using DreamOfMilitary.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamOfMilitary.Routine
{
    [DisallowMultipleComponent]
    public sealed class RoutineFlowController : MonoBehaviour
    {
        public static RoutineFlowController Instance { get; private set; }

        [SerializeField] private ProgressionConfig progressionConfig;
        [SerializeField] private RoutineConfig routineConfig;
        [SerializeField] private MinigameCatalog minigameCatalog;

        private RoutineSelector _routineSelector;
        private IReadOnlyList<MinigameDef> _selectedRoutine;
        private RoutineRunner _activeRunner;
        private string _lobbySceneName;
        private string _minigameSceneName;
        private int _sessionSeed;
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

            if (_activeRunner != null)
            {
                _activeRunner.RoutineCompleted -= CompleteRoutine;
            }
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

            if (string.IsNullOrWhiteSpace(minigameSceneName))
            {
                throw new ArgumentException("미니게임 씬 이름이 비어 있습니다.", nameof(minigameSceneName));
            }

            if (GameState.Instance == null)
            {
                throw new InvalidOperationException("GameState가 존재하지 않습니다.");
            }

            var snapshot = GameState.Instance.CaptureSnapshot();
            var stage = progressionConfig.GetRoutineStage(snapshot.Rank);

            _sessionSeed = Environment.TickCount;
            _selectedRoutine = _routineSelector.SelectRoutine(stage, routineConfig.MinigameCount, _sessionSeed);
            _lobbySceneName = SceneManager.GetActiveScene().name;
            _minigameSceneName = minigameSceneName;
            _isTransitioning = true;

            SceneManager.LoadScene(_minigameSceneName);
        }

        public void CompleteRoutine(RoutineReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            if (GameState.Instance == null)
            {
                throw new InvalidOperationException("GameState가 존재하지 않습니다.");
            }

            if (_activeRunner != null)
            {
                _activeRunner.RoutineCompleted -= CompleteRoutine;
                _activeRunner = null;
            }

            GameState.Instance.ApplyRoutineSettlement(report);

            var lobbySceneName = _lobbySceneName;
            ClearSession();
            SceneManager.LoadScene(lobbySceneName);
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

            _activeRunner.RoutineCompleted += CompleteRoutine;
            _activeRunner.StartRoutine(_selectedRoutine, _sessionSeed);
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

        private void ClearSession()
        {
            _selectedRoutine = null;
            _activeRunner = null;
            _lobbySceneName = null;
            _minigameSceneName = null;
            _sessionSeed = 0;
            _isTransitioning = false;
        }
    }
}
