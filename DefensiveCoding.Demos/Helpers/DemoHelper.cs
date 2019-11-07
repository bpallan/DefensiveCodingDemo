using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace DefensiveCoding.Demos.Helpers
{
    internal class DemoHelper
    {
        public static string DemoBaseUrl = "http://localhost:5001/";

        public static HttpClient DemoClient = new HttpClient()
        {
            BaseAddress = new Uri(DemoBaseUrl)
        };

        public static void Reset()
        {
            DemoClient.PostAsync("api/demo/reset", null).Wait();
        }
    }
}
