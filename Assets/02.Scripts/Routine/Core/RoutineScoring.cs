using System;

namespace DreamOfMilitary.Routine
{
    public static class RoutineScoring
    {
        public const int PointsPerSuccess = 1;

        public static int Calculate(MinigameJudgement judgement)
        {
            return judgement switch
            {
                MinigameJudgement.Failure => 0,
                MinigameJudgement.Success => PointsPerSuccess,
                _ => throw new ArgumentOutOfRangeException(nameof(judgement))
            };
        }
    }
}
