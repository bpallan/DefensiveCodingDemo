using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DefensiveCoding.Demos.Factories;
using DefensiveCoding.Demos.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using Polly.Registry;

namespace DefensiveCoding.Demos._07_PolicyRegistry
{
    [TestClass]
    public class PolicyRegistryDemos
    {
        [TestMethod]
        public async Task PolicyRegistry_BasicExample()
        {
            // typically this stuff would go in Startup.ConfigureServices
            IServiceCollection services = new ServiceCollection();
            services.AddPolicyRegistry(PolicyRegistryFactory.GetPolicyRegistry());

            // typically you would inject this into a class via constructor
            var serviceProvider = services.BuildServiceProvider();
            var registry = serviceProvider.GetService<IReadOnlyPolicyRegistry<string>>();

            // customer service contains a retry to this will succeed
            var customerServicePolicy = registry.Get<IAsyncPolicy<HttpResponseMessage>>("CustomerServicePolicy");
            var response = await customerServicePolicy.ExecuteAsync(ct => DemoHelper.DemoClient.GetAsync("api/demo/error?failures=1", ct),
                CancellationToken.None);
            var responseMessage = await response.Content.ReadAsStringAsync();
            Assert.AreEqual("Success!", responseMessage);

            DemoHelper.Reset();
            
            // product service does not contain a retry, so this will fail (return default)
            var productServicePolicy = registry.Get<IAsyncPolicy<HttpResponseMessage>>("ProductServicePolicy");
            response = await productServicePolicy.ExecuteAsync(
                ct => DemoHelper.DemoClient.GetAsync("api/demo/error?failures=1", ct), CancellationToken.None);
            responseMessage = await response.Content.ReadAsStringAsync();
            Assert.AreEqual("Default!", responseMessage);
        }

        [TestCleanup]
        public void Cleanup()
        {
            DemoHelper.Reset();
        }
    }
}
