# Deterministic Simulation Specification #16 — Adversarial Review & Critique

Date: 2026-05-01 (initial); 2026-05-02 (second pass + resolution log)
Reviewer mode: adversarial / implementation-risk focused
Validation pass: against `outline.md` expanded draft (2026-05-01); against full §1–§9 + appendices (2026-05-02)

## Executive Summary
The expanded outline is a meaningful improvement over the prior terse version, but it is still **not implementation-safe**. It names the right domains, yet leaves many high-risk determinism decisions unbound. If teams implement directly from this outline without further hardening, divergent behavior across subsystems and platforms is likely.

Top risks:
1. Missing canonical tick-order tie-break semantics.
2. RNG contract is incomplete for branch-dependent draw count normalization.
3. Snapshot/replay requirements omit byte-level canonicalization details.
4. Divergence detection lacks a frozen hash schema and field-order contract.
5. Cross-platform certification criteria are too generic to enforce objectively.

## 2026-05-02 Resolution Log (second-pass adversarial findings)
A second adversarial pass against the full §1–§9 + appendices (not just the outline) surfaced an additional 21 findings. All findings have been addressed by the spec edits dated 2026-05-02 (see version history entries `v0.7` across each section file):

| ID | Finding | Resolution |
|---|---|---|
| A-1 | `SPEC_INDEX.md` said `NOT STARTED` while content existed at v0.5–v0.6 | `SPEC_INDEX.md` updated to `IN PROGRESS`; §9.4 status reconciled to match |
| A-2 | Phantom interfaces declared against `NOT STARTED` consumer specs (#17/#18/#19) | §4.2 reframed as **non-normative sketches**; behavior contract retained as the normative anchor; CLAUDE.md "both sides specified" rule cited |
| B-3 | `actionOrdinal` was simultaneously a `StreamKey` component and a per-evaluation counter (mutually exclusive) | `StreamKey = SipHash-2-4-64(matchSeed, subsystemId, entityId, streamVersion)`; `RngCursor` advances by reservation budget; `actionOrdinal` is a per-stream reservation index in `RngCursor`, not in `StreamKey` |
| B-4 | Snapshot vs save boundary contradiction (`EndOfEvents` listed as legal save boundary before Snapshot phase ran) | New §1.3.0 terminology; `LEGAL_SAVE_BOUNDARIES = { EndOfSnapshot }` only |
| B-5 | "Big-endian byte string as produced by SHA-256" — misleading; SHA-256 has no inherent endianness | §3.2.4 reworded: SHA-256 output is opaque 32-octet string; payload integers are little-endian (`SNAPSHOT_PAYLOAD_ENDIANNESS`) |
| B-6 | Tier A bitwise float equality vs parallel float merges (non-deterministic on a single machine without pinning) | New §1.3.1.1 Stage 0 conditional Tier A; §4.8 extended to *recording-side* environment pinning with full `EnvironmentFingerprint` schema |
| C-7 | Constants split across §3.4 and §3.4.1; two error codes lacked hex IDs | Merged into one tagged catalogue; added `ERR_DS_TIERB_TOLERANCE_MISSING (0x1607)` and `ERR_DS_DIGEST_CHAIN_BREAK (0x1608)`; all constants now carry CLAUDE.md tags |
| C-8 | Outline used `FR-DET-`/`VR-DET-`/`OPS-DET-`; section files used `FR-DS-`/`T-DS-`/`GV-` | New §2.0 Identifier Taxonomy in section-2.md; outline.md migrated to `-DS-` family; CLAUDE.md `XC-`/`FM-`/`EC-`/`ERR-` prefixes acknowledged |
| C-9 | Tick rate ambiguity: 10 Hz tactical vs 60 Hz physics not addressed | §3.1.2 binds physics tick at 60 Hz; AI phase gated to 10 Hz via `AI_PHASE_STRIDE = 6` and `AI_NoOp` no-op phase to preserve digest stream invariants |
| D-11 | Tier B default tolerance silent fallback | New §3.4.2 explicitly forbids fallback; missing tolerance row triggers `ERR_DS_TIERB_TOLERANCE_MISSING` |
| D-12 | `PhaseDigest` preimage missing `Tick`/`PhaseId` | §3.2.2 preimage extended to `(DigestVersion ‖ Tick ‖ PhaseId ‖ phaseScopeFields)`; §5.10 rollup bound to canonical `(tick, phaseOrdinal)` order |
| D-13 | `actionOrdinal` per-evaluation vs per-draw confusion | §3.2.5 fully rewritten: `actionOrdinal` is reservation index (per evaluation, branch-safe); `RngCursor` advances by budget per evaluation |
| D-14 | `ResumeFrom` interface dropped digest validation that the outline lifecycle required | §4.2.2 normative replay lifecycle (8 steps) added with per-step error codes |
| D-15 | Cross-spec audit table relied on specs that are all `NOT STARTED` | §8.3 sequencing constraint stated; rows reclassified as `deferred dependency` |
| D-16 | Stage 0 host platform unnamed | §5.5 pinned to Windows x64, Unity 2022 LTS, IL2CPP; FR-DS-009-GATE split per stage |
| E-17 | Phase-ownership table listed "RNG seed root" prohibited only for `Resolve` | §3.6.1 universal-prohibitions block: seed root, environment fingerprint, snapshot history are immutable for all phases |
| E-19 | `RunTick` described as "pure" while mutating state | §4.2.1 reworded: "deterministic in (state, input, tickNumber); no ambient state observation" |
| E-20 | §7.2 digest upgrade had no trigger criteria | §7.2 expanded with explicit triggers (NIST deprecation / CPU regression / format change) and `DigestVersion`-based coexistence policy |
| E-21 | §9.5 self-graded checklist with the actual gating item unchecked | §9.5 reordered: presence-only checks vs gating implementation-readiness check made explicit; §9.4 status reconciled to `IN PROGRESS` to match `SPEC_INDEX.md` |

After 2026-05-02 edits the spec is **internally consistent and CLAUDE.md-compliant**. It remains in `IN PROGRESS` status; final sign-off remains gated on (a) the §9.5 implementation-readiness review and (b) §8.3 deferred-dependency audit rows resolving once consumer specs (#9 Fixed64, #17 Event System, #18 Performance Optimization, #19 Testing Strategy) reach `IN REVIEW`.

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
