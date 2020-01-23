using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Text;

namespace Cerlancism.ChatSystem.Extensions
{
    public static class ObjectCacheExtensions
    {
        public static T AddOrGetExisting<T>(this ObjectCache cache, string key, Func<T> valueFactory, CacheItemPolicy policy)
        {
            var newValue = new Lazy<T>(valueFactory);
            var oldValue = cache.AddOrGetExisting(key, newValue, policy) as Lazy<T>;

            try
            {
                return (oldValue ?? newValue).Value;
            }
            catch (Exception e)
            {
                cache.Remove(key);
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
