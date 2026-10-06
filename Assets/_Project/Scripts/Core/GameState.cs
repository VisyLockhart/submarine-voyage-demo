using System;
using System.Collections.Generic;
using System.Linq;

namespace SubmarineVoyage.Core
{
    /// <summary>Allowed time acceleration values (game seconds per real second).</summary>
    public static class TimeScaleOptions
    {
        public const double Default = 60;

        public static IReadOnlyList<double> All { get; } = new[] { 1d, 60d, 600d };

        public static bool IsValid(double value) => All.Contains(value);

        public static double Normalize(double value) => IsValid(value) ? value : Default;
    }

    /// <summary>Everything that is saved: wallet, fleet and settings.</summary>
    public sealed class GameState
    {
        public Wallet Wallet { get; }
        public Fleet Fleet { get; }
        public double TimeScale { get; private set; }

        public GameState(Wallet wallet, Fleet fleet, double timeScale = TimeScaleOptions.Default)
        {
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Fleet = fleet ?? throw new ArgumentNullException(nameof(fleet));
            SetTimeScale(timeScale);
        }

        public static GameState NewGame(int gold = 0, int materials = 0) =>
            new GameState(new Wallet(gold, materials), new Fleet());

        /// <summary>Affects only voyages that depart afterwards; return times already set stay fixed.</summary>
        public void SetTimeScale(double timeScale)
        {
            if (!TimeScaleOptions.IsValid(timeScale)) throw new ArgumentOutOfRangeException(nameof(timeScale));
            TimeScale = timeScale;
        }
    }
}
