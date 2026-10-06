using System;
using System.Collections.Generic;

namespace SubmarineVoyage.Core
{
    /// <summary>
    /// The player's submarine slots. The first is free; the rest are unlocked in order with gold.
    /// </summary>
    public sealed class Fleet
    {
        private static readonly int[] UnlockCosts = { 0, 500, 1500, 4000 };

        public static int MaxSlots => UnlockCosts.Length;

        public static string SlotName(int slot) => $"Submarine {slot + 1}";

        private readonly List<Submarine> _submarines = new List<Submarine>();

        public int SlotCount => MaxSlots;
        public int UnlockedCount => _submarines.Count;
        public IReadOnlyList<Submarine> Submarines => _submarines;

        public Fleet()
        {
            AddSubmarine();
        }

        private Fleet(IEnumerable<Submarine> submarines)
        {
            _submarines.AddRange(submarines);
            if (_submarines.Count == 0) AddSubmarine();
        }

        /// <summary>Rebuilds a fleet from saved submarines; slot 1 always exists.</summary>
        public static Fleet Restore(IEnumerable<Submarine> submarines)
        {
            if (submarines == null) throw new ArgumentNullException(nameof(submarines));
            return new Fleet(submarines);
        }

        public bool IsUnlocked(int slot) => slot >= 0 && slot < UnlockedCount;

        /// <summary>Cost of unlocking the given slot; slots must be unlocked in order.</summary>
        public int GetUnlockCost(int slot)
        {
            if (slot < 0 || slot >= SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
            return UnlockCosts[slot];
        }

        /// <summary>True when this slot is the next one in line to unlock.</summary>
        public bool IsNextToUnlock(int slot) => slot == UnlockedCount && slot < SlotCount;

        public bool TryUnlockNext(Wallet wallet)
        {
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            if (UnlockedCount >= SlotCount) return false;
            if (!wallet.TrySpend(GetUnlockCost(UnlockedCount))) return false;

            AddSubmarine();
            return true;
        }

        private void AddSubmarine() => _submarines.Add(new Submarine(SlotName(_submarines.Count)));
    }
}
