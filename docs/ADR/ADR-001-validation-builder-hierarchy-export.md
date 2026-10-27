# ADR-001: Validation builder hierarchy and root-only materialization

- Status: Accepted
- Date: 2026-05-10
- Deciders: ValidationGuard maintainers

## Context

ValidationBuilder originally relied on centralized parent-store accumulation and an `Export` naming model. The implementation evolved to support:

- explicit root vs nested builder roles,
- scoped entry storage per builder node,
- lazy hierarchy traversal for entry collection, and
- deterministic sorting with a dedicated comparer (`ValidationEntryComparer`) at final materialization.

Recent API changes introduced `ToValidationEntries()` on the root builder and removed export capability from nested builders.

Canonical internal entry storage is maintained in a near-RFC shape (`pointer`, `code`, `format`, `detail`) so root materialization can be projected to Problem Details `errors` with minimal, lossless transformation.

## Decision

Adopt a hierarchical builder architecture where:

1. Each builder node stores its own entries.
2. Child relationships are tracked as a tree of builder nodes.
3. Nested builders do not expose export/finalization APIs.
4. The root builder is the only public finalization point via `ToValidationEntries()`, which completes the whole
   hierarchy: entries are rendered once, later calls return the same entries, and no entry or nested builder can be
   added afterwards. `Valid` checks for entries without completing.
5. Materialization walks the hierarchy twice through the internal `IValidationBuilderNode`: `CountEntries()` sizes the
   result array exactly, then `Render()` completes each entry (pointer and detail) in place and writes it into that
   array. No intermediate collections are allocated.
6. Deterministic ordering is enforced at finalization with `ValidationEntryComparer`: by pointer, code, then the
   format as added (before translation), keeping entries that compare equal in the order they were added.

## Consequences

### Positive

- Clear ownership of finalization at the root.
- Better separation of scoped accumulation and final output materialization.
- Reduced intermediate allocations during hierarchy traversal.
- Deterministic output behavior is explicit and testable.

### Negative / Trade-offs

- Additional internal abstraction (`IValidationBuilderNode`) is required to traverse mixed generic child nodes.
- Entries are incomplete (no pointer or detail) until the root materializes them, so they cannot be inspected per
  scope before then.

## Alternatives Considered

1. Continue centralized parent-store writes from nested builders.
   - Rejected: weaker scoped ownership and less clear lifecycle boundaries.
2. Allow nested builders to export directly.
   - Rejected: conflicts with root-owned finalization model.
3. Keep LINQ sort chain in root finalization.
   - Rejected: replaced with `Array.Sort` + dedicated comparer for explicitness and lower overhead.
