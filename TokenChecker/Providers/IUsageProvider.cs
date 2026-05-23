using TokenChecker.Models;

namespace TokenChecker.Providers;

public interface IUsageProvider
{
    Task<ServiceUsage> FetchAsync(CancellationToken ct = default);
}
