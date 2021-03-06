﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest.Models;
using DefensiveCoding.Demos.Helpers;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace DefensiveCoding.Demos.Factories
{
    /// <summary>
    /// An example of how you might return policies using a factory
    /// By setting WithPolicyKey, you can reference the policy that was applied by name in your logging (context.PolicyKey)
    /// Note:  For demo, a real implementation would allow for policy settings to be tweaked and customer delegates to be defined.
    /// </summary>
    internal static class DemoPolicyFactory
    {
        public static IAsyncPolicy<HttpResponseMessage> GetHttpFallbackPolicy(string defaultValue)
        {
            return Policy
                .HandleResult<HttpResponseMessage>(resp => !resp.IsSuccessStatusCode) // catch any bad responses, transient or not         
                .Or<Exception>() // handle ANY exception we get back
                .FallbackAsync((result, context, ct) => FallbackAction(result, context, ct, defaultValue), PolicyLoggingHelper.LogFallbackAsync)
                .WithPolicyKey("MyFallbackPolicy");
        }

        public static IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>() // handle timeouts as failures so they get retried, decide if this is appropriate for your use case
                .RetryAsync(1, onRetryAsync: PolicyLoggingHelper.LogRetryAsync)
                .WithPolicyKey("MyRetryPolicy");
        }

        public static IAsyncPolicy<HttpResponseMessage> GetHttpCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30))
                .WithPolicyKey("MyCircuitBreakerPolicy");
        }

        public static IAsyncPolicy<HttpResponseMessage> GetHttpInnerTimeoutPolicy()
        {
            return Policy
                .TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(1), TimeoutStrategy.Optimistic) // optimistic will work well with http client factory
                .WithPolicyKey("MyInnerTimeoutPolicy");
        }

        // demonstrate returning a mock response
        private static Task<HttpResponseMessage> FallbackAction(DelegateResult<HttpResponseMessage> responseToFailedRequest, Context context, CancellationToken cancellationToken, string defaultValue)
        {
            // mocking a successful response
            // you can pass in responseToFailedRequest.Result.StatusCode if you want to preserve the original error response code
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(defaultValue)
            };
            return Task.FromResult(httpResponseMessage);
        }

        public static IAsyncPolicy<CustomerModel> GetCustomerDatabaseResiliencyPolicy()
        {
            var fallBackPolicy = Policy<CustomerModel>
                .Handle<Exception>()
                .FallbackAsync(new CustomerModel()
                {
                    Message = "Customer Is Not Available."
                }, onFallbackAsync: PolicyLoggingHelper.LogFallbackAsync);

            var timeoutPolicy = Policy
                .TimeoutAsync<CustomerModel>(TimeSpan.FromSeconds(5), TimeoutStrategy.Pessimistic);

            var circuitBreakerPolicy = Policy<CustomerModel>
                .Handle<Exception>()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

            var resiliencyPolicy = Policy
                .WrapAsync(fallBackPolicy, timeoutPolicy, circuitBreakerPolicy)
                .WithPolicyKey("MyResiliencyPolicy");

            return resiliencyPolicy;
        }
    }
}
