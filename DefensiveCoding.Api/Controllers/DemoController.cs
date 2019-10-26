﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DefensiveCoding.Api.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace DefensiveCoding.Api.Controllers
{
    [Route("api/demo")]
    [ApiController]
    public class DemoController : ControllerBase
    {
        private static int _slowCount = 0;
        private static int _errorCount = 0;
        private static int _timeoutCount = 0;

        [HttpPost]
        [Route("reset")]
        public ActionResult Reset()
        {
            _slowCount = 0;
            _errorCount = 0;
            _timeoutCount = 0;

            return Ok();
        }

        [HttpGet]
        [Route("success")]
        public ActionResult<string> Success()
        {
            return "Success!";
        }       

        [HttpGet]
        [Route("slow")]
        public async Task<ActionResult<string>> Slow([FromQuery]int failures)
        {
            if (_slowCount < failures)
            {
                _slowCount++;
                await Task.Delay(10000);                
            }
            else
            {
                _slowCount = 0;
            }

            return "Slow!";
        }

        [HttpGet]
        [Route("unauthorized")]
        public ActionResult<string> UnAuthorized([FromQuery]string token)
        {
            if (token == TokenFactory.GetGoodToken())
            {
                return "Authorized!";
            }
            else
            {
                return Unauthorized();
            }
        }        

        [HttpGet]
        [Route("error")]
        public ActionResult<string> Error([FromQuery] int failures)
        {
            if (_errorCount < failures)
            {
                _errorCount++;
                return new StatusCodeResult(500);
            }
            else
            {
                _errorCount = 0;
            }

            return "Success!";
        }        

        [HttpGet]
        [Route("timeout")]
        public async Task<ActionResult<string>> Timeout([FromQuery] int failures)
        {
            if (_timeoutCount < failures)
            {
                _timeoutCount++;
                await Task.Delay(10000);
                return new StatusCodeResult(408);
            }
            else
            {
                _timeoutCount = 0;
            }

            return "Success!";
        }        
    }
}
