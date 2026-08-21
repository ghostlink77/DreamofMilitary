using System;
using System.Collections.Generic;

namespace DreamOfMilitary.Routine
{
    /// <summary>
    /// 현재 일과 구간에 등장 가능한 일반 미니게임을 필터링하고,
    /// 셔플백 방식(후보를 한 번씩 모두 소진한 뒤 다시 섞음)으로 일과 목록을 만든다.
    /// </summary>
    public sealed class RoutineSelector
    {
        private readonly MinigameCatalog _catalog;
        private readonly Dictionary<RoutineStage, BagState> _bags = new Dictionary<RoutineStage, BagState>();

        public RoutineSelector(MinigameCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public IReadOnlyList<MinigameDef> SelectRoutine(RoutineStage stage, int count, int randomSeed)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "선정할 미니게임 수는 1 이상이어야 합니다.");
            }

            var candidates = BuildCandidateList(stage);

            if (!_bags.TryGetValue(stage, out var bag))
            {
                bag = new BagState();
                _bags.Add(stage, bag);
            }

            if (!bag.MatchesPool(candidates))
            {
                bag.ResetPool(candidates);
            }

            var random = new Random(randomSeed);
            var selected = new List<MinigameDef>(count);

            for (var index = 0; index < count; index++)
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

        private List<MinigameDef> BuildCandidateList(RoutineStage stage)
        {
            var candidates = new List<MinigameDef>();
            var knownIds = new HashSet<string>(StringComparer.Ordinal);

            for (var index = 0; index < _catalog.Definitions.Count; index++)
            {
                var definition = _catalog.Definitions[index];

                if (definition == null || !definition.SupportsStage(stage))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.Id))
                {
                    throw new InvalidOperationException(
                        "미니게임 정의에는 비어 있지 않은 ID가 필요합니다.");
                }

                if (!knownIds.Add(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"중복된 미니게임 ID가 있습니다: {definition.Id}");
                }

                candidates.Add(definition);
            }

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{stage} 구간에 등장 가능한 "
                    + "일과 미니게임이 없습니다.");
            }

            return candidates;
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
