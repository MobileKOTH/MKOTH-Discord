using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cerlancism.ChatSystem;

namespace UnitTest.ChatSystem
{
    [TestClass]
    public class BasicTests
    {
        [TestMethod]
        public void TrimMessageTests()
        {
            // Arrange
            var testMessage = "Hi! This is a test messsage. +_)(*&^%$#@!~=-\\][|}{';\":/.,?><你好";

            // Act
            var trimed = Chat.TrimMessage(testMessage);

            // Assert
            Assert.AreNotEqual(testMessage, trimed);
            Console.WriteLine(trimed);
        }
    }
}
