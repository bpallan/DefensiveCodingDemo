﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;

namespace DefensiveCoding.Demos._01_Fallback
{
    [TestClass]
    public class FallbackDemos
    {
        private static HttpClient _client = new HttpClient()
        {
            BaseAddress = new Uri("http://localhost:5001/")
        };

        [TestMethod]
        public async Task HandleAnyException()
        {
            var fallBackPolicy = Policy<string>
                .Handle<Exception>()
                .FallbackAsync<string>("Default!");

            var result = await fallBackPolicy.ExecuteAsync(() => _client.GetStringAsync("api/demo/error?failures=1"));

            Assert.AreEqual("Default!", result);
        }

        [TestMethod]
        public async Task HandleHttpErrorResponses()
        {
            var fallBackPolicy = Policy
                .HandleResult<HttpResponseMessage>(response => !response.IsSuccessStatusCode)
                .FallbackAsync(FallbackAction, OnFallbackAsync);

            var result = await fallBackPolicy.ExecuteAsync(() => _client.GetAsync("api/demo/error?failures=1"));
            var content = await result.Content.ReadAsStringAsync();

            Assert.AreEqual("Default!", content);
        }

        private Task OnFallbackAsync(DelegateResult<HttpResponseMessage> response, Context context)
        {
            Console.WriteLine("Log that fallback was hit");
            return Task.CompletedTask;
        }

        private Task<HttpResponseMessage> FallbackAction(DelegateResult<HttpResponseMessage> responseToFailedRequest, Context context, CancellationToken cancellationToken)
        {            
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(responseToFailedRequest.Result.StatusCode)
            {
                Content = new StringContent("Default!")
            };
            return Task.FromResult(httpResponseMessage);
        }

        [TestCleanup]
        public void Cleanup()
        {
            ResetDemo().Wait();
        }

        private static async Task ResetDemo()
        {            
            await _client.PostAsync("api/demo/reset", null);
        }
    }
}
