using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DefensiveCoding.Api.Helpers
{
    public static class TokenFactory
    {
        public static string GetGoodToken()
        {
            return "good_token";
        }

        public static string GetBadToken()
        {
            return "bad_token";
        }
    }
}
