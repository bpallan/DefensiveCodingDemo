using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest.Models;
using Newtonsoft.Json;

namespace DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest
{
    internal class CustomerService_ClientFactory
    {
        private readonly IHttpClientFactory _clientFactory;

        public CustomerService_ClientFactory(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<CustomerModel> GetCustomerByIdAsync(int customerId)
        {
            var client = _clientFactory.CreateClient("CustomerService");
            var response = await client.GetAsync($"api/customers/id/{customerId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<CustomerModel>(json);
        }
    }
}
