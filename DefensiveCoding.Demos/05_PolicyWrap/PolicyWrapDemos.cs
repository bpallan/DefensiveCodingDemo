using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DefensiveCoding.Demos.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace DefensiveCoding.Demos._05_PolicyWrap
{
    [TestClass]
    public class PolicyWrapDemos
    {
        // Http Async
        [TestMethod]
        public async Task AsyncHttpCall()
        {

            IAsyncPolicy<HttpResponseMessage> fallbackPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .Or<BrokenCircuitException>()
                .FallbackAsync(new HttpResponseMessage
                {                   
                    // fetch default values or execue whatever behavior you want to fall back to
                    Content = new StringContent("Default!")
                });

            IAsyncPolicy<HttpResponseMessage> outerTimeoutPolicy = Policy
                .TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10), TimeoutStrategy.Optimistic);

            IAsyncPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>() // retry on inner timeout (you decide if this fits your use case), don't retry on circuit breaker exception
                .WaitAndRetryAsync(1, retryAttempt => TimeSpan.FromMilliseconds(500));

            IAsyncPolicy<HttpResponseMessage> circuitBreakerPolicy = Policy
                .HandleResult<HttpResponseMessage>(resp => !resp.IsSuccessStatusCode) // consider if you want to ignore 404s
                .Or<Exception>()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(5)); // shortening for demo, should be 30 or 60 seconds

            IAsyncPolicy<HttpResponseMessage> innerTimeoutPolicy = Policy
                .TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(3), TimeoutStrategy.Pessimistic);  // had to set to pessimistic to get this to work, todo: figure out why

            // wrap policies from outer to inner
            var resiliencyPolicy = Policy
                .WrapAsync<HttpResponseMessage>(fallbackPolicy, outerTimeoutPolicy, retryPolicy, circuitBreakerPolicy, innerTimeoutPolicy);

            // verify retry on error
            var response = await resiliencyPolicy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/error?failures=1"));
            Assert.IsTrue(response.IsSuccessStatusCode);

            // verify retry on client timeout           
            response = await retryPolicy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/slow?failures=1", CancellationToken.None));
            Assert.IsTrue(response.IsSuccessStatusCode);

            // verify default if every call times out
            DemoHelper.Reset();
            response = await resiliencyPolicy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/slow", CancellationToken.None)); 
            var result = await response.Content.ReadAsStringAsync();
            Assert.AreEqual("Default!", result);

            // verify circuit broken on errors    
            response = await retryPolicy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/error", CancellationToken.None));
            Assert.AreEqual("Default!", result);
        }

        // Http Syc

        // 401 Retry

        // Non-Http

        [TestCleanup]
        public void Cleanup()
        {
            DemoHelper.Reset();
        }
    }
}
