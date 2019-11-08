using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DefensiveCoding.Demos.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using Polly.CircuitBreaker;

namespace DefensiveCoding.Demos._04_CircuitBreaker
{
    [TestClass]
    public class CircuitBreakerDemos
    {
        /// <summary>
        /// Demostrate basic circuit breaker functionality
        /// After 2 consecutive failures, the circuit will be broken and additional requests will fail fast (not execute)
        /// Circuit will be re-evaluated after 30 seconds
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task BasicCircuitBreaker_HandleAllExceptions()
        {
            var policy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(2, TimeSpan.FromSeconds(30));
            bool circuitIsBroken = false;
            int attemptCount = 0;

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    await policy.ExecuteAsync(async () =>
                    {
                        var response = await DemoHelper.DemoClient.GetAsync("api/demo/error");
                        attemptCount++;
                        response.EnsureSuccessStatusCode(); // required to throw exception that will get caught
                    });
                }
                catch (Exception e)
                {
                    if (policy.CircuitState == CircuitState.Open)
                    {
                        circuitIsBroken = true;
                    }
                }               
            }

            // the circuit is broken after the 2nd attempt so the 3rd attempt won't be made
            Assert.AreEqual(2, attemptCount);
            Assert.IsTrue(circuitIsBroken);
        }

        /// <summary>
        /// Demonstates the full life cycle of a circuit breaker
        /// 1. Starting state is Closed which means it will execute requests
        /// 2. After 2 concurrent failures, its state will transition to Open which means it will fail fast
        /// 3. After 5 seconds, the state will transition to 1/2 open which means 1 request will be executed
        /// 4. If the next request fails, then it will go back to Open for another 5 seconds
        /// 5. If the next request succeeds, then it will go back to Closed and start executing all requests
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task BasicCircuitBreaker_LifeCycle()
        {
            var policy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(2, TimeSpan.FromSeconds(5));

            // break circuit
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    await policy.ExecuteAsync(() => DemoHelper.DemoClient.GetStringAsync("api/demo/error"));
                }
                catch
                {
                    // ignored
                }
            }

            // circuit is broken (open state)
            Assert.AreEqual(CircuitState.Open, policy.CircuitState);


            // circuit breaker will transition to 1/2 open state after 5 seconds
            await Task.Delay(5000);            
            Assert.AreEqual(CircuitState.HalfOpen, policy.CircuitState);            

            // if first request fails while 1/2 open, it will open back up for the 5 seconds
            try
            {
                await policy.ExecuteAsync(() => DemoHelper.DemoClient.GetStringAsync("api/demo/error"));
            }
            catch 
            {
                // ignored
            }

            Assert.AreEqual(CircuitState.Open, policy.CircuitState);

            // wait another 5 seconds to get back to 1/2 open
            await Task.Delay(5000);
            Assert.AreEqual(CircuitState.HalfOpen, policy.CircuitState);

            // make a successful request
            await policy.ExecuteAsync(() => DemoHelper.DemoClient.GetStringAsync("api/demo/success"));

            // circuit is now closed (accepting requests)
            Assert.AreEqual(CircuitState.Closed, policy.CircuitState);
        }

        /// <summary>
        /// Demonstates the advanced circuit breaker which allows you greater control over the circuit behavior
        /// If 50% of requests fail over a 5 second interval, with a minimum of 5 requests for that interval, then break for 30 seconds
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task AdvancedCircuitBreaker_HandleAllExceptions()
        {
            var policy = Policy
                .Handle<Exception>()
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: 0.5, // 50% failure rate
                    samplingDuration: TimeSpan.FromSeconds(5), // resets every 5 seconds
                    minimumThroughput: 5, // must have at least 5 requests in 5 seconds to qualify
                    durationOfBreak: TimeSpan.FromSeconds(30) // how long to break
                );

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    // fail every other time
                    if (i % 2 == 0)
                    {
                        await policy.ExecuteAsync(() => DemoHelper.DemoClient.GetStringAsync("api/demo/success"));
                    }
                    else
                    {
                        await policy.ExecuteAsync(() => DemoHelper.DemoClient.GetStringAsync("api/demo/error"));
                    }
                    
                }
                catch
                {
                    // ignore
                }
            }

            Assert.AreEqual(CircuitState.Open, policy.CircuitState);
        }

        [TestCleanup]
        public void Cleanup()
        {
            DemoHelper.Reset();
        }
    }
}
