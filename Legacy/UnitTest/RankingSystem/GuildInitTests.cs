using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cerlancism.TieredEloRankingSystem;
using Cerlancism.TieredEloRankingSystem.Models;
using System.IO;

namespace UnitTest.RankingSystem
{
    [TestClass]
    public class GuildInitTests
    {
        string connectionString = "RankingTest.db";

        [TestMethod]
        public void InitGuildTest()
        {
            GuildSetting setting;
            using (var rankSystem = new RankingProcessor(123, connectionString))
            {
                // Arrange
                var channelId = 12345UL;

                // Act
                setting = rankSystem.InitialiseGuild(channelId);
            }
            using (var rankSystem = new RankingProcessor(123, connectionString))
            {
                // Assert
                var fromDbSetting = rankSystem.GuildSetting.Value;
                Assert.AreEqual(fromDbSetting.GuildId, setting.GuildId);
            }
        }

        [TestCleanup]
        public void CleanUp()
        {
            // Clean up
            // File.Delete(connectionString);
        }
    }
}
