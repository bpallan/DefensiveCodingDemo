using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Polly;

namespace DefensiveCoding.Demos.Helpers
{
    internal static class PolicyLoggingHelper
    {
        ///////////////////////// LOGGING DELEGATES /////////////////////////
        // Can use Func's instead
        public static void LogCircuitBroken<T>(DelegateResult<T> exception, TimeSpan timespan, Context context)
        {
            // example only, log whatever details you deem relevant for trouble shooting or monitoring
            Console.WriteLine($"Circuit is broken!  CorrelationId: {context?.CorrelationId}");
        }       

        public static void LogCircuitBroken(Exception exception, TimeSpan timespan, Context context)
        {
        }

        public static void LogCircuitReset(Context context)
        {
            // example only, log whatever details you deem relevant for trouble shooting or monitoring
            Console.WriteLine($"Circuit is reset!  CorrelationId: {context?.CorrelationId}");
        }

        public static async Task LogWaitAndRetryAsync<T>(DelegateResult<T> exception, TimeSpan timeSpan, Context context)
        {
            // example only, log whatever details you deem relevant for trouble shooting or monitoring
            await Console.Out.WriteLineAsync($"Retrying request!  CorrelationId: {context?.CorrelationId}");
        }

        public static async Task LogRetryAsync<T>(DelegateResult<T> exception, int retries, Context context)
        {
            await LogWaitAndRetryAsync(exception, TimeSpan.Zero, context);
        }

        public static async Task LogTimeoutAsync(Context context, TimeSpan timespan, Task task)
        {
            // example only, log whatever details you deem relevant for trouble shooting or monitoring
            await Console.Out.WriteLineAsync($"Timeout exceeded after {timespan.Seconds} seconds!  CorrelationId: {context?.CorrelationId}");
        }

        public static void LogTimeout(Context context, TimeSpan timespan, Task task)
        {
            // example only, log whatever details you deem relevant for trouble shooting or monitoring
            Console.Out.WriteLine($"Timeout exceeded after {timespan.Seconds} seconds!  CorrelationId: {context?.CorrelationId}");
        }

        public static async Task LogFallbackAsync<T>(DelegateResult<T> exception, Context context)
        {
            // example only, log whatever details you deem relevant for trouble shooting or monitoring
            await Console.Out.WriteAsync($"Returning fallback data!  CorrelationId: {context?.CorrelationId}");
        }
    }
}
