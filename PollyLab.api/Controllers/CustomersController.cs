using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using PollyLab.Api.Factories;
using PollyLab.Api.MockResponses;
using PollyLab.Helpers.Contracts;

namespace PollyLab.Api.Controllers
{
    [Route("api/lab/customers")]
    public class CustomersController : ControllerBase
    {
        private static readonly ConcurrentDictionary<Guid, Customer> _savedCustomers = new ConcurrentDictionary<Guid, Customer>();

        [HttpGet]
        public ActionResult<List<Customer>> Get()
        {
            return Ok(_savedCustomers.Select(x => x.Value).ToList());
        }

        [HttpPost]
        public HttpResponseMessage Post([FromBody] Customer customerToSave)
        {
            var response = MockResponseFactory.Create();

            while (!response.ShouldApply())
            {
                response = MockResponseFactory.Create();
            }

            var responseMessage = response.Execute();

            if (responseMessage.IsSuccessStatusCode)
            {
                _savedCustomers.TryAdd(customerToSave.CustomerId, customerToSave);
            }

            return responseMessage;
        }
    }
}
