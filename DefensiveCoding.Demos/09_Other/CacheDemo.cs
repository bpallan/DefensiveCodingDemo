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
        [TestMethod]
        public async Task CanCacheApiResult()
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var memoryCacheProvider = new MemoryCacheProvider(memoryCache);
            var cachePolicy = Policy.CacheAsync(memoryCacheProvider, TimeSpan.FromMinutes(5));

            var result = await cachePolicy.ExecuteAsync((ctx) => DemoHelper.DemoClient.GetStringAsync("api/demo/cache"), new Context("MyKey"));
        }
    }
}