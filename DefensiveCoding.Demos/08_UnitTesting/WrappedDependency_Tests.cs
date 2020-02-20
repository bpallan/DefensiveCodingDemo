using System.Diagnostics;
using System.Threading.Tasks;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest.Interfaces;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest.Models;
using DefensiveCoding.Demos.Factories;
using DefensiveCoding.Demos.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace DefensiveCoding.Demos._08_UnitTesting
{
    /// <summary>
    /// Demo testing policies that wrap code directly (instead of using HttpClientFactory)
    /// Examples might be database or cache calls, wcf service calls, or even http calls if working in legacy .net framework code
    /// </summary>
    [TestClass]
    public class WrappedDependency_Tests
    {
        /// <summary>
        /// Demo testing the original code w/out any policies interfering with the results
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Demo testing the policies w/out worry about what code is being wrapped
        /// Note: we are only verifying timeout and fallback in this demo.  All possible responses should be mocked/handled
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task WrappedDependency_TestPoliciesSeperateFromCode()
        {
            // setup
            var resiliencyPolicy = DemoPolicyFactory.GetCustomerDatabaseResiliencyPolicy();

            // act
            var sw = new Stopwatch();
            sw.Start();

            // simulate a slow call (should timeout and return default)
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

        /// <summary>
        /// Demo testing both the policies and code together.  
        /// Likely the most valueable since they confirm the policies work as expected and the code handles the results of them as it should.
        /// Only testing timeout and fallback.  All possible responses should be handled.  
        /// </summary>
        /// <returns></returns>
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
        
        [TestCleanup]
        public void Cleanup()
        {
            DemoHelper.Reset();
        }
    }
}
