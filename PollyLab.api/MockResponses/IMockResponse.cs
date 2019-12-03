using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PollyLab.Helpers.Contracts;

namespace PollyLab.Api.MockResponses
{
    internal interface IMockResponse
    {
        bool ShouldApply();
        HttpResponseMessage Execute();
    }
}
