# Project Memory

Last reviewed: 2026-07-18

## Identity

- The product name is **UsageBeacon**.
- UsageBeacon is an independent, unofficial community fork of `satonico/Token-Checker-win`.
- The project is not affiliated with or endorsed by the upstream maintainer, Anthropic, or OpenAI.
- Upstream attribution and the original MIT License must remain intact.

Evidence: [`README.md`](../README.md), [`docs/NOTICE.md`](../docs/NOTICE.md), and [`LICENSE`](../LICENSE).

## Technology and structure

- `UsageBeacon.sln` contains a .NET 8 WPF application and xUnit tests.
- Application code lives in `UsageBeacon/`; tests live in `UsageBeacon.Tests/`.
- Public documentation other than `README.md` belongs under `docs/`.
- GitHub Issue Forms and pull request templates belong under `.github/`.
- API and Windows integration code must remain outside views and follow the existing `Providers/`, `Services/`, and `Utilities/` boundaries.

## Language conventions

- Repository-facing documentation, code comments, commit messages, Issues, and pull requests are written in English.
- The current user-facing application UI is Japanese. Preserve that language unless localization is explicitly introduced.

## Security boundaries

- Never commit Claude or Codex credentials, tokens, local caches, or machine-specific paths.
- Credential discovery, WSL integration, startup registration, local cache handling, command execution, and network clients are security-sensitive.
- Public bug reports must direct vulnerability reports to the repository security policy.

Evidence: [`docs/SECURITY.md`](../docs/SECURITY.md) and [`.github/ISSUE_TEMPLATE/config.yml`](../.github/ISSUE_TEMPLATE/config.yml).
