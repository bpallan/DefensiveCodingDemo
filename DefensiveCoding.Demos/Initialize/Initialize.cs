using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DefensiveCoding.Api;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DefensiveCoding.Demos.Initialize
{
    [TestClass]
    public class Initialize
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            var webHost = WebHost.CreateDefaultBuilder(null)
                .UseKestrel()
                .UseUrls("http://localhost:5001")
                .UseStartup<Startup>()
                .Build();
            webHost.Start();              
        }
    }
}
