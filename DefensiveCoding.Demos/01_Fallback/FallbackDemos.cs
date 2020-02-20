using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        /// Demonstrates a very basic fallback in which we just return a default string when a http exception is thrown
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
                .Fallback("Default!");

            var result = fallBackPolicy.Execute(() => QueryData(1));
            
            Assert.AreEqual("Default!", result);
        }

        /// <summary>
        /// You can chain together multiple exception types to handle
        /// </summary>
        [TestMethod]
        public void HandleMultipleExceptionTypes()
        {
            // check for multiple exception types
            var fallBackPolicy = Policy<string>
                .Handle<TestException>()
                .Or<ArgumentException>()
                .Fallback("Default!");

            var result = fallBackPolicy.Execute(() => QueryData(0));
            var result2 = fallBackPolicy.Execute(() => QueryData(1));

            Assert.AreEqual("Default!", result);
            Assert.AreEqual("Default!", result2);
        }

        /// <summary>
        /// Demonstrates a basic fallback that is looking at the Response Status Code instead of trying to catch exceptions
        /// Fallback delegates demonstrate logging the fallback and returning a mock response
        /// Other behaviors might be to throw a custom exception, read from cache, etc
        /// This demo also shows how to use context to store a value used in the handler
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task HandleHttpErrorResponses()
        {
            // create policy
            var fallBackPolicy = Policy
                .HandleResult<HttpResponseMessage>(resp => !resp.IsSuccessStatusCode)
                .FallbackAsync(FallbackAction, PolicyLoggingHelper.LogFallbackAsync);

            // setup context
            var context = new Polly.Context();
            context["DefaultValue"] = "Default2!";

            // execute code wrapped in policy
            var response = await fallBackPolicy.ExecuteAsync((ctx) => DemoHelper.DemoClient.GetAsync("api/demo/error"), context);
            var content = await response.Content.ReadAsStringAsync();

            Assert.AreEqual("Default2!", content);
        }

        /// <summary>
        /// Demonstrate handling multiple results in 1 policy using separate handle statements
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task HandleMultipleResponses()
        {
            // check for multiple response types (could also just use or conditional and single handle)
            var fallBackPolicy = Policy
                .HandleResult<HttpResponseMessage>(resp => resp.StatusCode == HttpStatusCode.InternalServerError)
                .OrResult(resp => resp.StatusCode == HttpStatusCode.Unauthorized)
                .FallbackAsync(FallbackAction, PolicyLoggingHelper.LogFallbackAsync);

            var response1 = await fallBackPolicy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/error"));
            var content1 = await response1.Content.ReadAsStringAsync();

            var response2 = await fallBackPolicy.ExecuteAsync(() => DemoHelper.DemoClient.GetAsync("api/demo/unauthorized"));
            var content2 = await response2.Content.ReadAsStringAsync();

            Assert.AreEqual("Default!", content1);
            Assert.AreEqual("Default!", content2);
        }

        /// <summary>
        /// Demonstrates that you can catch inner and aggregate exceptions using HandleInner
        /// In the case of aggregate, will check all inner exceptions for a match
        /// Will also match against outer exception
        /// </summary>
        [TestMethod]
        public void HandleInnerAndAggregateExceptions()
        {
            var fallBackPolicy = Policy<string>
                .HandleInner<NotSupportedException>()
                .Fallback("Fallback!");

            var result = fallBackPolicy.Execute(ThrowAggregateException);

            Assert.AreEqual("Fallback!", result);
        }

        // demonstrate returning a mock response
        private Task<HttpResponseMessage> FallbackAction(DelegateResult<HttpResponseMessage> responseToFailedRequest, Context context, CancellationToken cancellationToken)
        {
            var defaultValue = context.ContainsKey("DefaultValue") ? (string) context["DefaultValue"] : "Default!";
            // mocking a successful response
            // you can pass in responseToFailedRequest.Result.StatusCode if you want to preserve the original error response code
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)        
            {
                Content = new StringContent(defaultValue)
            };
            return Task.FromResult(httpResponseMessage);
        }

        private string QueryData(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("id is required", nameof(id));
            }
            
            throw new TestException()
            {
                Number = 1205
            };
        }

        private string ThrowAggregateException()
        {
            throw new AggregateException(new List<Exception>()
            {
                new NotImplementedException(),
                new NotSupportedException()
            });
        }
        
        private class TestException : Exception
        {
            public int Number { get; set; }
        }
        
        [TestCleanup]
        public void Cleanup()
        {
            DemoHelper.Reset();
        }
    }
}
