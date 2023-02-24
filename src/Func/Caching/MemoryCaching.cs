using System.Runtime.Caching;

namespace CodeLogic
{
    public partial class CodeLogic_Funcs
    {
        // Types

        public static object GetCachedObject(string key)
        {
            ObjectCache cache = MemoryCache.Default;
            return cache.Get(key);
        }

        public static string GetCachedString(string key)
        {
            var value = Convert.ToString(GetCachedObject(key));
            return value;
        }

        public static bool GetCacheBool(string key)
        {
            var value = Convert.ToBoolean(GetCachedObject(key));
            return value;
        }
        public static int GetCacheInt(string key)
        {
            var value = Convert.ToInt32(GetCachedObject(key));
            return value;
        }
        public static object SetCachedObject(string key, object objectInput, int lifespanSecs)
        {
            ObjectCache cache = MemoryCache.Default;

            // Store data in the cache
            CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();
            cacheItemPolicy.AbsoluteExpiration = DateTime.Now.AddSeconds(lifespanSecs);
            cache.Add(key, objectInput, cacheItemPolicy);
            return objectInput;
        }
        public static void RemoveCachedObject(string key)
        {
            ObjectCache cache = MemoryCache.Default;
            cache.Remove(key);
        }

        public static bool CheckCachedObject(string key)
        {
            ObjectCache cache = MemoryCache.Default;
            return cache.Contains(key);
        }
    }
}