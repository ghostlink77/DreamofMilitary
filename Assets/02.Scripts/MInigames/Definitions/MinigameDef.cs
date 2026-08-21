// ========================
// 일과 미니게임 하나의 데이터를 담는 스크립터블 오브젝트
// id, 미니게임 프리팹, 등장 조건 등을 설정 및 관리한다.
// ========================

using UnityEngine;

namespace DreamOfMilitary.Routine
{
    [CreateAssetMenu(
        fileName = "MinigameDef",
        menuName = "Dream Of Military/Minigame Definition")]
    public sealed class MinigameDef : ScriptableObject
    {
        [Header("식별")]
        [SerializeField] private string _id;

        [SerializeField] private string _commandText;

        [SerializeField] private GameObject _prefab;

        [Header("일과 등장 조건")]
        [SerializeField] private RoutineStageMask _availableStages = RoutineStageMask.All;

        [Header("기본 설정")]
        [SerializeField, Min(0)] private int _difficultyTier;

        [SerializeField, Min(0.1f)] private float _timeLimitSeconds = 5f;

        [SerializeField] private MinigameTimeLimitRule _timeLimitRule = MinigameTimeLimitRule.MustCompleteBeforeLimit;


        // ---- Properties ----
        public string Id => _id;
        public string CommandText => _commandText;
        public GameObject Prefab => _prefab;
        public RoutineStageMask AvailableStages => _availableStages;
        public int DifficultyTier => _difficultyTier;
        public float TimeLimitSeconds => _timeLimitSeconds;
        public MinigameTimeLimitRule TimeLimitRule => _timeLimitRule;

        public bool SupportsStage(RoutineStage stage)
        {
            return (_availableStages & ToMask(stage)) != 0;
        }

        private static RoutineStageMask ToMask(RoutineStage stage)
        {
            switch (stage)
            {
                case RoutineStage.PromoteToPrivateFirstClass:
                    return RoutineStageMask.PromoteToPrivateFirstClass;

                case RoutineStage.PromoteToCorporal:
                    return RoutineStageMask.PromoteToCorporal;

                case RoutineStage.PromoteToSergeant:
                    return RoutineStageMask.PromoteToSergeant;

                case RoutineStage.Discharge:
                    return RoutineStageMask.Discharge;

                default:
                    return RoutineStageMask.None;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _id = _id?.Trim() ?? string.Empty;
            _commandText = _commandText?.Trim() ?? string.Empty;
        }
#endif
    }
}
