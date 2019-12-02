using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using PollyLab.Helpers.Contracts;
using PollyLab.Helpers.Factories;

namespace PollyLab.Before.Tests
{
    /// <summary>
    /// The goal is to send 100 valid customers to the api in less than 1 minute.
    /// YOU ARE ONLY ALLOWED TO MODIFY THIS CLASS 
    /// </summary>
    public static class Lab
    {
        public static void AddResiliency(this IHttpClientBuilder builder, Queue<Customer> customerQueue)
        {
            /// LAB WORK GOES HERE ///
            
        }
    }

    /// <summary>
    /// The goal is to send 100 valid customers to the api in less than 1 minute
    /// YOU ARE NOT ALLOWED TO MODIFY THIS TEST CLASS IN ANY WAY.  
    /// ALL CHANGES MUST BE APPLIED IN THE ABOVE LAB EXTENSION METHOD.
    /// </summary>
    [TestClass]
    public class VerifyLab
    {
        private readonly IServiceCollection _services = new ServiceCollection();
        private readonly Queue<Customer> _customerQueue = CustomerFactory.CreateCustomerQueue(100);

        public VerifyLab()
        {
            _services.AddHttpClient("CustomerService",
                client => { client.BaseAddress = new Uri("http://localhost:5002/"); })
                .AddResiliency(_customerQueue);

            _services.AddHttpClient("VerificationService",
                client => { client.BaseAddress = new Uri("http://localhost:5002/"); }); // ensure bad policies/etc don't stop our ability to verify
        }
        
        [TestMethod]
        public async Task ExecuteLab()
        {
            var serviceProvider = _services.BuildServiceProvider();
            var clientFactory = serviceProvider.GetService<IHttpClientFactory>();
            var client = clientFactory.CreateClient("CustomerService");

            // send to api
            var sw = new Stopwatch();
            sw.Start();
            while (_customerQueue.Count > 0)
            {
                var customer = _customerQueue.Dequeue();
                var content = new StringContent(JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");
                await client.PostAsync("api/lab/customers", content);
            }

            sw.Stop();

            // verify all customers received
            var verifyClient = clientFactory.CreateClient("VerificationService");
            var response = await verifyClient.GetAsync("api/lab/customers");
            var json = await response.Content.ReadAsStringAsync();
            var customerList = JsonConvert.DeserializeObject<List<Customer>>(json);
            Assert.AreEqual(0, _customerQueue.Count);
            Assert.AreEqual(100, customerList.Select(x => x.CustomerId).Distinct().Count());
            Assert.IsTrue(sw.ElapsedMilliseconds < 60000);
        }
    }
}
