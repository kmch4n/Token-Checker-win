using TokenChecker.Models;
using TokenChecker.Services;

namespace TokenChecker.Providers;

public sealed class CodexUsageProvider : IUsageProvider, IAsyncDisposable
{
    private readonly CodexAppServerClient _client;

    public CodexUsageProvider(CodexAppServerClient? client = null)
        => _client = client ?? new CodexAppServerClient();

    public async Task<ServiceUsage> FetchAsync(CancellationToken ct = default)
    {
        try
        {
            await _client.StartAsync(ct);
            var dto = await _client.ReadRateLimitsAsync(ct);
            return new ServiceUsage(
                FiveHour:     dto.FiveHourRateLimit(),
                Weekly:       dto.WeeklyRateLimit(),
                WeeklySonnet: null);
        }
        catch (DomainError e) when (e.Kind == DomainErrorKind.CodexProcessExited)
        {
            // プロセスが落ちていたら再起動して再試行
            _client.Stop();
            await _client.StartAsync(ct);
            var dto = await _client.ReadRateLimitsAsync(ct);
            return new ServiceUsage(
                FiveHour:     dto.FiveHourRateLimit(),
                Weekly:       dto.WeeklyRateLimit(),
                WeeklySonnet: null);
        }
    }

    public ValueTask DisposeAsync() => _client.DisposeAsync();
}
