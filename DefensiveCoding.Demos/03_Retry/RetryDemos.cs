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
        [TestMethod]
        public async Task SimpleRetryOnHttpException()
        {
            // this will retry on any http exception which is not desired, either check for transient faults yourself or use Http Polly extensions in next example
            var policy = Policy.Handle<HttpRequestException>().RetryAsync(1);

            var result = await policy.ExecuteAsync(async() =>
            {
                var response = await DemoHelper.DemoClient.GetAsync("api/demo/error?failures=1");
                response.EnsureSuccessStatusCode(); // exception won't be thrown if this isn't called
                return response;
            });

            Assert.IsTrue(result.IsSuccessStatusCode);
        }

        [TestMethod]
        public async Task RetryOnTransientFaultsOnly()
        {
            var policy = HttpPolicyExtensions.HandleTransientHttpError().RetryAsync(1);

            // no need to call EnsureSuccessStatusCode
            var result = await policy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/error?failures=1"));

            Assert.IsTrue(result.IsSuccessStatusCode);
        }
        
        [TestMethod]
        public async Task RetryOnResult()
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

        [TestMethod]
        public async Task WaitAndRetry()
        {
            var policy = HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(1, retryAttempt => TimeSpan.FromSeconds(5));

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var result = await policy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/error?failures=1"));

            Assert.IsTrue(sw.ElapsedMilliseconds > 4900); // verify there was a delay before retry
            Assert.IsTrue(result.IsSuccessStatusCode);
        }

        [TestMethod]
        public async Task WaitAndRetryWithBackoff()
        {
            var policy = HttpPolicyExtensions
                .HandleTransientHttpError()
                // 1 seconds, 2 seconds, 4 seconds, etc...
                .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)/2));

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var result = await policy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/error?failures=2"));

            Assert.IsTrue(sw.ElapsedMilliseconds > 2900); // verify there was a delay before retry
            Assert.IsTrue(result.IsSuccessStatusCode);
        }

        [TestMethod]
        public async Task HttpRequest_WhenBuiltOutsideOfExecute_ThrowsInvalidOperationException()
        {
            var policy = HttpPolicyExtensions.HandleTransientHttpError().RetryAsync(1);
            bool isInvalidOperationException = false;

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{DemoHelper.DemoBaseUrl}/api/demo/error?failures=1"));

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

        public void ClientTimeout_IsNotHttpRequestException()
        {

        }

        public void ClientTimeout_Handled()
        {

        }

        [TestCleanup]
        public void Cleanup()
        {
            DemoHelper.Reset();
        }
    }
}
