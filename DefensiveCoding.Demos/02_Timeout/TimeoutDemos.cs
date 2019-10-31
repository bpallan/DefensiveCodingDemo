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
        [TestMethod]
        public async Task BasicTimeout()
        {
            var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(1), TimeoutStrategy.Optimistic); // optimistic is default
            bool isTimeoutException = false;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            try
            {
                await timeoutPolicy.ExecuteAsync(ct => DemoHelper.DemoClient.GetAsync("api/demo/slow?failures=1", ct), CancellationToken.None);
            }
            catch (TimeoutRejectedException)
            {
                isTimeoutException = true;
            }

            Assert.IsTrue(isTimeoutException);
            Assert.IsTrue(sw.ElapsedMilliseconds < 2000);
        }

        [TestMethod]
        public async Task OptimisticTimeout_FailsWithoutCancellationToken()
        {
            var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(1), TimeoutStrategy.Optimistic); // optimistic is default

            Stopwatch sw = new Stopwatch();
            sw.Start();
            string result = await timeoutPolicy.ExecuteAsync(() => DemoHelper.DemoClient.GetStringAsync("api/demo/slow?failures=1"));            

            // note that we waited the entire 10 seconds to get here
            Assert.AreEqual("Slow!", result);
            Assert.IsTrue(sw.ElapsedMilliseconds > 2000);
        }

        [TestMethod]
        public void OptimisticTimeout_FailsForSynchronousCode()
        {
            var timeoutPolicy = Policy.Timeout(TimeSpan.FromSeconds(1), TimeoutStrategy.Optimistic); // optimistic is default

            Stopwatch sw = new Stopwatch();
            sw.Start();

            string result = timeoutPolicy.Execute(() => DemoHelper.DemoClient.GetStringAsync("api/demo/slow?failures=1").Result);

            // note that we waited the entire 10 seconds to get here
            Assert.AreEqual("Slow!", result);
            Assert.IsTrue(sw.ElapsedMilliseconds > 2000);
        }

        [TestMethod]
        public void PessimisticTimeout_WorksForSynchronousCode()
        {
            var timeoutPolicy = Policy.Timeout(TimeSpan.FromSeconds(1), TimeoutStrategy.Pessimistic); // optimistic is default
            bool isTimeoutException = false;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            try
            {
                string result = timeoutPolicy.Execute(() =>
                    DemoHelper.DemoClient.GetStringAsync("api/demo/slow?failures=1").Result);
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
