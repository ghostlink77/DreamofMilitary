using System;

namespace DreamOfMilitary.Routine
{
    /// <summary>
    /// 현재 계급이 아니라 이번 일과가 어느 심사를 준비하는 구간인지를 나타낸다.
    /// PromoteToPrivateFirstClass는 아직 일병이 아닌 이병 구간이다.
    /// </summary>
    public enum RoutineStage
    {
        PromoteToPrivateFirstClass = 0,
        PromoteToCorporal = 1,
        PromoteToSergeant = 2,
        Discharge = 3
    }

    public enum RoutineRunMode
    {
        Routine = 0,
        Exam = 1
    }

    [Flags]
    public enum RoutineStageMask
    {
        None = 0,
        PromoteToPrivateFirstClass = 1 << 0,
        PromoteToCorporal = 1 << 1,
        PromoteToSergeant = 1 << 2,
        Discharge = 1 << 3,
        All = PromoteToPrivateFirstClass
            | PromoteToCorporal
            | PromoteToSergeant
            | Discharge
    }

    public enum MinigameJudgement
    {
        Failure = 0,
        Success = 1
    }

    /// <summary>
    /// 제한시간에 도달했을 때 미니게임의 결과를 결정하는 방식이다.
    /// </summary>
    public enum MinigameTimeLimitRule
    {
        MustCompleteBeforeLimit = 0,
        SurviveUntilLimit = 1,
        EvaluateAtLimit = 2
    }

    // 미니게임 한 번의 설정 값
    public readonly struct MinigameContext
    {
        public int DifficultyTier { get; }
        public float TimeLimitSeconds { get; }
        public int RandomSeed { get; }

        public MinigameContext(int difficultyTier, float timeLimitSeconds, int randomSeed)
        {
            if (difficultyTier < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(difficultyTier),
                    "난이도 티어는 0 이상이어야 합니다.");
            }

            if (timeLimitSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeLimitSeconds),
                    "제한시간은 0보다 커야 합니다.");
            }

            DifficultyTier = difficultyTier;
            TimeLimitSeconds = timeLimitSeconds;
            RandomSeed = randomSeed;
        }
    }

    public interface IMinigame
    {
        /// <summary>
        /// 미니게임을 시작한다.
        /// 완료 콜백은 최대 한 번 호출해야 한다.
        /// 최종 흐름 제어와 제한시간 판정은 RoutineRunner가 담당한다.
        /// </summary>
        void Begin(MinigameContext context, Action<MinigameJudgement> onCompleted);

        /// <summary>
        /// 입력과 내부 진행을 즉시 중단한다.
        /// 호출 이후에는 플레이어 입력과 완료 콜백을 발생시키지 않아야 한다.
        /// </summary>
        void Abort();
    }

    /// <summary>
    /// 제한시간 종료 시 현재 상태를 기준으로 판정해야 하는 미니게임이 구현한다.
    /// </summary>
    public interface ITimeLimitResolver
    {
        MinigameJudgement ResolveAtTimeLimit();
    }
}
