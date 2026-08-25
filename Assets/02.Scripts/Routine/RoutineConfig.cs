using UnityEngine;
using UnityEngine.Serialization;

namespace DreamOfMilitary.Routine
{
    [CreateAssetMenu(fileName = "RoutineConfig", menuName = "Dream Of Military/Routine Config")]
    public sealed class RoutineConfig : ScriptableObject
    {
        [Header("일과 구성")]
        [SerializeField, Min(1)] private int _minigameCount = 9;

        [Header("화면 표시 시간")]
        [SerializeField, Min(0f)] private float _commandDisplaySeconds = 0.75f;
        [SerializeField, Min(0f)] private float _feedbackDisplaySeconds = 0.5f;

        [Header("계급별 미니게임 사이 대기시간")]
        [FormerlySerializedAs("_nextMinigameCountdownSeconds")]
        [SerializeField, Min(0f)] private float _privateSecondClassNextMinigameCountdownSeconds = 2f;
        [SerializeField, Min(0f)] private float _privateFirstClassNextMinigameCountdownSeconds = 1.5f;
        [SerializeField, Min(0f)] private float _corporalNextMinigameCountdownSeconds = 1f;
        [SerializeField, Min(0f)] private float _sergeantNextMinigameCountdownSeconds = 0.5f;

        [Header("제한시간 종료 처리")]
        [Tooltip("제한시간이 끝나면 즉시 Abort하여 입력을 차단한 뒤, 프리팹을 제거하기 전에 기다리는 정리 시간입니다.")]
        [SerializeField, Min(0f)] private float _abortCleanupGraceSeconds = 0.5f;

        [Header("일과 퍼펙트 보너스")]
        [FormerlySerializedAs("_allPerfectBonusPoints")]
        [SerializeField, Min(0)] private int _allSuccessBonusPoints = 2;

        public int MinigameCount => _minigameCount;
        public float CommandDisplaySeconds => _commandDisplaySeconds;
        public float FeedbackDisplaySeconds => _feedbackDisplaySeconds;
        public float NextMinigameCountdownSeconds => _privateSecondClassNextMinigameCountdownSeconds;
        public float AbortCleanupGraceSeconds => _abortCleanupGraceSeconds;
        public int AllSuccessBonusPoints => _allSuccessBonusPoints;

        public float GetNextMinigameCountdownSeconds(DreamOfMilitary.Progression.MilitaryRank currentRank)
        {
            return currentRank switch
            {
                DreamOfMilitary.Progression.MilitaryRank.PrivateSecondClass => _privateSecondClassNextMinigameCountdownSeconds,
                DreamOfMilitary.Progression.MilitaryRank.PrivateFirstClass => _privateFirstClassNextMinigameCountdownSeconds,
                DreamOfMilitary.Progression.MilitaryRank.Corporal => _corporalNextMinigameCountdownSeconds,
                DreamOfMilitary.Progression.MilitaryRank.Sergeant => _sergeantNextMinigameCountdownSeconds,
                _ => throw new System.ArgumentOutOfRangeException(nameof(currentRank))
            };
        }
    }
}
