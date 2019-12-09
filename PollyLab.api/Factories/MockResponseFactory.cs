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
        }

        public static IMockResponse Create()
        {
            // demo doesn't run in parralel but lock just in case (this isn't optimized)
            int localIndex;

            lock (_lock)
            {
                _currentResponseIndex++;

                if (_currentResponseIndex >= _responses.Count)
                {
                    _currentResponseIndex = 0;
                }

                localIndex = _currentResponseIndex;
            }

            return _responses[localIndex];
        }
    }
}
