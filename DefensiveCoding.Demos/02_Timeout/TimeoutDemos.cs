using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DefensiveCoding.Demos.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using Polly.Timeout;

namespace DefensiveCoding.Demos._02_Timeout
{
    [TestClass]
    public class TimeoutDemos
    {
        /// <summary>
        /// Demonstate basic timeout behavior when call takes longer than the configured timeout of 1 second
        /// Optimistic timeout will only work for async code that accepts a cancellation token
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task BasicTimeout()
        {
            var timeoutPolicy = Policy
                .TimeoutAsync(TimeSpan.FromSeconds(1), TimeoutStrategy.Optimistic, onTimeoutAsync: PolicyLoggingHelper.LogTimeoutAsync); // optimistic is default
            bool isTimeoutException = false;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            try
            {
                await timeoutPolicy.ExecuteAsync(ct => DemoHelper.DemoClient.GetAsync("api/demo/slow", ct), CancellationToken.None);
            }
            catch (TimeoutRejectedException)
            {
                isTimeoutException = true;
            }

            Assert.IsTrue(isTimeoutException);
            Assert.IsTrue(sw.ElapsedMilliseconds < 2000);
        }

        /// <summary>
        /// Demonstrates that optimistic timeout will not work if not combined with cancellation token
        /// The request will run to completion and return the result after the full 10 seconds has elapsed
        /// </summary>
        /// <returns></returns>
        [TestMethod]        
        public async Task OptimisticTimeout_FailsWithoutCancellationToken()
        {
            var timeoutPolicy = Policy
                .TimeoutAsync(TimeSpan.FromSeconds(1), TimeoutStrategy.Optimistic, onTimeoutAsync: PolicyLoggingHelper.LogTimeoutAsync); // optimistic is default

            Stopwatch sw = new Stopwatch();
            sw.Start();
            string result = await timeoutPolicy.ExecuteAsync(() => DemoHelper.DemoClient.GetStringAsync("api/demo/slow"));            

            // note that we waited the entire 10 seconds to get here
            Assert.AreEqual("Slow!", result);
            Assert.IsTrue(sw.ElapsedMilliseconds > 2000);
        }

        /// <summary>
        /// Demonstrates that like the above code, sync code which obviously doesn't support a cancellation token fails
        /// The request will run to completion and return the result after the full 10 seconds has elapsed
        /// </summary>
        [TestMethod]
        public void OptimisticTimeout_FailsForSynchronousCode()
        {
            var timeoutPolicy = Policy
                .Timeout(TimeSpan.FromSeconds(1), TimeoutStrategy.Optimistic, onTimeout: PolicyLoggingHelper.LogTimeout); // optimistic is default

            Stopwatch sw = new Stopwatch();
            sw.Start();

            string result = timeoutPolicy.Execute(() => DemoHelper.DemoClient.GetStringAsync("api/demo/slow").Result);

            // note that we waited the entire 10 seconds to get here
            Assert.AreEqual("Slow!", result);
            Assert.IsTrue(sw.ElapsedMilliseconds > 2000);
        }

        /// <summary>
        /// Demonstates that if you use a Pessimistic timeout, control will return to the caller after the specified timeout
        /// Note: Since it has no way of killing the thread, the abandoned thread will run into the background until it times out or faults
        /// I would reccomend combing this with a HttpClient timeout to avoid leaving the background thread open too long
        /// </summary>
        [TestMethod]
        public void PessimisticTimeout_WorksForSynchronousCode()
        {
            var timeoutPolicy = Policy
                .Timeout(TimeSpan.FromSeconds(1), TimeoutStrategy.Pessimistic, onTimeout: PolicyLoggingHelper.LogTimeout); 
            bool isTimeoutException = false;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            try
            {
                timeoutPolicy.Execute(() =>
                    DemoHelper.DemoClient.GetStringAsync("api/demo/slow").Result);
            }
            catch (TimeoutRejectedException)
            {
                isTimeoutException = true;
            }

            // note that we waited the entire 10 seconds to get here
            Assert.IsTrue(isTimeoutException);
            Assert.IsTrue(sw.ElapsedMilliseconds < 2000);
        }

        [TestCleanup]
        public void Cleanup()
        {
            DemoHelper.Reset();
        }
    }
}
