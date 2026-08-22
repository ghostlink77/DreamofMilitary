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
        [SerializeField, Min(0f)] private float _nextMinigameCountdownSeconds = 2f;

        [Header("제한시간 종료 처리")]
        [Tooltip("제한시간이 끝나면 즉시 Abort하여 입력을 차단한 뒤, 프리팹을 제거하기 전에 기다리는 정리 시간입니다.")]
        [SerializeField, Min(0f)] private float _abortCleanupGraceSeconds = 0.5f;

        [Header("일과 퍼펙트 보너스")]
        [FormerlySerializedAs("_allPerfectBonusPoints")]
        [SerializeField, Min(0)] private int _allSuccessBonusPoints = 2;

        public int MinigameCount => _minigameCount;
        public float CommandDisplaySeconds => _commandDisplaySeconds;
        public float FeedbackDisplaySeconds => _feedbackDisplaySeconds;
        public float NextMinigameCountdownSeconds => _nextMinigameCountdownSeconds;
        public float AbortCleanupGraceSeconds => _abortCleanupGraceSeconds;
        public int AllSuccessBonusPoints => _allSuccessBonusPoints;
    }
}
