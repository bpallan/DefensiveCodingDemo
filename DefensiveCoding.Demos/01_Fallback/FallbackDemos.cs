using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DefensiveCoding.Demos.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;

namespace DefensiveCoding.Demos._01_Fallback
{
    [TestClass]
    public class FallbackDemos
    {
        /// <summary>
        /// Demonstrates a very basic fallback in which we just return a default string when any exception is thrown
        /// Note:  GetStringAsync exceptions on an error response.  Methods that return a response object do not exception so you must check the result or call EnsureSuccessStatusCode
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task HandleAnyException()
        {
            var fallBackPolicy = Policy<string>
                .Handle<Exception>()
                .FallbackAsync<string>("Default!");

            var result = await fallBackPolicy.ExecuteAsync(() => DemoHelper.DemoClient.GetStringAsync("api/demo/error"));

            Assert.AreEqual("Default!", result);
        }

        /// <summary>
        /// Demonstrates a basic fallback that is looking at the Response Status Code instead of trying to catch exceptions
        /// Fallback delegates demonstrate logging the fallback and returning a mock response
        /// Other behaviors might be to throw a custom exception, read from cache, etc
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task HandleHttpErrorResponses()
        {
            var fallBackPolicy = Policy
                .HandleResult<HttpResponseMessage>(resp => !resp.IsSuccessStatusCode)
                .FallbackAsync(FallbackAction, OnFallbackAsync);

            var response = await fallBackPolicy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/error"));
            var content = await response.Content.ReadAsStringAsync();

            Assert.AreEqual("Default!", content);
        }

        // demonstrate logging when a fallback is fired
        private Task OnFallbackAsync(DelegateResult<HttpResponseMessage> response, Context context)
        {
            Console.WriteLine("Log that fallback was hit");
            return Task.CompletedTask;
        }

        // demonstrate returning a mock response
        private Task<HttpResponseMessage> FallbackAction(DelegateResult<HttpResponseMessage> responseToFailedRequest, Context context, CancellationToken cancellationToken)
        {            
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(responseToFailedRequest.Result.StatusCode)
            {
                Content = new StringContent("Default!")
            };
            return Task.FromResult(httpResponseMessage);
        }        
    }
}
