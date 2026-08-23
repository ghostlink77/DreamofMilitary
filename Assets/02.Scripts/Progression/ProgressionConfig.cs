// =================================
// 진급 심사에 필요한 상점을 정의하고
// 현재 진급 구간과진급 심사 가능 여부를 판단하는 스크립터블 오브젝트 
// =================================

using System;
using DreamOfMilitary.Routine;
using UnityEngine;

namespace DreamOfMilitary.Progression
{
    [CreateAssetMenu(
        fileName = "ProgressionConfig",
        menuName = "Dream Of Military/Progression Config")]
    public sealed class ProgressionConfig : ScriptableObject
    {
        [Header("진급·전역심사 누적 상점 요구량")]
        [SerializeField, Min(0)]
        private int _privateSecondClassToPrivateFirstClassPoints;

        [SerializeField, Min(0)]
        private int _privateFirstClassToCorporalPoints;

        [SerializeField, Min(0)]
        private int _corporalToSergeantPoints;

        [SerializeField, Min(0)]
        private int _sergeantToDischargePoints;

        [Header("진급·전역심사 미니게임 개수·합격 기준")]
        [SerializeField, Min(1)]
        private int _privateFirstClassExamMinigameCount = 12;

        [SerializeField, Min(1)]
        private int _privateFirstClassExamRequiredSuccessCount = 6;

        [SerializeField, Min(1)]
        private int _corporalExamMinigameCount = 12;

        [SerializeField, Min(1)]
        private int _corporalExamRequiredSuccessCount = 8;

        [SerializeField, Min(1)]
        private int _sergeantExamMinigameCount = 16;

        [SerializeField, Min(1)]
        private int _sergeantExamRequiredSuccessCount = 14;

        [SerializeField, Min(1)]
        private int _dischargeExamMinigameCount = 16;

        [SerializeField, Min(1)]
        private int _dischargeExamRequiredSuccessCount = 16;

        public int GetRequiredCumulativePoints(MilitaryRank currentRank)
        {
            switch (currentRank)
            {
                case MilitaryRank.PrivateSecondClass:
                    return _privateSecondClassToPrivateFirstClassPoints;

                case MilitaryRank.PrivateFirstClass:
                    return _privateFirstClassToCorporalPoints;

                case MilitaryRank.Corporal:
                    return _corporalToSergeantPoints;

                case MilitaryRank.Sergeant:
                    return _sergeantToDischargePoints;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(currentRank));
            }
        }

        /// <summary>
        /// 현재 계급을 기준으로 준비 중인 심사 구간을 반환한다.
        /// 예를 들어 이병은 일병 진급 구간이다.
        /// </summary>
        public RoutineStage GetRoutineStage(MilitaryRank currentRank)
        {
            switch (currentRank)
            {
                case MilitaryRank.PrivateSecondClass:
                    return RoutineStage.PromoteToPrivateFirstClass;

                case MilitaryRank.PrivateFirstClass:
                    return RoutineStage.PromoteToCorporal;

                case MilitaryRank.Corporal:
                    return RoutineStage.PromoteToSergeant;

                case MilitaryRank.Sergeant:
                    return RoutineStage.Discharge;

                default:
                    throw new ArgumentOutOfRangeException(nameof(currentRank));
            }
        }

        /// <summary>
        /// 현재 계급이 준비 중인 심사(진급·전역)에서 실행할 미니게임 총 개수.
        /// </summary>
        public int GetExamMinigameCount(MilitaryRank currentRank)
        {
            switch (currentRank)
            {
                case MilitaryRank.PrivateSecondClass:
                    return _privateFirstClassExamMinigameCount;

                case MilitaryRank.PrivateFirstClass:
                    return _corporalExamMinigameCount;

                case MilitaryRank.Corporal:
                    return _sergeantExamMinigameCount;

                case MilitaryRank.Sergeant:
                    return _dischargeExamMinigameCount;

                default:
                    throw new ArgumentOutOfRangeException(nameof(currentRank));
            }
        }

        /// <summary>
        /// 현재 계급이 준비 중인 심사(진급·전역)에서 합격하기 위해 필요한 최소 성공 개수.
        /// </summary>
        public int GetExamRequiredSuccessCount(MilitaryRank currentRank)
        {
            switch (currentRank)
            {
                case MilitaryRank.PrivateSecondClass:
                    return _privateFirstClassExamRequiredSuccessCount;

                case MilitaryRank.PrivateFirstClass:
                    return _corporalExamRequiredSuccessCount;

                case MilitaryRank.Corporal:
                    return _sergeantExamRequiredSuccessCount;

                case MilitaryRank.Sergeant:
                    return _dischargeExamRequiredSuccessCount;

                default:
                    throw new ArgumentOutOfRangeException(nameof(currentRank));
            }
        }

        public bool CanTakeExam(GameStateSnapshot state)
        {
            return state.TotalPoints >= GetRequiredCumulativePoints(state.Rank);
        }

        public bool IsDischargeExam(GameStateSnapshot state)
        {
            return state.Rank == MilitaryRank.Sergeant;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _privateSecondClassToPrivateFirstClassPoints = Mathf.Max(0, _privateSecondClassToPrivateFirstClassPoints);

            _privateFirstClassToCorporalPoints = Mathf.Max(0, _privateFirstClassToCorporalPoints);

            _corporalToSergeantPoints = Mathf.Max(0, _corporalToSergeantPoints);

            _sergeantToDischargePoints = Mathf.Max(0, _sergeantToDischargePoints);
        }
#endif
    }
}
