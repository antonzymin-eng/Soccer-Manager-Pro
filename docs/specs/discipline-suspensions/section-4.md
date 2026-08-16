# Discipline & Suspensions #44 — Section 4: Architecture

**Created:** July 24, 2026
**Last Updated:** August 16, 2026, later still (v0.11 — **`ERR-044-020`** (M3), the section-4 half of
the sync the reviewed-findings pass landed into `section-2.md`/`section-3.md` and recorded open against
this file (the fixer's owned set stopped short of it). §4.3 now declares `IDisciplineTickLedgerTap`'s
`CurrentTick` member and describes `CardLedgerFold.ObserveTick`'s consecutive-tick refusal +
partial-application poison latch — the same lossless-pump contract #37's `MatchAnalyticsAggregator`
enforces, matching the wording already synced into §2.2/§3.1. §4.5's "feed the tap every tick
(lossless)" root-contract bullet now states that losslessness is enforced by the tap contract itself
(`IDisciplineTickLedgerTap.CurrentTick`), not merely upheld by the root's own discipline, so a root
that skips or reorders a tick is refused rather than silently producing a wrong tally. No FR row
changes — this is a description sync of an already-landed code contract, not a new requirement. See
`spec-error-log.md` `ERR-044-020`.)
**Last Updated (prior):** August 16, 2026 (v0.10 — two adversarial-review findings, both in §4.5's
composition-root contract. **`ERR-044-015`** (H3): the clause instructed the root to "run
`FilterAvailable` at the resolve→configure seam", the one method FR-DC-009, §2.2, §3.3 and §6.2 all
say the root must NOT call — a root built against it verbatim bypasses `AvailabilityComposition`, so
#30 §2.3 F9's back-fill never runs for suspensions and ERR-044-003's defect is reinstated; rewritten
to the landed contract (removals gathered through `Availability.MarkSuspended` into the composed
seam), keeping the FR-DC-010/ERR-044-002 both-paths scope, which was correct. **`ERR-044-014`** (H1):
the `OnClubFixturePlayed` bullet now requires the club's ROSTER as well as its fielded eleven, states
that membership is read from it rather than derived from the `PlayerId` packing, and pins it to the
UNFILTERED squad)
**Last Updated (prior):** August 15, 2026, yet later still again (v0.9 — reviewed-findings pass, continuing
`ERR-044-008`: v0.8's own sweep fixed §4.3 but stopped short of §4.5, 45 lines below in the same file
— "the shared-tap composition is recorded from #44's side here (one tap, two folds)" was the
identical refuted claim in different words, missed because the sweep was for the exact phrase rather
than the underlying claim. Restated to match §4.3's corrected account: no shared tap, #44's own
`IDisciplineTickLedgerTap`, the engine's one-per-tick fill read by independent accessor shapes.)
**Last Updated (prior):** August 15, 2026, yet later still (v0.8 — reviewed-findings pass: `ERR-044-008`
corrects §4.3's "one tap feeds both when built" — both #37 and #44 now have `src/` assemblies and
neither can reach the other's tap interface (§4.1's reference rule), so the claim is false today, not
merely unbuilt; `ERR-044-007` adds a §4.5 composition-root MUST to call
`CardLedgerFold.RequireCommittableConfig()` once per round before the first fixture resolves, which
had no normative source anywhere despite being enforced in production and unit-tested; `ERR-044-010`
records that §4.5's fielded-eleven contract is satisfied today only because no substitution seam has
a production caller, and MUST widen once one does)
**Last Updated (prior):** August 15, 2026, later still (v0.7 — L20, the spec half of #44's adversarial-review
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
**Version:** 0.11
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
are ignored (FR-DC-004); the fold is pure accumulation (observer-neutral, digest-locked). No
`EventBus` registration, no ledger-byte parsing, no engine reference.

**#44 declares its own `IDisciplineTickLedgerTap` rather than consuming #37's identically-shaped
`ITickLedgerTap` (`ERR-044-008`).** Both #37 and #44 have carried `src/` assemblies since July 27,
2026, so "one tap feeds both when built" — this section's own prior wording — is no longer a future
condition; it is a claim about today, and today it is false. §4.1 forbids `discipline` to reference
the match engine, and `season-save` (the composition root that owns the engine) does not reference
`match-analytics` either — so #37's interface is unreachable from either end, and nothing shares an
adapter type. This is not the parallel-surface trap this project otherwise flags: that trap is
duplicated **rules** that can silently diverge (the `LineupSelector.CanSelect` class); these two
interfaces are read-only accessor shapes over the same three engine methods, with no rule inside
either to diverge. What it costs: the engine's own tap is still filled exactly once per tick — one
`TickLedgerSnapshot` — but reading it through two independent accessor shapes means **two reads per
tick** where a shared adapter would have needed one. Nothing is lost; the cost is stated so a future
reader does not assume a shared adapter exists.

**`IDisciplineTickLedgerTap` (declared in §2.2) carries four members: `CurrentTick`, `RecordCount`,
`OrdinalAt(int)`, `RecordAt<T>(int)`.** `CurrentTick` (`ERR-044-020`, M3) is the engine's own clock for
the tick the other three accessors describe — `MatchEngineDisciplineTap` forwards
`MatchEngine.CurrentTick`, an already-public read, so the engine itself needed no change — mirroring
#37's `MatchAnalyticsObservation.CurrentTick`. `CardLedgerFold.ObserveTick` (§3.1) uses it to enforce a
**lossless, in-order pump**, not merely to document one: the very first call anchors on whatever tick
it is given (a fixture need not begin at tick 0); every later call must name exactly one more than the
last tick observed, or the call is refused (`InvalidOperationException`, naming both the offending and
the last-observed tick) instead of silently losing the skipped tick's records. A tick that throws
part-way through — a record failing an F1/F4 refusal after earlier records in the same tick were
already buffered — latches the fold shut: every subsequent `ObserveTick` call is refused too, even one
that is otherwise perfectly consecutive, because nothing else distinguishes "this tick was skipped"
from "this tick partially applied" once the tick counter has already advanced. `Commit` is unaffected
by the latch — it still applies whatever was buffered before the failure (§3.1's atomicity is
unchanged).

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
  bench identities) before kickoff and feed the tap every tick, in tick order, with no gaps
  (lossless) — **this is no longer only the root's own discipline to uphold**: `CardLedgerFold.ObserveTick`
  (§3.1/§4.3) enforces it itself against `IDisciplineTickLedgerTap.CurrentTick` (`ERR-044-020`), so a
  skipped tick, an out-of-order tick, or any call following one that failed part-way through is
  refused rather than silently accepted; call
  `CardLedgerFold.RequireCommittableConfig()` **once per round, before the first fixture of the
  round is resolved** (`ERR-044-007`) — a bad `[GT]` discovered only when a per-fixture `Commit`
  throws strands the round permanently, because by the time `Commit` runs the fixture is already
  marked played (§3.1's own pseudocode comment on `Commit`); gather #44's removals through
  `Availability.MarkSuspended` into the composed availability seam (`AvailabilityComposition`) at the
  resolve→configure seam, on **both clubs' resolved squads of every fixture on both resolution
  paths** (FR-DC-010, re-scoped at ERR-044-002 — the seam does not run on the engine boot alone) —
  that seam owns the intersection with #41's removals and the #30 §2.3 F9 back-fill, and
  **`FilterAvailable` MUST NOT be the root's path** (`ERR-044-015`): it is FR-DC-009's own
  single-contributor surface, has no production call site, and a root built against the earlier
  wording of this clause would bypass `AvailabilityComposition` entirely, so the F9 back-fill would
  never run for suspensions and ERR-044-003's defect would be reinstated; call
  `OnClubFixturePlayed` once per played fixture per club, **passing the club's roster and the club's
  fielded eleven** (both resolution paths; FR-DC-011, amended ERR-044-003 stage 1 and
  `ERR-044-014`). The roster is what decides membership — #44 does not derive it from the `PlayerId`
  packing — and it MUST be the **unfiltered** squad, the one resolved before the seam above removed
  anybody, since every id whose ban is being served is precisely an id that seam removed. The eleven
  is what lets the call exempt a player who appeared through the extremis back-fill from serving that
  same fixture — **today that eleven is the STARTING eleven only, and the root MUST widen it to
  the eleven that actually took the field the moment a substitution seam gets a production caller
  (`ERR-044-010`)**, or a suspended player fielded as a substitute would not be recognized as
  having played and his ban would decrement for the fixture he took part in, reopening
  ERR-044-003's free-appearance defect one boundary over; route the roster re-key/retirement events
  to the migrate/drop hygiene (T-phase); and compose the sub-blob. It MUST NOT let the UI mutate
  `DisciplineState` directly.
- **#30** — the ERR-030-009 null seam (resolve → *filter* → configure) is the one spec-text
  change, filed at approval; the outer save bump is T1.
- **#37** — no change; **no shared tap** — #44 reads through its own `IDisciplineTickLedgerTap`
  (§4.3), not a type #37 shares, since §4.1 forbids `discipline` to reference the match engine and
  `season-save` does not reference `match-analytics` either (`ERR-044-008`). The engine's own
  per-tick fill is still written once; each read-only consumer defines its own accessor shape over
  it, so the cost of #37 (when built) is a second read of that one fill, not a second fill.
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
| 0.8 | 2026-08-15 | — | **Reviewed-findings pass.** **`ERR-044-008`:** §4.3's "one tap feeds both when built" removed — verified against `src/discipline/IDisciplineTickLedgerTap.cs`, which records the claim as unachievable today (§4.1 forbids `discipline` to reference the match engine; `season-save` does not reference `match-analytics`), not merely deferred; restated as #44 declaring its own tap interface, with the two-reads-not-two-behaviours cost stated explicitly. **`ERR-044-007`:** §4.5 gains a composition-root MUST to call `CardLedgerFold.RequireCommittableConfig()` once per round before the first fixture resolves — enforced in production (`SeasonLoop.PlayNextRound`) and unit-tested, but previously undeclared anywhere in this section. **`ERR-044-010`:** §4.5's fielded-eleven bullet now states that the contract holds today only because `SeasonLoop.FieldedXi` derives the STARTING eleven and no `MatchEngine.SubstitutePlayer` call site exists yet, and MUST widen to the eleven that actually played once one does. See `spec-error-log.md`. |
| 0.9 | 2026-08-15 | — | **Reviewed-findings pass, continuing `ERR-044-008`.** §4.5's `#37` bullet still read "the shared-tap composition is recorded from #44's side here (one tap, two folds)" — the identical refuted claim v0.8 fixed 45 lines up at §4.3, missed because that pass swept for the exact phrase rather than the underlying claim ("one tap feeds both") this bullet restated in different words. Corrected to match §4.3/§7.3/§8.1: no shared tap or adapter type; #44 reads through its own `IDisciplineTickLedgerTap`; the engine's one-per-tick fill is read by independent accessor shapes, so a second consumer costs a second read, not a second fill. No new ERR id — this is `ERR-044-008`'s own back-prop reaching the site its founding fix missed. See `spec-error-log.md` `ERR-044-008`. |
| 0.10 | 2026-08-16 | — | **Two adversarial-review findings, both §4.5.** **`ERR-044-015`** (H3): the composition-root clause read "run `FilterAvailable` at the resolve→configure seam", naming the one method four other places in this spec say the root must not call (FR-DC-009 in `section-2.md`, §2.2's `Availability` block comment, §3.3's pseudocode, §6.2) and which has zero production call sites. A root built against it verbatim would filter with #44 alone and never reach `AvailabilityComposition`, so #30 §2.3 F9's back-fill would not run for suspensions — reinstating ERR-044-003's defect. Rewritten to the landed contract: removals gathered through `Availability.MarkSuspended` into the composed seam, which owns the intersection and the back-fill, with an explicit MUST NOT on `FilterAvailable`. The FR-DC-010 / ERR-044-002 both-clubs-both-paths half of the sentence is unchanged — it was correct. **`ERR-044-014`** (H1): the same bullet's `OnClubFixturePlayed` contract now requires the club's roster beside its fielded eleven, records that membership is READ from the roster rather than derived from `PlayerId / CLUB_SQUAD_SIZE`, and requires the UNFILTERED squad — every id being served is one the seam above just removed. See `spec-error-log.md` `ERR-044-014`, `ERR-044-015`. |
| 0.11 | 2026-08-16, later still | — | **`ERR-044-020`** (M3), section-4 sync — the half of the fix the reviewed-findings pass landed into `section-2.md`/`section-3.md` and recorded open against this file, out of that pass's owned file set. §4.3 gains a paragraph declaring `IDisciplineTickLedgerTap`'s `CurrentTick` member (`MatchEngineDisciplineTap` forwards `MatchEngine.CurrentTick`) and describing `CardLedgerFold.ObserveTick`'s consecutive-tick refusal + partial-application poison latch, matching the wording already synced into §2.2/§3.1 and mirroring #37 `MatchAnalyticsAggregator`'s F6. §4.5's "feed the tap every tick (lossless)" root-contract bullet now states that losslessness is enforced by the tap contract itself, not merely upheld by the root's own discipline. No FR row changes; no behaviour change — a description sync of an already-landed code contract. See `spec-error-log.md` `ERR-044-020`. |
#endregion
