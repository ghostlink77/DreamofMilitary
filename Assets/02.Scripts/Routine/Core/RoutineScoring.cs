using System;

namespace DreamOfMilitary.Routine
{
    public readonly struct ScoreBreakdown
    {
        public int BasePoints { get; }
        public int TotalPoints => BasePoints;

        public ScoreBreakdown(int basePoints)
        {
            if (basePoints < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(basePoints));
            }

            BasePoints = basePoints;
        }
    }

    public static class RoutineScoring
    {
        public static ScoreBreakdown Calculate(int minigameBasePoints, MinigameJudgement judgement)
        {
            if (minigameBasePoints < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minigameBasePoints),
                    "기본 상점은 0 이상이어야 합니다.");
            }

            return judgement switch
            {
                MinigameJudgement.Failure => new ScoreBreakdown(0),
                MinigameJudgement.Success => new ScoreBreakdown(minigameBasePoints),
                _ => throw new ArgumentOutOfRangeException(nameof(judgement))
            };
        }
    }
}
