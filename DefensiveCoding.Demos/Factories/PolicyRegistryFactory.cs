using System;
using System.Collections.Generic;
using System.Text;
using Polly;
using Polly.Registry;

namespace DefensiveCoding.Demos.Factories
{
    internal static class PolicyRegistryFactory
    {
        public static IPolicyRegistry<string> GetPolicyRegistry()
        {
            PolicyRegistry registry = new PolicyRegistry();

            var customerServicePolicy =
                DemoPolicyFactory.GetHttpFallbackPolicy()
                    .WrapAsync(DemoPolicyFactory.GetHttpRetryPolicy()
                    .WrapAsync(DemoPolicyFactory.GetHttpCircuitBreakerPolicy())
                    .WrapAsync(DemoPolicyFactory.GetHttpInnerTimeoutPolicy()));

            // does not include retry
            var productServicePolicy =
                DemoPolicyFactory.GetHttpFallbackPolicy()
                    .WrapAsync(DemoPolicyFactory.GetHttpCircuitBreakerPolicy())
                    .WrapAsync(DemoPolicyFactory.GetHttpInnerTimeoutPolicy());

            registry.Add("CustomerServicePolicy", customerServicePolicy);
            registry.Add("ProductServicePolicy", productServicePolicy);

            return registry;
        }
    }
}
