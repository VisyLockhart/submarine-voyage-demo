using System;

namespace SubmarineVoyage.Core
{
    /// <summary>
    /// Plain data for one voyage route. Kept free of UnityEngine so the core can be tested
    /// without the editor; a ScriptableObject can map onto this later.
    /// </summary>
    public sealed class Route
    {
        public string Id { get; }
        public string DisplayName { get; }

        /// <summary>Voyage length in game time (before time acceleration).</summary>
        public TimeSpan GameDuration { get; }

        public int MinGold { get; }
        public int MaxGold { get; }
        public int MinMaterials { get; }
        public int MaxMaterials { get; }

        public Route(string id, string displayName, TimeSpan gameDuration,
            int minGold, int maxGold, int minMaterials, int maxMaterials)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Route id is required.", nameof(id));
            if (gameDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(gameDuration));
            if (minGold < 0 || maxGold < minGold) throw new ArgumentOutOfRangeException(nameof(maxGold));
            if (minMaterials < 0 || maxMaterials < minMaterials) throw new ArgumentOutOfRangeException(nameof(maxMaterials));

            Id = id;
            DisplayName = displayName;
            GameDuration = gameDuration;
            MinGold = minGold;
            MaxGold = maxGold;
            MinMaterials = minMaterials;
            MaxMaterials = maxMaterials;
        }
    }
}
