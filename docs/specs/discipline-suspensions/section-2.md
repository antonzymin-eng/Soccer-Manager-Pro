# Discipline & Suspensions #44 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 24, 2026
**Last Updated:** August 13, 2026, later same day (v0.5 — ERR-044-004 + ERR-044-005, back-props owed
by the #44 C1/C2 adversarial review: F2's fail-loud stated explicitly for a negative/unresolvable
`PlayerId`, not just a negative `clubId`; FR-DC-009 gains the all-suspended-squad `null`-return case)
**Last Updated (prior):** August 13, 2026 (v0.4 — ERR-044-002 + ERR-044-003, C1/C2 landing back-prop: FR-DC-010
re-scoped off "the engine-resolved fixture" to every resolved squad on both resolution paths; F5's
fail-loud withdrawn in favour of #30 §2.3 F9, with the suspension-as-stricter-reinstatement-tier
decision recorded)
**Last Updated (prior):** July 24, 2026 (v0.3 — cross-set AR pass 3; prior v0.2 PASS-1, v0.1 initial)
**Version:** 0.5
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
| FR-DC-009 | `FilterAvailable(in Squad) → Squad` MUST return a **reduced value copy** (available players only) for `ConfigureSquads`; it MUST NOT write #27 state; with no active ban it MUST pass the squad through unchanged. **When every player is suspended there is no reduced value copy to return — `Squad` cannot represent a zero-player roster — so `FilterAvailable` MUST return `null` for that case** (ERR-044-005; `Squad`'s own constructor refuses `players.Length == 0`, so returning it as a normal squad is not an option). This method is FR-DC-009's own surface; #44's production path is `MarkSuspended`'s removal mask, consumed directly by #30's composed availability seam. | MUST | KD-4 |
| FR-DC-010 | The filter MUST act at #30's pre-declared **resolve→configure** seam (ERR-030-009) and MUST apply to **every resolved squad of every fixture on both resolution paths** (the engine boot and the quick-sim rating alike) — the managed club's **and its opponent's**, whichever path resolved them (both pass through `ResolveByClubId` → `ConfigureSquads`, so both pass the seam; a banned opponent is excluded exactly as a banned managed-club player is). **Card *generation* stays engine-fixture-only at minimal (§3.3) — this row governs the filter, not the fold.** The fold MUST complete at fixture resolution — so a card in fixture N bans for fixture N+1 (no off-by-one). *(Re-scoped from "the engine-resolved fixture" — ERR-044-002, August 13, 2026: the narrower wording contradicted FR-DC-011's "regardless of resolution path" one row below and #30 §3.4's LIVE both-paths seam; a quick-sim-only implementation would have let a banned player's club decrement his ban on a fixture he had just played through.)* | MUST | KD-3 |
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
| **F2** | `OnClubFixturePlayed`/`FilterAvailable` naming a club/player outside the resolvable universe; a migration for an unknown source entry. **The player half is explicit: a negative or otherwise unresolvable `PlayerId` MUST be refused, not just a negative `clubId`** — C# integer division truncates toward zero, so every id in `[-CLUB_SQUAD_SIZE + 1, -1]` would otherwise derive to club 0 in `OnClubFixturePlayed` and be served, decremented and migrated as one of its players, silently (ERR-044-004). Refused at BOTH boundaries: `DisciplineEntry`'s constructor and `DisciplineSaveCodec.Decode` (F3). | **Fail loud** — identity validity is a caller-contract bug (the #31 F6 class). |
| **F3** | Discipline sub-blob: bad version / out-of-bounds length / trailing bytes / non-ascending keys / negative values | **Fail loud** — the `SeasonSaveCodec` posture (FR-DC-015). |
| **F4** | A `CardKind` outside `{0, 1, 2}` on the tap | **Fail loud** — an unknown card kind is an engine-contract change #44 must not guess about (contrast F5-class unknown *ordinals*, which are ignored — a known event with an unknown *payload value* is different). |
| **F5** | *(WITHDRAWN as a fail-loud, ERR-044-003, August 13, 2026 — see the note below the table.)* `FilterAvailable` reducing a squad below the engine's minimum viable size (fewer than the 18 `ConfigureSquads` consumes) | #44 contributes **removals only**; the composed seam's viability rule is **#30 §2.3 F9** (Season & Competition Loop, approved after this row was written). #44's `FilterAvailable`/`MarkSuspended` implement no viability gate at all — see below. |

**ERR-044-003 (F5 vs #30 §2.3 F9).** This spec's original F5 required `FilterAvailable` to fail loud
the moment it reduced a squad below eighteen. #30 §2.3 F9 / §3.4 (ERR-030-029, approved after #44)
settles the identical event the opposite way at the one seam both specs share: back-fill the
least-injured players in one at a time, probing the engine's own selector
(`SquadRating.CanFieldStartingEleven`), and fail loud only if the **whole** squad cannot field the
formation — and states outright that "the rule is #30's because FR-MD-023 puts selection on this
side of the seam; #44/#36 contribute removals only and inherit the rule unchanged when they join."
Two viability rules of opposite posture for one event on one shared method cannot both hold.
**#30 wins** — implementing #44's F5 as written would also wedge a career permanently mid-save on a
mass-suspension season, reachable at the engine's measured card rate (§1.5). `src/discipline/Availability.cs`
implements no viability gate; the composition and the single back-fill live in
`src/season-save/AvailabilityComposition.cs`.

**Recorded, not fixed — an owner decision, not a repair.** Preserving #30 §3.4's stated invariant
("the composed filter can never leave a club worse off than having no filter at all") means a
suspended player **is** reinstatable in extremis, which the Laws of the Game do not allow. The
implementation makes suspension a **stricter reinstatement tier** than injury — every injured player
is pressed back before any suspended one, and a suspended player plays only when the alternative is a
club that cannot take the field at all. §7.2's deferral queue is the designed alternative if the owner
would rather refuse the fixture than field a banned player; see that section for the note recording
this as now a live decision.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §2 (FR-DC-001..022, data structures, F1..F5), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1 (M): FR-DC-017 gains the **immediate `(0,0)`-drop canonical-minimality rule** (an all-zero entry and an absent entry must never both be encodable — a serialized-representation determinism hazard the v0.1 boundary-only phrasing left open). |
| 0.3 | 2026-07-24 | — | Cross-set AR pass 3 (M): FR-DC-010 pins **both-squads filter coverage** — the seam applies to each resolved squad of the engine-resolved fixture (managed club AND opponent); the unscoped v0.2 wording let a managed-squad-only implementation pass every test while banned opponents played through their bans. |
| 0.4 | 2026-08-13 | — | **C1/C2 landing back-prop.** **ERR-044-002:** FR-DC-010's "the engine-resolved fixture" contradicted FR-DC-011's "regardless of resolution path" one row below and #30 §3.4's LIVE both-paths seam; re-scoped to every resolved squad of every fixture on both resolution paths. **ERR-044-003:** F5's fail-loud withdrawn — #30 §2.3 F9 (approved after this spec) settles the same depleted-squad event by back-filling instead, and #44 contributes removals only; recorded that a suspended player is reinstatable in extremis under #30's never-worse-than-unfiltered invariant, making suspension a stricter reinstatement tier than injury rather than an absolute bar. |
| 0.5 | 2026-08-13 | — | **Adversarial-review back-prop.** **ERR-044-004:** F2 stated only "a club/player outside the resolvable universe" and the implementation had guarded the club half alone — a negative `PlayerId` truncation-derives to club 0 and was silently served, decremented and migrated; F2 now names the player half explicitly and cites both refusal sites (`DisciplineEntry`'s constructor, `DisciplineSaveCodec.Decode`/F3). **ERR-044-005:** FR-DC-009's "reduced value copy" requirement was total as written but unsatisfiable for an all-suspended squad (`Squad` cannot represent zero players); FR-DC-009 now states the `null`-return case and names `MarkSuspended`'s mask, consumed by #30's composed seam, as the actual production path — `FilterAvailable` is FR-DC-009's own surface, not #44's. |
#endregion
