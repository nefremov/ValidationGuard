# PRD — ValidationGuard Typed Validation Error Accumulator

<!-- toc -->

- [1. Overview](#1-overview)
  - [1.1 Purpose](#11-purpose)
  - [1.2 Background / Problem Statement](#12-background--problem-statement)
  - [1.3 Goals (Business Outcomes)](#13-goals-business-outcomes)
  - [1.4 Glossary](#14-glossary)
- [2. Actors](#2-actors)
  - [2.1 Human Actors](#21-human-actors)
  - [2.2 System Actors](#22-system-actors)
- [3. Operational Concept & Environment](#3-operational-concept--environment)
  - [3.1 Module-Specific Environment Constraints](#31-module-specific-environment-constraints)
- [4. Scope](#4-scope)
  - [4.1 In Scope](#41-in-scope)
  - [4.2 Out of Scope](#42-out-of-scope)
- [5. Functional Requirements](#5-functional-requirements)
  - [5.1 Validation Error Accumulation](#51-validation-error-accumulation)
  - [5.2 Problem Details Output](#52-problem-details-output)
- [6. Non-Functional Requirements](#6-non-functional-requirements)
  - [6.1 NFR Inclusions](#61-nfr-inclusions)
  - [6.2 NFR Exclusions](#62-nfr-exclusions)
- [7. Public Library Interfaces](#7-public-library-interfaces)
  - [7.1 Public API Surface](#71-public-api-surface)
  - [7.2 External Integration Contracts](#72-external-integration-contracts)
- [8. Use Cases](#8-use-cases)
- [9. Acceptance Criteria](#9-acceptance-criteria)
- [10. Dependencies](#10-dependencies)
- [11. Assumptions](#11-assumptions)
- [12. Risks](#12-risks)

<!-- /toc -->

## 1. Overview

### 1.1 Purpose

This module defines a .NET 10 library that accumulates typed validation errors across object graphs and serializes validation outcomes as RFC 9457 Problem Details with an `errors` extension.

The module enables consumers (web APIs and internal services) to return consistent, machine-readable validation responses with precise field paths, including nested object properties and array elements.

The canonical internal representation in the core library is a near-RFC entry shape (`pointer`, `code`, `format`, `detail`) that is directly projectable to the RFC 9457 Problem Details `errors` extension.

### 1.2 Background / Problem Statement

Validation in .NET applications is often fragmented across attributes, custom validators, and ad-hoc error payloads. Teams typically lose consistency in error code typing, path addressing, and response format, especially when validating nested collections and complex DTOs.

Without a standard accumulator and payload model, clients receive inconsistent error structures, making parsing, UX mapping, and automation difficult. A unified library is needed to centralize typed error collection and standardize output as Problem Details with an `errors` extension.

To minimize drift and transformation complexity, internal storage must remain serialization-ready for RFC 9457-compatible output.

### 1.3 Goals (Business Outcomes)

- Deliver a reusable validation library that standardizes error payloads across services by v1.0.
- Reduce custom validation-response mapping logic in consuming APIs by at least 60% within one release cycle after adoption.
- Ensure 100% of validation failures include deterministic location paths (root, nested, and array-indexed) in output payloads.

### 1.4 Glossary

| Term | Definition |
|------|------------|
| Typed validation error | Validation failure that includes a code identifier, a message format string, rendered detail text, pointer, and metadata |
| Problem Details | Standardized HTTP error response format defined by RFC 9457 |
| `errors` extension | Problem Details extension member containing validation failures as an array of error entries |
| Validation path | RFC 6901 JSON Pointer address to the invalid value in an object graph (e.g., `/address/street`, `/items/0/name`) |

## 2. Actors

> **Note**: Stakeholder needs are managed at project/task level by steering committee. Document **actors** (users, systems) that interact with this module.

### 2.1 Human Actors

#### API Developer

**ID**: `cpt-validationguard-actor-api-developer`

**Role**: Integrates the library into API request validation flows.
**Needs**: Deterministic error structures, low-friction API usage, and stable contracts for clients.

#### Client Application Developer

**ID**: `cpt-validationguard-actor-client-developer`

**Role**: Consumes validation responses from services that use the library.
**Needs**: Predictable path and code formats for UI field binding and localized messaging.

### 2.2 System Actors

#### ASP.NET Core API Host

**ID**: `cpt-validationguard-actor-aspnet-core-host`

**Role**: Produces HTTP responses and serializes Problem Details payloads.

#### JSON Serializer

**ID**: `cpt-validationguard-actor-json-serializer`

**Role**: Serializes Problem Details plus extension members into response JSON.

## 3. Operational Concept & Environment

### 3.1 Module-Specific Environment Constraints

- Target runtime is strictly `net10.0`.
- Payloads must be compatible with RFC 9457 Problem Details semantics.
- Integration assumes JSON-based HTTP APIs.

## 4. Scope

### 4.1 In Scope

- Typed accumulation of validation errors from root, nested object, and array/list contexts.
- Typed accumulation of validation errors from root, nested object, array/list, and indexer/keyed contexts.
- Support for deterministic JSON Pointer path capture for nested members and indexed elements.
- Support for RFC 6901 token escaping and numeric array-index token validation during pointer capture.
- Canonical internal storage of near-RFC validation entries (`pointer`, `code`, `format`, `detail`) in `ValidationBuilder`, ready for direct projection to Problem Details `errors`.
- Mapping accumulated errors into RFC 9457 Problem Details with `errors` extension.
- Public library contracts for consumers to add and merge validation outcomes, and to finalize output from the root builder.
- Separate ASP.NET Core adapter library for HTTP integration on top of the core library.

### 4.2 Out of Scope

- Business rule authoring DSL or full validation-rule engine.
- Localization/content management for translated error messages.
- UI rendering logic for displaying validation errors.
- Non-JSON transport payload formats.

## 5. Functional Requirements

> **Testing strategy**: All requirements verified via automated tests (unit, integration, e2e) targeting 90%+ code coverage unless otherwise specified. Document verification method only for non-test approaches (analysis, inspection, demonstration).

### 5.1 Validation Error Accumulation

#### Typed Error Registration

- [ ] `p1` - **ID**: `cpt-validationguard-fr-typed-error-registration`

The system **MUST** allow consumers to register validation failures with a `code`, a `format` field containing the message format string classifier, a `detail` field containing the rendered error text, and a `pointer` field containing the RFC 6901 JSON Pointer path reference.

**Rationale**: Ensures compile-time-safe error coding and consistent downstream interpretation.

**Actors**: `cpt-validationguard-actor-api-developer`

#### Canonical Internal Entry Storage

- [ ] `p1` - **ID**: `cpt-validationguard-fr-canonical-internal-entry-storage`

The system **MUST** store accumulated validation failures in an internal canonical entry shape containing `pointer`, `code`, `format`, and `detail`, aligned with the target Problem Details `errors` entry schema.

**Rationale**: Keeps core accumulation and HTTP projection aligned and reduces mapper complexity.

**Actors**: `cpt-validationguard-actor-api-developer`, `cpt-validationguard-actor-aspnet-core-host`

#### Nested Object Path Handling

- [ ] `p1` - **ID**: `cpt-validationguard-fr-nested-object-path-handling`

The system **MUST** support capturing and preserving RFC 6901 JSON Pointer paths for nested object members.

**Rationale**: Required for precise field-level feedback on complex request payloads.

**Actors**: `cpt-validationguard-actor-api-developer`, `cpt-validationguard-actor-client-developer`

#### Array Element Path Handling

- [ ] `p1` - **ID**: `cpt-validationguard-fr-array-element-path-handling`

The system **MUST** support capturing and preserving RFC 6901 JSON Pointer paths for indexed collection elements.

**Rationale**: Collection validation is common and must remain machine-addressable.

**Actors**: `cpt-validationguard-actor-api-developer`, `cpt-validationguard-actor-client-developer`

#### Indexer and Keyed Path Handling

- [ ] `p2` - **ID**: `cpt-validationguard-fr-indexer-keyed-path-handling`

The system **MUST** resolve accessor-based indexers (for example `x => x.Metadata["key"]`, `x => x.Bag["region"]`, `x => x.Numbered[3]`) into RFC 6901 JSON Pointer segments, apply RFC 6901 token escaping for keyed segments, and reject invalid numeric array-index tokens.

**Rationale**: Typed accessor-based scoped validation must support dictionary/custom indexer use cases while preserving RFC-compatible pointer correctness.

**Actors**: `cpt-validationguard-actor-api-developer`, `cpt-validationguard-actor-client-developer`

#### Configurable Pointer Fragment Naming

- [ ] `p2` - **ID**: `cpt-validationguard-fr-configurable-pointer-naming`

The system **MUST** allow consumers to select the naming convention applied to member-name segments of emitted RFC 6901 JSON Pointer paths (for example unchanged member names, `camelCase`, `snake_case`, or `kebab-case`), and **MUST** default to leaving member names unchanged.

Selecting a naming convention **MUST NOT** change the structure of a pointer path and **MUST NOT** affect the fidelity of `code`, `format`, and `detail`.

**Rationale**: Clients address validation failures against the JSON payload they received, whose field names follow the host serializer naming policy and commonly differ from .NET member naming. Pointer segments that do not match those names are not resolvable by the client.

**Actors**: `cpt-validationguard-actor-api-developer`, `cpt-validationguard-actor-client-developer`, `cpt-validationguard-actor-json-serializer`

#### Aggregation and Merge

- [ ] `p2` - **ID**: `cpt-validationguard-fr-aggregation-and-merge`

The system **MUST** support aggregating validation failures from multiple validators/scopes into one final result while preserving all paths and codes.

**Rationale**: Enables compositional validation for larger object graphs.

**Actors**: `cpt-validationguard-actor-api-developer`

### 5.2 Problem Details Output

#### RFC 9457-Compatible Output

- [ ] `p1` - **ID**: `cpt-validationguard-fr-rfc9457-compatible-output`

The system **MUST** produce validation failure output compatible with RFC 9457 Problem Details structure.

**Rationale**: Standardized error envelopes improve interoperability across clients and services.

**Actors**: `cpt-validationguard-actor-aspnet-core-host`, `cpt-validationguard-actor-client-developer`

#### Errors Extension Projection

- [ ] `p1` - **ID**: `cpt-validationguard-fr-errors-extension-projection`

The system **MUST** place validation failures into a Problem Details `errors` extension as an array of entries where each entry includes `pointer` (JSON Pointer path), `code`, `format` (message format string classifier), and `detail` (rendered error text).

Projection from internal canonical entries to the `errors` extension **MUST** be lossless for `pointer`, `code`, `format`, and `detail`.

**Rationale**: Clients require stable traversal of validation failures by location and consistent value fidelity.

**Actors**: `cpt-validationguard-actor-aspnet-core-host`, `cpt-validationguard-actor-client-developer`, `cpt-validationguard-actor-json-serializer`

#### Deterministic Output Ordering

- [ ] `p3` - **ID**: `cpt-validationguard-fr-deterministic-output-ordering`

The system **SHOULD** provide deterministic ordering of serialized validation entries in the `errors` array for stable testing and diagnostics.

**Rationale**: Reduces flaky tests and simplifies payload comparisons.

**Actors**: `cpt-validationguard-actor-api-developer`

## 6. Non-Functional Requirements

### 6.1 NFR Inclusions

#### Validation Serialization Performance

- [ ] `p2` - **ID**: `cpt-validationguard-nfr-serialization-performance`

The system **MUST** serialize a validation result containing up to 200 failures into Problem Details JSON (including `errors` array entries) within 25 ms at p95 on a standard developer workstation profile.

**Threshold**: p95 ≤ 25 ms for 200 failures, measured in automated performance tests.

**Rationale**: Validation error generation should not materially impact API response latency.

#### Memory Efficiency

- [ ] `p2` - **ID**: `cpt-validationguard-nfr-memory-efficiency`

The system **MUST** avoid unbounded memory growth while accumulating errors and keep per-failure overhead bounded and proportional to error count.

**Threshold**: Memory growth must be O(n) with number of failures and no retained transient graph references after export.

**Rationale**: Prevents validation subsystem pressure in high-throughput APIs.

#### Projection Efficiency

- [ ] `p2` - **ID**: `cpt-validationguard-nfr-projection-efficiency`

The system **MUST** project canonical internal validation entries to Problem Details `errors` without duplicating full intermediate entry collections and with linear-time behavior relative to error count.

**Threshold**: O(n) projection time and bounded temporary allocation proportional to output serialization requirements.

**Rationale**: Preserves throughput and memory characteristics while keeping mapper transformation minimal.

#### API Stability

- [ ] `p1` - **ID**: `cpt-validationguard-nfr-api-stability`

The system **MUST** provide semantic versioning guarantees for public interfaces and require major version changes for breaking public API changes.

**Threshold**: All breaking API changes trigger major version increment.

**Rationale**: Consumers depend on contract stability across upgrades.

### 6.2 NFR Exclusions

- **Internationalization** (UX-PRD-003): Not applicable because the module returns developer-supplied messages and does not manage localization catalogs.
- **Offline capability** (UX-PRD-004): Not applicable because this is a server-side library component.
- **Safety engineering controls** (SAFE-PRD-001/002): Not applicable because the module is an information-processing library without direct physical-system control.

## 7. Public Library Interfaces

Define the public API surface, versioning/compatibility guarantees, and integration contracts provided by this library.

### 7.1 Public API Surface

#### ValidationBuilder Contract

- [ ] `p1` - **ID**: `cpt-validationguard-interface-validation-builder-contract`

**Type**: .NET public type contract

**Stability**: stable

**Description**: Allows adding scoped errors, creating nested builders with accessor-based scope addressing, accumulating canonical near-RFC entries (`pointer`, `code`, `format`, `detail`), and producing standardized output from the root builder via `ToValidationEntries()`.

**Scope Addressing**: Property, nested object, and index scopes are specified via accessor expressions (for example `x => x.Property`, `x => x.Nested`, `x => x[5]`) rather than raw path string assembly.

**Breaking Change Policy**: Major version bump required.

#### Problem Details Mapper Contract

- [ ] `p1` - **ID**: `cpt-validationguard-interface-problem-details-mapper-contract`

**Type**: .NET public type contract

**Stability**: stable

**Description**: Converts canonical validation results to RFC 9457 Problem Details with `errors` extension using minimal transformation.

**Breaking Change Policy**: Major version bump required.

### 7.2 External Integration Contracts

#### ASP.NET Core ProblemDetails Integration

- [ ] `p2` - **ID**: `cpt-validationguard-contract-aspnetcore-problemdetails-integration`

**Direction**: required from client

**Protocol/Format**: HTTP `application/problem+json`

**Compatibility**: Backward-compatible extension shape across minor versions.

#### ASP.NET Core Adapter Package

- [ ] `p1` - **ID**: `cpt-validationguard-contract-aspnetcore-adapter-package`

**Direction**: provided by library

**Protocol/Format**: Separate `net10.0` adapter library package integrating with ASP.NET Core ProblemDetails pipeline

**Compatibility**: Core library and adapter version compatibility maintained within the same major version.

#### Errors Extension Consumer Contract

- [ ] `p2` - **ID**: `cpt-validationguard-contract-errors-extension-consumer`

**Direction**: provided by library

**Protocol/Format**: JSON extension array under `errors`, each entry containing `pointer`, `code`, `format`, and `detail`

**Compatibility**: `pointer`, `code`, `format`, and `detail` fields remain stable across minor versions.

## 8. Use Cases

#### Return nested validation errors for request DTO

- [ ] `p2` - **ID**: `cpt-validationguard-usecase-return-nested-validation-errors`

**Actor**: `cpt-validationguard-actor-api-developer`

**Preconditions**:
- API request DTO includes nested objects and collections.
- Validation rules are executed for root and nested nodes.

**Main Flow**:
1. API validates root DTO fields.
2. API validates nested object properties.
3. API validates array/list elements by index.
4. Validation errors are accumulated with typed codes and paths.
5. API maps result to RFC 9457 Problem Details.
6. API returns `application/problem+json` payload with `errors` extension.

**Postconditions**:
- Client receives one Problem Details response with complete validation errors and deterministic path mapping.

**Alternative Flows**:
- **No validation failures**: API returns normal success response and does not emit validation Problem Details.

#### Merge child validator results into parent result

- [ ] `p3` - **ID**: `cpt-validationguard-usecase-merge-child-validator-results`

**Actor**: `cpt-validationguard-actor-api-developer`

**Preconditions**:
- Multiple validators produce partial validation results.

**Main Flow**:
1. Parent validation context invokes child validators.
2. Child results are merged into a parent accumulator.
3. Parent result is finalized from the root builder as one Problem Details payload.

**Postconditions**:
- All failures from child validators are present in final output without path loss.

**Alternative Flows**:
- **Conflicting paths/codes**: Library preserves all entries according to merge policy and does not drop failures silently.

## 9. Acceptance Criteria

- [ ] Library can accumulate typed validation errors for root, nested objects, and array elements using JSON Pointer paths.
- [ ] Problem Details output is RFC 9457-compatible and includes `errors` extension.
- [ ] `errors` extension is an array of entries and preserves deterministic JSON Pointer paths for nested and indexed elements.
- [ ] Each validation entry includes `code`, `format`, `detail`, and `pointer`.
- [ ] Pointer member-name segments can be emitted in a consumer-selected naming convention (`camelCase`, `snake_case`, `kebab-case`), with unchanged member names as the default, without altering pointer structure.
- [ ] Internal accumulation stores canonical near-RFC entries with `pointer`, `code`, `format`, and `detail` in `ValidationBuilder`.
- [ ] Projection to Problem Details `errors` is lossless for canonical fields and does not require path or message recomputation.
- [ ] Solution includes core library and separate ASP.NET Core adapter library, both targeting `net10.0`.
- [ ] Automated tests verify accumulation and serialization behavior with at least one nested-object and one array-index scenario.

## 10. Dependencies

| Dependency | Description | Criticality |
|------------|-------------|-------------|
| .NET 10 runtime and SDK | Runtime and build target for the core and adapter libraries (`net10.0`) | p1 |
| RFC 9457-compatible ProblemDetails implementation | Standard envelope model and serialization behavior | p1 |
| JSON serialization stack | Serializes Problem Details and extensions | p1 |

## 11. Assumptions

- Consuming systems use HTTP APIs where Problem Details is acceptable for validation errors.
- Clients can parse an `errors` extension array and map JSON Pointer paths to UI or domain fields.
- Typed error representation standard is `code`, `format` (message format string classifier), and `detail` (rendered error text), with path stored as `pointer`.
- Internal canonical entry shape remains aligned with Problem Details `errors` entry schema to enable minimal mapping logic.

## 12. Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Inconsistent path notation across consumers | Client-side mapping failures and support overhead | Standardize on RFC 6901 JSON Pointer and publish examples/tests |
| Overly strict public API too early | High cost to evolve library before adoption stabilizes | Mark unstable areas before v1 and stabilize only validated contracts |
| Ambiguity in `errors` extension schema | Integration mismatches across client teams | Publish explicit extension schema and compatibility policy |
| Drift between internal entry model and RFC 9457 `errors` schema | Lossy mapping, duplicated transformation logic, and serialization defects | Keep canonical near-RFC internal schema (`pointer`, `code`, `format`, `detail`) and enforce projection fidelity via tests |
