// ====================
// 미니게임 하나의 결과를 담는 플레인 클래스
// 미니게임 종류(Id), 판정(성공, 실패, 퍼펙트),
// 종료 컨디션(성공, 시간종료(실패), 중지, 에러), 점수 등 관리
// ====================

using System;
using System.Collections.Generic;

namespace DreamOfMilitary.Routine
{
    public sealed class RoutineEntry
    {
        public string MinigameId { get; }
        public MinigameJudgement Judgement { get; }
        public MinigameEndReason EndReason { get; }
        public ScoreBreakdown Score { get; }
        public float ElapsedSeconds { get; }

        public RoutineEntry(string minigameId, MinigameJudgement judgement, MinigameEndReason endReason, ScoreBreakdown score, float elapsedSeconds)
        {
            if (string.IsNullOrWhiteSpace(minigameId))
            {
                throw new ArgumentException("미니게임 ID가 필요합니다.", nameof(minigameId));
            }

            if (elapsedSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsedSeconds));
            }

            MinigameId = minigameId;
            Judgement = judgement;
            EndReason = endReason;
            Score = score;
            ElapsedSeconds = elapsedSeconds;
        }
    }

    public sealed class RoutineReport
    {
        private readonly RoutineEntry[] _entries;

        public IReadOnlyList<RoutineEntry> Entries => _entries;

        public int FailureCount { get; }
        public int ClearCount { get; }
        public int PerfectCount { get; }

        public int BasePointsTotal { get; }
        public int PerfectBonusTotal { get; }
        public int RoutinePerfectBonus { get; }
        public int TotalPoints { get; }

        public bool IsAllPerfect =>
            _entries.Length > 0
            && PerfectCount == _entries.Length;

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
            var clearCount = 0;
            var perfectCount = 0;
            var basePointsTotal = 0;
            var perfectBonusTotal = 0;

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

                    case MinigameJudgement.Clear:
                        clearCount++;
                        break;

                    case MinigameJudgement.Perfect:
                        perfectCount++;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(entries),
                            "알 수 없는 판정이 포함되어 있습니다.");
                }

                basePointsTotal = checked(
                    basePointsTotal + entry.Score.BasePoints);

                perfectBonusTotal = checked(
                    perfectBonusTotal + entry.Score.PerfectBonus);
            }

            var isAllPerfect =
                _entries.Length > 0
                && perfectCount == _entries.Length;

            if (!isAllPerfect && routinePerfectBonus > 0)
            {
                throw new ArgumentException(
                    "일과 퍼펙트가 아닌 보고서에는 "
                    + "일과 퍼펙트 보너스를 넣을 수 없습니다.",
                    nameof(routinePerfectBonus));
            }

            FailureCount = failureCount;
            ClearCount = clearCount;
            PerfectCount = perfectCount;
            BasePointsTotal = basePointsTotal;
            PerfectBonusTotal = perfectBonusTotal;
            RoutinePerfectBonus = routinePerfectBonus;

            TotalPoints = checked(basePointsTotal + perfectBonusTotal + routinePerfectBonus);
        }
    }
}
