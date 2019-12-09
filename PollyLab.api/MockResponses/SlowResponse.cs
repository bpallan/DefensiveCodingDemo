using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PollyLab.Api.Enums;

namespace PollyLab.Api.MockResponses
{
    internal class SlowResponse : IMockResponse
    {
        public bool ShouldApply()
        {
            return true;
        }

        public ApiStates Execute()
        {
            Thread.Sleep(10000);
            return ApiStates.Slow;
        }
    }
}
