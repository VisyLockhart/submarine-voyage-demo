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
        public Route CurrentRoute { get; private set; }
        public DateTime? DepartedAtUtc { get; private set; }
        public DateTime? ReturnAtUtc { get; private set; }

        public Submarine(string name)
        {
            Name = name;
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

        /// <param name="timeScale">Game seconds per real second (60 = 1 real second is 1 game minute).</param>
        public void Depart(Route route, DateTime nowUtc, double timeScale)
        {
            if (route == null) throw new ArgumentNullException(nameof(route));
            if (GetState(nowUtc) != SubmarineState.Idle)
                throw new InvalidOperationException($"{Name} can only depart when idle.");

            // The return time is fixed at departure, so changing the time scale later
            // does not affect voyages already at sea.
            var realDuration = route.GetRealDuration(timeScale);
            CurrentRoute = route;
            DepartedAtUtc = nowUtc;
            ReturnAtUtc = nowUtc + realDuration;
        }

        public Reward Collect(DateTime nowUtc, IRandomSource random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (GetState(nowUtc) != SubmarineState.ReadyToCollect)
                throw new InvalidOperationException($"{Name} has nothing to collect yet.");

            var reward = new Reward(
                random.Range(CurrentRoute.MinGold, CurrentRoute.MaxGold),
                random.Range(CurrentRoute.MinMaterials, CurrentRoute.MaxMaterials));

            CurrentRoute = null;
            DepartedAtUtc = null;
            ReturnAtUtc = null;
            return reward;
        }
    }
}
