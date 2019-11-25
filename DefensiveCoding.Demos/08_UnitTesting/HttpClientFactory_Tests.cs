using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest;
using DefensiveCoding.Demos._08_UnitTesting.MockHttpHandlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DefensiveCoding.Demos._08_UnitTesting
{
    [TestClass]
    public class HttpClientFactory_Tests
    {
        /// <summary>
        /// Use a mock HttpClientFactory and Mock HttpMessageHandler to unit test a class that accepts a HttpClientFactory
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task HttpClientFactory_TestCodeSeperateWithoutPolicies()
        {
            // setup
            var mockClientFactory = new Mock<IHttpClientFactory>();
            mockClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(
                    new HttpClient(new CustomerSuccessMockHandler())
                    {
                        BaseAddress = new Uri("http://localhost")
                    });
            var classUnderTest = new CustomerService_ClientFactory(mockClientFactory.Object);

            // act
            var customer = await classUnderTest.GetCustomerByIdAsync(1);

            // assert
            Assert.AreEqual(1, customer.CustomerId);
            Assert.AreEqual("Test", customer.FirstName);
            Assert.AreEqual("Tester", customer.LastName);
            Assert.AreEqual("test@test.com", customer.Email);
        }
    }
}
