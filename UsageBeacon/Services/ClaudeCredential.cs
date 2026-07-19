namespace UsageBeacon.Services;

public sealed record ClaudeCredential(
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset? ExpiresAt,
    IReadOnlyList<string> Scopes,
    string Source)
{
    private static readonly TimeSpan ExpirationSkew = TimeSpan.FromMinutes(1);

    public bool IsUsableAt(DateTimeOffset now) =>
        !string.IsNullOrWhiteSpace(AccessToken) &&
        (!ExpiresAt.HasValue || ExpiresAt.Value > now.Add(ExpirationSkew));

    internal ClaudeCredentialOrigin Origin { get; init; } =
        ClaudeCredentialOrigin.Unknown;
}

internal enum ClaudeCredentialOriginKind
{
    Unknown,
    WindowsFile,
    CredentialManager,
    WslFile,
}

internal sealed record ClaudeCredentialOrigin(
    ClaudeCredentialOriginKind Kind,
    string? Identifier)
{
    public static ClaudeCredentialOrigin Unknown { get; } =
        new(ClaudeCredentialOriginKind.Unknown, null);
}

public interface IClaudeCredentialSource
{
    Task<ClaudeCredential> ReadCredentialAsync(CancellationToken ct = default);
}

public interface IClaudeTokenRefresher
{
    Task<ClaudeCredential> RefreshAsync(
        ClaudeCredential credential,
        CancellationToken ct = default);
}

public enum ClaudeCredentialPersistenceStatus
{
    Persisted,
    SourceChanged,
    Unsupported,
    Failed,
}

public interface IClaudeCredentialStore
{
    bool CanPersist(ClaudeCredential credential);

    Task<ClaudeCredentialPersistenceStatus> PersistRefreshedCredentialAsync(
        ClaudeCredential original,
        ClaudeCredential refreshed,
        CancellationToken ct = default);
}
