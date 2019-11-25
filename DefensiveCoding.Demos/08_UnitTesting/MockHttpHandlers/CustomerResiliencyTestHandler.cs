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
    internal class CustomerResiliencyTestHandler : DelegatingHandler
    {
        private readonly int _failures;
        private int _failureCount = 0;        

        public CustomerResiliencyTestHandler(int failures)  
        {
            _failures = failures;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_failureCount < _failures)
            {
                _failureCount++;
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.InternalServerError
                };
            }

            // todo: figure out how to re-use CustomerSuccessMockHandler (wouldn't let me use it w/ HttpClientBuilder)
            var mockCustomer = new CustomerModel
            {
                CustomerId = 1,
                Email = "test@test.com",
                FirstName = "Test",
                LastName = "Tester"
            };

            return await Task.FromResult(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(mockCustomer))
            });
        }
    }
}
