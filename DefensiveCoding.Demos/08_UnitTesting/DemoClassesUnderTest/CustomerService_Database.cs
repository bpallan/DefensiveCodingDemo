using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest.Interfaces;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest.Models;
using Polly;

namespace DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest
{
    internal class CustomerService_Database
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IAsyncPolicy<CustomerModel> _resiliencyPolicy;

        public CustomerService_Database(ICustomerRepository customerRepository, IAsyncPolicy<CustomerModel> resiliencyPolicy)
        {
            _customerRepository = customerRepository;
            _resiliencyPolicy = resiliencyPolicy;
        }

        public async Task<CustomerModel> GetCustomerByIdAsync(int customerId)
        {
            return await _resiliencyPolicy.ExecuteAsync(() => _customerRepository.QueryCustomerById(customerId));
        }
    }
}
