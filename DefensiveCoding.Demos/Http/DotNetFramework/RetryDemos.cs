using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DefensiveCoding.Api;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DefensiveCoding.Demos.Http.DotNetFramework
{
    [TestClass]
    public class RetryDemos
    {
        private TestServer _testServer;
        private HttpClient _httpClient;
        private Task _host;

        public RetryDemos()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                //.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            _testServer = new TestServer(
                new WebHostBuilder()
                    .UseEnvironment(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Local")
                    .UseConfiguration(builder.Build())
                    .UseStartup<Startup>());
            _httpClient = _testServer.CreateClient();

            var configuration =
                new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["urls"] = "http://localhost:5000"
                    })
                    .Build();

            var webHost = WebHost.CreateDefaultBuilder(null)
                .UseKestrel()                
                //.UseConfiguration(configuration)
                .UseUrls("http://localhost:5001")
                .UseStartup<Startup>()
                .Build();
            _host = webHost.StartAsync();
        }

        [TestMethod]
        public async Task WhenRequestOutsideRetry_ThrowsDuplicateRequestException()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync("http://localhost:5001/api/demo/success");
        }
    }
}
