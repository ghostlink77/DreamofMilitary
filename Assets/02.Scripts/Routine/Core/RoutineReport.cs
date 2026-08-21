// ====================
// 미니게임 하나의 결과를 담는 플레인 클래스
// 미니게임 종류(Id), 성공 여부, 획득 상점 등을 관리한다.
// ====================

using System;
using System.Collections.Generic;

namespace DreamOfMilitary.Routine
{
    public sealed class RoutineEntry
    {
        public string MinigameId { get; }
        public MinigameJudgement Judgement { get; }
        public int Score { get; }
        public float ElapsedSeconds { get; }

        public RoutineEntry(string minigameId, MinigameJudgement judgement, int score, float elapsedSeconds)
        {
            if (string.IsNullOrWhiteSpace(minigameId))
            {
                throw new ArgumentException("미니게임 ID가 필요합니다.", nameof(minigameId));
            }

            if (score < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(score));
            }

            if (elapsedSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsedSeconds));
            }

            MinigameId = minigameId;
            Judgement = judgement;
            Score = score;
            ElapsedSeconds = elapsedSeconds;
        }
    }

    public sealed class RoutineReport
    {
        private readonly RoutineEntry[] _entries;

        public IReadOnlyList<RoutineEntry> Entries => _entries;

        public int FailureCount { get; }
        public int SuccessCount { get; }

        public int BasePointsTotal { get; }
        public int RoutinePerfectBonus { get; }
        public int TotalPoints { get; }

        public bool IsAllSuccessful => _entries.Length > 0 && SuccessCount == _entries.Length;

        public RoutineReport(IReadOnlyList<RoutineEntry> entries, int routinePerfectBonus)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (routinePerfectBonus < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(routinePerfectBonus));
            }

            _entries = new RoutineEntry[entries.Count];

            var failureCount = 0;
            var successCount = 0;
            var basePointsTotal = 0;

            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index]
                    ?? throw new ArgumentException(
                        "일과 기록에는 null 항목을 넣을 수 없습니다.",
                        nameof(entries));

                _entries[index] = entry;

                switch (entry.Judgement)
                {
                    case MinigameJudgement.Failure:
                        failureCount++;
                        break;

                    case MinigameJudgement.Success:
                        successCount++;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(entries),
                            "알 수 없는 판정이 포함되어 있습니다.");
                }

                basePointsTotal = checked(basePointsTotal + entry.Score);
            }

            var isAllSuccessful =
                _entries.Length > 0
                && successCount == _entries.Length;

            if (!isAllSuccessful && routinePerfectBonus > 0)
            {
                throw new ArgumentException(
                    "모든 미니게임을 성공하지 않은 보고서에는 일과 퍼펙트 보너스를 넣을 수 없습니다.",
                    nameof(routinePerfectBonus));
            }

            FailureCount = failureCount;
            SuccessCount = successCount;
            BasePointsTotal = basePointsTotal;
            RoutinePerfectBonus = routinePerfectBonus;

            TotalPoints = checked(basePointsTotal + routinePerfectBonus);
        }
    }
}
