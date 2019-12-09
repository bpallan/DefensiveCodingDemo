using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace PollyLab.Api.MockResponses
{
    public class TransientErrorResponse : IMockResponse
    {
        public bool ShouldApply()
        {
            return true;
        }

        public HttpResponseMessage Execute()
        {
            return new HttpResponseMessage() { StatusCode = HttpStatusCode.InternalServerError};
        }
    }
}
