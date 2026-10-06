using System;
using System.Collections.Generic;
using System.Linq;

namespace SubmarineVoyage.Core
{
    /// <summary>Converts between live game objects and <see cref="SaveData"/>.</summary>
    public static class SaveMapper
    {
        public static SaveData Capture(GameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            return new SaveData
            {
                gold = state.Wallet.Gold,
                materials = state.Wallet.Materials,
                timeScale = state.TimeScale,
                submarines = state.Fleet.Submarines.Select(s => new SubmarineSaveData
                {
                    cargoLevel = s.CargoLevel,
                    speedLevel = s.SpeedLevel,
                    routeId = s.CurrentRoute?.Id ?? "",
                    departedAtTicks = s.DepartedAtUtc?.Ticks ?? 0,
                    returnAtTicks = s.ReturnAtUtc?.Ticks ?? 0
                }).ToList()
            };
        }

        /// <summary>
        /// Rebuilds the game from a save. Values are clamped rather than rejected so a hand-edited
        /// or outdated file still loads; a voyage on a route that no longer exists is dropped.
        /// </summary>
        public static GameState Restore(SaveData data, IEnumerable<Route> routes)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (routes == null) throw new ArgumentNullException(nameof(routes));

            var routesById = routes.ToDictionary(r => r.Id);
            var wallet = new Wallet(Math.Max(0, data.gold), Math.Max(0, data.materials));

            var saved = data.submarines ?? new List<SubmarineSaveData>();
            var submarines = new List<Submarine>();
            for (var i = 0; i < saved.Count && i < Fleet.MaxSlots; i++)
            {
                var s = saved[i];
                Route route = null;
                if (!string.IsNullOrEmpty(s.routeId) && s.returnAtTicks > 0)
                    routesById.TryGetValue(s.routeId, out route);

                submarines.Add(Submarine.Restore(
                    Fleet.SlotName(i),
                    ClampLevel(s.cargoLevel),
                    ClampLevel(s.speedLevel),
                    route,
                    route == null ? (DateTime?)null : FromTicks(s.departedAtTicks),
                    route == null ? (DateTime?)null : FromTicks(s.returnAtTicks)));
            }

            return new GameState(wallet, Fleet.Restore(submarines), TimeScaleOptions.Normalize(data.timeScale));
        }

        private static int ClampLevel(int level) =>
            Math.Min(UpgradeRules.MaxLevel, Math.Max(UpgradeRules.MinLevel, level));

        private static DateTime FromTicks(long ticks) =>
            new DateTime(Math.Min(Math.Max(ticks, DateTime.MinValue.Ticks), DateTime.MaxValue.Ticks), DateTimeKind.Utc);
    }
}
