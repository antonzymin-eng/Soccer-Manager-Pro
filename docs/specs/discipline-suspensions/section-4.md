# Discipline & Suspensions #44 — Section 4: Architecture

**Created:** July 24, 2026
**Last Updated:** August 15, 2026, later still (v0.7 — L20, the spec half of #44's adversarial-review
round 4 (`open-issues.md`): §4.1 named only 2 of the 4 references `src/discipline/discipline.asmdef`
actually declares — `TacticalDirector.EventSystem` and `TacticalDirector.PlayerDatabase`, omitting
`TacticalDirector.DeterministicSim` (`CanonicalSerializer`/`SaveBlobFramingHelpers` in
`DisciplineSaveCodec.cs`) and `TacticalDirector.ProjectConstants` (`GameplayConfigHolder`'s `[GT]`
loader in `DisciplineConstants.cs`), both verified present in the asmdef file and in the citing
source files directly. All four now named, with the `DeterministicSim` reference explicitly
distinguished from #16's RNG service, which #44 does not consume (FR-DC-019); the reference diagram
extended to show all four)
**Last Updated (prior):** August 15, 2026 (v0.6 — ERR-044-003 stage 1, owner decision: §4.5's composition-root
contract for calling `OnClubFixturePlayed` amended to require passing the club's fielded eleven, so
the call can exempt a player who appeared through the extremis back-fill from serving that fixture's
ban)
**Last Updated (prior):** August 13, 2026, later still (v0.5 — L12(b), a third adversarial-review pass over
the #44 C1/C2 landing: §4.2's file table listed 6 of the 9 files `src/discipline/` actually carries,
omitting `DisciplineEntry.cs`, `IDisciplineTickLedgerTap.cs` and `AssemblyInfo.cs`; the `DisciplineRules.cs`
and `Availability.cs` rows also corrected to name their full landed API rather than only the two
methods the row was first written against)
**Last Updated (prior):** August 13, 2026, later same day (v0.4 — L6, adversarial review over the C1/C2
landing: §4.1's "at the T-phase" and §4.2's "proposed, at T-phase" headers corrected — the assembly
and its file layout have existed since T0/T1, not just been proposed)
**Last Updated (prior):** August 13, 2026 (v0.3 — ERR-044-001 + ERR-044-002, C1/C2 landing back-prop: §4.4
gains the magic-before-version MUST and cites the frame v5 → 6 bump; §4.5's root contract re-scoped
to both resolution paths)
**Last Updated (prior):** July 24, 2026 (v0.2 — cross-set AR pass 3; prior v0.1 initial)
**Version:** 0.7
**Status:** APPROVED

---

## 4.1 Assembly & reference direction

**`TacticalDirector.Discipline`** (`src/discipline/`) — LANDED at T0/T1 (July 24 – August 13, 2026);
the sentence below describes the reference direction as designed and as built. **`discipline.asmdef`
declares four references**, all verified against the file directly (M25's sibling finding, L20):
**`#17 EventSystem`** (the `CardIssuedEvent`/`SubstitutionEvent` value types the tap yields);
**`#27 PlayerDatabase`** (`PlayerId`/`Squad`, read-only — `FilterAvailable` returns a value copy);
**`#16 DeterministicSim`** (`CanonicalSerializer`/`SaveBlobFramingHelpers` — `DisciplineSaveCodec.cs`'s
byte-level encode/decode, Deterministic Simulation #16 §3.2.4.1; **not** the RNG service, which #44
does not consume — see below); and **`ProjectConstants`** (`GameplayConfigHolder`'s `[GT]` loader —
`DisciplineConstants.cs`'s `Config.GetInt(...)` calls, `src/CLAUDE.md`'s "`[GT]` loading mechanism").
It references **neither #30 nor #43 nor #38 nor the match engine, and consumes no #16 RNG stream/tag/
ordinal** — the composition root wires the tap around engine-resolved fixtures, threads the lineup
mapping in, applies the filter at the ERR-030-009 seam, and reports played fixtures/roster events.

```
compositionRoot (season loop) ──► #44 Discipline ──► { #17 (event types), #27 (read-only),
        │                                ▲              #16 (serializer only), ProjectConstants ([GT]) }
        └─ taps the fixture's events /   └── #38 (screens), #43 (partitions), #46 (news)
           threads lineup / applies          — deferred consumers, no interface built (FR-LW-031)
           the filter / reports fixtures
```

Acyclic; no consumer references #44. **No RNG stream/tag/ordinal** — no #16 row exists or is
needed (the #37/#49 positive property).

## 4.2 File layout (as landed — see `docs/tracking/file-manifest.md`'s `src/discipline/` section for the authoritative inventory)

| File | Contents |
|---|---|
| `DisciplineState.cs` | the `(PlayerId, CompetitionId)` tally map (KD-1/KD-6) |
| `DisciplineEntry.cs` | one tally row — `PlayerId`, `CompetitionId`, `Yellows`, `BanMatchesRemaining` (F2's `PlayerId >= 0` gate) |
| `CardLedgerFold.cs` | the occupancy fold over the tap (KD-2/KD-5, §3.1) |
| `IDisciplineTickLedgerTap.cs` | the #37-class per-tick read interface `CardLedgerFold.ObserveTick` consumes (KD-2/§4.3) |
| `DisciplineRules.cs` | `ApplyCard`/`AddYellow`/`AddBan` thresholds + `OnClubFixturePlayed` serving + `RollToNextSeason`/`MigratePlayerId`/`DropPlayer` (§3.2/§3.3/§3.4) — the sole mutating entry point onto `DisciplineState` |
| `Availability.cs` | `IsAvailable` + `MarkSuspended` (#30's composed-seam contribution) + `FilterAvailable` (KD-4) |
| `DisciplineSaveCodec.cs` | `DISCIPLINE_SAVE_FORMAT_VERSION` sub-blob encode/decode (KD-1) |
| `DisciplineConstants.cs` | the Appendix A catalogue |
| `AssemblyInfo.cs` | assembly metadata (FR-CS-055) |

## 4.3 The tap read (KD-2)

#44 consumes the **same read-only per-tick ledger-tap pattern #37 pinned** (FR-AN-002): during an
engine-resolved fixture the root feeds each tick's Tier A records to the fold; unknown ordinals
are ignored (FR-DC-004); the fold is pure accumulation (observer-neutral, digest-locked). When
#37 and #44 are both built, **one tap feeds both** — a composition-root concern, not a #44
surface. No `EventBus` registration, no ledger-byte parsing, no engine reference.

## 4.4 Save composition (KD-1)

`DisciplineSaveCodec.Encode(in DisciplineState) → byte[]` produces the opaque sub-blob; the root
appends it to #30's `SeasonSaveCodec` frame (the sibling precedent; outer
`SEASON_SAVE_FORMAT_VERSION` bump coordinated at T1, landed 5 → 6 at ERR-030-035). Fail-loud posture:
**magic first, then the version gate** (`DISCIPLINE_SAVE_MAGIC` = `"DISC"`, checked BEFORE
`DISCIPLINE_SAVE_FORMAT_VERSION`), overflow-safe `Require` against `total − offset`, trailing-byte
guard, strict-ascending `(PlayerId, CompetitionId)` order, non-negative value gates (F3). Layout in
Appendix B. **No RNG-state field** (FR-DC-016).

**A format version is not a format identifier (MUST, ERR-044-001).** Every sub-blob format under
the season frame sits at version 1 (`TRAINING_`/`MEDICAL_`/`APPEARANCE_`/`PROGRESSION_SAVE_FORMAT_VERSION`
all = 1), and a bare `version | entryCount | entries…` prefix is byte-shaped identically across all
of them — so without a self-identifying magic, a transposed `byte[]` among `SeasonSaveCodec.Encode`'s
now-seven identically-typed payloads decodes cleanly, completely and silently as the wrong
subsystem's state. This is the fourth instance of the defect ERR-029-005 / ERR-041-009 turned into a
MUST in #29 §4.4 and #41 §4.4, and ERR-028-004 hit it again at #28; #44's own Appendix B originally
specified the block version-first with no magic, which this section and Appendix B now correct.

## 4.5 Interface contracts recorded for the composition root & #30

- **The composition root** MUST: seed the fold with the fixture's full lineup mapping (starting +
  bench identities) before kickoff and feed the tap every tick (lossless); run `FilterAvailable`
  at the resolve→configure seam on **both clubs' resolved squads of every fixture on both
  resolution paths** (FR-DC-010, re-scoped at ERR-044-002 — the seam does not run on the engine
  boot alone); call `OnClubFixturePlayed` once per played fixture per club, **passing the club's
  fielded eleven** (both resolution paths; FR-DC-011, amended ERR-044-003 stage 1 — the eleven is
  what lets the call exempt a player who appeared through the extremis back-fill from serving that
  same fixture); route the roster re-key/retirement events to
  the migrate/drop hygiene (T-phase); and compose the sub-blob. It MUST NOT let the UI mutate
  `DisciplineState` directly.
- **#30** — the ERR-030-009 null seam (resolve → *filter* → configure) is the one spec-text
  change, filed at approval; the outer save bump is T1.
- **#37** — no change; the shared-tap composition is recorded from #44's side here (one tap,
  two folds).
- **#31/#28** — no change; their existing roster-event surfaces deliver the hygiene at T-phase.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §4 (assembly/reference direction, file layout, the tap read, save composition, root/#30 contracts), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Cross-set AR pass 3 (M follow-through): the root contract's filter clause scoped to **both clubs' resolved squads** of the managed fixture (FR-DC-010). |
| 0.3 | 2026-08-13 | — | **C1/C2 landing back-prop.** **ERR-044-001:** §4.4 states the magic-before-version rule as a MUST (the ERR-029-005/ERR-041-009 class's fourth instance) and cites the `SEASON_SAVE_FORMAT_VERSION` 5 → 6 bump landed at ERR-030-035. **ERR-044-002:** §4.5's root contract re-scoped from "the managed fixture" to both clubs' resolved squads of every fixture on both resolution paths. |
| 0.4 | 2026-08-13 | — | **L6** (adversarial review over the C1/C2 landing): §4.1's "at the T-phase" and §4.2's "proposed, at T-phase" headers corrected to say the assembly and its layout are landed, pointing at `file-manifest.md` as the authoritative inventory. |
| 0.5 | 2026-08-13 | — | **L12(b)**, a third adversarial-review pass: §4.2's file table gains the three files it omitted — `DisciplineEntry.cs` (the tally row type), `IDisciplineTickLedgerTap.cs` (the tap interface `CardLedgerFold.ObserveTick` consumes), `AssemblyInfo.cs` — bringing it to all 9 files `src/discipline/` carries; the `DisciplineRules.cs` and `Availability.cs` rows widened from the two methods each was first written against to the full landed API (`ApplyCard`/`RollToNextSeason`/`MigratePlayerId`/`DropPlayer`; `MarkSuspended`). |
| 0.6 | 2026-08-15 | — | **ERR-044-003 stage 1**, owner decision: §4.5's composition-root contract corrected — the `OnClubFixturePlayed` call MUST pass the club's fielded eleven, not just its id, so a played fixture the banned player appeared in (via #30 §2.3 F9's extremis back-fill) does not serve his ban. |
| 0.7 | 2026-08-15 | — | **L20** (#44 adversarial-review round 4, `open-issues.md`): §4.1 named 2 of `discipline.asmdef`'s 4 declared references. Added `TacticalDirector.DeterministicSim` (`CanonicalSerializer`/`SaveBlobFramingHelpers`, consumed by `DisciplineSaveCodec.cs`) and `TacticalDirector.ProjectConstants` (`GameplayConfigHolder`, consumed by `DisciplineConstants.cs`), both verified directly against the asmdef and the citing `.cs` files; clarified that the `DeterministicSim` reference is the byte-level serializer, not #16's RNG service (#44 registers none, FR-DC-019). Reference diagram updated to show all four. |
#endregion
