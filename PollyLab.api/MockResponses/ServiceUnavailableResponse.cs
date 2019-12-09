using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using PollyLab.Api.Enums;

namespace PollyLab.Api.MockResponses
{
    internal class ServiceUnavailableResponse : IMockResponse
    {
        private static DateTime? lastCallDate = null;

        public bool ShouldApply()
        {
            // if more than 30 seconds since last called, don't apply any more
            if (lastCallDate < DateTime.UtcNow.AddSeconds(-30))
            {
                return false;
            }

            lastCallDate = DateTime.UtcNow;
            return true;
        }

        public ApiStates Execute()
        {
            return ApiStates.Down;
        }
    }
}
