using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest.Models;
using Newtonsoft.Json;

namespace DefensiveCoding.Demos._08_UnitTesting.MockHttpHandlers
{
    internal class CustomerSuccessMockHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var mockCustomer = new CustomerModel
            {
                CustomerId = 1,
                Email = "test@test.com",
                FirstName = "Test",
                LastName = "Tester"
            };

            return Task.FromResult(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(mockCustomer))
            });
        }
    }
}
