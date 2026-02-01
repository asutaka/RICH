using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace StockPr.Config
{
    public static class HttpPolicies
    {
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        logger.LogWarning($"Request failed with {outcome.Result?.StatusCode}. Waiting {timespan} before next retry. Attempt {retryAttempt}.");
                    });
        }

        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger logger)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1),
                    onBreak: (outcome, timespan) =>
                    {
                        logger.LogCritical($"CIRCUIT BREAKER: Breaking for {timespan} due to: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                    },
                    onReset: () => logger.LogInformation("CIRCUIT BREAKER: Reset."),
                    onHalfOpen: () => logger.LogInformation("CIRCUIT BREAKER: Half-open. Testing next request..."));
        }
    }
}
