# Deterministic Simulation Specification #16 — Adversarial Review & Critique

Date: 2026-05-01  
Reviewer mode: adversarial / implementation-risk focused  
Validation pass: against `outline.md` expanded draft

## Executive Summary
The expanded outline is a meaningful improvement over the prior terse version, but it is still **not implementation-safe**. It names the right domains, yet leaves many high-risk determinism decisions unbound. If teams implement directly from this outline without further hardening, divergent behavior across subsystems and platforms is likely.

Top risks:
1. Missing canonical tick-order tie-break semantics.
2. RNG contract is incomplete for branch-dependent draw count normalization.
3. Snapshot/replay requirements omit byte-level canonicalization details.
4. Divergence detection lacks a frozen hash schema and field-order contract.
5. Cross-platform certification criteria are too generic to enforce objectively.

## Validation Matrix (Conclusions + Required Fixes)

| Finding | Severity | Validation | Evidence from outline | Required fix |
|---|---|---|---|---|
| H-1 Tick order is phase-level only; intra-phase ordering unspecified | High | Validated | Section 1 defines phase order but not deterministic traversal/merge rules at sufficient granularity | Add canonical ordering contract per entity/event/job merge path and stable sort keys |
| H-2 RNG stream ownership and consumption model can still drift under branching | High | Validated | RNG section mentions forbidden branch-dependent draw count but gives no normalization algorithm | Define draw reservation/skip strategy with pseudocode and mandatory counters |
| H-3 Snapshot schema lacks canonical binary layout guarantees | High | Validated | Snapshot section mentions schema/versioning/endianness but not explicit byte encoding rules | Freeze field order, width, endian, padding policy, NaN/Inf handling, and schema checksum |
| H-4 Replay reconstruction algorithm boundaries are ambiguous | High | Validated | Warm-start and delta policy named, but resume boundaries and deterministic rehydration rules are absent | Define exact replay boot sequence, lifecycle hooks, and prohibited side effects during load |
| H-5 Divergence tooling lacks normative digest spec | High | Validated | State hashing is listed, but no required algorithm or byte canonicalization is bound | Add mandatory hash algorithm/versioning, digest scopes, and collision handling workflow |
| M-1 Determinism tiers (A/B/C) are useful but approval thresholds are undefined | Medium | Validated | Tier definitions exist without pass/fail mapping by subsystem | Map each authoritative subsystem to required tier and disallow Tier B without explicit tolerance table |
| M-2 Save/load equivalence criteria are test-idea level only | Medium | Validated | Randomized tick checks are referenced but no sample sizes/protocol | Bind minimum sample counts, scenario set, and statistical pass criteria |
| M-3 Instrumentation section lacks performance budgets and retention policy | Medium | Validated | Trace channels listed; no overhead limits | Add max CPU/storage overhead budgets and CI artifact retention contract |
| M-4 Regression governance missing baseline update policy details | Medium | Validated | Golden updates require approval but no review rubric | Define who approves, what evidence is required, and rollback expectations |
| L-1 Requirement taxonomy present but no IDs instantiated | Low | Validated | FR/NFR/VR/OPS prefixes proposed only | Seed initial requirement ID ledger with placeholders and owner fields |

## Cross-Section Contradictions / Ambiguity Risks
1. **Tier B tolerance vs cross-platform certification:** Section 4 allows epsilon tolerances while Section 8 implies parity claims. Need explicit rules for when tolerance-based parity is acceptable.
2. **Snapshot deltas vs load atomicity:** Section 3 names delta policy, Section 5 requires save/load equivalence. Without atomic snapshot chain guarantees, replay can become history-dependent.
3. **Job-system determinism vs performance claims:** Section 1 references deterministic scheduling barriers, but no budget guidance exists to avoid teams silently relaxing ordering guarantees.

## “Must Add Before Section Authoring Starts” Checklist
- [ ] Canonical tick pseudocode with deterministic intra-phase iteration and merge ordering.
- [ ] RNG consumption normalization algorithm (including branch-safe draw accounting).
- [ ] Snapshot binary contract (field table + byte layout + encoding examples).
- [ ] Replay reconstruction state machine with deterministic lifecycle hooks.
- [ ] Divergence digest spec (algorithm, scope, canonical byte serialization).
- [ ] Determinism tolerance matrix template with required per-field classifications.
- [ ] Certification matrix with objective pass thresholds and required scenario corpus.
- [ ] Golden-trace governance policy (approval authority, evidence, and rollback process).

## Recommended Remediation Plan

### Phase A — Normative Core Freezing
1. Freeze tick-order and RNG contracts first (Sections 1–2) with pseudocode and invariants.
2. Freeze snapshot/replay canonicalization (Sections 3 & 5) at byte-level detail.
3. Freeze divergence digest protocol and tolerance matrix (Section 4).

### Phase B — Verification Hardening
4. Define regression suite minimum corpus and seed schedules (Section 7).
5. Add cross-platform certification rubric with explicit acceptable drift policy (Section 8).
6. Define CI artifact requirements and automated desync triage outputs (Section 6).

### Phase C — Governance and Approval
7. Instantiate requirement IDs and traceability map.
8. Bind Section 9 checklist to objective gate criteria (not prose assertions).

## Overall Verdict
**Status: NOT READY FOR IMPLEMENTATION OR APPROVAL.**  
The outline is strong structurally but still too abstract for independent teams to produce guaranteed-compatible implementations.
