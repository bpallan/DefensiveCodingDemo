using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PollyLab.Api.MockResponses;

namespace PollyLab.Api.Factories
{
    internal static class MockResponseFactory
    {
        private static readonly List<IMockResponse> _responses = new List<IMockResponse>();
        private static int _currentResponseIndex = -1;
        private static object _lock = new object();

        static MockResponseFactory()
        {
            _responses.Add(new HealthyResponse());
            _responses.Add(new TransientErrorResponse());
            _responses.Add(new HealthyResponse());
            _responses.Add(new ServiceUnavailableResponse());
        }

        public static IMockResponse Create()
        {
            // wrapping in a lock to prevent concurrency issues
            // this is NOT performant but fine for this demo        
            lock (_lock)
            {
                _currentResponseIndex++;

                if (_currentResponseIndex >= _responses.Count)
                {
                    _currentResponseIndex = 0;
                }

                var response = _responses[_currentResponseIndex];

                if (response is ServiceUnavailableResponse)
                {
                    if (response.ShouldApply())
                    {
                        _currentResponseIndex--; // keep returning this until they stop calling service for a while
                    }
                    else
                    {
                        _responses.Remove(response);
                    }
                }

                return response;
            }
        }
    }
}
