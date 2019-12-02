using System.Net.Http;
using System.Threading.Tasks;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest.Models;
using Newtonsoft.Json;

namespace DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest
{
    /// <summary>
    /// Demo proxy class into another service.  Used to show how to unit test classes that dependen on HttpClientFactory
    /// Note:  This class is demowhare and doesn't virtually nothing.  Unit testing isn't very valuable for this but pretend it does a bunch of complex stuff.
    /// </summary>
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
