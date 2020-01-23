using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cerlancism.TieredEloRankingSystem;
using Cerlancism.TieredEloRankingSystem.Models;
using System.IO;
using Newtonsoft.Json;

namespace UnitTest.RankingSystem
{
    [TestClass]
    public class PlayerTests
    {
        private RankingProcessor rankSystem = new RankingProcessor(123, "TestRank.db");
        
        [TestMethod]
        public void AddPlayerTest()
        {
            // Arrange 
            rankSystem.InitialiseGuild(1245);
            var playerName1 = "TestPlayer1";
            var playerName2 = "TestPlayer2";

            //Act
            var action1 = rankSystem.AddPlayer(playerName1, 1234);
            var action2 = rankSystem.AddPlayer(playerName2, 1235);

            // Assert
            var canFind = rankSystem.GetActivePlayers().Any(x => x.Name == playerName1);
            Assert.IsTrue(canFind);
            Console.WriteLine(JsonConvert.SerializeObject(action1, Formatting.Indented));
        }

        [TestCleanup]
        public void CleanUp()
        {
            rankSystem.Dispose();
        }
    }
}
