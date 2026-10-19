using Fabula.Incrementant.Juice;
using Fabula.Incrementant.Settings;
using NUnit.Framework;

namespace Fabula.Incrementant.Tests
{
    public class JuiceTests
    {
        static readonly Easing.Fn[] Curves =
        {
            Easing.OutQuad, Easing.InQuad, Easing.OutCubic, Easing.InOutSine, Easing.OutBack, Easing.OutElastic,
        };

        [Test]
        public void CurvesStartAtZeroAndEndAtOne()
        {
            foreach (Easing.Fn curve in Curves)
            {
                Assert.AreEqual(0f, curve(0f), 1e-4f);
                Assert.AreEqual(1f, curve(1f), 1e-4f);
            }
        }

        [Test]
        public void CurvesNeverReturnNaN()
        {
            foreach (Easing.Fn curve in Curves)
                for (int i = 0; i <= 100; i++)
                    Assert.IsFalse(float.IsNaN(curve(i / 100f)));
        }

        [Test]
        public void OutBackOvershootsOne()
        {
            Assert.Greater(Easing.OutBack(0.6f), 1f);
        }

        [Test]
        public void Limiter_BlocksWithinIntervalPerType()
        {
            var limiter = new FxLimiter(new[] { 0.1f, 0.5f, 0.1f, 0.1f, 0.1f, 0.1f });
            Assert.IsTrue(limiter.TryUse(FxId.Dig, 1f));
            Assert.IsFalse(limiter.TryUse(FxId.Dig, 1.05f));
            Assert.IsTrue(limiter.TryUse(FxId.Drop, 1.05f), "tipos diferentes não se bloqueiam");
            Assert.IsTrue(limiter.TryUse(FxId.Dig, 1.1f));
            Assert.IsFalse(limiter.TryUse(FxId.Drop, 1.4f));
            Assert.IsTrue(limiter.TryUse(FxId.Drop, 1.6f));
        }

        [Test]
        public void Aggregator_SumsWithinWindowAndFlushesOnce()
        {
            var agg = new FxAggregator(0.5f);
            Assert.IsFalse(agg.TryFlush(1f, out _), "sem eventos não solta nada");
            agg.Add(1, 1f);
            agg.Add(1, 1.2f);
            agg.Add(3, 1.4f);
            Assert.IsFalse(agg.TryFlush(1.4f, out _), "janela ainda aberta");
            Assert.IsTrue(agg.TryFlush(1.5f, out int total));
            Assert.AreEqual(5, total);
            Assert.IsFalse(agg.TryFlush(2f, out _), "já foi entregue");
            agg.Add(2, 3f);
            Assert.IsTrue(agg.TryFlush(3.5f, out total));
            Assert.AreEqual(2, total);
        }

        [Test]
        public void Aggregator_OpenOnlyBetweenFirstAddAndFlush()
        {
            // FloatingText usa Open para ancorar o número onde o primeiro evento da janela aconteceu.
            var agg = new FxAggregator(0.3f);
            Assert.IsFalse(agg.Open);
            agg.Add(1, 0f);
            Assert.IsTrue(agg.Open);
            agg.Add(1, 0.1f);
            Assert.IsTrue(agg.TryFlush(0.3f, out _));
            Assert.IsFalse(agg.Open);
        }

        [Test]
        public void ScaleFollowsVfxLevel()
        {
            Assert.AreEqual(8, Vfx.Scale(VfxLevel.Full, 8));
            Assert.AreEqual(4, Vfx.Scale(VfxLevel.Reduced, 8));
            Assert.AreEqual(1, Vfx.Scale(VfxLevel.Reduced, 1));
            Assert.AreEqual(0, Vfx.Scale(VfxLevel.Off, 8));
        }
    }
}
