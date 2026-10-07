using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SubmarineVoyage.Core.Tests
{
    public class SaveMapperTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);

        private static readonly Route NearSea =
            new Route("near", "Near Sea", TimeSpan.FromMinutes(10), 50, 80, 0, 1);
        private static readonly Route DeepSea =
            new Route("deep", "Deep Sea", TimeSpan.FromMinutes(60), 300, 450, 1, 3);
        private static readonly List<Route> Routes = new List<Route> { NearSea, DeepSea };

        [Test]
        public void CaptureThenRestore_KeepsWalletFleetLevelsAndVoyages()
        {
            var wallet = new Wallet(gold: 1000, materials: 5);
            var fleet = new Fleet();
            fleet.TryUnlockNext(wallet);                                   // 500 gold, 2 submarines
            fleet.Submarines[0].TryUpgrade(UpgradeType.Cargo, wallet);     // 100 gold, 1 material
            fleet.Submarines[1].Depart(DeepSea, T0, 60);

            var state = new GameState(wallet, fleet, timeScale: 600);
            var restored = SaveMapper.Restore(SaveMapper.Capture(state), Routes);
            var restoredWallet = restored.Wallet;
            var restoredFleet = restored.Fleet;

            Assert.AreEqual(600, restored.TimeScale);

            Assert.AreEqual(400, restoredWallet.Gold);
            Assert.AreEqual(4, restoredWallet.Materials);
            Assert.AreEqual(2, restoredFleet.UnlockedCount);
            Assert.AreEqual(2, restoredFleet.Submarines[0].CargoLevel);
            Assert.AreEqual(SubmarineState.Idle, restoredFleet.Submarines[0].GetState(T0));

            var voyaging = restoredFleet.Submarines[1];
            Assert.AreEqual("Submarine 2", voyaging.Name);
            Assert.AreSame(DeepSea, voyaging.CurrentRoute);
            Assert.AreEqual(T0.AddSeconds(60), voyaging.ReturnAtUtc);
            Assert.AreEqual(DateTimeKind.Utc, voyaging.ReturnAtUtc.Value.Kind);
        }

        [Test]
        public void Restore_VoyageThatEndedWhileOffline_IsReadyToCollect()
        {
            var fleet = new Fleet();
            fleet.Submarines[0].Depart(NearSea, T0, 60);
            var data = SaveMapper.Capture(new GameState(new Wallet(), fleet));

            var restored = SaveMapper.Restore(data, Routes).Fleet;

            // Game reopened a day later.
            Assert.AreEqual(SubmarineState.ReadyToCollect, restored.Submarines[0].GetState(T0.AddDays(1)));
        }

        [Test]
        public void Restore_UnknownRoute_DropsVoyageAndLeavesSubmarineIdle()
        {
            var data = new SaveData
            {
                submarines = { new SubmarineSaveData { routeId = "removed", returnAtTicks = T0.Ticks } }
            };

            var fleet = SaveMapper.Restore(data, Routes).Fleet;

            Assert.AreEqual(SubmarineState.Idle, fleet.Submarines[0].GetState(T0));
            Assert.IsNull(fleet.Submarines[0].CurrentRoute);
        }

        [Test]
        public void Restore_OutOfRangeValues_AreClamped()
        {
            var data = new SaveData { gold = -50, materials = -1 };
            for (var i = 0; i < 6; i++)
                data.submarines.Add(new SubmarineSaveData { cargoLevel = 99, speedLevel = 0 });

            var restored = SaveMapper.Restore(data, Routes);
            var wallet = restored.Wallet;
            var fleet = restored.Fleet;

            Assert.AreEqual(0, wallet.Gold);
            Assert.AreEqual(0, wallet.Materials);
            Assert.AreEqual(Fleet.MaxSlots, fleet.UnlockedCount);
            Assert.AreEqual(UpgradeRules.MaxLevel, fleet.Submarines[0].CargoLevel);
            Assert.AreEqual(UpgradeRules.MinLevel, fleet.Submarines[0].SpeedLevel);
        }

        [Test]
        public void Restore_EmptySave_StartsWithOneSubmarine()
        {
            var restored = SaveMapper.Restore(new SaveData(), Routes);
            Assert.AreEqual(1, restored.Fleet.UnlockedCount);
        }

        [Test]
        public void Restore_MissingOrInvalidTimeScale_UsesDefault()
        {
            // Saves written before settings existed have timeScale = 0.
            Assert.AreEqual(TimeScaleOptions.Default, SaveMapper.Restore(new SaveData(), Routes).TimeScale);
            Assert.AreEqual(TimeScaleOptions.Default,
                SaveMapper.Restore(new SaveData { timeScale = 7 }, Routes).TimeScale);
        }
    }

    public class GameStateTests
    {
        [Test]
        public void SetTimeScale_RejectsValuesNotInOptions()
        {
            var state = GameState.NewGame();
            Assert.AreEqual(TimeScaleOptions.Default, state.TimeScale);

            state.SetTimeScale(1);
            Assert.AreEqual(1, state.TimeScale);
            Assert.Throws<ArgumentOutOfRangeException>(() => state.SetTimeScale(30));
            Assert.AreEqual(1, state.TimeScale);
        }

        private static readonly DateTime T0 = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);

        private static GameState CompletedFleetGame()
        {
            var state = GameState.NewGame(gold: 1000000, materials: 1000, startedAtUtc: T0);
            var fleet = state.Fleet;
            while (fleet.TryUnlockNext(state.Wallet)) { }
            foreach (var sub in fleet.Submarines)
                for (var i = 1; i < UpgradeRules.MaxLevel; i++)
                {
                    sub.TryUpgrade(UpgradeType.Cargo, state.Wallet);
                    sub.TryUpgrade(UpgradeType.Speed, state.Wallet);
                }
            return state;
        }

        [Test]
        public void FleetGoal_CountsLevelsOfUnlockedSubmarines()
        {
            var state = GameState.NewGame();
            Assert.AreEqual(2, state.Fleet.TotalLevels);   // one submarine at Lv1 / Lv1
            Assert.AreEqual(40, Fleet.MaxTotalLevels);     // 4 submarines x 2 upgrades x Lv5
            Assert.IsFalse(state.TryMarkCompleted(T0));
        }

        [Test]
        public void TryMarkCompleted_FirstTimeOnly_AndRecordsPlayTime()
        {
            var state = CompletedFleetGame();
            Assert.IsTrue(state.Fleet.IsComplete);

            Assert.IsTrue(state.TryMarkCompleted(T0.AddMinutes(90)));
            Assert.IsFalse(state.TryMarkCompleted(T0.AddMinutes(95)));
            Assert.AreEqual(TimeSpan.FromMinutes(90), state.PlayTimeToComplete);
        }

        [Test]
        public void CaptureThenRestore_KeepsGoalTimes_AndOldSavesHaveNone()
        {
            var state = CompletedFleetGame();
            state.TryMarkCompleted(T0.AddMinutes(90));

            var restored = SaveMapper.Restore(SaveMapper.Capture(state), new List<Route>());
            Assert.AreEqual(T0, restored.StartedAtUtc);
            Assert.AreEqual(T0.AddMinutes(90), restored.CompletedAtUtc);

            var old = SaveMapper.Restore(new SaveData(), new List<Route>());
            Assert.IsNull(old.StartedAtUtc);
            Assert.IsNull(old.CompletedAtUtc);
            Assert.IsNull(old.PlayTimeToComplete);
        }
    }
}
