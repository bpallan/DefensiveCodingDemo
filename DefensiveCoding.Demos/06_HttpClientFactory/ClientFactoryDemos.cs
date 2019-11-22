using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    public class ClientFactoryDemos
    {
        [TestMethod]
        public async Task HttpClientFactory_ApplyMultiplePolicies()
        {
            IServiceCollection services = new ServiceCollection();

            ///// SETUP POLICIES /////
            // I would typically setup a policy factory for this
            // Fallback
            IAsyncPolicy<HttpResponseMessage> fallbackPolicy = Policy
                .HandleResult<HttpResponseMessage>(resp => !resp.IsSuccessStatusCode) // catch any bad responses, transient or not         
                .Or<Exception>() // handle ANY exception we get back
                .FallbackAsync(FallbackAction, PolicyLoggingHelper.LogFallbackAsync);

            // Retry
            IAsyncPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>() // handle timeouts as failures so they get retried, decide if this is appropriate for your use case
                .RetryAsync(1, onRetryAsync: PolicyLoggingHelper.LogRetryAsync);

            // Circuit Breaker
            IAsyncPolicy<HttpResponseMessage> circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .CircuitBreakerAsync(2, TimeSpan.FromSeconds(30));

            // very useful for testing, if you maintain a static pointer to each circuit breaker, you can verify state and reset in your tests
            var circuitBreakerPointer = (ICircuitBreakerPolicy<HttpResponseMessage>)circuitBreakerPolicy;

            // Inner Timeout (per call)
            IAsyncPolicy<HttpResponseMessage> timeoutPolicy 
                = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(1), TimeoutStrategy.Optimistic); // optimistic will work well with http client factory


            ///// ADD HTTP CLIENT VIA HTTPCLIENTFACTORY AND POLICIES
            // this would typically happen in Startup.ConfigureServices
            // using a named client, can specify a <TClient> to inject it into a specific class
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
                .AddPolicyHandler(fallbackPolicy)
                .AddPolicyHandler(retryPolicy)
                .AddPolicyHandler(circuitBreakerPolicy)
                .AddPolicyHandler(timeoutPolicy);

            ///// TEST POLICIES /////
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
            for (int i = 0; i < 3; i++)
            {
                responseMessage = await httpClient.GetStringAsync("api/demo/error");
            }

            Assert.AreEqual(CircuitState.Open, circuitBreakerPointer.CircuitState);
        }

        // demonstrate returning a mock response
        private Task<HttpResponseMessage> FallbackAction(DelegateResult<HttpResponseMessage> responseToFailedRequest, Context context, CancellationToken cancellationToken)
        {
            // mocking a successful response
            // you can pass in responseToFailedRequest.Result.StatusCode if you want to preserve the original error response code
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Default!")
            };
            return Task.FromResult(httpResponseMessage);
        }

        [TestCleanup]
        public void Cleanup()
        {
            DemoHelper.Reset();
        }
    }
}
