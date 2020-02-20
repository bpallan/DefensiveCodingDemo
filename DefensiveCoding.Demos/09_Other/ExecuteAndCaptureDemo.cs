using DefensiveCoding.Demos.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DefensiveCoding.Demos._09_Other
{
    /// <summary>
    /// Execute and capture returns additional information about the policy being executed
    /// Includes result, exception (if failure), and the policy context
    /// </summary>
    [TestClass]
    public class ExecuteAndCaptureDemo
    {
        [TestMethod]
        public async Task ExecuteAndCaptureSuccess()
        {
            var policy = Policy
                .Handle<HttpRequestException>()
                .RetryAsync(1);

            var result = await policy.ExecuteAndCaptureAsync(async () =>
            {
                var response = await DemoHelper.DemoClient.GetAsync("api/demo/error?failures=1");
                response.EnsureSuccessStatusCode(); 
                return response;
            });

            // result is wrapped in a PolicyResult class
            Assert.IsTrue(result.Result.IsSuccessStatusCode);
        }

        /// <summary>
        /// Demonstrate that when an exception is thrown inside the execute, it won't bubble out to the caller
        /// Instead your policy result will have it in the FinalException field and result will be NULL
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ExecuteAndCaptureFailure()
        {
            var policy = Policy
                .Handle<HttpRequestException>()
                .RetryAsync(1);

            var result = await policy.ExecuteAndCaptureAsync(async () =>
            {
                var response = await DemoHelper.DemoClient.GetAsync("api/demo/error");
                response.EnsureSuccessStatusCode(); 
                return response;
            });

            // result is wrapped in a PolicyResult class, it will be null since an uncaught exception was thrown
            Assert.IsNull(result.Result);
            Assert.IsNotNull(result.FinalException);
        }

        [TestCleanup]
        public void Cleanup()
        {
            DemoHelper.Reset();
        }
    }
}
