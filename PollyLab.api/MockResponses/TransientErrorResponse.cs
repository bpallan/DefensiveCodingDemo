using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using PollyLab.Api.Enums;

namespace PollyLab.Api.MockResponses
{
    internal class TransientErrorResponse : IMockResponse
    {
        public bool ShouldApply()
        {
            return true;
        }

        public ApiStates Execute()
        {
            return ApiStates.TransientError;
        }
    }
}
