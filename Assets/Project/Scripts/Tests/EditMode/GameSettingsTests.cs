using Fabula.Incrementant.Settings;
using NUnit.Framework;

namespace Fabula.Incrementant.Tests
{
    public sealed class GameSettingsTests
    {
        [Test]
        public void JsonRoundTripKeepsEveryField()
        {
            var data = new GameSettingsData
            {
                MasterVolume = 0.4f,
                MusicVolume = 0.1f,
                SfxVolume = 0.9f,
                Vfx = VfxLevel.Reduced,
                Fullscreen = false,
                ResolutionWidth = 1280,
                ResolutionHeight = 720,
                VSync = false,
                FpsLimit = 144,
                ShowStats = false,
                BlockBuildOnRooms = false,
                Bloom = false,
                ShowRooms = false,
            };

            GameSettingsData read = GameSettingsData.FromJson(data.ToJson());

            Assert.AreEqual(0.4f, read.MasterVolume, 1e-5f);
            Assert.AreEqual(0.1f, read.MusicVolume, 1e-5f);
            Assert.AreEqual(0.9f, read.SfxVolume, 1e-5f);
            Assert.AreEqual(VfxLevel.Reduced, read.Vfx);
            Assert.IsFalse(read.Fullscreen);
            Assert.AreEqual(1280, read.ResolutionWidth);
            Assert.AreEqual(720, read.ResolutionHeight);
            Assert.IsFalse(read.VSync);
            Assert.AreEqual(144, read.FpsLimit);
            Assert.IsFalse(read.ShowStats);
            Assert.IsFalse(read.BlockBuildOnRooms);
            Assert.IsFalse(read.Bloom);
            Assert.IsFalse(read.ShowRooms);
        }

        [Test]
        public void BloomIsOnByDefaultAndForOldFiles()
        {
            Assert.IsTrue(new GameSettingsData().Bloom);
            Assert.IsTrue(GameSettingsData.FromJson("{\"MasterVolume\": 0.5}").Bloom);
        }

        [Test]
        public void BlockBuildOnRoomsIsOnByDefaultAndForOldFiles()
        {
            Assert.IsTrue(new GameSettingsData().BlockBuildOnRooms);
            Assert.IsTrue(GameSettingsData.FromJson("{\"MasterVolume\": 0.5}").BlockBuildOnRooms);
        }

        [Test]
        public void ShowRoomsIsOnByDefaultAndForOldFiles()
        {
            Assert.IsTrue(new GameSettingsData().ShowRooms);
            Assert.IsTrue(GameSettingsData.FromJson("{\"MasterVolume\": 0.5}").ShowRooms);
        }

        [Test]
        public void EmptyOrBrokenJsonGivesDefaults()
        {
            var defaults = new GameSettingsData();
            foreach (string json in new[] { null, "", "   ", "{ isso não é json" })
            {
                GameSettingsData read = GameSettingsData.FromJson(json);
                Assert.AreEqual(defaults.MasterVolume, read.MasterVolume, json);
                Assert.AreEqual(defaults.FpsLimit, read.FpsLimit, json);
                Assert.AreEqual(defaults.Vfx, read.Vfx, json);
            }
        }

        [Test]
        public void MissingFieldsKeepDefaults()
        {
            GameSettingsData read = GameSettingsData.FromJson("{\"MasterVolume\": 0.5}");

            Assert.AreEqual(0.5f, read.MasterVolume, 1e-5f);
            Assert.AreEqual(new GameSettingsData().SfxVolume, read.SfxVolume, 1e-5f);
            Assert.IsTrue(read.VSync);
        }

        [Test]
        public void ClampFixesOutOfRangeValues()
        {
            GameSettingsData read = GameSettingsData.FromJson(
                "{\"MasterVolume\": 3, \"MusicVolume\": -1, \"Vfx\": 9, \"FpsLimit\": 75, \"ResolutionWidth\": 800, \"ResolutionHeight\": 0}");

            Assert.AreEqual(1f, read.MasterVolume);
            Assert.AreEqual(0f, read.MusicVolume);
            Assert.AreEqual(VfxLevel.Full, read.Vfx);
            Assert.AreEqual(60, read.FpsLimit);
            Assert.AreEqual(0, read.ResolutionWidth);
            Assert.AreEqual(0, read.ResolutionHeight);
        }
    }
}
