using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;

namespace DefensiveCoding.Demos._09_Other
{
    [TestClass]
    public class BulkHead_Demos
    {
        [TestMethod]
        public void BulkHead_Demo()
        {
            var bulkHead = Policy.Bulkhead(1, 5, context =>
            {
                Console.Out.WriteLine("Bulkhead is full. Execution rejected.");
            }); // max concurrency of 1, max queue size of 5
            var testData = new List<int>();

            for (int i = 0; i < 60; i++)
            {
                testData.Add(i);
            }

            Parallel.ForEach(testData, currentData =>
            {
                bulkHead.Execute(() => SlowFunction(currentData));
            });
        }

        private void SlowFunction(int data)
        {
            Task.Delay(1000).Wait();
            Console.Out.WriteLine($"Received id {data}");
        }
    }
}