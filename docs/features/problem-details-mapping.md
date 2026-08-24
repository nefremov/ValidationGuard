---
version: 1.0.0
status: accepted
updated: 2026-08-06
---

# Feature: Problem Details Mapping


<!-- toc -->

- [1. Feature Context](#1-feature-context)
  - [1.1 Overview](#11-overview)
  - [1.2 Purpose](#12-purpose)
  - [1.3 Actors](#13-actors)
  - [1.4 References](#14-references)
- [2. Actor Flows (CDSL)](#2-actor-flows-cdsl)
  - [Project Validation Failures to Problem Details](#project-validation-failures-to-problem-details)
- [3. Processes / Business Logic (CDSL)](#3-processes--business-logic-cdsl)
  - [Build RFC 9457 Problem Details Envelope](#build-rfc-9457-problem-details-envelope)
  - [Validate Mapping Input Contract](#validate-mapping-input-contract)
- [4. States (CDSL)](#4-states-cdsl)
  - [Problem Details Projection State Machine](#problem-details-projection-state-machine)
- [5. Definitions of Done](#5-definitions-of-done)
  - [Implement Problem Details Mapper](#implement-problem-details-mapper)
- [6. Acceptance Criteria](#6-acceptance-criteria)
- [7. Changelog](#7-changelog)

<!-- /toc -->

- [ ] `p1` - **ID**: `cpt-validationguard-featstatus-problem-details-mapping`
## 1. Feature Context

- [ ] `p2` - `cpt-validationguard-feature-problem-details-mapping`

### 1.1 Overview

Builds RFC 9457 Problem Details envelope from near-RFC validation entries and projects them into the `errors` extension array with minimal transformation.

### 1.2 Purpose

Defines deterministic RFC 9457 payload envelope behavior over near-RFC entries and the final error-entry schema for consuming clients.

**Requirements**: `cpt-validationguard-fr-rfc9457-compatible-output`, `cpt-validationguard-fr-errors-extension-projection`, `cpt-validationguard-fr-deterministic-output-ordering`, `cpt-validationguard-nfr-serialization-performance`, `cpt-validationguard-nfr-projection-efficiency`, `cpt-validationguard-nfr-api-stability`

**Principles**: `cpt-validationguard-principle-rfc-first-envelope`, `cpt-validationguard-principle-stable-error-entry-contract`

### 1.3 Actors

| Actor | Role in Feature |
|-------|-----------------|
| `cpt-validationguard-actor-api-developer` | Invokes mapping to produce standardized Problem Details output |
| `cpt-validationguard-actor-client-developer` | Consumes stable `errors` entries in RFC 9457 responses |
| `cpt-validationguard-actor-json-serializer` | Serializes mapped payload including extension members |

### 1.4 References

- **PRD**: [PRD.md](../PRD.md)
- **Design**: [DESIGN.md](../DESIGN.md)
- **Dependencies**: `cpt-validationguard-feature-validation-builder`

## 2. Actor Flows (CDSL)

**Use cases**: `cpt-validationguard-usecase-return-nested-validation-errors`

### Project Validation Failures to Problem Details

- [ ] `p1` - **ID**: `cpt-validationguard-flow-problem-details-mapping-project-errors`

**Actor**: `cpt-validationguard-actor-api-developer`

**Success Scenarios**:
- Caller receives a valid RFC 9457 Problem Details object with `errors` extension array.

**Error Scenarios**:
- Invalid entry field values cause mapping rejection before serialization.

**Steps**:
1. [ ] - `p1` - Caller submits finalized near-RFC validation entries to mapper - `inst-submit-finalized-entries`
2. [ ] - `p1` - Mapper creates Problem Details base fields (`type`, `title`, `status`, `detail`, `instance`) - `inst-create-problem-details-base`
3. [ ] - `p1` - Mapper projects stored near-RFC entries into `errors[]` without field reshaping - `inst-project-errors-array`
4. [ ] - `p1` - Mapper preserves deterministic ordering provided by finalized canonical entries for `errors[]` - `inst-preserve-deterministic-order`
5. [ ] - `p1` - Mapper attaches `errors` extension to Problem Details - `inst-attach-errors-extension`
6. [ ] - `p1` - **RETURN** RFC 9457-compatible object for serialization - `inst-return-problem-details`

## 3. Processes / Business Logic (CDSL)

### Build RFC 9457 Problem Details Envelope

- [ ] `p2` - **ID**: `cpt-validationguard-algo-problem-details-mapping-build-envelope`

**Input**: Finalized near-RFC validation entries and response metadata.

**Output**: Problem Details object with `errors` extension.

**Steps**:
1. [ ] - `p1` - Initialize Problem Details with supplied metadata and defaults - `inst-init-envelope`
2. [ ] - `p1` - **FOR EACH** near-RFC entry in finalized set - `inst-loop-entries`
   1. [ ] - `p1` - Copy `pointer`, `code`, `format`, `detail` to extension entry object with minimal transformation - `inst-copy-entry-fields`
3. [ ] - `p1` - Preserve finalized deterministic entry order during extension projection - `inst-preserve-finalized-order`
4. [ ] - `p1` - Add extension array under key `errors` - `inst-set-errors-extension`
5. [ ] - `p1` - **RETURN** mapped Problem Details object - `inst-return-envelope`

### Validate Mapping Input Contract

- [ ] `p2` - **ID**: `cpt-validationguard-algo-problem-details-mapping-validate-input-contract`

**Input**: Finalized validation entry set.

**Output**: Validated entry set or failure.

**Steps**:
1. [ ] - `p1` - **IF** entry pointer is missing **RETURN** contract error - `inst-reject-missing-pointer`
2. [ ] - `p1` - **IF** entry code is missing **RETURN** contract error - `inst-reject-missing-code`
3. [ ] - `p1` - **IF** format classifier is missing **RETURN** contract error - `inst-reject-missing-format`
4. [ ] - `p1` - **IF** detail text is missing **RETURN** contract error - `inst-reject-missing-detail`
5. [ ] - `p1` - **RETURN** validated entries - `inst-return-validated-entries`

## 4. States (CDSL)

### Problem Details Projection State Machine

- [ ] `p2` - **ID**: `cpt-validationguard-state-problem-details-mapping-projection`

**States**: Unmapped, Mapped, Serialized

**Initial State**: Unmapped

**Transitions**:
1. [ ] - `p1` - **FROM** Unmapped **TO** Mapped **WHEN** mapper builds RFC 9457 object and `errors` extension - `inst-state-mapped`
2. [ ] - `p1` - **FROM** Mapped **TO** Serialized **WHEN** JSON serializer writes response payload - `inst-state-serialized`

## 5. Definitions of Done

### Implement Problem Details Mapper

- [ ] `p1` - **ID**: `cpt-validationguard-dod-problem-details-mapping-implement-mapper`

The system **MUST** map finalized canonical near-RFC validation entries to RFC 9457 Problem Details with deterministic `errors[]` extension entries using fields `pointer`, `code`, `format`, and `detail`, preserving field values losslessly and without semantic reshaping.

**Implements**:
- `cpt-validationguard-flow-problem-details-mapping-project-errors`
- `cpt-validationguard-algo-problem-details-mapping-build-envelope`
- `cpt-validationguard-algo-problem-details-mapping-validate-input-contract`

**Constraints**: `cpt-validationguard-constraint-strict-net10-target`

**Touches**:
- API: `N/A` (class library mapping contract)
- DB: `N/A`
- Entities: `ProblemDetailsMapper`, `ValidationEntry`

## 6. Acceptance Criteria

- [ ] Mapper output is RFC 9457-compatible and includes `errors` extension.
- [ ] `errors` is an array with deterministic ordering and required fields (`pointer`, `code`, `format`, `detail`).
- [ ] Mapping rejects invalid contracts and does not emit malformed entries.
- [ ] Serialization performance target for 200 entries remains compliant with PRD NFR.

## 7. Changelog

| Version | Date | Change |
|---------|------|--------|
| 1.0.0 | 2026-08-06 | Initial FEATURE. |
