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

    /// <summary>Everything that is saved: wallet, fleet, settings and goal progress.</summary>
    public sealed class GameState
    {
        public Wallet Wallet { get; }
        public Fleet Fleet { get; }
        public double TimeScale { get; private set; }

        /// <summary>When this game began; null for saves made before it was recorded.</summary>
        public DateTime? StartedAtUtc { get; }

        /// <summary>When the fleet goal was first reached; null until then.</summary>
        public DateTime? CompletedAtUtc { get; private set; }

        public GameState(Wallet wallet, Fleet fleet, double timeScale = TimeScaleOptions.Default,
            DateTime? startedAtUtc = null, DateTime? completedAtUtc = null)
        {
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Fleet = fleet ?? throw new ArgumentNullException(nameof(fleet));
            SetTimeScale(timeScale);
            StartedAtUtc = startedAtUtc;
            CompletedAtUtc = completedAtUtc;
        }

        public static GameState NewGame(int gold = 0, int materials = 0, DateTime? startedAtUtc = null) =>
            new GameState(new Wallet(gold, materials), new Fleet(), startedAtUtc: startedAtUtc);

        /// <summary>Real time from start to goal; null if not completed or the start is unknown.</summary>
        public TimeSpan? PlayTimeToComplete => CompletedAtUtc - StartedAtUtc;

        /// <summary>
        /// Records the goal the first time the fleet is complete. Returns true only on that call,
        /// so the completion dialog is shown once; later play continues normally.
        /// </summary>
        public bool TryMarkCompleted(DateTime nowUtc)
        {
            if (CompletedAtUtc != null || !Fleet.IsComplete) return false;
            CompletedAtUtc = nowUtc;
            return true;
        }

        /// <summary>Affects only voyages that depart afterwards; return times already set stay fixed.</summary>
        public void SetTimeScale(double timeScale)
        {
            if (!TimeScaleOptions.IsValid(timeScale)) throw new ArgumentOutOfRangeException(nameof(timeScale));
            TimeScale = timeScale;
        }
    }
}
