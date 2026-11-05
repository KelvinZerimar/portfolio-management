using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Infrastructure.Resilience;

public static class InferenceResilienceExtensions
{
    public static IHttpClientBuilder AddInferenceResilience(this IHttpClientBuilder builder, TimeSpan requestTimeout)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            // Per-attempt timeout for LLM inference calls (Ai:RequestTimeoutSeconds) - local CPU
            // inference can legitimately take well over the library's 10s default, especially on
            // a cold model load, so this must stay driven by configuration rather than hardcoded.
            options.AttemptTimeout.Timeout = requestTimeout;

            // Cumulative timeout budget across all retries. Twice the attempt timeout: still too
            // short to let a retry re-run an attempt that was itself killed for being slow (that
            // would just double the wait on an already-slow local model), but enough headroom for
            // a fast-failing attempt (e.g. a 429/503) to be retried once within budget.
            options.TotalRequestTimeout.Timeout = requestTimeout * 2;

            // Jittered exponential backoff for HTTP 429 (Rate Limit) and HTTP 5xx (Inference Server Overload)
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.Retry.Delay = TimeSpan.FromSeconds(2);

            // Circuit breaker sampling duration must be >= 2 * AttemptTimeout
            options.CircuitBreaker.SamplingDuration = requestTimeout * 2;
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 8;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
        });

        return builder;
    }
}
