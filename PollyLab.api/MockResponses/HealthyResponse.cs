using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PollyLab.Api.MockResponses
{
    internal class HealthyResponse : IMockResponse
    {
        public bool ShouldApply()
        {
            return true;
        }

        public bool RemoveFromQueue()
        {
            return false;
        }

        public HttpResponseMessage Execute()
        {
            return new HttpResponseMessage(HttpStatusCode.Created);
        }
    }
}
