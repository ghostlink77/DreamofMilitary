namespace DreamOfMilitary.Progression
{
    public enum MilitaryRank
    {
        PrivateSecondClass = 0,
        PrivateFirstClass = 1,
        Corporal = 2,
        Sergeant = 3
    }

    public readonly struct GameStateSnapshot
    {
        public MilitaryRank Rank { get; }
        public int ServiceMonths { get; }
        public int TotalPoints { get; }

        public GameStateSnapshot(
            MilitaryRank rank,
            int serviceMonths,
            int totalPoints)
        {
            Rank = rank;
            ServiceMonths = serviceMonths;
            TotalPoints = totalPoints;
        }
    }
}
