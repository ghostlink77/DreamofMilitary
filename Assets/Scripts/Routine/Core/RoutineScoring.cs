using System;

namespace DreamOfMilitary.Routine
{
    public readonly struct ScoreBreakdown
    {
        public int BasePoints { get; }
        public int PerfectBonus { get; }
        public int TotalPoints => checked(BasePoints + PerfectBonus);

        public ScoreBreakdown(int basePoints, int perfectBonus)
        {
            if (basePoints < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(basePoints));
            }

            if (perfectBonus < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(perfectBonus));
            }

            BasePoints = basePoints;
            PerfectBonus = perfectBonus;
        }
    }

    public static class RoutineScoring
    {
        public static ScoreBreakdown Calculate(
            int minigameBasePoints,
            MinigameJudgement judgement)
        {
            if (minigameBasePoints < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minigameBasePoints),
                    "기본 상점은 0 이상이어야 합니다.");
            }

            return judgement switch
            {
                MinigameJudgement.Failure => new ScoreBreakdown(0, 0),
                MinigameJudgement.Clear => new ScoreBreakdown(
                    minigameBasePoints,
                    0),
                MinigameJudgement.Perfect => CalculatePerfect(
                    minigameBasePoints),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(judgement))
            };
        }

        private static ScoreBreakdown CalculatePerfect(int basePoints)
        {
            if (basePoints % 2 != 0)
            {
                throw new InvalidOperationException(
                    "퍼펙트 보너스의 반올림 규칙이 확정되기 전에는 "
                    + "기본 상점을 짝수로 설정해야 합니다.");
            }

            return new ScoreBreakdown(
                basePoints,
                basePoints / 2);
        }
    }
}
