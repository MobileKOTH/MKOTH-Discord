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
    public class ActionTests
    {
        private RankingProcessor rankSystem = new RankingProcessor(123, "TestRank.db");

        [TestMethod]
        public void TestUndoLastAction()
        {
            // Arrange
            var lastAction = rankSystem.LastAction;

            // Action
            var undoedAction = rankSystem.UndoLastAction();
            rankSystem.RedoLastUndo();

            // Assert

        }

        [TestCleanup]
        public void CleanUp()
        {
            rankSystem.Dispose();
        }
    }
}
