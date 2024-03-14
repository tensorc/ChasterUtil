using System.Net;
using Polly;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;

namespace ChasterUtil;

public sealed class PollyHttpClient : HttpClient
{
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    public PollyHttpClient()
    {
        _pipeline = BuildPipeline(null);
    }

    public PollyHttpClient(HttpClientHandler httpClientHandler) : base(httpClientHandler)
    {
        _pipeline = BuildPipeline(null);
    }

    public PollyHttpClient(Func<FallbackActionArguments<HttpResponseMessage>, ValueTask<Outcome<HttpResponseMessage>>>? fallbackAction)
    {
        _pipeline = BuildPipeline(fallbackAction);
    }

    public PollyHttpClient(HttpClientHandler httpClientHandler, Func<FallbackActionArguments<HttpResponseMessage>, ValueTask<Outcome<HttpResponseMessage>>>? fallbackAction) : base(httpClientHandler)
    {
        _pipeline = BuildPipeline(fallbackAction);
    }

    private static ResiliencePipeline<HttpResponseMessage> BuildPipeline(Func<FallbackActionArguments<HttpResponseMessage>, ValueTask<Outcome<HttpResponseMessage>>>? fallbackAction)
    {
        var retryOptions = new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = 4,
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(r => r.StatusCode is >= HttpStatusCode.InternalServerError or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests)
                .Handle<HttpRequestException>(ex => ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests)
                .Handle<TimeoutRejectedException>(),
            DelayGenerator = GenerateDelay
        };

        var timeoutOptions = new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>().AddRetry(retryOptions).AddTimeout(timeoutOptions);

        if (fallbackAction is not null)
        {
            var fallbackOptions = new FallbackStrategyOptions<HttpResponseMessage>
            {
                FallbackAction = fallbackAction
            };

            pipeline.AddFallback(fallbackOptions);
        }

        return pipeline.Build();
    }

    private static ValueTask<TimeSpan?> GenerateDelay(RetryDelayGeneratorArguments<HttpResponseMessage> args)
    {
        if (args.Outcome.Result is { StatusCode: HttpStatusCode.TooManyRequests } response && response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValue))
        {
            var resetUnixTime = Convert.ToInt64(resetValue.FirstOrDefault());
            var resetTime = DateTimeOffset.FromUnixTimeSeconds(resetUnixTime);

            var delay = resetTime - DateTimeOffset.UtcNow;

            if (delay > TimeSpan.FromMinutes(1))
            {
                throw new Exception("Delay exceeds 1 minute. Retry should abort.");
            }

            return new ValueTask<TimeSpan?>(delay);
        }

        return new ValueTask<TimeSpan?>(TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber)));
    }

    public override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _pipeline.Execute(token => base.Send(request, token), cancellationToken);
    }

    public override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await _pipeline.ExecuteAsync(async token => await base.SendAsync(request, token), cancellationToken);
    }

}