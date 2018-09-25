using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace Cerlancism.ChatSystem.Utilities
{
    public static class DatabaseExtensions
    {
        public static LiteDatabase GetAndSetDataBase(out LiteDatabase outDatabase, string connectionString)
        {
            outDatabase = new LiteDatabase(connectionString);
            return outDatabase;
        }

        public static LiteCollection<T> GetAndSetCollection<T>(this LiteDatabase database, out LiteCollection<T> collection, string name = null)
        {
            collection = name != null ? database.GetCollection<T>(name) : database.GetCollection<T>();
            return collection;
        }
    }
}
