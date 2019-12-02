using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest.Models;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // required so that Moq can generate mock
namespace DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest.Interfaces
{
    internal interface ICustomerRepository
    {
        Task<CustomerModel> QueryCustomerById(int id);
    }
}
