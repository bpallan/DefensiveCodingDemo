using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using DefensiveCoding.Demos.Factories;
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
            IAsyncPolicy<CustomerModel> mockPolicy = Policy.NoOpAsync<CustomerModel>();
            var classUnderTest = new CustomerService_Database(mockRepository.Object, mockPolicy);

            // act
            var customer = await classUnderTest.GetCustomerByIdAsync(1);

            // assert
            Assert.AreEqual(testCustomer.CustomerId, customer.CustomerId);
            Assert.AreEqual(testCustomer.FirstName, customer.FirstName);
            Assert.AreEqual(testCustomer.LastName, customer.LastName);
            Assert.AreEqual(testCustomer.Email, customer.Email);
        }

        [TestMethod]
        public async Task WrappedDependency_TestPoliciesSeperateFromCode()
        {
            // setup
            var resiliencyPolicy = DemoPolicyFactory.GetCustomerDatabaseResiliencyPolicy();

            // act
            var sw = new Stopwatch();
            sw.Start();
            var customer = await resiliencyPolicy.ExecuteAsync(async () =>
            {
                await Task.Delay(10000);
                return new CustomerModel()
                {
                    CustomerId = 1,
                    FirstName = "Test",
                    LastName = "Tester",
                    Email = "test@test.com"
                };
            });

            // assert
            Assert.AreEqual("Customer Is Not Available.", customer.Message);
            Assert.IsTrue(sw.ElapsedMilliseconds < 10000);
        }

        [TestMethod]
        public async Task WrappedDependency_TestPoliciesWithCode()
        {
            // setup
            var resiliencyPolicy = DemoPolicyFactory.GetCustomerDatabaseResiliencyPolicy();
            var classUnderTest = new CustomerService_Database(new TestRepository(), resiliencyPolicy);

            // act
            var sw = new Stopwatch();
            sw.Start();
            var customer = await classUnderTest.GetCustomerByIdAsync(1);

            // assert
            Assert.AreEqual("Customer Is Not Available.", customer.Message);
            Assert.IsTrue(sw.ElapsedMilliseconds < 10000);
        }

        private class TestRepository : ICustomerRepository
        {
            public async Task<CustomerModel> QueryCustomerById(int id)
            {
                await Task.Delay(10000);
                return new CustomerModel()
                {
                    CustomerId = 1,
                    FirstName = "Test",
                    LastName = "Tester",
                    Email = "test@test.com"
                };
            }
        }
    }
}
