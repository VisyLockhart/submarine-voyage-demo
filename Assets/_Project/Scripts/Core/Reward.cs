namespace SubmarineVoyage.Core
{
    public readonly struct Reward
    {
        public int Gold { get; }
        public int Materials { get; }

        public Reward(int gold, int materials)
        {
            Gold = gold;
            Materials = materials;
        }
    }
}
