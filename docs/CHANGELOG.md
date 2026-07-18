# Changelog

All notable user-visible changes to UsageBeacon will be documented in this file.

## Unreleased

### Added

- Automatic migration of the legacy Token Checker settings directory and startup registration.
- English project documentation for contributing, security reporting, and attribution.
- Optional Claude Code status line integration for native five-hour and weekly usage data.
- Automated tests for Claude OAuth refresh, usage responses, rate-limit cooldowns, credential expiry, and status line preservation.

### Changed

- Renamed the application, solution, projects, executable, and namespaces to UsageBeacon.
- Reframed the repository as an independent, unofficial community fork with explicit upstream credit.
- Reduced automatic Claude OAuth usage polling to a minimum interval of 30 minutes.
- Preserved server-provided rate-limit cooldowns during manual refresh.
- Recorded Claude cache freshness and data source only after successful retrieval.

## Upstream history

Changes made before the UsageBeacon rename belong to the history of [satonico/Token-Checker-win](https://github.com/satonico/Token-Checker-win) and this repository's Git history.
