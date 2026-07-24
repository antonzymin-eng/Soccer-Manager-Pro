# Scouting & Player Knowledge #32 — Section 4: Architecture

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## 4.1 Assembly & reference direction

New assembly **`TacticalDirector.Scouting`** (`src/scouting/`, at the T-phase). References **`#27
PlayerDatabase`** (the `PlayerRecord`/`PlayerAttributes`/`AttrIdx` truth types, **read-only** — the
minimal subset) and, at the **deep tier**, **`#34 Staff`** (`ToScoutQuality` + the `StaffRecord` it
takes) and **`#16 DeterministicSim`** (the world-tick `DeterministicRngService` for keyed draws). It
references **neither #30 nor #31 nor #33 nor #28 nor #38 nor #22 nor #49** — the composition root
(the season loop, which owns `SeasonSave`) invokes #32 and threads truth records into the view calls,
so the reference is one-way.

```
compositionRoot (season loop) ──► #32 Scouting ──► { #27 (read-only) }          (minimal)
        │                              │  └───────► { #34, #16 }                (deep)
        │                              ▲
        └─ invokes the world-tick      └── #38 (render), #46 (aggregate), #42 (youth), AI-manager fog
           slot / AssignScout /            — deferred consumers, no interface built (FR-LW-031)
           threads truth via ISquadProvider
```

Acyclic; no consumer references #32. #27/#34/#31/#30/#16 stay **schema-untouched at approval** — #32
reads existing surfaces only.

## 4.2 File layout (proposed, at T-phase)

| File | Contents |
|---|---|
| `AttributeEstimate.cs` / `KnownPlayer.cs` | the readonly view value types (KD-1/KD-2) |
| `KnowledgeView.cs` | `ResolveBand` + `EstimateFor` (§3.1/§3.2 — the single view function, KD-8) |
| `ScoutOrdinal.cs` | `DeriveScoutOrdinal` (§3.3 fixed-radix bijection) + `ScoutDrawPurpose` |
| `ScoutingState.cs` | the overlay map + active assignment; canonical-order enumeration (FR-SC-017) |
| `ScoutingAssignments.cs` | `AssignScout` / `CancelAssignment` / `AdvanceScoutingDay` (§3.4, deep) |
| `ScoutRanking.cs` | `RankByEstimate` (§3.5, deep) |
| `ScoutingSaveCodec.cs` | `SCOUTING_SAVE_FORMAT_VERSION` sub-blob encode/decode (KD-6) |
| `ScoutingConstants.cs` | the Appendix A catalogue |

## 4.3 The view seam (KD-2/KD-8)

The composition root resolves squads through the **existing** `ISquadProvider` surface and passes
`PlayerRecord` value copies into `EstimateFor` — #32 never holds a squad reference, so the
view-not-mutation invariant (FR-SC-001) is structural, not disciplinary. The view output
(`KnownPlayer`) is the #38 FR-UI-002 immutable-projection shape, so the future UI renders it without
adaptation. At minimal the composition root calls the same seam with the same function — only
`ResolveBand`'s dial branch differs (KD-8): **no consumer ever branches on `fogEnabled`.**

## 4.4 Save composition (KD-6)

`ScoutingSaveCodec.Encode(in ScoutingState) → byte[]` produces the opaque sub-blob; the composition
root appends it to #30's `SeasonSaveCodec` frame (the **#41 `MEDICAL_` / #33 `HUMAN_SYSTEMS_` / #31
`TRANSFERS_` / #34 `STAFF_SAVE_FORMAT_VERSION` precedent**, all "No `WORLD_STORE_FORMAT_VERSION`
bump"), and the outer `SEASON_SAVE_FORMAT_VERSION` bump is coordinated with #30 at T1 (**exact
version TBD** — assigned by whichever T-phase lands first, not hardcoded here). The codec mirrors the
`SeasonSaveCodec` fail-loud posture exactly: version-gate first (F3), an overflow-safe
`Require(offset, need, total)` bound against **`total − offset`** on every length-prefixed read, a
trailing-byte guard, and the **strict-ascending-`PlayerId` canonical-order gate** (F4, FR-SC-017 —
the map's iteration order must never leak into bytes). Layout in Appendix B. **No `RngCursor`** is
serialized (keyed draws, FR-SC-014).

## 4.5 Interface contracts recorded for the composition root & #30

- **The composition root** (season loop) MUST: invoke #32's world-tick step at #30's new tick-order
  slot (null at minimal); thread truth records (via `ISquadProvider`) and, deep, #34's
  `ToScoutQuality` into #32's calls; route `AssignScout`/`CancelAssignment` from the UI through the
  command seam (never mutate `ScoutingState` directly); and dispatch the roster re-key/retirement
  drops (FR-SC-019) when those hooks exist. Genesis needs **no seeding call** (the empty overlay is
  the genesis state); a load reconstructs `ScoutingState` from the sub-blob.
- **#30** MUST, at the T-phase: (a) hold the **scouting tick-order null-seam slot** (ERR-030-007,
  declared at approval — §8); (b) bump `SEASON_SAVE_FORMAT_VERSION` (coordinated at T1) composing
  the sub-blob. **No roster-commit change** — #32 only *consumes* the roster-move hook #31's
  FR-TX-022 already specifies (and the #28 season-boundary lifecycle coordination FR-TX-028 names)
  for its entry drops.
- **#34** is consumed read-only (`ToScoutQuality` of the ChiefScout slot-holder, deep); #32 defines
  the neutral baseline constant #34's spec text left open — no #34 edit.
- **#31 / #33 / #28 / #38 / #22 / #49** — no reference, no change (§1.2/§8).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §4 (assembly/reference direction, file layout, the view seam, save composition, root/#30/#34 interface contracts), promoted from design supplement v0.3. Status IN REVIEW. |
#endregion
