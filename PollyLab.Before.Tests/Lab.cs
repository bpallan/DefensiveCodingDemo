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
            var customerList = CustomerFactory.CreateCustomers(100).ToList();

            // send to api

            // verify all customers received
        }
    }
}
