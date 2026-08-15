using System;
using System.Collections.Generic;

namespace DreamOfMilitary.Routine
{
    public readonly struct RoutineSelectionRequest
    {
        public RoutineStage Stage { get; }
        public int Count { get; }
        public int RandomSeed { get; }

        public RoutineSelectionRequest(
            RoutineStage stage,
            int count,
            int randomSeed)
        {
            if (!Enum.IsDefined(typeof(RoutineStage), stage))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stage));
            }

            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    "선정할 미니게임 수는 1 이상이어야 합니다.");
            }

            Stage = stage;
            Count = count;
            RandomSeed = randomSeed;
        }
    }

    public interface IRoutineSelectionStrategy
    {
        IReadOnlyList<MinigameDef> Select(
            RoutineSelectionRequest request,
            IReadOnlyList<MinigameDef> candidates);
    }
}
