# Discipline & Suspensions #44 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.3 — cross-set AR pass 3; prior v0.2 PASS-1, v0.1 initial)
**Version:** 0.3
**Status:** APPROVED

---

## 2.1 Functional requirements (FR-DC-001..022)

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-DC-001 | #44 MUST be a **read-only derivation**: it MUST NOT mutate engine state, #27 `Squad`/`PlayerRecord`, or #30 season state; its only owned mutable state is `DisciplineState`. | MUST | KD-4 |
| FR-DC-002 | The read MUST be the **#37-class read-only per-tick ledger tap** (the FR-AN-002 pattern — consumed every engine tick of an engine-resolved fixture, lossless); #44 MUST NOT parse `SerializeLedger` bytes, read post-match per-slot discipline state, or register a new subscription pattern. | MUST | KD-2 |
| FR-DC-003 | Tap consumption MUST be **observer-neutral**: an observed fixture is digest-identical to the same fixture unobserved (the `match-viewer` lock). | MUST | KD-7 |
| FR-DC-004 | Unknown Tier A ordinals on the tap MUST be ignored (the FR-AN-019/F5 forward-compatibility posture); #44 folds only `CardIssuedEvent` (0x06) and `SubstitutionEvent` (0x08). | MUST | KD-2 |
| FR-DC-005 | The fold MUST attribute each card to the **`PlayerId` occupying the recipient agent slot at the card's tick**: occupancy seeds from the fixture's configured lineup (root-supplied) and updates on each `SubstitutionEvent`, consumed in the bus's canonical publish order. | MUST | KD-2 |
| FR-DC-006 | The de-dup rule IS the verified emission contract: kind 0 ⇒ `Yellows += 1`; kind 2 (SecondYellow — a **single** event) ⇒ `Yellows += 1` AND a `SECOND_YELLOW_BAN_MATCHES` ban; kind 1 ⇒ a `STRAIGHT_RED_BAN_MATCHES` ban (no yellow). #44 MUST NOT expect or synthesize a separate red event after a kind-2. | MUST | KD-5 |
| FR-DC-007 | When `Yellows ≥ YELLOW_ACCUMULATION_THRESHOLD`, an `ACCUM_BAN_MATCHES` ban MUST be added and `Yellows` MUST be reduced by the threshold (residual kept); bans from any source MUST **stack additively** on `BanMatchesRemaining`. | MUST | §3.2 |
| FR-DC-008 | A player MUST be unavailable while `BanMatchesRemaining > 0`; `IsAvailable` MUST be a pure predicate over `DisciplineState`. | MUST | KD-4 |
| FR-DC-009 | `FilterAvailable(in Squad) → Squad` MUST return a **reduced value copy** (available players only) for `ConfigureSquads`; it MUST NOT write #27 state; with no active ban it MUST pass the squad through unchanged. | MUST | KD-4 |
| FR-DC-010 | The filter MUST act at #30's pre-declared **resolve→configure** seam (ERR-030-009) and MUST apply to **each** resolved squad of the engine-resolved fixture — the managed club's **and its opponent's** (both pass through `ResolveByClubId` → `ConfigureSquads`, so both pass the seam; a banned opponent is excluded exactly as a banned managed-club player is). The fold MUST complete at fixture resolution — so a card in fixture N bans for fixture N+1 (no off-by-one). | MUST | KD-3 |
| FR-DC-011 | A ban MUST decrement by exactly one per **played fixture of the player's club**, regardless of resolution path (engine-resolved or quick-sim); serving MUST be reported via `OnClubFixturePlayed`. | MUST | KD-3 |
| FR-DC-012 | The tally MUST key `(PlayerId, CompetitionId)` with `CompetitionId = 0` at minimal (an `int` key — no #43 assembly reference); #43-scoped accumulation is a partition activation, not a rewrite. | MUST | KD-6 |
| FR-DC-013 | On a roster **re-key** (#31 transfer) the entry — tally **and** unserved bans — MUST **migrate** old→new `PlayerId` (bans follow the player; the deliberate contrast with #32's drop rule); on **retirement** the entry MUST be dropped. Delivery: the FR-TX-022 hook / #28 lifecycle coordination (T-phase wiring). | MUST | KD-6 |
| FR-DC-014 | #44 state MUST persist as an opaque, independently version-gated `DISCIPLINE_SAVE_FORMAT_VERSION` sub-blob composed into #30's `SeasonSaveCodec`; no `WORLD_STORE_FORMAT_VERSION` bump; recompute-on-load is not an option (no ledgers are retained — KD-1). | MUST | KD-1 |
| FR-DC-015 | The sub-blob codec MUST fail loud (F3) on a version mismatch, an out-of-bounds length prefix (overflow-safe `total − offset`), trailing bytes, non-ascending `(PlayerId, CompetitionId)` entries, or out-of-range values (`Yellows < 0`, `BanMatchesRemaining < 0`). | MUST | KD-1/F3 |
| FR-DC-016 | The serialized block MUST contain no RNG-state field of any kind (#44 has none — the read-only class). | MUST | §1.5 |
| FR-DC-017 | At `RollToNextSeason`: `Yellows` MUST reset to 0; **unserved `BanMatchesRemaining` MUST carry**; genesis is the empty state; a load MUST reconstruct and never reset a ban. An entry that reaches `(Yellows = 0, BanMatchesRemaining = 0)` — at the boundary sweep **or mid-season** (a served ban with no residual yellows) — MUST be **dropped immediately** (canonical minimal representation: an all-zero entry and an absent entry must never both be encodable states, or two equivalent runs serialize different bytes). | MUST | KD-8 |
| FR-DC-018 | A season with no threshold-crossing cards MUST be byte-identical to pre-#44 **except #44's own sub-blob** (sub-threshold yellows accrue there; the filter passes through). | MUST | KD-7 |
| FR-DC-019 | #44 MUST register **no** RNG stream, domain tag, or `SubsystemOrdinals` entry (the #37/#49 read-only class — a positive property, no #16 row needed); quick-sim card synthesis, if ever built, is a **#30-owned** extension on #30's `0x22` stream, never a #44 stream. | MUST | §1.5 |
| FR-DC-020 | Every tally/threshold/ban field MUST be integer; #44 MUST introduce **no** float. | MUST | §1.5 |
| FR-DC-021 | Same fixture events ⇒ same tallies, bans, and filtered squads — two-run deterministic; the fold's result MUST be independent of when the tap records are consumed within a tick (canonical order only). | MUST | §1.5 |
| FR-DC-022 | #44 MUST build no #38/#43/#46 interface (FR-LW-031); availability/suspension views for #38 are read-only value copies. | MUST | KD-4 |

## 2.2 Data structures

```csharp
// The per-player season tally (serialized, KD-1). Keyed (PlayerId, CompetitionId); canonical
// ascending order; CompetitionId = 0 at minimal (FR-DC-012). All integer.
public sealed class DisciplineState
{ /* map (int PlayerId, int CompetitionId) -> (int Yellows, int BanMatchesRemaining); NO RNG state */ }

// KD-2 — the read-only fold, fed by the #37-class per-tick tap during an engine-resolved fixture.
public sealed class CardLedgerFold
{
    /* slot->PlayerId occupancy (seeded from the root's configured lineup, incl. bench identities);
       SubstitutionEvent updates occupancy; CardIssuedEvent attributes to the occupant at its tick
       and applies FR-DC-006/007. Unknown ordinals ignored (FR-DC-004). */
}

// KD-4 — the availability view (pure; never mutates #27 state).
public static bool  IsAvailable(in DisciplineState s, int playerId, int competitionId = 0);
public static Squad FilterAvailable(in Squad resolved, in DisciplineState s);   // reduced VALUE COPY

// KD-3 — ban serving (one decrement per played club fixture, either resolution path).
public void OnClubFixturePlayed(int clubId /*, int competitionId = 0 */);
```

## 2.3 Failure modes

| ID | Condition | Handling |
|---|---|---|
| **F1** | A fold record referencing an agent slot with no occupancy mapping (a card/sub for an unmapped id) | **Fail loud** — the lineup seed is incomplete, a root-contract bug; silent misattribution is the trap. |
| **F2** | `OnClubFixturePlayed`/`FilterAvailable` naming a club/player outside the resolvable universe; a migration for an unknown source entry | **Fail loud** — identity validity is a caller-contract bug (the #31 F6 class). |
| **F3** | Discipline sub-blob: bad version / out-of-bounds length / trailing bytes / non-ascending keys / negative values | **Fail loud** — the `SeasonSaveCodec` posture (FR-DC-015). |
| **F4** | A `CardKind` outside `{0, 1, 2}` on the tap | **Fail loud** — an unknown card kind is an engine-contract change #44 must not guess about (contrast F5-class unknown *ordinals*, which are ignored — a known event with an unknown *payload value* is different). |
| **F5** | `FilterAvailable` reducing a squad below the engine's minimum viable size (fewer than the 18 `ConfigureSquads` consumes) | **Fail loud** — a mass-suspension edge the caller must surface, never silently padded (the `ConfigureSquads` bounds gate would reject it downstream anyway; #44 fails first with the better message). |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §2 (FR-DC-001..022, data structures, F1..F5), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1 (M): FR-DC-017 gains the **immediate `(0,0)`-drop canonical-minimality rule** (an all-zero entry and an absent entry must never both be encodable — a serialized-representation determinism hazard the v0.1 boundary-only phrasing left open). |
| 0.3 | 2026-07-24 | — | Cross-set AR pass 3 (M): FR-DC-010 pins **both-squads filter coverage** — the seam applies to each resolved squad of the engine-resolved fixture (managed club AND opponent); the unscoped v0.2 wording let a managed-squad-only implementation pass every test while banned opponents played through their bans. |
#endregion
