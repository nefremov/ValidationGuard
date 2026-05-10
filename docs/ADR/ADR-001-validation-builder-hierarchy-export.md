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

## Decision

Adopt a hierarchical builder architecture where:

1. Each builder node stores its own entries.
2. Child relationships are tracked as a tree of builder nodes.
3. Nested builders do not expose export/finalization APIs.
4. The root builder is the only public finalization point via `ToValidationEntries()`.
5. Hierarchy traversal uses lazy `IEnumerable<ValidationEntry>` (`GetEntries()`) and allocates an array only at final root materialization.
6. Deterministic ordering is enforced at finalization with `ValidationEntryComparer`.

## Consequences

### Positive

- Clear ownership of finalization at the root.
- Better separation of scoped accumulation and final output materialization.
- Reduced intermediate allocations during hierarchy traversal.
- Deterministic output behavior is explicit and testable.

### Negative / Trade-offs

- Additional internal abstraction (`IValidationBuilderNode`) is required to traverse mixed generic child nodes.
- Merge logic requires runtime checks ensuring builders belong to the same internal model.

## Alternatives Considered

1. Continue centralized parent-store writes from nested builders.
   - Rejected: weaker scoped ownership and less clear lifecycle boundaries.
2. Allow nested builders to export directly.
   - Rejected: conflicts with root-owned finalization model.
3. Keep LINQ sort chain in root finalization.
   - Rejected: replaced with `Array.Sort` + dedicated comparer for explicitness and lower overhead.
