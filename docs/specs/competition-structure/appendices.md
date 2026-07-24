# Competition Structure #43 — Appendices

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## Appendix A — Constant catalogue

| Constant | Tag | Value | Notes |
|---|---|---|---|
| `COMPETITION_SAVE_FORMAT_VERSION` | `[FIXED]` | 1 | the competition sub-blob's own version gate (KD-6). |
| `LEAGUE_COMPETITION_ID` | `[FIXED]` | 0 | instance 0 — the #30 league binding (FR-CP-004). |
| `PROMOTION_COUNT` / `RELEGATION_COUNT` | `[GT]` | 3 / 3 (illustrative) | the season-boundary swap sizes; MUST be equal (a squad-size-preserving swap, §3.4); balance-pass-pinned. |
| `CP_ROUND_RADIX` | `[FIXED]` | 32 | fixed ordinal radix above any bracket's round count (§3.2); never derived from a live count (append-parity). |
| `CP_SLOT_RADIX` | `[FIXED]` | 256 | fixed ordinal radix above any round's slot count (128-entrant brackets covered). |
| `CP_PURPOSE_RADIX` | `[FIXED]` | 16 | fixed ordinal radix for `CompetitionDrawPurpose` (APPEND-only; `Pairing = 0`, `GroupAssign = 1`). |
| `CUP_ROUND_SPACING_DAYS` | `[GT]` | illustrative | *(deep)* the merged-view slotting spacing (§3.5); balance-pass-pinned. |
| `CLUB_SQUAD_SIZE` / club-id universe | `[CROSS]` | #27 | entrant identity (stable `ClubId`s). |
| `_RESERVED_0x2C_` / `SubsystemOrdinals.Competition = 94` | `[CROSS]` | #16 | created by ERR-043-001; RESERVED at approval (draw-free minimal); promotes at the deep first draw. |

**Tag note:** the `[GT]` magnitudes are illustrative pending a balance pass — the reviewed contract
is the shapes (equal-count swap, fixed radices, congestion-free slotting), not the numbers (the
#21 G2 precedent). The radices are `[FIXED]` because key-parity is a load-bearing contract.

## Appendix B — Competition sub-blob layout (KD-6)

Composed into #30's `SeasonSaveCodec` frame as an opaque, independently version-gated block (the
sibling posture; every length-prefixed read `Require`-bounded against `total − offset`):

| Field | Type | Notes |
|---|---|---|
| version | u32 | `COMPETITION_SAVE_FORMAT_VERSION`; **gate first** (F3) |
| instanceCount | u32 | `Require`-bounded (1 at minimal — the instance-0 binding) |
| per instance: CompetitionId | i32 | **strictly ascending** (F4); 0 = the league binding |
| per instance: Format | u8 | ordinal-stable |
| per instance: entrantCount + EntrantClubIds | u32 + i32×n | **strictly ascending `ClubId`** (F4); 0 entrants for instance 0 (lives in #30) |
| per RoundRobin instance: fixtures + table | — | the #30 value types, canonically serialized (absent for instance 0) |
| per Knockout instance: roundCount; per round: entrants (drawn order) + winners | u32 + i32×… | the F4 coherence gates run at decode (winner ∈ pairing; halving) |
| division chain (deep): divisionCount + per-division CompetitionId | u32 + i32×n | the promotion/relegation subject (KD-4); absent/0 at minimal |
| (trailing-byte guard) | — | `if (o != len) throw` (F3) |

**No `RngCursor`/`actionOrdinal` field** (FR-CP-014 — keyed draws). Deep extensions (two-legged
ties, seeding pots) **append** behind the version gate; the layout above is never reordered.
Instance 0 carries no fixture/table data — the league lives in #30's blob (one source of truth).

## Appendix C — Worked examples

**C.1 Keyed draw (§3.2).** Cup `competitionId = 5`, season 0, round 0, canonical entrants
`[3, 7, 12, 20]`: draws (mod 4, 3, 2) of `2, 0, 1` produce `[12, 7, 20, 3]` ⇒ pairings
**12 v 7, 20 v 3**. Stable across call orders, days, saves; shuffled input canonicalizes to the
same result; competition 6 drawing the same day is keyed under its own `entityId` and cannot
perturb this sequence.

**C.2 Promotion/relegation (§3.4).** Two 12-club divisions, counts = 3: div-1 finishers 10/11/12
(clubs 8, 2, 19) swap membership with div-2 finishers 1/2/3 (clubs 30, 27, 41). Step (c) then
regenerates both divisions' fixtures from the **post-swap** club sets (FR-CP-017). Every club
keeps its `ClubId`; squads, finances, and knowledge overlays are untouched (FR-CP-016). Same
standings ⇒ same swap, two runs identical.

**C.3 Minimal identity.** A singleton collection: the sub-blob is `version=1, instanceCount=1,
{0, RoundRobin, 0 entrants}` — and the season's every byte matches bare #30 (T-CP-NEU-001).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial appendices (constant catalogue, sub-blob layout, worked draw/promotion/identity examples), promoted from design supplement v0.3. Status IN REVIEW. |
#endregion
