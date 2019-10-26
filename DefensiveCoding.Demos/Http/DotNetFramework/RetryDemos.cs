using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using DefensiveCoding.Api;
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
        }

        [TestMethod]
        public void WhenRequestOutsideRetry_ThrowsDuplicateRequestException()
        {

        }
    }
}
