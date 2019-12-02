using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PollyLab.Api.Enums
{
    internal enum ApiStates
    {
        // api is healthy, return a 200
        Healthly = 0,

        // token has expired, return a 401
        // keep returning a 401 until a new token is provided
        UnAuthorized = 1,

        // return a 500 (1 response only)        
        TransientError = 2,

        // wait for 30 seconds before responeding (1 response only)        
        Slow = 3,

        // keep returning errors until you go 30 seconds w/out a request
        Down = 4
    }
}
