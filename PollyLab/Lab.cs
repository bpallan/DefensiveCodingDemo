using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Polly;
using PollyLab.Helpers.Contracts;
using PollyLab.Helpers.Factories;

namespace PollyLab
{
    /// <summary>
    /// The goal is to send 100 valid customers to the api in less than 1 minute.
    /// The api is omnipotent so sending the same customer multiple times in the case of a timeout won't create duplicates
    /// Microsoft.Extensions.Http.Polly has already been installed (required to use Context below)
    /// YOU ARE ONLY ALLOWED TO MODIFY THIS CLASS     
    /// </summary>
    public static class Lab
    {
        public static void AddResiliency(this IHttpClientBuilder builder, Queue<Customer> customerQueue)
        {
            /// LAB WORK GOES HERE ///
            // stub in fallback since syntax is difficult (don't forget to add policy to builder)
            var fallback = Policy
                .HandleResult<HttpResponseMessage>(resp => !resp.IsSuccessStatusCode) // catch any bad responses, transient or not         
                .Or<Exception>() // handle ANY exception we get back
                .FallbackAsync(fallbackAction: (result, context, ct) =>
                {
                    // fallback logic goes here


                    // return a default response
                    return Task.FromResult(result.Result ?? new HttpResponseMessage() { StatusCode = HttpStatusCode.ServiceUnavailable }); // result is null on exception, so need to return something.
                }, onFallbackAsync: (exception, context) => Task.CompletedTask);
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

        [TestMethod]
        public async Task ExecuteLab()
        {
            var serviceProvider = _services.BuildServiceProvider();
            var clientFactory = serviceProvider.GetService<IHttpClientFactory>();
            var client = clientFactory.CreateClient("CustomerService");

            // setup timers/cancellation token
            var outOfTimeToken = new CancellationTokenSource(60000);
            var sw = new Stopwatch();
            sw.Start();

            // send to api
            while (_customerQueue.Count > 0 && !outOfTimeToken.IsCancellationRequested)
            {
                var customer = _customerQueue.Dequeue();
                var context = new Polly.Context();
                context["Customer"] = customer;
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "api/lab/customers");
                request.SetPolicyExecutionContext(context);
                request.Content = new StringContent(JsonConvert.SerializeObject(customer), Encoding.UTF8,
                    "application/json");

                try
                {
                    var response = await client.SendAsync(request, outOfTimeToken.Token);
                    await Console.Out.WriteLineAsync($"{sw.ElapsedMilliseconds:N}ms - {response.StatusCode}");
                }
                catch (Exception)
                {
                    break; // abort test on exception
                }
            }

            sw.Stop();

            // verify all customers received
            var verifyClient = clientFactory.CreateClient("VerificationService");
            var verifyResponse = await verifyClient.GetAsync("api/lab/customers");
            var json = await verifyResponse.Content.ReadAsStringAsync();
            var savedCustomerList = JsonConvert.DeserializeObject<List<Customer>>(json);
            Assert.AreEqual(0, _customerQueue.Count);
            Assert.AreEqual(100, savedCustomerList.Select(x => x.CustomerId).Distinct().Count());
            Assert.IsTrue(sw.ElapsedMilliseconds < 60000);
        }

        public VerifyLab()
        {
            _services.AddHttpClient("CustomerService",
                    client => { client.BaseAddress = new Uri("http://localhost:5002/"); })
                .AddResiliency(_customerQueue);

            _services.AddHttpClient("VerificationService",
                client =>
                {
                    client.BaseAddress = new Uri("http://localhost:5002/");
                }); // ensure bad policies/etc don't stop our ability to verify
        }
    }
}
