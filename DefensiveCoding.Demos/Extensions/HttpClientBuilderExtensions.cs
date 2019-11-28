using System.Net.Http;
using DefensiveCoding.Demos.Factories;
using Microsoft.Extensions.DependencyInjection;
using Polly.CircuitBreaker;

namespace DefensiveCoding.Demos.Extensions
{
    /// <summary>
    /// A nice way to apply policies to HttpClientFactory
    /// Note:  For demo, a real implementation would allow for policy settings to be tweaked and customer delegates to be defined.
    /// </summary>
    internal static class HttpClientBuilderExtensions
    {
        // return a reference to the circuit breaker so callers can maninuplate it as needed for testing
        public static IHttpClientBuilder AddResiliencyPolicies(this IHttpClientBuilder builder, out ICircuitBreakerPolicy<HttpResponseMessage> circuitBreaker, string defaultValue)
        {
            var circuitBreakerPolicy = DemoPolicyFactory.GetHttpCircuitBreakerPolicy();
            circuitBreaker = (ICircuitBreakerPolicy<HttpResponseMessage>)circuitBreakerPolicy;

            builder
                .AddPolicyHandler(DemoPolicyFactory.GetHttpFallbackPolicy(defaultValue))
                .AddPolicyHandler(DemoPolicyFactory.GetHttpRetryPolicy())
                .AddPolicyHandler(circuitBreakerPolicy)
                .AddPolicyHandler(DemoPolicyFactory.GetHttpInnerTimeoutPolicy());

            return builder;
        }
    }
}
