# Player Progression & Lifecycle #28 — Section 4: Architecture

**Created:** July 23, 2026
**Last Updated:** August 22, 2026 (v0.4 — **ERR-028-020 / ERR-028-021**: §4's file map for `AbilityModel.cs` brought current — it now carries §3.1.3's age curve (`DailyBandPoints` / `AccruedBandPoints`, with `ClassifyAgeBand` demoted to a read of it) and §3.4's `RetirementAgeDays`, neither of which existed when the map was written. No file added, no reference direction changed. Prior entry below.)
**Last Updated (prior):** August 10, 2026 (v0.3 — ERR-028-017: §4.2's "Season-save composition" note corrected from a stale "2 → 3" frame-version claim to a citation of #30 Appendix A's 1→5 chain; §4.5's seam sentence restated in `TrainingInputBatch` terms and its "sole seam" claim replaced with the verified full public surface of `ProgressionEngine`)
**Last Updated (prior):** July 23, 2026 (v0.2 — section-file PASS-1 (0H+2M) → AR-2 (3M cross-fix) → AR-3 convergence; APPROVED)
**Version:** 0.4
**Status:** APPROVED

---

## 4.1 Assembly placement & reference direction

New off-pitch assembly **`TacticalDirector.PlayerProgression`** (`src/player-progression/`).

**Reference direction (the load-bearing invariant):**
- References **`TacticalDirector.PlayerDatabase`** (#27 — `PlayerRecord` / `PlayerAttributes` /
  `RosterGenerator` / `Squad` / `PlayerDatabaseConstants`, read-only for the roster world + reused
  draw pattern) and **`TacticalDirector.DeterministicSim`** (#16 — `DeterministicRngService` +
  `CanonicalSerializer`). Nothing else.
- **#28 MUST NOT reference the season assembly** (#30 / `TacticalDirector.SeasonSave`). #30 depends
  on #28 (its day-advance loop + season-boundary roll invoke `ProgressionEngine.AdvanceDay` /
  `RunSeasonBoundary`, and its season-save root composes #28's blob) — never the reverse. This is the
  #26-manager-gate-invoked-by-the-engine + FR-LW-003 hygiene (the composing root is the only assembly
  above both, so it wires #28 and #30 without either referencing the other).
- **#28 MUST NOT reference the match engine.** Lifecycle runs on the world tick only (FR-PG-001).

## 4.2 File layout (proposed)

**File list below verified against `src/player-progression/*.cs` at ERR-028-017 — `ClubCareerStates.cs`
and `TrainingInputBatch.cs` (both public, both load-bearing since T1/T2a) were missing, and
`RetirementResult.cs`/`RegenResult.cs` do not exist: the season-boundary signals are declared as inline
`readonly struct` sketches in §2.2's code block, not implemented files — `RunSeasonBoundary` itself is
deferred (§9.2), so nothing constructs or consumes them yet. Kept in the list as a NOT-YET-BUILT row so
the deferred boundary is visible rather than silently dropped.**

```
src/player-progression/                 // references PlayerDatabase + DeterministicSim only
├── player-progression.asmdef
├── ProgressionEngine.cs                // sealed: AdvanceDay / SquadFor / Snapshot / Restore; sole writer (KD-7); RunSeasonBoundary NOT YET BUILT (§9.2)
├── PlayerLifecycle.cs                  // the per-player overlay value type (§2.2)
├── ClubCareerStates.cs                 // the per-club (records, lifecycles) pair Encode/Decode and ToBlocks/FromBlocks carry (§3.5)
├── GrowthProjection.cs                 // static pure: the §3.1 daily projection (sole attribute-mutation path)
├── AbilityModel.cs                     // static pure: ComputeCA, the §3.1.3 age curve (DailyBandPoints /
│                                       //   AccruedBandPoints) + ClassifyAgeBand as its read, §3.4's
│                                       //   RetirementAgeDays, and the weighted spend order (§3.1.2/§3.2)
├── RegenGenerator.cs                   // static pure: §3.3 single-player generation (reuses #27's draw pattern)
├── TrainingInput.cs                    // the per-player #29 seam element (Neutral identity, §2.2)
├── TrainingInputBatch.cs               // the FR-PG-021 batch parameter: ClubTrainingInputs[] + Neutral (§2.2)
├── RetirementResult.cs / RegenResult.cs // NOT YET BUILT — the season-boundary signals `RunSeasonBoundary` would emit; only sketched inline in §2.2 today
├── LifecycleViewModel.cs               // read-only observer surface for #31/#38 (KD-7)
├── ProgressionSaveCodec.cs             // pure: the PROGRESSION_SAVE_FORMAT_VERSION block (§3.5); fail-loud gates
├── PlayerProgressionConstants.cs       // Fixed / Derived / GT catalogue (Appendix A)
└── tests/
    └── player-progression-tests.asmdef
```

**Season-save composition (owned by #30, not built here):** #30's season-save root adds
`ProgressionEngine.Snapshot()/Restore()` as one more opaque length-prefixed sub-blob in
`SeasonSaveCodec` and bumps the outer `SEASON_SAVE_FORMAT_VERSION` — **4 → 5, at #28 T1 (ERR-030-030 /
FR-PG-017); see #30 Appendix A for the full 1 → 5 chain, not restated here (ERR-028-017 — this line
previously carried its own stale "2 → 3" copy, a third home for a version number #30 already owns and
tracks).** The world/match blobs stay byte-untouched (FR-PG-017). #28 provides the blob API; #30 frames it.

## 4.3 Determinism & naming — promoting the reserved #16 rows

`deterministic-sim/section-3.md` already holds `_RESERVED_0x20_` (a reserved-pending-promotion row)
and `SubsystemOrdinals` 82 for #28. At approval this spec files the back-prop that **promotes** them:
- **`DOMAIN_TAG_PLAYER_PROGRESSION = 0x20`** — the hash-domain tag for the `player-progression.regen`
  draw site, off-pitch subsystem-ordinal band **`SubsystemOrdinals.PlayerProgression = 82`** (resolves
  ERR-028-001). A stream registers **per club** (`entityId = clubId`, FR-PG-020 — the #27
  `RosterGenerator` per-club-stream pattern), so each club's newgen sequence is an independent
  reproducible sub-stream.
- The code const + per-club RNG-stream registration land at the **first regen (T-phase)**, never
  earlier (registering a stream with zero draw sites is the phantom-surface class FR-LW-031 forbids —
  the `world.arcs` / #30 `0x22` precedent). Pure namespace allocation; **no `DETERMINISM_DIGEST_VERSION`
  bump** (the catalogue grows; no preimage layout / field width / hash-input rule changes).

Aging/decline/growth register **no** stream (pure integer projection, FR-PG-002) — `0x20` covers
regen generation only. The Stage-3 stochastic-retirement / growth-jitter dials would each add a
**documented, appended** draw site on the same stream (APPEND-only, FR-PG-020), never a new tag.

## 4.4 CS0104 hazard (name collision)

`ProgressionEngine` / `PlayerLifecycle` / `GrowthProjection` / `TrainingInput` / `RegenGenerator` are
new names; a grep of `docs/specs/**` + `src/**` at T0 MUST confirm no existing type shares them before
the assembly is wired. In particular, `PlayerProgression.PlayerAttributes` is **not** introduced —
#28 consumes #27's `PlayerDatabase.PlayerAttributes` directly (there is no #28 attribute type), so the
`AgentMovement.PlayerAttributes` ↔ `PlayerDatabase.PlayerAttributes` CS0104 class #27 T1 hit does not
recur here. Should a future spec bring a same-named type into a shared scope, fully-qualify from line
one (the KD-P6 discipline).

## 4.5 Interface contracts

Per the CLAUDE.md "Interface Design Principle" (write interfaces only when both sides are specified),
#28 declares **no** interface for the #29 producer — the training seam is a **`TrainingInputBatch`
method parameter** (KD-2 / FR-PG-009/021; corrected from "a `TrainingInput` method parameter" at
ERR-028-017 — the seam's actual shape is the batch §2.2 declares, `TrainingInput` being only the
per-player element inside it), not an `IProgressionInput` against an unbuilt consumer. The
`RetirementResult`/`RegenResult` outputs are plain value types the roster owner reads.

**The "sole seam" claim below understated the real public surface (ERR-028-017) — corrected against
`src/player-progression/ProgressionEngine.cs` rather than restated from memory.** `ProgressionEngine`'s
full public surface, split by who it is for:
- **The #30 contract** (§4.3, KD-7) — what `SeasonLoop`/`SeasonSaveManager` actually call: `SeedFrom`
  (the new-game entry point, §3.1), `AdvanceDay` (the FR-PG-021 batch daily step), `SquadFor` (the
  KD-4 roster projection every consumer must read through — the mechanism that retires the
  from-the-world-seed reopening property, ERR-030-030/ERR-028-017 M4), and `Snapshot`/`Restore` (§3.5).
- **Codec-internal** — `ToBlocks`/`FromBlocks`, the per-club array shape `Snapshot`/`Restore`
  round-trip through `ProgressionSaveCodec`; not a seam another assembly composes against.
- **Observation** (FR-PG-023, KD-7) — `LifecycleView` (the read-only per-player view for #31/#38) and
  the three cheap read-only properties `ClubCount`, `NextPlayerId`, `CarriesClub(clubId)`.
- **Construction convenience** — the static `Empty` (a zero-club store — the honest pre-#28
  composition `SeasonSaveManager`'s own guard reasons about, ERR-028-008/ERR-028-013).

`RunSeasonBoundary` does not yet exist (§9.2 — deliberately deferred pending the `player-progression.regen`
stream); this section's prior text listed it as already part of the seam.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial architecture: assembly placement + one-way reference direction, file layout, determinism-identifier promotion, CS0104 note, no-phantom-interface discipline. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (0H+2M: M-1 age-model muddle → one BirthWorldDay-derived representation; M-2 per-club regen stream) → AR-2 (3M cross-fix regressions) → AR-3 convergence; APPROVED. See section-9 §9.3.1. |
| 0.3 | 2026-08-10 | — | ERR-028-017 (AR pass 5 spec-vs-code sweep, no code change): §4.2's file layout was stale against `src/player-progression/*.cs` — missing `ClubCareerStates.cs` and `TrainingInputBatch.cs` (both public, both load-bearing since T1/T2a) and listing `RetirementResult.cs`/`RegenResult.cs`, which do not exist (only sketched inline in §2.2; `RunSeasonBoundary` itself is deferred) — corrected, with the not-yet-built pair marked as such rather than removed. §4.2's "Season-save composition" paragraph carried a stale "2 → 3" `SEASON_SAVE_FORMAT_VERSION` claim — actual is 4 → 5 as of #28 T1 — replaced with a citation to #30 Appendix A rather than a fourth restated copy of a chain #30 already owns (the AR pass 13 "a third copy is not re-synchronised" lesson, applied at the second copy this time). §4.5's seam sentence corrected from "a `TrainingInput` method parameter" to the actual `TrainingInputBatch` shape, and its "sole seam" list — which named `RunSeasonBoundary` (not yet built) and omitted `SeedFrom`/`SquadFor`/`ToBlocks`/`FromBlocks`/`Empty`/`ClubCount`/`NextPlayerId`/`CarriesClub` — replaced with the verified full public surface of `ProgressionEngine`, split into the #30 contract, codec-internal members, observation, and construction convenience. |
| 0.4 | 2026-08-22 | — | **ERR-028-020 / ERR-028-021** (football-judgment proxy review, batch 1 — spec + code, same commit). §4's file map annotation for `AbilityModel.cs` updated: it now hosts §3.1.3's continuous age curve (`DailyBandPoints` / `AccruedBandPoints`) with `ClassifyAgeBand` as a read of that curve rather than an independent classifier, plus §3.4's `RetirementAgeDays`. No new file, no new assembly reference, no change to the reference direction. |
#endregion
