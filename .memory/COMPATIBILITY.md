# Legacy Compatibility Contracts

Last reviewed: 2026-07-18

The entries below are intentional upgrade contracts, not incomplete rename work. Do not remove them without a deliberate breaking-change decision, tests, and release documentation.

## Application data

- Current directory: `%APPDATA%\UsageBeacon`
- Legacy directory: `%APPDATA%\TokenChecker`
- When only the legacy directory exists, UsageBeacon attempts to move it to the current name.
- If the move is blocked by another process or policy, UsageBeacon continues using the legacy directory instead of discarding settings.

Evidence: [`UsageBeacon/Services/AppDataPaths.cs`](../UsageBeacon/Services/AppDataPaths.cs).

## Windows startup registration

- Current Run value name: `UsageBeacon`
- Legacy Run value name: `TokenChecker`
- On migration, UsageBeacon creates the current value when needed and removes the legacy value.
- Disabling startup removes both names. Startup detection accepts either name during the transition.

Evidence: [`UsageBeacon/Services/StartupManager.cs`](../UsageBeacon/Services/StartupManager.cs).

## Single-instance behavior

- Current mutex: `Local\UsageBeacon`
- Legacy mutex: `Local\TokenChecker`
- UsageBeacon holds both mutexes so the renamed application cannot run alongside a legacy TokenChecker process.

Evidence: [`UsageBeacon/App.xaml.cs`](../UsageBeacon/App.xaml.cs).

## Required regression coverage

Changes to these contracts should cover fresh installs, successful migration, blocked migration, both startup value names, startup disablement, and coexistence with a legacy process as applicable.
