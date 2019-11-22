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
        /// <summary>
        /// Demonstrates how to use the policy registry to define 2 different policies for 2 different dependencies
        /// The registry is built using the PolicyRegistryFactory class in the Factories folder
        /// Typically the registry is setup in Startup.ConfigureServices and you inject into the classes that need it
        /// Not demo'ed here but you could add an out parameter to capture a pointer to the circuit breaker if you need to do so for testing
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Demonstrates injecting the policy registry into HttpClientFactory        
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task PolicyRegistry_WithHttpClientFactory()
        {
            // typically this stuff would go in Startup.ConfigureServices
            IServiceCollection services = new ServiceCollection();
            services.AddPolicyRegistry(PolicyRegistryFactory.GetPolicyRegistry());

            services.AddHttpClient("CustomerService",
                    client =>
                    {
                        client.BaseAddress = new Uri(DemoHelper.DemoBaseUrl);
                        client.Timeout =
                            TimeSpan.FromSeconds(
                                3); // this will apply as an outer timeout to the entire request flow, including waits, retries,etc
                    })
                .AddPolicyHandlerFromRegistry("CustomerServicePolicy");

            // typically you would inject http client factory into class via constructor
            var serviceProvider = services.BuildServiceProvider();
            var clientFactory = serviceProvider.GetService<IHttpClientFactory>();

            // create client using factory
            var httpClient = clientFactory.CreateClient("CustomerService");

            // verify retry on error
            var responseMessage = await httpClient.GetStringAsync("api/demo/error?failures=1");
            Assert.AreEqual("Success!", responseMessage);
        }

        [TestCleanup]
        public void Cleanup()
        {
            DemoHelper.Reset();
        }
    }
}
