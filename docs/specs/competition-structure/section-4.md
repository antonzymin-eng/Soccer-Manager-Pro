# Competition Structure #43 — Section 4: Architecture

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## 4.1 Assembly & reference direction

New assembly **`TacticalDirector.Competition`** (`src/competition/`, at the T-phase). References
**`#27 PlayerDatabase`** (club-id universe, read-only) and — at the deep tier — **`#16
DeterministicSim`** (the `competition.draws` keyed stream) and **#30's pure types**
(`FixtureScheduler`/`LeagueTable` — value machinery consumed per instance). It holds **no #30
`SeasonState` reference** (instance 0 is a binding row; instance-0 reads and every `SeasonState`
mutation go through the composition root against #30's public API — FR-CP-002/017).

```
compositionRoot (season loop) ──► #43 Competition ──► { #27 (ids) }              (minimal)
        │                               │  └────────► { #16, #30 pure types }    (deep)
        │                               ▲
        └─ drives fixture days /        └── #44 (CompetitionId scoping), #36 (overlay),
           the (a') transform hook /        #40 (prize money, deep), #38 (bracket screens)
           threads #30 reads                — deferred consumers, no interface built (FR-LW-031)
```

Acyclic; no consumer references #43. #30/#40/#27 stay schema-untouched at approval; the #16 change
is the ERR-043-001 placeholder sweep only.

## 4.2 File layout (proposed, at T-phase)

| File | Contents |
|---|---|
| `CompetitionFormat.cs` / `Competition.cs` / `CompetitionSet.cs` | the format enum + instance + registry (KD-1) |
| `BracketState.cs` | persisted knockout rounds + coherence gates (KD-3) |
| `CompetitionDraws.cs` | `DeriveDrawOrdinal` + `DrawRound` keyed Fisher–Yates (KD-2/KD-7) |
| `PromotionRelegation.cs` | `ApplyPromotionRelegation` (KD-4) |
| `CompetitionCalendarView.cs` | the merged fixture-day view (KD-5) |
| `CompetitionSaveCodec.cs` | `COMPETITION_SAVE_FORMAT_VERSION` sub-blob encode/decode (KD-6) |
| `CompetitionConstants.cs` | the Appendix A catalogue |

## 4.3 The instance-0 binding (KD-1/KD-8)

The minimal collection is `{binding(0)}`: #43 stores an id row, nothing else. The composition root
answers any instance-0 query from #30's read surface (`SeasonViewModel`-class value copies,
FR-SN-033) and never routes league mutations through #43. Consequently the minimal-tier
behaviour-neutral proof is structural: #43 executes no code on the season path — a #43-present
season is #30's season plus an inert registry.

## 4.4 Save composition (KD-6)

`CompetitionSaveCodec.Encode(in CompetitionSet) → byte[]` produces the opaque sub-blob; the root
appends it to #30's `SeasonSaveCodec` frame (the #41/#33/#31/#34/#32 precedent; outer
`SEASON_SAVE_FORMAT_VERSION` bump coordinated at T1, exact version TBD). Fail-loud posture:
version-gate first (F3), overflow-safe `Require` against `total − offset`, trailing-byte guard,
canonical-order gates (ascending `CompetitionId`; ascending entrant `ClubId`s), and the F4 bracket
coherence gates at decode. Layout in Appendix B. **No `RngCursor`** (FR-CP-014). Instance 0's
league data is never duplicated (one source of truth — #30's blob).

## 4.5 Interface contracts recorded for the composition root & #30

- **The composition root** MUST: answer instance-0 reads from #30's surface; at deep, query the
  merged fixture-day view when >1 competition and drive per-instance round resolution through the
  same `MatchEngine`/quick-sim paths #30 uses; sequence the (a') transform (via the T-phase
  ERR-030-008 hook) so its membership output reaches `SeasonState.ClubIds` through #30's command
  API before roll step (c); and compose the sub-blob. It MUST NOT let the UI mutate #43 state
  directly (views are value copies, FR-CP-022).
- **#30** at the T-phase: (a) the outer save-version bump (T1); (b) the ERR-030-008 code-side
  coordinations — the (a') execution hook (T2) and the deep multi-competition fixture-day driver
  (T3). **No #30 spec-text change at approval** (FR-SN-031's (a') pre-exists).
- **#40** — nothing: the (b')-after-(a') ordering is already recorded on both sides.
- **#44/#36/#38** — deferred consumers of `CompetitionId`/the competition model/bracket views; no
  interface built (FR-LW-031).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §4 (assembly/reference direction, file layout, the instance-0 binding, save composition, root/#30 interface contracts), promoted from design supplement v0.3. Status IN REVIEW. |
#endregion
