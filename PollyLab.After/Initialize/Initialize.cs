using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PollyLab.Api;

namespace PollyLab.After.Initialize
{
    [TestClass]
    public class Initialize
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            var webHost = WebHost.CreateDefaultBuilder(null)
                .UseKestrel()
                .UseUrls("http://localhost:5002")
                .UseStartup<Startup>()
                .Build();
            webHost.Start();
        }
    }
}
