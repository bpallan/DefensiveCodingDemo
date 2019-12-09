using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PollyLab.Api.Enums;

namespace PollyLab.Api.MockResponses
{
    internal class HealthyResponse : IMockResponse
    {
        public bool ShouldApply()
        {
            return true;
        }

        public ApiStates Execute()
        {
            return ApiStates.Healthy;
        }
    }
}
