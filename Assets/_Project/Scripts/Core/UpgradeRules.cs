using System;

namespace SubmarineVoyage.Core
{
    public enum UpgradeType
    {
        Cargo,
        Speed
    }

    /// <summary>
    /// Upgrade numbers in one place so they are easy to tune after playtesting.
    /// Levels start at 1; costs are based on the current level.
    /// </summary>
    public static class UpgradeRules
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 5;

        public static int GoldCost(int currentLevel) => 100 * currentLevel * currentLevel;

        public static int MaterialCost(int currentLevel) => currentLevel;

        /// <summary>+20% reward per level above 1.</summary>
        public static double CargoMultiplier(int level) => 1 + 0.2 * (level - 1);

        /// <summary>Voyage time x0.9 per level above 1 (multiplicative, never reaches zero).</summary>
        public static double SpeedMultiplier(int level) => Math.Pow(0.9, level - 1);
    }
}
