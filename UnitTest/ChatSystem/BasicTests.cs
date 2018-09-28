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
            var trimed = Chat.RemovePunctuations(testMessage);

            // Assert
            Assert.AreNotEqual(testMessage, trimed);
            Console.WriteLine(trimed);
        }

        [TestMethod]
        public void CollectionSpeedTest_EnumerableSlowest()
        {
            // Arrange
            var rng = new Random();
            var source = new SortedList<int, double>(5000000);
            for (int i = 0; i < source.Capacity; i++)
            {
                source.Add(i, rng.NextDouble());
            }

            // Act
            (int, double) testVar; 
            var stopWatch = Stopwatch.StartNew();

            var collection = new ConcurrentBag<(int, double)>();
            source.AsParallel()
                .ForAll(x => collection.Add((x.Key, x.Value)));

            var ordered = collection.AsParallel().OrderBy(x => x.Item1);

            stopWatch.Stop();
            var concurrentTime = stopWatch.Elapsed.TotalMilliseconds;

            var list = ordered.ToList();
            var array = ordered.ToArray();
            var enumerable = ordered.ToList().AsEnumerable();

            stopWatch.Restart();

            for (int i = 0; i < list.Count; i++)
            {
                testVar = list[i];
                testVar = default;
            }

            stopWatch.Stop();
            var listTime = stopWatch.Elapsed.TotalMilliseconds;

            for (int i = 0; i < list.Count; i++)
            {
                testVar = list[i];
                if (testVar.Item1 != i)
                {
                    //Console.WriteLine($"{testVar} {i}");
                    Assert.Fail();
                }
            }

            stopWatch.Restart();

            for (int i = 0; i < array.Length; i++)
            {
                testVar = array[i];
                testVar = default;
            }

            stopWatch.Stop();
            var arrayTime = stopWatch.Elapsed.TotalMilliseconds;

            stopWatch.Restart();
            var length = enumerable.Count();
            for (int i = 0; i < length; i++)
            {
                testVar = enumerable.ElementAtOrDefault(i);
                testVar = default;
            }

            stopWatch.Stop();
            var enumerableTime = stopWatch.Elapsed.TotalMilliseconds;

            // Assert
            Console.WriteLine(
                $"concurrentTime: {concurrentTime} ms \n" +
                $"enumerableTime: {enumerableTime} ms \n" +
                $"listTime: {listTime} ms \n" +
                $"arrayTime: {arrayTime} ms \n");
            Assert.IsTrue((enumerableTime > listTime) && (listTime > arrayTime));
        }
    }
}
