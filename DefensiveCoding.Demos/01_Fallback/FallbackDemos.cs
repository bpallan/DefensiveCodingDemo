using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
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
        public async Task HandleSingleException()
        {            
            // create policy
            var fallBackPolicy = Policy<string>
                .Handle<HttpRequestException>()
                .FallbackAsync<string>((ctx, ct) => Task.FromResult((string) ctx["DefaultValue"]), onFallbackAsync: PolicyLoggingHelper.LogFallbackAsync);

            // create context
            // you can put ANY object in your context that you need for logging or performing any other actions in your policies
            // for demo I am setting the default value, but this could be an id, a full customer data model, etc.
            var context = new Polly.Context {["DefaultValue"] = "Default!"};

            // execute code wrapped in policy
            var result = await fallBackPolicy.ExecuteAsync((ctx) => DemoHelper.DemoClient.GetStringAsync("api/demo/error"), context);

            Assert.AreEqual("Default!", result);
        }
        
        /// <summary>
        /// Simple example of checking an error number and returning a default value.
        /// This is showing a sync code example and not logging (since example logger is async only)
        /// </summary>
        [TestMethod]
        public void HandleExceptionTypeWithCondition()
        {
            // check for a specific type of exception and error number
            var fallBackPolicy = Policy<string>
                .Handle<TestException>(ex => ex.Number == 1205)
                .Fallback<string>("Default!");

            var result = fallBackPolicy.Execute(QueryData);
            
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
            // crate policy
            var fallBackPolicy = Policy
                .HandleResult<HttpResponseMessage>(resp => !resp.IsSuccessStatusCode)
                .FallbackAsync(FallbackAction, PolicyLoggingHelper.LogFallbackAsync);

            // setup context
            var context = new Polly.Context();
            context["DefaultValue"] = "Default2!";

            // execute code wrappe din policy
            var response = await fallBackPolicy.ExecuteAsync((ctx) => DemoHelper.DemoClient.GetAsync("api/demo/error"), context);
            var content = await response.Content.ReadAsStringAsync();

            Assert.AreEqual("Default2!", content);
        }

        // demonstrate returning a mock response
        private Task<HttpResponseMessage> FallbackAction(DelegateResult<HttpResponseMessage> responseToFailedRequest, Context context, CancellationToken cancellationToken)
        {
            // mocking a successful response
            // you can pass in responseToFailedRequest.Result.StatusCode if you want to preserve the original error response code
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)        
            {
                Content = new StringContent((string)context["DefaultValue"])
            };
            return Task.FromResult(httpResponseMessage);
        }

        private string QueryData()
        {
            throw new TestException()
            {
                Number = 1205
            };
        }
        
        private class TestException : Exception
        {
            public int Number { get; set; }
        }
    }
}
