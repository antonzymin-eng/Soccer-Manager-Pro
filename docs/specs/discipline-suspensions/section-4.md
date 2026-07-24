# Discipline & Suspensions #44 — Section 4: Architecture

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## 4.1 Assembly & reference direction

New assembly **`TacticalDirector.Discipline`** (`src/discipline/`, at the T-phase). References
**`#17 EventSystem`** (the `CardIssuedEvent`/`SubstitutionEvent` value types the tap yields) and
**`#27 PlayerDatabase`** (`PlayerId`/`Squad`, read-only — `FilterAvailable` returns a value
copy). It references **neither #30 nor #43 nor #38 nor the match engine nor #16's RNG service**
— the composition root wires the tap around engine-resolved fixtures, threads the lineup mapping
in, applies the filter at the ERR-030-009 seam, and reports played fixtures/roster events.

```
compositionRoot (season loop) ──► #44 Discipline ──► { #17 (event types), #27 (read-only) }
        │                                ▲
        └─ taps the fixture's events /   └── #38 (screens), #43 (partitions), #46 (news)
           threads lineup / applies          — deferred consumers, no interface built (FR-LW-031)
           the filter / reports fixtures
```

Acyclic; no consumer references #44. **No RNG stream/tag/ordinal** — no #16 row exists or is
needed (the #37/#49 positive property).

## 4.2 File layout (proposed, at T-phase)

| File | Contents |
|---|---|
| `DisciplineState.cs` | the `(PlayerId, CompetitionId)` tally map (KD-1/KD-6) |
| `CardLedgerFold.cs` | the occupancy fold over the tap (KD-2/KD-5, §3.1) |
| `DisciplineRules.cs` | `AddYellow`/`AddBan` thresholds + `OnClubFixturePlayed` serving (§3.2/§3.3) |
| `Availability.cs` | `IsAvailable` + `FilterAvailable` (KD-4) |
| `DisciplineSaveCodec.cs` | `DISCIPLINE_SAVE_FORMAT_VERSION` sub-blob encode/decode (KD-1) |
| `DisciplineConstants.cs` | the Appendix A catalogue |

## 4.3 The tap read (KD-2)

#44 consumes the **same read-only per-tick ledger-tap pattern #37 pinned** (FR-AN-002): during an
engine-resolved fixture the root feeds each tick's Tier A records to the fold; unknown ordinals
are ignored (FR-DC-004); the fold is pure accumulation (observer-neutral, digest-locked). When
#37 and #44 are both built, **one tap feeds both** — a composition-root concern, not a #44
surface. No `EventBus` registration, no ledger-byte parsing, no engine reference.

## 4.4 Save composition (KD-1)

`DisciplineSaveCodec.Encode(in DisciplineState) → byte[]` produces the opaque sub-blob; the root
appends it to #30's `SeasonSaveCodec` frame (the sibling precedent; outer
`SEASON_SAVE_FORMAT_VERSION` bump coordinated at T1, exact version TBD). Fail-loud posture:
version-gate first, overflow-safe `Require` against `total − offset`, trailing-byte guard,
strict-ascending `(PlayerId, CompetitionId)` order, non-negative value gates (F3). Layout in
Appendix B. **No RNG-state field** (FR-DC-016).

## 4.5 Interface contracts recorded for the composition root & #30

- **The composition root** MUST: seed the fold with the fixture's full lineup mapping (starting +
  bench identities) before kickoff and feed the tap every tick (lossless); run `FilterAvailable`
  at the resolve→configure seam for the managed fixture; call `OnClubFixturePlayed` once per
  played fixture per club (both resolution paths); route the roster re-key/retirement events to
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
#endregion
