# Repository Memory

This directory is the canonical, versioned record of repository knowledge that is not sufficiently obvious from the current source tree. Its purpose is to prevent maintainers and agents from relying on private conversational context or unstated assumptions.

## Required workflow

1. Read this file and the indexed files below before planning or modifying the repository.
2. Verify facts that may have changed, especially remotes, branch state, release links, dependency versions, and external service behavior.
3. Record newly discovered constraints, decisions, compatibility requirements, and pending external work in the appropriate file during the same change.
4. Update existing entries rather than creating conflicting versions of the same fact.
5. Reconcile memory with the implementation before committing or handing work off.

If memory conflicts with current source or Git state, treat the source or Git state as evidence of current behavior and correct the memory. User instructions and repository agent guidelines remain authoritative for how work is performed.

## Index

- [PROJECT.md](PROJECT.md): stable identity, architecture, conventions, and security boundaries.
- [DECISIONS.md](DECISIONS.md): durable decisions, their reasons, and consequences.
- [COMPATIBILITY.md](COMPATIBILITY.md): contracts retained from Token Checker for Windows.
- [STATUS.md](STATUS.md): verified repository state, validation results, and pending external work.

## Recording standards

- Write shared memory in English using UTF-8 without a BOM and LF newlines.
- Distinguish verified facts, decisions, and temporary status.
- Use ISO dates and mark decisions as `Active` or `Superseded`.
- Link to source files or documentation that support an entry.
- Do not duplicate full documentation or source code; record the constraint and point to its evidence.
- Do not store credentials, tokens, personal data, local caches, or absolute machine-specific paths.

Local-only notes may be placed in `.memory/local/`, which is ignored by Git. Required project knowledge must never exist only in that directory.
