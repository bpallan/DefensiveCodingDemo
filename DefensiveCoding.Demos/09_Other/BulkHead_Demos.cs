using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using Polly.Bulkhead;

namespace DefensiveCoding.Demos._09_Other
{
    [TestClass]
    public class BulkHead_Demos
    {
        private List<int> _testData = new List<int>();
        private ConcurrentBag<int> _processed = new ConcurrentBag<int>();
        private ConcurrentBag<int> _rejected = new ConcurrentBag<int>();

        public BulkHead_Demos()
        {
            for (int i = 0; i < 100; i++)
            {
                _testData.Add(i);
            }
        }
        
        /// <summary>
        /// Demonstrates maxing out the bulkhead so that it rejects executions
        /// Doesn't check bulkhead status, just catches BulkHeadRejectedException
        /// You can check available execution and queue slots by checking (see in watch window):
        ///     bulkHead.BulkheadAvailableCount
        ///     bulkHead.QueueAvailableCount
        /// </summary>
        [TestMethod]
        public void BulkHeadRejectsExecutionWhenQueueFull()
        {
            _processed.Clear();
            _rejected.Clear();
            bool exceptionThrown = false;
            var bulkHead = Policy.Bulkhead(1, 5, context =>
            {
                var id = context["id"];
                Console.Out.WriteLine($"Rejected id {id}");
                _rejected.Add((int)id);
            }); // max concurrency of 2, max queue size of 5

            Parallel.ForEach(_testData, id =>
            {
                try
                {
                    var context = new Polly.Context {["id"] = id};
                    bulkHead.Execute((ctx) => SlowFunction(id), context);
                }
                catch (BulkheadRejectedException)
                {
                    // keep demo running
                    exceptionThrown = true;
                }
            });
            
            Assert.IsTrue(exceptionThrown);
            Assert.IsTrue(_processed.Count > 0);
            Assert.IsTrue(_rejected.Count > 0);
            Assert.IsTrue(_rejected.Count > _processed.Count); // we will always reject more since method takes 1 second
        }

        private void SlowFunction(int id)
        {
            Task.Delay(1000).Wait();
            Console.Out.WriteLine($"Received id {id}");
            _processed.Add(id);
        }
    }
}