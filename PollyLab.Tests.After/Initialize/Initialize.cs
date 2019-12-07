using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PollyLab.Api;

namespace PollyLab.Tests.After.Initialize
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
