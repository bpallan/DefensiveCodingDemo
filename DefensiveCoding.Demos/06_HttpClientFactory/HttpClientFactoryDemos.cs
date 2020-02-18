using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DefensiveCoding.Demos.Extensions;
using DefensiveCoding.Demos.Factories;
using DefensiveCoding.Demos.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using Polly.CircuitBreaker;

namespace DefensiveCoding.Demos._06_HttpClientFactory
{
    [TestClass]
    public class HttpClientFactoryDemos
    {        
        /// <summary>
        /// Demonstrates how to add multiple policies directly to HttpClientFactory during Startup
        /// The actual policy definitions are less relevant to this demo so I moved them to a DemoPolicyFactory class to clean up this demo
        /// Typically you would inject an HttpClientFactory or HttpClient into your class instead of pulling it directly from services
        /// I am using named clients as I prefer that approach.  It is easy to modify this code to inject an HttpClient into a specific class
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task HttpClientFactory_ApplyMultiplePolicies()
        {
            IServiceCollection services = new ServiceCollection();          

            ///// ADD HTTP CLIENT VIA HTTPCLIENTFACTORY AND POLICIES            
            // this would typically happen in Startup.ConfigureServices
            // using a named client, can specify a <TClient> to inject it into a specific class

            // setup circuit breaker seperately so you can maintain a pointer to it for testing (validate state, reset, etc)
            var circuitBreakerPolicy = DemoPolicyFactory.GetHttpCircuitBreakerPolicy();
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
                .AddPolicyHandler(DemoPolicyFactory.GetHttpFallbackPolicy("Default!"))
                .AddPolicyHandler(DemoPolicyFactory.GetHttpRetryPolicy())
                .AddPolicyHandler(circuitBreakerPolicy)
                .AddPolicyHandler(DemoPolicyFactory.GetHttpInnerTimeoutPolicy());

            ///// TEST POLICIES /////            
            await VerifyResiliencyPolicies(services, circuitBreakerPointer);
        }

        /// <summary>
        /// Demonstrates how to add policies to HttpClientFactory via extension method.
        /// The extension method is located in the Extensions folder (HttpClientBuilderExtensions)
        /// A reference to the circuit breaker is provided via out parameter so it can be referenced for testing (check status, reset, etc)
        /// </summary>
        /// <returns></returns>
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
                .AddResiliencyPolicies(out var circuitBreakerPointer, "Default!");
            await VerifyResiliencyPolicies(services, circuitBreakerPointer);
        }

        /// <summary>
        /// Demonstrate only applying a retry policy if we are doing a get
        /// Might be useful to prevent duplicates if request is not idempotent
        /// Other policies can still be applied to all (not shown)
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task HttpClientFactory_OnlyApplyPolicyToGetRequests()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddHttpClient("CustomerService",
                    client =>
                    {
                        client.BaseAddress = new Uri(DemoHelper.DemoBaseUrl);
                        client.Timeout =
                            TimeSpan.FromSeconds(3);
                    })
                .AddPolicyHandler(request =>
                    request.Method == HttpMethod.Get
                        ? DemoPolicyFactory.GetHttpRetryPolicy()
                        : Policy.NoOpAsync<HttpResponseMessage>());
            var serviceProvider = services.BuildServiceProvider();
            var clientFactory = serviceProvider.GetService<IHttpClientFactory>();
            var httpClient = clientFactory.CreateClient("CustomerService");

            var responseMessage = await httpClient.GetAsync("api/demo/error?failures=1");

            Assert.IsTrue(responseMessage.IsSuccessStatusCode);
        }

        // extracted this out to a seperate method since the above 2 demos are accomplishing the exact same thing
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
