using UsageBeacon.Models;
using UsageBeacon.Services;

namespace UsageBeacon.Providers;

public sealed class ClaudeUsageProvider : IUsageProvider
{
    private readonly IClaudeCredentialSource _credentialSource;
    private readonly IClaudeCredentialStore _credentialStore;
    private readonly IAnthropicUsageApiClient _api;
    private readonly IClaudeTokenRefresher _tokenRefresher;
    private readonly SemaphoreSlim _credentialGate = new(1, 1);
    private ClaudeCredential? _cachedCredential;
    private PendingCredentialUpdate? _pendingUpdate;

    public ClaudeUsageProvider(
        IClaudeCredentialSource? credentialSource = null,
        IAnthropicUsageApiClient? api = null,
        IClaudeTokenRefresher? tokenRefresher = null,
        IClaudeCredentialStore? credentialStore = null)
    {
        _credentialSource = credentialSource ?? new WindowsTokenSource();
        _credentialStore = credentialStore ?? new ClaudeCredentialFileStore();
        _api = api ?? new AnthropicUsageApiClient();
        _tokenRefresher = tokenRefresher ?? new ClaudeOAuthClient();
    }

    public async Task<ServiceUsage> FetchAsync(CancellationToken ct = default)
    {
        var lease = await GetUsableCredentialAsync(ct);
        AnthropicUsageDto dto;
        try
        {
            dto = await _api.FetchAsync(lease.Credential.AccessToken, ct);
        }
        catch (DomainError e) when (
            e.Kind == DomainErrorKind.AnthropicUnauthorized &&
            !string.IsNullOrWhiteSpace(lease.Credential.RefreshToken))
        {
            var retryCredential = await GetCredentialAfterUnauthorizedAsync(lease, ct);
            dto = await _api.FetchAsync(retryCredential.AccessToken, ct);
        }

        return new ServiceUsage(
            FiveHour:     dto.FiveHour?.ToRateLimit(),
            Weekly:       dto.SevenDay?.ToRateLimit(),
            WeeklySonnet: dto.SevenDaySonnet?.ToRateLimit());
    }

    private async Task<CredentialLease> GetUsableCredentialAsync(CancellationToken ct)
    {
        await _credentialGate.WaitAsync(ct);
        try
        {
            await TryPersistPendingUpdateAsync();
            var now = DateTimeOffset.UtcNow;
            var credential = _pendingUpdate?.Refreshed.IsUsableAt(now) == true
                ? _pendingUpdate.Refreshed
                : _cachedCredential?.IsUsableAt(now) == true
                    ? _cachedCredential
                    : await _credentialSource.ReadCredentialAsync(ct);
            if (credential.IsUsableAt(now))
            {
                _cachedCredential = credential;
                return new CredentialLease(credential, WasRefreshed: false);
            }

            return await RefreshAndPersistAsync(credential, ct);
        }
        finally
        {
            _credentialGate.Release();
        }
    }

    private async Task<ClaudeCredential> GetCredentialAfterUnauthorizedAsync(
        CredentialLease rejectedLease,
        CancellationToken ct)
    {
        await _credentialGate.WaitAsync(ct);
        try
        {
            await TryPersistPendingUpdateAsync();
            var now = DateTimeOffset.UtcNow;
            if (_cachedCredential?.IsUsableAt(now) == true &&
                !HasSameOAuthState(_cachedCredential, rejectedLease.Credential))
                return _cachedCredential;

            if (rejectedLease.WasRefreshed)
                throw DomainError.AnthropicUnauthorized();

            var latest = await _credentialSource.ReadCredentialAsync(ct);
            if (latest.IsUsableAt(now) &&
                !HasSameOAuthState(latest, rejectedLease.Credential))
            {
                _cachedCredential = latest;
                return latest;
            }

            return (await RefreshAndPersistAsync(rejectedLease.Credential, ct)).Credential;
        }
        finally
        {
            _credentialGate.Release();
        }
    }

    private async Task<CredentialLease> RefreshAndPersistAsync(
        ClaudeCredential credential,
        CancellationToken ct)
    {
        if (!_credentialStore.CanPersist(credential))
            throw DomainError.AnthropicUnauthorized();

        ClaudeCredential refreshed;
        try
        {
            refreshed = await _tokenRefresher.RefreshAsync(credential, ct);
        }
        catch (DomainError e) when (e.Kind == DomainErrorKind.AnthropicUnauthorized)
        {
            var latest = await _credentialSource.ReadCredentialAsync(ct);
            if (latest.IsUsableAt(DateTimeOffset.UtcNow) &&
                !HasSameOAuthState(latest, credential))
            {
                _cachedCredential = latest;
                return new CredentialLease(latest, WasRefreshed: false);
            }

            throw;
        }

        _pendingUpdate = new PendingCredentialUpdate(credential, refreshed);
        _cachedCredential = refreshed;
        await TryPersistPendingUpdateAsync();
        return new CredentialLease(refreshed, WasRefreshed: true);
    }

    private async Task TryPersistPendingUpdateAsync()
    {
        var pending = _pendingUpdate;
        if (pending == null) return;

        ClaudeCredentialPersistenceStatus status;
        try
        {
            status = await _credentialStore.PersistRefreshedCredentialAsync(
                pending.Original,
                pending.Refreshed,
                CancellationToken.None);
        }
        catch
        {
            status = ClaudeCredentialPersistenceStatus.Failed;
        }

        if (status == ClaudeCredentialPersistenceStatus.Persisted)
            _pendingUpdate = null;
        else if (status == ClaudeCredentialPersistenceStatus.SourceChanged)
            await ReconcileChangedSourceAsync(pending);
    }

    private async Task ReconcileChangedSourceAsync(PendingCredentialUpdate pending)
    {
        try
        {
            var latest = await _credentialSource.ReadCredentialAsync(CancellationToken.None);
            if (HasSameOAuthState(latest, pending.Refreshed))
                _pendingUpdate = null;
        }
        catch
        {
            // Keep the refreshed credential pending for this process lifetime.
        }
    }

    private static bool HasSameOAuthState(
        ClaudeCredential left,
        ClaudeCredential right) =>
        string.Equals(left.AccessToken, right.AccessToken, StringComparison.Ordinal) &&
        string.Equals(left.RefreshToken, right.RefreshToken, StringComparison.Ordinal) &&
        left.ExpiresAt?.ToUnixTimeMilliseconds() ==
            right.ExpiresAt?.ToUnixTimeMilliseconds() &&
        left.Scopes.SequenceEqual(right.Scopes, StringComparer.Ordinal);

    private sealed record CredentialLease(
        ClaudeCredential Credential,
        bool WasRefreshed);

    private sealed record PendingCredentialUpdate(
        ClaudeCredential Original,
        ClaudeCredential Refreshed);
}
