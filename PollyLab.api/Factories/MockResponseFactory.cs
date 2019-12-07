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
        private static int _currentResponseIndex = 0;
        private static object _lock;

        static MockResponseFactory()
        {
            _responses.Add(new HealthyResponse());
        }

        public static IMockResponse Create()
        {
            // demo doesn't run in parralel but just in case
            int localIndex;

            lock (_lock)
            {
                if (_currentResponseIndex >= _responses.Count)
                {
                    _currentResponseIndex = 0;
                }
                else
                {
                    _currentResponseIndex++;
                }

                localIndex = _currentResponseIndex;
            }

            return _responses[localIndex];
        }
    }
}
