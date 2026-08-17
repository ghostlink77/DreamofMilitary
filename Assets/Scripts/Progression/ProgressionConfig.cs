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

        public int GetRequiredCumulativePoints(
            MilitaryRank currentRank)
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
        public RoutineStage GetRoutineStage(
            MilitaryRank currentRank)
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
