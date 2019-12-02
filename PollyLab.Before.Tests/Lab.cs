using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PollyLab.Helpers.Factories;

namespace PollyLab.Before.Tests
{
    [TestClass]
    public class Lab
    {
        [TestMethod]
        public void ExecuteLab()
        {
            // create 100 customers
            var customerQueue = CustomerFactory.CreateCustomerQueue(100);

            // send to api
            while (customerQueue.Count > 0)
            {
                var customer = customerQueue.Dequeue();
            }

            // verify all customers received
            Assert.AreEqual(0, customerQueue.Count);
        }
    }
}
