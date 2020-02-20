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
                DemoPolicyFactory.GetHttpFallbackPolicy("Default!")
                    .WrapAsync(DemoPolicyFactory.GetHttpRetryPolicy()
                    .WrapAsync(DemoPolicyFactory.GetHttpCircuitBreakerPolicy())
                    .WrapAsync(DemoPolicyFactory.GetHttpInnerTimeoutPolicy()))
                    .WithPolicyKey("CustomerServicePolicy"); // nice to include for logging

            // does not include retry
            var productServicePolicy =
                DemoPolicyFactory.GetHttpFallbackPolicy("Default!")
                    .WrapAsync(DemoPolicyFactory.GetHttpCircuitBreakerPolicy())
                    .WrapAsync(DemoPolicyFactory.GetHttpInnerTimeoutPolicy())
                    .WithPolicyKey("ProductServicePolicy");

            registry.Add("CustomerServicePolicy", customerServicePolicy);
            registry.Add("ProductServicePolicy", productServicePolicy);

            return registry;
        }
    }
}
