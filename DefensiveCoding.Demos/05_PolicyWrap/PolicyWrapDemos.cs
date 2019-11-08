using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DefensiveCoding.Api.Helpers;
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
        public async Task AddResiliencyToHttpCall()
        {
            // alternative would be to wrap ExecuteAsync in try/catch and return default from there
            IAsyncPolicy<HttpResponseMessage> fallbackPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .Or<BrokenCircuitException>()
                .FallbackAsync(new HttpResponseMessage
                {                   
                    // fetch default values or execue whatever behavior you want to fall back to
                    Content = new StringContent("Default!")
                });

            // for sync code, you should replace this by setting the timeout on the HttpClient
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

        // 401 Retry
        [TestMethod]
        public async Task RefreshTokenOnUnauthorized()
        {
            string token = "MyBadToken";
            bool giveGoodToken = true;

            var circuitBreaker401Policy = Policy
                .HandleResult<HttpResponseMessage>(resp => resp.StatusCode == HttpStatusCode.Unauthorized)
                .CircuitBreakerAsync(1, TimeSpan.FromSeconds(30)); // because this is wrapped in retry, a failure means we still got a 401 even after token refresh 

            var retry401Policy = Policy
                .HandleResult<HttpResponseMessage>(resp => resp.StatusCode == HttpStatusCode.Unauthorized)
                .RetryAsync(1, (resp, retryAttempt) =>
                {
                    if (resp.Result.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        // typically this would be call to IDS for new token
                        token = giveGoodToken ? TokenFactory.GetGoodToken() : "AnotherBadToken";
                    }
                });

            // in this case the circuit breaker is wrapping the retry so that we stop trying to get new tokens after a couple unauthorized failures in a row
            var unauthorizedResiliencyPolicy = Policy
                .WrapAsync(circuitBreaker401Policy, retry401Policy);

            // refresh token like normal
            var response =
                await unauthorizedResiliencyPolicy.ExecuteAsync(() =>
                    DemoHelper.DemoClient.GetAsync($"api/demo/unauthorized?token={token}"));
            Assert.IsTrue(response.IsSuccessStatusCode);

            // simulate still getting 401 (ex. not in whitelist for api, using wrong scope, authority, etc)
            token = "MyBadToken";
            giveGoodToken = false;

            response = await unauthorizedResiliencyPolicy.ExecuteAsync(() =>
                DemoHelper.DemoClient.GetAsync($"api/demo/unauthorized?token={token}"));

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.AreEqual(CircuitState.Open, circuitBreaker401Policy.CircuitState);
        }

        // Non-Http

        [TestCleanup]
        public void Cleanup()
        {
            DemoHelper.Reset();
        }
    }
}
