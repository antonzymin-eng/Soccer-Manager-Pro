# Discipline & Suspensions #44 — Appendices

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## Appendix A — Constant catalogue

| Constant | Tag | Value | Notes |
|---|---|---|---|
| `DISCIPLINE_SAVE_FORMAT_VERSION` | `[FIXED]` | 1 | the discipline sub-blob's own version gate (KD-1). |
| `YELLOW_ACCUMULATION_THRESHOLD` | `[GT]` | 5 (illustrative) | yellows per accumulation ban (§3.2); balance-pass-pinned against real-competition rules. |
| `ACCUM_BAN_MATCHES` | `[GT]` | 1 (illustrative) | ban length for an accumulation threshold crossing. |
| `SECOND_YELLOW_BAN_MATCHES` | `[GT]` | 1 (illustrative) | ban length for a kind-2 dismissal. |
| `STRAIGHT_RED_BAN_MATCHES` | `[GT]` | 2 (illustrative) | ban length for a kind-1 dismissal. |
| `LEAGUE_COMPETITION_KEY` | `[FIXED]` | 0 | the minimal-tier `CompetitionId` partition key (FR-DC-012; aligns with #43's `LEAGUE_COMPETITION_ID = 0`). |
| `CardIssuedEvent` 0x06 / `SubstitutionEvent` 0x08 | `[CROSS]` | #17/engine | the fold's inputs (payloads verified — XC-044-001); kinds `{0,1,2}` with the single-event kind-2 contract. |
| the 18-slot `ConfigureSquads` minimum | `[CROSS]` | match engine | the F5 filter floor. |

**Tag note:** the `[GT]` magnitudes are illustrative pending the balance pass (the #21 G2
precedent) — the reviewed contract is the shapes (threshold-and-residual accumulation, additive
stacking, per-fixture serving), not the numbers.

## Appendix B — Discipline sub-blob layout (KD-1)

Composed into #30's `SeasonSaveCodec` frame as an opaque, independently version-gated block
(every length-prefixed read `Require`-bounded against `total − offset`):

| Field | Type | Notes |
|---|---|---|
| version | u32 | `DISCIPLINE_SAVE_FORMAT_VERSION`; **gate first** (F3) |
| entryCount | u32 | `Require`-bounded (0 at genesis) |
| per entry: PlayerId | i32 | **strictly ascending `(PlayerId, CompetitionId)`** across entries (F3) |
| per entry: CompetitionId | i32 | `0` at minimal (the #43 partition key) |
| per entry: Yellows | i32 | `≥ 0` (F3) |
| per entry: BanMatchesRemaining | i32 | `≥ 0` (F3); carries across `RollToNextSeason` |
| (trailing-byte guard) | — | `if (o != len) throw` (F3) |

**No RNG-state field of any kind** (FR-DC-016 — #44 has none). Deep extensions (per-offence
classes) **append** behind the version gate.

## Appendix C — Worked fold example (end to end)

Fixture N (engine-resolved). Lineup seeds slot 7 → PlayerId 183, bench slot 19 → PlayerId 201.
Tap sequence: tick 4 000 `CardIssuedEvent{Recipient: 7, Kind: 0}` → 183 `Yellows` 4 → 5 ⇒ ban 1,
`Yellows` 0 (threshold 5). Tick 9 000 `SubstitutionEvent{Outgoing: 7, Incoming: 19}` → occupancy
7 → 201 (or 19 → 201, absorbed either way). Tick 12 000 `CardIssuedEvent{Recipient: <occupied>,
Kind: 2}` → **201** `Yellows` +1 **and** ban +1 (one event, one yellow, one dismissal — KD-5).
Fixture N+1 selection: `FilterAvailable` excludes 183 and 201; after N+1 is played,
`OnClubFixturePlayed` decrements both to 0 — available for N+2. The engine's slot-7 yellow count
was reset by the substitution (v1.33) and **never read** — the tally kept 183's card. All
integer; two runs identical; #27 squads byte-untouched.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial appendices (constant catalogue, sub-blob layout, end-to-end worked fold example), promoted from design supplement v0.3. Status IN REVIEW. |
#endregion
