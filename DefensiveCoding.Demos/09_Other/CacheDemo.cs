using System;
using System.Threading.Tasks;
using DefensiveCoding.Demos.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using Polly.Caching.Memory;

namespace DefensiveCoding.Demos._09_Other
{
    [TestClass]
    public class CacheDemo
    {
        /// <summary>
        /// Demonstrate using the cache policy
        /// The first 2 calls are the same so the result is the same (2nd call not made)
        /// The 3rd uses a different cache key so it gets a new value
        /// Note: Polly also supports distributed cache
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task CanCacheApiResult()
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var memoryCacheProvider = new MemoryCacheProvider(memoryCache);
            var cachePolicy = Policy.CacheAsync(memoryCacheProvider, TimeSpan.FromMinutes(5));

            // typically cache key would be dynamically generated to be unique per unique request (same for equivalent requests)
            var result = await cachePolicy.ExecuteAsync((ctx) => DemoHelper.DemoClient.GetStringAsync("api/demo/cache"), new Context("MyKey"));
            var result2 = await cachePolicy.ExecuteAsync((ctx) => DemoHelper.DemoClient.GetStringAsync("api/demo/cache"), new Context("MyKey"));
            var result3 = await cachePolicy.ExecuteAsync((ctx) => DemoHelper.DemoClient.GetStringAsync("api/demo/cache"), new Context("MyKey2"));
            
            Assert.AreEqual(result, result2);
            Assert.AreNotEqual(result, result3);
        }
    }
}