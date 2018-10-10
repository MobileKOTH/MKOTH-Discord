using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Cerlancism.TieredEloRankingSystem.Utilities
{
    public static class TypeCaster
    {
        static JsonSerializerSettings serializerSettings => new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
        };

        public static T DownCastToNew<T, U>(U inObject) where T : U
        {
            var json = JsonConvert.SerializeObject(inObject, serializerSettings);
            var newObject = JsonConvert.DeserializeObject<T>(json, serializerSettings);
            return newObject;
        }

        public static T UpCastToNew<T, U>(U inObject) where U : T
        {
            var json = JsonConvert.SerializeObject(inObject, serializerSettings);
            var newObject = JsonConvert.DeserializeObject<T>(json, serializerSettings);
            return newObject;
        }

        public static T CastToNew<T>(object inObject)
        {
            var json = JsonConvert.SerializeObject(inObject, serializerSettings);
            var newObject = JsonConvert.DeserializeObject<T>(json, serializerSettings);
            return newObject;
        }
    }
}
