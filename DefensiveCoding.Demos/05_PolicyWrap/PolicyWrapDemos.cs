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
                }, 
                    // optional func or delegate to execute when fallback happens
                    onFallbackAsync: OnFallbackAsync);

            // for sync code, you should replace this by setting the timeout on the HttpClient
            IAsyncPolicy<HttpResponseMessage> outerTimeoutPolicy = Policy
                .TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10), TimeoutStrategy.Optimistic,                     
                    // optional func or delegate to execute when timeout occurs
                    onTimeoutAsync: OnTimeoutAsync);

            IAsyncPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()

                // retry on inner timeout (might be a bad idea for some scenerios, you decide if this fits your use case)
                //don't retry on circuit breaker exception
                .Or<TimeoutRejectedException>() 
                .WaitAndRetryAsync(1, retryAttempt => TimeSpan.FromMilliseconds(500),                     
                    // optional func or delegate to execute when retry occurs
                    onRetryAsync: OnRetryAsync);

            IAsyncPolicy<HttpResponseMessage> circuitBreakerAsyncPolicy = Policy
                .HandleResult<HttpResponseMessage>(resp => !resp.IsSuccessStatusCode) // consider if you want to ignore 404s
                .Or<Exception>()
                // shortening for demo, should be 30 or 60 seconds
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(5), 
                    onBreak: (exception, timespan, context) =>
                    {
                        Console.WriteLine("Circuit is open!");
                    },
                    onReset: (context) =>
                    {
                        Console.WriteLine("Circuit is closed!");
                    });

            // pointer to the circuit breaker so we can read/update its state as needed (haven't figured out a better way to do this)
            ICircuitBreakerPolicy<HttpResponseMessage> circuitBreaker = (ICircuitBreakerPolicy<HttpResponseMessage>)circuitBreakerAsyncPolicy;

            // inner timeout is totally optional and depends on use case.  The outer timeout is the most important for requests with callers waiting
            IAsyncPolicy<HttpResponseMessage> innerTimeoutPolicy = Policy
                .TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(3), TimeoutStrategy.Pessimistic, onTimeoutAsync: OnTimeoutAsync);  // had to set to pessimistic to get this to work, todo: figure out why

            // wrap policies from outer to inner
            var resiliencyPolicy = Policy
                .WrapAsync<HttpResponseMessage>(fallbackPolicy, outerTimeoutPolicy, retryPolicy, circuitBreakerAsyncPolicy, innerTimeoutPolicy);

            // verify retry on error
            var response = await resiliencyPolicy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/error?failures=1"));
            Assert.IsTrue(response.IsSuccessStatusCode);

            // verify retry on client timeout           
            response = await resiliencyPolicy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/slow?failures=1", CancellationToken.None));
            Assert.IsTrue(response.IsSuccessStatusCode);

            // verify default if every call times out
            DemoHelper.Reset();
            response = await resiliencyPolicy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/slow", CancellationToken.None)); 
            var result = await response.Content.ReadAsStringAsync();
            Assert.AreEqual("Default!", result);

            // verify circuit broken on errors    
            for (int i = 0; i < 6; i++)
            {
                response = await resiliencyPolicy.ExecuteAsync(() =>
                    DemoHelper.DemoClient.GetAsync("api/demo/error", CancellationToken.None));
                result = await response.Content.ReadAsStringAsync();
                Assert.AreEqual("Default!", result);
            }

            Assert.AreEqual(CircuitState.Open, circuitBreaker.CircuitState);

            // let circuit breaker reset (goes to 1/2 open after 5 seconds)
            await Task.Delay(5100);
            Assert.AreEqual(CircuitState.HalfOpen, circuitBreaker.CircuitState);

            // verify circuit breaker is back open
            await resiliencyPolicy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/success"));
            Assert.AreEqual(CircuitState.Closed, circuitBreaker.CircuitState);
        }

        private async Task OnRetryAsync(DelegateResult<HttpResponseMessage> exception, TimeSpan timeSpan, Context context)
        {
            // example only, log whatever details you deem relevant for trouble shooting or monitoring
            await Console.Out.WriteLineAsync($"Retrying request!  CorrelationId: {context?.CorrelationId}");
        }

        private async Task OnTimeoutAsync(Context context, TimeSpan timespan, Task task)
        {
            // example only, log whatever details you deem relevant for trouble shooting or monitoring
            await Console.Out.WriteLineAsync($"Timeout exceeded after {timespan.Seconds} seconds!  CorrelationId: {context?.CorrelationId}");
        }

        private async Task OnFallbackAsync(DelegateResult<HttpResponseMessage> exception, Context context)
        {
            // example only, log whatever details you deem relevant for trouble shooting or monitoring
            await Console.Out.WriteAsync($"Returning fallback data!  CorrelationId: {context?.CorrelationId}");
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
        [TestMethod]
        public async Task AddResiliencyToNonHttpCall()
        {
            var fallBackPolicy = Policy<string>
                .Handle<Exception>()
                .FallbackAsync<string>("Default!");

            var timeoutPolicy = Policy
                .TimeoutAsync<string>(TimeSpan.FromSeconds(5), TimeoutStrategy.Pessimistic);

            var retryPolicy = Policy<string>
                .Handle<Exception>()
                .RetryAsync<string>(1);

            var circuitBreakerPolicy = Policy<string>
                .Handle<Exception>()
                .CircuitBreakerAsync<string>(5, TimeSpan.FromSeconds(30));

            var resiliencyPolicy = Policy
                .WrapAsync<string>(fallBackPolicy, timeoutPolicy, retryPolicy, circuitBreakerPolicy);

            // verify retry
            string result = await resiliencyPolicy.ExecuteAsync(UnreliableDatabaseCall);
            Assert.AreEqual("Success!", result);

            // verify timeout
            result = await resiliencyPolicy.ExecuteAsync(SlowDatabaseCall);
            Assert.AreEqual("Default!", result);

            // verify circuit breaker
            for (int i = 0; i < 5; i++)
            {
                result = await resiliencyPolicy.ExecuteAsync(DatabaseIsDead);
            }

            Assert.AreEqual(CircuitState.Open, circuitBreakerPolicy.CircuitState);
            Assert.AreEqual("Default!", result);
        }

        private async Task<string> SlowDatabaseCall()
        {
            await Task.Delay(10000);
            return "Success!";
        }

        private int _counter = 0;
        private async Task<string> UnreliableDatabaseCall()
        {
            _counter++;

            if (_counter % 2 != 0)
            {
                throw new Exception("Something Exploded!!!");
            }

            return await Task.FromResult("Success!");
        }

        private Task<string> DatabaseIsDead()
        {
            throw new Exception("You broke the db!!!");
        }

        [TestCleanup]
        public void Cleanup()
        {                        
            DemoHelper.Reset();
        }
    }
}
