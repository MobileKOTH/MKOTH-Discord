using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cerlancism.TieredEloRankingSystem.Utilities;
using Cerlancism.TieredEloRankingSystem.Models;

namespace UnitTest.RankingSystem
{
    [TestClass]
    public class GeneralTests
    {
        [TestMethod]
        public void PercentileTest()
        {
            // Arrange
            var values = new float[] { 1180, 1220, 1160, 1240, 1120.43f, 1123.23f, 1330.22f };
            var percentile = 0.75f;

            // Act
            var percentileValue = PercentileCalculator.GetPercentile(values, percentile);

            // Assert
            Assert.IsTrue(percentileValue > 1200);
            Console.WriteLine(percentileValue);
        }

        [TestMethod]
        public void CastTest()
        {
            // Arrange
            var activePlayer = new ActivePlayer();

            // Act
            var holidayPlayer = TypeCaster.CastToNew<HolidayPlayer>(activePlayer);

            // Assert
            Assert.IsTrue(holidayPlayer is HolidayPlayer);
        }
    }
}
