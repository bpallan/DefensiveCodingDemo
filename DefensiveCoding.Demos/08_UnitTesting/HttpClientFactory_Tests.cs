using System;
using System.Net.Http;
using System.Threading.Tasks;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest.Models;
using DefensiveCoding.Demos._08_UnitTesting.MockHttpHandlers;
using DefensiveCoding.Demos.Extensions;
using DefensiveCoding.Demos.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Polly.CircuitBreaker;

namespace DefensiveCoding.Demos._08_UnitTesting
{
    /// <summary>
    /// Demo how to test policies that are applied to a HttpClientFactory implementation
    /// This is the best practice for .net core applications
    /// Note:  These demos are fully self contained and do not rely on any external dependencies
    /// Unlike the other demos, we are using mock handlers to simulate results instead of calling an actual api
    /// </summary>
    [TestClass]
    public class HttpClientFactory_Tests
    {
        /// <summary>
        /// Test code w/out applying any policies.  
        /// Use a mock HttpClientFactory and Mock HttpMessageHandler to unit test a class that accepts a HttpClientFactory.        
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

        /// <summary>
        /// Test policies w/out worrying about the code consuming them
        /// Add policies to HttpClientFactory via extension method.  Test client directly using mock handler to simulate faults.
        /// Note:  Only testing retry and fallback.  In a real example you should test all possible responses.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task HttpClientFactory_TestPoliciesSeperateFromCode()
        {
            // setup
            IServiceCollection services = new ServiceCollection();
            services
                .AddHttpClient("CustomerService")
                .AddResiliencyPolicies(out var circuitBreaker, JsonConvert.SerializeObject(GetDefaultCustomer())) // comment out this line of code to verify mock handler is working
                .AddHttpMessageHandler(() => new CustomerErrorMockHandler(1)); 
            var serviceProvider = services.BuildServiceProvider();
            var clientFactory = serviceProvider.GetService<IHttpClientFactory>();
            var client = clientFactory.CreateClient("CustomerService");

            // act
            var response = await client.GetAsync($"http://localhost/api/customers/id/1"); // it really doesn't matter what you pass here since mock handler doesn't check the request
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var customer = JsonConvert.DeserializeObject<CustomerModel>(json);

            // assert
            Assert.AreEqual(1, customer.CustomerId);
            Assert.AreEqual("Test", customer.FirstName);
            Assert.AreEqual("Tester", customer.LastName);
            Assert.AreEqual("test@test.com", customer.Email);
        }

        /// <summary>
        /// Test both the code and the policies being applied together
        /// Add policies to HttpClientFactory via extension method.  Add mock handler to simulate failures.  Pass client factory to class under test.
        /// Note:  Like above, I just verified the retry.  Then I decided to go ahead and test the circuit breaker and fallback for fun.
        /// If you share the collection or policies (static) across many tests, then you must reset the circuit breaker after this test is complete.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task HttpClientFactory_TestPoliciesWithCode()
        {
            // setup
            var mockHandler = new CustomerErrorMockHandler(1);
            IServiceCollection services = new ServiceCollection();
            services.AddHttpClient("CustomerService", client => client.BaseAddress = new Uri("http://localhost/"))
                .AddResiliencyPolicies(out var circuitBreaker, JsonConvert.SerializeObject(GetDefaultCustomer()))
                .AddHttpMessageHandler(() => mockHandler);
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

            // for fun let's break the circuit and verify fallbakc
            mockHandler.ResetFailures();

            for (int i = 0; i <= 5; i++)
            {
                customer = await classUnderTest.GetCustomerByIdAsync(1);
            }

            Assert.AreEqual(CircuitState.Open, circuitBreaker.CircuitState);
            Assert.AreEqual(GetDefaultCustomer().Message, customer.Message);

            // not actually required for this test since it is self contained.  Just showing you how to do it if you want to share across multiple tests.
            circuitBreaker.Reset();
        }        

        private static CustomerModel GetDefaultCustomer()
        {
            return new CustomerModel()
            {
                Message = "Customer Is Not Available."
            };
        }
        
        [TestCleanup]
        public void Cleanup()
        {
            DemoHelper.Reset();
        }
    }
}
