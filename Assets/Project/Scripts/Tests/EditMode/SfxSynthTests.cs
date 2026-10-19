using System;
using Fabula.Incrementant.Audio;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;

namespace Fabula.Incrementant.Tests
{
    public sealed class SfxSynthTests
    {
        static SfxId[] AllIds()
        {
            var ids = new SfxId[(int)SfxId.Count];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = (SfxId)i;
            return ids;
        }

        [Test]
        public void EverySoundRendersAudibleFiniteSamplesWithinRange()
        {
            foreach (SfxId id in AllIds())
            {
                float[] samples = SfxSynth.Render(id);
                Assert.AreEqual((int)(SfxSynth.Seconds(id) * SfxSynth.SampleRate), samples.Length, id.ToString());
                float peak = 0f;
                foreach (float s in samples)
                {
                    Assert.IsFalse(float.IsNaN(s) || float.IsInfinity(s), id + " tem amostra inválida");
                    peak = Math.Max(peak, Math.Abs(s));
                }
                Assert.That(peak, Is.InRange(0.85f, 0.95f), id + " deve sair normalizado");
            }
        }

        [Test]
        public void RenderIsDeterministic()
        {
            foreach (SfxId id in AllIds())
                CollectionAssert.AreEqual(SfxSynth.Render(id), SfxSynth.Render(id), id.ToString());
        }

        [Test]
        public void ShortSoundsEndSilentAndLoopsJoinWithoutAJump()
        {
            foreach (SfxId id in AllIds())
            {
                float[] samples = SfxSynth.Render(id);
                if (SfxSynth.IsLoop(id))
                {
                    // A emenda do laço (última -> primeira amostra) não pode ser um degrau grande.
                    Assert.Less(Math.Abs(samples[samples.Length - 1] - samples[0]), 0.35f, id.ToString());
                }
                else
                {
                    Assert.Less(Math.Abs(samples[samples.Length - 1]), 0.01f, id + " termina em degrau");
                }
            }
        }

        [Test]
        public void ColonyReportsRaidWarningAndStaysSilentWithoutListeners()
        {
            var grid = new PixelGrid(120, 40);
            var layout = new TerrainLayout(20, new UnityEngine.Vector2Int(50, 2), 60, 70);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 3);
            // Sem ouvinte nada acontece (e nada quebra).
            Assert.DoesNotThrow(() => colony.ForceRaid());

            var colony2 = new AntColony(grid, new ColonySettings(), layout, seed: 3);
            var heard = new System.Collections.Generic.List<SfxId>();
            colony2.Sfx += heard.Add;
            colony2.ForceRaid();
            CollectionAssert.Contains(heard, SfxId.RaidWarning);
            CollectionAssert.DoesNotContain(heard, SfxId.Buy);
        }
    }
}
