using UnityEngine;

namespace DreamOfMilitary.Routine
{
    [CreateAssetMenu(
        fileName = "RoutineConfig",
        menuName = "Dream Of Military/Routine Config")]
    public sealed class RoutineConfig : ScriptableObject
    {
        [Header("일과 구성")]
        [SerializeField, Min(1)]
        private int _minigameCount = 10;

        [Header("화면 표시 시간")]
        [SerializeField, Min(0f)]
        private float _commandDisplaySeconds = 0.75f;

        [SerializeField, Min(0f)]
        private float _feedbackDisplaySeconds = 0.5f;

        [Header("제한시간 종료 처리")]
        [Tooltip(
            "제한시간이 끝나면 즉시 Abort하여 입력을 차단한 뒤, "
            + "프리팹을 제거하기 전에 기다리는 정리 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _abortCleanupGraceSeconds = 0.5f;

        [Header("일과 퍼펙트 보너스")]
        [SerializeField, Min(0)]
        private int _allPerfectBonusPoints;

        public int MinigameCount => _minigameCount;

        public float CommandDisplaySeconds =>
            _commandDisplaySeconds;

        public float FeedbackDisplaySeconds =>
            _feedbackDisplaySeconds;

        public float AbortCleanupGraceSeconds =>
            _abortCleanupGraceSeconds;

        public int AllPerfectBonusPoints =>
            _allPerfectBonusPoints;

#if UNITY_EDITOR
        private void OnValidate()
        {
            _minigameCount = Mathf.Max(1, _minigameCount);

            _commandDisplaySeconds = Mathf.Max(
                0f,
                _commandDisplaySeconds);

            _feedbackDisplaySeconds = Mathf.Max(
                0f,
                _feedbackDisplaySeconds);

            _abortCleanupGraceSeconds = Mathf.Max(
                0f,
                _abortCleanupGraceSeconds);

            _allPerfectBonusPoints = Mathf.Max(
                0,
                _allPerfectBonusPoints);
        }
#endif
    }
}
