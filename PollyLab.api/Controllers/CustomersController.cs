using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
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
            return Ok(_savedCustomers.ToList());
        }

        [HttpPost]
        public ActionResult Post([FromBody] Customer customerToSave)
        {
            _savedCustomers.TryAdd(customerToSave.CustomerId, customerToSave);
            return Ok();
        }
    }
}
