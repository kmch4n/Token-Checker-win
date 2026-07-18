using UsageBeacon.Models;
using UsageBeacon.Services;

namespace UsageBeacon.Providers;

public sealed class CodexUsageProvider : IUsageProvider, IAsyncDisposable
{
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);

    private readonly CodexAppServerClient _client;
    private int _consecutiveFailures;
    private DateTime _nextAttemptUtc = DateTime.MinValue;
    private DomainError? _lastError;

    public CodexUsageProvider(CodexAppServerClient? client = null)
        => _client = client ?? new CodexAppServerClient();

    public async Task<ServiceUsage> FetchAsync(CancellationToken ct = default)
    {
        // Back off after repeated failures to avoid a start-exit loop on every poll.
        if (_lastError != null && DateTime.UtcNow < _nextAttemptUtc)
            throw _lastError;

        try
        {
            var usage = await FetchOnceAsync(ct);
            _consecutiveFailures = 0;
            _nextAttemptUtc = DateTime.MinValue;
            _lastError = null;
            return usage;
        }
        catch (OperationCanceledException)
        {
            throw; // Do not count shutdown or cancellation as a failure.
        }
        catch (DomainError e)
        {
            _consecutiveFailures++;
            var seconds = Math.Min(MaxBackoff.TotalSeconds, 5 * Math.Pow(2, _consecutiveFailures - 1));
            _nextAttemptUtc = DateTime.UtcNow.AddSeconds(seconds);

            // Attach stderr diagnostics such as an old CLI version when available.
            var stderr = _client.LastStderr;
            // Preserve the explicit unauthorized error instead of replacing it with stderr.
            _lastError = (string.IsNullOrWhiteSpace(stderr) || e.Kind == DomainErrorKind.CodexUnauthorized)
                ? e
                : DomainError.CodexRpcError($"{e.Message} / {stderr}");
            throw _lastError;
        }
    }

    /// <summary>Clears backoff so a manual refresh can retry immediately.</summary>
    public void ResetBackoff()
    {
        _consecutiveFailures = 0;
        _nextAttemptUtc = DateTime.MinValue;
        _lastError = null;
        // Restart the old process after sign-in so it uses the new token.
        _client.Stop();
    }

    private async Task<ServiceUsage> FetchOnceAsync(CancellationToken ct)
    {
        try
        {
            await _client.StartAsync(ct);
            return Map(await _client.ReadRateLimitsAsync(ct));
        }
        catch (DomainError e) when (e.Kind == DomainErrorKind.CodexProcessExited)
        {
            // Restart and retry once if the process exited.
            _client.Stop();
            await _client.StartAsync(ct);
            return Map(await _client.ReadRateLimitsAsync(ct));
        }
    }

    private static ServiceUsage Map(CodexRateLimitsDto dto) => new(
        FiveHour:     dto.FiveHourRateLimit(),
        Weekly:       dto.WeeklyRateLimit(),
        WeeklySonnet: null);

    public ValueTask DisposeAsync() => _client.DisposeAsync();
}
