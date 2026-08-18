using System;
using System.Collections.Generic;

namespace DreamOfMilitary.Routine
{
    /// <summary>
    /// 현재 일과 구간에 등장 가능한 일반 미니게임을 필터링하고,
    /// 지정한 선정 규칙으로 일과 목록을 만든다.
    /// </summary>
    public sealed class RoutineSelector
    {
        private readonly MinigameCatalog _catalog;
        private readonly IRoutineSelectionStrategy _strategy;

        public RoutineSelector(MinigameCatalog catalog, IRoutineSelectionStrategy strategy = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

            _strategy = strategy ?? new ShuffleBagRoutineSelectionStrategy();
        }

        public IReadOnlyList<MinigameDef> SelectRoutine(RoutineStage stage, int count, int randomSeed)
        {
            var candidates = BuildCandidateList(stage);

            var request = new RoutineSelectionRequest(stage, count, randomSeed);

            return _strategy.Select(request, candidates);
        }

        private List<MinigameDef> BuildCandidateList(RoutineStage stage)
        {
            var candidates = new List<MinigameDef>();
            var knownIds = new HashSet<string>(StringComparer.Ordinal);

            for (var index = 0; index < _catalog.Definitions.Count; index++)
            {
                var definition = _catalog.Definitions[index];

                if (definition == null || definition.IsExamMinigame || !definition.SupportsStage(stage))
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
    }
}
