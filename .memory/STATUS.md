# Current Repository Status

Last verified: 2026-07-18

## Git and naming state

- The active development branch is `main`.
- The `fork` remote currently points to `https://github.com/kmch4n/Token-Checker-win.git`.
- The `origin` remote points to `https://github.com/satonico/Token-Checker-win`.
- Product, solution, projects, namespaces, and executable have been renamed to UsageBeacon.
- The GitHub repository itself has not yet been renamed. The README already uses the intended `kmch4n/UsageBeacon` clone and release URLs, so those links depend on completing the repository rename or being corrected.

Remote facts are drift-prone and must be verified with `git remote -v` before relying on them.

## Shared agent configuration

- `.codex/AGENTS.md` is the canonical repository agent guidance.
- `.claude/CLAUDE.md` imports that guidance so both agent environments use the same rules.
- `.claude/settings.local.json`, `.codex/config.local.toml`, and `.memory/local/` are explicitly local-only and ignored.

## Last validation

The UsageBeacon rename and documentation changes were validated on 2026-07-18:

- `dotnet test UsageBeacon.sln -c Debug`: 2 passed, 0 failed.
- `dotnet build UsageBeacon.sln -c Debug`: 0 warnings, 0 errors.
- `dotnet build UsageBeacon.sln -c Release`: 0 warnings, 0 errors when built to an alternate output path.
- GitHub Issue Form YAML parsed successfully.

The ordinary Release output path was locked by a running UsageBeacon process during the final check. When this occurs, close the running application with user awareness or use an alternate `BaseOutputPath`; do not treat the file lock as a compilation failure.
