# Decision Log

## D-001: Maintain an independent unofficial fork

- Date: 2026-07-18
- Status: Active
- Decision: Develop the fork independently while preserving respectful upstream attribution and avoiding any implication of endorsement.
- Reason: Upstream contributions were not being accepted, but continued Windows-focused maintenance is desired.
- Consequences: Fork status must remain prominent in the README and attribution documentation. The upstream MIT License remains unchanged.
- Evidence: [`README.md`](../README.md) and [`docs/NOTICE.md`](../docs/NOTICE.md).

## D-002: Rename the product and .NET projects to UsageBeacon

- Date: 2026-07-18
- Status: Active
- Decision: Use `UsageBeacon` for the product, executable, solution, projects, namespaces, application data, startup registration, and primary mutex.
- Reason: The fork needs a distinct identity while continuing the original product direction.
- Consequences: Legacy `TokenChecker` names remain only where required for migration, compatibility, history, and attribution.
- Evidence: [`UsageBeacon.sln`](../UsageBeacon.sln), [`UsageBeacon/UsageBeacon.csproj`](../UsageBeacon/UsageBeacon.csproj), and [`COMPATIBILITY.md`](COMPATIBILITY.md).

## D-003: Keep repository documentation in English

- Date: 2026-07-18
- Status: Active
- Decision: Keep `README.md` at the repository root and place other public documentation under `docs/`. Repository-facing templates and agent guidance are also English.
- Reason: A consistent public language and predictable documentation layout make the fork easier to maintain and contribute to.
- Consequences: The Japanese application UI is not translated by this decision.
- Evidence: [`README.md`](../README.md), [`docs/`](../docs/), and [`.github/`](../.github/).

## D-004: Preserve legacy installations during the rename

- Date: 2026-07-18
- Status: Active
- Decision: Migrate legacy application data and startup registration automatically, fall back safely when data migration is blocked, and hold the legacy mutex.
- Reason: Existing Token Checker for Windows users must not lose settings or accidentally run both applications after upgrading.
- Consequences: Compatibility identifiers cannot be removed as cosmetic leftovers without an explicit migration plan.
- Evidence: [`COMPATIBILITY.md`](COMPATIBILITY.md).

## D-005: Version repository knowledge explicitly

- Date: 2026-07-18
- Status: Active
- Decision: Store non-obvious repository knowledge in `.memory/` and prohibit reliance on unrecorded agent context.
- Reason: Decisions and constraints must survive tool changes, new sessions, and contributor handoffs.
- Consequences: Every change that introduces or invalidates a non-obvious constraint must update `.memory/` in the same work.
- Evidence: [`.memory/README.md`](README.md) and [`.codex/AGENTS.md`](../.codex/AGENTS.md).
