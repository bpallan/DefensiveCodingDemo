using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using PollyLab.Helpers.Contracts;

namespace PollyLab.Api.Controllers
{
    [Route("api/lab/customers")]
    public class CustomersController : ControllerBase
    {
        private static readonly ConcurrentBag<Customer> _savedCustomers = new ConcurrentBag<Customer>();

        [HttpGet]
        public ActionResult<List<Customer>> Get()
        {
            return Ok(_savedCustomers);
        }

        [HttpPost]
        public ActionResult Post([FromBody] Customer customerToSave)
        {
            _savedCustomers.Add(customerToSave);
            return Ok();
        }
    }
}
