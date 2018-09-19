using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using LiteDB;

namespace Cerlancism.ChatSystem
{
    public static class Tester
    {
        public static void TestMethod()
        {
            Console.WriteLine($"Test {typeof(Tester).FullName}");

            var obj = JsonConvert.DeserializeObject<dynamic>("{\"Test\": \"Test\"}");
            Console.WriteLine(obj.Test);

            using (var db = new LiteDatabase("Test.db"))
            {
                var collection = db.GetCollection<IdString>("test");
                collection.Insert(new IdString { Value = "Test"});
                Console.WriteLine($"test: {collection.FindAll().First().Value}");
                db.DropCollection(collection.Name);
            }
        }

        public class IdString
        {
            public int Id { get; set; }
            public string Value { get; set; }
        }

    }
}
