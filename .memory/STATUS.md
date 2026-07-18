# Current Repository Status

Last verified: 2026-07-18

## Git and naming state

- The active development branch is `main`.
- The `fork` remote points to `https://github.com/kmch4n/UsageBeacon.git`.
- The `origin` remote points to `https://github.com/satonico/Token-Checker-win`.
- Product, solution, projects, namespaces, and executable have been renamed to UsageBeacon.
- The GitHub repository has been renamed to `kmch4n/UsageBeacon`, matching the clone and release URLs in the README.

Remote facts are drift-prone and must be verified with `git remote -v` before relying on them.

## Shared agent configuration

- `.codex/AGENTS.md` is the canonical repository agent guidance.
- `.claude/CLAUDE.md` imports that guidance so both agent environments use the same rules.
- `.claude/settings.local.json`, `.codex/config.local.toml`, and `.memory/local/` are explicitly local-only and ignored.

## Last validation

The Claude usage reliability changes were validated on 2026-07-18:

- `dotnet test UsageBeacon.sln -c Debug`: 17 passed, 0 failed.
- `dotnet build UsageBeacon.sln -c Debug`: 0 warnings, 0 errors.
- `dotnet build UsageBeacon.sln -c Release`: 0 warnings, 0 errors when built to an alternate output path.
- The status line integration test executed the embedded PowerShell bridge, parsed synthetic native rate-limit data, and verified that session paths were not persisted.
- A manual smoke test of the alternate Release build observed Claude usage retrieval after enabling the Claude Code integration.

The ordinary Release output path was locked by a running UsageBeacon process during the final check. When this occurs, close the running application with user awareness or use an alternate `BaseOutputPath`; do not treat the file lock as a compilation failure.
