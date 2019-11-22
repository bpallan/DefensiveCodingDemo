using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using DefensiveCoding.Demos.Factories;
using DefensiveCoding.Demos._06_HttpClientFactory;
using Microsoft.Extensions.DependencyInjection;
using Polly.CircuitBreaker;

namespace DefensiveCoding.Demos.Extensions
{
    internal static class HttpClientBuilderExtensions
    {
        // return a reference to the circuit breaker so callers can maninuplate it as needed for testing
        public static IHttpClientBuilder AddResiliencyPolicies(this IHttpClientBuilder builder, out ICircuitBreakerPolicy<HttpResponseMessage> circuitBreaker)
        {
            var circuitBreakerPolicy = DemoPolicyFactory.GetCircuitBreakerPolicy();
            circuitBreaker = (ICircuitBreakerPolicy<HttpResponseMessage>)circuitBreakerPolicy;

            builder
                .AddPolicyHandler(DemoPolicyFactory.GetFallbackPolicy())
                .AddPolicyHandler(DemoPolicyFactory.GetRetryPolicy())
                .AddPolicyHandler(circuitBreakerPolicy)
                .AddPolicyHandler(DemoPolicyFactory.GetInnerTimeoutPolicy());

            return builder;
        }
    }
}
