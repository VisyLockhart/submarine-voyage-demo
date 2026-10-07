using System;
using NUnit.Framework;

namespace SubmarineVoyage.Core.Tests
{
    public class UpgradeTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc);

        private static Route DeepSea() =>
            new Route("deep", "Deep Sea", TimeSpan.FromMinutes(60), 300, 450, 1, 3);

        private sealed class MaxRandom : IRandomSource
        {
            public int Range(int minInclusive, int maxInclusive) => maxInclusive;
        }

        [Test]
        public void Costs_FollowCurrentLevel()
        {
            Assert.AreEqual(100, UpgradeRules.GoldCost(1));
            Assert.AreEqual(1600, UpgradeRules.GoldCost(4));
            Assert.AreEqual(4, UpgradeRules.MaterialCost(4));
        }

        [Test]
        public void TryUpgrade_SpendsGoldAndMaterials_AndRaisesLevel()
        {
            var sub = new Submarine("Sub 1");
            var wallet = new Wallet(gold: 150, materials: 2);

            Assert.IsTrue(sub.TryUpgrade(UpgradeType.Cargo, wallet));

            Assert.AreEqual(2, sub.CargoLevel);
            Assert.AreEqual(1, sub.SpeedLevel);
            Assert.AreEqual(50, wallet.Gold);
            Assert.AreEqual(1, wallet.Materials);
        }

        [Test]
        public void TryUpgrade_NotEnoughMaterials_FailsWithoutSpending()
        {
            var sub = new Submarine("Sub 1");
            var wallet = new Wallet(gold: 1000, materials: 0);

            Assert.IsFalse(sub.TryUpgrade(UpgradeType.Speed, wallet));
            Assert.AreEqual(1, sub.SpeedLevel);
            Assert.AreEqual(1000, wallet.Gold);
        }

        [Test]
        public void TryUpgrade_AtMaxLevel_Fails()
        {
            var sub = new Submarine("Sub 1");
            var wallet = new Wallet(gold: 100000, materials: 100);
            for (var i = UpgradeRules.MinLevel; i < UpgradeRules.MaxLevel; i++)
                Assert.IsTrue(sub.TryUpgrade(UpgradeType.Speed, wallet));

            Assert.IsTrue(sub.IsMaxLevel(UpgradeType.Speed));
            var goldBefore = wallet.Gold;
            Assert.IsFalse(sub.TryUpgrade(UpgradeType.Speed, wallet));
            Assert.AreEqual(goldBefore, wallet.Gold);
        }

        [Test]
        public void SpeedLevel_ShortensNextVoyage_ButNotOneAlreadyAtSea()
        {
            var sub = new Submarine("Sub 1");
            sub.Depart(DeepSea(), T0, 60); // 60 real seconds at level 1
            sub.TryUpgrade(UpgradeType.Speed, new Wallet(gold: 100, materials: 1));

            Assert.AreEqual(T0.AddSeconds(60), sub.ReturnAtUtc);

            var now = T0.AddSeconds(60);
            sub.Collect(now, new MaxRandom());
            sub.Depart(DeepSea(), now, 60);
            Assert.AreEqual(now.AddSeconds(54), sub.ReturnAtUtc); // x0.9
        }

        [Test]
        public void CargoLevel_ScalesReward_RoundedToNearest()
        {
            var sub = new Submarine("Sub 1");
            sub.TryUpgrade(UpgradeType.Cargo, new Wallet(gold: 100, materials: 1)); // level 2 = x1.2

            sub.Depart(DeepSea(), T0, 60);
            var reward = sub.Collect(T0.AddMinutes(5), new MaxRandom());

            Assert.AreEqual(540, reward.Gold);     // 450 x 1.2
            Assert.AreEqual(4, reward.Materials);  // 3 x 1.2 = 3.6 -> 4
        }

        [Test]
        public void GetRewardRange_IncludesCargoLevel()
        {
            var sub = new Submarine("Sub 1");
            sub.TryUpgrade(UpgradeType.Cargo, new Wallet(gold: 100, materials: 1)); // level 2 = x1.2

            var (min, max) = sub.GetRewardRange(DeepSea());

            Assert.AreEqual(360, min.Gold);       // 300 x 1.2
            Assert.AreEqual(1, min.Materials);    // 1 x 1.2 = 1.2 -> 1
            Assert.AreEqual(540, max.Gold);
            Assert.AreEqual(4, max.Materials);
        }

        [Test]
        public void IsFullyUpgraded_OnlyWhenBothUpgradesAreMax()
        {
            var sub = new Submarine("Sub 1");
            var wallet = new Wallet(gold: 100000, materials: 100);
            for (var i = 0; i < 4; i++) sub.TryUpgrade(UpgradeType.Cargo, wallet);
            Assert.IsFalse(sub.IsFullyUpgraded);

            for (var i = 0; i < 4; i++) sub.TryUpgrade(UpgradeType.Speed, wallet);
            Assert.IsTrue(sub.IsFullyUpgraded);
        }
    }

    public class FleetTests
    {
        [Test]
        public void NewFleet_HasOneFreeSubmarine()
        {
            var fleet = new Fleet();
            Assert.AreEqual(4, fleet.SlotCount);
            Assert.AreEqual(1, fleet.UnlockedCount);
            Assert.IsTrue(fleet.IsUnlocked(0));
            Assert.IsTrue(fleet.IsNextToUnlock(1));
            Assert.IsFalse(fleet.IsNextToUnlock(2));
        }

        [Test]
        public void TryUnlockNext_SpendsCost_InOrder()
        {
            var fleet = new Fleet();
            var wallet = new Wallet(gold: 2000);

            Assert.IsTrue(fleet.TryUnlockNext(wallet));   // slot 1: 500
            Assert.AreEqual(1500, wallet.Gold);
            Assert.IsTrue(fleet.TryUnlockNext(wallet));   // slot 2: 1500
            Assert.AreEqual(0, wallet.Gold);
            Assert.IsFalse(fleet.TryUnlockNext(wallet));  // slot 3: 4000, cannot afford
            Assert.AreEqual(3, fleet.UnlockedCount);
            Assert.AreEqual("Submarine 3", fleet.Submarines[2].Name);
        }

        [Test]
        public void TryUnlockNext_WhenAllUnlocked_ReturnsFalse()
        {
            var fleet = new Fleet();
            var wallet = new Wallet(gold: 100000);
            for (var i = 1; i < fleet.SlotCount; i++) Assert.IsTrue(fleet.TryUnlockNext(wallet));

            Assert.IsFalse(fleet.TryUnlockNext(wallet));
            Assert.AreEqual(100000 - 500 - 1500 - 4000, wallet.Gold);
        }
    }
}
