// ========================
// 현재 게임 상황을 관리하는 싱글톤 클래스
// 현재 계급, 복무 개월 수, 전체 상점 등은 여기서 관리한다.
// ========================

using System;
using DreamOfMilitary.Routine;
using UnityEngine;

namespace DreamOfMilitary.Progression
{
    [DisallowMultipleComponent]
    public sealed class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        [Header("새 게임 초기값")]
        [SerializeField]
        private MilitaryRank _startingRank = MilitaryRank.PrivateSecondClass;

        [SerializeField, Min(0)]
        private int _startingServiceMonths;

        [SerializeField, Min(0)]
        private int _startingTotalPoints;

        public MilitaryRank CurrentRank { get; private set; }
        public int ServiceMonths { get; private set; }
        public int TotalPoints { get; private set; }
        public int TotalMinigameSuccessCount { get; private set; }
        public int TotalMinigameFailureCount { get; private set; }

        public event Action<GameStateSnapshot> StateChanged;

        public event Action<GameStateSnapshot> MonthAdvanced;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeFromStartingValues();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public GameStateSnapshot CaptureSnapshot()
        {
            return new GameStateSnapshot(CurrentRank, ServiceMonths, TotalPoints);
        }

        /// <summary>
        /// 일과 정산 결과를 반영하고 복무 개월을 1개월 증가시킨다.
        /// 상점 반영과 개월 증가가 끝난 상태로 이벤트를 한 번 발행한다.
        /// </summary>
        public void ApplyRoutineSettlement(RoutineReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            var nextTotalPoints = checked(TotalPoints + report.TotalPoints);
            var nextSuccessCount = checked(TotalMinigameSuccessCount + report.SuccessCount);
            var nextFailureCount = checked(TotalMinigameFailureCount + report.FailureCount);

            // 정산값만 일부 반영된 중간 상태가 남지 않도록
            // 개월 수 오버플로도 변경 전에 검사한다.
            _ = checked(ServiceMonths + 1);

            TotalPoints = nextTotalPoints;
            TotalMinigameSuccessCount = nextSuccessCount;
            TotalMinigameFailureCount = nextFailureCount;
            AdvanceMonth();
        }

        public void ApplyExamSettlement(RoutineReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            var nextSuccessCount = checked(TotalMinigameSuccessCount + report.SuccessCount);
            var nextFailureCount = checked(TotalMinigameFailureCount + report.FailureCount);
            _ = checked(ServiceMonths + 1);

            TotalMinigameSuccessCount = nextSuccessCount;
            TotalMinigameFailureCount = nextFailureCount;
            AdvanceMonth();
        }

        /// <summary>
        /// 복무 개월 변경은 이 메서드를 통해서만 처리한다.
        /// 일과 완료와 진급심사 실패 모두 1개월이 경과한다.
        /// </summary>
        public void AdvanceMonth()
        {
            ServiceMonths = checked(ServiceMonths + 1);

            var snapshot = CaptureSnapshot();

            MonthAdvanced?.Invoke(snapshot);
            StateChanged?.Invoke(snapshot);
        }

        /// <summary>
        /// 현재 계급에서 한 단계 진급한다.
        /// 누적 상점과 복무 개월은 변경하지 않는다.
        /// 병장인 경우 전역심사 대상이므로 false를 반환한다.
        /// </summary>
        public bool TryPromote()
        {
            if (CurrentRank == MilitaryRank.Sergeant)
            {
                return false;
            }

            CurrentRank = (MilitaryRank)((int)CurrentRank + 1);

            StateChanged?.Invoke(CaptureSnapshot());
            return true;
        }

        public void ResetForNewGame()
        {
            InitializeFromStartingValues();
            StateChanged?.Invoke(CaptureSnapshot());
        }

        private void InitializeFromStartingValues()
        {
            CurrentRank = _startingRank;
            ServiceMonths = Mathf.Max(0, _startingServiceMonths);
            TotalPoints = Mathf.Max(0, _startingTotalPoints);
            TotalMinigameSuccessCount = 0;
            TotalMinigameFailureCount = 0;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _startingServiceMonths = Mathf.Max(0, _startingServiceMonths);
            _startingTotalPoints = Mathf.Max(0, _startingTotalPoints);
        }
#endif
    }
}
