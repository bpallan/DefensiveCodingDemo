﻿using System.Threading.Tasks;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest.Interfaces;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest.Models;
using Polly;

namespace DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest
{
    /// <summary>
    /// Demo class that simulates wrapping a repository call with a resiliency policy
    /// Note: This class is demoware.  A real example would likely have a lot more logic and be more valuable to test than this is.
    /// </summary>
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
