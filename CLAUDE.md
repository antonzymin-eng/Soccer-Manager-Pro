# CLAUDE.md — Tactical Director

> **Created:** March 26, 2026, 11:00 PM PST
> **Last Updated:** May 18, 2026 (Attacking AI #15 advanced `NOT STARTED → IN REVIEW` via section files v0.2; PASS-1 adversarial review (7 findings: 1 H / 5 M / 1 L) all resolved. Counts: 16 APPROVED, 4 IN REVIEW (#11 / #12 / #14 / #15), 0 NOT STARTED.)
> **Purpose:** Authoritative rules for any AI agent (Claude Code, Claude chat, etc.) working on this project. Read this file completely before every task.

---

## PROJECT IDENTITY

**Tactical Director** is a football (soccer) simulation game targeting "Football Manager killer" ambitions. It follows a 10-year, 6-stage development plan. The project is solo-developed with AI assistance.

**Current phase:** Stage 0 — Physics Foundation. Writing 20 formal technical specifications. **No code exists yet. No code will be written until all 20 specs are approved.**

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
└── src/                            ← Empty until all 20 specs approved
```

**Rules:**
- Each spec folder contains ONLY current-version files. No version suffixes in filenames. Git tracks history.
- `SPEC_INDEX.md` is the canonical source of truth for spec numbers, folder names, and approval status.
- `PROGRESS.md` is the canonical source of truth for schedule and milestone tracking.
- Never create files in `src/` during the specification phase.

**Deferred: `src/CLAUDE.md`** — Do NOT create until coding begins (after all 20 specs are approved). At that point it should cover: C# file naming conventions, constant catalogue file locations, Unity project structure, and build/test commands.

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

- **Attacking AI (#15) — IN REVIEW** — *May 18, 2026.* Section files v0.1 authored May 17, 2026 from `outline-detailed.md` v1.1 (9 section files + appendices.md, 3,306 lines total); PASS-1 adversarial review (7 findings: 1 H / 5 M / 1 L) filed and resolved in v0.2 fix pass May 18, 2026. **Key fixes:** (H) §3.3 WEAK_SIDE condition corrected — was `weakSideCount < MIN_WEAK_SIDE_AGENT_THRESHOLD` (allowed up to 4 WEAK_SIDE), now `attackingPool.count >= MIN_WEAK_SIDE_AGENT_THRESHOLD and weakSideCount == 0` (pool-size gate, one agent max); (M) `ATTACK_DWELL_TICKS [EST]` → `[GT]` propagated through §2 / §3 (was missing from FR-AT-022 / FR-AT-023 / §2.2.4 / §3.12 / §3.14 despite §6.1 and §9.1 declaring promotion); (M) `HysteresisEntry` struct completed (added `candidateRole` and `candidateDwell`; `prevPhase` moved to `TransitionHoldState`); (M) §2.2.4 / §2.2.5 field names made consistent with §3.9 / §3.12 / §3.13 / §6.5 (renamed `holdTicks`→`candidateDwell`, `ticksRemaining`→`transitionHoldTick`, `holdActive bool`→`prevPhase`); (M) §3.13 `hysteresisState.prevPhase` → `transitionHoldState.prevPhase`; (M) T-AT-U-048 expected output corrected; (L) §3.8 `overloadFlank = NONE` removed; §6.5 memory estimates corrected. `SPEC_INDEX.md` row 15 flipped `NOT STARTED → IN REVIEW` atomically with v0.2 landing. **Outstanding (gates `IN REVIEW → APPROVED`):** (a) ERR-015-001 `DOMAIN_TAG_ATTACKING_AI = 0x1B` allocation in #16 §3.4; (b) ERR-015-002 `TacticalContext.AttackIntent[]?` ratification in #8 §2.2.6; (c) ERR-015-005 one-token back-prop to #8 §1.3.2; (d) #12 Positioning AI reaches APPROVED; (e) ERR-015-003 / ERR-015-004 channel rows in #17 §3.10 (Stage 1; non-blocking); (f) lead-developer R-01..R-05 sign-off. 36 FRs in §2.1; 38 constants in §6.1; 85 tests in §5.1.

- **Goalkeeper Mechanics (#11) — IN REVIEW** — *May 16, 2026.* Section files v0.1 authored from `outline-detailed.md` v1.2; section-files PASS-1 adversarial review (`goalkeeper-mechanics/adversarial-review-section-files-v1.md`, 11 findings: 3 H / 5 M / 3 L) filed and resolved in v0.2 same day. **High-severity items resolved:** AR-S1-H1 (§6.1 steady-state budget reconciled ≤30 µs → ≤40 µs to match §6.3.1 decomposition); AR-S1-H2 (§3.5.3 `spillVelocity` Gaussian removed — KD-7 single-purpose-per-site rule); AR-S1-H3 (`HANDLING_K_BALL_SPEED` unit corrected `per m/s` → `dimensionless`). All 5 M and 3 L resolved per Appendix F mapping. `SPEC_INDEX.md` row 11 flipped `NOT STARTED → IN REVIEW` atomically with v0.2 landing. **Outstanding (gates `IN REVIEW → APPROVED`):** (i) `ERR-011-001` `DOMAIN_TAG_GOALKEEPER` allocation in #16 §3.4 — proposed `0x17` per KD-7; collision-management with open ERR-012-001 (#12 proposed block `0x17…0x1C`): whichever spec reaches `APPROVED` first takes `0x17`; if ERR-012-001 lands first, `DOMAIN_TAG_GOALKEEPER` shifts to `0x1D` (mirrors May 16 #10 / #12 shift); (ii) OI-002 #18 Appendix F.0 channel-registry rows for 12 `gk.*` channels at Stage 0+1 delivery schedule; (iii) OI-003 DOI verification for four `[CITATION-PENDING]` external references in §8.3 (Savelsbergh 2002 already DOI-verified; Dicks 2010, Spratford 2009, Suzuki 1988, Williams & Burwitz 1993 pending; Opta/StatsBomb retained as commercial-data baseline class per Heading #10 §9.6 precedent); (iv) OI-005 atomic patch revision to Positioning AI #12 §3.3.3 / §6 promoting `GK_DEPTH_M` / `GK_ADVANCE_FACTOR` / `GK_LATERAL_FACTOR` `[EST]` → `[GT]` per KD-13 — fires atomically with #11 IN REVIEW status flip; (v) OI-006 `Ball.SetPossessor` surface verification against Ball Physics #1 §3.1 — if absent, file `ERR-011-002` for pure namespace amendment; (vi) lead-developer R-01..R-05 sign-off. OI-008 (`certification-platform.md` Stage-0 host pin) remains carved out — does not block #11 sign-off; shared with Heading #10 OI-006. KD-12 dive-Z ownership preserved (#11-owned synthetic Z trajectory at Stage 0; retires to AM #2 native Z at Stage 1+ via §7.5; `GroundedReason.DIVING_SAVE` added at that time as non-behavioral patch). 44 FRs in §2.1; ~79 constants in §3.4; 4 RNG draw sites in §4.4.2.

- **Heading Mechanics (#10) — APPROVED** — *May 16, 2026.* Section files v0.1 authored from `outline-detailed.md` v1.1 (with prior outline PASS-1 fix pass landed in v1.1). Section-files PASS-1 adversarial review (`heading-mechanics/adversarial-review-section-files-v1.md`, 21 findings: 5 H / 9 M / 7 L) filed and resolved in v0.2 same day; OI-001..OI-005 closure fix pass + lead-developer R-01..R-05 sign-off completed in v0.3. **All Outstanding Items RESOLVED:** (i) `ERR-010-001` `DOMAIN_TAG_HEADING = 0x16` allocation landed in #16 §3.4 v1.0.2 patch (pure namespace allocation per ERR-017-001 / `0x15` precedent; no `DETERMINISM_DIGEST_VERSION` bump); #10 §3.1 row `[CROSS-PENDING] → [CROSS]` promoted atomically; (ii) OI-002 channel-registry re-anchored from non-existent "#18 §3.10 channel rows" to #18 Appendix F.0 schema with Stage 0+1 deliverable schedule (subsystem-channel rows populate at first `src/Gameplay/Heading/` commit per #18 Appendix F.0 / §7.2); (iii) OI-003 DOI verification — 5/5 academic DOIs verified, 2 v0.1 fabricated references (Bull 1985 — not findable; Auger & Pellegrini 2007 — not findable) replaced with real equivalents (Babbs 2001 *Sci World J* DOI [10.1100/tsw.2001.56](https://doi.org/10.1100/tsw.2001.56); Tomczak et al. 2021 *J Hum Kinet* DOI [10.2478/hukin-2021-0012](https://doi.org/10.2478/hukin-2021-0012)); Opta/StatsBomb retained as commercial-data baseline class per §9.6 post-approval drafter task; (iv) OI-005 anchors pinned (#3 §3.4.2 `ICollisionEventConsumer` consumer pattern — v0.1 / v0.2 incorrectly described a non-existent pull-API; #8 §1.7.2 Stage 0 deferral row with §4.6.1 Stage 0+1 activation framing; #17 §3.2.1 `Publish API surface` with Tier B / Tier C overload tagging; #1 §3.1.11.2 `Ball.ApplyKick` / `BallState` surface). OI-006 (`certification-platform.md` Stage-0 host pin) remains carved out — does not block #10 sign-off per §6.1 / §9.4; gates `FR-PO-052` perf-gate activation only. KD-18 jump-kinematics ownership preserved (#10-owned synthetic Z trajectory at Stage 0; retires to AM #2 native Z at Stage 1+ per §7.8). Post-approval follow-ups (not gating, per §9.6): Goalkeeper #11 interface confirmation once #11 reaches `IN REVIEW`; comprehensive audit at draft-level rigor parity with #8.


- **Positioning AI (#12) — IN REVIEW** — *May 16, 2026.* Section files v0.1 (May 15, 2026) were authored ahead of the formal `SPEC_INDEX.md` status flip (same pattern as #16 on May 2, 2026). PASS-1 adversarial review of v0.1 (`positioning-ai/adversarial-review-section-files-v1.md`, 21 findings: 7 H / 9 M / 5 L) filed and resolved in v0.2 same day. **High-severity items resolved:** AR-S1-01 §3.5 compactness formula inverted (now "higher = tighter" semantics aligned with prose); AR-S1-02 §3.3.1 per-archetype `lineCutIndices` with AM `defaultLine` override for 4-2-3-1; AR-S1-03 §3.7 step order: line/lane resolved AFTER spacing+clamp; AR-S1-04 §4.4.3 rewritten to write `TacticalContext.FormationSlot` field directly (not via match-init `Stage0Default()` factory which would clobber `PressingInstruction`/`PassingInstruction`/`DefensiveLineDepth`); AR-S1-05 §3.5.2 aligned with §3.11 pseudocode subject (`baseSlot − centroid`); AR-S1-06 spacing iterates up to `SPACING_MAX_PASSES = 4`; AR-S1-07 `SENTINEL_NO_SLOT = Vector2.NegativeInfinity` distinct from NaN. All 9 M and 5 L findings resolved per §9.4 map. `SPEC_INDEX.md` row 12 flipped `NOT STARTED → IN REVIEW` atomically with v0.2 landing. **Outstanding (gates `IN REVIEW → APPROVED`):** (i) `ERR-012-001` Phase B/C domain-tag block allocation — original `0x16…0x1B` proposal shifted to `0x17…0x1C` on May 16, 2026 after #10 took `0x16` via ERR-010-001 (first-to-APPROVED precedent); `DOMAIN_TAG_POSITIONING_AI = 0x17` `[CROSS-PENDING]` (section-2 / section-6 / section-8 / section-1 updated atomically; collision avoided exactly per ERR-012-001's design intent); (ii) Appendix A derivation entries for the eight `[EST]` hysteresis / offset constants (`ANCHOR_DWELL_TICKS`, `LINE_HYSTERESIS_M`, `LINE_DWELL_TICKS`, `LANE_HYSTERESIS_M`, `PHASE_HYSTERESIS_TICKS`, `PHASE_LOOSE_VELOCITY_THRESHOLD`, `OFFSET_RANGE_X_M`, `OFFSET_RANGE_Y_M`) — values published, derivations pending; (iii) `ERR-012-002` one-token patch to #8 §3.1.7.2 already landed May 15, 2026 (closed); (iv) lead-developer R-01..R-05 sign-off. GK constants (`GK_DEPTH_M`, `GK_ADVANCE_FACTOR`, `GK_LATERAL_FACTOR`) remain `[EST]` pending #11 Goalkeeper Mechanics declaring its consumer contract — per CLAUDE.md "Interface Design Principle", #12 cannot ratify these `[GT]` ahead of #11.

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
- **EntityId no-reuse cross-spec back-propagation — RESOLVED at spec-text level** — *May 3, 2026 → May 6, 2026.* Deterministic Simulation #16 §3.2.5 declares a normative cross-spec constraint binding APPROVED specs Agent Movement (#2) and Decision Tree (#8) to guarantee EntityId uniqueness for the lifetime of a match (no despawned-ID reuse). Filed as `ERR-016-002` in `docs/tracking/spec-error-log.md`. **Resolution (May 6, 2026):** added `XC-002-001` to Agent Movement #2 §2.5 (v1.1.1, non-behavioral patch) and `XC-008-001` to Decision Tree #8 §1.7.3 (v1.1.1, non-behavioral patch). Both are patch-level revisions — no formula or contract change, only the formal cross-spec binding. **Outstanding:** small prose update in `docs/specs/deterministic-sim/section-3.md` §3.2.5 to change "filed for back-propagation" → "back-propagated to #2 §2.5 and #8 §1.7.3". Once that prose update lands, this issue closes and the spec-error-log row for ERR-016-002 flips to fully closed.
- **Deterministic Simulation #16 third-pass critique resolved (specs only)** — *May 3, 2026; approval model split May 6, 2026.* Third adversarial pass (4 H, 6 M, 6 L, 1 cross-cutting) addressed in §1, §3, §4, §5, §9 at v0.9. Full per-finding fix log: `docs/specs/deterministic-sim/third-pass-fix-log.md`. **§9 v1.1 (May 6, 2026)** split approval into Tier 1 (Conditional Approval — self-contained spec content; pursuable now) and Tier 2 (Final Approval — gated on #9 / #17 / #18 / #19 reaching `IN REVIEW`; #17 is now `APPROVED` — beyond the `IN REVIEW` gate — satisfying that precondition). `TBD-NORMATIVE` tag introduced for §8.3.1 placeholder cross-spec citation rows. **Golden-vector files (§9.5 #4):** the two RFC-derived KAT files (`hkdf-sha256-kat.md`, `siphash-2-4-kat.md`) are authorable now and are part of Tier 1; `serialize-canonical-corpus.md` remains Tier 2.
- **Superseded files from old naming convention** — Tracked in `file-manifest.md`. Manifest reconciled May 6, 2026 to post-migration folder names; covers all 20 spec folders with current statuses.
- **Stage 0 host platform pin — `certification-platform.md` is a placeholder, not pinned** — *verified May 6, 2026.* `docs/tracking/certification-platform.md` exists (created May 2, 2026) but every Stage 0 row except CPU architecture is `_TBD_` / `⏳ Not pinned`. Required pins: OS (Win 10/11), Unity LTS revision (e.g. 2022.3.X exact), backend (Mono vs IL2CPP), IL2CPP version (if applicable), compiler-flag set (denormals-are-zero off, fp-contract off, fma off unless platform-pinned), worker thread count, SIMD feature level. **Action:** lead-developer to fill in concrete values before the first Spec #16 §5.5 `FR-DS-009-GATE` certification run; until then the gate is unactivatable on Stage 0 (which is consistent with §5.5's own note). Not blocking spec drafting; blocking first cert run only.
