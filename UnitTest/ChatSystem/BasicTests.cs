using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cerlancism.ChatSystem;

namespace UnitTest.ChatSystem
{
    [TestClass]
    public class BasicTests
    {
        [TestMethod]
        public void TrimMessageTest()
        {
            // Arrange
            var testMessage = "Hi! This is a test messsage. +_)(*&^%$#@!~=-\\][|}{';\":/.,?><你好";

            // Act
            var trimed = Chat.TrimMessage(testMessage);

            // Assert
            Assert.AreNotEqual(testMessage, trimed);
            Console.WriteLine(trimed);
        }

        [TestMethod]
        public void CollectionSpeedTest_EnumerableSlowest()
        {
            // Arrange
            var rng = new Random();
            var source = new List<(int, double)>(1000000);
            var collection = new ConcurrentQueue<(int, double)>();
            for (int i = 0; i < source.Capacity; i++)
            {
                source.Add((i, rng.NextDouble()));
            }

            source.AsParallel()
                .AsOrdered()
                .ForAll(collection.Enqueue);

            var list = collection.ToList();
            var array = collection.ToArray();
            var enumerable = collection.ToList().AsEnumerable();

            // Act
            var stopWatch = Stopwatch.StartNew();
            (int, double) testVar; 
            for (int i = 0; i < list.Count; i++)
            {
                testVar = list[i];
                if (testVar.Item1 != i)
                {
                    Console.WriteLine($"{testVar} {i}");
                    Assert.Fail();
                }
                testVar = default;
            }

            stopWatch.Stop();
            var listTime = stopWatch.Elapsed.TotalMilliseconds;

            stopWatch.Start();

            for (int i = 0; i < array.Length; i++)
            {
                testVar = array[i];
                testVar = default;
            }

            stopWatch.Stop();
            var arrayTime = stopWatch.Elapsed.TotalMilliseconds;

            stopWatch.Restart();
            var length = enumerable.Count();
            for (int i = 0; i < enumerable.Count(); i++)
            {
                testVar = enumerable.ElementAtOrDefault(i);
                testVar = default;
            }

            stopWatch.Stop();
            var enumerableTime = stopWatch.Elapsed.TotalMilliseconds;

            // Assert
            //Assert.IsTrue((enumerableTime > listTime) && (listTime > arrayTime));
            Console.WriteLine($"enumerableTime: {enumerableTime} ms \nlistTime: {listTime} ms \narrayTime: {arrayTime} ms");
        }
    }
}
