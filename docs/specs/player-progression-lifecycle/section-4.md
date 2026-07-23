# Player Progression & Lifecycle #28 — Section 4: Architecture

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file PASS-1 (0H+2M) → AR-2 (3M cross-fix) → AR-3 convergence; APPROVED)
**Version:** 0.2
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

```
src/player-progression/                 // references PlayerDatabase + DeterministicSim only
├── player-progression.asmdef
├── ProgressionEngine.cs                // sealed: AdvanceDay / RunSeasonBoundary / Snapshot / Restore; sole writer (KD-7)
├── PlayerLifecycle.cs                  // the per-player overlay value type (§2.2)
├── GrowthProjection.cs                 // static pure: the §3.1 daily projection (sole attribute-mutation path)
├── AbilityModel.cs                     // static pure: ComputeCA + ClassifyAgeBand + the weighted spend order (§3.1.2/§3.2)
├── RegenGenerator.cs                   // static pure: §3.3 single-player generation (reuses #27's draw pattern)
├── TrainingInput.cs                    // the #29 seam value type (Neutral identity, §2.2)
├── RetirementResult.cs / RegenResult.cs // the season-boundary signals #30/#27 apply
├── LifecycleViewModel.cs               // read-only observer surface for #31/#38 (KD-7)
├── ProgressionSaveCodec.cs             // pure: the PROGRESSION_SAVE_FORMAT_VERSION block (§3.5); fail-loud gates
├── PlayerProgressionConstants.cs       // Fixed / Derived / GT catalogue (Appendix A)
└── tests/
    └── player-progression-tests.asmdef
```

**Season-save composition (owned by #30, not built here):** #30's season-save root adds
`ProgressionEngine.Snapshot()/Restore()` as one more opaque length-prefixed sub-blob in
`SeasonSaveCodec` and bumps the outer `SEASON_SAVE_FORMAT_VERSION` (2 → 3 after #30's season block);
the world/match blobs stay byte-untouched (FR-PG-017). #28 provides the blob API; #30 frames it.

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
#28 declares **no** interface for the #29 producer — the training seam is a `TrainingInput` **method
parameter** (KD-2 / FR-PG-009), not an `IProgressionInput` against an unbuilt consumer. The
`RetirementResult`/`RegenResult` outputs are plain value types the roster owner reads; the
`ProgressionEngine` public surface (`AdvanceDay` / `RunSeasonBoundary` / `Snapshot` / `Restore` +
`LifecycleViewModel` accessors) is the sole seam #30 and #38 consume.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial architecture: assembly placement + one-way reference direction, file layout, determinism-identifier promotion, CS0104 note, no-phantom-interface discipline. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (0H+2M: M-1 age-model muddle → one BirthWorldDay-derived representation; M-2 per-club regen stream) → AR-2 (3M cross-fix regressions) → AR-3 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
