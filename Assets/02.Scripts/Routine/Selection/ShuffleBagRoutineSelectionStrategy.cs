using System;
using System.Collections.Generic;

namespace DreamOfMilitary.Routine
{
    /// <summary>
    /// 각 일과 구간별 후보를 한 번씩 모두 사용한 뒤 다시 섞는다.
    /// 후보가 일과 진행 개수보다 적어도 정상적으로 반복 선정된다.
    /// </summary>
    public sealed class ShuffleBagRoutineSelectionStrategy: IRoutineSelectionStrategy
    {
        private readonly Dictionary<RoutineStage, BagState> _bags = new Dictionary<RoutineStage, BagState>();

        public IReadOnlyList<MinigameDef> Select(RoutineSelectionRequest request, IReadOnlyList<MinigameDef> candidates)
        {
            ValidateCandidates(candidates);

            if (!_bags.TryGetValue(request.Stage, out var bag))
            {
                bag = new BagState();
                _bags.Add(request.Stage, bag);
            }

            if (!bag.MatchesPool(candidates))
            {
                bag.ResetPool(candidates);
            }

            var random = new Random(request.RandomSeed);
            var selected = new List<MinigameDef>(request.Count);

            for (var index = 0; index < request.Count; index++)
            {
                if (bag.Remaining.Count == 0)
                {
                    RefillBag(bag, random);
                }

                var lastIndex = bag.Remaining.Count - 1;
                var definition = bag.Remaining[lastIndex];

                bag.Remaining.RemoveAt(lastIndex);
                selected.Add(definition);
            }

            return selected;
        }

        private static void ValidateCandidates(IReadOnlyList<MinigameDef> candidates)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    "선정 가능한 미니게임이 없습니다.");
            }

            for (var index = 0; index < candidates.Count; index++)
            {
                if (candidates[index] == null)
                {
                    throw new ArgumentException(
                        "후보 목록에는 null 미니게임을 넣을 수 없습니다.",
                        nameof(candidates));
                }
            }
        }

        private static void RefillBag(BagState bag, Random random)
        {
            bag.Remaining.Clear();

            for (var index = 0; index < bag.Pool.Count; index++)
            {
                bag.Remaining.Add(bag.Pool[index]);
            }

            for (var index = bag.Remaining.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);

                (bag.Remaining[index], bag.Remaining[swapIndex]) = (bag.Remaining[swapIndex], bag.Remaining[index]);
            }
        }

        private sealed class BagState
        {
            public List<MinigameDef> Pool { get; } = new List<MinigameDef>();

            public List<MinigameDef> Remaining { get; } = new List<MinigameDef>();

            public bool MatchesPool(IReadOnlyList<MinigameDef> candidates)
            {
                if (Pool.Count != candidates.Count)
                {
                    return false;
                }

                for (var index = 0; index < candidates.Count; index++)
                {
                    if (!Pool.Contains(candidates[index]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public void ResetPool(IReadOnlyList<MinigameDef> candidates)
            {
                Pool.Clear();
                Remaining.Clear();

                for (var index = 0; index < candidates.Count; index++)
                {
                    Pool.Add(candidates[index]);
                }
            }
        }
    }
}
