using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DefensiveCoding.Demos.Extensions;
using DefensiveCoding.Demos.Factories;
using DefensiveCoding.Demos.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace DefensiveCoding.Demos._06_HttpClientFactory
{
    [TestClass]
    public class HttpClientFactoryDemos
    {        
        [TestMethod]
        public async Task HttpClientFactory_ApplyMultiplePolicies()
        {
            IServiceCollection services = new ServiceCollection();          

            ///// ADD HTTP CLIENT VIA HTTPCLIENTFACTORY AND POLICIES            
            // this would typically happen in Startup.ConfigureServices
            // using a named client, can specify a <TClient> to inject it into a specific class

            // setup circuit breaker seperately so you can maintain a pointer to it for testing (validate state, reset, etc)
            var circuitBreakerPolicy = DemoPolicyFactory.GetCircuitBreakerPolicy();
            ICircuitBreakerPolicy<HttpResponseMessage> circuitBreakerPointer = (ICircuitBreakerPolicy<HttpResponseMessage>)circuitBreakerPolicy;

            services.AddHttpClient("CustomerService",
                    client =>
                    {
                        client.BaseAddress = new Uri(DemoHelper.DemoBaseUrl);
                        client.Timeout =
                            TimeSpan.FromSeconds(
                                3); // this will apply as an outer timeout to the entire request flow, including waits, retries,etc
                    })

                // add policies from outer (1st executed) to inner (closest to dependency call)
                // all policies must implement IASyncPolicy
                .AddPolicyHandler(DemoPolicyFactory.GetFallbackPolicy())
                .AddPolicyHandler(DemoPolicyFactory.GetRetryPolicy())
                .AddPolicyHandler(circuitBreakerPolicy)
                .AddPolicyHandler(DemoPolicyFactory.GetInnerTimeoutPolicy());

            ///// TEST POLICIES /////            
            await VerifyResiliencyPolicies(services, circuitBreakerPointer);
        }

        [TestMethod]
        public async Task HttpClientFactory_ApplyPoliciesUsingExtensionMethod()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddHttpClient("CustomerService",
                    client =>
                    {
                        client.BaseAddress = new Uri(DemoHelper.DemoBaseUrl);
                        client.Timeout =
                            TimeSpan.FromSeconds(
                                3); // this will apply as an outer timeout to the entire request flow, including waits, retries,etc
                    })
                .AddResiliencyPolicies(out var circuitBreakerPointer);
            await VerifyResiliencyPolicies(services, circuitBreakerPointer);
        }

        private static async Task VerifyResiliencyPolicies(IServiceCollection services,
            ICircuitBreakerPolicy<HttpResponseMessage> circuitBreakerPointer)
        {
            // you would typically inject an IHttpClientFactory (if using named clients) or HttpClient (if not) into your classes that need them
            var serviceProvider = services.BuildServiceProvider();
            var clientFactory = serviceProvider.GetService<IHttpClientFactory>();

            // create client using factory
            var httpClient = clientFactory.CreateClient("CustomerService");

            // verify happy path
            var responseMessage = await httpClient.GetStringAsync("api/demo/success");
            Assert.AreEqual("Success!", responseMessage);

            // verify retry
            responseMessage = await httpClient.GetStringAsync("api/demo/error?failures=1");
            Assert.AreEqual("Success!", responseMessage);

            // verify timeout           
            responseMessage = await httpClient.GetStringAsync("api/demo/slow");
            Assert.AreEqual("Default!", responseMessage);

            // verify circuit breaker
            for (int i = 0; i < 5; i++)
            {
                await httpClient.GetStringAsync("api/demo/error");
            }

            Assert.AreEqual(CircuitState.Open, circuitBreakerPointer.CircuitState);
        }        

        [TestCleanup]
        public void Cleanup()
        {
            DemoHelper.Reset();
        }
    }    
}
