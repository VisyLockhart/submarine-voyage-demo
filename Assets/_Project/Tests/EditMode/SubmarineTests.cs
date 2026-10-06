using System;
using NUnit.Framework;

namespace SubmarineVoyage.Core.Tests
{
    public class SubmarineTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);

        // 10 game minutes, gold 50-80, materials 0-1 (GDD "near sea" initial values).
        private static Route NearSea() =>
            new Route("near", "Near Sea", TimeSpan.FromMinutes(10), 50, 80, 0, 1);

        private sealed class FixedRandom : IRandomSource
        {
            private readonly bool _useMax;
            public FixedRandom(bool useMax) { _useMax = useMax; }
            public int Range(int minInclusive, int maxInclusive) => _useMax ? maxInclusive : minInclusive;
        }

        [Test]
        public void NewSubmarine_IsIdle()
        {
            var sub = new Submarine("Sub 1");
            Assert.AreEqual(SubmarineState.Idle, sub.GetState(T0));
            Assert.AreEqual(TimeSpan.Zero, sub.GetRemaining(T0));
        }

        [Test]
        public void Depart_AppliesTimeScale_ToRealDuration()
        {
            var sub = new Submarine("Sub 1");
            sub.Depart(NearSea(), T0, timeScale: 60);

            // 10 game minutes at 60x = 10 real seconds.
            Assert.AreEqual(T0.AddSeconds(10), sub.ReturnAtUtc);
            Assert.AreEqual(SubmarineState.Voyaging, sub.GetState(T0.AddSeconds(9.9)));
            Assert.AreEqual(TimeSpan.FromSeconds(4), sub.GetRemaining(T0.AddSeconds(6)));
        }

        [Test]
        public void AfterReturnTime_IsReadyToCollect()
        {
            var sub = new Submarine("Sub 1");
            sub.Depart(NearSea(), T0, 60);

            Assert.AreEqual(SubmarineState.ReadyToCollect, sub.GetState(T0.AddSeconds(10)));
            // Long after (e.g. the app was closed) it is still ready, not lost.
            Assert.AreEqual(SubmarineState.ReadyToCollect, sub.GetState(T0.AddDays(3)));
        }

        [Test]
        public void Collect_ReturnsRewardInRange_AndGoesIdle()
        {
            var sub = new Submarine("Sub 1");
            sub.Depart(NearSea(), T0, 60);
            var now = T0.AddSeconds(10);

            var reward = sub.Collect(now, new FixedRandom(useMax: true));

            Assert.AreEqual(80, reward.Gold);
            Assert.AreEqual(1, reward.Materials);
            Assert.AreEqual(SubmarineState.Idle, sub.GetState(now));
            Assert.IsNull(sub.CurrentRoute);
        }

        [Test]
        public void Depart_WhileVoyaging_Throws()
        {
            var sub = new Submarine("Sub 1");
            sub.Depart(NearSea(), T0, 60);
            Assert.Throws<InvalidOperationException>(() => sub.Depart(NearSea(), T0.AddSeconds(1), 60));
        }

        [Test]
        public void Collect_BeforeReturn_Throws()
        {
            var sub = new Submarine("Sub 1");
            Assert.Throws<InvalidOperationException>(() => sub.Collect(T0, new FixedRandom(false)));

            sub.Depart(NearSea(), T0, 60);
            Assert.Throws<InvalidOperationException>(() => sub.Collect(T0.AddSeconds(5), new FixedRandom(false)));
        }
    }

    public class RouteTests
    {
        [Test]
        public void GetRealDuration_SixHourRouteAt60x_IsSixMinutes()
        {
            var farSea = new Route("far", "Far Sea", TimeSpan.FromHours(6), 2000, 3000, 3, 6);
            Assert.AreEqual(TimeSpan.FromMinutes(6), farSea.GetRealDuration(60));
        }

        [Test]
        public void GetRealDuration_NonPositiveTimeScale_Throws()
        {
            var route = new Route("near", "Near Sea", TimeSpan.FromMinutes(10), 50, 80, 0, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => route.GetRealDuration(0));
        }

        [Test]
        public void Constructor_MaxBelowMin_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new Route("bad", "Bad", TimeSpan.FromMinutes(1), 80, 50, 0, 1));
        }
    }

    public class WalletTests
    {
        [Test]
        public void Add_IncreasesGoldAndMaterials()
        {
            var wallet = new Wallet();
            wallet.Add(new Reward(60, 1));
            Assert.AreEqual(60, wallet.Gold);
            Assert.AreEqual(1, wallet.Materials);
        }

        [Test]
        public void TrySpend_FailsWhenNotEnough_AndLeavesGoldUnchanged()
        {
            var wallet = new Wallet(gold: 100);
            Assert.IsFalse(wallet.TrySpend(101));
            Assert.AreEqual(100, wallet.Gold);
            Assert.IsTrue(wallet.TrySpend(100));
            Assert.AreEqual(0, wallet.Gold);
        }
    }
}
