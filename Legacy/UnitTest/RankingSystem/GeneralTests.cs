using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cerlancism.TieredEloRankingSystem.Utilities;
using Cerlancism.TieredEloRankingSystem.Models;
using System.Linq;
using LiteDB;
using System.IO;

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


        [TestMethod]
        public void DbRefTest()
        {
            var file = "TestDb.db";
            if (File.Exists(file))
            {
                File.Delete(file);
            }
            using (var db = new LiteRepository(file))
            {
                BsonMapper.Global.Entity<Player>().DbRef(x => x.Rank);
                var playerCollection = db.Query<Player>().Include(x => x.Rank);
                var rankCollection = db.Query<Rank>().Include(x => x.Player);

                var player = new Player
                {
                    Name = "Test"
                };

                player.Id = db.Insert(player);

                var rank = new Rank
                {
                    Player = player
                };

                rank.Id = db.Insert(rank);

                player.Rank = rank;

                db.Update(player);

                var dbPlayer = playerCollection.First();
                var dbRank = rankCollection.First();

                Console.WriteLine(dbPlayer);

            }
        }

        class Player
        {
            public int Id { get; set; }
            public string Name { get; set; }

            // Ref
            public Rank Rank { get; set; }
        }

        class Rank
        {
            public int Id { get; set; }

            [BsonRef]
            public Player Player { get; set; }
        }

    }
}
