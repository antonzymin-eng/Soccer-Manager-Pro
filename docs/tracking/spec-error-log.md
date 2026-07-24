# Specification Error Log

**Purpose:** Records architectural errors, unnecessary complexity, and incorrect patterns
identified during specification review. Each entry documents the problem, the correct
approach, and every file requiring revision. Fixes are deferred — this log is the
authoritative remediation backlog.

**Created:** February 19, 2026, 5:00 PM PST
**Version:** 1.40
**Updated:** July 24, 2026 (v1.40 — ERR-030-009 filed + RESOLVED at Discipline & Suspensions #44 section-file approval: `season-competition-loop/section-2.md` FR-SN-013 (v0.8) + `section-3.md` §3.4 (v0.8) gain the **#44 suspension-availability-filter null seam** on the managed squad's resolve→configure path (`ISquadProvider.ResolveByClubId` → *filter* → `ConfigureSquads`; a value-copy reduction, empty until #44 T2 — the flow-side sibling of the ERR-030-002/004/006/007 tick-order pre-declarations). **ERR-030-008 remains soft-reserved by #43** (its T-phase (a') hook + deep fixture-day driver), so #44 takes 009. **No #16 change** — #44 is the read-only class (no RNG stream / domain tag / `SubsystemOrdinals` entry; the #37/#49 positive property): its accumulation is a pure fold over already-deterministic Tier A card/substitution events via the #37-class per-tick ledger tap, and any future quick-sim card synthesis is #30-owned on #30's `0x22` stream. Doc-only; no `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 24, 2026 (v1.39 — ERR-043-001 filed + RESOLVED at Competition Structure #43 section-file approval: `deterministic-sim/section-3.md` §3.4 (v1.0.14) gains the three A-04 placeholder rows `_RESERVED_0x2B_` (Youth Academy #42, ordinal 93) / `_RESERVED_0x2C_` (Competition Structure #43, `SubsystemOrdinals.Competition = 94`) / `_RESERVED_0x2D_` (Board & Ownership #45, ordinal 95) — the gap-rule sweep completing the roadmap §6 contiguous block `0x20`–`0x2D` (the v1.0.13 precedent; the catalogue previously ended at `0x2A`, and #43 is the first of the three to reach it). `_RESERVED_0x2C_` **stays reserved at #43's approval** — #43's minimal tier (a singleton-league collection) is draw-free; it promotes to `DOMAIN_TAG_COMPETITION = 0x2C` at #43 T3's first knockout draw (keyed draws on `competition.draws`, `entityId = competitionId`, fixed-radix ordinals — no cursor, nothing serialized). **No #30/#40 change** — FR-SN-031's (a') promotion/relegation insertion point and #40's (b')-after-(a') ordering were pre-declared at those specs' approvals (#43 is the first management spec whose #30 spec-text seams were all reserved ahead; the code-side (a') hook + deep fixture-day driver are soft-reserved as ERR-030-008, T-phase). Pure namespace reservation; no `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 24, 2026 (v1.38 — ERR-030-007 filed + RESOLVED at Scouting & Player Knowledge #32 section-file approval: `season-competition-loop/section-3.md` §3.3 `RunWorldTickInFixedOrder` gains the **scouting null seam as tick-order step 7** (after staff so a scouting day reads the day's staff state — the ChiefScout doing the scouting; before the world-day tick; `AdvanceDay` → step 8), FR-SN-034 enumeration extended (section-2 v0.7 / section-3 v0.7). A **deep-tier position reservation** — #32's minimal tier is the fog-off omniscient identity (no assignment can exist; `AdvanceScoutingDay` no-ops with fog off), so the seam is empty until the deep tier's daily assignment progress (the ERR-030-002 #41 / ERR-030-004 #31 / ERR-030-006 #34 precedent). **No #16 change** — #32's minimal tier is draw-free (every read short-circuits at zero width before any draw), so `_RESERVED_0x24_` / `SubsystemOrdinals.Scouting = 86` stay RESERVED (the #40 ERR-040-001 / #31 / #34 precedent); promotion to `DOMAIN_TAG_SCOUTING = 0x24` lands at #32 T3's first accuracy draw. Doc-only; the world-floor byte-identity (FR-SN-026) is unaffected (null seam). No `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 23, 2026 (v1.37 — ERR-030-006 filed + RESOLVED at Staff & Backroom #34 section-file approval: `season-competition-loop/section-3.md` §3.3 `RunWorldTickInFixedOrder` gains the **staff null seam as tick-order step 6** (after transfers, before the world-day tick; `AdvanceDay` → step 7), FR-SN-034 enumeration extended (section-2 v0.6 / section-3 v0.6). A **deep-tier position reservation** — #34's scaffold projections are pull-based (threaded into #29/#41 when their inputs are built), so the seam is empty until the deep tier's daily candidate-pool / in-flight-hiring processing (the ERR-030-002 #41 / ERR-030-004 #31 precedent). **No #16 change** — #34's scaffold is draw-free, so `_RESERVED_0x26_` / `SubsystemOrdinals.Staff = 88` stay RESERVED (the #40 ERR-040-001 / #31 ERR-030-004 precedent). Doc-only; the world-floor byte-identity (FR-SN-026) is unaffected (null seam). No `DETERMINISM_DIGEST_VERSION` bump. **ERR-030-005 is soft-reserved by #31** (its deferred `RequestRosterCommit` build), so #34 takes 006. Prior update below.)
**Updated (prior):** July 23, 2026 (v1.36 — ERR-030-004 filed + RESOLVED at Transfers, Contracts & Negotiation #31 section-file approval: `season-competition-loop/section-3.md` §3.3 `RunWorldTickInFixedOrder` gains the **transfers null seam as tick-order step 5** (after injuries, before the world-day tick; `AdvanceDay` → step 6), FR-SN-034 enumeration extended (section-2 v0.5 / section-3 v0.5). A **deep-tier position reservation** — minimal #31 transfers are command-driven (`SubmitBid`), so the seam is empty until the deep tier's daily negotiation/rival-bid processing (the ERR-030-002 #41 documented-position precedent). **No #16 change** — #31's minimal tier is draw-free, so `_RESERVED_0x23_` / `SubsystemOrdinals.Transfers = 85` stay RESERVED (the #40 ERR-040-001 / #29 precedent). Doc-only; the world-floor byte-identity (FR-SN-026) is unaffected (null seam). No `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 23, 2026 (v1.34 — ERR-029-001 filed + RESOLVED at #29 (Training System) section-file approval: **no determinism promotion** — #29 is fully deterministic (pure integer projections; deterministic own-attribute variation), registers no RNG stream, so `_RESERVED_0x21_` / `SubsystemOrdinals.Training = 83` **stay reserved** (not promoted, unlike ERR-028-001's `0x20`). `deterministic-sim/section-3.md` §3.4 (v1.0.10) updates the `_RESERVED_0x21_` rationale; no code const, no `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated:** July 23, 2026 (v1.35 — ERR-040-001 + ERR-030-003 filed at Club Finances & Economy #40 section-file approval: `deterministic-sim/section-3.md` §3.4 (v1.0.12) adds the `_RESERVED_0x29_` placeholder / `SubsystemOrdinals.ClubFinances = 91` **RESERVED not promoted** (minimal tier is a pure integer budget projection, no draw — the #29 `0x21` precedent); and `season-competition-loop/section-3.md` §3.5 gains the finance-settlement null seam at boundary-roll step (b') after the (a') #43 point (ERR-030-003, doc-only). No `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 23, 2026 (v1.34 — ERR-041-001 + ERR-030-002 filed at Injuries & Medical #41 section-file approval: `deterministic-sim/section-3.md` §3.4 (v1.0.11) allocates `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` / `SubsystemOrdinals.InjuriesMedical = 92` for the `injuries.occurrence` world-tick keyed-draw sub-stream (spec-text-first — code + registration at #41 T2); and `season-competition-loop/section-3.md` §3.3 gains the injuries null seam as tick-order step 4 (FR-SN-034 enumeration extended, ERR-030-002, doc-only). No `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 23, 2026 (v1.33 — ERR-028-001 filed at #28 section-file approval: `deterministic-sim/section-3.md` §3.4 (v1.0.9) promotes the `_RESERVED_0x20_` placeholder → `DOMAIN_TAG_PLAYER_PROGRESSION = 0x20` / `SubsystemOrdinals.PlayerProgression = 82` for the per-club `player-progression.regen` regen stream; spec-text-first like ERR-030-001 — code const + registration at #28 T2 with the first regen; `_RESERVED_0x21_` (#29) stays a placeholder; no `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 22, 2026, later same day (ERR-030-001 filed at #30 section-file approval: `DOMAIN_TAG_SEASON_LOOP = 0x22` / `SubsystemOrdinals.SeasonLoop = 84` reserved in `deterministic-sim/section-3.md` §3.4 (v1.0.8) for the Season & Competition Loop #30 season RNG sub-stream + the two `_RESERVED_0x20_`/`_RESERVED_0x21_` placeholders for #28/#29 (roadmap §6 block). This back-prop is **spec-text-first** (◑ partial): the code const + stream registration land at #30 T2 with the first draw site (FR-LW-031 — no phantom stream), unlike the code-first ERR-022/027-001. No `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 22, 2026 (ERR-027-001 + ERR-022-001 filed and RESOLVED at #27 promotion: the off-pitch determinism allocations `DOMAIN_TAG_PLAYER_DATABASE = 0x1F` / `SubsystemOrdinals.PlayerDatabase = 81` (#27) and `DOMAIN_TAG_LIVING_WORLD = 0x1E` / `SubsystemOrdinals.LivingWorld = 80` (#22) — both landed in code but never recorded in the #16 §3.4 spec text — are now filed there and in this log. `deterministic-sim/section-3.md` §3.4 gains both rows (v1.0.7); pure namespace allocations, no `DETERMINISM_DIGEST_VERSION` bump. Prior update below.)
**Updated (prior):** July 10, 2026, later same day (ERR-024-001 filed and RESOLVED at #23–#26 T0 implementation: Build-Up Structures #24 Appendix A v0.2's PASS-1 M-3 "lane-key correction" keyed every overlay row to lane values NO slot occupies — FR-BU-007 keys rows by the RECORDED `FormationSlotRecord.DefaultLine`/`DefaultLane`, and all three `PositioningAIConstants.Family*` tables record fullbacks at `LH`/`RH` (half-space) and central mids/forwards at `C`, so the catalogue as spec'd was a structural no-op. M-3 verified lane GEOMETRY (LB at y = 10.2 m is in the wide bin) but not the recorded seed values the key actually uses. Appendix A v0.3 + §3.2 v0.3 re-keyed with magnitudes/intents unchanged; `BuildUpOverlayCatalogue.cs` implements the corrected keys; `BuildUpStructureTests.Catalogue_RowKeys_HitEveryFamily_Err024001Regression` locks that every family receives a non-zero own-third offset per structure.)
**Updated (prior):** July 10, 2026 (ERR-021-005 through ERR-021-007, ERR-012-007 through ERR-012-009, and ERR-008-012 filed and RESOLVED same commit — the seven cross-spec back-props landed atomically with specs #23 Dismarking / #24 Build-Up Structures / #25 Positional Rotations reaching `APPROVED` (each spec's §2.3/§2.4 pending-ERR table, per its own pipeline step 6; #26 Tactical Presets declares no back-props at T0–T3). #21-side: `TeamTactic` gains `DismarkIntensity`/`BuildUpStructure`/`RotationFreedom` field rows + Appendix B canonical-order appends in pinned approval order #23 → #24 → #25 after `MarkingOrientation` (`tactical-instructions/section-2.md` v0.5 + `appendices.md` v0.5); serialization enters `WriteTeamTactic` with a `SNAPSHOT_SCHEMA_VERSION` bump only when each owning spec's wiring lands. #12-side: new `positioning-ai/section-3.md` §3.7.1 (v0.6) records the build-up overlay stage (ContextModifier → spacing), the dismark offset stage (spacing → pitch clamp, FR-DM-008), the `RotationController` pre-composition tick position, and the `AgentPositioningData.SlotIndex` single-writer contract amendment (no longer immutable after `SeedFromFormation`; `RotationController` sole post-seed writer). #8-side: `decision-tree/section-3-2.md` v1.5 §3.2.2.1 anchors the FM-DM-03 marked-pass-target multiplier in the external tactical-multiplier product before the final clamp. All amendments identity-preserving at zero-value dials; ERR-012-004..006 remain soft-reserved for the June-13 quarantine adjudication cluster and were deliberately skipped.)
**Updated (prior):** June 16, 2026 (ERR-016-006 through ERR-016-008 + ERR-017-003 filed from the `src/deterministic-sim/` + `src/event-system/` foundation adversarial review. ERR-016-006 (H) RESOLVED same commit — `SaveManager.Load` discarded the on-disk header so the digest chain was unverifiable on reload + `ReplayEngine` step-3 null-fingerprint NRE; `SaveManager.cs` v1.5 (`ReadHeaderBytes` + header-reconstructing `Load` overload) and `ReplayEngine.cs` v1.3 (fail-closed env guard). ERR-016-007 (M, open) fingerprint not on the on-disk header — cross-process digest/env verification blocked, needs a `SNAPSHOT_SCHEMA_VERSION` bump. ERR-016-008 (M, open) RNG zero-count `Reserve` ambiguity + `Skip`/`Reserve` by-convention parity. ERR-017-003 (M, open) `EventBus` producer-phase enforcement is debug-only → debug/release digest divergence on a mis-phased publish. The three open items are deferred for gate-verified follow-up — they are digest/wire-format-sensitive and the remote review environment has no .NET SDK.)
**Updated (prior):** June 13, 2026 (ERR-007-001 through ERR-007-003 filed from the Perception System #7 implementation AR-3 adversarial review (1H+1M+1L-cluster); all patched and CLOSED same commit — forced-refresh double-advance of cross-heartbeat state, pre-dedup candidate-buffer truncation, DeterministicHash Mathf.Abs overflow)
**Updated (prior):** June 11, 2026 (ERR-008-002 through ERR-008-011 filed from the Decision Tree #8 comprehensive audit (spec + May 29 implementation); all ten spec-side defects patched and CLOSED same commit — see the consolidated entry below and `decision-tree/audit-report.md`)
**Updated (prior):** May 22, 2026 (ERR-020-001 filed and resolved: Code Standards #20 §4.2 `[CROSS]` mirror ALL_CAPS → PascalCase; `section-4.md` v1.0.1 patched; `src/CLAUDE.md` v1.4 discrepancy note updated)
**Status:** ERR-001 through ERR-012, ERR-010-001 (closed May 16, 2026), ERR-011-001 (closed May 18, 2026), ERR-012-001 (closed May 18, 2026), ERR-012-002 (closed), ERR-016-001, ERR-016-002 (FULLY CLOSED May 18, 2026), ERR-017-001, ERR-018-001 through ERR-018-018 logged. ERR-010 closed (March 6, 2026). ERR-012 appended from addendum (April 22, 2026). ERR-016-001 added May 2, 2026 (phantom interface mitigation in Deterministic Simulation §4.2). ERR-016-002 added May 3, 2026; spec-text resolved May 6, 2026 (`XC-002-001` in #2 §2.5; `XC-008-001` in #8 §1.7.3); #16 §3.2.5 back-prop prose confirmed landed (OBS-1, stress-test run 2, May 18, 2026) — FULLY CLOSED. ERR-017-001 added May 12, 2026 (Event System #17 PASS 2 review — `DOMAIN_TAG_EVENT_LEDGER` allocation back-prop into #16 §3.4); fully resolved May 15, 2026 — #16-side allocation landed May 14, 2026 (`0x15` in #16 §3.4 v1.0.1) and #17-side `[CROSS-PENDING]` → `[CROSS]` promotion landed in #17 §1.0.1 patch revision May 15, 2026 (literal value inlined across §3.4.2 / §3.10 / §1.4 / §2.4.4 / §7.5 D9 / §8.1.4 / §8.3.4 / §8.4 / §9.2 Q10 / §9.3 R3 / Appendix B / Appendix D). ERR-018-001 added May 13, 2026 and resolved same day at outline level (Performance Optimization #18 `outline-detailed.md` v1.1 inverts KD-3 — #18 owns trace pipeline, #16 retains record format / regression scenarios / emission constraints; section-number citations corrected). ERR-018-002 through ERR-018-011 added May 14, 2026 from PASS-1 adversarial review of #18 section files v0.1 (4 H + 6 M findings); all resolved in v0.2 fix pass (May 14, 2026). ERR-018-012 through ERR-018-018 added May 14, 2026 from PASS-2 adversarial review of #18 section files v0.2 (2 H + 5 M findings tracing primarily to PR #59 + PR #60 parallel-branch merge collisions); all resolved in v0.3 fix pass (May 14, 2026) — #18 section files at v0.3. ERR-002 and ERR-003 remain open. ERR-003-001 through ERR-003-004 added June 10, 2026 (Collision System #3 implementation AR-7 adversarial review — force-conversion calibration, FROM_BEHIND normal convention, same-team stumble gap, candidate-counted pair valve); ERR-003-005 and ERR-003-006 added same day from the AR-8 follow-up sweep (inverted approach gate in §3.3 impulse response; FROM_BEHIND shadowed by the shoulder predicate); all six spec-and-code patched and CLOSED June 10, 2026. ERR-004-003 through ERR-004-005 added June 10, 2026 (First Touch #4 implementation AR-7 adversarial review — §3.3.2 IncomingDir sign inversion, agent-anchored interception proximity, vacuous DEFLECTION alignment gate); ERR-004-003 and ERR-004-004 spec-and-code patched and CLOSED same day; ERR-004-005 documented-open (model observation, gate retained per spec). ERR-004-006 added June 10, 2026 (AR-8 follow-up sweep — §5.10 VS-001 hand-calc used an additive below-reference velocity modifier contradicting normative §3.2.3) — spec and test patched and CLOSED same day. ERR-017-002 added June 12, 2026 (constraint-only Publish/Subscribe overload triple — CS0111, event-system production assembly never compiled; found by the first-ever full-tree compile on the dotnet CI gate) — spec §3.2.1/§3.2.2 and code patched and CLOSED same day. ERR-016-004 and ERR-016-005 added June 15, 2026 from the `src/deterministic-sim/` implementation adversarial review (ERR-016-004 H: `Skip()` advanced `RngCursor` but not the determinism-relevant `ActionOrdinal`, breaking RNG branch-safety; ERR-016-005 M: `SnapshotCodec.Encode` hashed payload-only instead of the §3.2.3 chained header‖payload digest, with the golden-corpus suite reconstructing the preimage by hand so the divergence was untested) — both code-patched and CLOSED same day; regression tests added.
**Raised During:** Pass Mechanics Spec #5 pre-Section 3 cross-spec audit; Decision Tree Spec #8 BLK-001

---

## Error Index

| ID | Title | Severity | Files Affected | Status |
|----|-------|----------|---------------|--------|
| ERR-001 | `IBallPhysicsCallback` fragments a single operation into four methods | Major | 2 | Closed — fixed in First_Touch_Spec_Section_4_v1_1.md |
| ERR-002 | `StringIDs` papers over an undesigned event bus with the wrong solution | Moderate | 1 | Open — low priority, fix at convenience |
| ERR-003 | `PerformanceContext` violation mandate imposes governance with no Stage 0 benefit | Moderate | 10 | Open — low priority, fix at convenience |
| ERR-004 | `IPossessionManager` and `IFirstTouchEventQueue` interface against unspecified systems | Major | 4 | Closed — fixed in First_Touch_Spec_Section_4_v1_1.md |
| ERR-005 | `KickType` enum encodes caller intent into Ball Physics (eliminated by design decision) | Major | 2 | Closed — resolved during audit |
| ERR-006 | `Ball.ApplyKick()` / `KickType` referenced in Ball Physics §8 but never defined in §3.1.11 | Critical | 2 | Closed — resolved in Ball_Physics_Spec_Section_3_1_v2_5.md |
| ERR-007 | `KickPower`, `WeakFootRating`, `Crossing` absent from `PlayerAttributes` | Critical | 1 | Closed — resolved in Agent_Movement_Spec_Section_3_5_v1_3.md |
| ERR-008 | `BallState` has no `PossessingAgentId` field; `ApplyKick()` amendment references it incorrectly | Critical | 2 | Closed — Option B adopted; possession external to BallState; resolved in Ball_Physics_Spec_Section_3_1_v2_5.md |
| ERR-009 | `PassThroughGround` / `PassThroughAerial` are redundant `KickType` values | Minor | 1 | Closed — resolved during audit; through passes use `PassGround`/`PassLofted` |
| ERR-010 | Shot Mechanics §1.1 refers to Decision Tree as Spec #7 — canonical number is #8 | Minor | 1 | ✅ Closed — Fixed in shot-mechanics/section-1.md v1.2 (March 6, 2026); part of comprehensive audit renumbering cascade |
| ERR-011 | `SpatialHashGrid.Query()` ignores radius parameter — always returns fixed 3×3 neighbourhood | Major | 1 | ✅ Closed — Fixed in Collision_System_Spec_Section_3_v1_1.md (March 5, 2026) |
| ERR-012 | First Touch §7 refers to Decision Tree as Spec #7 (5 occurrences) | Minor | 1 | ✅ Closed — Fixed in first-touch/section-7.md v1.1 (March 5, 2026) |
| ERR-012-001 | `DOMAIN_TAG_POSITIONING_AI` allocation + Phase B/C block (originally proposed `0x16…0x1B`; shifted to `0x17…0x1C` May 16, 2026 after #10 took `0x16`) needed in #16 §3.4 | Medium | 1 | ✅ Resolved May 18, 2026 — `DOMAIN_TAG_POSITIONING_AI = 0x17` allocated in #16 §3.4 v1.0.5; §6.1 `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promoted atomically with #12 `APPROVED`; body-text instances in §1/§2/§3/§4/§8 promoted in v0.3/v0.4 fix passes |
| ERR-012-002 | Decision Tree #8 `section-3-1.md` L716 cites Formation System as "Spec #14" — current #14 is Defensive AI; Formation System is #12 | Minor | 1 | ✅ Closed — Fixed in decision-tree/section-3-1.md v1.1.1 (May 15, 2026); single-token "Spec #14" → "Positioning AI, Spec #12"; approval status preserved |
| ERR-008-001 | Decision Tree #8 §3.2 `PitchGeometry` pseudocode class uses centered origin `(0,0) = centre of pitch` with X:−52.5–+52.5m/Y:−34–+34m — contradicts CLAUDE.md + Ball Physics #1 §1.2 corner-origin; all goal constants wrong | High | 1 | ✅ Resolved May 18, 2026 — `section-3-2.md` v1.3: class rewritten to corner-origin (0,0,0); all `Vector2` goal constants replaced with `Vector3` using correct values; citation corrected to §1.2 and Appendix C; XC-GEOM-01 verification note added |
| ERR-008-002 | DT #8 §2.2.5 `MatchContext.BallZone` is a single shared field documented "from own goal line" — unsatisfiable for both teams; implementation consumed home-perspective zone for away agents (all zone modifiers inverted; away in-range shots ×0.10) | High | 3 | ✅ Resolved June 11, 2026 — §2.2.5 field note (home-perspective; normative consumption is per-team derivation from `BallPosition.x`), §3.2.1.3 consumption note; `DecisionContextAssembler.cs` v1.2 + `PitchGeometry.cs` v1.1 + `UtilityScorer.cs` v1.2 |
| ERR-008-003 | DT #8 §3.4.5 line-depth pseudocode adjusts `adjustedSlotY` — Y is the touchline axis in the corner-origin system; formula also lacks the team sign. Implementation copied the Y form verbatim (latent: Stage 0 depth pinned 0.5) | Medium | 2 | ✅ Resolved June 11, 2026 — §3.4.5 pseudocode rewritten to team-signed X; `TacticalContext.cs` v1.1 |
| ERR-008-004 | DT #8 §3.4.2 PassingStyle table cell `DRIBBLE 0.9 [GT]` under DIRECT contradicts §3.4.4 prose ("neutral under all three styles") and the §3.4.7 catalogue (no such constant) | Low | 1 | ✅ Resolved June 11, 2026 — table cell corrected to 1.0 (prose + catalogue + implementation agree) |
| ERR-008-005 | DT #8 §3.4.6 gates press urgency on `PossessionState.OPPONENT` — no such enum member (§2.2.5 enum is absolute HOME/AWAY/CONTESTED); implementation literalised it as `== AWAY_TEAM`, inverting urgency for away agents | Medium | 2 | ✅ Resolved June 11, 2026 — §3.4.6 reworded to the derived perspective flag; `DecisionContext.OpponentHasBall` added (assembler-derived); `TacticalModifierResolver.cs` v1.1 |
| ERR-008-006 | DT #8 §3.1.3.4 CROSS gate tests "AgentPosition.x in WIDE_ZONE" — wide channels are touchline-relative (Y axis), and WIDE_ZONE is declared in no constant table; gate unimplementable at Stage 0 | Low | 1 | ⚠ Documented-open June 11, 2026 — SPEC-DEVIATION NOTE at `OptionGenerator.DerivePassType` (CROSS classified from range + facing angle; `Crossing` attribute doc-noted unconsumed); WIDE_ZONE declaration is a Stage 1 spec task |
| ERR-008-007 | DT #8 allocates FM-DT-09 twice: §3.1.1.3 possession-uncertainty warning AND §3.5.9 unknown-ActionType dispatch failure | Low | 1 | ✅ Resolved June 11, 2026 — §3.5.9 row renumbered FM-DT-14 (next free ID); FM-DT-09 stays with §3.1.1.3; `DecisionTreeConstants.cs` v1.2 |
| ERR-008-008 | DT #8 §3.7.2 row 5 lists only HOLD/MOVE as continuous, leaving DRIBBLE/PRESS/INTERCEPT in EXECUTING pending a completion signal that no Stage 0 system emits (agents would freeze after first movement dispatch); no DT→executor cancel entry point exists for the §3.6.3 action-change path | Medium | 2 | ✅ Resolved June 11, 2026 — §3.7.2 Stage 0 deviation note (all movement-routed actions continuous; PASS/SHOOT hold EXECUTING; executor self-cancel via Pass #5 FM-08/§3.8.5); `DecisionTreeStateMachine.cs` v1.1 |
| ERR-008-009 | DT #8 §3.1.9.2 tags `DRAG_APPROX = 0.3 s⁻¹` as `[CROSS — Ball Physics #1 §3.x]` — #1 models quadratic drag and declares no such constant; value is a DT-side calibration, so [CROSS] violates the verbatim-copy rule (citation also names no real section) | Low | 2 | ✅ Resolved June 11, 2026 — retagged [EST] with derivation note in §3.1.9.2 and `UtilityWeights.cs` v1.2 |
| ERR-008-010 | DT #8 §3.4.7 / §3 completion summary claim 23 constants but list 22 rows (`PRESS_URGENCY_FACTOR` double-counted across the tactical and dispatch groups); §3.2.7.2 also claims pressing modifiers live in `UtilityWeights.cs`, contradicting §3.4.7 "exclusively in TacticalWeights.cs" | Low | 2 | ✅ Resolved June 11, 2026 — tallies corrected to 22 (16+6); file-rule contradiction resolved in favour of §3.4.7; `TacticalWeights.cs` v1.1 header |
| ERR-008-011 | DT #8 §3.1.4.3 pseudocode offsets goal posts along X (`GoalCentre + Vector2(±3.66, 0)`) — the goal line runs along Y at fixed X; §3.2.1.4 PitchGeometry (post-ERR-008-001) has the correct form | Low | 1 | ✅ Resolved June 11, 2026 — §3.1.4.3 corrected to Y ± 3.66 |
| ERR-007-001 | Perception #7 §4.6 forced refresh re-ran the full pipeline, double-advancing cross-heartbeat recognition-expiry/scheduler/latency state out of the 10 Hz cadence (§4.6.2 mandates resetting only the triggering entity) — premature eviction + off-cadence shoulder checks; `FilteredView` depended on whether a refresh fired (determinism hazard) | High | 3 | ✅ Resolved June 13, 2026 (impl AR-3 H-1) — all three mutations gated behind `!forcedRefresh`; new side-effect-free `IsConfirmed`/`IsBlindSideConfirmed` reads; dead `ResetObserver` removed. `PerceptionSystem.cs` v1.4, `RecognitionLatencyTracker.cs` v1.3, `ShoulderCheckScheduler.cs` v1.2 |
| ERR-007-002 | Perception #7 §3.0 Step 1 truncated the spatial-hash query to the first `MaxAgents+1` entries BEFORE de-duplication (ball never deduped) — multi-cell straddle could drop a unique agent from perception | Medium | 1 | ✅ Resolved June 13, 2026 (impl AR-3 M-1) — dedup (agents + ball) across the full raw query before any cap; `id ≥ MaxAgents` dropped at source. `PerceptionSystem.cs` v1.4 |
| ERR-007-003 | Perception #7 §3.3.4 `DeterministicHash` returned `Mathf.Abs(h)` — `Math.Abs(int.MinValue)` throws (latent ~1-in-2³² crash) and a negative hash made caller `% N` (L_rec noise / shoulder jitter) out-of-range | Low | 4 | ✅ Resolved June 13, 2026 (impl AR-3 L-cluster) — `h & 0x7FFFFFFF`; bundled: possession multiplier constant (FR-CS-016), FoV doc. (Window-close `>`→`>=` proposal WITHDRAWN — broke SC-002; expiry is the last active tick by design.) `RecognitionLatencyTracker.cs` v1.3, `ShoulderCheckScheduler.cs` v1.2, `PerceptionConstants.cs` v1.3, `FovCalculator.cs` v1.1 |
| ERR-015-006 | Attacking AI #15 §1/§2/§3/§4 retain 7 stale `[CROSS-PENDING]` tags on `DOMAIN_TAG_ATTACKING_AI` after ERR-015-001 declared resolved; §9 checklist falsely claims "0 `[CROSS-PENDING]` remain" | Medium | 4 | ✅ Resolved May 18, 2026 — promoted all 7 hits to `[CROSS: #16 §3.4]` in §1 (4 instances), §2 FR-AT-005, §3 constant table, §4 §4.6 prose; v0.3 version-history rows added to all four section files |
| ERR-015-007 | Attacking AI #15 §3.13 Step 4 pseudocode `if isStable: continue` neither sets `agent.assignedRole` for stable agents nor counts stable RUNNERs/WEAK_SIDEs toward `runnerCount`/`weakSideCount` — non-stable agents are then assigned as if no stable holders existed, so MAX_RUNNERS and the single-WEAK_SIDE gate are enforced only retroactively by the §3.11 invariant pass | Medium | 1 | ✅ Resolved June 15, 2026 (impl AR-4) — folded into the ERR-015-009 single-pass rewrite: §3.13 / §3.3 Step 4 now evaluate every agent in one EntityId-ascending pass and count on the **committed** (post-hysteresis) role, so stable/retained RUNNERs and WEAK_SIDEs always seed the cap. Supersedes the initial two-pass patch (the single-pass form subsumes it). `RoleAssigner.cs` v1.2 |
| ERR-015-009 | Attacking AI #15 §3.12/§3.13/§3.3 use `isStable()` (dwellCounter ≥ ATTACK_DWELL_TICKS) as an evaluation gate (`if isStable: retain; continue`) — once an agent's role has been held `ATTACK_DWELL_TICKS` ticks it is never re-evaluated, so the `candidateDwell` transition machinery can never observe a newly-preferred role and the role is **permanently locked** for the rest of the possession (a SUPPORT_BALL agent stays SUPPORT_BALL after the ball moves 60 m away). Shared spec + implementation defect | High | 1 | ✅ Resolved June 15, 2026 (impl AR-4 H-1) — removed the is-stable short-circuit. Role-assignment is now a single always-evaluate pass; the §3.12 anti-thrash hysteresis lives entirely in `update()`'s `candidateDwell` (retains `currentRole` until a *different* candidate persists the dwell window). `isStable()` retagged diagnostic-only in spec + code. `RoleAssigner.cs` v1.2, `AttackHysteresis.cs` v1.2, `AttackHysteresisState.cs` v1.1, `section-3.md` §3.3/§3.12/§3.13 |
| ERR-015-010 | Attacking AI #15 §2.2.6 `AttackIntentSnapshot.intents` typed `ReadOnlySpan<AttackIntent>` — illegal as a field of a non-ref `readonly struct` (won't compile). The implementation worked around it with a raw `AttackIntent[] Intents` (Length = SQUAD_SIZE = 22) + a separate `IntentCount`, but the XML doc claimed "length IntentCount", so a consumer iterating `Intents.Length` reads stale/default entries past the valid count, and the raw array leaks the orchestrator's mutable buffer | Medium | 1 | ✅ Resolved June 15, 2026 (impl AR-5 M-1) — replaced with a bounded `ArraySegment<AttackIntent>` view (zero-alloc; `.Count` == valid count; consumers iterate that). Spec §2.2.6 struct + prose patched (`ReadOnlySpan` → `ArraySegment`). `AttackIntentSnapshot.cs` v1.1 |
| ERR-015-011 | Attacking AI #15 §2.3 / FR-AT-008 state a loose ball (carrier `null`/`-1`) MUST yield an empty directive, and `AttackingSnapshot`'s own doc says `-1` is treated as OUT_OF_POSSESSION — but `AttackingAITick.Tick` gated only on `PositioningAI.GetPhase()` and never checked `BallCarrierEntityId`. An IN_POSSESSION tick with carrier `-1` ran the pipeline against an undefined `BallCarrierPosition` (run-target origin §3.4, support radius §3.5) | Medium | 1 | ✅ Resolved June 15, 2026 (impl AR-5 M-2) — added the FR-AT-008 loose-ball guard in `Tick` (after the phase gate, before pool build): `BallCarrierEntityId < 0` → `SetEmpty` + return. `AttackingAITick.cs` v1.2 |
| ERR-015-008 | Attacking AI #15 §3.13 Step 10 pseudocode emits `validThroughTick = currentTick + 1`, contradicting the §2.2.2 `AttackIntent` data-structure contract ("equals currentTick") and the staleness rule (consumer treats `vt < currentTick` as stale) | Medium | 1 | ✅ Resolved June 15, 2026 (impl AR-4 M-1) — `section-3.md` Step 10 corrected to `validThroughTick = currentTick` with an inline §2.2.2 cite. Implementation `AttackingAITick.PublishIntents` already stamps `currentTick`; intra-spec contradiction removed |
| ERR-016-003 | Domain tag registry (#16 §3.4) silent gaps at `0x18` and `0x1C` — no `_RESERVED_0xNN_` placeholder rows; `0x18` orphaned when GK shifted to `0x1D`; `0x1C` block-end margin never documented | Medium | 1 | ✅ Resolved May 18, 2026 — `deterministic-sim/section-3.md` v1.0.6: `_RESERVED_0x18_` and `_RESERVED_0x1C_` placeholder rows added to §3.4 domain-tag table; v1.0.6 version-history row added |
| ERR-016-004 | Deterministic Sim #16 §3.2.5 `DeterministicRngService.Skip()` advanced only `RngCursor`, but draw values key on `ActionOrdinal` (bumped only by `Reserve`); `RngCursor` is not a hash input. A branch that took `Skip` instead of `Reserve` ended the draw-site evaluation with a different `ActionOrdinal` and desynced **every subsequent draw** on the stream — branch-safety silently broken. Implementation defect | High | 1 | ✅ Resolved June 15, 2026 (impl AR) — `Skip()` now advances `ActionOrdinal` (one consumed action) **and** `RngCursor`, and rejects an open reservation (`ERR_DS_RNG_BUDGET_MISMATCH`; signature `void`→`ushort`). `DeterministicRngService.cs` v1.3; new `DeterministicSimAdversarialRegressionTests` lock parity + open-reservation rejection |
| ERR-016-005 | Deterministic Sim #16 §3.2.3 `SnapshotCodec.Encode` computed `SHA-256(payload)` only — not the spec chained digest `SHA-256(0x12‖schema‖tick‖prevSnapshotDigest‖envFpDigest ‖ 0x11‖payload)`. The "digest chain" was not chained (altering an earlier snapshot left every later digest valid) and ignored the domain tags + header the golden corpus D-07 pins. `SerializeCanonicalCorpusTests` reconstructs the D-04..D-07 preimages by hand and never calls `Encode`, so the production divergence was untested (encode-not-catch pattern). Implementation + test-coverage gap | Medium | 2 | ✅ Resolved June 15, 2026 (impl AR) — `Encode` now builds the §3.2.3 header‖payload preimage (TransformBlock, no combined-buffer alloc); new `EnvironmentFingerprint.ComputeDigest()` supplies the 32-byte envFp slot. Bundled doc/semantic fixes (mirroring the perception L-cluster precedent): env mutation-guard over-claim corrected (readonly enforces immutability), `RngStreamState` `DrawIndex`/`BudgetRemaining` docs, `SaveManager.Load` storage-vs-schema error split, `TickOrchestrator` codec-owns-chain + AI-no-op doc. `SnapshotCodec.cs` v1.2, `EnvironmentFingerprint.cs` v1.1, `RngStreamState.cs`, `SaveManager.cs` v1.4, `TickOrchestrator.cs` v1.2; regression tests added |
| ERR-016-006 | Deterministic Sim #16 §4.2.2/§4.6.1 `SaveManager.Load` read only the payload and discarded the on-disk header, so replay's `ValidateHeader` / `ValidatePrevDigest` / cursor step-7 ran against a caller-supplied placeholder — the digest chain could not be verified across a process restart (a save→quit→reload could not detect a tampered/foreign snapshot). Compounded: `ReplayEngine` step 3 dereferenced `header.Fingerprint` (null on a disk-loaded header) → NRE. Implementation defect (foundation AR H-1/H-2/M-3) | High | 2 | ✅ Resolved June 16, 2026 — new `SaveManager.ReadHeaderBytes` + `Load(tick, headerOut, payloadOut)` overload reconstruct the header from disk (purely additive; on-disk format and the old payload-only `Load` delegate unchanged), so the chain is now verifiable on load; `ReplayEngine` step 3 fails closed (`ERR_DS_REPLAY_ENV_MISMATCH`) on a null fingerprint/live instead of NRE. `SaveManager.cs` v1.5, `ReplayEngine.cs` v1.3. Round-trip + chain test deferred to the Stage-0 file-I/O test-enablement follow-up (existing `DeterministicSimSaveLoadTests` file-I/O cases are `Assert.Ignore` at Stage 0) |
| ERR-016-007 | Deterministic Sim #16 §4.8 the on-disk snapshot header does NOT serialize the `EnvironmentFingerprint`, yet the fingerprint digest is part of the §3.2.3 digest preimage AND the §4.2.2 step-3 env-validation input. A snapshot reloaded in a fresh process therefore cannot recompute/verify its own digest or run step 3 (the disk-loaded header carries a null fingerprint; ERR-016-006 makes that fail closed). Wire-format gap (foundation AR M-4) | Medium | 2 | ⚠ Documented-open June 16, 2026 — fixing requires serializing the fingerprint (or its digest) into the on-disk header, which is a `SNAPSHOT_SCHEMA_VERSION` bump and would disturb the pinned `serialize-canonical-corpus.md` D-04/D-07 vectors + the #17 boot-wiring smoke digest; deferred to a gate-verified change (no local SDK in the remote review environment). Until then cross-process replay env-validation is blocked by design |
| ERR-016-008 | Deterministic Sim #16 §3.2.5 `DeterministicRngService.Reserve(stream, 0)` sets `BudgetRemaining = 0`, indistinguishable from "no reservation open", so a subsequent `Reserve`/`Skip` is not rejected (open-state is overloaded onto the count field); and `Skip(count)` ↔ sibling `Reserve(count)` branch parity is enforced only by caller convention — a mismatched `count` silently desyncs `RngCursor` (Tier-A snapshot state) and surfaces as a spurious HardDesync rather than a caught budget error. Implementation defect (foundation AR M-1/M-2) | Medium | 1 | ⚠ Documented-open June 16, 2026 — proposed fix: a dedicated `IsReserved` flag independent of the count (or reject `count <= 0` in `Reserve`), and derive `Skip`/`Reserve` budgets from the draw-site registration rather than the caller. Deferred with ERR-016-007 for a gate-verified RNG change |
| ERR-016-001 | Phantom interface risk in Deterministic Simulation §4.2 | Medium | 1 | ✅ Mitigated — §4.2 reclassified as non-normative sketches in v0.7 fix pass |
| ERR-016-002 | EntityId no-reuse cross-spec constraint not back-propagated to specs #2 and #8 | Medium | 3 | ✅ FULLY RESOLVED May 18, 2026 — (1) `XC-002-001` added to Agent Movement #2 §2.5 (v1.1.1, May 6, 2026); (2) `XC-008-001` added to Decision Tree #8 §1.7.3 (v1.1.1, May 6, 2026); (3) #16 §3.2.5 prose updated from "filed for back-propagation" to "back-propagated to #2 §2.5 and #8 §1.7.3" (confirmed landed per OBS-1 stress-test run 2, May 18, 2026). CLAUDE.md OPEN ISSUES entry removed. |
| ERR-017-001 | `DOMAIN_TAG_EVENT_LEDGER` allocation needed in Deterministic Simulation #16 §3.4 domain-tag table | Medium | 2 | ✅ FULLY RESOLVED. (1) #16-side May 14, 2026: `DOMAIN_TAG_EVENT_LEDGER = 0x15` allocated in #16 §3.4 (v1.0.1 patch revision); §8.3.1 #17 row promoted to `complete`. (2) #17-side May 15, 2026 (§1.0.1 patch revision): `[CROSS-PENDING]` → `[CROSS]` promotion completed across §3.4.2 / §3.10 / §1.4 / §2.4.4 / §7.5 D9 / §8.1.4 / §8.3.4 / §8.4 / §9.2 Q10 / §9.3 R3; Appendix B byte streams and Appendix D glossary now carry the literal value `0x15`. |
| ERR-017-002 | Event System #17 §3.2.1/§3.2.2 specified three `Publish<T>`/`Subscribe<T>` overloads distinguished ONLY by generic constraint (`IEventA`/`IEventB`/`IEventC`) — illegal C# (CS0111: constraints are not part of a method signature); `EventBus.cs` and five spec `EventBusStub.cs` files implemented it verbatim, so the event-system production assembly never compiled | High | 8 | ✅ RESOLVED June 12, 2026 (same day; found by the first-ever compile on the dotnet CI gate, `tools/dotnet-ci/`). Spec §3.2.1/§3.2.2 patched to a single `where T : struct` method with cached tier-marker dispatch (section-3.md v1.0.2); code: `EventBus.cs` v1.9, new `EventTierCache.cs` v1.0, `CosmeticChannel.cs` v1.9 (`SubscribeFromBus` seam), 5× `EventBusStub.cs` merged to a single forwarder. Call sites unchanged; FR-EVT-009a exactly-one-marker contract enforced at the entry point. Adjacent boot-order fix: `EventRegistry.EnsureInitialized()` (v1.5) — `EventOrdinalCache<T>` reads never triggered the seeded-row cctor. |
| ERR-017-003 | Event System #17 §3.2.1 `EventBus.Publish<T>` enforces the registered producer phase only under `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` (a `Debug.Assert`); in a release/certification build a Tier A/B event published from the wrong phase is accepted, and `PublishAuthoritative` stamps the FM-017-002 sort key with the *actual* current phase rather than the registered producer phase. Determinism holds within one build config, but a debug run and a release run of the same scenario can produce different canonical orderings/digests if any producer is mis-phased — defeating the cross-environment digest contract. Implementation defect (foundation AR, event-system) | Medium | 1 | ⚠ Documented-open June 16, 2026 — proposed fix: promote the producer-phase comparison to an unconditional guard (the data — `GetProducerPhaseIndex(ordinal)` — is already available). Deferred (not applied blind): the change is digest-sensitive (it gates which publishes reach the ledger and could alter the pinned #17 boot-wiring smoke digest), and the remote review environment has no .NET SDK to run the gate. Apply with CI verification |
| ERR-018-001 | Performance Optimization #18 `outline-detailed.md` cites Deterministic Simulation #16 sections by stale numbers / non-existent name (`#16 §7 regression scenarios`, `#16 §5 canonical save format`, `#16 §8 trace channels`) | Medium | 1 | ✅ Resolved at outline level — May 13, 2026 (same day as filing). `outline-detailed.md` v1.1 (a) inverts KD-3 (Spec #18 owns the trace pipeline; Spec #16 retains authority over canonical record format §3.2.4.1, regression scenarios §5, and determinism-of-emission constraints / veto authority over tick-pipeline trace points §3.1), and (b) corrects every `TBD-NORMATIVE`-marked #16 section-number citation against current `deterministic-sim/section-*.md`. Rationale for inversion: trace channels are an observability concern, not a determinism concern; mirrors KD-4 (#19 owns testing infrastructure, consumes #16 scenarios). New FR-PO-058a in §3.8.3 enforces determinism-of-emission for every #18-emitted trace point. Section files drafted from v1.1 will not inherit the drift. Architectural concern (re-anchor vs invert) is closed; section-file authoring still required to faithfully implement inverted KD-3 (FR-PO-058a in §3.8.3, #16-owner sign-off audit in §5.7, record-format binding in §3.8.4). |
| ERR-018-002 | `[HotPathAllocExempt]` attribute cited in #18 as "declared in Spec #20 §3" but does not exist in `code-standards/` | High | 5 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §3.7.5 declares governance identifier in #18; Spec #20 §3 cited as policy authority only; C# attribute deferred to Stage 0+1 |
| ERR-018-003 | MUST/MAY conflict between FR-PO-067 (§2.2.9) and §3.4.4 on baseline-reproducibility re-run | High | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §3.4.4 "MAY" → "MUST" |
| ERR-018-004 | Three-way stage-of-resolution contradiction on +5% threshold: FR-PO-031 "Stage 0+1" vs §7.5 D9 "Stage 1" vs §7.1 Stage 0+1 deliverable | High | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §7.5 D9 "Stage 1" → "Stage 0+1" |
| ERR-018-005 | Channel registry schema absent from Appendix F; §3.8.2 "Stage 0 declares schema" obligation unmet; F.1/F.2/F.4 reference `perf.budget`/`perf.alloc` channels without registry backing | High | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): Appendix F.0 channel registry schema added |
| ERR-018-006 | Hot-path allocation budget = 0 bytes/tick tagged `[GT]` in §3.10 instead of `[FIXED]` — not a designer-tunable value | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §3.10 and §8.4 tags updated `[GT]` → `[FIXED]` |
| ERR-018-007 | Three Spec #19 body-text citations missing `TBD-NORMATIVE` tag and absent from §9.4.1 blocker list: §3.4.3 ("per Spec #19 §3.4.3"), §3.3.5 ("parallel Spec #19 §6.1"), §3.9.5 ("Spec #19 §3.1") | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): TBD-NORMATIVE added to all three citations; §9.4.1 blocker list extended |
| ERR-018-008 | §3.9.1 ±20% `[EST]`→`[GT]` promotion tolerance untagged; not in §3.10 constants catalogue (CLAUDE.md requires source tag on every constant) | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): `[GT]` tag added inline; §3.10 and §8.4 rows added |
| ERR-018-009 | FR-PO-070 (Stage 0 MUST) requires `tools/run-perf-local.sh` to invoke `tools/budget-auditor.py`, which is a Stage 0+1 deliverable per §7.1 — bootstrapping contradiction | Medium | 2 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): FR-PO-070 stage column updated to "Stage 0 (manual) / Stage 0+1 (automated)" with qualifier note |
| ERR-018-010 | Appendix F.1 `N=100` captures `[GT]` and Appendix F.5 1% flake-rate threshold are governance constants absent from §3.10 catalogue; F.5 threshold also untagged | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §3.10 and §8.4 rows added; F.5 threshold tagged `[GT]` |
| ERR-018-011 | `SPEC_INDEX.md` row 18 still shows `IN PROGRESS`; #18 §9.4 prematurely declares `IN REVIEW` (canonical registry contradicted per CLAUDE.md "SPEC_INDEX.md is the canonical source of truth") | Medium | 3 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): SPEC_INDEX.md row 18 updated to `IN REVIEW`; CLAUDE.md and file-manifest.md updated atomically |
| ERR-018-012 | Appendix F has two `### F.0 Channel Registry Schema` sections (lines 231 and 258) with conflicting field sets (13 fields vs 7 fields, different names — `owning_subsystem` vs `subsystem_owner`, `inside_tick_pipeline`+`sign_off_log_ref` vs `emission_veto_required`) | High | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): kept canonical 13-field F.0; merged in `perf.budget`/`perf.alloc`/`perf.trace` anchor rows from the duplicate as Stage 0 illustrative entries. Root cause: PR #59 + PR #60 parallel-branch merge of independent ERR-018-005 fixes |
| ERR-018-013 | `section-3.md` §3.10 Constants Catalogue has three pairs of duplicate-constant rows: ±20% promotion tolerance (565↔572), N=100 dashboard window (566↔573), 1% flake threshold (567↔574) | High | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): deleted the three v0.1 rows; kept the v0.2 rows with richer rationale. Root cause: same PR #59 + PR #60 merge collision as ERR-018-012 |
| ERR-018-014 | Seven section files (section-2 / 3 / 5 / 7 / 8 / 9 + appendices) carry duplicate v0.2 version-history rows sandwiching the v0.1 row | Medium | 7 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): consolidated each pair into a single v0.2 row carrying the union of fix-list notes; v0.3 row appended below |
| ERR-018-015 | `section-1.md` header `Last Updated: May 13, 2026` is stale vs its own v0.2 row dated May 14, 2026 (every other section file's header is May 14) | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): header updated to `May 14, 2026 (v0.3 PASS-2 adversarial-review fix pass)` |
| ERR-018-016 | `section-3.md` §3.5.2 Shot Mechanics example conflates the +5% per-PR gate (vs measured pre-PR baseline) with the ±20% `[EST]`→`[GT]` promotion tolerance from §3.9.1 — invokes the +5% gate against an un-promoted spec-time anchor | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): example rewritten to apply ±20% promotion tolerance at first capture, then +5% (or per-spec tighter override) for subsequent per-PR captures |
| ERR-018-017 | FR-PO-019 levels `MAY` but its statement embeds an unconditional MUST ("manifest ID and seed MUST be recorded the same way") — same structural shape as ERR-018-003 | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): split into FR-PO-019 (MAY: cross-scenario profiling is permitted) and FR-PO-019a (MUST: manifest ID and seed MUST be recorded per FR-PO-016) |
| ERR-018-018 | §3.7.5 pre-specifies a C# attribute signature (`Method | Constructor` targets, `string rationale` constructor argument) at spec-time without a specified consumer — phantom-interface trap per CLAUDE.md "Interface Design Principle" (ERR-001 / ERR-004 hazard) | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): §3.7.5 deferred concrete C# signature to Stage 0+1 alongside §7.5 D2 alloc-tracker pin; retained governance contract (rationale, sign-off, source-level marker) which is signature-independent |
| ERR-013-001 | Pressing AI #13 requires a back-prop into Decision Tree #8 §2.2.6 to add `PressDirective?` field to `TacticalContext`. Option B selected. | Medium | 2 | ✅ **Resolved May 17, 2026** — `decision-tree/section-2-1-to-2-2.md` v1.1.1: nullable `PressDirective?` field added to `TacticalContext` struct (null at Stage 0; #13 writes at Stage 1+; DT reads for PRESS utility §3.2.7). |
| ERR-013-002 | Pressing AI #13 requires `PRESS_TRIGGERED` channel registration in Event System #17 §3.10 channel registry. Channel emitted when a `PressDirective` becomes non-empty (non-trivial press fires). | Low | 1 | Open (Stage 1) — filed May 17, 2026 from #13 section-files v0.1. Non-blocking for #13 Stage 0 spec text per KD-11 ("no #17 channels at Stage 0"). Lands at Stage 1 first commit per #18 Appendix F.0 / §7.2. |
| ERR-013-003 | Pressing AI #13 requires `PRESS_DISENGAGED` channel registration in Event System #17 §3.10 channel registry. Channel emitted when a `PressDirective` returns to all-`HOLD_SHAPE` after a non-trivial press. | Low | 1 | Open (Stage 1) — filed May 17, 2026 from #13 section-files v0.1. Non-blocking for #13 Stage 0 spec text per KD-11. Lands at Stage 1 first commit per #18 Appendix F.0 / §7.2. |
| ERR-013-004 | Stale "Fatigue System #13" reference at `decision-tree/section-3-1.md` L753 — but #13 is Pressing AI. | Minor | 1 | ✅ **Resolved May 17, 2026** — one-token patch: "Fatigue System #13" → "Pressing AI #13" at `decision-tree/section-3-1.md` L753. |
| ERR-013-005 | `DOMAIN_TAG_PRESSING_AI = 0x19` allocation needed in Deterministic Simulation #16 §3.4. | Medium | 1 | ✅ **Resolved May 17, 2026** — allocated in `deterministic-sim/section-3.md` v1.0.3 (`0x17` reserved for #12, `0x18` for #11, `0x19` for #13); #13 §6.1 `[CROSS-PENDING]` → `[CROSS]` atomically. |
| ERR-013-007 | Pressing AI #13 requires `GetPhase(TeamId)` as a Stage 1 accessor on Positioning AI #12. | Medium | 2 | ✅ **Resolved May 17, 2026** — declared in `positioning-ai/section-4.md` §4.5.1 v0.3 patch as Stage 1 publication commitment. |
| ERR-013-008 | Pressing AI #13 requires `GetLine(EntityId)` elevated from Stage 1+ to Stage 1 on Positioning AI #12. | Medium | 2 | ✅ **Resolved May 17, 2026** — declared in `positioning-ai/section-4.md` §4.5.1 v0.3 patch; `GetLine` elevated Stage 1+ → Stage 1. |
| ERR-013-009 | Pressing AI #13 §3.1.2 `BACKWARD_PASS` dotted the pass direction against `attackingDirection` (the **pressing** team's), but "backward" is backward for the team **in possession**, which attacks the opposite goal. The trigger therefore fired on the possessing team's *forward* pass (home/away inversion class — AR-3 implementation review). It also did not exclude a pressing-team passer. | High | 2 | ✅ **Resolved June 15, 2026** — `pressing-ai/section-3.md` v0.4 §3.1.2: pseudocode + worked example use `-attackingDirection` (possessing team's forward) and add an own-team-passer guard; implementation `TriggerEvaluator.cs` v1.3 matches; tests T-U-002 re-derived + new own-team-passer guard test. |
| ERR-013-010 | Pressing AI #13 §3.4 `receiverProgressionGain` dotted against `attackingDirection` (the **pressing** team's), rewarding receivers retreating toward their own goal as most threatening — same inversion as ERR-013-009. | High | 2 | ✅ **Resolved June 15, 2026** — `pressing-ai/section-3.md` v0.4 §3.4: formula + worked example use `-attackingDirection`; implementation `CoverShadowSelector.cs` v1.3 matches; T-U-031 fixture re-derived in the corrected frame. Zone/third frames (§3.8/§3.9) unchanged — those correctly use the pressing team's direction. |
| ERR-020-001 | Code Standards #20 §4.2 `[CROSS]` mirror example uses ALL_CAPS field name (`PHYSICS_TICK_HZ`) — contradicts §3.2.3 PascalCase rule for `[CROSS]` constants. | Minor | 2 | ✅ **Resolved May 22, 2026** — `code-standards/section-4.md` v1.0.1: mirror field renamed `PHYSICS_TICK_HZ` → `PhysicsTickHz`; XML doc updated with spec+section citation. `src/CLAUDE.md` v1.4: discrepancy note updated with ERR-020-001 reference. |
| ERR-021-005 | Dismarking AI #23 back-prop: `TeamTactic` gains `DismarkIntensity` (`Off = 0` identity) + Appendix B canonical-order row (after `MarkingOrientation`); `WriteTeamTactic` coverage + `SNAPSHOT_SCHEMA_VERSION` bump land with #23's wiring stage | Medium | 2 | ✅ Resolved July 10, 2026 — filed and landed atomically with #23 `APPROVED`: `tactical-instructions/section-2.md` v0.5 field row + `appendices.md` v0.5 Appendix B append |
| ERR-021-006 | Build-Up Structures #24 back-prop: `TeamTactic` gains `BuildUpStructure` (`None = 0` identity) + Appendix B row (after `DismarkIntensity`) | Medium | 2 | ✅ Resolved July 10, 2026 — filed and landed atomically with #24 `APPROVED`: same v0.5 pair as ERR-021-005 |
| ERR-021-007 | Positional Rotations #25 back-prop: `TeamTactic` gains `RotationFreedom` (`Off = 0` identity) + Appendix B row (after `BuildUpStructure`) | Medium | 2 | ✅ Resolved July 10, 2026 — filed and landed atomically with #25 `APPROVED`: same v0.5 pair as ERR-021-005 |
| ERR-012-007 | Dismarking AI #23 back-prop: #12 `SlotComposer` pipeline gains the dismark offset stage between spacing and pitch clamp (order pinned by FR-DM-008; identity no-op at `Off`) | Medium | 1 | ✅ Resolved July 10, 2026 — `positioning-ai/section-3.md` v0.6 new §3.7.1 (combined #23/#24 stage order cited from #23 §4.2 / #24 §4.2) |
| ERR-012-008 | Build-Up Structures #24 back-prop: #12 `SlotComposer` pipeline gains the build-up overlay stage between `ContextModifier` and spacing + per-team `BuildUpZoneState` classifier state (identity no-op at `None`) | Medium | 1 | ✅ Resolved July 10, 2026 — `positioning-ai/section-3.md` v0.6 §3.7.1 |
| ERR-012-009 | Positional Rotations #25 back-prop: #12 contract amendment — `RotationController` runs before slot composition, and `AgentPositioningData.SlotIndex` is no longer immutable after `SeedFromFormation` (the `RotationController` is its sole post-seed writer; single-writer rule per #25 §4.4) | Medium | 1 | ✅ Resolved July 10, 2026 — `positioning-ai/section-3.md` v0.6 §3.7.1 (numbers ERR-012-004..006 deliberately skipped — soft-reserved by the June-13 dotnet-CI quarantine adjudication cluster, whose ERR-012-003 citation is already live in section-3.md v0.5) |
| ERR-008-012 | Dismarking AI #23 back-prop: #8 §3.2 `UtilityScorer` gains the FM-DM-03 marked-pass-target multiplier row in the external tactical-multiplier product, applied before the single final clamp (identity ×1.0 at `Off`) | Medium | 1 | ✅ Resolved July 10, 2026 — `decision-tree/section-3-2.md` v1.5 §3.2.2.1 back-prop anchor note; #23 owns formula/constants/tests |
| ERR-008-013 | GK/Heading #11/#10 integration: #8 gains a DT-emitted `SAVE` action (ordinal 7) — the goalkeeper save the #11 `SaveIntent` doc always anticipated the DT committing. Supersedes the `MatchEngine` heuristic save trigger. Off-ball-branch-only, gated on a new `TacticalContext.SaveAvailable` fact (set only under the opt-in `EnableGkHeading` flag); emitted as the SOLE off-ball option so selection is robust; `PlayerTacticActionMultiplier` exempts SAVE (its #21 tables are 7-wide) | Medium | 1 | ✅ Resolved July 23, 2026 — see the ERR-008-013 detailed section; `decision-tree/section-2.md`/`section-3-1.md`/`section-3-2.md`/`section-3-5.md` notes; code landed (`ActionType.cs` v1.1, `OptionGenerator`/`UtilityScorer`/`ActionDispatcher`/`DecisionTree`/`IDtSaveDispatch`, `MatchEngine.cs` `HostSaveDispatch`) |
| ERR-024-001 | Build-Up Structures #24 Appendix A v0.2 keyed every overlay row to lane values no slot occupies (fullbacks recorded `LH`/`RH` in every family table, not wide L/R; central mids `C`, not LH/RH) — the whole FR-BU-007 catalogue was a structural no-op; the PASS-1 M-3 "correction" checked lane geometry, not the recorded `DefaultLane` key values | High | 3 | ✅ Resolved July 10, 2026 (T0 implementation) — `build-up-structures/appendices.md` v0.3 + `section-3.md` v0.3 re-keyed to the recorded values (magnitudes/intents unchanged); `BuildUpOverlayCatalogue.cs` v1.0 implements the corrected keys; regression test locks non-zero own-third coverage in every family |
| ERR-022-001 | Living World #22 back-prop: `DOMAIN_TAG_LIVING_WORLD = 0x1E` + `SubsystemOrdinals.LivingWorld = 80` (first entry of the off-pitch 80–99 band) allocation needed in Deterministic Simulation #16 §3.4 for the `world.text` / `world.arcs` sub-streams. | Medium | 1 | ✅ Resolved July 22, 2026 — the `0x1E` / `80` allocation landed in code with #22's slice-3 wiring (`DeterministicSimConstants` / `SubsystemOrdinals`); the #16 §3.4 spec-text row + this ERR were filed retroactively (the code back-prop had preceded the doc back-prop). Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump. |
| ERR-027-001 | Squad/Player Data Layer #27 back-prop: `DOMAIN_TAG_PLAYER_DATABASE = 0x1F` + `SubsystemOrdinals.PlayerDatabase = 81` allocation needed in Deterministic Simulation #16 §3.4 (the `RosterGenerator` RNG stream, KD-5). | Medium | 1 | ✅ Resolved July 22, 2026 — allocated in `deterministic-sim/section-3.md` §3.4 (`0x1F`, next after `DOMAIN_TAG_LIVING_WORLD = 0x1E`); the code allocation (`DeterministicSimConstants.DOMAIN_TAG_PLAYER_DATABASE` / `SubsystemOrdinals.PlayerDatabase`) landed with #27 T0; #27 Appendix A `[CROSS]` cross-cite confirmed. Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump. |
| ERR-028-001 | Player Progression & Lifecycle #28 back-prop: promote `_RESERVED_0x20_` → `DOMAIN_TAG_PLAYER_PROGRESSION = 0x20` + `SubsystemOrdinals.PlayerProgression = 82` in Deterministic Simulation #16 §3.4 (the per-club `player-progression.regen` regen/newgen RNG stream, siteId `player-progression.regen`, `entityId = clubId`; FR-PG-020 / KD-3). | Medium | 1 | ◑ Spec-text promoted July 23, 2026 at #28 section-file approval — `deterministic-sim/section-3.md` §3.4 promotes the former `_RESERVED_0x20_` placeholder to the `DOMAIN_TAG_PLAYER_PROGRESSION = 0x20` row (aging/decline/growth is a pure integer projection and registers no stream — `0x20` covers regen generation only). **Like ERR-030-001 (spec-text-first), this row PRECEDES the code:** the code const (`DeterministicSimConstants.DOMAIN_TAG_PLAYER_PROGRESSION` / `SubsystemOrdinals.PlayerProgression`) + the per-club RNG-stream registration land at **#28 T2** with the first regen (registering a stream with zero draw sites now would be the phantom-surface class FR-LW-031 avoids, the `world.arcs` precedent). Pure namespace promotion; no `DETERMINISM_DIGEST_VERSION` bump. Fully resolves when the T2 code const lands. |
| ERR-029-001 | Training System #29 determinism note: confirm at #29 section-file approval whether `_RESERVED_0x21_` / `SubsystemOrdinals.Training = 83` is promoted (FR-TR-008 / KD-6). | Low | 0 | ✅ RESOLVED July 23, 2026 at #29 section-file approval — **NO promotion.** #29 was authored + APPROVED and confirmed **fully deterministic**: conditioning / training-fatigue / growth-input are pure integer projections, and per-player variation is a deterministic function of the player's own attributes, so #29 registers **no** RNG stream. Unlike ERR-028-001 (`0x20`/#28, whose regen is a genuine draw site), #29 has no #29-owned stochastic outcome — growth flows through #28's deterministic curve; injury variation is #41's. Promoting `0x21` to a named tag with a zero-draw stream would be the phantom-surface class FR-LW-031 forbids (`world.arcs` precedent). `deterministic-sim/section-3.md` §3.4 (v1.0.10) updates the `_RESERVED_0x21_` row rationale to record this; the reservation **stands** (no code const, no new row, no `DETERMINISM_DIGEST_VERSION` bump). A future stochastic training extension would promote it at that first draw site. |
| ERR-030-001 | Season & Competition Loop #30 back-prop: `DOMAIN_TAG_SEASON_LOOP = 0x22` + `SubsystemOrdinals.SeasonLoop = 84` allocation needed in Deterministic Simulation #16 §3.4 (the season RNG sub-stream, siteId `season-loop.season-events`; FR-SN-027 / KD-5). | Medium | 1 | ◑ Spec-text reserved July 22, 2026 at #30 section-file approval — `deterministic-sim/section-3.md` §3.4 gains the `DOMAIN_TAG_SEASON_LOOP = 0x22` row (v1.0.8) + the two reserved-pending-promotion placeholders `_RESERVED_0x20_`/`_RESERVED_0x21_` (#28/#29, roadmap §6 contiguous block; #30 reached the catalogue first as Wave 1). **Unlike ERR-022/027-001 (code-first), this row PRECEDES the code:** the code const (`DeterministicSimConstants.DOMAIN_TAG_SEASON_LOOP` / `SubsystemOrdinals.SeasonLoop`) + the RNG-stream registration land at **#30 T2** with the first draw site (the FR-SN-013a quick-sim round-resolution model) — registering a stream with zero draw sites now would be the phantom-surface class FR-LW-031 avoids (the `world.arcs` precedent). Pure namespace reservation; no `DETERMINISM_DIGEST_VERSION` bump. Fully resolves when the T2 code const lands. |
| ERR-030-002 | Season & Competition Loop #30 back-prop (at Injuries & Medical #41 approval): the KD-2 day-advance tick order (`season-competition-loop/section-3.md` §3.3 `RunWorldTickInFixedOrder`) enumerated null seams for #28/#29/#33 only (FR-SN-034, authored before #41). #41 needs a per-day world-tick step (recovery countdown + occurrence draw). | Low | 1 | ✅ Resolved July 23, 2026 at #41 approval — the tick order gains an **injuries null seam as step 4** (after #28/#29 so the occurrence-risk assembly reads the day's updated training-fatigue/condition; before the live `WorldStore.AdvanceDay()` tick), and FR-SN-034's enumeration + the "documented positions" prose extend to #41. Doc-only re-pin of a documented position (no interface, no code — the seam is empty until #41 T2 wires `AdvanceMedicalDay`); the world-floor byte-identity (FR-SN-026) is unaffected since the seam is null. |
| ERR-030-003 | Season & Competition Loop #30 back-prop (at Club Finances & Economy #40 approval): the season-boundary roll (`season-competition-loop/section-3.md` §3.5 `RollToNextSeason`, FR-SN-029/031) needs a finance-settlement step; FR-SN-031 reserved an insertion point for #43 promotion/relegation only. | Low | 1 | ✅ Resolved July 23, 2026 at #40 approval — the boundary roll gains a **finance-settlement null seam at step (b')**, positioned **after** the (a') #43 promotion/relegation insertion point (budget depends on post-promotion division) and **before** (c) regenerate; FR-SN-031 now enumerates both insertion points. Doc-only re-pin of a documented position (no interface, no code — the seam is empty until #40 T2 wires `SettleFinances`); the transform stays a pure function of `SeasonState + nextSeed` (FR-SN-029 restartable contract preserved). |
| ERR-030-004 | Season & Competition Loop #30 back-prop (at Transfers, Contracts & Negotiation #31 approval): the KD-2 day-advance tick order (`season-competition-loop/section-3.md` §3.3 `RunWorldTickInFixedOrder`) enumerated null seams for #28/#29/#33/#41 only (FR-SN-034, authored before #31). #31's deep-tier daily negotiation/rival-bid processing needs a world-tick step. | Low | 1 | ✅ Resolved July 23, 2026 at #31 approval — the tick order gains a **transfers null seam as step 5** (after injuries, before the live `WorldStore.AdvanceDay()` tick, which becomes step 6), and FR-SN-034's enumeration + the "documented positions" prose extend to #31. A **deep-tier position reservation** (the ERR-030-002 #41 precedent): minimal #31 transfers are command-driven (`SubmitBid`), so the seam is **empty even after #31 T-phase minimal**; it fills at the deep tier. Doc-only re-pin (no interface, no code); the world-floor byte-identity (FR-SN-026) is unaffected (null seam). No #16 change — #31 is draw-free, so `_RESERVED_0x23_`/85 stays reserved. |
| ERR-030-006 | Season & Competition Loop #30 back-prop (at Staff & Backroom #34 approval): the KD-2 day-advance tick order (`season-competition-loop/section-3.md` §3.3 `RunWorldTickInFixedOrder`) enumerated null seams for #28/#29/#33/#41/#31 only (FR-SN-034, authored before #34). #34's deep-tier daily candidate-pool / in-flight-hiring processing needs a world-tick step. | Low | 1 | ✅ Resolved July 23, 2026 at #34 approval — the tick order gains a **staff null seam as step 6** (after transfers, before the live `WorldStore.AdvanceDay()` tick, which becomes step 7), and FR-SN-034's enumeration + the "documented positions" prose extend to #34. A **deep-tier position reservation** (the ERR-030-002 #41 / ERR-030-004 #31 precedent): #34's scaffold projections are pull-based (threaded into #29/#41 when their inputs are built), so the seam is **empty even after #34 T-phase scaffold**; it fills at the deep tier. Doc-only re-pin (no interface, no code); the world-floor byte-identity (FR-SN-026) is unaffected (null seam). No #16 change — #34's scaffold is draw-free, so `_RESERVED_0x26_`/88 stays reserved. **ERR-030-005 is soft-reserved by #31** (its deferred `RequestRosterCommit` build), so #34 takes 006. |
| ERR-040-001 | Club Finances & Economy #40: `_RESERVED_0x29_` / `SubsystemOrdinals.ClubFinances = 91` reservation in Deterministic Simulation #16 §3.4 (roadmap §6 off-pitch block). | Low | 1 | ✅ Resolved July 23, 2026 at #40 section-file approval — `deterministic-sim/section-3.md` §3.4 (v1.0.12) gains the `_RESERVED_0x29_` placeholder row, **RESERVED not promoted** (the #29 `_RESERVED_0x21_` precedent): #40's minimal tier is a pure integer `budget = f(finalTablePosition, prizeMoney)` projection with no draw, so it registers no stream, and a named tag with a zero-draw stream would be the phantom-surface class FR-LW-031 forbids. Promotes to `DOMAIN_TAG_CLUB_FINANCES = 0x29` at #40 T3's first stochastic sponsorship/revenue draw (keyed on `(clubId, seasonNumber, purpose)`). No code const; no `DETERMINISM_DIGEST_VERSION` bump. |
| ERR-041-001 | Injuries & Medical #41 back-prop: `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` + `SubsystemOrdinals.InjuriesMedical = 92` allocation needed in Deterministic Simulation #16 §3.4 (the `injuries.occurrence` world-tick sub-stream, siteId `injuries.occurrence`, `entityId = playerId`, position-independent keyed draws; #41 KD-1 / §5). | Medium | 1 | ◑ Spec-text allocated July 23, 2026 at #41 section-file approval — `deterministic-sim/section-3.md` §3.4 gains the `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` row (v1.0.11; value `0x2A` per roadmap §6, block skips `0x23`–`0x29` reserved for #31–#40). **Spec-text-first like ERR-030-001** (not code-first like ERR-022/027-001): the code const (`DeterministicSimConstants.DOMAIN_TAG_INJURIES_MEDICAL` / `SubsystemOrdinals.InjuriesMedical`) + the `injuries.occurrence` stream registration land at **#41 T2** with the first draw site (FR-LW-031 — no phantom stream). Pure namespace allocation; no `DETERMINISM_DIGEST_VERSION` bump. Fully resolves when the T2 code const lands. |

---

## ERR-001: `IBallPhysicsCallback` fragments a single operation into four methods

**Severity:** Major
**Detected:** February 19, 2026
**Root Cause:** Interface written by producer (First Touch) to describe what it provides
to Ball Physics, rather than by the consumer (Ball Physics) to describe what it needs.
The four methods encode First Touch's internal `TouchResult` taxonomy into Ball Physics,
creating coupling between two systems that should be independent.

**Problem in detail:**
`IBallPhysicsCallback` defines four methods:
- `OnControlled(agentID, position, velocity)`
- `OnLooseBall(position, velocity)`
- `OnDeflected(position, deflectionVelocity)`
- `OnIntercepted(interceptingAgentID, position, velocity)`

All four do the same physical thing: set ball position and velocity. The method name
encodes why First Touch is calling — which is First Touch's concern, not Ball Physics'.
Ball Physics does not and should not change its behaviour based on which `TouchResult`
produced the call. Teaching Ball Physics about `TouchResult` states via method names
is inverted responsibility.

**Correct approach:**
Single method: `SetBallState(Vector3 position, Vector3 velocity)`
First Touch calls it once with the computed position and velocity regardless of outcome.
Ball Physics applies the state. The `TouchResult` outcome is First Touch's internal
classification and stays there.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `First_Touch_Spec_Section_4_v1_0.md` | §4.5.2 | Remove `IBallPhysicsCallback` interface definition; replace 4-method calls with single `SetBallState(position, velocity)` call in `ApplyTouchResult()`; update §4.5 interface table entry; update flow diagram ASCII art at §4.4 |
| `First_Touch_Spec_Outline_v1_0.md` | Interface contracts table | Remove `IBallControlCallback` row; replace with `SetBallState()` direct call note |

**Version impact:** `First_Touch_Spec_Section_4_v1_0.md` → v1.1

---

## ERR-002: `StringIDs` papers over an undesigned event bus with the wrong solution

**Severity:** Moderate
**Detected:** February 19, 2026
**Root Cause:** Premature optimisation for a system (Event Bus) that has not yet been
designed. The `StringIDs` pattern assumes the Event Bus will dispatch on string keys and
pre-hashes them to avoid runtime allocation. This assumption may be wrong.

**Problem in detail:**
`Master_Vol_4_Tech_Implementation.md` specifies a `StringIDs` static class that
pre-hashes string constants (player names, tactic names) to `int32` at startup:

```csharp
public static class StringIDs {
    public static readonly int TACTIC_GEGENPRESS = Hash("Gegenpressing");
}
```

This pattern only makes sense if the Event Bus dispatches on string keys. If the Event
Bus uses typed event structs (the standard C# pattern: `EventBus.Publish<TEvent>(evt)`),
dispatch is on the type identity — zero strings, zero hashing, zero `StringIDs` class
needed. The `StringIDs` solution solves the wrong problem.

**Correct approach:**
Remove `StringIDs`. Document that the Event Bus will use typed event structs. String
hashing is a last resort for systems that cannot use typed dispatch (e.g., scripting
bridges, serialised network events). Those cases, if they arise, are addressed when
the Event System (Spec #17) is designed.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Master_Vol_4_Tech_Implementation.md` | `StringIDs` section | Remove class definition and example; replace with note: "Event Bus dispatches on typed structs. String-keyed dispatch is not used. String hashing deferred pending Event System Spec #17 design." |

**Version impact:** `Master_Vol_4_Tech_Implementation.md` → minor revision

---

## ERR-003: `PerformanceContext` violation mandate imposes governance with no Stage 0 benefit

**Severity:** Moderate
**Detected:** February 19, 2026
**Root Cause:** Legitimate Stage 4 architecture (`PerformanceContext` modifier chain)
given an enforcement rule that designates direct attribute access as a "specification
violation" — in a stage where the gateway is a passthrough multiplying by 1.0.

**Problem in detail:**
`Agent_Movement_Spec_Section_3_2_v1_0.md` §3.2.1 contains:

> "Any specification that evaluates a player attribute for gameplay purposes MUST call
> `EvaluateAttribute()` or `EvaluateAttributePair()`. Direct access to raw attribute
> values for gameplay calculations is a **specification violation**."

`PerformanceContext` and `EvaluateAttribute()` are correct long-term architecture — in
Stage 4, a rated-18 player performing like a 13 during a bad season is a genuinely
valuable simulation feature. The gateway earns its existence.

The problem is the **violation designation**. Calling `EvaluateAttribute(18)` in Stage 0
returns exactly `18.0f`. The mandate forces every spec (all 20) to import, instantiate,
and route through `PerformanceContext` for a multiply-by-one operation, on pain of
being in violation. This governance overhead is disproportionate to Stage 0 benefit.

**Correct approach:**
Keep `PerformanceContext` and `EvaluateAttribute()` — they are good architecture.
Reword the enforcement rule as a recommendation:

> "Specifications evaluating player attributes for gameplay calculations should route
> through `EvaluateAttribute()`. This enables Stage 4 form, psychology, and career
> modifiers to activate without refactoring downstream formulas."

No violation designation. Compliance by convention, not mandate.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Agent_Movement_Spec_Section_3_2_v1_0.md` | §3.2.1 | Remove bolded violation rule; reword as recommendation |
| `Agent_Movement_Spec_Section_3_5_v1_2.md` | PerformanceContext usage note (`CRITICAL` block) | Remove `CRITICAL` designation; reword as convention note |
| `Agent_Movement_Spec_Section_3_6_v1_1.md` | Any violation reference | Remove violation language |
| `Agent_Movement_Spec_Section_3_7_v1_2.md` | Test descriptions referencing violation | Remove violation language from test pass criteria |
| `Agent_Movement_Spec_Section_4_v1_1.md` | Any violation reference | Remove violation language |
| `Agent_Movement_Spec_Section_6_v1_1.md` | Future extensions referencing enforcement | Remove violation language |
| `Agent_Movement_Spec_Section_9_Approval_Checklist.md` | Any checklist item verifying enforcement compliance | Reword as convention check, not violation check |
| `Agent_Movement_Spec_Appendices_v1_1.md` | Any enforcement reference | Remove violation language |
| `Agent_Movement_Spec_Remaining_Sections_Outline.md` | Any enforcement reference | Remove violation language |
| `First_Touch_Spec_Outline_v1_0.md` | Any PerformanceContext violation reference | Remove violation language |

**Note:** `PerformanceContext` struct definition, `EvaluateAttribute()` method, factory
methods, and all formula usage remain unchanged. Only the enforcement designation is
removed.

**Version impact:** 10 files → minor revision each (single sentence change per file)

---

## ERR-004: `IPossessionManager` and `IFirstTouchEventQueue` interface against unspecified systems

**Severity:** Major
**Detected:** February 19, 2026
**Root Cause:** Interfaces written before the systems they interface with have been
specified. Interfaces written speculatively against undesigned consumers will be
redesigned when the real consumer is specified, making the Stage 0 interface vestigial
or a constraint on the future design.

**Problem in detail:**

**`IPossessionManager`** (First Touch §4.5.4):
The spec notes: *"Implementer: PossessionManager (Spec TBD, Stage 0 stub sufficient)"*
The Stage 0 stub is one line of work. An interface written against "Spec TBD" will
either be replaced when the Possession Manager is specified, or will constrain that
spec's design to fit an interface written without knowing what the system needs to do.

**`IFirstTouchEventQueue`** (First Touch §4.5.5):
A ring buffer interface with capacity 64, connected to Event System (Spec #17, Stage 1).
The Event System has not been designed. The ring buffer capacity (64) and the
`Enqueue(FirstTouchEvent)` method shape are speculative. When Stage 1 Event System is
designed, it will define its own buffering and dispatch requirements — at which point
this interface is either replaced or becomes a constraint.

**Correct approach:**
Remove both interfaces. Replace with direct, minimal Stage 0 implementations:

- Possession: `ball.PossessingAgentId = agentId` (pending BallState amendment ERR-008)
- Event queue: comment stub — *"Event publishing deferred to Stage 1. When Event System
  (Spec #17) is designed, First Touch will implement its consumer interface here."*

Write the interfaces when both sides (First Touch and their consumers) are fully
specified. Do not write an interface when one side is "Spec TBD."

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `First_Touch_Spec_Section_4_v1_0.md` | §4.5.4 | Remove `IPossessionManager` interface; replace possession assignment logic with direct `BallState` field write; update §4.5 interface table; update flow diagram |
| `First_Touch_Spec_Section_4_v1_0.md` | §4.5.5 | Remove `IFirstTouchEventQueue` interface and ring buffer specification; replace with deferred comment stub; update §4.5 interface table |
| `Agent_Movement_Spec_Section_5_v1_1.md` | Any test mocking `IFirstTouchEventQueue` | Remove or replace with stub |
| `Collision_System_Spec_Section_6_v1_1.md` | Any performance reference to event queue | Remove or note as deferred |
| `First_Touch_Spec_Section_6_v1_0.md` | Event queue in performance budget | Remove ring buffer from budget; note as deferred |

**Version impact:** `First_Touch_Spec_Section_4_v1_0.md` → v1.1 (combined with ERR-001 fix)

---

## ERR-005: `KickType` enum encodes caller intent into Ball Physics

**Severity:** Major
**Detected:** February 19, 2026
**Status:** CLOSED — resolved during audit session

**Resolution:**
`KickType` enum eliminated entirely. `Ball.ApplyKick()` signature reduced to physical
parameters only: `ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin,
int agentId, float matchTime)`. The pass type is fully encoded in the velocity and
spin vectors — Ball Physics does not need to know the caller's intent label to simulate
correct aerodynamics. Pass Mechanics maps its `PassType` to physical parameters; that
is its entire job.

**Files affected by resolution:**
- `Ball_Physics_Spec_Section_3_1_Amendment_1_v1_0.md` — drafted without `KickType`
- `Pass_Mechanics_Spec_Outline_v1_0.md` — `KickType` references are outline-only;
  will not appear in Section 3 implementation

---

## ERR-006: `Ball.ApplyKick()` referenced in Ball Physics §8 but never defined in §3.1.11

**Severity:** Critical
**Detected:** February 19, 2026
**Status:** CLOSED — Resolved in Ball_Physics_Spec_Section_3_1_v2_5.md (February 21, 2026)

**Resolution:**
`ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin, int agentId, float matchTime)`
defined at §3.1.11.2. No `KickType` parameter (ERR-005 resolution). Option B possession
model applied (ERR-008 resolution). State transitions to `AIRBORNE` or `ROLLING` on kick;
agent system observes and clears possession on its side.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Ball_Physics_Spec_Section_3_1_v2_4.md` | §3.1.11 | Add §3.1.11.1 label to `CheckPossession()`; add §3.1.11.2 `ApplyKick()` method (no `KickType` per ERR-005 resolution); update table of contents |
| `Ball_Physics_Spec_Section_8_v1_2.md` | §8.3 reference | Update `§3.1.11.2` cross-reference to `§3.1.11.2` (or §3.1.11.3 per final subsection numbering) |

**Version impact:** `Ball_Physics_Spec_Section_3_1_v2_4.md` → v2.5

---

## ERR-007: `KickPower`, `WeakFootRating`, `Crossing` absent from `PlayerAttributes`

**Severity:** Critical
**Detected:** February 19, 2026
**Status:** CLOSED — Resolved in Agent_Movement_Spec_Section_3_5_v1_3.md (February 22, 2026)

**Resolution:**
`KickPower` (1–20), `WeakFootRating` (1–5), and `Crossing` (1–20) added to
`PlayerAttributes` struct. All 9 blocked Pass Mechanics tests (PV-006, WF-001–WF-006,
IT-004) are now unblocked.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Agent_Movement_Spec_Section_3_5_v1_2.md` | §3.5.6 `PlayerAttributes` | Add `KickPower` (1–20), `WeakFootRating` (1–5), `Crossing` (1–20); update struct comment `Consumed by` list; update struct size estimate |

**Version impact:** `Agent_Movement_Spec_Section_3_5_v1_2.md` → v1.3

---

## ERR-008: `BallState` has no `PossessingAgentId` field; `ApplyKick()` amendment references it

**Severity:** Critical
**Detected:** February 19, 2026
**Status:** CLOSED — Option B adopted February 22, 2026. Resolved in Ball_Physics_Spec_Section_3_1_v2_5.md.

**Design Decision: Option B — Possession external to BallState**

Possession is agent state, not ball state. `BallState` is a pure physics struct; adding
`PossessingAgentId` would introduce the only agent reference in Ball Physics, violating
single responsibility. It would also create a synchronisation hazard between two systems
both tracking possession.

**Resolution:**
`ApplyKick()` transitions `ball.State` from `CONTROLLED` to `AIRBORNE` (or `ROLLING`).
The agent system observes this state transition and clears its own possession record.
Agent system is the single source of truth for possession. No `PossessingAgentId` field
added to `BallState`.

Ball_Physics_Spec_Section_3_1_v2_5.md §3.1.11.2 documents this design with full rationale.

---

## ERR-009: `PassThroughGround` / `PassThroughAerial` are redundant `KickType` values

**Severity:** Minor
**Detected:** February 19, 2026
**Status:** CLOSED — resolved during audit session

**Resolution:**
Through passes use the same aerodynamic profile as their non-through equivalents
(`PassGround` and `PassLofted` respectively). The distinction between a through ball
and a regular pass is entirely a Pass Mechanics targeting concern — the receiver
prediction model, lane detection, and lead distance calculation. Ball Physics sees
identical physics profiles. Separate `KickType` values were unnecessary.

The `KickType` enum was subsequently eliminated entirely (ERR-005), making this
resolution moot. Recorded for completeness.

---

## ERR-011: `SpatialHashGrid.Query()` ignores radius parameter — always returns fixed 3×3 neighbourhood

**Severity:** Major
**Detected:** February 23, 2026 (Shot Mechanics Spec #6 §4 cross-spec audit)
**Status:** CLOSED — Fixed in Collision_System_Spec_Section_3_v1_1.md; Query() now uses
dynamic neighbourhood sizing: `cellRadius = Ceil(radius / CELL_SIZE)`. Interim workaround in Shot Mechanics §4.4.1; root cause unfixed

**Root Cause:**

`SpatialHashGrid.Query(Vector3 position, float radius)` accepts a `radius` argument
but never reads it. The implementation unconditionally queries the 3×3 cell neighbourhood
around the query position (covering approximately ±1.5m regardless of the radius
argument passed). This was documented in the Collision System spec as a comment
("not currently used; 3×3 query is always sufficient") but the architectural consequence
for callers using larger pressure radii was not evaluated.

**Problem in detail:**

All three systems that query the spatial hash for pressure detection — Pass Mechanics,
Shot Mechanics, and First Touch — pass `PRESSURE_RADIUS_MAX = 3.0m` to `Query()`. The
call returns only entities within the fixed ±1.5m neighbourhood. Opponents at 1.6–3.0m
are invisible to the pressure model in all three specifications.

**Impact by system:**
- **Pass Mechanics (Spec #5):** `PassErrorCalculator` under-estimates pressure for shots
  taken with opponents at 1.6–3.0m. Passes executed under moderate pressure behave as if
  under no pressure.
- **Shot Mechanics (Spec #6):** Same effect on `ShotErrorCalculator`. Shots under
  moderate defensive pressure are not penalised correctly.
- **First Touch (Spec #4):** Same effect on `FirstTouchPressureEvaluator`. Ball control
  under moderate pressure is over-estimated.

**Interim workaround (applied in Shot Mechanics §4.4.1 v1.3):**

Callers must distance-filter the `Query()` result set after receiving it:

```csharp
List<AgentId> queriedEntities = SpatialHash.QueryRadius(center, PRESSURE_RADIUS_MAX, filter);
List<AgentId> nearbyOpponents = queriedEntities
    .Where(id => Vector3.Distance(center, AgentSystem.GetAgent(id).Position)
                 <= PRESSURE_RADIUS_MAX)
    .ToList();
```

This workaround is correct — the 3×3 neighbourhood is a superset of all entities within
3.0m (a 3.0m radius on 1.0m cells requires at most ±3 cells to capture; the 3×3 returns
±1 cells). **The workaround does NOT fully fix the defect** — opponents at 1.6–3.0m that
fall in cells beyond the ±1 neighbourhood are still missed. However, at normal match
density (22 agents on a 105×68m pitch), the probability of an opponent being at 1.6–3.0m
but outside the 3×3 neighbourhood is low. The workaround reduces the error but does not
eliminate it.

**Correct fix:**

`SpatialHashGrid.Query()` must compute a dynamic neighbourhood based on the radius
parameter:

```csharp
public List<int> Query(Vector3 position, float radius)
{
    int cellRadius = Mathf.CeilToInt(radius / SpatialHashConstants.CELL_SIZE);
    // Query (2*cellRadius+1)² cells instead of fixed 3×3
    for (int dy = -cellRadius; dy <= cellRadius; dy++)
    for (int dx = -cellRadius; dx <= cellRadius; dx++)
    { /* add cells */ }
}
```

For `PRESSURE_RADIUS_MAX = 3.0m` on 1.0m cells: `cellRadius = 3`, query covers 7×7 = 49
cells (vs current 9). Performance impact is negligible at N=22 agents.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Collision_System_Spec_Section_3_v1_0.md` | §3.1.4 `Query()` implementation | Dynamic neighbourhood: `cellRadius = Ceil(radius / CELL_SIZE)`; iterate `(2*cellRadius+1)²` cells |
| `Pass_Mechanics_Spec_Section_4_v1_0.md` | §4.4.1 pressure query | Add interim workaround comment (or remove workaround once Collision System fixed) |
| `First_Touch_Spec_Section_4_v1_1.md` | §4.4 pressure query | Add interim workaround comment |

**Version impact:** `Collision_System_Spec_Section_3_v1_0.md` → v1.1 (when fixed)

---

## ERR-008-002 … ERR-008-011: Decision Tree #8 comprehensive audit (June 11, 2026)

Filed during the comprehensive audit of the Decision Tree #8 spec + its May 29, 2026
implementation (the audit the April 27 approval carved out as a pre-implementation
follow-up; implementation landed first, so the audit ran as a combined document-and-code
review). Full findings, severities, and fix traceability:
`docs/specs/decision-tree/audit-report.md`. Code-side companions: H-1 (assembly never
compiled — static calls to instance executors; the SIXTH consecutive spec with a
structurally dead build surface, and the first where the PRODUCTION assembly was dead),
H-2 (= ERR-008-002), H-3 (= ERR-008-008 vicinity), M-1..M-11, L batch. All spec-side
entries patched in the same commit; ERR-008-006 documented-open (Stage 1 WIDE_ZONE
declaration).

---

## Revision Summary

| Priority | ERR ID | Blocking | Status |
|----------|--------|----------|--------|
| ~~1 — Fix before Section 3~~ | ERR-006, ERR-007, ERR-008 | ~~Yes~~ | ✅ All three closed |
| ~~2 — Fix before approval~~ | ERR-001, ERR-004 | ~~Yes~~ | ✅ Both closed in First_Touch_Spec_Section_4_v1_1.md |
| 3 — Fix at convenience | ERR-002, ERR-003 | No | Open — minor edits to Master_Vol_4 and Agent Movement §3.2 |
| **2 — Fix before Collision System approval** | **ERR-011** | **Yes (blocks Collision System §4 approval)** | **Closed — fixed in Collision_System_Spec_Section_3_v1_1.md (Mar 5, 2026)** |
| 3 — Fix at convenience before Shot Mechanics final sign-off | ERR-010 | No | ✅ Closed — fixed in shot-mechanics/section-1.md v1.2 (March 6, 2026) |
| 3 — Fix at convenience | ERR-012 | No | ✅ Closed — fixed in first-touch/section-7.md v1.1 (March 5, 2026) |

**All critical Shot Mechanics cross-spec audit defects resolved (A1–A7). ERR-011 is a
Collision System defect with an interim workaround applied — it blocks Collision System
Section 3 revision, not Shot Mechanics approval. ERR-010 is a minor documentation
error (Decision Tree spec number) in Shot Mechanics §1.1 — non-blocking on approval.**

---

**v1.4 Changes (Mar 5, 2026):
- ERR-009 (SpatialHash Query) renumbered to ERR-011 to resolve duplicate ID
  conflict with ERR-009 (KickType, closed). ERR-011 now CLOSED.

End of Error Log v1.4**

---

## ERR-012: First Touch §7 refers to Decision Tree as Spec #7 (5 occurrences)

**Severity:** Minor (documentation error; no architectural impact)
**Detected:** March 5, 2026
**Detected During:** First Touch Specification #4 comprehensive audit
**Root Cause:** Same as ERR-010 — First Touch Section 7 was written before the specification
numbering was finalised. Decision Tree was tentatively #7; Perception System was subsequently
inserted at #7, bumping Decision Tree to #8.

**Problem in detail:**
`First_Touch_Spec_Section_7_v1_0.md` references "Decision Tree Spec #7" in 5 locations:
- §7.1.4 body text: "Decision Tree (Spec #7, Stage 1)"
- §7.2.4 body text: "Decision Tree (Spec #7, Stage 1/2 scope)"
- §7.2.4 dependency line: "Decision Tree Spec #7"
- §7.6 dependency map row: "Decision Tree Spec #7 | Intent flag | Stage 1"
- §7.6 dependency map row: "Decision Tree Spec #7 | Intent flag | Stage 2"

**Correct approach:**
Replace all 5 instances of "Spec #7" (referring to Decision Tree) with "Spec #8".

**Status:** ✅ CLOSED — Fixed in `first-touch/section-7.md` (March 5, 2026, First Touch
comprehensive audit remediation).

**Files revised:**

| File | Section | Change |
|------|---------|--------|
| `first-touch/section-7.md` (was v1.0 → v1.1) | §7.1.4, §7.2.4, §7.6 | All "Decision Tree Spec #7" → "Decision Tree Spec #8" |

**Version impact:** `first-touch/section-7.md` → v1.1

---

*End of Spec Error Log v1.5 — April 22, 2026. Add new entries after this line.*

---

## ERR-016-001: Phantom interface risk in Deterministic Simulation Spec #16 §4.2

**Severity:** Medium (architectural discipline; no immediate code impact — Stage 0 spec phase)
**Detected:** May 2, 2026
**Detected During:** Deterministic Simulation Spec #16 drafting (adversarial review + v0.7 fix pass)
**Root Cause:** Same root cause as ERR-001 and ERR-004. §4.2 originally contained normative C#-shaped interface sketches (`IDeterministicRngService`, `IReplayRunner`, etc.) against consumer specs (#17 Event System, #18 Performance Optimization, #19 Testing Strategy) that are all currently `NOT STARTED`. Writing normative interface shapes before the consumer is specified creates phantom interfaces that constrain future design.

**Mitigation applied (v0.7 fix pass):**
§4.2 was reframed as explicitly **non-normative sketches** — the C# shapes are illustrative only. The §4.2.1 *behavior contract* remains normative (determinism in inputs→outputs, byte-idempotent serialization, canonical ordering in Compare output). The note at the top of §4.2 explicitly cites CLAUDE.md's "write interfaces only when both sides are specified" rule and the ERR-001/004 hazard, and prohibits promotion to normative `.cs` interfaces until consumer specs #17/#18/#19 reach at least `IN REVIEW`.

**Status:** ✅ MITIGATED — phantom interface risk contained by non-normative classification. Full resolution requires co-authoring final interface shapes with specs #17/#18/#19.

**Files revised:**

| File | Section | Change |
|------|---------|--------|
| `docs/specs/deterministic-sim/section-4.md` | §4.2 preamble | Added non-normative disclaimer and phantom-interface hazard citation |

---

*End of Spec Error Log v1.6 — May 2, 2026.*

---

## ERR-016-002: EntityId no-reuse cross-spec constraint not back-propagated

**Severity:** Medium (consistency/discipline; latent integrity hazard if specs #2/#8 silently reuse EntityIds during a match)
**Detected:** May 3, 2026
**Detected During:** Deterministic Simulation Spec #16 third-pass adversarial critique (finding M-F)
**Root Cause:** Deterministic Simulation §3.2.5 declares a normative constraint binding two already-APPROVED specs:

> "entity allocators in Agent Movement (#2) and the AI subsystem (Decision Tree #8) MUST guarantee EntityId uniqueness for the lifetime of a match; once an EntityId is despawned it MUST NOT be reassigned."

This is the renumbering-cascade hazard CLAUDE.md flags: a downstream spec adding a normative constraint to upstream specs after they have been approved, without filing reciprocal `XC-` cross-references in those specs. As of May 3, 2026, neither Agent Movement (#2) nor Decision Tree (#8) carries a corresponding `XC-` reference to Deterministic Simulation §3.2.5; the constraint is "floating".

**Problem in detail:**
- Agent Movement #2 was approved Apr 27, 2026.
- Decision Tree #8 was approved Apr 27, 2026 (at draft-level rigor).
- The EntityId no-reuse constraint is necessary for #16's RNG stream isolation and replay parity, but is unenforceable until specs #2 and #8 explicitly carry it.
- Without back-propagation, an implementer of Agent Movement could legitimately recycle a despawned EntityId to a new agent on the same tick. This would silently break per-stream RNG cursor isolation in Deterministic Simulation, manifesting only as a hard desync at replay time.

**Required fix:**
1. Add an `XC-002-NNN` cross-reference in Agent Movement #2 §3 (entity allocator) citing Deterministic Simulation §3.2.5; declare the no-reuse constraint normatively in #2's own constants/contracts.
2. Add an `XC-008-NNN` cross-reference in Decision Tree #8 (subsystem entity allocation, if any) likewise.
3. File the back-propagation as a minor revision of both specs, version-bumped (no behavioral changes; constraint is consistent with how a sane allocator would behave anyway).
4. Once both reciprocal references exist, mark this entry CLOSED.

**Status:** ✅ FULLY RESOLVED — May 18, 2026. All three required steps confirmed complete:
1. Agent Movement #2 §2.5 as `XC-002-001` (v1.1.1, non-behavioral patch) — landed May 6, 2026.
2. Decision Tree #8 §1.7.3 as `XC-008-001` (v1.1.1, non-behavioral patch) — landed May 6, 2026.
3. `docs/specs/deterministic-sim/section-3.md` §3.2.5 prose confirmed updated from "filed for back-propagation" to "back-propagated to #2 §2.5 and #8 §1.7.3" (verified by OBS-1 probe in stress-test Tier A Run 2, May 18, 2026). CLAUDE.md OPEN ISSUES entry removed.

**Files revised:**

| File | Section | Change |
|---|---|---|
| `docs/specs/agent-movement/section-1-2.md` | New §2.5 | `XC-002-001` (EntityId no-reuse). v1.1.1 patch. |
| `docs/specs/decision-tree/section-1.md` | New §1.7.3 | `XC-008-001` (EntityId no-reuse). v1.1.1 patch. |
| `docs/specs/deterministic-sim/section-3.md` §3.2.5 | post-fix prose | Pending: update "filed for back-propagation" line. |

**Version impact:** Patch revision (v1.1 → v1.1.1) of Agent Movement #2 and Decision Tree #8 — no behavioral change; constraint formalizes existing sensible allocator behavior.

---

## ERR-017-001: `DOMAIN_TAG_EVENT_LEDGER` allocation required in Deterministic Simulation #16 §3.4

**Severity:** Medium (cross-spec back-prop; latent if not landed before #17 IN REVIEW)
**Detected:** May 12, 2026
**Detected During:** PASS 2 adversarial review of `event-system/outline-detailed.md` v1.0 (finding 3)
**Root Cause:** Event System #17 §3.4.2 declares the `Events`-phase digest preimage as `SerializeCanonical(DOMAIN_TAG_EVENT_LEDGER ‖ EventLedgerRecord[T])`. This domain-tag entry is normatively owned by Deterministic Simulation #16 §3.4's domain-tag table, but no allocation exists there. There is no documented mechanism by which a downstream spec registers a domain-tag need with #16; the dependency direction (#17 cites #16) makes this a chicken-and-egg.

**Problem in detail:**
- Spec #17 needs a stable numeric `DOMAIN_TAG_EVENT_LEDGER` to commit its FM-017-001 formula to.
- Spec #16 §3.4 currently does not enumerate `EVENT_LEDGER` among its allocated domain tags.
- Without back-prop, #17 cannot reach `APPROVED` (its `[CROSS-PENDING]` constant cannot promote to `[CROSS]`).
- The same hazard class as ERR-016-002 (downstream spec adds normative constraint on upstream after the upstream's review pass).

**Required fix:**
1. At `event-system/outline-detailed.md` reaching IN REVIEW, file a patch to `docs/specs/deterministic-sim/section-3.md` §3.4 domain-tag table allocating `DOMAIN_TAG_EVENT_LEDGER` (next available numeric value in #16's tag-namespace).
2. Update §3.10 constants catalogue in `event-system/outline-detailed.md` (and any drafted §3 section file) to pin the literal value and promote `[CROSS-PENDING]` → `[CROSS]` at the same beat that resolves the citation's `TBD-NORMATIVE` tag (gated on #16 reaching `APPROVED` per KD-2).
3. Once the allocation lands in #16, mark this entry CLOSED.

**Status:** ✅ FULLY RESOLVED.

- **#16-side — May 14, 2026.** `DOMAIN_TAG_EVENT_LEDGER = 0x15` allocated in `docs/specs/deterministic-sim/section-3.md` §3.4 (next value after `DOMAIN_TAG_ENV_FP = 0x14`); §3.5 v1.0.1 patch-revision history entry recorded; §8.3.1 #17 row promoted `pending re-audit → complete` atomically with this resolution; §8 v1.2 version-history entry recorded.
- **#17-side — May 15, 2026 (#17 §1.0.1 patch revision).** `[CROSS-PENDING]` → `[CROSS]` promotion completed and literal value `0x15` inlined across `docs/specs/event-system/`: §3.4.2 prose; §3.10 constants catalogue row + trailing-notes paragraph; §1.4 cross-spec-constants-imported summary; §2.4.4 `EventLedgerRecord` preimage description; §7.5 D9 deferred-decisions row (RESOLVED); §8.1.4 ERR-017-001 row; §8.3.4 imported-constants table (heading renamed `[CROSS]` constants imported); §8.4 constant-provenance summary row; §9.2 Q10 quality-checklist row; §9.3 R3 review-checklist row; Appendix B preamble + B.1 / B.2 / B.3 byte streams (symbolic `DT` replaced with literal `15`); Appendix D glossary row. Section-version histories on §1 / §2 / §3 / §7 / §8 / §9 / appendices each carry a v1.0.1 row recording the patch.

**Files revised at #16 side:**

| File | Section | Change |
|---|---|---|
| `docs/specs/deterministic-sim/section-3.md` | §3.4 constants catalogue | Added `DOMAIN_TAG_EVENT_LEDGER = 0x15` `[FIXED]` row citing ERR-017-001 |
| `docs/specs/deterministic-sim/section-3.md` | §3.5 version history | v1.0.1 patch-revision entry recording the allocation and rationale (no `DETERMINISM_DIGEST_VERSION` bump) |
| `docs/specs/deterministic-sim/section-8.md` | §8.3.1 audit table + §8.5 v1.2 | #17 row promoted to `complete`; ERR-017-001 closure recorded |

**Files revised at #17 side (May 15, 2026; §1.0.1 patch revision):**

| File | Section | Change |
|---|---|---|
| `docs/specs/event-system/section-1.md` | §1.4 | `[CROSS-PENDING]` → `[CROSS]`; literal value `0x15` inlined; ERR-017-001 marked RESOLVED |
| `docs/specs/event-system/section-2.md` | §2.4.4 | `EventLedgerRecord` preimage prose updated to `0x15` / `[CROSS]` |
| `docs/specs/event-system/section-3.md` | §3.4.2, §3.10 + trailing notes | `[CROSS-PENDING]` → `[CROSS]`; literal value `0x15` inlined in formula prose and constants catalogue |
| `docs/specs/event-system/section-7.md` | §7.5 D9 | Deferred-decision row marked RESOLVED with `0x15` |
| `docs/specs/event-system/section-8.md` | §8.1.4 ERR-017-001, §8.3.4 heading + row, §8.4 row | ERR-017-001 RESOLVED; `[CROSS]` table and provenance summary updated to `0x15` |
| `docs/specs/event-system/section-9-approval-checklist.md` | §9.2 Q10, §9.3 R3 | Evidence rows updated to reflect `[CROSS]` promotion and ERR-017-001 RESOLVED |
| `docs/specs/event-system/appendices.md` | Appendix B preamble + B.1 / B.2 / B.3, Appendix D | Byte streams inline literal `15`; glossary row updated to `0x15` / `[CROSS]` |

**Version impact:** Patch revision (`v1.0` → `v1.0.1`) on the #16 side (§3.5) and on the #17 side (sections 1, 2, 3, 7, 8, 9-approval-checklist, appendices). No behavioral change on either side; pure namespace allocation in #16 (catalogue grew; no preimage layout, field width, or hash-input rule changed; no `DETERMINISM_DIGEST_VERSION` bump) and pure tag/value substitution in #17 (no FR text changed, no formula re-derived).

---

## ERR-017-002: §3.2.1/§3.2.2 Publish/Subscribe API specified as constraint-only overloads — illegal C# (CS0111)

**Severity:** High (production assembly never compiled; every claim resting on event-system test execution was unverifiable)
**Detected:** June 12, 2026
**Detected During:** First-ever full-tree compile on the non-certifying dotnet CI gate (`tools/dotnet-ci/`)
**Root Cause:** #17 §3.2.1 declared `Publish<T>(in T evt)` three times, distinguished only by `where T : struct, IEventA/IEventB/IEventC`, asserting "the compiler picks the path at the call site; there is no runtime tier dispatch." C# generic constraints are NOT part of a method signature — overloading on constraints alone is CS0111 in every compiler, including Unity's. The spec passed two adversarial review passes because no reviewer compiled the surface; the implementation (`EventBus.cs`, plus `EventBusStub.cs` in pass-mechanics / shot-mechanics / perception-system / heading-mechanics / goalkeeper-mechanics) reproduced the illegal triple verbatim, so `TacticalDirector.EventSystem` and the five forwarding surfaces never compiled. Eighth instance of the structurally-dead-build-surface class; second (after Decision Tree AR-2 H-1) in a PRODUCTION assembly.

**Resolution (June 12, 2026, same commit):**

1. **Spec:** §3.2.1/§3.2.2 rewritten to ONE `Publish<T>`/`Subscribe<T>` (`where T : struct`) with tier routing via per-closed-type cached marker flags; exactly-one-marker contract (FR-EVT-009a) enforced at the entry point at runtime; §3.2.2 compile-time-mismatch note re-anchored (section-3.md v1.0.2).
2. **Code:** new `EventTierCache<T>` (type-init reflection only; JIT folds the flags to constants — FR-EVT-048 zero-alloc preserved); `EventBus.cs` v1.9 single-method dispatch + tier-contract throw; `CosmeticChannel.cs` v1.9 internal `SubscribeFromBus` seam (public `Subscribe` keeps its `IEventC` constraint) + internal `Publish` constraint relaxed; five `EventBusStub.cs` files merged to a single `where T : struct` forwarder. All call sites compile unchanged.
3. **Adjacent fix surfaced by first execution:** `EventOrdinalCache<T>` is a separate static-generic type, so reading it never triggered `EventRegistry`'s seeded-row static constructor — a Subscribe/Publish of a #17-owned event before anything else touched `EventRegistry` threw `ERR_EVT_UNREGISTERED_ORDINAL`. New no-op `EventRegistry.EnsureInitialized()` called at the EventBus entry points (EventRegistry.cs v1.5).

**Status:** ✅ RESOLVED June 12, 2026.

---

## ERR-010-001: `DOMAIN_TAG_HEADING` allocation required in Deterministic Simulation #16 §3.4

**Severity:** Medium (cross-spec back-prop; latent if not landed before #10 APPROVED)
**Detected:** May 16, 2026
**Detected During:** Section-files v0.1 → v0.2 PASS-1 adversarial-review fix pass (`heading-mechanics/adversarial-review-section-files-v1.md` finding M-1). v0.1 KD-10 / Appendix G / §9.4 OI-001 each claimed the entry was "created during section authoring", but `grep ERR-010 docs/tracking/spec-error-log.md` returned only the long-closed ERR-010 (Shot Mechanics renumbering; March 6, 2026). v0.2 files this row.
**Root Cause:** Heading Mechanics #10 §3.4 + §3.7 route Gaussian and float draws through `DeterministicRngService` (Deterministic Simulation #16 §4.1) keyed on `DOMAIN_TAG_HEADING`. This domain-tag entry is normatively owned by #16 §3.4's domain-tag table, but no allocation exists there yet. Same hazard class and same resolution shape as `ERR-017-001` (Event System #17 / `DOMAIN_TAG_EVENT_LEDGER = 0x15`, closed May 15, 2026).

**Problem in detail:**
- Spec #10 needs a stable numeric `DOMAIN_TAG_HEADING` to commit its three draw-site IDs (`DRAW_SITE_DUEL_TIEBREAK`, `DRAW_SITE_CONTACT_POINT_ERROR`, `DRAW_SITE_TIMING_JITTER`) to.
- Spec #16 §3.4 currently does not enumerate `HEADING` among its allocated domain tags.
- Without back-prop, #10 cannot reach `APPROVED` (its `[CROSS-PENDING]` constant in §3.1 cannot promote to `[CROSS]`).
- Next available numeric slot in #16 §3.4's tag-namespace is `0x16` (verified May 16, 2026: current allocations run `0x10`..`0x15`).

**Required fix:**
1. At `heading-mechanics/SPEC_INDEX.md` row 10 reaching `IN REVIEW`, file a patch to `docs/specs/deterministic-sim/section-3.md` §3.4 domain-tag table allocating `DOMAIN_TAG_HEADING = 0x16` (next available numeric value in #16's tag-namespace). Pure namespace allocation — no `DETERMINISM_DIGEST_VERSION` bump required, per the `ERR-017-001` precedent (#16 §3.5 v1.0.1 patch revision, May 14, 2026).
2. Update §3.1 Master Physical Profile Table in `heading-mechanics/section-3.md` to pin the literal value `0x16` and promote `[CROSS-PENDING]` → `[CROSS]` at the same beat that #16's allocation lands.
3. Once the allocation lands in #16, mark this entry CLOSED.

**Status:** ✅ FULLY RESOLVED.

- **#16-side — May 16, 2026.** `DOMAIN_TAG_HEADING = 0x16` allocated in `docs/specs/deterministic-sim/section-3.md` §3.4 (next value after `DOMAIN_TAG_EVENT_LEDGER = 0x15`); §3.5 v1.0.2 patch-revision history entry recorded. Pure namespace allocation in #16's tag-namespace; no `DETERMINISM_DIGEST_VERSION` bump (catalogue grew; no preimage layout, field width, or hash-input rule changed). Follows the v1.0.1 / ERR-017-001 precedent exactly.
- **#10-side — May 16, 2026 (#10 v0.3 patch revision).** `[CROSS-PENDING]` → `[CROSS]` promotion completed in `heading-mechanics/section-3.md` §3.1 Master Physical Profile Table; literal value `0x16` retained; ERR-010-001 reference updated `pending → RESOLVED`. §1.3 KD-10 wording updated; §1.4 dependency table updated; §8.2 / §8.4 / §9.1 / §9.2 / §9.4 OI-001 status rows all updated. Section-version histories on §1 / §3 / §9 / appendices each carry a v0.3 row recording the patch.

**Files revised at #16 side:**

| File | Section | Change |
|---|---|---|
| `docs/specs/deterministic-sim/section-3.md` | §3.4 constants catalogue | Added `DOMAIN_TAG_HEADING = 0x16` `[FIXED]` row citing ERR-010-001 |
| `docs/specs/deterministic-sim/section-3.md` | §3.5 version history | v1.0.2 patch-revision entry recording the allocation and rationale (no `DETERMINISM_DIGEST_VERSION` bump) |

**Files revised at #10 side (May 16, 2026; v0.3 patch revision):**

| File | Section | Change |
|---|---|---|
| `docs/specs/heading-mechanics/section-1.md` | §1.3 KD-10, §1.4 | Wording updated to reflect RESOLVED filing; #16 anchor pinned |
| `docs/specs/heading-mechanics/section-3.md` | §3.1 | `[CROSS-PENDING]` → `[CROSS]`; literal value `0x16` retained |
| `docs/specs/heading-mechanics/section-8.md` | §8.2, §8.4 | XC-010-004 row marked RESOLVED; #16 row updated |
| `docs/specs/heading-mechanics/section-9-approval-checklist.md` | §9.1, §9.2, §9.4 OI-001, §9.5 | All checklist rows referencing OI-001 / `DOMAIN_TAG_HEADING` checked/RESOLVED |
| `docs/specs/heading-mechanics/appendices.md` | Appendix G | OI-001 status updated to RESOLVED |

**Version impact:** Patch revision (#16 §3.5: `v1.0.1 → v1.0.2`; #10 sections: `v0.2 → v0.3`). No behavioral change on either side; pure namespace allocation in #16 and pure tag-promotion in #10.

---

## ERR-011-001: `DOMAIN_TAG_GOALKEEPER` allocation required in Deterministic Simulation #16 §3.4

**Severity:** Medium (cross-spec back-prop; latent if not landed before #11 APPROVED)
**Detected:** May 16, 2026
**Detected During:** Section-files v0.1 → v0.2 PASS-1 adversarial-review fix pass (`goalkeeper-mechanics/adversarial-review-section-files-v1.md`). Filed at the moment Goalkeeper Mechanics #11 section files v0.2 land and `SPEC_INDEX.md` row 11 flips `NOT STARTED → IN REVIEW`.

**Root Cause:** Goalkeeper Mechanics #11 §3.3 / §3.5 / §3.6 route Gaussian draws through `DeterministicRngService` (Deterministic Simulation #16 §4.1) keyed on `DOMAIN_TAG_GOALKEEPER`. Same hazard class and same resolution shape as `ERR-010-001` (Heading #10 / `DOMAIN_TAG_HEADING = 0x16`, closed May 16, 2026) and `ERR-017-001` (Event System #17 / `DOMAIN_TAG_EVENT_LEDGER = 0x15`, closed May 15, 2026).

**Problem in detail:**
- Spec #11 needs a stable numeric `DOMAIN_TAG_GOALKEEPER` to commit its four draw-site IDs (`DRAW_SITE_HANDLING_NOISE`, `DRAW_SITE_HANDLING_POINT_NOISE`, `DRAW_SITE_DIVE_TIMING_JITTER`, `DRAW_SITE_CROSS_CLAIM_TIEBREAK`) to.
- Spec #16 §3.4 currently does not enumerate `GOALKEEPER` among its allocated domain tags.
- Without back-prop, #11 cannot reach `APPROVED` (its `[CROSS-PENDING]` constant in §3.4 cannot promote to `[CROSS]`).
- **Collision-management policy (KD-7).** Open ERR-012-001 proposes block `0x17…0x1C` for Positioning AI #12 Phase B/C; whichever spec reaches `APPROVED` first takes `0x17`. If ERR-011-001 lands first, the #12 block re-shifts to `0x18…0x1D` (mirroring the May 16, 2026 #10 / #12 shift via ERR-010-001 vs. ERR-012-001). If ERR-012-001 lands first, `DOMAIN_TAG_GOALKEEPER` shifts to `0x1D`. The `[CROSS-PENDING]` tag accommodates either outcome.

**Required fix:**
1. At `goalkeeper-mechanics/SPEC_INDEX.md` row 11 reaching `APPROVED`, file a patch to `docs/specs/deterministic-sim/section-3.md` §3.4 domain-tag table allocating `DOMAIN_TAG_GOALKEEPER`. Numeric value depends on collision-management outcome (`0x17` or `0x1D`). Pure namespace allocation — no `DETERMINISM_DIGEST_VERSION` bump, per ERR-010-001 / ERR-017-001 precedent.
2. Update §3.4.9 in `goalkeeper-mechanics/section-3.md` to pin the literal value and promote `[CROSS-PENDING]` → `[CROSS]` at the same beat that #16's allocation lands.
3. Once the allocation lands in #16, mark this entry CLOSED.

**Status:** ✅ Resolved May 18, 2026 — `DOMAIN_TAG_GOALKEEPER = 0x1D` allocated in #16 §3.4 v1.0.5 (Positioning AI #12 reached APPROVED first and claimed `0x17`; per KD-7 first-to-APPROVED precedent GK shifted to `0x1D`); #11 §3.4.9 `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promoted atomically with #16 back-prop landing.

---

*End of Spec Error Log v1.11 — May 16, 2026.*

---

## ERR-010: Shot Mechanics §1.1 refers to Decision Tree as Spec #7

**Severity:** Minor (documentation error; no architectural impact)  
**Detected:** February 27, 2026  
**Detected During:** Decision Tree Specification #8 Outline v1.1 pre-approval review (BLK-001)  
**Root Cause:** Shot Mechanics Specification #6 was written before the specification
numbering was finalised. At time of authoring, the Decision Tree was tentatively
assigned #7. Perception System was subsequently inserted at #7, bumping Decision Tree
to #8. The Shot Mechanics text was not updated.

**Problem in detail:**  
`Shot_Mechanics_Spec_Section_1_v1_1.md` §1.1 Dependencies section references:
> "Decision Tree Specification #7"

The canonical specification number for the Decision Tree, as recorded in
`PROGRESS.md` (authoritative), `FILE_MANIFEST.md`, and Perception System
Specification #7 §1.1, is **#8**.

This creates an inconsistency that could mislead implementers cross-referencing
Shot Mechanics with Decision Tree documentation.

**Correct approach:**  
Replace all instances of "Decision Tree Specification #7" with "Decision Tree
Specification #8" in `Shot_Mechanics_Spec_Section_1_v1_1.md`.

**Blocking condition:**  
This error is non-blocking on Shot Mechanics approval (the architectural content is
correct; only the number is wrong). However, it **must be closed before**:
1. Shot Mechanics receives final lead developer sign-off, and
2. Decision Tree Specification #8 Section 4 (interface contracts) is written and
   references Shot Mechanics as a dependency by number.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Shot_Mechanics_Spec_Section_1_v1_1.md` | §1.1 Dependencies table, any other references | Replace "Spec #7" with "Spec #8" for Decision Tree |

**Version impact:** No version increment required for minor text correction. Document
in Shot Mechanics changelog when the edit is made.

---

## ERR-018-002: `[HotPathAllocExempt]` cited as declared in Spec #20 §3 but does not exist there

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; option-2 path; Spec #20 not touched).
**Severity:** High (citation of APPROVED spec for content it does not contain — matches CLAUDE.md "fabricated checklist values" hazard class)
**Detected:** May 14, 2026
**Detected During:** PASS-1 adversarial review of Performance Optimization #18 section files v0.1
**Root Cause:** The `[HotPathAllocExempt]` C# attribute is referenced as a key allocation-exemption mechanism in five locations in #18, every one of which treats the attribute as already declared in Spec #20 §3 (APPROVED May 11, 2026). Grep against the entire `code-standards/` folder returns zero hits for `HotPathAllocExempt` or any allocation-exemption attribute. The attribute is not declared in Spec #20.

**Problem in detail:**

Cited locations:
- `section-2.md` FR-PO-053: "exempt via `[HotPathAllocExempt]` (declared in Spec #20 §3, cite-not-redefine per KD-1)"
- `section-3.md` §3.1.2: "exempted via `[HotPathAllocExempt]` (cite Spec #20 §3)"
- `section-3.md` §3.7.5: "exempted via the `[HotPathAllocExempt]` attribute declared in Spec #20 §3"
- `section-8.md` §8.1.4: "§3 `[HotPathAllocExempt]` attribute (cited by §3.7.5, FR-PO-053)"
- `appendices.md` Appendix B: "Exemptions require `[HotPathAllocExempt]` per Spec #20 §3"

§3.7.5 itself hedges with "Coordinate with the #20 author if the attribute is not yet declared … attribute presence to be verified at first `src/` commit," which directly contradicts the surrounding "declared in Spec #20 §3" claim. The spec is simultaneously asserting the attribute exists in #20 and acknowledging it may not.

**Required fix (choose one):**

1. **Update Spec #20 §3** to formally declare the `[HotPathAllocExempt]` attribute with version-history entry and lead-developer re-sign-off (Spec #20 is APPROVED; any spec change requires sign-off per CLAUDE.md). Spec #18 citations then resolve.
2. **Move ownership to Spec #18** — declare the attribute in #18 §3.7 directly; drop the KD-1 cite-not-redefine framing for this case. Update Spec #20's `[HotPathAllocExempt]` row only if/when #20 adopts it.
3. **Tag as `[CROSS-PENDING]`** — treat the attribute name as a cross-spec constant gated on a future Spec #20 patch; file the back-prop expectation here and in #18's body text.

Option (2) has the smallest cross-spec blast radius because #20 is APPROVED and (1) would require re-review.

**Files requiring revision (per resolution path chosen):**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-2.md` | FR-PO-053 | Reword to remove "declared in Spec #20 §3" claim |
| `docs/specs/performance-optimization/section-3.md` | §3.1.2, §3.7.5 | Same |
| `docs/specs/performance-optimization/section-8.md` | §8.1.4 | Same |
| `docs/specs/performance-optimization/appendices.md` | Appendix B | Same |
| `docs/specs/code-standards/section-3.md` (option 1 only) | §3 | Add attribute declaration |

**Version impact:** #18 section-file revision (v0.1 → v0.2). Option (1) additionally bumps Spec #20 (re-review required).

**Resolution (May 14, 2026):** Option (2) applied. `section-3.md` §3.7.5, `section-2.md` FR-PO-053, and `appendices.md` Appendix B all updated. `[HotPathAllocExempt]` declared as Spec #18 §3.7.5 governance identifier. Spec #20 unchanged.

---

## ERR-018-003: MUST/MAY conflict between FR-PO-067 and §3.4.4 on baseline-reproducibility re-run

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; §3.4.4 upgraded MAY → MUST with Stage 0 carve-out).
**Severity:** High (binding-requirement contradiction within the same spec)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review of #18 section files v0.1
**Root Cause:** FR-PO-067 in `section-2.md §2.2.9` states the baseline-reproducibility auditor **MUST** re-run the recorded session manifest. §3.4.4 in `section-3.md` (the implementing mechanics section for that FR) states the validator **MAY** re-run the session. §2 is the binding-requirement section; §3 is the implementing mechanics. The verbs disagree directly on the same action.

**Problem in detail:**

FR-PO-067 (normative MUST): *"The §5.4 baseline-reproducibility auditor MUST re-run the recorded session manifest and confirm the recaptured metric matches within §3.4.3 confidence interval."*

§3.4.4 (mechanics MAY): *"Reproducibility check (Stage 0+1): the validator MAY re-run the session under the recorded seed + fingerprint + platform pin and confirm the captured metric matches within the §3.4.3 confidence interval."*

FR-PO-068 makes failure to re-run a merge-blocking event. The §3.4.4 "MAY" would allow the validator to silently skip the check without triggering FR-PO-068's block.

**Required fix:**

Either upgrade §3.4.4 to "MUST re-run" (aligning §3 with §2's binding requirement), or downgrade FR-PO-067 to SHOULD (aligning §2 with §3's permissive mechanic). FR-PO-068's merge-blocking semantics push toward the MUST resolution.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.4.4 | "MAY" → "MUST" (recommended) |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `section-3.md` §3.4.4 "MAY" → "MUST". FR-PO-067 (MUST) and §3.4.4 (now MUST) are consistent.

---

## ERR-018-004: Three-way stage-of-resolution contradiction on +5% threshold (FR-PO-031 / §7.5 D9 / §7.1)

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; §7.5 D9 re-anchored Stage 0+1 to match FR-PO-031 and §7.1).
**Severity:** High (three locations in the same spec state three different resolution stages for the same governance number)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** The +5% per-PR regression threshold (`[GT]` governance number) has its resolution stage stated three times with three different answers.

**Problem in detail:**

- **FR-PO-031** (`section-2.md §2.2.5`): "`[GT]` pinned at Stage 0+1 §7.5 D9" — implies pin at Stage 0+1.
- **§7.5 D9** (`section-7.md`): "Resolution stage: Stage 1 | Notes: Tie to first-month variance measurement" — explicit Stage 1.
- **§7.1** (`section-7.md`) Stage 0+1 Transition Deliverables: "§3.5.2 +5% threshold re-evaluated against actual baseline variance" — listed as Stage 0+1 deliverable.

The three statements cannot all be true. Either the threshold is pinned/re-evaluated at Stage 0+1 (FR-PO-031 + §7.1) and D9 is wrong, or D9 is correct and FR-PO-031 + §7.1 are wrong.

**Required fix:**

Choose one canonical stage and update all three locations. Recommended: Stage 0+1 (matches FR-PO-031 + §7.1 which jointly outvote D9; matches the operational reality that you can't gate Stage 0+1 CI on a Stage-1 threshold).

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-7.md` | §7.5 D9 | "Stage 1" → "Stage 0+1" (under recommended resolution) |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `section-7.md` §7.5 D9 resolution stage changed from "Stage 1" to "Stage 0+1". All three locations (FR-PO-031, §7.1, §7.5 D9) now consistently state Stage 0+1.

---

## ERR-018-005: Channel registry schema absent from Appendix F; §3.8.2 "Stage 0 declares schema" obligation unmet

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; new **Appendix F.0 Channel Registry Schema** authored with 12 schema fields; §3.8.2 channel-registry bullet rewritten to cite F.0 as the Stage 0 schema deliverable).
**Severity:** High (declared Stage 0 deliverable is missing; channel names used without registry backing)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** §3.8.2 in `section-3.md` explicitly states the channel registry is a Stage 1 deliverable but the **schema** for the registry is a Stage 0 deliverable to be published in Appendix F. Appendix F as written contains only F.1–F.5 dashboard schemas; there is no channel registry schema. Compounding this, F.1, F.2, and F.4 reference channel names (`perf.budget`, `perf.alloc`) as data sources without those channels having registry entries.

**Problem in detail:**

§3.8.2: *"Channel registry. Named channels per subsystem, declared in Appendix F catalogue (Stage 1 deliverable; **Stage 0 declares schema**)."*

Appendix F section headings: F.1 Per-Spec Per-Tick Budget Dashboard, F.2 Per-PR Delta Dashboard, F.3 Milestone-Baseline Trend Dashboard, F.4 Allocation-Tracker Dashboard, F.5 Flake/Determinism Cross-Reference Dashboard. All five are dashboard schemas; none is a channel registry schema. No section in Appendix F defines what fields a channel registry entry carries (channel name, owning subsystem, default verbosity level, sampling rule, sink routing, determinism class, etc.).

**Required fix:**

Author an "Appendix F.0 — Channel Registry Schema" (or "Appendix H — Channel Registry Schema") before F.1, declaring the schema fields per channel entry. Stage 0 deliverable; populated entries are Stage 1.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/appendices.md` | New Appendix F.0 / H | Add channel registry schema headers (channel name, subsystem, verbosity, sampling rule, sink, determinism class) |

**Version impact:** #18 appendices revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** Appendix F.0 "Channel Registry Schema" added to `appendices.md` with full field schema (channel_name, subsystem_owner, verbosity_tier_min, sink_targets, emission_veto_required, record_format, declared_stage) and Stage 0 channel registry table with three seed entries (perf.budget, perf.alloc, perf.trace).

---

## ERR-018-006: Hot-path allocation budget = 0 bytes/tick tagged `[GT]` instead of `[FIXED]` in §3.10

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; §3.10 row re-tagged `[GT]` → `[FIXED]`; §8.4 mirror row updated).
**Severity:** Medium (constant-tag misclassification; implies designer-tunability of an architectural mandate)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** `section-3.md` §3.10 tags "Hot-path allocation budget = 0 bytes/tick" as `[GT]`. Per CLAUDE.md "Constant Tags" table, `[GT]` = "Gameplay-Tuned; Designer sets value; must live in tunable config." The zero-allocation budget is a non-negotiable architectural mandate from CLAUDE.md "When Writing Code: zero-allocation architecture in the game loop" — not a designer-settable value. Tagging it `[GT]` creates a false implication that a game designer could change it.

**Required fix:**

Re-tag as `[FIXED]` ("invariant by project mandate") or remove from the constants catalogue entirely and treat as a pure CLAUDE.md cite. FR-PO-050's "MUST declare allocation budget = 0 bytes per tick" reinforces the non-tunable nature.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.10 Constants Catalogue | "Hot-path allocation budget = 0 bytes/tick" tag `[GT]` → `[FIXED]` |
| `docs/specs/performance-optimization/section-8.md` | §8.4 Constant Provenance Summary | Mirror the tag change |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `section-3.md` §3.10 tag updated `[GT]` → `[FIXED]`; rationale updated to "non-tunable invariant". `section-8.md` §8.4 mirrored.

---

## ERR-018-007: Three Spec #19 body-text citations missing `TBD-NORMATIVE` tag

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; `TBD-NORMATIVE` added to §3.3.5, §3.4.3, §3.9.5; §9.4.1 #19 blocker list extended).
**Severity:** Medium (KD-4 status caveat violated; §9.4.1 blocker list incomplete)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** KD-4 mandates that every Spec #19 citation in #18 carry a `TBD-NORMATIVE` tag because #19 is `IN REVIEW`. §9.4.1 enumerates blocked sections — but three #19 body-text citations are absent from that list and carry no tag.

**Problem in detail:**

1. **`section-3.md` §3.4.3:** *"provisional value 30 samples / 95% CI per Spec #19 §3.4.3 parallel convention"* — no `TBD-NORMATIVE`; not in §9.4.1.
2. **`section-3.md` §3.3.5:** *"selection criteria parallel Spec #19 §6.1 — must support deterministic re-play …"* — no `TBD-NORMATIVE`; not in §9.4.1.
3. **`section-3.md` §3.9.5:** *"owned by Spec #19 §3.1 end-to-end / soak layer for test execution"* — no `TBD-NORMATIVE`; not in §9.4.1.

All three would silently rot if #19's section numbering shifts before #18 is approved.

**Required fix:**

Add `(TBD-NORMATIVE)` parenthetical to each citation and add §3.4.3, §3.3.5, §3.9.5 to §9.4.1's #19 blocker list.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.4.3, §3.3.5, §3.9.5 | Add `TBD-NORMATIVE` tag to each #19 citation |
| `docs/specs/performance-optimization/section-9-approval-checklist.md` | §9.4.1 | Add §3.4.3, §3.3.5, §3.9.5 to #19 blocker list |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `(TBD-NORMATIVE)` added to all three citations in `section-3.md`. `section-9-approval-checklist.md` §9.4.1 #19 blocker list extended with §3.3.5, §3.4.3, §3.9.5.

---

## ERR-018-008: §3.9.1 ±20% promotion tolerance untagged and absent from constants catalogue

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; inline `[GT]` tag at §3.9.1; new ±20% row in §3.10 + §8.4 with rationale).
**Severity:** Medium (untagged constant; CLAUDE.md requires source tag on every constant in every spec)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** `section-3.md` §3.9.1 declares: *"the first Stage 0+1 baseline capture promotes the estimate to a measured value tagged `[GT]` if within ±20% of estimate, or files an `ERR-018-NNN` review finding if not."* The ±20% threshold governs whether a spec's implementation matches its design estimate — a consequential governance number. It carries no `[GT]`/`[EST]`/`[FIXED]` tag and is absent from §3.10's constants catalogue.

**Required fix:**

Add the ±20% threshold to §3.10's table with `[GT]` tag and rationale (e.g., "twice the +5% per-PR threshold for first-measurement variance"). Also add to §8.4 constant-provenance summary.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.9.1 | Append `[GT]` tag to ±20% |
| `docs/specs/performance-optimization/section-3.md` | §3.10 | Add ±20% row with `[GT]` and rationale |
| `docs/specs/performance-optimization/section-8.md` | §8.4 | Mirror row |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `[GT]` tag added inline in `section-3.md` §3.9.1. §3.10 row added: "±20% acceptance tolerance `[GT]`". `section-8.md` §8.4 mirrored.

---

## ERR-018-009: FR-PO-070 (Stage 0 MUST) requires invoking Stage 0+1 tooling

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; option (b) — FR-PO-070 split Stage 0 manual / Stage 0+1 automated; §5.2 activation row and §5.6 traceability row updated).
**Severity:** Medium (FR activation-stage / tooling-availability mismatch)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** FR-PO-070 (`section-2.md §2.2.10`) has activation stage Stage 0 and MUST-level binding: *"`tools/run-perf-local.sh` (Appendix E) MUST invoke the §5.3 schema-conformance auditor and §5.5 loop-tag auditor against `docs/specs/` only."* Appendix E's shell script invokes `python3 tools/budget-auditor.py`, which §7.1 lists as a Stage 0+1 deliverable. At Stage 0 the tool does not exist; the script as written cannot run.

**Problem in detail:**

Appendix E partially acknowledges this: *"`tools/budget-auditor.py` and `tools/perf-harness/run.sh` are Stage 0+1 deliverables (§7.1). At Stage 0 the auditor's behaviour is a manual review against §3.1.2 schema and §3.2.2 loop-tag mandate; the script above is the structure into which the automated implementation will land."* But FR-PO-070's MUST language and "Stage 0" activation do not reflect this caveat.

**Required fix:**

Either (a) move FR-PO-070 to "Stage 0+1" activation stage in §2.2.10 — matching when its tool dependencies exist — or (b) keep at Stage 0 but qualify the MUST to "MUST execute the manual review equivalents of the schema-conformance and loop-tag auditors per §5.3 and §5.5."

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-2.md` | §2.2.10 FR-PO-070 | Move to Stage 0+1, or qualify Stage 0 manual interpretation |
| `docs/specs/performance-optimization/section-5.md` | §5.2 Stage-Gated Activation Table | Update FR-PO-069 … 074 row if FR-PO-070 stage shifts |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** FR-PO-070 stage column updated to "Stage 0 (manual) / Stage 0+1 (automated)" with qualifier note clarifying Stage 0 uses manual audit execution per Appendix E template.

---

## ERR-018-010: Appendix F.1 N=100 and F.5 1% flake-rate thresholds absent from §3.10

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; both values added to §3.10 + §8.4 with rationale; Appendix F.5 inline `[GT]` tag appended).
**Severity:** Medium (governance constants outside the declared constants catalogue; F.5 also untagged)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** §3.10 declares itself the constants catalogue for #18's governance numerics. Appendix F (`appendices.md`) introduces two governance numbers not present in §3.10:

- **F.1:** "per-spec p50/p99 over last **N=100** captures (`[GT]`, pinned at Stage 0+1)."
- **F.5:** "flake rate **> 1%** triggers boundary-defect routing (§5.7.3)." — untagged.

§3.10's evidence-artifact convention says each `[GT]` value's evidence is the section-file path that introduces it; these two values introduce themselves in Appendix F but are not catalogued.

**Required fix:**

Add both values to §3.10 (and §8.4 mirror) with tags and rationale. F.5's threshold needs a tag (`[GT]` likely).

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.10 | Add `N=100 captures` row (`[GT]`, Appendix F.1) and `1% flake-rate threshold` row (`[GT]`, Appendix F.5) |
| `docs/specs/performance-optimization/section-8.md` | §8.4 | Mirror both rows |
| `docs/specs/performance-optimization/appendices.md` | Appendix F.5 | Append `[GT]` tag to "> 1%" |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `section-3.md` §3.10 rows added for N=100 and 1% flake-rate. `section-8.md` §8.4 mirrored. `appendices.md` F.5 "> 1%" tagged `[GT]`.

---

## ERR-018-011: `SPEC_INDEX.md` row 18 not updated; §9.4 prematurely claims `IN REVIEW`

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; option (a) — `SPEC_INDEX.md` row 18 + CLAUDE.md OPEN ISSUES + `file-manifest.md` row 18 all flipped to `IN REVIEW` atomically; §9.3 atomic-update checkbox flipped `[x]` for the `IN PROGRESS → IN REVIEW` transition; `IN REVIEW → APPROVED` flip remains the future atomic update with lead-developer sign-off).
**Severity:** Medium (canonical-registry contradiction; CLAUDE.md says SPEC_INDEX.md is the source of truth on status)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** `section-9-approval-checklist.md` §9.4 declares *"Status: `IN REVIEW` (author-driven flip; lead-developer review pending)."* `SPEC_INDEX.md` row 18 still shows `IN PROGRESS`. CLAUDE.md states: *"SPEC_INDEX.md is the canonical source of truth for spec numbers, folder names, and approval status."* By that rule, the spec is `IN PROGRESS`, regardless of what §9.4 claims. CLAUDE.md OPEN ISSUES entry for #18 also still says "Section files remain stubs," which is no longer accurate.

**Problem in detail:**

§9.3 checklist row *"`SPEC_INDEX.md` status updated atomically with sign-off"* is correctly marked `[ ]` (unchecked) — acknowledging the update hasn't happened. But §9.4's Decision block then asserts `IN REVIEW` as the current status. The §9.4 status claim contradicts both the canonical registry and the unchecked §9.3 checklist row in the same file.

**Required fix:**

Either (a) update `SPEC_INDEX.md` row 18 and CLAUDE.md OPEN ISSUES entry to `IN REVIEW` atomically (the section files are authored — this state would be consistent), or (b) revert §9.4's status claim to `IN PROGRESS` until lead-developer sign-off. The status flip and the registry/CLAUDE.md updates must move together.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/SPEC_INDEX.md` | Row 18 | `IN PROGRESS` → `IN REVIEW` (option a) |
| `CLAUDE.md` | OPEN ISSUES entry for #18 | Update "Section files remain stubs" → "Section files drafted at v0.1; PASS-1 adversarial review filed (ERR-018-002…011); v0.2 fix pass pending"; flip status text to `IN REVIEW` |
| `docs/tracking/file-manifest.md` | #18 rows | Move section files from "stub" to "drafted" |
| `docs/specs/performance-optimization/section-9-approval-checklist.md` | §9.4 (option b alternative) | Revert "IN REVIEW" → "IN PROGRESS" |

**Version impact:** No section-file content revision required; metadata-only across three tracking files (option a). Option b is a one-line §9.4 edit.

**Resolution (May 14, 2026):** Option (a) applied. `SPEC_INDEX.md` row 18 updated `IN PROGRESS` → `IN REVIEW` with changelog entry. `CLAUDE.md` OPEN ISSUES entry for #18 updated to reflect `IN REVIEW` status and v0.2 section files. `file-manifest.md` row 18 updated from "stubs" to "section-1 through section-9-approval-checklist + appendices.md at v0.2".

---

## ERR-018-012: Appendix F has two conflicting `### F.0 Channel Registry Schema` sections

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** High
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` H-1)
**Root Cause:** PR #59 (`claude/fix-performance-specs-J1t5Z`, commit `14c6ba6`) and PR #60 (`claude/review-performance-specs-YHGga`, commit `dd6a87c`) both authored an Appendix F.0 channel-registry schema as fixes for `ERR-018-005`. Both PRs merged into `main` without de-duplication, leaving two `### F.0 Channel Registry Schema` sections in `appendices.md` (lines 231–256 and 258–281) with materially different field sets — 13 fields vs 7 fields, different names (`owning_subsystem` vs `subsystem_owner`, `inside_tick_pipeline` + `sign_off_log_ref` pair vs single `emission_veto_required` boolean, `record_format_version` semver vs `record_format` reference). The §5.7.1 audit hook walks `sign_off_log_ref` — present only in the first schema. The F.1–F.5 dashboards cite `perf.budget` / `perf.alloc` / `perf.trace` channel names — populated only as anchor rows in the second schema.

**Resolution:** Kept the canonical 13-field F.0 (richer, supports §5.7.1 audit hook against `sign_off_log_ref`, declares `record_format_version` semver per KD-11). Merged the duplicate's `perf.budget` / `perf.alloc` / `perf.trace` example rows into the canonical schema as illustrative Stage 0 anchor entries so F.1–F.5 dashboard data-source citations resolve at draft time. Per-subsystem channels (`ai.*`, `physics.*`) remain Stage 0+1 deliverables.

---

## ERR-018-013: `section-3.md` §3.10 has three duplicate-constant rows

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** High
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` H-2)
**Root Cause:** Same PR #59 + PR #60 parallel-branch merge as ERR-018-012. Both branches resolved ERR-018-008 (±20% promotion tolerance) and ERR-018-010 (N=100 dashboard window, 1% flake threshold) by appending rows to §3.10. Merge retained both row sets:

| First (v0.1) row | Duplicate (v0.2) row | Constant |
|------------------|----------------------|----------|
| `[EST]-baseline acceptance tolerance = ±20%` `[GT]` → §3.9.1 | `[EST]→[GT]` promotion tolerance = ±20% `[GT]` → §3.9.1 | ±20% promotion tolerance |
| Dashboard sample window = 100 captures `[GT]` → Appendix F.1 | Per-spec p50/p99 rolling window N = 100 captures `[GT]` → Appendix F.1 | N=100 dashboard window |
| Flake-rate alert threshold = 1% `[GT]` → Appendix F.5 | Flake-rate boundary-defect routing threshold = 1% `[GT]` → Appendix F.5 | 1% flake threshold |

**Resolution:** Deleted the three v0.1 rows; kept the v0.2 rows whose rationale columns are richer. §8.4 mirror table was already correct (v0.1 §3.10 was not mirrored there) — no §8.4 change required.

---

## ERR-018-014: Seven section files carry duplicate v0.2 version-history rows

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** Medium
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` M-1)
**Root Cause:** Same PR #59 + PR #60 merge as ERR-018-012 / 013. Each branch independently authored its own v0.2 version-history row. Merge retained both, producing the pattern `v0.2 (summary) | v0.1 | v0.2 (detailed fix list)` in seven files: `section-2.md`, `section-3.md`, `section-5.md`, `section-7.md`, `section-8.md`, `section-9-approval-checklist.md`, `appendices.md`. (`section-1.md`, `section-4.md`, `section-6.md` were not affected — only one branch touched each.)

**Resolution:** Consolidated each pair into a single v0.2 row carrying the union of fix-list notes — the more detailed (PR #59) text plus any uniquely-stated items from the PR #60 summary. v0.3 row appended below for this fix-pass landing.

---

## ERR-018-015: `section-1.md` header `Last Updated` is stale

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** Medium
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` M-2)
**Root Cause:** `section-1.md` line 4 still reads `**Last Updated:** May 13, 2026` despite the v0.2 row at §1.5 being dated May 14, 2026. Every other section file's header is `May 14, 2026 (v0.2 PASS-1 adversarial-review fix pass)`. The v0.2 PR for section-1 updated §1.5 but missed the header.

**Resolution:** Updated header to `**Last Updated:** May 14, 2026 (v0.3 PASS-2 adversarial-review fix pass)`.

---

## ERR-018-016: §3.5.2 conflates +5% per-PR gate with ±20% `[EST]`→`[GT]` promotion tolerance

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** Medium
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` M-3)
**Root Cause:** §3.5.2 *"Per-spec overrides"* bullet says: *"For example, Shot Mechanics #6 §4.5 already declares a 0.05 ms total budget; deviations larger than 5% from the 0.017 ms estimated cite #6 §4.5 authority, not §3.5.2 default."* The +5% per-PR threshold (§3.5.2 / FR-PO-031) is defined against a **measured pre-PR baseline**. The 0.017 ms is a spec-time `[EST]` anchor, not a captured baseline. Per §3.9.1, the first Stage 0+1 capture promotes `[EST]` → `[GT]` if within ±20%; the +5% gate only activates against promoted `[GT]` baselines. The example invokes the +5% gate against an un-promoted anchor.

**Resolution:** Rewrote the example to clarify the staging:
- First Stage 0+1 capture: apply §3.9.1 ±20% promotion tolerance (gate's MAY-override surface not exercised yet — value still an `[EST]` anchor).
- Once promoted: subsequent per-PR captures apply §3.5.2 default +5% gate against the measured baseline, or tighter per-spec override.

---

## ERR-018-017: FR-PO-019 levels `MAY` but embeds an unconditional MUST

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** Medium
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` M-4)
**Root Cause:** FR-PO-019 stated: *"Cross-scenario profiling (Spec #19 KD-8 cross-spec scenarios) is permitted; the manifest ID and seed MUST be recorded the same way."* Level column: `MAY`. RFC 2119 grammar treats the row's declared level as binding for the whole statement — a MAY-row that embeds a MUST is structurally identical to the MUST/MAY conflict PASS-1 caught as `ERR-018-003` (FR-PO-067 vs §3.4.4). Conformance auditor reading the level column would not enforce the recording requirement.

**Resolution:** Split into two FRs:
- FR-PO-019 (MAY): *"Cross-scenario profiling (Spec #19 KD-8 cross-spec scenarios) is permitted."*
- FR-PO-019a (MUST): *"For any cross-scenario profiling session entered into the baseline corpus, the manifest ID and seed MUST be recorded per FR-PO-016."*

---

## ERR-018-018: §3.7.5 pre-specifies C# attribute signature without specified consumer

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** Medium
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` M-5)
**Root Cause:** §3.7.5 stated: *"the C# `Attribute` definition lands at first `src/` commit (targets: `Method | Constructor`; required constructor argument: `string rationale`; companion lead-developer-sign-off comment cites the `spec-error-log.md` row that authorizes the exemption)."* The attribute's C# signature is fully pinned at spec time — target enum, constructor argument, companion-comment grammar — but its consumer (the CI allocation-tracker build step that reads the attribute) is unspecified anywhere in #18 / #19 / #20. The allocation-tracker pin is §7.5 D2 / Stage 0+1. CLAUDE.md "Interface Design Principle" (ERR-001 / ERR-004 hazard): *"Write interfaces only when both sides are specified."*

**Resolution:** §3.7.5 deferred the concrete C# signature to Stage 0+1 alongside §7.5 D2. Retained the signature-independent governance contract:
- Every exemption MUST carry a rationale.
- Every exemption MUST be authorized by lead-developer sign-off recorded in `spec-error-log.md`.
- Every exempted call site MUST be marked at the source level so the alloc-tracker CI step can exclude it from the §3.7.4 diff.

---

## ERR-012-001: `DOMAIN_TAG_POSITIONING_AI` allocation needed in #16 §3.4 — proposed Phase B/C block-allocation policy

**Status:** ✅ Resolved May 18, 2026 — `DOMAIN_TAG_POSITIONING_AI = 0x17` allocated in #16 §3.4 v1.0.5; #12 §6.1 `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promoted atomically; all body-text instances in §1/§2/§3/§4/§8 promoted in v0.3/v0.4 fix passes.
**Severity:** Medium
**Detected:** May 15, 2026
**Detected During:** Positioning AI #12 `outline-detailed.md` v1.1 self-adversarial review (AR-V1-01); resolution proposed in v1.2.
**Files Affected:** 1 (`deterministic-sim/section-3.md` §3.4 domain-tag table)

**Root Cause:** Spec #12 Positioning AI requires a `DOMAIN_TAG_POSITIONING_AI` value to bind `DeterministicRngService` calls per #16 §3.4 / KD-9. The current §3.4 table ends at `DOMAIN_TAG_EVENT_LEDGER = 0x15` (#17, allocated May 14, 2026 per ERR-017-001). Five further Phase B/C specs (#10 Heading, #11 Goalkeeper, #13 Pressing, #14 Defensive, #15 Attacking) will each need their own tag during their own outline → section-file phases.

If each spec unilaterally claims the next-available value at outline time (first-come, first-served), there is a real risk of (a) value collisions when two specs draft concurrently and (b) fragmented patch revisions to #16's APPROVED tag namespace. The cleanest pattern is a single block allocation now, gated on lead-developer sign-off, that all six specs cite as `[CROSS-PENDING]` until the patch lands.

**Proposed Resolution (Phase B/C block `0x17 … 0x1C`) — REVISED May 16, 2026:**

The original proposal (`0x16…0x1B`) assigned `0x16` to Positioning AI #12. However, Heading Mechanics #10 reached `APPROVED` first (May 16, 2026, via ERR-010-001 resolution per the same project precedent — first-to-APPROVED claims the next-available slot) and took `0x16`. The block therefore shifts one slot:

| Spec | Domain Tag | Proposed Value | Notes |
|---|---|---|---|
| #10 Heading Mechanics | `DOMAIN_TAG_HEADING` | `0x16` | ✅ ALLOCATED May 16, 2026 via ERR-010-001 (#16 §3.5 v1.0.2 patch) |
| #12 Positioning AI | `DOMAIN_TAG_POSITIONING_AI` | `0x17` | Drafting NOW (#12 IN REVIEW); shifted from `0x16` after #10's allocation landed |
| #11 Goalkeeper Mechanics | `DOMAIN_TAG_GOALKEEPER` | `0x18` | NOT STARTED |
| #13 Pressing AI | `DOMAIN_TAG_PRESSING_AI` | `0x19` | NOT STARTED |
| #14 Defensive AI | `DOMAIN_TAG_DEFENSIVE_AI` | `0x1A` | NOT STARTED |
| #15 Attacking AI | `DOMAIN_TAG_ATTACKING_AI` | `0x1B` | NOT STARTED |
| #16 reserve | — | `0x1C` | Reserved (one slot of margin from the original `0x1B` ceiling). |

The collision avoidance ERR-012-001 was authored to prevent — multiple specs unilaterally claiming the same slot at outline time — did NOT trigger here because #10's allocation was formal (#16 §3.4 patch landed) before #12's `0x16` `[CROSS-PENDING]` was promoted. #12 must update its `outline-detailed.md` and section files to cite `0x17` when its own back-prop lands.

Block is contiguous with `DOMAIN_TAG_HEADING = 0x16` and consumes one nibble of u8 namespace. No `DETERMINISM_DIGEST_VERSION` bump required (pure namespace allocation, no preimage layout / field width / hash-input rule changes — mirrors the ERR-017-001 resolution pattern).

**Patch landing site:** `deterministic-sim/section-3.md` §3.4 constants catalogue (add 6 rows in canonical numerical order). One revision, six rows; #16 §3.5 version-history row notes Phase B/C namespace allocation.

**Atomic promotion mechanic:** all six specs carry the tag as `[CROSS-PENDING]` until the #16 patch revision lands. On patch merge, each spec promotes its row from `[CROSS-PENDING]` → `[CROSS]` in its own §3.10 / §3.4 / KD-9 citation site in a follow-up patch (parallel to ERR-017-001 #17-side promotion).

**Sign-off required:** Lead developer (#16 owner). Once ratified, #12 outline KD-9 and FR-PA-005 promote from `[CROSS-PENDING]` to `[CROSS]` and section-file authoring proceeds with the value fixed.

---

## ERR-012-002: `decision-tree/section-3-1.md` L716 cites Formation System as "Spec #14" — stale spec number

**Status:** ✅ Closed — Fixed May 15, 2026 in `decision-tree/section-3-1.md` v1.1.1 (single-token patch; approval status preserved)
**Severity:** Minor
**Detected:** May 15, 2026
**Detected During:** Positioning AI #12 `outline-detailed.md` v1.2 Outstanding-Questions resolution pass (Q3 grep against #8).
**Files Affected:** 1 (`decision-tree/section-3-1.md` L716)

**Root Cause:** Decision Tree #8 §3.1.7.2 reads: *"Stage 1 wires the Formation System (Spec #14) to provide live formation slot positions that adjust with tactical instructions and ball position."* Current `SPEC_INDEX.md` row 14 is **Defensive AI**. The Formation System functionality is #12 Positioning AI (verified — #8 §1.4.21 and §1.7.3 already use the canonical #12 number elsewhere in #8). Stale spec number left over from an earlier numbering scheme — same regression class as ERR-010 (Shot Mechanics #6 §1.1 calling Decision Tree #7) and ERR-012 (First Touch §7 calling Decision Tree #7), both closed in the March 2026 renumbering cascade. #8 §3.1.7.2 was missed by that cascade.

**Resolution:** Patch `decision-tree/section-3-1.md` L716 to read "Positioning AI (Spec #12)". One-token change in an APPROVED spec; no behavioural impact; patch-revision row in #8 §3.x version history.

**Detection grep:** `grep -n "Spec #14" decision-tree/` returns only this one line in `section-3-1.md`. (`grep -n "Formation System" decision-tree/section-*.md` returns multiple "Formation System (Stage 1+)" references without spec numbers — those are correct as-is and should not be touched.)

**Recommended patch landing:** alongside #16 §3.4 ERR-012-001 patch (same lead-developer revision pass), or as a standalone one-token revision.

---

## ERR-012-003: Documentary anchor for `XC-012-001`..`XC-012-009` allocation

**Status:** ✅ Closed (informational — no remediation required)
**Severity:** Minor
**Detected:** May 16, 2026
**Detected During:** Positioning AI #12 section-files PASS-1 adversarial review (AR-S1-18).
**Files Affected:** 1 (`positioning-ai/section-8.md` §8.3)

**Root Cause:** AR-S1-18 noted that #9 / #16 / #17 / #19 precedent files at least a short error-log row when allocating `XC-NNN-NNN` typed cross-reference IDs, so cross-spec readers can discover them by grep. Spec #12 §8.3 allocates `XC-012-001`..`XC-012-009` at section-file v0.1 without a corresponding error-log entry.

**Resolution:** This entry serves as the documentary anchor. `XC-012-NNN` are not erratum-class entries — they are typed cross-reference IDs published in `positioning-ai/section-8.md` §8.3 against approved upstreams #2, #8, #16, #18, #20. No remediation; entry exists for grep discoverability.

---

---

## ERR-008-001: Decision Tree #8 §3.2 `PitchGeometry` class uses centered coordinate origin

**Status:** ✅ Resolved May 18, 2026 — `decision-tree/section-3-2.md` v1.3: class rewritten to corner-origin (0,0,0); all `Vector2` goal constants replaced with `Vector3` using correct values; citation corrected to §1.2 and Appendix C; XC-GEOM-01 verification note added.
**Severity:** High
**Detected:** May 18, 2026
**Detected During:** Stress-test Tier A run 1, probe A-06 (coordinate-convention-guard FAIL) + T-03 (inverted domain conventions).
**Files Affected:** 1 (`decision-tree/section-3-2.md`, lines 305–350+)

**Root Cause:** The `PitchGeometry` static class in Decision Tree #8 §3.2 is authored with a center-origin coordinate system — the same defect class logged in CLAUDE.md "Things That Have Gone Wrong Before" ("Wrong coordinate origin — 'Pitch center' comment in Agent Movement §3.5"). The class comment states:

```
/// Coordinate system (consistent with Ball Physics #1 §2.2 and Agent Movement #2 §2.1):
///   Origin (0, 0) = centre of pitch
///   X-axis: pitch length (−52.5m to +52.5m; total 105m)
///   Y-axis: pitch width (−34m to +34m; total 68m)
```

The authoritative coordinate system (CLAUDE.md §"Coordinate System", Ball Physics #1 §1.2 and Appendix C, verified in `ball-physics/section-3-1.md` and `agent-movement/section-3-5-part-1.md`) is:
- Origin: corner of pitch (0, 0, 0)
- X: 0–105m (goal-to-goal)
- Y: 0–68m (touchline-to-touchline)

**Consequence — all goal position constants are wrong:**

| Constant | DT §3.2 value (centered) | Correct corner-origin value |
|----------|--------------------------|----------------------------|
| `HOME_OPPONENT_GOAL_CENTRE` | `(52.5, 0)` | `(105.0, 34.0, 0)` |
| `HOME_OPPONENT_GOAL_POST_L` | `(52.5, +3.66)` | `(105.0, 37.66, 0)` |
| `HOME_OPPONENT_GOAL_POST_R` | `(52.5, −3.66)` | `(105.0, 30.34, 0)` |
| `HOME_OWN_GOAL_CENTRE` | `(−52.5, 0)` | `(0.0, 34.0, 0)` |
| `HOME_OWN_GOAL_POST_L` | `(−52.5, +3.66)` | `(0.0, 37.66, 0)` |
| `HOME_OWN_GOAL_POST_R` | `(−52.5, −3.66)` | `(0.0, 30.34, 0)` |

The citation "consistent with Ball Physics #1 §2.2" is also incorrect — the authoritative section per CLAUDE.md is §1.2 (not §2.2).

**Resolution:**
1. Rewrite `PitchGeometry` class in `decision-tree/section-3-2.md` to use corner-origin (0,0,0) throughout.
2. Update `Origin` comment to `Origin (0, 0, 0) = corner of pitch (home team's left defensive corner)`.
3. Update `X-axis` range to `0m to 105m`. Update `Y-axis` range to `0m to 68m`.
4. Recalculate and update all `Vector2`/`Vector3` goal position constants using the correct system.
5. Switch goal positions to `Vector3` (not `Vector2`) to match the 3D coordinate system; or add a note that Y-component = 0 (ground-level Z in the spec's convention) and Y in `Vector2` here maps to X in the global system — this requires careful thought; simpler to use `Vector3` directly to avoid axis-label confusion.
6. Correct the citation from "§2.2" to "§1.2 and Appendix C".
7. Append a version-history row to `section-3-2.md`.

**Probe trigger:** A-06 FAIL — phrase "Origin (0, 0) = centre of pitch" is a direct origin claim. T-03 defect class (inverted coordinate convention).

---

## ERR-015-006: Attacking AI #15 §1/§2/§3/§4 retain stale `[CROSS-PENDING]` tags after ERR-015-001 resolution

**Status:** ✅ Resolved May 18, 2026 — all 7 stale `[CROSS-PENDING]` hits promoted to `[CROSS: #16 §3.4]` in §1 (4 instances), §2 FR-AT-005, §3 constant table, §4 §4.6 prose; v0.3 version-history rows added to all four section files.
**Severity:** Medium
**Detected:** May 18, 2026
**Detected During:** Stress-test Tier A run 1, probe A-03 (cross-pending-tracker FAIL) + T-05 + T-02.
**Files Affected:** 4 (`attacking-ai/section-1.md`, `section-2.md`, `section-3.md`, `section-4.md`)

**Root Cause:** ERR-015-001 was resolved on May 18, 2026 — `DOMAIN_TAG_ATTACKING_AI = 0x1B` was allocated in `deterministic-sim/section-3.md` §3.4 (v1.0.4), and the `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promotion was applied in `section-6.md` §6.1.9 and `section-9-approval-checklist.md`. However, the same tag appears as `[CROSS-PENDING]` in four additional section files that were not part of the promotion pass. The approval checklist therefore falsely claims "0 `[CROSS-PENDING]` remain" (T-02: fabricated checklist entry).

**Stale hits (all in `attacking-ai/`):**

| File | Line | Stale text |
|------|------|------------|
| `section-1.md` | 114 | `DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS-PENDING]` in §1.4 dependency table |
| `section-1.md` | 164 | "`[CROSS-PENDING]` throughout this spec until ERR-015-001 is ratified" in KD-11 note |
| `section-1.md` | 245 | `0x1B [CROSS-PENDING]` in KD table column |
| `section-1.md` | 266 | `DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS-PENDING] until ERR-015-001 ratified` in cross-spec compliance table |
| `section-2.md` | 25 | FR-AT-005: `([CROSS-PENDING] until ERR-015-001 is ratified in #16 §3.4)` |
| `section-3.md` | 948 | Constant reference table: `\| DOMAIN_TAG_ATTACKING_AI \| [CROSS-PENDING] \| 0x1B (ERR-015-001) \|` |
| `section-4.md` | 206 | `DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS-PENDING] (ERR-015-001; see …)` |

**Resolution:** In each location above, replace `[CROSS-PENDING]` with `[CROSS: #16 §3.4]` and update "until ERR-015-001 is ratified" clauses to "resolved May 18, 2026". Update `section-9-approval-checklist.md` §9.1 evidence row to accurately state which files were updated. Append version-history rows to each of the four section files.

**Probe trigger:** A-03 FAIL — `[CROSS-PENDING]` present in approved spec body text with no matching `Status: OPEN` ERR entry (ERR-015-001 is CLOSED). T-05 (dangling tag after upstream APPROVED). T-02 (fabricated checklist claim).

---

## ERR-016-003: Domain tag registry (#16 §3.4) silent gaps at `0x18` and `0x1C`

**Status:** ✅ Resolved May 18, 2026 — `deterministic-sim/section-3.md` v1.0.6: `_RESERVED_0x18_` and `_RESERVED_0x1C_` placeholder rows added to §3.4 domain-tag table; v1.0.6 version-history row added.
**Severity:** Medium
**Detected:** May 18, 2026
**Detected During:** Stress-test Tier A run 1, probe A-04 (domain-tag-allocator-audit FAIL) + T-08.
**Files Affected:** 1 (`deterministic-sim/section-3.md` §3.4 domain-tag table)

**Root Cause:** The ERR-012-001 Phase B/C block originally proposed the range `0x17…0x1C` (with `0x1C` as one slot of margin). As allocations landed, `0x18` was informally noted in the v1.0.3 changelog as "reserved for #11 Goalkeeper" before Goalkeeper Mechanics was reallocated to `0x1D` (because Positioning AI reached APPROVED first and claimed `0x17`, triggering the first-to-APPROVED cascade that shifted GK from `0x17` to `0x1D`). Neither `0x18` nor `0x1C` was ever assigned or documented in the live §3.4 table as a placeholder.

**A-04 requirement:** "every gap in the allocation sequence has an explicit `_RESERVED_0xNN_` placeholder row in the §3.4 table."

**Actual allocation sequence:**
```
0x10 DOMAIN_TAG_PHASE
0x11 DOMAIN_TAG_SNAPSHOT_PAYLOAD
0x12 DOMAIN_TAG_SNAPSHOT_HEADER
0x13 DOMAIN_TAG_RNGDRAW
0x14 DOMAIN_TAG_ENV_FP
0x15 DOMAIN_TAG_EVENT_LEDGER
0x16 DOMAIN_TAG_HEADING
0x17 DOMAIN_TAG_POSITIONING_AI
[0x18 — MISSING; no row]
0x19 DOMAIN_TAG_PRESSING_AI
0x1A DOMAIN_TAG_DEFENSIVE_AI
0x1B DOMAIN_TAG_ATTACKING_AI
[0x1C — MISSING; no row]
0x1D DOMAIN_TAG_GOALKEEPER
```

**Risk:** A developer assigning the next subsystem domain tag would search for the last-allocated value and find `0x1D`, concluding `0x1E` is next-available. The orphaned `0x18` and `0x1C` remain permanently unavailable for reuse but are not documented as such, creating a silent encoding hole.

**Resolution:** Add two rows to the §3.4 domain-tag table in `deterministic-sim/section-3.md` (in numerical order between the existing rows):

```
| _RESERVED_0x18_ | 0x18 | — | Skipped. Originally informally noted in #16 §3.4 v1.0.3 changelog as a reservation for Goalkeeper Mechanics #11 (ERR-011-001). GK was subsequently reallocated to 0x1D when Positioning AI #12 reached APPROVED first and claimed 0x17 per first-to-APPROVED precedent (ERR-011-001 KD-7 policy). Value 0x18 is permanently orphaned — must not be reassigned to any subsystem without explicit ERR tracking. |
| _RESERVED_0x1C_ | 0x1C | — | Skipped. Block-end margin value of the ERR-012-001 Phase B/C block (0x17…0x1C). Block was closed when 0x1B was allocated to Attacking AI #15 (ERR-015-001). Value 0x1C was never assigned; permanently orphaned — must not be reassigned without explicit ERR tracking. |
```

Append a v1.0.6 version-history row to `deterministic-sim/section-3.md`. No `DETERMINISM_DIGEST_VERSION` bump required (placeholder rows are namespace documentation, not preimage-layout changes).

**Probe trigger:** A-04 FAIL (silent gap without placeholder row). T-08 (DOMAIN_TAG gap).

---

## ERR-020-001: Code Standards #20 §4.2 `[CROSS]` mirror example uses ALL_CAPS field name, contradicting §3.2.3 PascalCase rule

**Spec:** Code Standards #20  
**Section:** §4.2 Constant Catalogue File Convention — `ProjectConstants.cs` Cross-Spec Source of Truth  
**Severity:** Minor  
**Detected During:** `src/CLAUDE.md` v1.3 adversarial review (May 22, 2026), finding M-3.  
**Status:** ✅ Resolved May 22, 2026

**Problem:** The §4.2 worked example for a `[CROSS]` mirror constant in `BallPhysicsConstants.cs` used `PHYSICS_TICK_HZ` (ALL_CAPS) as the mirror field name:

```csharp
public static readonly float PHYSICS_TICK_HZ = ProjectConstants.PHYSICS_TICK_HZ;
```

Spec #20 §3.2.3 (Tag → C# Storage Class Mapping) is the authoritative naming rule and explicitly states that `[CROSS]` constants use PascalCase. The ALL_CAPS convention is reserved exclusively for `[FIXED]` (`public const`) constants. A developer reading only §4.2 would use ALL_CAPS for every `[CROSS]` mirror, producing a codebase-wide naming inconsistency.

**Root Cause:** The §4.2 example was authored with the `PHYSICS_TICK_HZ` name matching the source constant in `ProjectConstants.cs` (which is correctly `[FIXED]` ALL_CAPS) rather than following the mirror field naming convention from §3.2.3.

**Files Affected:**
| File | Location | Change |
|---|---|---|
| `docs/specs/code-standards/section-4.md` | §4.2 mirror example (line ~160) | `PHYSICS_TICK_HZ` → `PhysicsTickHz`; XML doc updated with spec+section citation |
| `src/CLAUDE.md` | `[CROSS]` mirrors naming discrepancy note | Reference to ERR-020-001 added; "has been patched" noted |

**Resolution:** `code-standards/section-4.md` v1.0.1 patch: mirror field renamed to `PhysicsTickHz` (PascalCase); XML doc updated to include authoritative spec and section citation (`Ball Physics #1 §1.2`) and value (`60 Hz`) per FR-CS-022. `src/CLAUDE.md` v1.4 discrepancy note updated with ERR-020-001 reference.

**Rule confirmed:** The source constant in `ProjectConstants.cs` is `[FIXED]` and correctly uses ALL_CAPS (`PHYSICS_TICK_HZ`). The mirror field in any spec's constants catalogue is `[CROSS]` and uses PascalCase (`PhysicsTickHz`). The right-hand side of the mirror assignment must reference the source by its ALL_CAPS name (`= ProjectConstants.PHYSICS_TICK_HZ`).

---

## ERR-004-002: `FirstTouchContext` does not expose the nearest opponent's agent ID — `PossessionStateMachine` cannot resolve `InterceptingAgentID` on INTERCEPTION outcome

**Spec:** First Touch Mechanics #4
**Section:** §3.4.2 (priority-ordered outcome state machine), §4.3.1 (FirstTouchContext fields), §4.3.2 (FirstTouchResult fields)
**Severity:** Minor (Stage 0 carve-out; documented placeholder behaviour)
**Detected During:** `src/first-touch/` AR-5 adversarial review (June 6, 2026), finding L-4.
**Status:** 🟡 Open — placeholder behaviour in place; spec revision deferred

**Problem:** `PossessionStateMachine.Determine` (Priority 1 — INTERCEPTION branch) returns `(TouchResult.Interception, AGENT_ID_NONE, AGENT_ID_NONE)` because `FirstTouchContext` exposes only `HasNearbyOpponent` (bool) + `NearestOpponentDistance` (float) — there is no field carrying the nearest opponent's entity ID. The third tuple element of the return value is supposed to be `InterceptingAgentID`, but the data needed to populate it is not in the context. Result: the `FirstTouchResult.InterceptingAgentID` field surfaced to callers is `AGENT_ID_NONE = -1` on every INTERCEPTION outcome, which is indistinguishable from "no interception" downstream — Stage 1+ consumers that route possession to the intercepting opponent have no way to identify the receiving agent.

**Root Cause:** First Touch #4 §3.4.2 specifies the outcome classification logic but §4.3.1 omits a `NearestOpponentEntityId` field from `FirstTouchContext`. The omission was discovered post-implementation when `PossessionStateMachine` was wired up. The implementation placed an inline `// TODO: spec gap …` comment at the INTERCEPTION return; the AR-5 review found the gap was untracked in the error log.

**Files Affected:**
| File | Location | Change |
|---|---|---|
| `src/first-touch/PossessionStateMachine.cs` | Priority 1 INTERCEPTION return (~line 40) | Inline `TODO:` comment replaced with `ERR-004-002` anchor |
| `docs/specs/first-touch/section-4.md` (pending) | §4.3.1 FirstTouchContext field list | Add `NearestOpponentEntityId : int` field (or equivalent) |
| `src/first-touch/FirstTouchContext.cs` (pending) | Field declarations after `NearestOpponentDistance` | Add the field once §4.3.1 is patched |
| `src/first-touch/FirstTouchSystem.cs` (pending) | EvaluateFirstTouch wiring | Forward the ID into `PossessionStateMachine.Determine` |

**Resolution (proposed):** Add `int NearestOpponentEntityId` (sentinel `AGENT_ID_NONE` when `!HasNearbyOpponent`) to `FirstTouchContext` in a coordinated §4.3.1 patch. Caller (currently the integration boundary in `FirstTouchSystem`) populates it from the same scan that produces `NearestOpponentDistance` — typically the `PressureEvaluator` result. `PossessionStateMachine.Determine` then uses it for the INTERCEPTION return tuple. No formula change; pure data-flow gap closure.

**Stage 0 carve-out:** Until §4.3.1 is patched, INTERCEPTION outcomes carry `InterceptingAgentID = AGENT_ID_NONE`. Stage 0 has no downstream consumer that routes on this field (FirstTouchSystem.ApplyTouchResult only consumes `PossessingAgentID`); the gap blocks Stage 1+ AI-routed interception handoffs but not the Stage 0 test surface.

**Probe trigger:** AR-5 L-4 (June 6, 2026).

---

## ERR-003-001: Collision System #3 §3.3 impulse-to-force conversion F = j × 60 Hz inflates contact force ~10× against literature-calibrated thresholds

**Spec:** Collision System #3
**Section:** §3.3 Step 6 (impact force); contradicts the §3.3.1 threshold derivations (FALL_FORCE_BASE 500 N, FALL_FORCE_PER_STRENGTH 50 N, FALL_PROBABILITY_RANGE 500 N — sustained-force literature values)
**Severity:** Critical
**Detected During:** `src/collision-system/` AR-7 adversarial review (June 10, 2026), finding H-1.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** `F = Mathf.Abs(j) * 60f` assumes the whole collision impulse acts within one 16.7 ms frame. For an 85 kg equal pair, F ≈ 3315 × vRel (N), so the entire stochastic fall/stumble band (500–1500 N) spanned closing speeds of 0.15–0.6 m/s — below walking pace. Every real contact (jog ≈ 4 m/s closing → 13 kN) was a guaranteed knockdown roll, the failed roll guaranteed a stumble, and `knockdownForceOut` saturated at 1.0 (MaxCollisionForceRef = 2000 N at vRel ≈ 0.6 m/s). The test suite encoded the same scale (FL-002 asserted likely-stumble at vRel = 0.23 m/s), so the calibration defect was invisible to it.

**Resolution:** New `[GT]` `CONTACT_DURATION_S = 0.15 s` (biomechanics contact time ~0.1–0.3 s) added to the §3.3 catalogue; conversion patched to `F = j / CONTACT_DURATION_S` in spec pseudocode and `CollisionResponse.cs` v1.5 (`CollisionPhysicsConstants.ContactDurationS`; `PHYSICS_TICK_HZ` removed — that conversion was its sole consumer). Stochastic band now spans vRel ≈ 1.4–5.4 m/s. FL-001..005 / DT-001..002 closing speeds re-derived (tests v1.2).

---

## ERR-003-002: Collision System #3 §3.3/§3.4 FROM_BEHIND classification — normal convention sign-inverted on two surfaces

**Spec:** Collision System #3
**Section:** §3.3 `ClassifyContactType` (behindDot formula) and §3.4 `ProcessAgentAgentCollision` (Classify call site + `ForceDirection`)
**Severity:** Major
**Detected During:** `src/collision-system/` AR-7 adversarial review (June 10, 2026), finding M-1.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** Both `Classify` and `ContactForceData.ForceDirection` are documented against an instigator→victim normal, but the §3.4 call site always passed `manifold.Normal` (entity1→entity2) unflipped — sign-inverted whenever the instigator is the second agent. Compounding it, the §3.3 formula `Dot(-collisionNormal, victimDir) > 0.5` detects a victim moving TOWARD the instigator (head-on), not a fleeing victim; with a doc-correct normal FROM_BEHIND could never fire. Net behaviour: FROM_BEHIND fired only when the second agent instigated, via two cancelling sign errors; identical geometry with the first agent instigating yielded SIDE_IMPACT.

**Resolution:** §3.3 formula corrected to `Dot(collisionNormal, victimDir)` (victim fleeing along instigator→victim normal); §3.4 call site computes `instigatorToVictim = instigatorIdx == 0 ? manifold.Normal : -manifold.Normal` and feeds it to both `Classify` and `ForceDirection`. Implementation: `ContactTypeClassifier.cs` v1.2 + `CollisionSystem.cs` v1.6. Stage 0 consumers do not act on FoulData (Referee is Stage 1+), but the event stream is replay/analytics surface.

---

## ERR-003-003: Collision System #3 §3.3 same-team contacts above fallThreshold escape both fall and stumble branches

**Spec:** Collision System #3
**Section:** §3.3 `DetermineFallOrStumble` (stumble condition)
**Severity:** Major
**Detected During:** `src/collision-system/` AR-7 adversarial review (June 10, 2026), finding M-2.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** The fall branch requires `!isSameTeam`; the stumble branch required `impactForce <= fallThreshold`. A same-team impact above fallThreshold matched neither — the hardest same-team collisions were consequence-free while moderate ones could stumble (non-monotonic).

**Resolution:** Upper gate dropped; stumble probability clamped to 1 (`Clamp01`). Opposing-team forces above fallThreshold still return from the fall branch first, so its behaviour is unchanged. Spec pseudocode + `CollisionResponse.cs` v1.5.

---

## ERR-003-004: Collision System #3 §3.4 MAX_COLLISION_PAIRS_PER_FRAME valve counts broad-phase candidates and aborts the whole frame

**Spec:** Collision System #3
**Section:** §3.4 `UpdateCollisions` pair loop; §8 sizing rationale ("~10–20 pairs in practice") counted colliding pairs, not candidates
**Severity:** Major
**Detected During:** `src/collision-system/` AR-7 adversarial review (June 10, 2026), finding M-3.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** The valve charged the 50-pair budget per broad-phase candidate (3×3-cell neighbour after dedupe) and on exceedance aborted all remaining processing including agent-ball. A goalmouth scramble (~15 clustered agents) generates 100+ unique candidates, so the valve fired in exactly the scenarios where collisions matter, deterministically but silently dropping response for the higher-indexed roster half. Candidate iteration needs no valve — it is already bounded at 253 pairs by the dedupe bitfield.

**Resolution:** `ProcessAgentAgent` / `ProcessAgentBall` return narrow-phase confirmation; the valve counts confirmed collisions only (cap = event-buffer capacity, so the buffer cannot overflow). Spec pseudocode + `CollisionSystem.cs` v1.6.

---

## ERR-003-005: Collision System #3 §3.3 impulse response — approach/separation gate inverted for the a1→a2 normal convention

**Spec:** Collision System #3
**Section:** §3.3 Step 2 (relative velocity gate) and Step 4 (impulse application signs); §3.2 defines the manifold normal as pointing from Entity1 toward Entity2
**Severity:** Critical
**Detected During:** `src/collision-system/` AR-8 adversarial review (June 10, 2026), finding H-1.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** With n pointing a1→a2, `vRel = (v1 − v2)·n > 0` means a1 closes on a2 — approaching. The pseudocode gated `if (vRel > 0) → separation only` (labelled "separating") and computed `j = −(1+e)·vRel/Σ(1/m)` with `Δv1 = +j·n/m1`. Net behaviour: genuine closing collisions produced penetration separation only — no momentum exchange, no ImpactForce, and `DetermineFallOrStumble` was unreachable for real contacts — while overlapped pairs already moving apart received a velocity-reversing impulse back toward re-collision (energy injection). The unit suite encoded the inversion: CR-001 set both agents moving outward and rationalised it as a "passed-through state".

**Resolution:** Gate corrected to `vRel <= 0 → separation only`; `j = +(1+e)·vRel/Σ(1/m)` (preserving the j > 0 invariant the AR-3/AR-5 simplifications rely on); application signs corrected to `Δv1 = −j·n/m1`, `Δv2 = +j·n/m2`. Restitution verified: equal-mass head-on at ±5 m/s, e = 0.3 → ∓1.5 m/s with separation speed = e·closing speed. Spec §3.3 pseudocode + `CollisionResponse.cs` v1.6; CR-001..003 / FL-001..005 / DT-001..002 / EC-004 setups flipped to approaching geometry (tests v1.3).

---

## ERR-003-006: Collision System #3 §3.3 contact classification — FROM_BEHIND shadowed by the velocity-only shoulder predicate

**Spec:** Collision System #3
**Section:** §3.3 `ClassifyContactType` branch order
**Severity:** Major
**Detected During:** `src/collision-system/` AR-8 adversarial review (June 10, 2026), finding M-1.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** A chase-down (instigator catching a fleeing victim) has parallel velocities, so the shoulder predicate `Dot(approachDir, victimDir) > 0.7` — which tests velocity alignment only, with no contact geometry — classified every from-behind contact as SHOULDER_TO_SHOULDER before the from-behind test ran. Latent until ERR-003-002 made the from-behind geometry test correct; the two defects together meant FROM_BEHIND was effectively unreachable for its canonical geometry.

**Resolution:** FROM_BEHIND evaluated before SHOULDER_TO_SHOULDER; the contact normal is the discriminator (back-on contact: victimDir ∥ instigator→victim normal; side-by-side: perpendicular, falls through to the shoulder test). Spec §3.3 pseudocode + `ContactTypeClassifier.cs` v1.3.

---


## ERR-001-001: Ball Physics #1 §3.1.8.1 bounce pseudocode uses Unity Y-up `Vector3.up` as the ground normal in a Z-up coordinate system

**Spec:** Ball Physics #1
**Section:** §3.1.8.1 (Impulse-Based Bounce); contradicts §1.2 / Appendix C (Z = height) and Appendix B ("v_n ... vertical for a flat pitch")
**Severity:** Critical
**Detected During:** `src/ball-physics/` AR-7 adversarial review (June 9, 2026), finding H-1.
**Status:** ✅ Closed — spec and implementation patched June 9, 2026

**Problem:** The §3.1.8.1 pseudocode sets `Vector3 normal = Vector3.up;`. Unity's `Vector3.up` is `(0, 1, 0)` — the touchline (Y) axis in this project's corner-origin Z-up coordinate system. `BallGroundInteraction.ApplyBounce` implemented the line faithfully, so restitution and friction were computed against the lateral velocity component: a vertically falling ball had `v_n = v_y = 0`, zero restitution impulse, zero friction budget (`J_n = 0`), and never rebounded. Every other surface in the assembly (gravity `-Z`, height gates `.z`, the bounce's own `Position.z = RADIUS` write) is Z-up. Undetectable by the test suite because the Unity project is not yet initialized (tests have never executed).

**Resolution:** Spec §3.1.8.1 pseudocode patched to `new Vector3(0f, 0f, 1f)` with an inline ERR-001-001 warning (changelog row 2.8); `BallGroundInteraction.cs` v1.3 fixed identically (AR-7 H-1). Unit/integration expectations re-verified by a numerical mirror of the corrected model.

---

## ERR-001-002: Ball Physics #1 §3.1.8.1 friction stick impulse omits the rotational-coupling divisor

**Spec:** Ball Physics #1
**Section:** §3.1.8.1 STEP 4 (tangential friction impulse)
**Severity:** Major
**Detected During:** `src/ball-physics/` AR-7 adversarial review (June 9, 2026), finding M-1.
**Status:** ✅ Closed — spec and implementation patched June 9, 2026

**Problem:** `J_t_required = m * contactSpeed` is the impulse that zeroes contact-point slip for a non-rotating body. For a sphere the friction impulse also changes ω, so the contact-point velocity changes by `(1 + m·r²/I)` per unit of tangential Δv — for the hollow-sphere model (I = ⅔·m·r²) the factor is 2.5. When the μ·J_n cap is not binding, the applied impulse therefore reversed the slip by ~150% instead of zeroing it, injecting spurious tangential velocity and spin at every gripping bounce.

**Resolution:** Stick impulse divided by the catalogued `[DERIVED]` constant `BallPhysicsConstants.Bounce.StickImpulseCouplingDivisor = 1 + (MASS × RADIUS²) / MomentOfInertia` in both the spec pseudocode (changelog row 2.8) and `BallGroundInteraction.cs` v1.3 (AR-7 M-1).

---

## ERR-001-003: Ball Physics #1 — seven `[EST]` constants lack the FR-CS-020 validation log entries

**Spec:** Ball Physics #1 / Code Standards #20 (FR-CS-020)
**Section:** `src/ball-physics/BallPhysicsConstants.cs` — `Drag.CrisisSpeedLow` (20.0 m/s), `Drag.CrisisSpeedHigh` (25.0 m/s), `Spin.RollingSpinDecayPerSecond` (5.0 rad/s²), `Bounce.SpinToLinearRatio` (0.1), `Limits.MaxVelocity` (50 m/s), `Limits.MaxSpin` (80 rad/s), `Limits.MaxHeight` (50 m)
**Severity:** Minor (documentation-governance gap; values plausible, none validated)
**Detected During:** `src/ball-physics/` AR-8 adversarial review (June 9, 2026), finding L-2.
**Status:** 🟡 Open — this entry IS the required FR-CS-020 record; per-constant validation (promotion to `[GT]`/`[DERIVED]`/`[FIXED]`) is a Stage 1 tuning task

**Problem:** FR-CS-020 requires every `[EST]` constant to carry a `spec-error-log.md` entry tracking its validation path; the seven constants above had none. (An eighth, `Ball.MomentOfInertia`, was retagged `[EST]` → `[DERIVED]` in AR-7 L-2 — it is a documented formula over `[FIXED]` inputs, not an estimate.)

**Validation paths:** `CrisisSpeedLow/High` — literature check against Asai et al. (2007) drag-crisis Reynolds range; `RollingSpinDecayPerSecond` and `SpinToLinearRatio` — empirical tuning against rolling/bounce footage at Stage 1; `Limits.*` — sanity ceilings (fastest recorded shot ≈ 45 m/s) that promote to `[GT]` once gameplay tuning begins.

---

## ERR-004-003: First Touch #4 §3.3.2 direction blend negates ball velocity — heavy touches displaced against their own retained momentum

**Spec:** First Touch Mechanics #4
**Section:** §3.3.2 (Angular Error Model pseudocode); contradicts §3.3.2's own intent prose and §3.3.5 (BallRetained)
**Severity:** Critical
**Detected During:** `src/first-touch/` AR-7 adversarial review (June 10, 2026), finding H-1.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** The §3.3.2 pseudocode set `IncomingDir = Normalise(Vector2(-ball.Velocity.x, -ball.Velocity.y))` ("the direction the ball came FROM"). The same subsection states the intended q=0 behaviour four times — "ball goes entirely along incoming direction (no control)", "fallback to IncomingDir — ball follows original path, which is the correct heavy-touch behaviour", and the design rationale "a poorly executed touch deflects the ball further along its original path" — and §3.3.5 retains momentum along `+ball.Velocity`. `BallDisplacementProcessor.cs` implemented the negation faithfully (its v1.1 "H-4 fix" cited the pseudocode line), so a heavy touch teleported the ball up to 2.0 m back toward the passer while its velocity pointed forward — the ball then travelled back through the receiving agent. The test suite ENCODED both conventions simultaneously (BD-002 asserts travel-direction; BD-003/BD-004 assert the negation) — mutually unsatisfiable, and undetected because the suite has never compiled (see FirstTouchTests.cs v1.2 structural note).

**Resolution:** Spec §3.3.2 pseudocode patched to `Normalise(Vector2(ball.Velocity.x, ball.Velocity.y))` with an inline ERR-004-003 warning (changelog v1.4); `BallDisplacementProcessor.cs` v1.5 fixed identically and the degenerate-blend fallback aligned to the spec's IncomingDir mandate (AR-7 M-2). Test expectations re-derived from a numerical mirror of the corrected model. NOTE: `OrientationDetector` negates velocity CORRECTLY (facing-vs-approach comparison) and is untouched.

---

## ERR-004-004: First Touch #4 §3.4.2 interception proximity implemented agent-anchored instead of ball-anchored

**Spec:** First Touch Mechanics #4 / implementation drift
**Section:** §3.4.2 (Determination Logic — `SpatialQuery(newBallPosition, INTERCEPTION_RADIUS)`)
**Severity:** Major
**Detected During:** `src/first-touch/` AR-7 adversarial review (June 10, 2026), finding M-1.
**Status:** ✅ Closed — implementation patched June 10, 2026 (Stage 0 single-candidate approximation documented; full SpatialQuery + interceptor ID land with ERR-004-002)

**Problem:** §3.4.2 anchors the INTERCEPTION opponent query at `newBallPosition`. `PossessionStateMachine` tested `ctx.NearestOpponentDistance` — computed by `PressureEvaluator` around the AGENT and truncated at PressureRadius (3.0 m). With displacement up to RadiusHeavy (2.0 m) against the 2.5 m interception radius, the anchor error reached 80 % of the radius, and an opponent 2.5–3.0 m from the displaced ball but > 3.0 m from the agent read +∞ (invisible). Interceptions both spuriously fired and spuriously missed. The §3.4.5 interception velocity redirect ("Ball velocity set toward intercepting opponent (not zero)") was additionally unimplemented — INTERCEPTION outcomes kept the generic displacement velocity, breaking the Frame N+1 contact chain.

**Resolution:** `PressureEvaluator` v1.3 tracks the global nearest opponent (no radius truncation) and emits `NearestOpponentPositionXY`; `FirstTouchContext` v1.2 / `PressureResult` v1.1 carry it; `PossessionStateMachine` v1.3 measures `|opponent − newBallPosition| ≤ INTERCEPTION_RADIUS`; `FirstTouchSystem` v1.5 Step 7.5 implements the §3.4.5 velocity redirect (speed preserved). Residual Stage 0 approximation: only the single nearest-to-agent opponent is a candidate — the full multi-candidate `SpatialQuery` arrives with the ERR-004-002 context surface (same query returns the interceptor ID).

---

## ERR-004-005: First Touch #4 §3.4.2 DEFLECTION alignment gate is effectively vacuous through the public pipeline

**Spec:** First Touch Mechanics #4
**Section:** §3.4.2 (DEFLECTION momentum-alignment condition) interacting with §3.1 (q model) and §3.3.5 (velocity model)
**Severity:** Minor (model observation; no incorrect code — gate retained per spec)
**Detected During:** `src/first-touch/` AR-7 adversarial review (June 10, 2026), filed with the fix pass.
**Status:** 🟡 Open — documented; revisit when §3.3.5 gains Stage 1 loft/contact modelling

**Problem:** DEFLECTION requires `r ≥ 1.50 m` AND `alignment ≥ 0.70`. Reaching r ≥ 1.50 m requires small q (heavy band), and at small q the §3.3.5 velocity is dominated by `BallRetained = +v·(1−q)·0.5` (agent contribution ≤ DRIBBLE_MAX_SPEED·q ≈ 1.1 m/s vs retention ≥ ~8 m/s for the ball speeds that produce heavy touches), so alignment ≈ 1.0 always. Consequently every non-intercepted touch at r ≥ 1.50 m classifies DEFLECTION, and the low-alignment LOOSE_BALL escape is unreachable for physically producible inputs. Original test PO-005 encoded the unreachable expectation (90° intent ⇒ LOOSE_BALL) and could never pass.

**Resolution path:** Gate retained verbatim per §3.4.2 (it is cheap and becomes meaningful if Stage 1 contact modelling lets the agent contribution scale). PO-005 re-derived to lock the actual behaviour with an ERR-004-005 anchor; branch comment added in `PossessionStateMachine.cs` v1.3. Designer-facing implication: LOOSE_BALL occupies exactly r ∈ [0.60, 1.50) ∪ non-aligned degenerates.

---

## ERR-004-006: First Touch #4 §5.10 VS-001 hand-calc applies the velocity modifier additively and below reference speed, contradicting normative §3.2.3

**Spec:** First Touch Mechanics #4
**Section:** §5.10 VS-001 (validation scenario hand-calc + expected outputs); contradicts §3.2.3 (Velocity Modifier)
**Severity:** Major (test-encoded wrong expectation; no code defect — implementation matches §3.2.3)
**Detected During:** `src/first-touch/` AR-8 follow-up sweep (June 10, 2026), via full-pipeline numerical mirror.
**Status:** ✅ Closed — spec §5.10 and test VS-001 patched June 10, 2026

**Problem:** §3.2.3 defines the velocity modifier as multiplicative on the EXCESS above VELOCITY_REFERENCE (`r = r_base × (1 + Max(0, speed − 15)/15 × 0.25)`), so a 14 m/s ball gets no modifier. The §5 v1.2 changelog (Feb 22, 2026) "corrected" VS-001 from r = 0.195 m to r = 0.428 m by ADDING `(14/15) × 0.25 = 0.233 m` — an additive modifier applied below reference speed, a formula that exists nowhere in §3.2.3 (and whose Appendix B verification inherited the same arithmetic). `FirstTouchTests.cs` VS-001 encoded the 0.428 m expectation; against the §3.2.3-conformant `TouchRadiusCalculator` the actual radius is 0.195 m, so the test could never pass. Undetected because the suite has never compiled (FirstTouchTests.cs v1.2 structural note).

**Resolution:** §5.10 hand-calc and expected outputs re-derived per §3.2.3 (r ≈ 0.195 m; outcome CONTROLLED unaffected) in `section-5-7-to-5-13.md` v1.1 with a §5 changelog row in `section-5-1-to-5-6.md` v1.4; test VS-001 expectation updated to 0.195 ± 0.02 m. The original v1.1→v1.2 flip-flop (0.195 → 0.428 → 0.195) is preserved in both changelogs.

---

## ERR-007-001: Perception System #7 §4.6 forced refresh re-runs the full pipeline and double-advances cross-heartbeat recognition/scheduler state

**Spec:** Perception System #7
**Section:** §4.6 (Forced mid-heartbeat refresh) interacting with §3.3.6 (expiry) and §3.4.2 (shoulder-check scheduling)
**Severity:** High
**Detected During:** `src/perception-system/` AR-3 adversarial review (June 13, 2026), finding H-1.
**Status:** ✅ Closed — implementation patched June 13, 2026

**Problem:** `PerceptionSystem.HandleForcedRefresh` (§4.6) ran the complete `RunAgentPipeline`, including the §3.3.6 second-pass `ProcessInvisible` expiry loop, the `ShoulderCheckScheduler.UpdateAgent` autonomous-schedule advance, and the per-(observer,target) `ProcessVisible` / `ProcessBlindSideEntity` latency increments. Because a forced refresh fires out of the normal 10 Hz cadence (an extra pipeline run between heartbeats), every one of those stateful counters was ticked twice per logical heartbeat whenever a refresh occurred: confirmed-but-invisible entities had their expiry drained early and were evicted prematurely; scheduled shoulder checks could fire or be retimed off-cadence; and visible non-triggering entities accumulated extra latency toward confirmation. §4.6.2 mandates that a forced refresh reset `L_rec` for the **triggering entity only** — all other cross-heartbeat state must be left untouched. The defect made the produced `FilteredView` depend on whether a refresh happened to fire, a determinism hazard.

**Resolution:** `PerceptionSystem.cs` v1.4 gates all three cross-heartbeat mutations behind `!forcedRefresh`. Non-triggering entities now resolve through two new side-effect-free reads — `RecognitionLatencyTracker.IsConfirmed` and `ShoulderCheckScheduler.IsBlindSideConfirmed` — while the triggering entity still force-confirms (`L_rec = 0`) via `ProcessVisible(forcedRefresh: true)` per §4.6.2. The expiry loop and `UpdateAgent` are skipped entirely on a forced refresh. Dead `RecognitionLatencyTracker.ResetObserver` (never called; its doc-comment misrepresented §4.6.2 as a full-observer reset) removed in the same pass. Files: `PerceptionSystem.cs` v1.4, `RecognitionLatencyTracker.cs` v1.3, `ShoulderCheckScheduler.cs` v1.2.

---

## ERR-007-002: Perception System #7 §3.0 Step 1 candidate enumeration truncates the spatial-hash query before de-duplication, dropping unique agents

**Spec:** Perception System #7
**Section:** §3.0 Step 1 (spatial query / candidate enumeration), §4.1 (zero-alloc buffer sizing)
**Severity:** Medium
**Detected During:** `src/perception-system/` AR-3 adversarial review (June 13, 2026), finding M-1.
**Status:** ✅ Closed — implementation patched June 13, 2026

**Problem:** `RunAgentPipeline` copied candidates into the pre-allocated `_candidateIds` buffer (capacity `MaxAgents + 1 = 23`) under `limit = Min(candidates.Count, 23)` — i.e. it truncated the raw query to the first 23 entries **before** the `_candidateVisited` de-duplication ran. `SpatialHashGrid.Query` can return the same entity from multiple cells (body-radius straddle — the very reason the AR-1 M-1 dedup exists), and the ball sentinel (`-1`) was never deduped at all, so the raw list routinely exceeded 23 with duplicates. When it did, a unique agent appearing only past the cap behind duplicate entries was silently dropped from perception. Out-of-range positive ids (`id ≥ MaxAgents`) were also written into the buffer (skipped only downstream), wasting cap slots.

**Resolution:** `PerceptionSystem.cs` v1.4 de-dups across the **full** raw query before any capacity check: agents via `_candidateVisited`, the ball via a single `ballAdded` flag, and `id ≥ MaxAgents` dropped at source. Unique entities are bounded by `MaxAgents + 1`, so the 23-slot buffer cannot overflow and no unique agent can be truncated out (a defensive `break` guards the invariant). File: `PerceptionSystem.cs` v1.4.

---

## ERR-007-003: Perception System #7 §3.3.4 DeterministicHash uses Mathf.Abs — int.MinValue overflow and negative-modulo jitter

**Spec:** Perception System #7
**Section:** §3.3.4 (deterministic L_rec noise) / §3.4.2 (shoulder-check jitter); KD-4
**Severity:** Low
**Detected During:** `src/perception-system/` AR-3 adversarial review (June 13, 2026), finding L-1 (filed with the L-cluster).
**Status:** ✅ Closed — implementation patched June 13, 2026

**Problem:** `RecognitionLatencyTracker.DeterministicHash` returned `Mathf.Abs(h)`. `Math.Abs(int.MinValue)` throws `OverflowException` regardless of the surrounding `unchecked` context (a ~1-in-2³² latent crash on the avalanche output), and any negative hash that escaped would make the callers' `% N` (L_rec noise `% 2`, jitter `% (2·range+1)`) produce an out-of-range negative result — e.g. a jitter of −5 against the intended `[−2, +2]` band.

**Resolution:** `RecognitionLatencyTracker.cs` v1.3 returns `h & 0x7FFFFFFF` (mask off the sign bit) — always non-negative, no overflow, distribution preserved for the downstream modulos. Bundled L items in the same pass (no separate ERR rows): `ShoulderCheckScheduler` possession-interval magic literal `2.0f` → `PerceptionConstants.PossessionCheckIntervalMultiplier` [GT] (FR-CS-016); `FovCalculator.ComputeEffectiveFoV` doc clarified (decisions/ATTR_MAX normalisation per §3.9; only the `MIN_FOV_ANGLE` floor is explicit, the 170° ceiling holds by construction). **NOTE — withdrawn finding:** the AR-3 review additionally flagged the shoulder-check window-close comparison `>` as an off-by-one (window spans `DURATION + 1` ticks) and proposed `>=`; this was REVERTED after the CI gate showed it broke `SC002_Window_ClosesAfterDurationTicks` — that test locks `GetWindowExpiryFrame` as the **last active tick (inclusive)**, so the window is active through expiry by design, not by defect. An anti-re-tightening comment was added instead. Files: `RecognitionLatencyTracker.cs` v1.3, `ShoulderCheckScheduler.cs` v1.2, `PerceptionConstants.cs` v1.3, `FovCalculator.cs` v1.1.

---

## ERR-016-004: Deterministic Sim #16 §3.2.5 DeterministicRngService.Skip() breaks RNG branch-safety (advances RngCursor, not ActionOrdinal)

**Spec:** Deterministic Simulation #16
**Section:** §3.2.5 (branch-safe reservation API: Reserve / DrawReserved / CloseReservation / Skip)
**Severity:** High
**Detected During:** `src/deterministic-sim/` implementation adversarial review (June 15, 2026), finding H-1.
**Status:** ✅ Closed — implementation patched June 15, 2026

**Problem:** A per-stream draw value is `SipHash(streamKey, ActionOrdinal, drawIndex)` — `RngCursor` is **not** an input to the hash. `ActionOrdinal` advances only in `Reserve()`. The pre-fix `Skip(count)` advanced only `RngCursor` and left `ActionOrdinal` untouched, so a code path that took the skip branch instead of the Reserve branch ended the draw-site evaluation with a *different* `ActionOrdinal` than the drawing branch. Every subsequent `Reserve`+`DrawReserved` on that stream then drew from a shifted `ActionOrdinal`, desyncing all later draws. The whole point of the API is branch-safe RNG parity, yet `Skip` — advertised in both its XML doc and `src/CLAUDE.md` as the parity-preserving alternative — silently failed to preserve the only counter that matters. No test exercised `Skip`-vs-`Reserve` divergence, so it was invisible (the existing T-DS-RNG-002 only compares two Reserve branches).

**Resolution:** `Skip(streamIndex, count)` now treats a skip as one consumed draw-site evaluation: it advances `ActionOrdinal` exactly as `Reserve()` does **and** advances `RngCursor` by `count` for cursor parity, and it rejects an open reservation with `ERR_DS_RNG_BUDGET_MISMATCH` (signature `void`→`ushort`, parallel to `Reserve`). `DeterministicRngService.cs` v1.3. New `DeterministicSimAdversarialRegressionTests` fixture locks (a) a Skip branch and a Reserve+draw branch produce identical subsequent draw values, ActionOrdinal, and RngCursor; (b) Skip during an open reservation returns the budget-mismatch code.

---

## ERR-016-005: Deterministic Sim #16 §3.2.3 SnapshotCodec.Encode hashes payload-only — digest chain not chained, and untested

**Spec:** Deterministic Simulation #16
**Section:** §3.2.3 (SnapshotDigest chain) / §3.9.2 / §4.6.1; golden corpus `serialize-canonical-corpus.md` D-04..D-07
**Severity:** Medium
**Detected During:** `src/deterministic-sim/` implementation adversarial review (June 15, 2026), finding M-1.
**Status:** ✅ Closed — implementation patched June 15, 2026

**Problem:** §3.2.3 defines `SnapshotDigest[T] = SHA-256( SerializeCanonical(0x12 ‖ SnapshotHeader[T]) ‖ SerializeCanonical(0x11 ‖ SnapshotPayload[T]) )`, where `SnapshotHeader[T]` carries `prevSnapshotDigest` — that is how the chain links. Production `SnapshotCodec.Encode` instead computed `SHA-256(payloadBytes)` only: it ignored both domain tags, the header (schema/tick/prevDigest), and the environment-fingerprint slot. Consequence: each `CurrentSnapshotDigest` was independent of its predecessor, so altering or diverging an earlier snapshot did **not** invalidate any later digest — the "chain" provided ordering metadata, not tamper-evidence, and `ReplayEngine.ValidatePrevDigest` only verified a stored field that no digest depended on. The defect was untested because the golden-corpus suite (`SerializeCanonicalCorpusTests.Corpus_SnapshotDigestChain_D04toD07`) rebuilds the D-04..D-07 preimages by hand and never calls `SnapshotCodec.Encode` — the recurring "test encodes the spec but does not catch the production divergence" pattern.

**Resolution:** `Encode` now builds the §3.2.3 header preimage (`0x12 ‖ schemaVersion(u32) ‖ tick(u64) ‖ prevSnapshotDigest(32) ‖ envFpDigest(32)`) into a reused buffer and hashes `headerPreimage ‖ 0x11 ‖ payloadBytes` via `TransformBlock`/`TransformFinalBlock` (no combined-buffer allocation); `PrevSnapshotDigest` is threaded in *before* hashing so `CurrentSnapshotDigest` genuinely chains off its predecessor. New `EnvironmentFingerprint.ComputeDigest()` produces the 32-byte envFp slot (canonical `DOMAIN_TAG_ENV_FP ‖ workerCount ‖ length-prefixed §4.8 strings`, cached). The unused `ComputeSha256` helper was removed. `TickOrchestrator` now passes `prevDigest:null` to `Initialize` (the codec is the chain authority). Bundled lower-severity items resolved in the same pass (no separate ERR rows, per the perception L-cluster precedent): (i) `EnvironmentFingerprint` `Lock()`/file doc no longer claim a runtime `ERR_DS_ENV_MUTATION` guard that never existed — immutability is enforced structurally by the `readonly` fields; (ii) `RngStreamState.DrawIndex`/`BudgetRemaining` docs corrected to the actual random-access window semantics; (iii) `SaveManager.Load` returns `ERR_DS_STORAGE_ATOMICITY` for read/IO failure vs `ERR_DS_SCHEMA_INCOMPATIBLE` for a present-but-malformed file; (iv) `TickOrchestrator` AI-no-op comment no longer claims a per-phase digest emission that does not exist; (v) `SaveManager` class doc no longer asserts directory-fsync as a satisfied contract (Stage-0 Windows carve-out). Files: `SnapshotCodec.cs` v1.2, `EnvironmentFingerprint.cs` v1.1, `RngStreamState.cs`, `SaveManager.cs` v1.4, `TickOrchestrator.cs` v1.2; new regression tests assert `Encode` matches the §3.2.3 preimage digest and that the digest depends on `prevDigest`. **Follow-up (informational, non-blocking):** the real (non-empty) envFp preimage encoding is project-chosen — corpus D-05 is explicitly *illustrative-empty* — and should be pinned to a golden vector when the §4.8 EnvironmentFingerprint corpus row lands.

---

## ERR-016-006: Deterministic Sim #16 §4.8.3 floatModelHash tuple is IL2CPP-shaped and contradicts the Stage-0 Mono pin; no live-host hasher exists

**Spec:** Deterministic Simulation #16 (tuple defect); Platform Certification #16 §5.5 / `docs/tracking/certification-platform.md` (contradiction)
**Section:** §4.8 (EnvironmentFingerprint) / §4.8.3 (floatModelHash composition) / §5.5 (certification matrix) / §5.5.1 (deterministic flag strings)
**Severity:** Medium (latent — not currently blocking; the fingerprint is unwired at Stage 0, see below)
**Detected During:** Review of `EnvironmentFingerprint` against §4.8.3 (July 19, 2026) while assessing `SessionManifest`'s fingerprint requirement.
**Status:** ✅ Spec resolved (Option A, owner sign-off July 19, 2026) + live-host hasher landed. Host-blocked remainder (§4.8.2 runtime MXCSR validation; certified capture) tracked below and in the root `CLAUDE.md` OPEN ISSUES.

**Problem:** Three linked issues.

1. **No live-host hasher.** §4.8.3 defines `floatModelHash = SHA-256(SerializeCanonical(0x14 ‖ floatFlagTuple))` over an 11-field tuple of compiler/runtime float-mode flags. No code computes it: `EnvironmentFingerprint.FloatModelHash` is a plain `string` constructor argument, and the class's own `ComputeDigest()` hashes the *outer* 6-field fingerprint for the §3.2.3 snapshot-header preimage — a different digest. The 11-field float-flag tuple has no implementation anywhere.

2. **Spec-vs-pin contradiction on the tuple's own fields.** Tuple fields 1–4 (`compilerToolchain` ∈ {MSVC,Clang,AppleClang,GCC}, `compilerVersion`, `targetTriple` LLVM-style, `il2cppVersion`) are native-compilation / IL2CPP concepts. §5.5 row 0 pins the **Stage-0 developer host to "IL2CPP (MSVC backend)"** and §4.8.3 field 4 states "Stage-0 certification REQUIRES IL2CPP … MUST reject any snapshot whose fingerprint contains `"MONO"`". But `docs/tracking/certification-platform.md` v1.3 pins the Stage-0 backend to **Mono** ("IL2CPP migration is a Stage 5+ concern"), with an explicit `IL2CPP version | N/A (Mono backend)` row. §4.8.3/§5.5 (May 3–4, 2026) predate the platform pin (June 7, 2026). Consequently fields 1–4 have no defined meaning for the runtime actually pinned — a live hasher cannot be written respectably until the spec decides what the tuple means under Mono JIT (or the pin flips to IL2CPP).

3. **Placeholder factory was wrong on `simdFeatureLevel`.** `CreateStage0Dev()` — the sole factory, used by MatchEngine boot and every perf-harness/scenario test — stamped `simdFeatureLevel: "SSE2"`, matching neither the pinned SSE4.2 baseline (certification-platform.md §4.8) nor any other pin. (§4.8.3 field 11 `simdLevel` must equal `simdFeatureLevel`; the dev factory was at least self-consistent at SSE2, but both were wrong.)

**Current blast radius (why Medium, not High):** latent. `SaveManager` writes `headerOut.Fingerprint = null` (not yet wired into the save path), and the fingerprint is load-bearing only at a real certification run, which is independently blocked (no Unity host; `certification-platform.md` is `⏳ RECERT REQUIRED`). Nothing is silently drifting; the risk is that an honest-but-wrong placeholder papers over the undecided spec. Related: ERR-016-005's follow-up already flagged that the *outer* envFp preimage needs a golden vector when the §4.8 corpus row lands; that is distinct from this inner §4.8.3 tuple.

**Resolution (code side, this pass — no fabrication):** `EnvironmentFingerprint.cs` v1.2 — `CreateStage0Dev()` `simdFeatureLevel` `"SSE2"` → `"SSE4.2"` (matches the pin); the placeholder `floatModelHash` lifted to a named `FloatModelHashDevPlaceholder` sentinel; a new `IsDevPlaceholder` property lets a future cert-run gate reject a placeholder fingerprint (the analogue of §4.8.3's "reject MONO" rule for the unimplemented hasher); `FloatModelHash`/`CreateStage0Dev` docs now flag the missing hasher and the IL2CPP/Mono gap. **Deliberately NOT done:** synthesising fields 1–4 or writing a live-host hasher — that is blocked on the spec decision (fabricating those values is precisely what this ERR exists to prevent).

**Resolution (Option A, July 19, 2026 — owner sign-off in `env-fingerprint-float-model-hash-mono-mapping.md` v0.2):** the §4.8.3 tuple is mapped onto the pinned Stage-0 Mono backend, keeping the 11-field shape. **Spec:** `section-4.md` v1.1 — field 1 gains `"Mono"`; field 4 flips so Stage-0 certification ACCEPTS `"MONO"` (reject-MONO / IL2CPP-required move to Stage 5+); a "Stage-0 Mono backend mapping" paragraph pins fields 1–4 (compilerToolchain `"Mono"`, compilerVersion = host-supplied Mono version, targetTriple = RID `"win-x64"`, il2cppVersion `"MONO"`). `section-5.md` v1.1 — §5.5 row 0 backend → Mono; §5.5.1 Mono flag-strings note. **Code:** new `FloatFlagTuple.cs` (`ComputeHash()` = `SHA-256(SerializeCanonical(0x14 ‖ tuple))`) + `EnvironmentFingerprint.CreateStage0MonoCertified(monoRuntimeVersion)` (v1.3) — a genuine, non-placeholder fingerprint from the Option-A fields + the §4.8.3 Required Stage-0 flag values; golden vector + determinism/sensitivity tests in `DeterministicSimTests`.

**Still host-blocked (Stage-1+ / cert-run, NOT done here):** (a) the §4.8.2 runtime MXCSR validation (query live float-mode flags at match start, reject on mismatch) — needs native interop on the pinned host; (b) the certified capture — supplying the real Mono runtime version and running on the pinned Windows/Unity/Mono host (`cert-run-runbook.md` P2), unrunnable in the current Linux/no-Unity environment. The recorded tuple already uses the pinned Stage-0 flag values, which is exactly what (a) validates against.

---

## ERR-021-005 … ERR-021-007, ERR-012-007 … ERR-012-009, ERR-008-012: #23/#24/#25 approval back-props (July 10, 2026)

**Specs:** Tactical Instructions #21, Positioning AI #12, Decision Tree #8 (targets); Dismarking AI #23, Build-Up Structures #24, Positional Rotations #25 (owners)
**Severity:** Medium (all seven)
**Detected During:** Not defects — these are the planned cross-spec amendments each owning spec's §2.3/§2.4 pending-ERR table declared for filing "atomically with `APPROVED`" (pipeline step 6), following the ERR-014-001 / ERR-015-002 precedent.
**Status:** ✅ All seven filed and RESOLVED July 10, 2026, in the same commit as the #23/#24/#25 `SPEC_INDEX.md` status flips.

**Amendments landed:**

1. **#21 `TeamTactic` field appends (ERR-021-005/006/007):** `DismarkIntensity` (#23), `BuildUpStructure` (#24), `RotationFreedom` (#25) added to the §2.2.1 field table and the Appendix B canonical snapshot order, appended after `MarkingOrientation` in pinned approval order #23 → #24 → #25 (the #24 §2.2.1 append-order coordination rule; all three approved in one pass, so the order is pinned in #21 Appendix B and mirrored in each owning spec's Appendix B). All three are zero-value identities (`Off`/`None`/`Off`), so `TeamTactic.Balanced` needs no non-zero seeding and FR-TI-031 default-behaviour identity is preserved. Serialization is deferred: each field enters `WriteTeamTactic` with its own `SNAPSHOT_SCHEMA_VERSION` bump only when its owning spec's wiring stage lands (the `MarkingOrientation` 10 → 11 pattern). Files: `tactical-instructions/section-2.md` v0.5, `tactical-instructions/appendices.md` v0.5.
2. **#12 pipeline amendments (ERR-012-007/008/009):** new `positioning-ai/section-3.md` §3.7.1 records the Stage-1 stage insertions against the §3.7 step list — the #24 build-up overlay between `ContextModifier` and spacing, the #23 dismark offset between spacing and the pitch clamp (FR-DM-008), the combined order `anchor → offset → ContextModifier → build-up overlay → spacing → dismark offset → pitch clamp → lines → lanes` (pinned jointly in #23 §4.2 / #24 §4.2; second implementer adds the shared stage-order test), the #25 `RotationController` pre-composition tick position with its serialized `LastComposedTarget` cache, and the **`SlotIndex` single-writer contract amendment**: `AgentPositioningData.SlotIndex` is no longer immutable after `SeedFromFormation`; the `RotationController` is its sole post-seed writer (#25 §4.4 — the amendment the design supplement ranked riskiest, now an explicit documented invariant). Numbering note: ERR-012-004..006 were deliberately skipped — the June-13 dotnet-CI quarantine adjudication proposed (and section-3.md v0.5 already cites) the ERR-012-003..006 cluster, of which only -003's citation is live; reusing -004..006 here would collide if that cluster is ever formally filed. File: `positioning-ai/section-3.md` v0.6.
3. **#8 scorer row (ERR-008-012):** `decision-tree/section-3-2.md` §3.2.2.1 gains the back-prop anchor note placing the FM-DM-03 marked-pass-target multiplier (`Lerp(1.0, TARGET_MARKED_UTILITY_MULT, targetProx01 × awareness01)`, #23 §3.4) in the external tactical-multiplier product — next to the #21 §3.2 Mentality risk, #21 §3.3 `PlayerTactic` product, and §7.7 rest-defense multipliers — applied after the four §3.2.1.1 components and before the single final clamp (§3.2.1.5 timing unchanged). `DismarkIntensity.Off` ⇒ ×1.0 exactly. #23 owns the formula, constants (`TacticalWeights` per FR-DM-016), and tests. File: `decision-tree/section-3-2.md` v1.5.

All seven amendments are documentation/contract changes only — no code changed in this commit; every inserted stage/multiplier is an identity no-op until its owning spec's implementation lands, preserving byte-identical default behaviour.

---

## ERR-024-001: #24 overlay catalogue keyed to lane values no slot occupies (structural no-op)

**Spec:** Scripted Build-Up Structures #24
**Section:** Appendix A (overlay catalogue) / §3.2 worked example
**Severity:** High
**Detected During:** #23–#26 T0 implementation (July 10, 2026) — authoring `BuildUpOverlayCatalogue.cs` against the real `PositioningAIConstants.Family*` tables.
**Status:** ✅ Resolved July 10, 2026, same commit (freeze-then-amend pattern).

**Problem:** FR-BU-007 addresses catalogue rows by the slot's EXISTING
`FormationSlotRecord.DefaultLine` / `DefaultLane`. Appendix A v0.2 (the PASS-1 M-3 "lane-key
correction") keyed the fullback rows to the wide L/R lanes and the midfield rows to LH/RH — but
every family table records fullbacks at `DefaultLane` LH/RH (half-space), wide
midfielders/wingers/AMs at LW/RW, and central mids/DMs/forwards at C. No v0.2 row key matched any
slot in any family: with a non-`None` dial the overlay stage would have run and displaced nothing
— a silent structural no-op of the spec's entire behavioural payload. Root cause: M-3 verified
lane *geometry* (LB's LateralPct 0.15 → y = 10.2 m sits in the LW bin) but not the recorded seed
values the FR-BU-007 key actually uses (the #12 tables deliberately seed fullbacks as half-space —
a data-vs-geometry divergence inside #12 itself that the key inherits).

**Resolution:** Appendix A v0.3 + §3.2 v0.3 re-keyed every row to the recorded values —
BackThree: (DEF, LH/RH) fullback tuck + (MID, C) central drop; DoublePivot: (MID, C) pivot +
(ATT, C) link drop; InvertedFullBacks: (DEF, LH/RH) inversion. Magnitudes and row intents
unchanged (the `[GT]` shapes stay as reviewed). `BuildUpOverlayCatalogue.cs` v1.0 implements the
corrected keys; `BuildUpStructureTests.Catalogue_RowKeys_HitEveryFamily_Err024001Regression`
mechanically locks that every `FormationFamily` receives at least one non-zero own-third offset
per structure, so a future table/key drift of this class fails the suite immediately.

---

---

## ERR-022-001, ERR-027-001: off-pitch domain-tag / subsystem-ordinal back-props (July 22, 2026)

Two off-pitch determinism allocations that had landed in **code** but were never recorded in the
#16 §3.4 spec text or this log. Both are pure namespace allocations — no `DETERMINISM_DIGEST_VERSION`
bump, matching every other §3.4 tag row.

1. **Living World #22 (ERR-022-001):** `DOMAIN_TAG_LIVING_WORLD = 0x1E` + `SubsystemOrdinals.LivingWorld
   = 80` opened the off-pitch subsystem-ordinal band (80–99, disjoint from the match
   Physics/Mechanics/AI bands) with #22's slice-3 `world.text` wiring. The code (`DeterministicSimConstants`
   / `SubsystemOrdinals`) had it since July 2, 2026; the §3.4 spec-text row and this ERR were filed
   retroactively so the table is honest about `0x1E` being taken (a future "next-free-after-the-table"
   reader would otherwise have re-grabbed it).

2. **Squad/Player Data Layer #27 (ERR-027-001):** `DOMAIN_TAG_PLAYER_DATABASE = 0x1F` +
   `SubsystemOrdinals.PlayerDatabase = 81` (next after `LivingWorld`), the deterministic
   `RosterGenerator` roster-generation stream (siteId `player-database.roster-generation`,
   `entityId = clubId`; a boot / off-match-tick draw site). Filed as part of #27's promotion
   review to confirm the Appendix A `[CROSS]` cross-cite (the R-03 gate).

**Resolution:** `deterministic-sim/section-3.md` §3.4 gains a `DOMAIN_TAG_LIVING_WORLD` (`0x1E`) row
and a `DOMAIN_TAG_PLAYER_DATABASE` (`0x1F`) row, each citing its off-pitch subsystem ordinal and its
resolving ERR. The #27 Appendix A `DOMAIN_TAG_PLAYER_DATABASE` / `SubsystemOrdinals.PlayerDatabase`
`[CROSS]` rows are now a confirmed cross-cite against §3.4.

---

## ERR-028-001: Player Progression & Lifecycle #28 back-prop — promote `_RESERVED_0x20_` → `DOMAIN_TAG_PLAYER_PROGRESSION = 0x20` (July 23, 2026)

Filed at #28's section-file approval. The July-22 v1.0.8 pass had left `_RESERVED_0x20_` as a
reserved-pending-promotion placeholder for #28 (the roadmap §6 contiguous-block reservation, because
Season Loop #30 (Wave 1) reached the catalogue before #28/#29 (Wave 2)). #28's approval promotes it:

- **`DOMAIN_TAG_PLAYER_PROGRESSION = 0x20`** + **`SubsystemOrdinals.PlayerProgression = 82`** — the
  per-club regen/newgen RNG stream (siteId `player-progression.regen`, `entityId = clubId`, the #27
  `RosterGenerator` per-club-stream pattern; FR-PG-020 / KD-3). Aging/decline/growth of existing
  players is a pure deterministic integer projection and registers **no** stream — `0x20` covers
  regen generation only (#28 §4.3/§5).

**Resolution:** `deterministic-sim/section-3.md` §3.4 replaces the `_RESERVED_0x20_` row with the
`DOMAIN_TAG_PLAYER_PROGRESSION` (`0x20`) row (v1.0.9), citing `SubsystemOrdinals.PlayerProgression =
82` and this ERR. **Like ERR-030-001 (spec-text-first, unlike the code-first ERR-022/027-001):** the
code const + per-club RNG-stream registration land at **#28 T2** with the first regen — registering a
stream with zero draw sites now would be the phantom-surface class FR-LW-031 avoids (the `world.arcs`
precedent). `_RESERVED_0x21_` (Training #29) stays a placeholder until #29 promotes. Pure namespace
promotion; no `DETERMINISM_DIGEST_VERSION` bump. Fully resolves when the T2 code const lands.

---

## ERR-008-013: Decision Tree #8 gains a DT-emitted goalkeeper SAVE action (July 23, 2026)

**Context.** The GK (#11) / Heading (#10) engine integration (`gk-heading-engine-integration-design.md`)
landed the save/header intents fired from **engine-side world-state heuristics**
(`MatchEngine.TryCommitSaveIntents` → `GkHeadingIntentSource.SaveArmed`), listing "a DT-driven
GK/heading decision layer" as future work. The #11 `SaveIntent` doc, however, already states the intent
is "committed by the Decision Tree at the 10 Hz tactical tick" — i.e. #8 was always meant to own the
save decision. This ERR files the #8 change that realizes it, for the **SAVE** case (a DT-emitted
HEADER is deferred — ordinal 8 would overflow the 3-bit composure-noise field and force a rebaseline).
Governed by `docs/tracking/gk-heading-dt-producer-design.md` (outline + detailed plan, each
AR-converged; implementation AR-6 clean).

**The change (additive, off-ball-branch-only, opt-in-gated).**
1. `ActionType.SAVE = 7` — the last ordinal that fits the 3-bit `ActionSelector.ComputeOptionNoise`
   field. Ordinals 0–6 unchanged (no composure-noise rebaseline).
2. `TacticalContext.SaveAvailable` (bool; zero value `false` = identity) — set only for the threatened
   keeper, only under `MatchEngine.EnableGkHeading()`, from `GkHeadingIntentSource.SaveArmed`.
3. `OptionGenerator.GenerateOffBallBranch` short-circuits to **SAVE alone** when `SaveAvailable`, so
   the keeper's save is selected robustly (independent of composure noise / mentality / role tiebreak
   — a must-happen, geometry-gated action must not depend on out-scoring INTERCEPT, which can reach the
   utility ceiling under an aggressive per-agent tactic).
4. `UtilityScorer.ComputeUtility` scores SAVE = `U_BASE_SAVE` and **exempts SAVE from
   `PlayerTacticActionMultiplier`** — that multiplier indexes the #21 `RoleWeightModifiers` /
   `TempoActionBias` tables (7-wide, ordinals 0–6) by the action ordinal, so scoring `a = SAVE(7)`
   without the exemption reads out of bounds. **No #21 table is widened.**
5. `IDtSaveDispatch` seam (primitives only) + `ActionDispatcher` SAVE case + `DecisionTree` ctor param;
   `MatchEngine.HostSaveDispatch` maps agent→GK slot, applies the v18 per-episode latch, projects
   `PlayerAttributeProjection.ToGoalkeeper`, and commits the same Stage-0 `SaveIntent` the removed
   heuristic built. `MatchEngine.DriveGkHeadingTactical` drops `TryCommitSaveIntents`.

**Determinism / scope.** No `SNAPSHOT_SCHEMA_VERSION` change (SAVE reuses `AgentAction.Type`/
`TargetPosition`; no new serialized field). Flag-off is byte-identical (SaveAvailable false ⇒ off-ball
branch untouched, SAVE=7 never enters the noise field, the `!= SAVE` guard is always-true so
`PlayerTacticActionMultiplier` runs identically). Flag-on differs from the pre-change heuristic only in
the keeper's serialized DecisionTree `LastAction` (now SAVE) — expected, KD-11 non-neutral. **Full
dotnet gate PASSED, 0 failures.**

**Resolution.** `decision-tree/section-3-1.md` (§3.1 SAVE generation — off-ball, `SaveAvailable`-gated,
sole-option) and `section-3-2.md` (§3.2 `ScoreSave` + the `PlayerTacticActionMultiplier` SAVE
exemption) gain concise ERR-008-013 back-prop anchor notes (the ERR-008-012 anchor-note precedent — the
formula/behaviour is owned by this ERR + the code, the section note points to it). The `ActionType`
enum member (§2.2.1) and the dispatch seam (§3.5) are described here; their section files carry the
note by reference. Additive to an APPROVED spec via the established ERR-008 back-prop pattern.

---

*End of Spec Error Log v1.40 — July 24, 2026.*
