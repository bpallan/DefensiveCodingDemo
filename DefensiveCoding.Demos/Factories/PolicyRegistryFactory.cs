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
                DemoPolicyFactory.GetFallbackPolicy()
                    .WrapAsync(DemoPolicyFactory.GetRetryPolicy()
                    .WrapAsync(DemoPolicyFactory.GetCircuitBreakerPolicy())
                    .WrapAsync(DemoPolicyFactory.GetInnerTimeoutPolicy()));

            // does not include retry
            var productServicePolicy =
                DemoPolicyFactory.GetFallbackPolicy()
                    .WrapAsync(DemoPolicyFactory.GetCircuitBreakerPolicy())
                    .WrapAsync(DemoPolicyFactory.GetInnerTimeoutPolicy());

            registry.Add("CustomerServicePolicy", customerServicePolicy);
            registry.Add("ProductServicePolicy", productServicePolicy);

            return registry;
        }
    }
}
