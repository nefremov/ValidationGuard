# Decomposition: ValidationGuard

<!-- toc -->

- [1. Overview](#1-overview)
- [2. Entries](#2-entries)
  - [2.1 Validation Builder - HIGH](#21-validation-builder---high)
  - [2.2 Problem Details Mapping - MEDIUM](#22-problem-details-mapping---medium)
  - [2.3 ASP.NET Core Adapter - LOW](#23-aspnet-core-adapter---low)
- [3. Feature Dependencies](#3-feature-dependencies)

<!-- /toc -->

## 1. Overview

This decomposition breaks the design element `cpt-validationguard-design-validation-error-architecture` (DESIGN.md, Architecture Overview) into three implementable features. The design describes a layered library: a domain layer that accumulates typed validation errors, a mapping layer that projects those errors into RFC 9457 Problem Details, and a transport layer that returns them over HTTP through ASP.NET Core. Each layer becomes one feature.

**Decomposition strategy**:

- Features follow the layered architecture in DESIGN.md Section 1.1 and 1.3.
  - Domain accumulation, RFC 9457 mapping, and HTTP transport are separated because each has distinct responsibility boundaries stated in the component descriptions (DESIGN.md Section 3.2).
- Ordering and tiers follow the `Dependencies` line each FEATURE artifact already declares.
  - Validation Builder has no dependencies and is foundational (HIGH).
  - Problem Details Mapping depends only on Validation Builder (MEDIUM).
  - ASP.NET Core Adapter depends on both other features and sits at the outermost layer (LOW).
- Each DESIGN component maps to exactly one feature.
  - Pointer Builder and Validation Builder (`cpt-validationguard-component-pointer-builder`, `cpt-validationguard-component-validation-builder`) map to the Validation Builder feature.
  - ProblemDetails Mapper (`cpt-validationguard-component-problem-details-mapper`) maps to the Problem Details Mapping feature.
  - ASP.NET Core Adapter (`cpt-validationguard-component-aspnetcore-adapter`) maps to the ASP.NET Core Adapter feature.
- Two DESIGN components fold into the Validation Builder feature because they form one implementable unit.
  - Pointer composition has no consumer other than the builder that attaches pointers to entries (DESIGN.md Section 3.2, Pointer Builder "Related components").
  - They ship in the same package, behind the same public surface, so neither can be delivered or accepted on its own.
  - The other two features stay one-to-one with their component.
- Configuration items: the Validation Builder and Problem Details Mapping features ship together in the core package.
  - The ASP.NET Core Adapter ships as a separately versioned adapter package (DESIGN.md Section 1.1).
  - Core library and adapter version compatibility is maintained within the same major version (`cpt-validationguard-contract-aspnetcore-adapter-package`, PRD.md Section 7.2).
- The single DESIGN sequence `cpt-validationguard-seq-validation-failure-response` (DESIGN.md Section 3.6) spans all three components end to end.
  - It is listed under all three features below, each covering the segment of the sequence that its own component performs.
  - This is a deliberate shared reference, not a scope overlap: components and DoD deliverables stay distinct and non-overlapping across features.
  - Where a requirement or entity does repeat, the reason is annotated on the repeated entry itself.
- The NFR `cpt-validationguard-nfr-api-stability` is listed under all three features.
  - It is not a shared deliverable. The rule applies independently to each package's own public surface.
  - The builder API, the mapper API, and the adapter API each carry the guarantee on their own.
- The database element `cpt-validationguard-db-none` (DESIGN.md Section 3.7) states the library has no persistent storage requirement.
  - This applies to all three features equally, so it is dispositioned once here rather than repeated.
  - No feature below has persistent storage in scope, and every `Data` field is recorded as `N/A` for that reason.
- Subordinate implementation phases, steps, and Definition of Done items live in each linked FEATURE artifact. They are deliberately not duplicated here, so this document stays a feature-level breakdown only.

## 2. Entries

**Overall implementation status:**

- [ ] `p1` - **ID**: `cpt-validationguard-status-overall`

### 2.1 [Validation Builder](features/validation-builder.md) - HIGH

- [ ] `p1` - **ID**: `cpt-validationguard-feature-validation-builder`

- **Purpose**: Defines a validation-error accumulation surface that spares API developers from hand-assembling path strings for deep object graphs, an approach that is repetitive and drifts between validators.

- **Depends On**: None

- **Scope** (a feature-level summary; the normative wording lives in the PRD requirements listed under Requirements Covered below):
  - Register typed validation entries carrying `code`, `format`, `detail`, and an RFC 6901 `pointer`.
  - Compose RFC 6901 pointer segments for nested object members, array indices, and indexer/keyed accessors, including token escaping and numeric index validation.
  - Convert member-name pointer segments to a consumer-selected naming convention, defaulting to unchanged member names.
  - Create nested validation scopes for root objects, nested members, and collection elements, with each scope accumulating its own entries for as long as that scope is open.
  - Merge child validation results into a parent accumulator without dropping entries.
  - Materialize deterministic, flattened output from the root builder, so one traversal yields one ordered result set.

- **Out of scope**:
  - Projecting accumulated entries into RFC 9457 Problem Details format (owned by the Problem Details Mapping feature).
  - HTTP transport and ASP.NET Core pipeline integration (owned by the ASP.NET Core Adapter feature).

- **Requirements Covered**:

  - [ ] `p1` - `cpt-validationguard-fr-typed-error-registration`
  - [ ] `p1` - `cpt-validationguard-fr-canonical-internal-entry-storage`
  - [ ] `p1` - `cpt-validationguard-fr-nested-object-path-handling`
  - [ ] `p1` - `cpt-validationguard-fr-array-element-path-handling`
  - [ ] `p2` - `cpt-validationguard-fr-indexer-keyed-path-handling`
  - [ ] `p2` - `cpt-validationguard-fr-configurable-pointer-naming`
  - [ ] `p2` - `cpt-validationguard-fr-aggregation-and-merge`
  - [ ] `p2` - `cpt-validationguard-nfr-memory-efficiency`
  - [ ] `p1` - `cpt-validationguard-nfr-api-stability`
  - [ ] `p1` - `cpt-validationguard-interface-validation-builder-contract`

- **Design Principles Covered**:

  - [ ] `p2` - `cpt-validationguard-principle-stable-error-entry-contract`

- **Design Constraints Covered**:

  - [ ] `p2` - `cpt-validationguard-constraint-strict-net10-target`

- **Domain Model Entities**:
  - ValidationBuilder
  - ValidationEntry

- **Design Components**:

  - [ ] `p2` - `cpt-validationguard-component-pointer-builder`
  - [ ] `p2` - `cpt-validationguard-component-validation-builder`

- **API**:
  - N/A — class library contract with no HTTP surface (per `cpt-validationguard-dod-validation-builder-implement-core-api` Touches: API `N/A`).

- **Sequences**:

  - [ ] `p2` - `cpt-validationguard-seq-validation-failure-response` (this feature performs the entry-registration and nested-builder segments of the sequence; shared across all three features, see Overview)

- **Data**:

  - N/A — this library has no persistent storage requirement (`cpt-validationguard-db-none`, DESIGN.md Section 3.7).

### 2.2 [Problem Details Mapping](features/problem-details-mapping.md) - MEDIUM

- [ ] `p2` - **ID**: `cpt-validationguard-feature-problem-details-mapping`

- **Purpose**: Defines a standards-based error format so API clients get one predictable shape, rather than a shape invented by each service that uses the library.

- **Depends On**: `cpt-validationguard-feature-validation-builder`

- **Scope** (a feature-level summary; the normative wording lives in the PRD requirements listed under Requirements Covered below):
  - Build the Problem Details base fields (`type`, `title`, `status`, `detail`, `instance`) from finalized entries and response metadata.
  - Project stored near-RFC entries into the `errors[]` extension array, copying `pointer`, `code`, `format`, and `detail` without field reshaping.
  - Preserve the deterministic entry ordering that the root ValidationBuilder already established.
  - Check incoming entries against the stable entry contract, and reject those that do not satisfy it before mapping begins.

- **Out of scope**:
  - Accumulating validation entries or composing pointers (owned by the Validation Builder feature).
  - Writing the HTTP response, setting response content type, or ASP.NET Core service registration (owned by the ASP.NET Core Adapter feature).

- **Requirements Covered**:

  - [ ] `p1` - `cpt-validationguard-fr-rfc9457-compatible-output` (payload construction)
  - [ ] `p1` - `cpt-validationguard-fr-errors-extension-projection`
  - [ ] `p3` - `cpt-validationguard-fr-deterministic-output-ordering`
  - [ ] `p2` - `cpt-validationguard-nfr-serialization-performance`
  - [ ] `p2` - `cpt-validationguard-nfr-projection-efficiency`
  - [ ] `p1` - `cpt-validationguard-nfr-api-stability`
  - [ ] `p1` - `cpt-validationguard-interface-problem-details-mapper-contract`
  - [ ] `p2` - `cpt-validationguard-contract-errors-extension-consumer`

- **Design Principles Covered**:

  - [ ] `p2` - `cpt-validationguard-principle-rfc-first-envelope`
  - [ ] `p2` - `cpt-validationguard-principle-stable-error-entry-contract`

- **Design Constraints Covered**:

  - [ ] `p2` - `cpt-validationguard-constraint-strict-net10-target`

- **Domain Model Entities**:
  - ProblemDetailsProjection
  - ValidationEntry (consumed as input; owned by the Validation Builder feature)

- **Design Components**:

  - [ ] `p2` - `cpt-validationguard-component-problem-details-mapper`

- **API**:
  - N/A — class library mapping contract with no HTTP surface (per `cpt-validationguard-dod-problem-details-mapping-implement-mapper` Touches: API `N/A`).

- **Sequences**:

  - [ ] `p2` - `cpt-validationguard-seq-validation-failure-response` (this feature performs the mapping segment of the sequence; shared across all three features, see Overview)

- **Data**:

  - N/A — this library has no persistent storage requirement (`cpt-validationguard-db-none`, DESIGN.md Section 3.7).

### 2.3 [ASP.NET Core Adapter](features/aspnetcore-adapter.md) - LOW

- [ ] `p3` - **ID**: `cpt-validationguard-feature-aspnetcore-adapter`

- **Purpose**: Defines pipeline-native delivery of ValidationGuard responses for ASP.NET Core applications, instead of each host writing its own integration code.

- **Depends On**: `cpt-validationguard-feature-validation-builder`, `cpt-validationguard-feature-problem-details-mapping`

- **Scope** (a feature-level summary; the normative wording lives in the PRD requirements listed under Requirements Covered below):
  - Register adapter services, including Problem Details integration hooks and the mapper abstraction, in the ASP.NET Core service container.
  - Bind adapter options for default status, title, and type values.
  - Invoke the Problem Details mapper to build the RFC 9457 payload from finalized validation entries obtained from the request context.
  - Serialize the mapped payload, set the `application/problem+json` content type, and write the HTTP validation response.

- **Out of scope**:
  - Core validation entry accumulation, nested builders, and pointer composition (owned by the Validation Builder feature).
  - Problem Details envelope construction and `errors` projection logic (owned by the Problem Details Mapping feature).

- **Requirements Covered**:

  - [ ] `p1` - `cpt-validationguard-contract-aspnetcore-adapter-package`
  - [ ] `p2` - `cpt-validationguard-contract-aspnetcore-problemdetails-integration`
  - [ ] `p1` - `cpt-validationguard-fr-rfc9457-compatible-output` (response transport and content-type compliance)
  - [ ] `p1` - `cpt-validationguard-nfr-api-stability`

- **Design Principles Covered**:

  - [ ] `p2` - `cpt-validationguard-principle-rfc-first-envelope`

- **Design Constraints Covered**:

  - [ ] `p2` - `cpt-validationguard-constraint-strict-net10-target`

- **Domain Model Entities**:
  - ProblemDetailsProjection (consumed as the payload to serialize; owned by the Problem Details Mapping feature)
  - ValidationEntry (consumed as input read from the request context; owned by the Validation Builder feature)

- **Design Components**:

  - [ ] `p2` - `cpt-validationguard-component-aspnetcore-adapter`

- **API**:
  - `application/problem+json` response integration surface (per `cpt-validationguard-dod-aspnetcore-adapter-implement-package` Touches: API).

- **Sequences**:

  - [ ] `p2` - `cpt-validationguard-seq-validation-failure-response` (this feature performs the response-emission segment of the sequence; shared across all three features, see Overview)

- **Data**:

  - N/A — this library has no persistent storage requirement (`cpt-validationguard-db-none`, DESIGN.md Section 3.7).

---

## 3. Feature Dependencies

```text
cpt-validationguard-feature-validation-builder
    │
    ├──→ cpt-validationguard-feature-problem-details-mapping
    │                     │
    │                     ↓
    └───────────────────→ cpt-validationguard-feature-aspnetcore-adapter
```

**Dependency Rationale**:

- `cpt-validationguard-feature-problem-details-mapping` requires `cpt-validationguard-feature-validation-builder`: the mapper's input is the finalized near-RFC entries that only the root ValidationBuilder produces via `ToValidationEntries()` (problem-details-mapping.md Section 1.4, `Dependencies`).
- `cpt-validationguard-feature-aspnetcore-adapter` requires `cpt-validationguard-feature-validation-builder`: request handling in the adapter obtains finalized validation entries generated by the core builder (aspnetcore-adapter.md, step `inst-generate-validation-entries`).
- `cpt-validationguard-feature-aspnetcore-adapter` requires `cpt-validationguard-feature-problem-details-mapping`: the adapter delegates payload construction to the mapper before writing the HTTP response (aspnetcore-adapter.md Section 1.4, `Dependencies`; DESIGN.md Section 3.2, ASP.NET Core Adapter "Related components").
- `cpt-validationguard-feature-problem-details-mapping` has no dependency on `cpt-validationguard-feature-aspnetcore-adapter`, so the mapping feature can be implemented and tested independently of any HTTP hosting concerns.
