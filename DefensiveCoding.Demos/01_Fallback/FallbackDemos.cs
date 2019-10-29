using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;

namespace DefensiveCoding.Demos._01_Fallback
{
    [TestClass]
    public class FallbackDemos
    {
        [TestMethod]
        public void HandleAnyException_Explicit()
        {
            var fallBackPolicy = Policy<string>
                .Handle<Exception>()
                .FallbackAsync<string>("Default!");


        }
    }
}
