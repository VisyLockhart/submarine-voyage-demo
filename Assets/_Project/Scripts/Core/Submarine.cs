using System;

namespace SubmarineVoyage.Core
{
    /// <summary>
    /// One submarine: Idle -> Voyaging -> ReadyToCollect -> Idle.
    /// The state is derived from the stored return timestamp instead of being ticked every
    /// frame, so it stays correct across pauses and app restarts (offline progress).
    /// </summary>
    public sealed class Submarine
    {
        public string Name { get; }
        public int CargoLevel { get; private set; } = UpgradeRules.MinLevel;
        public int SpeedLevel { get; private set; } = UpgradeRules.MinLevel;
        public Route CurrentRoute { get; private set; }
        public DateTime? DepartedAtUtc { get; private set; }
        public DateTime? ReturnAtUtc { get; private set; }

        public Submarine(string name)
        {
            Name = name;
        }

        /// <summary>Rebuilds a submarine from saved values. Pass a null route for an idle submarine.</summary>
        public static Submarine Restore(string name, int cargoLevel, int speedLevel,
            Route route, DateTime? departedAtUtc, DateTime? returnAtUtc)
        {
            if (cargoLevel < UpgradeRules.MinLevel || cargoLevel > UpgradeRules.MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(cargoLevel));
            if (speedLevel < UpgradeRules.MinLevel || speedLevel > UpgradeRules.MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(speedLevel));
            if (route != null && returnAtUtc == null)
                throw new ArgumentException("A voyage needs a return time.", nameof(returnAtUtc));

            return new Submarine(name)
            {
                CargoLevel = cargoLevel,
                SpeedLevel = speedLevel,
                CurrentRoute = route,
                DepartedAtUtc = route == null ? null : departedAtUtc,
                ReturnAtUtc = route == null ? null : returnAtUtc
            };
        }

        public SubmarineState GetState(DateTime nowUtc)
        {
            if (ReturnAtUtc == null) return SubmarineState.Idle;
            return nowUtc < ReturnAtUtc.Value ? SubmarineState.Voyaging : SubmarineState.ReadyToCollect;
        }

        /// <summary>Real time left until return; zero when not voyaging.</summary>
        public TimeSpan GetRemaining(DateTime nowUtc)
        {
            if (GetState(nowUtc) != SubmarineState.Voyaging) return TimeSpan.Zero;
            return ReturnAtUtc.Value - nowUtc;
        }

        /// <summary>Real voyage time for this submarine, including its speed upgrade.</summary>
        public TimeSpan GetVoyageDuration(Route route, double timeScale)
        {
            if (route == null) throw new ArgumentNullException(nameof(route));
            var baseDuration = route.GetRealDuration(timeScale);
            return TimeSpan.FromTicks((long)(baseDuration.Ticks * UpgradeRules.SpeedMultiplier(SpeedLevel)));
        }

        /// <param name="timeScale">Game seconds per real second (60 = 1 real second is 1 game minute).</param>
        public void Depart(Route route, DateTime nowUtc, double timeScale)
        {
            if (route == null) throw new ArgumentNullException(nameof(route));
            if (GetState(nowUtc) != SubmarineState.Idle)
                throw new InvalidOperationException($"{Name} can only depart when idle.");

            // The return time is fixed at departure, so later changes to the time scale or
            // speed level do not affect voyages already at sea.
            var realDuration = GetVoyageDuration(route, timeScale);
            CurrentRoute = route;
            DepartedAtUtc = nowUtc;
            ReturnAtUtc = nowUtc + realDuration;
        }

        public Reward Collect(DateTime nowUtc, IRandomSource random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (GetState(nowUtc) != SubmarineState.ReadyToCollect)
                throw new InvalidOperationException($"{Name} has nothing to collect yet.");

            var multiplier = UpgradeRules.CargoMultiplier(CargoLevel);
            var gold = random.Range(CurrentRoute.MinGold, CurrentRoute.MaxGold);
            var materials = random.Range(CurrentRoute.MinMaterials, CurrentRoute.MaxMaterials);
            var reward = new Reward(Scale(gold, multiplier), Scale(materials, multiplier));

            CurrentRoute = null;
            DepartedAtUtc = null;
            ReturnAtUtc = null;
            return reward;
        }

        /// <summary>Smallest and largest reward this submarine can bring back, including its cargo upgrade.</summary>
        public (Reward Min, Reward Max) GetRewardRange(Route route)
        {
            if (route == null) throw new ArgumentNullException(nameof(route));
            var multiplier = UpgradeRules.CargoMultiplier(CargoLevel);
            return (new Reward(Scale(route.MinGold, multiplier), Scale(route.MinMaterials, multiplier)),
                    new Reward(Scale(route.MaxGold, multiplier), Scale(route.MaxMaterials, multiplier)));
        }

        public int GetLevel(UpgradeType type) => type == UpgradeType.Cargo ? CargoLevel : SpeedLevel;

        public bool IsMaxLevel(UpgradeType type) => GetLevel(type) >= UpgradeRules.MaxLevel;

        public bool IsFullyUpgraded => IsMaxLevel(UpgradeType.Cargo) && IsMaxLevel(UpgradeType.Speed);

        /// <summary>Spends the upgrade cost from the wallet. Returns false if maxed or not affordable.</summary>
        public bool TryUpgrade(UpgradeType type, Wallet wallet)
        {
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            if (IsMaxLevel(type)) return false;

            var level = GetLevel(type);
            if (!wallet.TrySpend(UpgradeRules.GoldCost(level), UpgradeRules.MaterialCost(level))) return false;

            if (type == UpgradeType.Cargo) CargoLevel++;
            else SpeedLevel++;
            return true;
        }

        private static int Scale(int value, double multiplier) =>
            (int)Math.Round(value * multiplier, MidpointRounding.AwayFromZero);
    }
}
