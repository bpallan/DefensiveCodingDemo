using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DefensiveCoding.Demos.Helpers;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace DefensiveCoding.Demos.Factories
{
    /// <summary>
    /// demoware, should make these much more configurable
    /// </summary>
    internal static class DemoPolicyFactory
    {
        public static IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy()
        {
            return Policy
                .HandleResult<HttpResponseMessage>(resp => !resp.IsSuccessStatusCode) // catch any bad responses, transient or not         
                .Or<Exception>() // handle ANY exception we get back
                .FallbackAsync(FallbackAction, PolicyLoggingHelper.LogFallbackAsync);
        }

        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>() // handle timeouts as failures so they get retried, decide if this is appropriate for your use case
                .RetryAsync(1, onRetryAsync: PolicyLoggingHelper.LogRetryAsync);
        }

        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
        }

        public static IAsyncPolicy<HttpResponseMessage> GetInnerTimeoutPolicy()
        {
            return Policy
                .TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(1), TimeoutStrategy.Optimistic); // optimistic will work well with http client factory
        }

        // demonstrate returning a mock response
        private static Task<HttpResponseMessage> FallbackAction(DelegateResult<HttpResponseMessage> responseToFailedRequest, Context context, CancellationToken cancellationToken)
        {
            // mocking a successful response
            // you can pass in responseToFailedRequest.Result.StatusCode if you want to preserve the original error response code
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Default!")
            };
            return Task.FromResult(httpResponseMessage);
        }
    }
}
