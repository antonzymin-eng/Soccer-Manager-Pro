# Deterministic Simulation Specification #16 — Refined Outline (Post-Adversarial Pass)

> **STATUS: SUPERSEDED — see `section-2.md` §2.1 for canonical FR list.**
> As of May 4, 2026 (Pass 5 H-1), this outline is frozen as historical scaffolding. The section files (v1.0 across §1–§6 and §9) have moved past it: FR IDs from `FR-DS-004` onward, §13 Open Questions, and several §3 / §5 / §7 subsections diverge from the resolved spec. New readers MUST cross-reference the section files; this outline is retained only for traceability of how the spec evolved.

## 0. Document Metadata
- **Spec ID:** 16
- **Title:** Deterministic Simulation
- **Status:** SUPERSEDED (frozen May 4, 2026; canonical content in section files at v1.0)
- **Version:** 0.4 (May 2, 2026) — superseded May 4, 2026 (no further revisions; see Pass 5 H-1 fix in `critique-log.md`)
- **Primary audience:** Gameplay engineering, physics engineering, AI systems, tooling, QA automation, build/release
- **Normative keywords:** MUST, MUST NOT, SHOULD, SHOULD NOT, MAY (text below is non-normative; the canonical normative content is in the section files)

---

## 1. Purpose
Define mandatory deterministic contracts for runtime execution, persistence, replay, and cross-platform validation so independent implementations produce equivalent authoritative outcomes.

### 1.1 Determinism guarantees (normative)
1. **Run-to-run:** Same build hash, initial state, seed bundle, and authoritative inputs MUST produce equivalent state/event outputs.
2. **Replay:** Replaying the authoritative input log from tick 0 or any valid checkpoint tick MUST reproduce the same output stream.
3. **Save/load equivalence:** Load at tick `T` then continue MUST match uninterrupted simulation from tick `T` onward.
4. **Cross-platform certification:** Supported target matrix MUST pass certification corpus under defined parity policy.

### 1.2 Determinism tiers (frozen)
- **Tier A (authoritative hard):** bitwise exact equality required.
- **Tier B (bounded-authoritative):** deterministic epsilon policy allowed only for approved fields in the tolerance matrix.
- **Tier C (non-authoritative):** may vary; MUST NOT feed back into authoritative state.

### 1.3 Tier mapping policy
- World state, gameplay state machines, event ledgers, and RNG counters: **Tier A**.
- Whitelisted numeric fields with approved tolerance rows: **Tier B**.
- VFX/UI/audio-only fields: **Tier C**.

---

## 2. Scope
This spec covers:
- Canonical per-tick order and intra-phase ordering.
- RNG ownership, stream derivation, and branch-safe consumption.
- Snapshot/replay binary contract.
- Divergence detection and digest protocol.
- Save/load atomicity and equivalence validation.
- Regression suite governance and cross-platform certification.

Out of scope:
- Gameplay tuning values.
- Rendering-only randomness that cannot affect authoritative state.

---

## 3. Section 1 — Authoritative Tick Order (Refined)

### 3.1 Canonical tick pipeline
`Input -> Intent -> AI -> Physics -> Resolve -> Events -> Snapshot`

### 3.2 Intra-phase deterministic ordering contract (new)
Every phase MUST define:
1. Primary sort key: `EntityId` ascending.
2. Secondary key: subsystem-specific deterministic ordinal.
3. Tertiary key: insertion sequence captured in deterministic queue index.

Iteration over unordered containers is forbidden in authoritative paths unless entries are copied to a deterministically sorted buffer first.

### 3.3 Job-system merge determinism (new)
- Parallel jobs MAY execute in any order internally, but merge MUST occur at deterministic barriers.
- Merge outputs MUST be reduced in canonical key order.
- Floating-point reductions MUST use deterministic reduction topology (fixed tree shape), or be marked Tier B with explicit tolerance approval.

### 3.4 Required artifacts
- Canonical per-tick pseudocode.
- Phase contract table (`ReadSet`, `WriteSet`, `Owner`, `AllowedSideEffects`).
- Deterministic ordering table for each authoritative collection.

### 3.5 Acceptance checks
- Same input trace => identical phase digest sequence.
- Mutation outside owning phase => validation failure.

---

## 4. Section 2 — RNG Policy (Refined)

### 4.1 RNG ownership
- All authoritative random draws MUST route through `DeterministicRngService`.
- Direct use of `System.Random`, engine RNG, wall-clock time, or hardware entropy is forbidden in authoritative code.

### 4.2 Stream derivation contract
Per-stream key:
`(matchSeed, subsystemId, entityId, actionOrdinal, streamVersion)`

### 4.3 Branch-safe draw normalization (new)
To prevent call-count drift:
- Authoritative code MUST predeclare draw budget per decision site, or
- Use reservation API that advances stream by fixed count regardless of branch.

Required API examples:
- `Reserve(siteId, count)`
- `DrawReserved(siteId, index)`
- `Skip(siteId, count)`

### 4.4 RNG snapshot contract
Snapshot MUST include stream cursor/counter per active stream.

### 4.5 Required artifacts
- RNG pseudocode and fixed vector corpus.
- Draw-site registry with stable IDs.
- Lint rule specification banning non-authoritative RNG usage.

### 4.6 Acceptance checks
- Branch-variant scripts preserve global cursor parity.
- Replay after load reproduces identical draw ledger.

---

## 5. Section 3 — Snapshot Schema & Replay Reconstruction (Refined)

### 5.1 Canonical binary layout (new)
Snapshot schema MUST freeze:
- Field order (lexically by schema table order).
- Field width and signedness.
- Endianness (single canonical endianness).
- Padding/alignment policy (explicitly none unless declared field).
- Enum encoding policy.
- Float special-value handling (NaN/Inf policy) or prohibition.

### 5.2 Snapshot chain integrity (new)
Each snapshot record MUST include:
- `SchemaVersion`
- `Tick`
- `PrevSnapshotDigest`
- `CurrentSnapshotDigest`

### 5.3 Replay reconstruction state machine (new)
Define lifecycle:
1. Load snapshot bytes.
2. Validate schema/digest chain.
3. Rehydrate authoritative state only.
4. Restore RNG cursors.
5. Resume at next legal phase boundary.
6. Reapply authoritative inputs from `T+1`.

### 5.4 Mid-tick save rule
Mid-tick save is forbidden unless normalized to a legal boundary with explicit phase marker.

### 5.5 Required artifacts
- Schema table and binary examples.
- Replay boot sequence pseudocode.
- Compatibility matrix and migration strategy.

### 5.6 Acceptance checks
- Byte-roundtrip idempotence.
- Checkpoint replay equivalence across randomized tick set.

---

## 6. Section 4 — Tolerances & Divergence Detection (Refined)

### 6.1 Mandatory tolerance matrix format
Each field path MUST specify: `Tier`, `Comparator`, `ToleranceValue`, `Rationale`, `Owner`, `ReviewDate`.

### 6.2 Deterministic digest protocol (new)
Freeze:
- Digest algorithm and version.
- Canonical field serialization order.
- Per-phase digest scopes.
- Collision response workflow.

### 6.3 Divergence taxonomy
- **Hard desync:** Tier A mismatch.
- **Soft drift:** Tier B out-of-bound mismatch.
- **Cosmetic divergence:** Tier C mismatch only.

### 6.4 First-divergence localization
Tooling MUST emit:
- First divergent tick.
- First divergent phase.
- Field path(s) and comparator deltas.
- RNG cursor diff if present.

### 6.5 Acceptance checks
- Fault injection triggers expected divergence class.
- Bisect tooling isolates first bad change in CI.

---

## 7. Section 5 — Save/Load Equivalence (Refined)

### 7.1 Atomic save transaction
Save MUST capture all authoritative domains in one atomic transaction or fail with deterministic error code.

### 7.2 Equivalence protocol (new)
Minimum validation protocol MUST define:
- Scenario corpus size.
- Randomized checkpoint count per scenario.
- Required pass percentage.
- Required digest equality scope.

### 7.3 Failure behavior
Corrupt/incompatible saves MUST fail deterministically with explicit error IDs and no partial rehydration.

---

## 8. Section 6 — Instrumentation & Desync Debugging (Refined)

### 8.1 Trace channels
- Tick header
- Phase digest stream
- Event ledger
- RNG draw ledger
- Snapshot digest chain

### 8.2 Performance and storage budgets (new)
Spec MUST set:
- Max CPU overhead per verbosity tier.
- Max memory overhead.
- Max artifact size per match.
- CI retention duration and compression policy.

### 8.3 Triage workflow
`record -> replay -> compare -> locate first divergence -> classify -> route owner`

### 8.4 Required artifacts
- Trace schema.
- CLI command reference.
- Incident bundle format.

---

## 9. Section 7 — Determinism Regression Suite (Refined)

### 9.1 Test architecture
- Unit: RNG, hashing, serialization codecs.
- Integration: subsystem phase determinism.
- Scenario: full-match deterministic traces.
- Soak: long-horizon drift detection.

### 9.2 Golden trace governance (new)
Golden updates MUST include:
- Change reason tag.
- Before/after divergence report.
- Reviewer approvals from owning subsystem + determinism owner.
- Rollback plan if production drift is detected.

### 9.3 CI gating
Any hard desync in protected branches is blocking.

---

## 10. Section 8 — Cross-Platform Certification (Refined)

### 10.1 Certification matrix
Freeze supported: OS, CPU arch, compiler/runtime versions, build flags.

### 10.2 Objective pass criteria (new)
- Tier A fields: exact match required.
- Tier B fields: must pass tolerance rows with zero unauthorized fields.
- Tier C fields: ignored for authoritative parity.

### 10.3 Re-certification triggers
Runtime upgrade, compiler update, numeric-library change, architecture addition, or deterministic core refactor.

### 10.4 Required artifacts
- Signed certification report.
- Exception register with owner + expiry.

---

## 11. Section 9 — Approval Checklist (Refined)
Approval gates MUST be objective and evidence-backed:
- Architecture contracts frozen and linked.
- RNG/vector corpus approved.
- Snapshot schema + digest protocol frozen.
- Regression suite and certification matrix passing.
- Tooling and runbooks operational.
- Open exceptions explicitly approved with expiry.

Outcome:
- Approved / Approved with bounded exceptions / Rejected.

---

## 12. Requirement ID Seed Set (SUPERSEDED — see `section-2.md` §2.1)
> The IDs below were a draft seed set. The canonical FR list is now `section-2.md` §2.1 (`FR-DS-001..013`). The IDs `FR-DS-004` and beyond in this seed set DO NOT match `section-2.md` §2.1 semantics — readers MUST use §2.1 as the source of truth (Pass 5 H-1). The list is retained here only for historical reference.

Historical seed set (do not cite in new work):
- ~~`FR-DS-001`: canonical tick pipeline~~ → see `section-2.md` §2.1 FR-DS-001
- ~~`FR-DS-002`: intra-phase ordering keys~~ → see §2.1 FR-DS-002
- ~~`FR-DS-003`: authoritative RNG ownership~~ → see §2.1 FR-DS-003
- ~~`FR-DS-004`: branch-safe RNG normalization~~ → folded into §2.1 FR-DS-003
- ~~`FR-DS-005`: snapshot canonical binary schema~~ → §2.1 FR-DS-004
- ~~`FR-DS-006`: replay reconstruction state machine~~ → §2.1 FR-DS-005
- ~~`FR-DS-007`: deterministic digest protocol~~ → integrated across §2.1 FR-DS-007/008
- ~~`FR-DS-008`: save/load equivalence protocol~~ → §2.1 FR-DS-006
- ~~`VR-DS-001`/`VR-DS-002`/`VR-DS-003`~~ → tracked as test cards in §5.3 / §5.11 (`T-DS-*`); VR-IDs are reserved but not yet instantiated
- ~~`OPS-DS-001`: golden trace governance workflow~~ → tracked operationally; not yet instantiated as a formal `OPS-DS-NNN` requirement

---

## 13. Open Questions (SUPERSEDED — historical record)
> All four questions below are resolved or superseded as of May 4, 2026 (Pass 5 L-3). New open items are tracked in `critique-log.md` "Outstanding Items" and `CLAUDE.md` "Open Issues".

1. ~~Stage-0 float paths: which fields remain Tier B and for how long?~~ — **Resolved by `section-1.md` §1.3.1.1 (Pass 3 H-C, May 3, 2026):** every parallel-touched float at Stage 0 is Tier B; Tier A is restricted to serial-path fields. Fixed64 migration (Stage 5+) removes the restriction.
2. ~~Fixed64 transition trigger and coexistence plan.~~ — **Deferred to Stage 5+** per CLAUDE.md "Fixed64 stage scope decision" (April 26, 2026). Defined by Spec #9 when that spec reaches IN REVIEW (§8.3 dependency).
3. ~~Snapshot cadence defaults vs storage budgets.~~ — **Partially resolved**: `Snapshot` phase runs every tick (§1.3.0); durable `Save` cadence is scheduled (not every-tick) and bounded by §6.10 budgets; cadence default is owned by Performance Optimization #18 (§8.3 dependency).
4. ~~CI budget for full certification matrix frequency.~~ — **Deferred to Stage 5+** per the FR-DS-009-GATE policy (§5.5); not a Stage 0 gate.

---

## ADVERSARIAL REVIEW — May 6, 2026

> Reviewer: AI agent (claude/review-spec-sections-JQ8jy). Scope: this outline file
> as a historical/SUPERSEDED document. The §0 banner correctly redirects readers
> to section files at v1.0; findings below are not blockers for the spec itself
> (canonical content lives elsewhere) but flag residual ways this file can mislead
> a reader who skims past the banner.
>
> Severity legend: **H** = the outline as it stands could mislead implementation;
> **M** = should resolve when next touching this file; **L** = follow-up.

### Verified premises
- `SPEC_INDEX.md` lists Spec #16 status: IN PROGRESS (May 2, 2026 reconciliation).
- `CLAUDE.md` "Open Issues" notes `EntityId` no-reuse cross-spec back-propagation
  pending (`ERR-016-002`) and three golden-vector files named in §9.5 not yet
  authored (`hkdf-sha256-kat.md`, `siphash-2-4-kat.md`, `serialize-canonical-corpus.md`).
- `CLAUDE.md` "Fixed64 stage scope decision" (April 26, 2026) moved Fixed64 and
  cross-platform parity to Stage 5+. Stage 0 uses `float`.
- This outline §0 already declares SUPERSEDED with section files canonical.

### Findings

1. **[H] §1.2 Tier A definition contradicts Stage-0 float reality without
   inline qualifier.** "Tier A (authoritative hard): bitwise exact equality
   required" is correct in principle but, at Stage 0, only serial-path /
   integer / fixed-encoding fields can satisfy it (per `section-1.md`
   §1.3.1.1, Pass 3 H-C). The outline's §1.2 has no qualifier and a reader
   skipping the SUPERSEDED banner could believe Tier A applies to all
   fields including parallel-touched floats. Add a one-line qualifier
   pointing to `section-1.md` §1.3.1.1 even in this superseded file.

2. **[H] §10 cross-platform certification framed as in-scope.** "Freeze
   supported: OS, CPU arch, compiler/runtime versions, build flags" reads
   as a Stage 0 deliverable. Per CLAUDE.md "Fixed64 stage scope decision"
   this is Stage 5+. The Open Questions §13 notes the deferral but §10
   itself does not. A reader sampling §10 in isolation would over-scope.
   Add a Stage-5+ banner at §10 head.

3. **[M] §3.3 deterministic reduction topology is mandated without cost
   accounting.** "Floating-point reductions MUST use deterministic reduction
   topology (fixed tree shape), or be marked Tier B" imposes a non-trivial
   implementation pattern across every parallel job. Section files have
   moved past this (Tier B is the default at Stage 0 per `section-1.md`
   §1.3.1.1) but the outline still states the stronger MUST. Cross-link
   to the resolved policy.

4. **[M] §12 Requirement ID Seed Set retains stale IDs with strikethrough.**
   `~~FR-DS-004~~` etc. relies on markdown strikethrough rendering. Some
   markdown viewers and grep-based searches do not visually de-emphasize
   strikethrough. A future reader greps for `FR-DS-004` and finds it here
   pointing at semantics that no longer exist (Pass 5 H-1). Recommend
   replacing with `FR-DS-004 [SUPERSEDED]` plain text instead of (or
   alongside) the strikethrough so plain-text search matches the warning.

5. **[M] EntityId no-reuse constraint not surfaced.** Per CLAUDE.md
   "Open Issues" (May 3, 2026), `ERR-016-002` declares a normative
   cross-spec constraint binding APPROVED specs Agent Movement #2 and
   Decision Tree #8 to guarantee EntityId lifetime uniqueness. The
   outline §3.2 (intra-phase ordering) keys off `EntityId ascending`
   but says nothing about reuse semantics. Even in a SUPERSEDED outline,
   §3.2 should cross-link to `section-3.md` §3.2.5 / `ERR-016-002`.

6. **[M] §9.5 acceptance-criterion artifacts unmentioned in outline.**
   Three golden-vector files (`hkdf-sha256-kat.md`, `siphash-2-4-kat.md`,
   `serialize-canonical-corpus.md`) are required per CLAUDE.md but the
   outline §9 ("Determinism Regression Suite") names "fixed vector corpus"
   only abstractly. Add explicit cross-link to the §9.5 acceptance criterion
   so readers know the corpus is more than a placeholder.

7. **[L] §8.2 trace channel "Snapshot digest chain" overlap with §5.2.**
   §5.2 already mandates `PrevSnapshotDigest` / `CurrentSnapshotDigest` in
   the snapshot record itself, so the digest chain is part of the snapshot
   payload, not a separate trace channel. Outline reads as if they are
   independent budget line items; this risks double-counting in the §8.2
   storage budget calculation.

8. **[L] §11 Approval Checklist does not pre-commit a Pass-Mechanics-style
   programmatic verification rule.** Project history (ERR-005) shows
   Approval Checklists are vulnerable to fabricated values. Outline §11
   says "objective and evidence-backed" but does not require that each
   checklist entry resolve to a programmatic check or a named file.
   Section files at v1.0 likely already address this; outline should
   cross-link if so.

9. **[L] No reference to Pass 5 H-1 in `critique-log.md`.** §0 banner says
   "see Pass 5 H-1 fix in critique-log.md" but the file does not list a
   line/section anchor. Add an anchor for findability.

### Recommended next steps
- This outline is frozen (SUPERSEDED). Findings #1, #2 warrant inline
  qualifier banners even in the frozen file because they are reader-trap
  hazards. Findings #3–#9 can be cross-link additions only.
- Do not re-version the outline; add an "Adversarial Review Notes" preamble
  pointer at §0 if the qualifiers above are applied.
- Findings about content (vs. signage) should be tracked against
  `section-*.md` files at v1.0, not against this outline.

