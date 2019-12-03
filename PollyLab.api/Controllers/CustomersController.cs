using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using PollyLab.Api.MockResponses;
using PollyLab.Helpers.Contracts;

namespace PollyLab.Api.Controllers
{
    [Route("api/lab/customers")]
    public class CustomersController : ControllerBase
    {
        private static readonly ConcurrentDictionary<Guid, Customer> _savedCustomers = new ConcurrentDictionary<Guid, Customer>();
        private static readonly List<IMockResponse> _responses = new List<IMockResponse>();
        private static int _currentResponseIndex = 0;

        public CustomersController()
        {
            _responses.Add(new HealthyResponse());            
        }

        [HttpGet]
        public ActionResult<List<Customer>> Get()
        {
            return Ok(_savedCustomers.Select(x => x.Value).ToList());
        }

        [HttpPost]
        public HttpResponseMessage Post([FromBody] Customer customerToSave)
        {
            if (_currentResponseIndex >= _responses.Count)
            {
                _currentResponseIndex = 0;
            }

            var response = _responses[_currentResponseIndex];

            while (!response.ShouldApply())
            {
                response = _responses[_currentResponseIndex++];
            }

            var responseMessage = response.Execute();

            if (responseMessage.IsSuccessStatusCode)
            {
                _savedCustomers.TryAdd(customerToSave.CustomerId, customerToSave);
            }

            _currentResponseIndex++;

            return responseMessage;
        }
    }
}
