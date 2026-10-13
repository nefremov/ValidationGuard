---
version: 1.1.0
status: accepted
updated: 2026-08-24
---

# Feature: Validation Builder


<!-- toc -->

- [1. Feature Context](#1-feature-context)
  - [1.1 Overview](#11-overview)
  - [1.2 Purpose](#12-purpose)
  - [1.3 Actors](#13-actors)
  - [1.4 References](#14-references)
- [2. Actor Flows (CDSL)](#2-actor-flows-cdsl)
  - [Build Validation Errors with Nested Scopes](#build-validation-errors-with-nested-scopes)
- [3. Processes / Business Logic (CDSL)](#3-processes--business-logic-cdsl)
  - [Compose Pointer and Register Entry](#compose-pointer-and-register-entry)
  - [Create and Manage Nested Builders](#create-and-manage-nested-builders)
  - [Convert Pointer Fragment Names](#convert-pointer-fragment-names)
- [4. States (CDSL)](#4-states-cdsl)
  - [Validation Builder Nested Scope State Machine](#validation-builder-nested-scope-state-machine)
- [5. Definitions of Done](#5-definitions-of-done)
  - [Implement ValidationBuilder Core API](#implement-validationbuilder-core-api)
- [6. Acceptance Criteria](#6-acceptance-criteria)
- [7. Changelog](#7-changelog)

<!-- /toc -->

- [ ] `p1` - **ID**: `cpt-validationguard-featstatus-validation-builder`
## 1. Feature Context

- [ ] `p1` - `cpt-validationguard-feature-validation-builder`

### 1.1 Overview

Provides the core builder API for accumulating near-RFC validation failure entries (`pointer`, `code`, `format`, `detail`) with RFC 6901 pointer composition across root, nested object, and array scopes using nested builders.

### 1.2 Purpose

Defines the implementable behavior for nested-builder accumulation semantics in the core library, where child builders capture pointer prefixes and keep scoped entries that are flattened by the root builder.

**Requirements**: `cpt-validationguard-fr-typed-error-registration`, `cpt-validationguard-fr-canonical-internal-entry-storage`, `cpt-validationguard-fr-nested-object-path-handling`, `cpt-validationguard-fr-array-element-path-handling`, `cpt-validationguard-fr-indexer-keyed-path-handling`, `cpt-validationguard-fr-configurable-pointer-naming`, `cpt-validationguard-fr-aggregation-and-merge`, `cpt-validationguard-nfr-memory-efficiency`, `cpt-validationguard-nfr-api-stability`

**Principles**: `cpt-validationguard-principle-stable-error-entry-contract`

### 1.3 Actors

| Actor | Role in Feature |
|-------|-----------------|
| `cpt-validationguard-actor-api-developer` | Uses ValidationBuilder to add errors and manage nested scopes while validating DTOs |
| `cpt-validationguard-actor-client-developer` | Relies on deterministic pointer paths produced by builder behavior |

### 1.4 References

- **PRD**: [PRD.md](../PRD.md)
- **Design**: [DESIGN.md](../DESIGN.md)
- **Dependencies**: None

## 2. Actor Flows (CDSL)

**Use cases**: `cpt-validationguard-usecase-return-nested-validation-errors`, `cpt-validationguard-usecase-merge-child-validator-results`

### Build Validation Errors with Nested Scopes

- [ ] `p1` - **ID**: `cpt-validationguard-flow-validation-builder-build-nested-errors`

**Actor**: `cpt-validationguard-actor-api-developer`

**Success Scenarios**:
- Parent validator records root errors and nested child errors using nested builders that capture scope prefixes.

**Error Scenarios**:
- Nested builder is created with invalid prefix and builder rejects scope creation.

**Steps**:
1. [ ] - `p1` - Validator creates a ValidationBuilder instance for one validation operation - `inst-create-builder`
2. [ ] - `p1` - Validator adds root-level entry with `code`, `format`, `detail`, `pointer` - `inst-add-root-entry`
3. [ ] - `p1` - Validator creates nested builder via `using` lifetime using property accessor (for example `x => x.Nested`) - `inst-create-object-nested-builder`
4. [ ] - `p1` - Validator invokes nested validator with nested builder that records scoped entries within the nested builder node - `inst-call-nested-validator`
5. [ ] - `p1` - Nested validator adds local-pointer entry while remaining path-agnostic - `inst-add-local-pointer-entry`
6. [ ] - `p1` - Validator creates additional nested builders for indexed scopes using accessors (for example `x => x.Items[0]`, `x => x.Items[1]`) - `inst-create-array-nested-builders`
7. [ ] - `p1` - Nested validators add local-pointer entries for each indexed element through their nested builders - `inst-add-array-local-entry`
8. [ ] - `p1` - Validator merges optional child results without dropping entries - `inst-merge-child-results`
9. [ ] - `p1` - Parent builder calls `ToValidationEntries()` to flatten hierarchy and sort deterministic entries - `inst-return-finalized-entries`

## 3. Processes / Business Logic (CDSL)

### Compose Pointer and Register Entry

- [ ] `p2` - **ID**: `cpt-validationguard-algo-validation-builder-compose-pointer-register-entry`

**Input**: Captured nested-builder prefix, local pointer segment, entry fields (`code`, `format`, `detail`).

**Output**: Stored near-RFC validation entry with normalized RFC 6901 pointer.

**Steps**:
1. [ ] - `p1` - Resolve accessor expression to member/index pointer segment and validate expression shape - `inst-resolve-accessor-expression`
2. [ ] - `p1` - Resolve object indexer calls (`get_Item`) for keyed and numeric accessors - `inst-resolve-indexer-get-item`
3. [ ] - `p1` - Apply RFC 6901 escaping to keyed/indexer pointer tokens (for example `~` -> `~0`, `/` -> `~1`) - `inst-escape-indexer-token-rfc6901`
4. [ ] - `p1` - Validate numeric index token format for array/indexed accessors and reject invalid tokens - `inst-validate-array-index-token-rfc6901`
5. [ ] - `p1` - Parse and validate local pointer syntax - `inst-parse-local-pointer`
6. [ ] - `p1` - Combine nested-builder captured prefix with local pointer - `inst-combine-prefix-local-pointer`
7. [ ] - `p1` - Normalize and escape pointer according to RFC 6901 - `inst-normalize-pointer`
8. [ ] - `p1` - Persist near-RFC entry with `pointer`, `code`, `format`, `detail` in builder store - `inst-store-entry`
9. [ ] - `p1` - **RETURN** success - `inst-return-success`

### Create and Manage Nested Builders

- [ ] `p2` - **ID**: `cpt-validationguard-algo-validation-builder-create-manage-nested-builders`

**Input**: Scope pointer prefix from containing validator.

**Output**: Nested builder instance active for the `using` block duration and connected to parent entry store.

**Steps**:
1. [ ] - `p1` - Validate incoming accessor expression (for example `x => x.Property`, `x => x.Nested`, `x => x[5]`, `x => x.Metadata["key"]`) - `inst-validate-scope-accessor`
2. [ ] - `p1` - Resolve accessor expression to RFC 6901 pointer prefix - `inst-resolve-scope-prefix`
3. [ ] - `p1` - Create nested builder that captures prefix and manages scoped entry storage for that node - `inst-create-nested-builder`
4. [ ] - `p1` - **TRY** execute nested validation using nested builder - `inst-run-nested-validation`
5. [ ] - `p1` - **CATCH** nested builder misuse error and mark operation invalid - `inst-catch-nested-misuse`
6. [ ] - `p1` - Dispose nested builder at end of `using` block without mutating sibling builder state - `inst-dispose-nested-builder`
7. [ ] - `p1` - **RETURN** parent builder context remains active for additional parallel scopes - `inst-return-parent-context`

### Convert Pointer Fragment Names

- [x] `p2` - **ID**: `cpt-validationguard-algo-validation-builder-convert-fragment-name`

**Input**: Member name resolved from an accessor expression, and the naming convention selected for the validation operation.

**Output**: Converted member-name segment ready for RFC 6901 pointer composition.

**Steps**:
1. [x] - `p1` - Select the naming convention for the validation operation from the supported set and default to unchanged member names when the consumer selects none - `inst-select-convention`
2. [x] - `p1` - Reject a naming selection that supplies no conversion contract - `inst-reject-missing-converter`
3. [x] - `p1` - Expose a conversion contract that reports converted length and writes the converted member name, and forbid introducing RFC 6901 reserved characters absent from the original value - `inst-declare-conversion-contract`
4. [x] - `p1` - Apply the selected convention casing and separator rules across letter, digit, and acronym boundaries - `inst-apply-convention-casing`

## 4. States (CDSL)

### Validation Builder Nested Scope State Machine

- [ ] `p2` - **ID**: `cpt-validationguard-state-validation-builder-nested-scope-lifecycle`

**States**: Idle, ScopeActive, Finalized

**Initial State**: Idle

**Transitions**:
1. [ ] - `p1` - **FROM** Idle **TO** ScopeActive **WHEN** containing validator creates a nested builder in a `using` block - `inst-state-open-scope`
2. [ ] - `p1` - **FROM** ScopeActive **TO** Idle **WHEN** nested builder is disposed at end of `using` block - `inst-state-close-scope`
3. [ ] - `p1` - **FROM** Idle **TO** Finalized **WHEN** root builder produces finalized entries via `ToValidationEntries()` - `inst-state-finalize`

## 5. Definitions of Done

### Implement ValidationBuilder Core API

- [ ] `p1` - **ID**: `cpt-validationguard-dod-validation-builder-implement-core-api`

The system **MUST** provide ValidationBuilder contracts for entry registration, nested-builder accumulation using `using` lifetime semantics, and deterministic finalized output materialization via `ToValidationEntries()`.

**Implements**:
- `cpt-validationguard-flow-validation-builder-build-nested-errors`
- `cpt-validationguard-algo-validation-builder-compose-pointer-register-entry`
- `cpt-validationguard-algo-validation-builder-create-manage-nested-builders`
- `cpt-validationguard-algo-validation-builder-convert-fragment-name`

**Constraints**: `cpt-validationguard-constraint-strict-net10-target`

**Touches**:
- API: `N/A` (class library contract)
- DB: `N/A`
- Entities: `ValidationBuilder`, `ValidationEntry`

## 6. Acceptance Criteria

- [ ] ValidationBuilder supports nested and array scope composition through nested builders created with `using` lifetime.
- [ ] ValidationBuilder supports scope declaration through accessor expressions (`x => x.Property`, `x => x.Nested`, `x => x[5]`).
- [ ] ValidationBuilder supports object/dictionary indexers and RFC 6901 token escaping when resolving accessor-based scope pointers.
- [ ] ValidationBuilder rejects invalid RFC 6901 numeric index tokens for array/indexed accessor paths.
- [ ] Nested validators remain path-agnostic and produce correct final pointers.
- [ ] Member-name pointer segments are emitted in the naming convention selected for the validation operation, defaulting to unchanged member names.
- [ ] Nested builders keep scoped entries and the root builder flattens all node entries into one consolidated result.
- [ ] Multiple sibling nested builders can be used for independent parallel scopes without state leakage.
- [ ] Finalized entries preserve `pointer`, `code`, `format`, and `detail` for downstream mapping.

## 7. Changelog

| Version | Date | Change |
|---------|------|--------|
| 1.1.0 | 2026-08-24 | Added algo `cpt-validationguard-algo-validation-builder-convert-fragment-name` and wired it into the DoD and acceptance criteria. |
| 1.0.0 | 2026-08-06 | Initial FEATURE. |
