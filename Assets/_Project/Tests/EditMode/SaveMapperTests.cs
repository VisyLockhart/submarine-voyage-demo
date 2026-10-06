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

            var (restoredWallet, restoredFleet) = SaveMapper.Restore(SaveMapper.Capture(wallet, fleet), Routes);

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
            var data = SaveMapper.Capture(new Wallet(), fleet);

            var (_, restored) = SaveMapper.Restore(data, Routes);

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

            var (_, fleet) = SaveMapper.Restore(data, Routes);

            Assert.AreEqual(SubmarineState.Idle, fleet.Submarines[0].GetState(T0));
            Assert.IsNull(fleet.Submarines[0].CurrentRoute);
        }

        [Test]
        public void Restore_OutOfRangeValues_AreClamped()
        {
            var data = new SaveData { gold = -50, materials = -1 };
            for (var i = 0; i < 6; i++)
                data.submarines.Add(new SubmarineSaveData { cargoLevel = 99, speedLevel = 0 });

            var (wallet, fleet) = SaveMapper.Restore(data, Routes);

            Assert.AreEqual(0, wallet.Gold);
            Assert.AreEqual(0, wallet.Materials);
            Assert.AreEqual(Fleet.MaxSlots, fleet.UnlockedCount);
            Assert.AreEqual(UpgradeRules.MaxLevel, fleet.Submarines[0].CargoLevel);
            Assert.AreEqual(UpgradeRules.MinLevel, fleet.Submarines[0].SpeedLevel);
        }

        [Test]
        public void Restore_EmptySave_StartsWithOneSubmarine()
        {
            var (_, fleet) = SaveMapper.Restore(new SaveData(), Routes);
            Assert.AreEqual(1, fleet.UnlockedCount);
        }
    }
}
