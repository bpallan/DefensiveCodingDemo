using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
        /// <summary>
        /// Demonstrate adding a full range of policies for Http Calls
        /// Also demonstrating that a wrapped policy can be wrapped inside other policies
        /// Fallback -> Outer Timeout -> Retry -> Circuit Breaker -> Inner Timeout -> Client Call
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task AddResiliencyToHttpCall()
        {
            ///////////////////////// OUTER TIMEOUT POLICY /////////////////////////
            // for sync code, you should replace this by setting the timeout on the HttpClient
            IAsyncPolicy<HttpResponseMessage> outerTimeoutPolicy = Policy
                .TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10), TimeoutStrategy.Optimistic,                     
                    onTimeoutAsync: PolicyLoggingHelper.LogTimeoutAsync);

            ///////////////////////// RETRY POLICY /////////////////////////
            // we are retrying on inner timeout, this might be a bad idea for many scenerios
            // you never want to retry on a circuit breaker exception
            IAsyncPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>() 
                .WaitAndRetryAsync(1, retryAttempt => TimeSpan.FromMilliseconds(500),                     
                    onRetryAsync: PolicyLoggingHelper.LogWaitAndRetryAsync);

            ///////////////////////// CIRCUIT BREAKER POLICY /////////////////////////
            // typical duration of break should be 30-60 seconds.  Using 5 for this demo to make demo run faster
            IAsyncPolicy<HttpResponseMessage> circuitBreakerAsyncPolicy = Policy
                .HandleResult<HttpResponseMessage>(resp => !resp.IsSuccessStatusCode) // consider if you want to ignore 404s
                .Or<Exception>()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(5), 
                    onBreak: PolicyLoggingHelper.LogCircuitBroken,
                    onReset: PolicyLoggingHelper.LogCircuitReset);

            // pointer to the circuit breaker so we can read/update its state as needed (haven't figured out a better way to do this)
            // this is EXTREMELY useful for resetting between unit tests (expose a reset method to allow callers to reset your circuit breaker)
            ICircuitBreakerPolicy<HttpResponseMessage> circuitBreaker = (ICircuitBreakerPolicy<HttpResponseMessage>)circuitBreakerAsyncPolicy;

            ///////////////////////// INNER TIMOUT POLICY (Optional) /////////////////////////
            // inner timeout is totally optional and depends on use case.  
            // The outer timeout is the most important for requests with callers waiting
            // Sometimes you might want an inner timeout slightly lower than the outer just so the circuit breaker will break on too many timeouts
            IAsyncPolicy<HttpResponseMessage> innerTimeoutPolicy = Policy
                .TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(3), TimeoutStrategy.Pessimistic, onTimeoutAsync: PolicyLoggingHelper.LogTimeoutAsync);  // had to set to pessimistic to get this to work, todo: figure out why

            ///////////////////////// POLICY WRAP /////////////////////////
            // wrap policies from outer to inner
            var commonResiliencyPolicy = Policy
                .WrapAsync<HttpResponseMessage>(outerTimeoutPolicy, retryPolicy, circuitBreakerAsyncPolicy, innerTimeoutPolicy);
            
            ///////////////////////// FALLBACK POLICY (Wraps wrapped policy) /////////////////////////
            // alternative would be to wrap ExecuteAsync in try/catch and return default from there
            // I am wrapping the above resiliency policy to show that you can wrap a wrapped policy inside another policy
            // Fallback is a good use case for this since it can vary a lot between different calls
            IAsyncPolicy<HttpResponseMessage> resiliencyPolicyWithFallback = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .Or<BrokenCircuitException>()
                .FallbackAsync(new HttpResponseMessage
                {
                    // fetch default values or execute whatever behavior you want to fall back to
                    Content = new StringContent("Default!")
                }, onFallbackAsync: PolicyLoggingHelper.LogFallbackAsync)
                .WrapAsync(commonResiliencyPolicy);

            // verify retry on error
            var response = await resiliencyPolicyWithFallback.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/error?failures=1"));
            Assert.IsTrue(response.IsSuccessStatusCode);

            // verify retry on client timeout           
            response = await resiliencyPolicyWithFallback.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/slow?failures=1", CancellationToken.None));
            Assert.IsTrue(response.IsSuccessStatusCode);

            // verify default if every call times out
            DemoHelper.Reset();
            response = await resiliencyPolicyWithFallback.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/slow", CancellationToken.None)); 
            var result = await response.Content.ReadAsStringAsync();
            Assert.AreEqual("Default!", result);

            // verify circuit broken on errors    
            for (int i = 0; i < 6; i++)
            {
                response = await resiliencyPolicyWithFallback.ExecuteAsync(() =>
                    DemoHelper.DemoClient.GetAsync("api/demo/error", CancellationToken.None));
                result = await response.Content.ReadAsStringAsync();
                Assert.AreEqual("Default!", result);
            }

            Assert.AreEqual(CircuitState.Open, circuitBreaker.CircuitState);

            // let circuit breaker reset (goes to 1/2 open after 5 seconds)
            await Task.Delay(5100);
            Assert.AreEqual(CircuitState.HalfOpen, circuitBreaker.CircuitState);

            // verify circuit breaker is back open
            await commonResiliencyPolicy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/success"));
            Assert.AreEqual(CircuitState.Closed, circuitBreaker.CircuitState);
        }        

        /// <summary>
        ///Demonstrate using retry and circuit breaker policies to get a new token on an unauthorized (401) response
        /// Circuit breaker should always be used so we don't beat up the auth server if we have mis-matched credentials       
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task RefreshTokenOnUnauthorized()
        {
            string token = "MyBadToken";
            bool giveGoodToken = true;

            // setting the the threshold very low because a failure means we got a new token but still got a 401
            // typically means auth environment or scope wrong... or client not in the white list for calling api
            var circuitBreaker401Policy = Policy
                .HandleResult<HttpResponseMessage>(resp => resp.StatusCode == HttpStatusCode.Unauthorized)
                .CircuitBreakerAsync(1, TimeSpan.FromSeconds(30),
                    onBreak: PolicyLoggingHelper.LogCircuitBroken,
                    onReset: PolicyLoggingHelper.LogCircuitReset); 

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

            // refresh token successfully
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

        /// <summary>
        /// Demonstrate applying multiple policies for non-http calls
        /// WCF, Database, Cache, another others
        /// Main difference is you have to be more exception based then response based
        /// You also want to avoid retrying database or cache calls as that can make the problem worse
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task AddResiliencyToNonHttpCall()
        {
            var fallBackPolicy = Policy<string>
                .Handle<Exception>()
                .FallbackAsync<string>("Default!", onFallbackAsync: PolicyLoggingHelper.LogFallbackAsync);

            var timeoutPolicy = Policy
                .TimeoutAsync<string>(TimeSpan.FromSeconds(5), TimeoutStrategy.Pessimistic);

            // retry works well for WCF calls, should mostly likely be avoided for database or cache
            var retryPolicy = Policy<string>
                .Handle<Exception>()
                .RetryAsync<string>(1, onRetryAsync: PolicyLoggingHelper.LogRetryAsync);

            var circuitBreakerPolicy = Policy<string>
                .Handle<Exception>()
                .CircuitBreakerAsync<string>(5, TimeSpan.FromSeconds(30));

            var resiliencyPolicy = Policy
                .WrapAsync<string>(fallBackPolicy, timeoutPolicy, retryPolicy, circuitBreakerPolicy);

            // verify retry
            string result = await resiliencyPolicy.ExecuteAsync(UnreliableCall);
            Assert.AreEqual("Success!", result);

            // verify timeout
            result = await resiliencyPolicy.ExecuteAsync(SlowCall);
            Assert.AreEqual("Default!", result);

            // verify circuit breaker
            for (int i = 0; i < 5; i++)
            {
                result = await resiliencyPolicy.ExecuteAsync(BrokenCall);
            }

            Assert.AreEqual(CircuitState.Open, circuitBreakerPolicy.CircuitState);
            Assert.AreEqual("Default!", result);
        }        

        ///////////////////////// HELPER FUNCTIONS FOR NON-HTTP DEMOS /////////////////////////
        private async Task<string> SlowCall()
        {
            await Task.Delay(10000);
            return "Success!";
        }

        private int _counter = 0;
        private async Task<string> UnreliableCall()
        {
            _counter++;

            if (_counter % 2 != 0)
            {
                throw new Exception("Something Exploded!!!");
            }

            return await Task.FromResult("Success!");
        }

        private Task<string> BrokenCall()
        {
            throw new Exception("This service/database/etc is dead!!!");
        }

        [TestCleanup]
        public void Cleanup()
        {                        
            DemoHelper.Reset();
        }
    }
}
