# CLAUDE.md — Tactical Director

> **Created:** March 26, 2026, 11:00 PM PST
> **Last Updated:** June 2, 2026 (Testing Strategy #19 AR-2 fix pass: 1M + 5L all fixed against the AR-1 v1.1 state. M-1: `GoldenVectorEntry` constructor enforces non-empty `name` / `sourcePath` / `citation` (parallel to AR-1 L-2/L-3 invariant guards on `GoldenVectorResult` + `DeterminismTierResult`). L-1: `GoldenVectorRunner.Catalogue()` + `RunAll()` wrap their backing arrays in `ReadOnlyCollection<T>` so callers cannot cast `IReadOnlyList<T>` back to mutable `T[]`; `DeterminismGate.RunTiers` applies the same wrap to the local `tierResults` array. L-2: `DeterminismGate.s_canonicalTierOrder` retyped `private static readonly IReadOnlyList<DeterminismTierKind>` backed by `ReadOnlyCollection<T>` (elements no longer reassignable through the field reference). L-3: range-style violations now throw `ArgumentOutOfRangeException` with explicit `actualValue` argument — `vectorsExecuted < 0`, `vectorsFailed < 0 \|\| vectorsFailed > vectorsExecuted`, `testsExecuted < 0`, `testsFailed < 0 \|\| testsFailed > testsExecuted`; pass invariant + null/empty diagnostic stay `ArgumentException`. L-4: KD-5 "Stage-gated" annotation extended to the remaining GT rows AR-1 L-5 skipped (UnitWallTimeBoundMs, PreCommitWallTimeBoundSeconds, QuarantineExpiryDays, EvictionQuarantineCount, EvictionWindowDays). L-5: maintainer notes added to `DeterminismTierKind` enum and `DeterminismGate.s_canonicalTierOrder` reminding contribs to extend atomically when a new tier is added. L-6: XML doc on `GoldenVectorEntry` / `GoldenVectorResult` / `DeterminismTierResult` notes default-value bypass (C# struct default-value semantics — constructor invariant checks skipped for `default(T)`). Files: GoldenVectorEntry.cs v1.2, GoldenVectorResult.cs v1.2, GoldenVectorRunner.cs v1.2, DeterminismTierKind.cs v1.1, DeterminismTierResult.cs v1.2, DeterminismGate.cs v1.2, TestingStrategyConstants.cs v1.2; src/CLAUDE.md v1.29 version-history row added. Last section adversarially reviewed: Testing Strategy (#19) AR-2. — Prior June 2 AR-1: 1H + 3M + 6L all fixed against the 14-file scaffold. H-1: `PerfGateRunner.Run` misleading `current?.` null-conditional removed (RegressionGate.Evaluate NPEs first on null current); explicit `ArgumentNullException` guards added on `baseline` + `current`; inner `Manifest?.` kept for the malformed-record diagnostic path only. M-1: `GoldenVectorRunner` private const `GoldenVectorRootRelPath` + `Stage0DeferredDiagnostic` gained XML `<summary>` per FR-CS-061. M-2: `DeterminismGate.Stage0DeferredDiagnostic` private const gained XML `<summary>`. M-3: `DeterminismSuiteResult.AllPassed` now fails closed on empty tier or golden-vector lists — previously returned true via empty early-exit loops, which would silently green-light FR-DS-009-GATE for a degenerate caller. L-1: `GoldenVectorEntry` property declaration order reordered to `Kind / Name / SourcePath / Citation` matching constructor parameter order. L-2/L-3: `GoldenVectorResult` + `DeterminismTierResult` constructors now enforce non-empty diagnostic, `0 ≤ failed ≤ executed`, and `passed == (executed > 0 && failed == 0)` via `ArgumentException` — the `(passed=false, executed=0, failed=0)` deferred-status shape stays valid because `0 > 0 && 0 == 0` evaluates to `false`. L-4: `DeterminismSuiteResult` constructor null-checks both list arguments with explicit `ArgumentNullException` instead of letting `.Count` NRE. L-5: `TestingStrategyConstants` pyramid bound + per-tier coverage XML docs gained "Stage-gated per KD-5" annotation. L-6: `DeterminismGate.RunTiers` tier-order array promoted from per-call local to `private static readonly s_canonicalTierOrder`. Files: PerfGateRunner.cs v1.1, GoldenVectorRunner.cs v1.1, DeterminismGate.cs v1.1, DeterminismSuiteResult.cs v1.1, GoldenVectorEntry.cs v1.1, GoldenVectorResult.cs v1.1, DeterminismTierResult.cs v1.1, TestingStrategyConstants.cs v1.1; src/CLAUDE.md v1.28 version-history row added. Last section adversarially reviewed: Testing Strategy (#19) AR-1. — Prior June 2: Testing Strategy #19 scaffold initiated: 14 new files under `src/testing-strategy/` initiating the golden-vector harness, determinism gates, and perf-gate runner. asmdef references TacticalDirector.DeterministicSim + TacticalDirector.PerformanceOptimization (autoReferenced false). `TestingStrategyConstants.cs` catalogues §3.10 governance: Fixed `MATCH_LENGTH_MINUTES=90` + GT pyramid bounds (0.60/0.25/0.12/0.03) + Tier A/B line+branch coverage minimums + `UnitWallTimeBoundMs=1.0` + `PreCommitWallTimeBoundSeconds=60.0` + quarantine/eviction windows (14d/3-strike/90d). `TestTier` (TierA/B/C) mirrors #16 §1.1.1 per KD-1; `TestLayer` enumerates the §3.1.1 five-layer taxonomy (FR-TS-001). Golden-vector harness (`GoldenVectorKind` + `GoldenVectorEntry` + `GoldenVectorResult` + `GoldenVectorRunner`): catalogues the three corpora #16 §9.5 acceptance criterion #4 pins (HKDF-SHA256 RFC 5869, SipHash-2-4-64 Aumasson&Bernstein 2012 App. A, SerializeCanonical #16 §3.2.4.1); Stage 0 returns deferred-status results naming the upstream KAT bodies in DeterministicSimTests until §7.5 D1 test-runner pin lands; Stage 0+1 parses the corpus markdown and invokes DeterministicRngService / CanonicalSerializer directly. Determinism gate (`DeterminismTierKind` + `DeterminismTierResult` + `DeterminismSuiteResult` + `DeterminismGate`): `DeterminismTierKind` enumerates #16 §5's canonical tier order (Unit/Integration/Scenario/Soak) per FR-TS-011 / FR-TS-018; `DeterminismGate.RunTiers()` is the single integration point per FR-TS-016 and aggregates the four §5 tiers with the golden-vector corpus into one FR-DS-009-GATE merge-block signal. Perf-gate runner (`PerfGateRunner` + `PerfGateReport`): `PerfGateRunner.Run(specId, loopTag, baseline, current, milestoneMs)` delegates the verdict to #18 `RegressionGate.Evaluate` (KD-3 boundary, §3.9.2 pointer) and wraps the result in `PerfGateReport` carrying spec/loop/scenario context for FR-PO-036 message formatting. `FR-DS-009-GATE` activation still gated on the Stage 0 host platform pin in `docs/tracking/certification-platform.md` per the standing OPEN ISSUES entry. Files: src/testing-strategy/{testing-strategy.asmdef + TestingStrategyConstants.cs + TestTier.cs + TestLayer.cs + GoldenVectorKind.cs + GoldenVectorEntry.cs + GoldenVectorResult.cs + GoldenVectorRunner.cs + DeterminismTierKind.cs + DeterminismTierResult.cs + DeterminismSuiteResult.cs + DeterminismGate.cs + PerfGateReport.cs + PerfGateRunner.cs}. src/CLAUDE.md v1.27 tree annotation + version-history row added. Last section adversarially reviewed: Event System (#17) AR-7. — Prior June 2 AR-7 (Event System #17): 2 findings (1M+1L) all fixed. M-1: EventTypeDispatcher<T>.AddHandler now scans for and reuses null slots left by RemoveHandler before appending at HandlerCount; returns the actual slot index so SubscriptionToken points at the occupied slot. Without slot reuse, Tier C runtime subscribe/unsubscribe cycling (FR-EVT-022) exhausted MaxTierCHandlersPerType (64) after that many cycles even when the net subscriber count was 0 — the 65th Subscribe<IEventC> would throw ERR_EVT_QUEUE_OVERFLOW (0x1701) on a logically-empty dispatcher. EventLedger.Subscribe and CosmeticChannel.Subscribe updated to consume AddHandler's return value instead of reading HandlerCount beforehand. L-1: EventBus.Publish<T>(IEventB) gained the same #if UNITY_EDITOR||DEVELOPMENT_BUILD producer-phase assertion that AR-1 M-2 added to the Tier A overload — both tiers route through PublishAuthoritative and both have producer-phase metadata in the registry; closing the Tier A↔Tier B asymmetry before Stage 5+ Tier B wiring lands. Files: EventLedger.cs v1.6, CosmeticChannel.cs v1.7, EventBus.cs v1.6. Last section adversarially reviewed: Event System (#17) AR-7. — Prior June 2 AR-6: 3 findings (1M+2L) all fixed. M-1: FR-CS-057 violation introduced by AR-5 — `Modified:` header dates in EventBus.cs / EventLedger.cs / CardIssuedEvent.cs were 2026-05-31 / 2026-05-31 / 2026-05-30 while their latest version-history rows were all 2026-06-02; headers refreshed to 2026-06-02; v1.5.1 / v1.5 / v1.2.1 documentation-only rows added. L-1: EventLedger.SerializeLedger magic literals `tier == 0 || tier == 1` replaced with `(byte)DeterminismTier.TierA / .TierB` per FR-CS-016 (#16 §3.2 owns the enum). L-2: src/CLAUDE.md event-system tree annotation for CardIssuedEvent.cs refreshed (0xFF → ushort/0xFFFF) to match the AR-5 L-1 widening. Files: EventBus.cs v1.5.1 (header date only), EventLedger.cs v1.5, CardIssuedEvent.cs v1.2.1 (header date only), src/CLAUDE.md tree row. Last section adversarially reviewed: Event System (#17) AR-6. — Prior June 2 AR-5: 3 findings (2M+1L) all fixed. M-1: EventBus.PublishAuthoritative structSize guards reordered to precede QueueCount++ reservation — a guard throwing after slot reservation left PayloadBuffer/SlotMeta unpopulated, causing SerializeLedger to emit a zero-byte Tier A record (CLR-default ordinal 0 → tier 0) and corrupt the canonical digest; M-2: EventLedger.Subscribe hard cast (EventTypeDispatcher<T>) replaced with 'as' + null-check, emitting ERR_EVT_ORDINAL_COLLISION (0x1707) diagnostic on duplicate-ordinal RegisterExternalRow misuse (symmetric to AR-4 L fix in CosmeticChannel.Subscribe — Tier A/B path was missed); L-1: CardIssuedEvent.FoulOrdinal widened byte → ushort (doc claimed it stored ushort intraPhaseDrawIndex, but a byte could not represent draw indices > 254; sentinel 0xFF → 0xFFFF; no call sites exist yet — non-breaking). Files: EventBus.cs v1.5, EventLedger.cs v1.4, CardIssuedEvent.cs v1.2. Last section adversarially reviewed: Event System (#17) AR-5. — Prior June 2 Performance Optimization #18 AR-2: 4 findings (2M+2L) all fixed. M-1: RegressionGate.PassesAbsoluteDriftCheck now treats NaN milestone as the "skip drift check" signal (returns true), aligning helper with Evaluate; M-2: TraceChannelDescriptor constructor gained XML <summary> (FR-CS-060); L-1: SessionManifest.IsComplete now also validates HardwareCounters.CpuModel/ThermalState (default-struct slip-through closed per §3.3.2); L-2: TraceChannelDescriptor constructor enforces the symmetric F.0 invariant InsideTickPipeline ⇒ SignOffLogRef non-empty. Files: RegressionGate.cs v1.2, TraceChannelDescriptor.cs v1.2, SessionManifest.cs v1.1. — Prior June 2 PO #18 AR-1: 8 findings (2H+3M+3L) all fixed. H-1: TraceChannel.cs (5 public types) split into ChannelVerbosity.cs / ChannelSamplingRule.cs / ChannelDeterminismClass.cs / TraceChannelDescriptor.cs / TraceChannelRegistry.cs per CLAUDE.md FILE NAMING; H-2: HotPathAllocBudgetBytes → HOT_PATH_ALLOC_BUDGET_BYTES ([FIXED] constants are ALL_CAPS per FR-CS-001); M-1: RegressionGate.Evaluate dedupes via PassesPerPrCheck/PassesAbsoluteDriftCheck; M-2: RegressionGate degenerate baseline/milestone (≤0 or NaN) now fail-closed (float.NaN milestone remains the explicit skip-drift signal); M-3: BaselineReproducibilityAuditor degenerate origP50 (≤0/NaN) fails closed; L-1: TraceChannelDescriptor ctor enforces SamplingRule=PerNTicks ⇒ SampleN>0 invariant; L-2: BaselineReproducibilityAuditor sealed→static; L-3: IBudgetSource.GetEntries() returns IReadOnlyList<BudgetRollupEntry>; L-4: PromotionToleranceFraction stale reproducibility sentence removed. Files: PerformanceOptimizationConstants.cs v1.2, RegressionGate.cs v1.1, BaselineReproducibilityAuditor.cs v1.1, IBudgetSource.cs v1.1, TraceChannel.cs removed. Last section adversarially reviewed: Performance Optimization (#18) AR-1.) — Prior May 31: Event System #17 AR-4 (2H+1M+1L) Files: EventLedger.cs v1.3, EventBus.cs v1.4. Prior May 30: Event System #17 coded: 20 files (18 .cs + 2 asmdef), AR-1 3H+3M+2L fixed, AR-2 1L fixed, AR-3 clean. — Prior May 29: Deterministic Simulation #16 coded (23 files, AR-1 4H+4M, AR-2 1L, AR-3 1L, AR-3 clean); Attacking AI #15 coded (24 files, AR-1 2H+4M, AR-2 2L, AR-3 clean); Defensive AI #14 coded (19 files, AR-1 2H+1M, AR-2 clean); Pressing AI #13 coded (21 files, AR-1 3H+1M+1L, AR-2 clean); Positioning AI #12 coded (20 files, AR-1+AR-2+AR-3 clean); Decision Tree #8 coded (36 files + AR-1 2H+3M+4L + AR-2 clean); Perception System #7 coded (14 files + AR-1 3M+3L + AR-2 3L clean). May 28: Shot Mechanics #6 (27 files + AR-1 pass); Heading Mechanics #10 (25 files + AR-1/AR-2 passes); Goalkeeper Mechanics #11 (36 files + AR-1/AR-2/AR-3 passes). — Prior: May 27, 2026 (Collision System #3, First Touch #4, Pass Mechanics #5 coded and reviewed.) — Prior: May 18, 2026 (**Stage 0 specification phase COMPLETE. Counts: 20 APPROVED, 0 IN REVIEW, 0 NOT STARTED.**)
> **Purpose:** Authoritative rules for any AI agent (Claude Code, Claude chat, etc.) working on this project. Read this file completely before every task.

---

## PROJECT IDENTITY

**Tactical Director** is a football (soccer) simulation game targeting "Football Manager killer" ambitions. It follows a 10-year, 6-stage development plan. The project is solo-developed with AI assistance.

**Current phase:** Stage 0+1 — Implementation. All 20 formal technical specifications are APPROVED. **Coding has begun.** Ball Physics (#1) and Agent Movement (#2) have initial implementations. `src/CLAUDE.md` is the authoritative coding guide.

---

## REPO STRUCTURE

```
Soccer-Manager-Pro/
├── CLAUDE.md                       ← You are here. Read first. Always.
├── docs/
│   ├── planning/                   ← Master volumes, dev plan, best practices
│   ├── specs/
│   │   ├── SPEC_INDEX.md           ← Canonical spec numbering and status
│   │   ├── ball-physics/           ← Spec #1
│   │   ├── agent-movement/         ← Spec #2
│   │   ├── collision-system/       ← Spec #3
│   │   ├── first-touch/            ← Spec #4
│   │   ├── pass-mechanics/         ← Spec #5
│   │   ├── shot-mechanics/         ← Spec #6
│   │   ├── perception-system/      ← Spec #7
│   │   ├── decision-tree/          ← Spec #8
│   │   └── ...                     ← Specs #9–#20 as written
│   └── tracking/                   ← PROGRESS.md, spec-error-log.md, fix-manifest-pass-mechanics.md, file-manifest.md
└── src/                            ← Implementation (coding began May 2026)
    ├── CLAUDE.md                   ← Coding guide (read before writing any code)
    ├── ball-physics/               ← Spec #1 implementation (BallPhysicsCore, BallState, etc.)
    │   └── Tests/
    └── agent-movement/             ← Spec #2 implementation (13 files: see src/CLAUDE.md)
```

**Rules:**
- Each spec folder contains ONLY current-version files. No version suffixes in filenames. Git tracks history.
- `SPEC_INDEX.md` is the canonical source of truth for spec numbers, folder names, and approval status.
- `PROGRESS.md` is the canonical source of truth for schedule and milestone tracking.
- `src/CLAUDE.md` is the authoritative guide for all coding conventions. Read it before writing any code.

**Note on `src/` folder structure:** Spec source code lives at `src/<spec-folder-name>/` (e.g. `src/agent-movement/`). All spec folders now match this convention — the Ball Physics structural deviation (`src/Core/Physics/Ball/`) was corrected May 30, 2026 (see src/CLAUDE.md v1.23).

---

## CRITICAL DOMAIN CONVENTIONS

These conventions have caused bugs. Memorize them.

### Coordinate System

**Authoritative source:** Ball Physics Spec #1, §1.2 and Appendix C.

| Axis | Direction | Range | Notes |
|------|-----------|-------|-------|
| X | Goal-to-goal (pitch length) | 0–105m | |
| Y | Touchline-to-touchline (pitch width) | 0–68m | |
| Z | Height (vertical, up) | 0m = ground | Ball center rests at 0.11m |

**Origin:** Corner of pitch (0, 0, 0) — NOT pitch center.

### Fatigue Convention

`0.0 = fully rested`, `1.0 = fully fatigued`. Any inversion is a critical error. This has been found inverted before (Pass Mechanics §2 FR-02, now fixed).

### Constant Tags

Every constant in every spec MUST have exactly one of these source tags:

| Tag | Meaning | Rule |
|-----|---------|------|
| `[GT]` | Gameplay-Tuned | Designer sets value; must live in tunable config |
| `[EST]` | Estimated | Placeholder; must be validated before implementation |
| `[FIXED]` | Fixed / physical law | Derived from physics; never tune |
| `[DERIVED]` | Derived from other constants | Formula must be documented; never set independently |
| `[CROSS]` | Cross-spec constant | Defined in another approved spec; consumed read-only here; never set independently in this spec. Citation must name the authoritative spec and section. Use `[CROSS]` only when the value is copied verbatim without modification — if a formula transforms it, tag the result `[DERIVED]`. |
| `[CROSS-PENDING]` | Cross-spec constant blocked on an upstream `IN PROGRESS` spec | Used when a spec consumes a constant that will be `[CROSS]` once the upstream authority spec reaches `APPROVED`, but the numeric value is not yet allocated. Citation must name the authoritative spec, section, and the `spec-error-log.md` back-prop ID tracking the allocation. Promoted to `[CROSS]` atomically with upstream approval. Use sparingly — every `[CROSS-PENDING]` tag is an outstanding cross-spec dependency that gates the consuming spec's own `APPROVED` transition. |

Constants live in their designated `.cs` constant catalogues — no magic numbers in formula code.

### Parameter-Based Physics (No Type Enums)

The Decision Tree supplies physical intent parameters (velocity, spin, angle). Physics systems translate these into vectors. There are NO `KickType`, `ShotType`, or `PassType` enums in the physics layer.

### Heartbeat Tick Rate

Tactical/AI loop: **10 Hz** (100ms per tick). Physics/render loop: **60 Hz** (~16.67ms per frame). These are different loops. Do not conflate them.

### Interface Design Principle

**Write interfaces only when both sides are specified.** Do not create interfaces against unspecified systems. This avoids phantom interface proliferation (ERR-001, ERR-004).

---

## CROSS-REFERENCE SYSTEM

Specs use typed cross-reference IDs:

| Prefix | Meaning | Example |
|--------|---------|---------|
| `XC-` | Cross-spec reference | XC-001 |
| `FM-` | Formula reference | FM-003 |
| `EC-` | Edge case reference | EC-012 |
| `ERR-` | Spec Error Log entry | ERR-010 |

**KNOWN HAZARD — Spec Renumbering Cascades:** When any spec changes its canonical number, ALL cross-references across ALL files must be updated. This has been the single most recurring bug class in this project.

**KNOWN HAZARD — Stale Spec Numbers in Old Files:** Many files written before February 2026 contain wrong spec numbers from an earlier numbering scheme. A complete old-to-correct mapping is in `SPEC_INDEX.md` under "FORMER NUMBERING".

---

## SPEC FILE CONVENTIONS

### Template Structure (every spec follows this)

| Section | Content |
|---------|---------|
| 1 | Introduction, scope, dependencies, key decisions |
| 2 | Functional requirements, data structures, failure modes |
| 3 | Core formulas, algorithms, pseudocode (subsections as needed) |
| 4 | Architecture, file layout, interface contracts |
| 5 | Test plan (unit + integration + validation scenarios) |
| 6 | Performance analysis and budgets |
| 7 | Future extensions and Stage 1+ deferrals |
| 8 | References, citations, DOI verification |
| 9 | Approval Checklist (quality gate) |
| Appendices | Derivations, verification tables, sensitivity analysis |

### Naming Inside Spec Folders

Files within a spec folder use descriptive names without version suffixes:

```
pass-mechanics/
├── outline.md
├── section-1.md
├── section-2.md
├── section-3-1.md          ← Subsections use hyphens
├── section-3-2.md
├── section-3-3-to-3-4.md   ← Grouped subsections
├── section-3-5-to-3-6.md
├── section-3-7-to-3-9.md
├── section-4.md
├── section-5.md
├── section-6.md
├── section-7.md
├── section-8.md
├── section-9-approval-checklist.md
├── appendices.md
└── audit-report.md          ← Comprehensive audit (if completed)
```

---

## AI BEHAVIORAL RULES

### Before Any Task

1. Read this entire `CLAUDE.md`.
2. Check `SPEC_INDEX.md` for current spec numbers and approval status.
3. If modifying a spec, read ALL files in that spec's folder first — not just the target file.
4. If the task involves cross-references, grep the entire `docs/specs/` tree for stale references before finishing.

### When Writing or Editing Specs

- Every constant must have a `[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]`, or `[CROSS]` tag.
- Every formula must include units, valid input ranges, and at least one worked example.
- Never fabricate verification values in Approval Checklists. All values must be programmatically verifiable against source files.
- Append a version history entry to every modified file.
- Include creation date and purpose header on every new file.

### When Writing Code (Future — after all 20 specs approved)

- C# with Unity 2022 LTS conventions.
- Struct-based, zero-allocation architecture in the game loop.
- All constants in designated constant catalogue files — no magic numbers.
- **Stage 0 uses `float`. Fixed64 migration is a Stage 5+ concern** (Spec #9 will define the library; existing approved physics specs are drafted against `float` and re-verified against Fixed64 only when cross-platform multiplayer becomes a requirement). Single-machine determinism (replay, save/load, debug rewind) is achieved via state snapshots, not deterministic arithmetic. Cross-platform bit-exact parity is a Stage 5 deliverable, not a Stage 0 quality gate.
- Deterministic replay is a hard requirement — no `System.Random`, no `DateTime.Now` in game logic.
- SplitMix64 for deterministic RNG. In Python tooling: omit `UL` suffix from C# constants; mask all intermediate multiplications with `& 0xFFFFFFFFFFFFFFFF`.

### Things That Have Gone Wrong Before (Learn From History)

| Trap | What happened | Prevention |
|------|---------------|------------|
| Stale spec numbers | Decision Tree was #7 in ~75 places; canonical is #8 | Always check SPEC_INDEX.md |
| Fabricated checklist values | Approval Checklist claimed sections existed that were never written | Verify every checklist entry against actual files |
| Inverted fatigue | FR-02 said "1 = rested" — opposite of correct | 0 = rested, 1 = fatigued. Always. |
| Wrong coordinate origin | "Pitch center" comment in Agent Movement §3.5 | Corner-origin is authoritative (Ball Physics §1.2) |
| Phantom interfaces | Interfaces written against unspecified consumer systems | Only write interfaces when both sides are specified |
| Superseded file references | Approval checklist pointed to v1.2 when current was v1.3 | Git versioning eliminates this (no version suffixes) |

---

## TRACKING DOCUMENTS

| Document | Location | Purpose |
|----------|----------|---------|
| `SPEC_INDEX.md` | `docs/specs/SPEC_INDEX.md` | Canonical spec numbering, folder mapping, approval status |
| `PROGRESS.md` | `docs/tracking/PROGRESS.md` | Schedule, milestones, weekly log |
| `spec-error-log.md` | `docs/tracking/spec-error-log.md` | Cross-spec architectural errors and remediation status |
| `fix-manifest-pass-mechanics.md` | `docs/tracking/fix-manifest-pass-mechanics.md` | Per-audit fix tracking (Pass Mechanics #5) |
| `file-manifest.md` | `docs/tracking/file-manifest.md` | Authoritative file inventory (update after every file change) |

---

## OPEN ISSUES

> **Keep this section current.** When resolving an issue, remove or update its entry here in the same commit. Each entry includes a "since" date so staleness is visible.

- **Attacking AI (#15) — APPROVED** — *May 18, 2026.* Section files v0.1 authored May 17, 2026 from `outline-detailed.md` v1.1 (9 section files + appendices.md, 3,306 lines total); PASS-1 adversarial review (7 findings: 1 H / 5 M / 1 L) filed and resolved in v0.2 fix pass May 18, 2026. Lead-developer R-01..R-05 sign-off granted May 18, 2026. **All outstanding gate items CLOSED:** (a) ERR-015-001 — `DOMAIN_TAG_ATTACKING_AI = 0x1B` allocated in #16 §3.4 v1.0.4; `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promoted in §6.1.9; (b) ERR-015-002 — `TacticalContext.AttackIntent[]?` field added to #8 §2.2.6 v1.1.2; Stage 1+ RUNNER override note added to #8 §3.1.7; (c) ERR-015-005 — "Attacking AI (#15)" added to multi-agent-coordination deferral in #8 §1.3.2 v1.1.2. Non-blocking deferred: ERR-015-003/004 (#17 channel-registry rows for `ATTACK_RUN_STARTED`/`OVERLOAD_DECLARED`) land at Stage 1 first commit. 36 FRs in §2.1; 38 constants in §6.1 (33 `[GT]` + 4 `[CROSS]` + 1 `[DERIVED]`); 85 tests in §5.1.

- **Goalkeeper Mechanics (#11) — APPROVED** — *May 18, 2026.* Section files v0.1 (May 16) → v0.2 (May 18 APPROVED). PASS-1 adversarial review (11 findings: 3 H / 5 M / 3 L) resolved in v0.2 same day. **All outstanding gate items CLOSED:** (i) ERR-011-001 — `DOMAIN_TAG_GOALKEEPER = 0x1D` allocated in #16 §3.4 v1.0.5 (value `0x1D`; shifted from proposed `0x17` because #12 reached `APPROVED` first per KD-7 first-to-`APPROVED` precedent); `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promoted in #11 §3.4.9; (ii) OI-005 — #12 GK constants `GK_DEPTH_M` / `GK_ADVANCE_FACTOR` / `GK_LATERAL_FACTOR` promoted `[EST]` → `[GT]` atomically in #12 §6.1 v0.3. Non-blocking deferred (post-approval): OI-002 (`gk.*` channel-registry rows land Stage 1 first commit); OI-003 (DOI verification for 4 `[CITATION-PENDING]` references); OI-006 (`Ball.SetPossessor` verification). Lead-developer R-01..R-05 signed May 18, 2026. 44 FRs in §2.1; ~79 constants in §3.4; 4 RNG draw sites in §4.4.2.

- **Defensive AI (#14) — APPROVED** — *May 18, 2026.* Section files v0.1 (May 17) → v0.3 (May 18 APPROVED). PASS-1 adversarial review (17 findings: 6 H / 7 M / 4 L) filed and resolved in v0.2 same day. **All outstanding gate items CLOSED:** (i) ERR-014-001 — `TacticalContext.MarkDirective?` nullable field added to #8 §2.2.6 v1.1.3 (May 18, 2026); `Stage0Default` initializes to `null`; ownership table row added; (ii) ERR-014-004 — `DOMAIN_TAG_DEFENSIVE_AI = 0x1A` allocated in #16 §3.4 v1.0.5; `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promoted in #14 §6.1 and §4.6. Non-blocking deferred: ERR-014-002 (MARK_ASSIGNED channel) and ERR-014-003 (LINE_STEPPED channel) land at Stage 1 first commit. Lead-developer R-01..R-05 signed May 18, 2026. 37 FRs; 26 constants (22 `[GT]` + 4 `[CROSS]`); ≥85 tests; ≤0.12 ms per-tick budget.

- **Heading Mechanics (#10) — APPROVED** — *May 16, 2026.* Section files v0.1 authored from `outline-detailed.md` v1.1 (with prior outline PASS-1 fix pass landed in v1.1). Section-files PASS-1 adversarial review (`heading-mechanics/adversarial-review-section-files-v1.md`, 21 findings: 5 H / 9 M / 7 L) filed and resolved in v0.2 same day; OI-001..OI-005 closure fix pass + lead-developer R-01..R-05 sign-off completed in v0.3. **All Outstanding Items RESOLVED:** (i) `ERR-010-001` `DOMAIN_TAG_HEADING = 0x16` allocation landed in #16 §3.4 v1.0.2 patch (pure namespace allocation per ERR-017-001 / `0x15` precedent; no `DETERMINISM_DIGEST_VERSION` bump); #10 §3.1 row `[CROSS-PENDING] → [CROSS]` promoted atomically; (ii) OI-002 channel-registry re-anchored from non-existent "#18 §3.10 channel rows" to #18 Appendix F.0 schema with Stage 0+1 deliverable schedule (subsystem-channel rows populate at first `src/Gameplay/Heading/` commit per #18 Appendix F.0 / §7.2); (iii) OI-003 DOI verification — 5/5 academic DOIs verified, 2 v0.1 fabricated references (Bull 1985 — not findable; Auger & Pellegrini 2007 — not findable) replaced with real equivalents (Babbs 2001 *Sci World J* DOI [10.1100/tsw.2001.56](https://doi.org/10.1100/tsw.2001.56); Tomczak et al. 2021 *J Hum Kinet* DOI [10.2478/hukin-2021-0012](https://doi.org/10.2478/hukin-2021-0012)); Opta/StatsBomb retained as commercial-data baseline class per §9.6 post-approval drafter task; (iv) OI-005 anchors pinned (#3 §3.4.2 `ICollisionEventConsumer` consumer pattern — v0.1 / v0.2 incorrectly described a non-existent pull-API; #8 §1.7.2 Stage 0 deferral row with §4.6.1 Stage 0+1 activation framing; #17 §3.2.1 `Publish API surface` with Tier B / Tier C overload tagging; #1 §3.1.11.2 `Ball.ApplyKick` / `BallState` surface). OI-006 (`certification-platform.md` Stage-0 host pin) remains carved out — does not block #10 sign-off per §6.1 / §9.4; gates `FR-PO-052` perf-gate activation only. KD-18 jump-kinematics ownership preserved (#10-owned synthetic Z trajectory at Stage 0; retires to AM #2 native Z at Stage 1+ per §7.8). Post-approval follow-ups (not gating, per §9.6): Goalkeeper #11 interface confirmation once #11 reaches `IN REVIEW`; comprehensive audit at draft-level rigor parity with #8.

- **Positioning AI (#12) — APPROVED** — *May 18, 2026.* Section files v0.1 (May 15) → v0.3 (May 18 APPROVED). PASS-1 adversarial review (21 findings: 7 H / 9 M / 5 L) filed and resolved in v0.2 (May 16). **All outstanding gate items CLOSED:** (i) ERR-012-001 — `DOMAIN_TAG_POSITIONING_AI = 0x17` allocated in #16 §3.4 v1.0.5 (#12 reached `APPROVED` first, claims `0x17`; `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promoted in §6.1); (ii) Appendix A.1–A.8 derivation entries confirmed for all 8 hysteresis/offset constants (all promoted `[EST]` → `[GT]`); (iii) KD-13 OI-005 — GK constants `GK_DEPTH_M` / `GK_ADVANCE_FACTOR` / `GK_LATERAL_FACTOR` promoted `[EST]` → `[GT]` atomically with #11 `APPROVED`. Lead-developer R-01..R-05 signed May 18, 2026. 47 active FRs; 18 `[GT]` + 7 `[DERIVED]`/`[FIXED]`/`[CROSS]` constants in §6.1 (no `[EST]` remain).

- **Fixed64 Math Library (#9) — APPROVED** — *May 15, 2026.* §9 v1.0 lead-developer sign-off granted; `SPEC_INDEX.md` row 9 flipped `IN REVIEW → APPROVED`. §9.1–§9.6 evidence-anchored items all verified against cited section files; §9.2 engine + gameplay owner sign-offs granted (gameplay confirmed against §8.4 typed cross-references `XC-009-002`..`-005` covering #1 / #2 / #3 / #8; #6 has no Fixed64-dependent surface at Stage 0 — parameter-based physics consumes Ball Physics arithmetic transitively); §9.7 reciprocal `XC-016-NNN` resolved via Deterministic Simulation #16 §8.3.2 (May 14, 2026) — comparator-glossary deferral documented under CLAUDE.md "Interface Design Principle" (writing the reciprocal against an unpublished #9 comparator glossary would manufacture exactly the phantom-interface class of error ERR-001 / ERR-004 prohibit); §6.6 forward-binding constraint preserved at Stage 5+. **Stage 0 effect:** none — Fixed64 binds to Stage 5+ per #9 §8.1 v0.2; Stage 0 continues to use `float` + state-snapshot determinism. **Post-`APPROVED` follow-ups, not blocking:** §9.8 lists four implementation-time deliverables (golden-vector corpus per §9.3 / §9.6; CI bench against §5.2 pinned host; full-matrix harness digest comparison; owning-team ledger per §9.4) — none of these are spec-text changes; they land alongside the Stage 5+ reference implementation. Stage 0 Priority 1–2 spec set is now fully complete (#1–#9).

- **Deterministic Simulation (#16) — APPROVED (Tier 2)** — *May 14, 2026 (later same day).* Tier 2 sign-off granted; `SPEC_INDEX.md` row 16 flipped `IN REVIEW → APPROVED`. All §9.4.2 Tier 2 gates cleared: §9.5 #4(a)/(b)/(c) spec-level sub-conditions all SATISFIED (golden-vector files at `golden-vectors/{hkdf-sha256-kat.md v1.1, siphash-2-4-kat.md v1.1, serialize-canonical-corpus.md v1.0}`); §8.3.1 cross-spec re-audit COMPLETE (§8 v1.2) — all four upstream rows (#9 / #17 / #18 / #19) promoted to `complete`; ERR-017-001 closed atomically by allocating `DOMAIN_TAG_EVENT_LEDGER = 0x15` in #16 §3.4 v1.0.1 (next value after `DOMAIN_TAG_ENV_FP = 0x14`; pure namespace allocation, no `DETERMINISM_DIGEST_VERSION` bump); §9.3 sign-offs (lead-developer Tier 2, QA-automation, platform-certification) granted at §9 v1.7 with platform-certification carrying explicit Stage-0 host-platform-pin caveat. **Downstream effects:** unblocks #17 §3.10 `[CROSS-PENDING]` → `[CROSS]` promotion (`DOMAIN_TAG_EVENT_LEDGER`); satisfies the #18 / #19 KD-2 `TBD-NORMATIVE` precondition (a) for their own `APPROVED` advancement. **Outstanding (post-`APPROVED`) follow-ups, not blocking #16:** (i) #9 / #18 / #19 still need to resolve their own `TBD-NORMATIVE` cites of #16 against the now-`APPROVED` text; (ii) §8.3.2 #9 comparator-glossary deferral re-opens at Stage-5+ when #9 publishes its glossary; (iii) `certification-platform.md` Stage-0 host-platform pinning remains a lead-developer task tracked in its own OPEN ISSUES entry below.

- **Event System (#17) — APPROVED** — *May 13, 2026; ERR-017-001 fully closed May 15, 2026.* Section files authored May 13, 2026 from `outline-detailed.md` v1.1; PASS 1 (3 H / 5 M / 12 L) and PASS 2 (2 H / 6 M / 7 L) adversarial critiques resolved in v0.2 and v0.3; lead-developer sign-off granted May 13, 2026; all section files at v0.3. **ERR-017-001 fully resolved**: #16-side allocation landed May 14, 2026 (`DOMAIN_TAG_EVENT_LEDGER = 0x15` in #16 §3.4 v1.0.1); #17-side promotion landed May 15, 2026 in #17 §1.0.1 patch — `[CROSS-PENDING]` → `[CROSS]` across §3.4.2 / §3.10 / §1.4 / §2.4.4 / §7.5 D9 / §8.1.4 / §8.3.4 / §8.4 / §9.2 Q10 / §9.3 R3, with literal value `0x15` inlined in §3.4.2 prose, §3.10 catalogue row, Appendix B byte streams, and Appendix D glossary. No further #17-side residual.
- **Performance Optimization Strategy (#18) — APPROVED** — *May 15, 2026 (later same day still).* §9 v1.0 lead-developer sign-off granted per `performance-optimization/section-9-approval-checklist.md`. v1.0 `[TBD-NORMATIVE]` sweep landed across §1 / §2 / §3 / §4 / §5 / §6 / §8 / appendices — 60 citation-qualifier instances resolved against #16 APPROVED text and #19 v1.0.1 patch-revised text. §9.4.1 blocker list all marked RESOLVED. KD-2 sequencing satisfied (#16 Tier 2 APPROVED May 14; #19 APPROVED May 15 earlier same day; #18 advances same day). `SPEC_INDEX.md` row 18 flipped `IN REVIEW → APPROVED`. KD-3 inverted boundary with #16 preserved: #18 owns trace pipeline; #16 retains §3.2.4.1 record format / §5 regression scenarios / §3.1 emission veto. `certification-platform.md` Stage-0 row `_TBD_` remains a lead-developer task tracked in its own OPEN ISSUES entry; blocks first `FR-PO-052` Stage 0+1 perf-gate activation only.

- **Performance Optimization Strategy (#18) — IN REVIEW** — *May 13, 2026 (outline v1.0 / v1.1); section files v0.1 May 13, 2026; PASS-1 adversarial review filed May 14; v0.2 fix pass applied May 14, 2026; `SPEC_INDEX.md` row 18 flipped `IN PROGRESS → IN REVIEW` atomically with v0.2 landing.* *(superseded above; entry retained for history.)* Detailed outline (`outline-detailed.md`) authored May 13, 2026 from `outline.md` v1.0 + May 6 adversarial review (6 H / 5 M / 2 L findings, all resolved). Establishes 11 cross-cutting design decisions (KD-1…KD-11) including KD-2 per-spec §6 ratify-not-override authority, KD-3 boundary with #16 (inverted in v1.1 — Spec #18 owns trace pipeline; Spec #16 retains canonical record format §3.2.4.1 + regression scenarios §5 + determinism-of-emission veto over tick-pipeline trace points §3.1), KD-4 boundary with #19, KD-7 Tier-C-only degradation (deterministic-replay-safe), KD-8 10 Hz / 60 Hz loop-separation tagging, KD-6 determinism-aware profiling, KD-10 hot-path enumeration via per-spec §6 union. **Section-file PASS-1 adversarial review (May 14, 2026)** filed `ERR-018-002` through `ERR-018-011` (4 H + 6 M findings: stale `[HotPathAllocExempt]` citation against #20; MUST/MAY conflict between FR-PO-067 and §3.4.4 on baseline re-run; three-way stage contradiction on +5% threshold across FR-PO-031 / §7.5 D9 / §7.1; absent channel-registry-schema deliverable from Appendix F; `[GT]` vs `[FIXED]` mis-tag on 0-byte hot-path allocation budget; three #19 body-text citations lacking `TBD-NORMATIVE`; untagged ±20% promotion tolerance; FR-PO-070 Stage-0 MUST invoking Stage-0+1-only tooling; untagged Appendix F.1 / F.5 governance constants absent from §3.10; premature §9.4 status flip versus `SPEC_INDEX.md`). **v0.2 fix pass (May 14, 2026)** resolves all 10: attribute ownership relocated to #18 §3.7.5 (declaration site at first `src/` commit); §3.4.4 MAY → MUST and Stage 0 carve-out recorded; §7.5 D9 re-anchored Stage 0+1 to match FR-PO-031 / §7.1; new Appendix F.0 channel-registry-schema authored (Stage 0 deliverable / Stage 1 rows); §3.10 + §8.4 retag 0 bytes/tick `[FIXED]`; three #19 citations gain `TBD-NORMATIVE` and §9.4.1 blocker list extended; ±20% / N=100 / 1%-flake added to §3.10 + §8.4; FR-PO-070 split Stage 0 manual / Stage 0+1 automated; canonical status flipped to `IN REVIEW` in `SPEC_INDEX.md` row 18 and recorded here. **`ERR-018-001` resolved at outline level in v1.1 (May 13, 2026).** v1.0 inherited the same #16 citation drift #19 v0.2 corrected May 12 (#16 §7 → actually §5; #16 §5 → actually §3.2.4.1; "§8 trace channels" → no such section in #16). v1.1 resolves both surface drift and the underlying architectural ambiguity (KD-3 inverted — trace pipeline is an observability concern owned by #18; #16 retains record format + emission constraints + scenario corpus). New FR-PO-058a enforces emission constraints. Lead-developer sign-off on `IN REVIEW → APPROVED` remains pending and gated on §9.4.1 `TBD-NORMATIVE` resolution (Spec #16 Tier 2 `APPROVED` + Spec #19 `APPROVED`). **Unblocks:** Spec #19 KD-2 precondition (b) — satisfied.
- **Testing Strategy & Framework (#19) — APPROVED** — *May 15, 2026 (later same day).* §9 v1.0 lead-developer sign-off granted per `testing-strategy/section-9-approval-checklist.md`. KD-2 sequencing preconditions all cleared: (a) #16 Tier 2 `APPROVED` May 14 (§9.3.7); (b) #18 outline + section files v0.3 IN REVIEW (§9.3.6); (c) `[TBD-NORMATIVE]` sweep landed in #19 §1.0.1 patch May 15 across §1 / §3 / §4 / §5 / §6 / §8 / appendices — 51 citation-qualifier instances resolved against #16's APPROVED text and #18's v0.3 surface. Remaining grep hits on the bare `TBD-NORMATIVE` token are meta-references only (sweep history, version-history entries, glossary definitions, §2.3 self-applied failure-mode descriptions, §9 grep instructions). **Downstream effect:** clears Spec #18's last KD-2 gate ("#19 APPROVED") for its own `IN REVIEW → APPROVED` advancement.

- **Testing Strategy & Framework (#19) — IN REVIEW** — *May 12, 2026; precondition (b) cleared May 13, 2026.* *(superseded above; entry retained for history.)* Initial section-file draft authored May 12 from `outline-detailed.md` v1.1; v0.2 self-critique sweep applied same day (3 H / 6 M / 8 L findings, all resolved). Per spec §9.3.6–§9.3.8 (KD-2 sequencing), advancement to `APPROVED` is gated on three preconditions: (a) Spec #16 (Deterministic Simulation) reaching Tier 2 `APPROVED`, (b) Spec #18 (Performance Optimization Strategy) having at least an outline-level draft with §4 and §7 headers, and (c) all `TBD-NORMATIVE`-tagged citations of #16 and #18 in the #19 spec text resolved. **Status (May 14, 2026):** (a) #16 advanced `IN PROGRESS → IN REVIEW` (Tier 1) on May 14, 2026 — precondition (a) still NOT cleared because it requires Tier 2 `APPROVED`, not Tier 1 `IN REVIEW`; (b) **CLEARED** by Spec #18 `outline-detailed.md` v1.0 landing — §4 (Architecture & Integration) and §7 (Future Extensions) headers both present and detailed; (c) `TBD-NORMATIVE` tags remain in `testing-strategy/section-3.md` and `appendices.md` — NOT cleared (still gated on #16 Tier 2 approval; #18-side tags now resolvable against #18 section files v0.2 at `IN REVIEW`). Lead-developer sign-off on `IN REVIEW → APPROVED` remains blocked until (a) and (c) clear.
- **Code Standards & Style Guide (#20) — APPROVED** — *May 11, 2026.* Section files + appendices authored May 7–8 from `outline-detailed.md` v1.3; adversarial review pass-1 applied May 11; lead-developer R-01..R-05 sign-off completed same day. `SPEC_INDEX.md` row 20 reflects `APPROVED`. §9 v1.1 APPROVED.
- **Pass Mechanics (#5) — RESOLVED** — *re-approved May 6, 2026.* Original Feb 22 approval → suspended Mar 25 (19 audit findings, all fixed per `fix-manifest-pass-mechanics.md`) → re-approved May 6 after §3.3–§3.9 follow-up findings F-A01 / F-A02 resolved via option-3 hybrid (`spinBase`/`spinMax` columns added to §3.1.4 Master Physical Profile Table; `WINDUP_FRAMES`/`FOLLOWTHROUGH_FRAMES` localized in §3.8.10). Re-review packet at `pass-mechanics/re-review-packet.md`. Stage 0 Priority 1–2 spec set (#1–#8) is now complete.
- **Decision Tree (#8) draft-level quality gate** — *April 27, 2026.* Approved at draft-level rigor (lighter than Pass Mechanics #5 / Shot Mechanics #6 programmatic audit). Comprehensive audit is a candidate before implementation begins. Tracked here as a follow-up item, not a blocker.
- **Renumbering cascade resolved** — *April 26, 2026:* Three-pass sweep applied across all specs. ~115 body-text substitutions in three commits covering Decision Tree #7→#8, Heading #9→#10, Goalkeeper #10→#11, Fixed64 #8→#9. Audit reports and changelog rows intentionally left unchanged (they document history). Verification grep on April 26, 2026 returns zero remaining stale body-text references. Re-run grep before any future tagging.
- **Approval tags created locally, not yet pushed** — *April 26–27, 2026; precondition check added May 6, 2026; tag 8 added May 13, 2026:* Annotated tags `spec-ball-physics-v1.0-approved`, `spec-agent-movement-v1.0-approved`, `spec-collision-system-v1.0-approved`, `spec-first-touch-v1.0-approved`, `spec-shot-mechanics-v1.0-approved`, `spec-perception-system-v1.0-approved`, `spec-decision-tree-v1.0-approved`, `spec-event-system-v1.0-approved` created on their respective branches. Remote returns HTTP 403 on tag push (branch-protection). Push after merge to main with privileged credentials. **Precondition check (run BEFORE `git push --tags`):** (1) After merge to main, run `git tag -v <tagname>` for each of the 7 tags and confirm the SHA each tag points to is still reachable from `origin/main` (`git merge-base --is-ancestor <tag-sha> origin/main`). (2) If main uses **squash-merge** or **rebase-merge**, the original branch commit SHAs are orphaned and the tags point to dead commits — in that case **delete and recreate** each tag against the corresponding squashed/rebased commit on main (use `git log --grep` or commit-message search to identify the new commit), do NOT just `--force` push. (3) If main uses true merge commits, normal `git push origin --tags` is correct. Document which path was taken in this OPEN ISSUES entry when resolving.
- **Fixed64 stage scope decision** — *April 26, 2026:* After review, Fixed64 migration is **Stage 5+** (not Stage 0). Stage 0 uses `float` and achieves single-machine determinism via state snapshots. Cross-platform bit-exact parity is no longer a Stage 0 quality gate — it moves to Stage 5 when multiplayer is added. CLAUDE.md "When Writing Code" rules and `master-development-plan.md` (§2.3, §8 risk mitigation, §9 metrics, Stage 0 success criteria, Stage 6 description) all updated to match. Existing approved physics specs were drafted under this Stage 5+ assumption and remain consistent — no spec content changes needed.
- **Deterministic Simulation #16 third-pass critique resolved (specs only)** — *May 3, 2026; approval model split May 6, 2026.* Third adversarial pass (4 H, 6 M, 6 L, 1 cross-cutting) addressed in §1, §3, §4, §5, §9 at v0.9. Full per-finding fix log: `docs/specs/deterministic-sim/third-pass-fix-log.md`. **§9 v1.1 (May 6, 2026)** split approval into Tier 1 (Conditional Approval — self-contained spec content; pursuable now) and Tier 2 (Final Approval — gated on #9 / #17 / #18 / #19 reaching `IN REVIEW`; #17 is now `APPROVED` — beyond the `IN REVIEW` gate — satisfying that precondition). `TBD-NORMATIVE` tag introduced for §8.3.1 placeholder cross-spec citation rows. **Golden-vector files (§9.5 #4):** the two RFC-derived KAT files (`hkdf-sha256-kat.md`, `siphash-2-4-kat.md`) are authorable now and are part of Tier 1; `serialize-canonical-corpus.md` remains Tier 2.
- **Superseded files from old naming convention** — Tracked in `file-manifest.md`. Manifest reconciled May 6, 2026 to post-migration folder names; covers all 20 spec folders with current statuses.
- **Stage 0 host platform pin — `certification-platform.md` is a placeholder, not pinned** — *verified May 6, 2026.* `docs/tracking/certification-platform.md` exists (created May 2, 2026) but every Stage 0 row except CPU architecture is `_TBD_` / `⏳ Not pinned`. Required pins: OS (Win 10/11), Unity LTS revision (e.g. 2022.3.X exact), backend (Mono vs IL2CPP), IL2CPP version (if applicable), compiler-flag set (denormals-are-zero off, fp-contract off, fma off unless platform-pinned), worker thread count, SIMD feature level. **Action:** lead-developer to fill in concrete values before the first Spec #16 §5.5 `FR-DS-009-GATE` certification run; until then the gate is unactivatable on Stage 0 (which is consistent with §5.5's own note). Not blocking spec drafting; blocking first cert run only.
