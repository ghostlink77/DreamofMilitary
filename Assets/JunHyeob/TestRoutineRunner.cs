using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamOfMilitary.Routine
{
    /// <summary>
    /// 루틴에서 미니게임의 실행 흐름을 관리한다.
    ///
    /// 담당:
    /// - MinigameDef에서 실행할 미니게임 선택
    /// - Prefab 생성
    /// - IMinigame.Begin 호출
    /// - 제한시간 관리
    /// - Timeout 발생 시 Abort 호출
    /// - 미니게임 판정 수신
    /// - 다음 미니게임 실행
    ///
    /// 담당하지 않음:
    /// - GameState 직접 수정
    /// - 계급 변경
    /// - 복무 개월 증가
    /// - 최종 점수 저장
    /// - 로비 이동
    /// </summary>
    public sealed class TestRoutineRunner : MonoBehaviour
    {
        [Header("미니게임 정의")]
        [SerializeField]
        private List<MinigameDef> _minigameDefinitions =
            new List<MinigameDef>();

        [Header("루틴 설정")]
        [SerializeField]
        private RoutineStage _currentStage =
            RoutineStage.PromoteToPrivateFirstClass;

        [SerializeField, Min(1)]
        private int _minigamesPerRoutine = 3;

        [Header("랜덤")]
        [SerializeField]
        private int _randomSeed = 0;

        [SerializeField]
        private bool _useRandomSeed = true;

        [Header("실행 위치")]
        [SerializeField]
        private Transform _minigameRoot;

        private readonly List<MinigameDef> _availableMinigames =
            new List<MinigameDef>();

        private IMinigame _currentMinigame;

        private GameObject _currentMinigameObject;

        private MinigameDef _currentMinigameDef;

        private float _currentTimeLimit;
        private float _elapsedTime;

        private int _currentMinigameIndex;

        private bool _isRoutineRunning;
        private bool _isMinigameRunning;
        private bool _isEnding;

        private System.Random _random;

        /// <summary>
        /// 현재 루틴이 실행 중인지 여부.
        /// </summary>
        public bool IsRoutineRunning =>
            _isRoutineRunning;

        /// <summary>
        /// 현재 미니게임이 실행 중인지 여부.
        /// </summary>
        public bool IsMinigameRunning =>
            _isMinigameRunning;

        /// <summary>
        /// 현재 실행 중인 미니게임 정의.
        /// </summary>
        public MinigameDef CurrentMinigameDef =>
            _currentMinigameDef;

        /// <summary>
        /// 현재 루틴의 진행 번호.
        /// </summary>
        public int CurrentMinigameIndex =>
            _currentMinigameIndex;

        /// <summary>
        /// 현재 루틴에서 실행할 미니게임 개수.
        /// </summary>
        public int MinigamesPerRoutine =>
            _minigamesPerRoutine;

        /// <summary>
        /// 루틴 전체가 끝났을 때 발생한다.
        ///
        /// bool:
        /// true  = 정상적으로 모든 미니게임 완료
        /// false = 루틴 중단/실패
        /// </summary>
        public event Action<bool> RoutineCompleted;

        /// <summary>
        /// 미니게임 하나가 끝났을 때 발생한다.
        /// </summary>
        public event Action<
            MinigameDef,
            MinigameJudgement> MinigameCompleted;

        private void Awake()
        {
            if (_minigameRoot == null)
            {
                _minigameRoot = transform;
            }

            InitializeRandom();
        }
        //임시 테스트용
        private void Start()
        {
            StartRoutine();
        }
        private void Update()
        {
            if (!_isMinigameRunning)
            {
                return;
            }

            UpdateMinigameTimer();
        }

        /// <summary>
        /// 루틴을 시작한다.
        /// </summary>
        public void StartRoutine()
        {
            if (_isRoutineRunning)
            {
                Debug.LogWarning(
                    "RoutineRunner: 이미 루틴이 실행 중입니다.");

                return;
            }

            if (_minigameDefinitions == null ||
                _minigameDefinitions.Count == 0)
            {
                Debug.LogError(
                    "RoutineRunner: 등록된 미니게임이 없습니다.");

                return;
            }

            if (_minigamesPerRoutine <= 0)
            {
                Debug.LogError(
                    "RoutineRunner: 루틴 미니게임 개수가 1 이상이어야 합니다.");

                return;
            }

            InitializeRandom();

            _isRoutineRunning = true;
            _isEnding = false;

            _currentMinigameIndex = 0;

            BuildAvailableMinigameList();

            if (_availableMinigames.Count == 0)
            {
                Debug.LogError(
                    $"RoutineRunner: " +
                    $"{_currentStage}에서 실행 가능한 미니게임이 없습니다.");

                EndRoutine(false);
                return;
            }

            RunNextMinigame();
        }

        /// <summary>
        /// 현재 실행 중인 루틴을 강제로 중단한다.
        /// </summary>
        public void AbortRoutine()
        {
            if (!_isRoutineRunning)
            {
                return;
            }

            _isEnding = true;

            if (_isMinigameRunning &&
                _currentMinigame != null)
            {
                _currentMinigame.Abort();
            }

            CleanupCurrentMinigame();

            _isMinigameRunning = false;
            _isRoutineRunning = false;

            RoutineCompleted?.Invoke(false);
        }

        private void BuildAvailableMinigameList()
        {
            _availableMinigames.Clear();

            for (int i = 0;
                 i < _minigameDefinitions.Count;
                 i++)
            {
                MinigameDef def =
                    _minigameDefinitions[i];

                if (def == null)
                {
                    continue;
                }

                if (!def.SupportsStage(_currentStage))
                {
                    continue;
                }

                _availableMinigames.Add(def);
            }
        }

        private void RunNextMinigame()
        {
            if (!_isRoutineRunning)
            {
                return;
            }

            if (_isEnding)
            {
                return;
            }

            if (_currentMinigameIndex >=
                _minigamesPerRoutine)
            {
                EndRoutine(true);
                return;
            }

            MinigameDef definition =
                SelectNextMinigame();

            if (definition == null)
            {
                Debug.LogError(
                    "RoutineRunner: 실행할 미니게임을 선택하지 못했습니다.");

                EndRoutine(false);
                return;
            }

            StartMinigame(definition);
        }

        private MinigameDef SelectNextMinigame()
        {
            if (_availableMinigames.Count == 0)
            {
                return null;
            }

            int index =
                _random.Next(
                    _availableMinigames.Count);

            return _availableMinigames[index];
        }

        private void StartMinigame(
            MinigameDef definition)
        {
            if (definition.Prefab == null)
            {
                Debug.LogError(
                    $"RoutineRunner: " +
                    $"미니게임 '{definition.Id}'에 Prefab이 없습니다.");

                EndRoutine(false);
                return;
            }

            _currentMinigameDef = definition;

            _currentMinigameObject =
                Instantiate(
                    definition.Prefab,
                    _minigameRoot);

            if (_currentMinigameObject == null)
            {
                Debug.LogError(
                    $"RoutineRunner: " +
                    $"미니게임 '{definition.Id}' 생성에 실패했습니다.");

                EndRoutine(false);
                return;
            }

            _currentMinigame =
                _currentMinigameObject
                    .GetComponent<IMinigame>();

            if (_currentMinigame == null)
            {
                Debug.LogError(
                    $"RoutineRunner: " +
                    $"Prefab '{definition.Prefab.name}'에 " +
                    $"IMinigame 구현체가 없습니다.");

                CleanupCurrentMinigame();

                EndRoutine(false);
                return;
            }

            _elapsedTime = 0f;

            _currentTimeLimit =
                definition.TimeLimitSeconds;

            if (_currentTimeLimit <= 0f)
            {
                Debug.LogError(
                    $"RoutineRunner: " +
                    $"미니게임 '{definition.Id}'의 " +
                    $"제한시간이 올바르지 않습니다.");

                CleanupCurrentMinigame();

                EndRoutine(false);
                return;
            }

            int minigameSeed =
                GenerateMinigameSeed();

            MinigameContext context =
                new MinigameContext(
                    definition.DifficultyTier,
                    definition.TimeLimitSeconds,
                    minigameSeed);

            _isMinigameRunning = true;

            try
            {
                _currentMinigame.Begin(
                    context,
                    OnMinigameCompleted);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                HandleMinigameError();
            }
        }

        private void UpdateMinigameTimer()
        {
            _elapsedTime += Time.deltaTime;

            if (_elapsedTime <
                _currentTimeLimit)
            {
                return;
            }

            HandleTimeout();
        }

        private void HandleTimeout()
        {
            if (!_isMinigameRunning)
            {
                return;
            }

            if (_isEnding)
            {
                return;
            }

            _isMinigameRunning = false;

            IMinigame minigame =
                _currentMinigame;

            if (minigame != null)
            {
                minigame.Abort();
            }

            MinigameDef definition =
                _currentMinigameDef;

            CleanupCurrentMinigame();

            if (definition != null)
            {
                MinigameCompleted?.Invoke(
                    definition,
                    MinigameJudgement.Failure);
            }

            _currentMinigameIndex++;

            RunNextMinigame();
        }

        private void OnMinigameCompleted(
            MinigameJudgement judgement)
        {
            if (!_isMinigameRunning)
            {
                return;
            }

            if (_isEnding)
            {
                return;
            }

            _isMinigameRunning = false;

            MinigameDef definition =
                _currentMinigameDef;

            CleanupCurrentMinigame();

            if (definition != null)
            {
                MinigameCompleted?.Invoke(
                    definition,
                    judgement);
            }

            _currentMinigameIndex++;

            RunNextMinigame();
        }

        private void HandleMinigameError()
        {
            if (_isEnding)
            {
                return;
            }

            _isMinigameRunning = false;

            MinigameDef definition =
                _currentMinigameDef;

            if (_currentMinigame != null)
            {
                try
                {
                    _currentMinigame.Abort();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            CleanupCurrentMinigame();

            if (definition != null)
            {
                MinigameCompleted?.Invoke(
                    definition,
                    MinigameJudgement.Failure);
            }

            _currentMinigameIndex++;

            RunNextMinigame();
        }

        private void CleanupCurrentMinigame()
        {
            _currentMinigame = null;

            if (_currentMinigameObject != null)
            {
                Destroy(
                    _currentMinigameObject);

                _currentMinigameObject = null;
            }

            _currentMinigameDef = null;
            _currentTimeLimit = 0f;
            _elapsedTime = 0f;
        }

        private void EndRoutine(bool completed)
        {
            if (_isEnding)
            {
                return;
            }

            _isEnding = true;

            CleanupCurrentMinigame();

            _isMinigameRunning = false;
            _isRoutineRunning = false;

            RoutineCompleted?.Invoke(
                completed);
        }

        private void InitializeRandom()
        {
            if (_useRandomSeed)
            {
                _random =
                    new System.Random(
                        Environment.TickCount);
            }
            else
            {
                _random =
                    new System.Random(
                        _randomSeed);
            }
        }

        private int GenerateMinigameSeed()
        {
            return _random.Next(
                int.MinValue,
                int.MaxValue);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _minigamesPerRoutine =
                Mathf.Max(
                    1,
                    _minigamesPerRoutine);
        }
#endif
    }
}
