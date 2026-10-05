using System;

namespace SubmarineVoyage.Core
{
    public sealed class Wallet
    {
        public int Gold { get; private set; }
        public int Materials { get; private set; }

        public Wallet(int gold = 0, int materials = 0)
        {
            if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            if (materials < 0) throw new ArgumentOutOfRangeException(nameof(materials));
            Gold = gold;
            Materials = materials;
        }

        public void Add(Reward reward)
        {
            Gold += reward.Gold;
            Materials += reward.Materials;
        }

        public bool CanAfford(int gold) => gold >= 0 && Gold >= gold;

        public bool TrySpend(int gold)
        {
            if (!CanAfford(gold)) return false;
            Gold -= gold;
            return true;
        }
    }
}
