using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DefensiveCoding.Demos.Extensions;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest.Models;
using DefensiveCoding.Demos._08_UnitTesting.MockHttpHandlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

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

        [TestMethod]
        public async Task HttpClientFactory_TestPoliciesSeperateFromCode()
        {
            // setup
            IServiceCollection services = new ServiceCollection();
            services
                .AddHttpClient("CustomerService")
                .AddResiliencyPolicies(out var circuitBreaker) // comment out this line of code to verify mock handler is working
                .AddHttpMessageHandler(() => new CustomerResiliencyTestHandler(1)); 
            var serviceProvider = services.BuildServiceProvider();
            var clientFactory = serviceProvider.GetService<IHttpClientFactory>();
            var client = clientFactory.CreateClient("CustomerService");

            // act
            var response = await client.GetAsync($"http://localhost/api/customers/id/1"); // it really doesn't matter what you pass here since mock behavior isn't check the request
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var customer = JsonConvert.DeserializeObject<CustomerModel>(json);

            // assert
            Assert.AreEqual(1, customer.CustomerId);
            Assert.AreEqual("Test", customer.FirstName);
            Assert.AreEqual("Tester", customer.LastName);
            Assert.AreEqual("test@test.com", customer.Email);
        }

        [TestMethod]
        public async Task HttpClientFactory_TestPoliciesWithCode()
        {
            // setup
            IServiceCollection services = new ServiceCollection();
            services.AddHttpClient("CustomerService", client => client.BaseAddress = new Uri("http://localhost/"))
                .AddResiliencyPolicies(out var circuitBreaker)
                .AddHttpMessageHandler(() => new CustomerResiliencyTestHandler(1));
            services.AddTransient<CustomerService_ClientFactory>();
            var serviceProvider = services.BuildServiceProvider();
            var classUnderTest = serviceProvider.GetService<CustomerService_ClientFactory>();

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
