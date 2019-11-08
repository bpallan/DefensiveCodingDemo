using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DefensiveCoding.Api.Helpers;
using DefensiveCoding.Demos.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using Polly.Extensions.Http;

namespace DefensiveCoding.Demos._03_Retry
{
    [TestClass]
    public class RetryDemos
    {
        /// <summary>
        /// Demonstate retrying on any Http exception
        /// Note:  Typically you only want to retry on transient faults, so this is not the desired approach
        /// Either check the result status code or use Http Polly Extensions (shown below) to limit retries to transient faults
        /// API call returns a 500 Internal Server Error on first call
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SimpleRetryOnHttpException()
        {
            var policy = Policy
                .Handle<HttpRequestException>()
                .RetryAsync(1);

            var result = await policy.ExecuteAsync(async() =>
            {
                var response = await DemoHelper.DemoClient.GetAsync("api/demo/error?failures=1");
                response.EnsureSuccessStatusCode(); // exception won't be thrown if this isn't called
                return response;
            });

            Assert.IsTrue(result.IsSuccessStatusCode);
        }

        /// <summary>
        /// Demonstrates using Http Polly Extensions to only retry on transient faults
        /// This is the preferred approach
        /// API call returns a 500 Internal Server Error on first call
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task RetryOnTransientFaultsOnly()
        {
            var policy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .RetryAsync(1);

            // no need to call EnsureSuccessStatusCode
            var response = await policy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/error?failures=1"));

            Assert.IsTrue(response.IsSuccessStatusCode);
        }
        
        /// <summary>
        /// Demonstrates checking the result yourself if you don't want to use the above extensions
        /// In this case we are just handling a 401 which isn't a transient fault but can have special behavior (token refresh)      
        /// A more typical approach would be to catch 500 errors and request timeouts (408)
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task RetryOnASpecificResult()
        {
            string token = "MyExpiredToken";

            // demo only, this should ALWAYS be combined with a circuit breaker to avoid beating up the auth server
            var refreshTokenPolicy = Policy
                .HandleResult<HttpResponseMessage>(resp => resp.StatusCode == HttpStatusCode.Unauthorized) 
                .RetryAsync(1, (resp, retryAttempt) =>
                {
                    if (resp.Result.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        token = TokenFactory.GetGoodToken();
                    }
                });

            // passing token in querystring for demo, would typically put in header
            var response = await refreshTokenPolicy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync($"api/demo/unauthorized?token={token}"));

            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        /// <summary>
        /// Demonstrates a basic wait and retry in which there is a delay between retries
        /// Typically when a caller is waiting for a response, you want this to be much shorter than 5 seconds demonstrated below
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task WaitAndRetry()
        {
            var policy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(1, retryAttempt => TimeSpan.FromSeconds(5));

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var response = await policy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/error?failures=1"));

            Assert.IsTrue(sw.ElapsedMilliseconds > 4900); // verify there was a delay before retry
            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        /// <summary>
        /// Demonstates an incremental backoff with a jitter
        /// A jitter prevents a high volume application from retrying a bunch of failed requests at the exact same time
        /// This can be extremely useful for async processing in which you have time to wait
        /// When a client is waiting for a response, the time between retires needs to be minimal
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task WaitAndRetryWithBackoff_WithJitter()
        {
            Random jitterer = new Random();
            var policy = HttpPolicyExtensions
                .HandleTransientHttpError()
                // 1 seconds, 2 seconds, 4 seconds, etc... + 0 to 500 ms jitter
                .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)/2) + TimeSpan.FromMilliseconds(jitterer.Next(0, 500)));

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var response = await policy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/error?failures=2"));

            Assert.IsTrue(sw.ElapsedMilliseconds > 2900); // verify there was a delay before retry
            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        /// <summary>
        /// Demonstates that if you build a HttpRequestMessage outside of the retry policy, then retries will blow up
        /// You don't have to worry about this if using delegating handlers         
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task HttpRequest_WhenBuiltOutsideOfExecute_ThrowsInvalidOperationException()
        {
            var policy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .RetryAsync(1);
            bool isInvalidOperationException = false;

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{DemoHelper.DemoBaseUrl}api/demo/error?failures=1"));

            try
            {
                await policy.ExecuteAsync(() => DemoHelper.DemoClient.SendAsync(request));
            }
            catch (InvalidOperationException ex)
            {
                isInvalidOperationException = true;
            }            

            Assert.IsTrue(isInvalidOperationException);
        }

        /// <summary>
        ///  Demonstates that a timeout happening on your HttpClient isn't considered a transient fault by Polly
        /// See below for how to handle        
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ClientTimeout_IsNotTransientError()
        {
            var policy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .RetryAsync(1);

            HttpClient client = new HttpClient()
            {
                BaseAddress = new Uri(DemoHelper.DemoBaseUrl),
                Timeout = TimeSpan.FromSeconds(1)
            };

            bool isTimeoutException = false;

            try
            {
                await policy.ExecuteAsync(() => client.GetAsync("api/demo/slow?failures=1"));
            }
            catch (TaskCanceledException ex)
            {
                isTimeoutException = true;
            }                        

            Assert.IsTrue(isTimeoutException);
        }

        /// <summary>
        /// Demonstates that you can add an "Or" to handle additional exceptions or results
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ClientTimeout_Handled()
        {
            var policy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TaskCanceledException>()
                .RetryAsync(1);

            HttpClient client = new HttpClient()
            {
                BaseAddress = new Uri(DemoHelper.DemoBaseUrl),
                Timeout = TimeSpan.FromSeconds(1)
            };

            var response = await policy.ExecuteAsync(() => client.GetAsync("api/demo/slow?failures=1"));

            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        /// <summary>
        /// Demonstrates that unlike a client timeout, a request timeout on the server (408) is considered a transient fault by Polly
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ServerTimeout_IsTransientError()
        {
            var policy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .RetryAsync(1);

            var response = await policy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/timeout?failures=1"));

            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        [TestCleanup]
        public void Cleanup()
        {
            DemoHelper.Reset();
        }
    }
}
