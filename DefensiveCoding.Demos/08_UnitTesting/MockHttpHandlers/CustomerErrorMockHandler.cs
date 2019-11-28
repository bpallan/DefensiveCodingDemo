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
    /// <summary>
    /// Sample delegating handler for demo'ing mock responses
    /// A regular HttpMessageHandler would work just as well for this exact demo. 
    /// A real delegating handler would typically do something like:
    ///    1. perform logic against rhe request
    ///    2. call base.SendAsync
    ///    3. perform additional logic against the response
    ///    4. return to caller
    /// </summary>
    internal class CustomerErrorMockHandler : DelegatingHandler
    {
        private int _failures;
        private int _failureCount = 0;        

        public CustomerErrorMockHandler(int failures = 0)  
        {
            _failures = failures;
        }

        public void ResetFailures(int failures = 0)
        {
            _failures = failures;
            _failureCount = 0;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_failures == 0 || _failureCount < _failures)
            {
                _failureCount++;
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.InternalServerError
                };
            }

            // todo: figure out how to re-use CustomerSuccessMockHandler - was trying to use it as an InnerHandler but it wouldn't work
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
