using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest.Interfaces;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace DefensiveCoding.Demos._08_UnitTesting
{
    [TestClass]
    public class WrappedDependency_Tests
    {
        [TestMethod]
        public async Task WrappedDependency_TestCodeSeperateWithoutPolicies()
        {
            // setup
            var testCustomer = new CustomerModel()
            {
                CustomerId = 1,
                Email = "test@test.com",
                FirstName = "Test",
                LastName = "Tester"
            };
            var mockRepository = new Mock<ICustomerRepository>();
            mockRepository.Setup(x => x.QueryCustomerById(It.IsAny<int>())).Returns(Task.FromResult(testCustomer));
            IAsyncPolicy mockPolicy = Policy.NoOpAsync();
            var classUnderTest = new CustomerService_Database(mockRepository.Object, mockPolicy);

            // act
            var customer = await classUnderTest.GetCustomerByIdAsync(1);

            // assert
            Assert.AreEqual(testCustomer.CustomerId, customer.CustomerId);
            Assert.AreEqual(testCustomer.FirstName, customer.FirstName);
            Assert.AreEqual(testCustomer.LastName, customer.LastName);
            Assert.AreEqual(testCustomer.Email, customer.Email);
        }
    }
}
