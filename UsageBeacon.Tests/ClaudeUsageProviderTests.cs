using UsageBeacon.Models;
using UsageBeacon.Providers;
using UsageBeacon.Services;

namespace UsageBeacon.Tests;

public sealed class ClaudeUsageProviderTests
{
    [Fact]
    public async Task FetchAsync_RefreshesExpiredCredential_BeforeFetchingUsage()
    {
        var expired = new ClaudeCredential(
            "expired",
            "refresh",
            DateTimeOffset.UtcNow.AddHours(-1),
            ["user:profile"],
            "test");
        var refreshed = expired with
        {
            AccessToken = "fresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        };
        var api = new StubUsageApiClient();
        var refresher = new StubTokenRefresher(refreshed);
        var provider = new ClaudeUsageProvider(
            new StubCredentialSource(expired),
            api,
            refresher,
            new StubCredentialStore());

        var result = await provider.FetchAsync();

        Assert.Equal("fresh", api.LastAccessToken);
        Assert.Equal(1, refresher.CallCount);
        Assert.Equal(0.25, result.FiveHour?.Utilization);
    }

    [Fact]
    public async Task FetchAsync_RefreshesAndRetries_WhenUsageApiReturnsUnauthorized()
    {
        var valid = new ClaudeCredential(
            "rejected",
            "refresh",
            DateTimeOffset.UtcNow.AddHours(1),
            ["user:profile"],
            "test");
        var refreshed = valid with { AccessToken = "fresh" };
        var api = new StubUsageApiClient(rejectFirstRequest: true);
        var refresher = new StubTokenRefresher(refreshed);
        var provider = new ClaudeUsageProvider(
            new StubCredentialSource(valid),
            api,
            refresher,
            new StubCredentialStore());

        await provider.FetchAsync();

        Assert.Equal(2, api.CallCount);
        Assert.Equal("fresh", api.LastAccessToken);
        Assert.Equal(1, refresher.CallCount);
    }

    [Fact]
    public async Task FetchAsync_DoesNotRefresh_WhenCredentialCannotBePersisted()
    {
        var expired = new ClaudeCredential(
            "expired",
            "refresh",
            DateTimeOffset.UtcNow.AddHours(-1),
            [],
            "credential-manager:test");
        var refresher = new StubTokenRefresher(expired with
        {
            AccessToken = "fresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        });
        var provider = new ClaudeUsageProvider(
            new StubCredentialSource(expired),
            new StubUsageApiClient(),
            refresher,
            new StubCredentialStore(canPersist: false));

        var error = await Assert.ThrowsAsync<DomainError>(() => provider.FetchAsync());

        Assert.Equal(DomainErrorKind.AnthropicUnauthorized, error.Kind);
        Assert.Equal(0, refresher.CallCount);
    }

    [Fact]
    public async Task FetchAsync_RefreshesOnlyOnce_WhenCallsOverlap()
    {
        var expired = new ClaudeCredential(
            "expired",
            "refresh",
            DateTimeOffset.UtcNow.AddHours(-1),
            [],
            "test");
        var refreshed = expired with
        {
            AccessToken = "fresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        };
        var refresher = new StubTokenRefresher(refreshed, TimeSpan.FromMilliseconds(50));
        var provider = new ClaudeUsageProvider(
            new StubCredentialSource(expired),
            new StubUsageApiClient(),
            refresher,
            new StubCredentialStore());

        await Task.WhenAll(provider.FetchAsync(), provider.FetchAsync());

        Assert.Equal(1, refresher.CallCount);
    }

    [Fact]
    public async Task FetchAsync_RetainsRefreshedCredential_WhenPersistenceFails()
    {
        var expired = new ClaudeCredential(
            "expired",
            "refresh",
            DateTimeOffset.UtcNow.AddHours(-1),
            [],
            "test");
        var refreshed = expired with
        {
            AccessToken = "fresh",
            RefreshToken = "rotated",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        };
        var api = new StubUsageApiClient();
        var refresher = new StubTokenRefresher(refreshed);
        var store = new StubCredentialStore(
            status: ClaudeCredentialPersistenceStatus.Failed);
        var provider = new ClaudeUsageProvider(
            new StubCredentialSource(expired),
            api,
            refresher,
            store);

        await provider.FetchAsync();
        await provider.FetchAsync();

        Assert.Equal(1, refresher.CallCount);
        Assert.Equal("fresh", api.LastAccessToken);
        Assert.True(store.CallCount >= 2);
    }

    private sealed class StubCredentialSource(ClaudeCredential credential) : IClaudeCredentialSource
    {
        public Task<ClaudeCredential> ReadCredentialAsync(CancellationToken ct = default)
            => Task.FromResult(credential);
    }

    private sealed class StubTokenRefresher(
        ClaudeCredential credential,
        TimeSpan? delay = null) : IClaudeTokenRefresher
    {
        public int CallCount { get; private set; }

        public async Task<ClaudeCredential> RefreshAsync(
            ClaudeCredential current,
            CancellationToken ct = default)
        {
            CallCount++;
            if (delay.HasValue) await Task.Delay(delay.Value, ct);
            return credential;
        }
    }

    private sealed class StubCredentialStore(
        bool canPersist = true,
        ClaudeCredentialPersistenceStatus status =
            ClaudeCredentialPersistenceStatus.Persisted) : IClaudeCredentialStore
    {
        public int CallCount { get; private set; }

        public bool CanPersist(ClaudeCredential credential) => canPersist;

        public Task<ClaudeCredentialPersistenceStatus> PersistRefreshedCredentialAsync(
            ClaudeCredential original,
            ClaudeCredential refreshed,
            CancellationToken ct = default)
        {
            CallCount++;
            return Task.FromResult(status);
        }
    }

    private sealed class StubUsageApiClient(bool rejectFirstRequest = false) : IAnthropicUsageApiClient
    {
        public int CallCount { get; private set; }
        public string? LastAccessToken { get; private set; }

        public Task<AnthropicUsageDto> FetchAsync(
            string accessToken,
            CancellationToken ct = default)
        {
            CallCount++;
            LastAccessToken = accessToken;
            if (rejectFirstRequest && CallCount == 1)
                throw DomainError.AnthropicUnauthorized();

            return Task.FromResult(new AnthropicUsageDto
            {
                FiveHour = new BucketDto { Utilization = 25 },
            });
        }
    }
}
