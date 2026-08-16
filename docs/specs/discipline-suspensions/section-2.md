# Discipline & Suspensions #44 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 24, 2026
**Last Updated:** August 16, 2026, latest of all (v0.15 — reviewed-findings pass, finding M18: §2.3's F1
row is extended to the seed/substitution boundary refusals landed with `ERR-044-021`/`ERR-044-022` (a
non-one-to-one construction seed, a self-substitution, an on-pitch `Incoming`, a bench `Outgoing`, and
an out-of-range `onPitchAgentIdCount`), none of which the table named despite being enforced in
production and unit-tested; a new **F7** row declares the lossless-pump refusals `ERR-044-020` added
(the non-consecutive-tick refusal and the partial-application poison latch), which F1..F6 had no room
for. `section-5.md` v0.8 carries the matching test-method citations.)
**Last Updated (prior):** August 16, 2026, latest (v0.14 — reviewed findings pass, finding A. `CardLedgerFold`'s
§2.2 constructor line gains a required `onPitchAgentIdCount` parameter (**`ERR-044-022`**): the
constructor now takes `int[] occupancyByAgentId, int onPitchAgentIdCount, int competitionId`, matching
`CardLedgerFold.cs` v1.10. The boundary marks where on-pitch agent ids end and the engine's synthetic
bench ids begin (`MatchEngineConstants.SQUAD_SIZE` in production) — `ApplySubstitution` (§3.1) uses it
to refuse a `SubstitutionEvent` whose `Incoming` names an on-pitch id or whose `Outgoing` names a bench
id, closing the gap the M1 seed-injectivity check (v0.12) could not see on its own: that check runs
once, over player ids, and never learns which agent ids are on-pitch versus bench. Without it,
`Sub(Outgoing=5, Incoming=6)` with agent id 6 an occupied ON-PITCH slot silently destroyed slot 5's
prior occupant's mapping and misattributed his cards — the Appendix C "slot 19" family (`ERR-044-001`)
one layer deeper than the seed check alone could reach.)
**Last Updated (prior):** August 16, 2026, later still (v0.13 — **`ERR-030-045`** (an adversarially-reviewed
High continuing `ERR-030-044`'s, filed at #30 which owns the rule; back-propagated here). §2.3's
`ERR-044-019` note stated the extremis compromise in a two-case form whose second case — "forced to
start" — read as if only positional forcing could reach it (its parenthetical named the club's only
goalkeeper as *the* case). That is narrower than the truth: a club short by **more than one** player
gets no useful probe on any reinstatement but the last, because fieldability is monotone in adding
players, so #30's amended within-tier key decides those picks blind and can only make them *well*. If
every completing choice starts a suspended player, one starts. #30's key is therefore a **best-effort
minimisation** of the forced-start case, not a guarantee against it — a distinction #44 has to state,
because a mass-suspension club is the population its own subject creates. The goalkeeper parenthetical
is moved into the new note and the bullet reworded to "no candidate choice keeps every
reinstated-suspended player out of the eleven", which is the condition that actually holds. No FR row
changed; FR-DC-011 is untouched and correct in every case. §7.2's mirror amended in the same commit
(`section-7.md` v0.9); #30 `section-3.md` v2.8; code `src/season-save/AvailabilityComposition.cs` v1.6,
`src/match-engine/SquadRating.cs` v1.5.)
**Last Updated (prior):** August 16, 2026, yet later (v0.12 — final fixer pass over the reviewed-findings
round: **`ERR-044-018`** (M8) — §2.2's `DisciplineState` block was a bare `{ /* map ... */ }` comment
while the landed type exposes `Count`, `EntryAt(int)`, `EntryFor(int,int)`, `HasEntry(int,int)` and the
restore-door `FromEntries(DisciplineEntry[])`, whose strictly-ascending + no-all-zero-rows refusals are
what FR-DC-015/FR-DC-017 rest on at the file boundary (`DisciplineSaveCodec.Decode` returns through
it) — now declared, cross-referenced to F3/FR-DC-017, and noting `EntryFor`'s current negative-key
posture (returns the zero row, not a throw — `DisciplineState.cs` v1.1, L1). **`ERR-044-020`** (M3) —
§2.2's `CardLedgerFold` block referenced `IDisciplineTickLedgerTap` without ever declaring it; the
interface is now declared, with `CurrentTick` (the member `CardLedgerFold.ObserveTick` uses to enforce
a lossless, in-order pump) and a note on `ObserveTick`'s own declaration describing the consecutive-tick
refusal and the partial-application poison latch as normative — not a contradiction of existing text,
which was simply silent on enforcement. **M7** — the four `[GT]` threshold/ban constants renamed
ALL_CAPS → PascalCase (`YellowAccumulationThreshold`/`AccumBanMatches`/`SecondYellowBanMatches`/
`StraightRedBanMatches`, FR-DC-006/007) to match the code and `src/CLAUDE.md` §3.2.3's PascalCase rule
for `[GT]` constants — filed as **`ERR-044-017`**. **L6** — two `section-2.md`-owned version rows
(v0.8, v0.9) cited `DisciplineRules.cs`/`CardLedgerFold.cs` by line number; both drifted from the code
they cited and are replaced in place with member names, annotated rather than silently rewritten.)
**Last Updated (prior):** August 16, 2026, later (v0.11 — **`ERR-044-019`**, adversarial-review H2, cross-filed
at #30 as `ERR-030-044` which owns the rule: §2.3's ERR-044-003 note asserted that "a suspended player
plays only when the alternative is a club that cannot take the field at all". That was FALSE of the
implementation — #30 §3.4's probe is the FULL selection walk (eleven starters PLUS the seven-slot
bench), so the extremis tier fires on **bench depth** on a club that could field a legal XI, and the
pre-fix within-tier key (earliest roster position) let the rating-greedy selector START the reinstated
man. Corrected to the **two-case** form the amended key produces: benched ⇒ not in the fielded eleven ⇒
FR-DC-011's decrement is NOT exempted and the ban advances normally; forced to start (no candidate
choice avoids the XI — the sole-goalkeeper case) ⇒ exempt, and only then does the ban stall. §7.2's
mirror of the same claim corrected in the same commit (`section-7.md` v0.8). No FR row changes: the
defect was #30's ordering key and #44's description of its consequence, not #44's own requirements.)
**Last Updated (prior):** August 16, 2026 (v0.10 — `ERR-044-014`, adversarial-review H1: club membership is
READ FROM THE ROSTER, not derived from #27's id packing. FR-DC-011 now requires `OnClubFixturePlayed`
to take the club's roster as well as its fielded eleven and to decide membership by presence in it;
§2.2's signature gains `int[] clubPlayerIds`; §2.3 **F2** gains the matching null refusal and records
that the negative-`PlayerId`-divides-to-club-0 hazard is no longer reachable through this method,
whose membership test no longer divides. The derivation was a second notion of membership beside
`MarkSuspended`'s roster walk, resting on a migration rule (FR-DC-013) that has no production caller;
on the first disagreement — a #31 transfer, or §7.2's required id-space widening — a banned player is
removed from every squad he is really in while his ban never decrements)
**Last Updated (prior):** August 15, 2026, later still again (v0.9 — reviewed-findings pass. `ERR-044-007`
(M2): FR-DC-009's `FilterAvailable(in Squad) → Squad` requirement corrected to the landed three-
parameter signature (`Squad squad, DisciplineState state, int competitionId`) — verified against
`src/discipline/Availability.cs`, which has never taken `in Squad` alone. `ERR-044-013` (M5, new id):
§2.2's `CardLedgerFold` block gains its `NO_PLAYER` sentinel — caller-facing (the constructor throws
on any other negative value) and used normatively by Appendix C ("every other bench id `NO_PLAYER`")
with no §2.2 declaration and no Appendix A row, the same omission class §2.2 was extended for on
`DisciplineRules.State`/`CardLedgerFold.PendingCardCount`/`RequireCommittableConfig()` at v0.8)
**Last Updated (prior):** August 15, 2026 (v0.7 — ERR-044-003 stage 1, owner decision: FR-DC-011 amended so a
ban no longer decrements on a fixture the player appeared in through #30 §2.3 F9's extremis back-fill —
`OnClubFixturePlayed` now takes the club's fielded eleven and exempts anyone in it; the §2.3
"recorded, not fixed" note updated to record the free-appearance half as FIXED (reinstatement tier
order unchanged) and to point at §7.2's staged youth/generated-cover plan, in place of the deferral
queue, which was NOT chosen)
**Last Updated (prior):** August 15, 2026, later still (v0.8 — reviewed-findings pass over the #44 C1/C2
landing. `ERR-044-007`: §2.2's `DisciplineRules.OnClubFixturePlayed` signature still showed the
pre-stage-1 `(int clubId)` form — a signature the code has not had since v0.7's own change — and the
block was missing three landed members (`DisciplineRules.State`, `CardLedgerFold.PendingCardCount`,
`CardLedgerFold.RequireCommittableConfig()`); F2 extended to cover the null-`fieldedPlayerIds`
refusal §3.3's pseudocode already attributed to it but this table never stated. `ERR-044-010`:
FR-DC-011 gains a note that "the fielded eleven" means who actually played, not who started, and that
today's `SeasonLoop.FieldedXi` supplies the latter and is correct only in the absence of a
`SubstitutePlayer` call site)
**Last Updated (prior):** August 13, 2026, later still (v0.6 — L12(c) + L13, a third adversarial-review pass
over the #44 C1/C2 landing: §2.2's API block corrected to the real landed signatures — no
`MarkSuspended`, `DisciplineRules` or `DisciplineEntry` shown at all, and `FilterAvailable`/`IsAvailable`
carried signatures the code does not have (no `competitionId` on the filter, `in DisciplineState` on a
class) — now the actual `DisciplineState`/`DisciplineEntry`/`CardLedgerFold`/`Availability`/
`DisciplineRules` surface; §2.3 gains **F6**, the two `[GT]` fail-loud guards
(`RequireYellowThreshold`/`RequireBanLength`), which had no normative source at all)
**Last Updated (prior):** August 13, 2026, later same day (v0.5 — ERR-044-004 + ERR-044-005, back-props owed
by the #44 C1/C2 adversarial review: F2's fail-loud stated explicitly for a negative/unresolvable
`PlayerId`, not just a negative `clubId`; FR-DC-009 gains the all-suspended-squad `null`-return case)
**Last Updated (prior):** August 13, 2026 (v0.4 — ERR-044-002 + ERR-044-003, C1/C2 landing back-prop: FR-DC-010
re-scoped off "the engine-resolved fixture" to every resolved squad on both resolution paths; F5's
fail-loud withdrawn in favour of #30 §2.3 F9, with the suspension-as-stricter-reinstatement-tier
decision recorded)
**Last Updated (prior):** July 24, 2026 (v0.3 — cross-set AR pass 3; prior v0.2 PASS-1, v0.1 initial)
**Version:** 0.15
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
| FR-DC-006 | The de-dup rule IS the verified emission contract: kind 0 ⇒ `Yellows += 1`; kind 2 (SecondYellow — a **single** event) ⇒ `Yellows += 1` AND a `SecondYellowBanMatches` ban; kind 1 ⇒ a `StraightRedBanMatches` ban (no yellow). #44 MUST NOT expect or synthesize a separate red event after a kind-2. | MUST | KD-5 |
| FR-DC-007 | When `Yellows ≥ YellowAccumulationThreshold`, an `AccumBanMatches` ban MUST be added and `Yellows` MUST be reduced by the threshold (residual kept); bans from any source MUST **stack additively** on `BanMatchesRemaining`. | MUST | §3.2 |
| FR-DC-008 | A player MUST be unavailable while `BanMatchesRemaining > 0`; `IsAvailable` MUST be a pure predicate over `DisciplineState`. | MUST | KD-4 |
| FR-DC-009 | `FilterAvailable(Squad squad, DisciplineState state, int competitionId) → Squad` MUST return a **reduced value copy** (available players only) for `ConfigureSquads`; it MUST NOT write #27 state; with no active ban it MUST pass the squad through unchanged. Three parameters over the tally and its competition partition — never `in Squad` alone, which has never been the signature (`ERR-044-007`, verified against `src/discipline/Availability.cs`). **When every player is suspended there is no reduced value copy to return — `Squad` cannot represent a zero-player roster — so `FilterAvailable` MUST return `null` for that case** (ERR-044-005; `Squad`'s own constructor refuses `players.Length == 0`, so returning it as a normal squad is not an option). This method is FR-DC-009's own surface; #44's production path is `MarkSuspended`'s removal mask, consumed directly by #30's composed availability seam. | MUST | KD-4 |
| FR-DC-010 | The filter MUST act at #30's pre-declared **resolve→configure** seam (ERR-030-009) and MUST apply to **every resolved squad of every fixture on both resolution paths** (the engine boot and the quick-sim rating alike) — the managed club's **and its opponent's**, whichever path resolved them (both pass through `ResolveByClubId` → `ConfigureSquads`, so both pass the seam; a banned opponent is excluded exactly as a banned managed-club player is). **Card *generation* stays engine-fixture-only at minimal (§3.3) — this row governs the filter, not the fold.** The fold MUST complete at fixture resolution — so a card in fixture N bans for fixture N+1 (no off-by-one). *(Re-scoped from "the engine-resolved fixture" — ERR-044-002, August 13, 2026: the narrower wording contradicted FR-DC-011's "regardless of resolution path" one row below and #30 §3.4's LIVE both-paths seam; a quick-sim-only implementation would have let a banned player's club decrement his ban on a fixture he had just played through.)* | MUST | KD-3 |
| FR-DC-011 | A ban MUST decrement by exactly one per **played fixture of the player's club that the player did not appear in**, regardless of resolution path (engine-resolved or quick-sim); serving MUST be reported via `OnClubFixturePlayed`, which MUST take the club's **roster** and the club's fielded eleven as inputs. **"The player's club" MUST be read from that roster, never derived from the `PlayerId` packing (`ERR-044-014`)** — one notion of club membership, the same one FR-DC-009/FR-DC-010's removal walk uses; a derivation is a second notion that agrees only while #27's packing holds, and the first disagreement (a #31 transfer, the §7.2 id-space widening) leaves a banned player removed from the squad he is really in with his ban never decrementing — suspended forever, silently. *(Amended — ERR-044-003 stage 1, August 15, 2026: the original row read "per played fixture of the player's club" full stop, which is correct only while a banned player can never take the field. #30 §2.3 F9's depleted-squad back-fill can field him in extremis — see the ERR-044-003 note below — and under the original wording that appearance ALSO served his ban, so it cost him nothing and a two-match red cost a depleted club nothing at all. A suspension means the club plays **without** him; a fixture he plays in is not one of it.)* **The "fielded eleven" this row requires is the eleven that actually took part, not merely the eleven that started (`ERR-044-010`).** `SeasonLoop.FieldedXi` supplies the STARTING eleven today, which satisfies this row only because #44 is scrupulously substitution-correct for card attribution while no `MatchEngine.SubstitutePlayer` call site exists on the season path (Stage 0 fields a fixed XI); §3.3's pseudocode records the dependency and what breaks once one exists. | MUST | KD-3 |
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
// ascending order — every read is a binary search over it, the invariant only Upsert and
// FromEntries below can establish; CompetitionId = 0 at minimal (FR-DC-012). All integer; NO RNG
// state (FR-DC-016/019). Mutation (Upsert/Remove) is internal — DisciplineRules is the sole public
// writer (§2.2, ERR-044-018).
public sealed class DisciplineState
{
    public DisciplineState();                              // genesis — empty (FR-DC-017)
    public int  Count { get; }
    public DisciplineEntry EntryAt(int index);              // canonical-order row access; F2-class
                                                             // range guard outside [0, Count)
    public DisciplineEntry EntryFor(int playerId, int competitionId);   // pure; absent OR a negative
                                                             // playerId both return the clean zero
                                                             // row (FR-DC-008) — never a throw
    public bool HasEntry(int playerId, int competitionId);
    // The restore door (FR-DC-015/F3's caller): requires rows STRICTLY ascending and none empty,
    // because every read above is a binary search — an unordered or all-zero-row block would make a
    // lookup silently miss a player who IS carried (the PlayerCareerStates.FromBlocks H1 class).
    public static DisciplineState FromEntries(DisciplineEntry[] entries);
}

// One tally row (F2's PlayerId >= 0 invariant enforced at construction).
public readonly struct DisciplineEntry
{
    public readonly int PlayerId;
    public readonly int CompetitionId;
    public readonly int Yellows;
    public readonly int BanMatchesRemaining;
}

// KD-2 — the #37-class per-tick read-only tap #44 reads through (its OWN interface, not a shared
// one — §4.1/§4.3). CurrentTick (ERR-044-020) is what CardLedgerFold.ObserveTick uses to enforce a
// lossless, in-order pump — the same role #37's MatchAnalyticsObservation.CurrentTick plays there.
public interface IDisciplineTickLedgerTap
{
    ulong CurrentTick { get; }
    int    RecordCount { get; }
    byte   OrdinalAt(int index);
    T      RecordAt<T>(int index) where T : struct;
}

// KD-2 — the read-only fold, fed by the #37-class per-tick tap during an engine-resolved fixture.
// Buffers the fixture's cards and commits them ONCE, at resolution (§3.1, FR-DC-010).
public sealed class CardLedgerFold
{
    // [FIXED] Occupancy sentinel: this agent id maps to no player (an unused seed slot). Appendix
    // A/C use the name normatively — a caller-facing, load-bearing value (ERR-044-013).
    public const int NO_PLAYER = -1;
    // onPitchAgentIdCount (ERR-044-022) marks where on-pitch agent ids end and the engine's synthetic
    // bench ids begin — MatchEngineConstants.SQUAD_SIZE in production. ApplySubstitution (§3.1) uses
    // it to refuse an Incoming that names an on-pitch id or an Outgoing that names a bench id, a
    // distinction the seed's own one-to-one check cannot make on its own. Also ERR-044-023: the seed
    // itself must be a snapshot taken AT BOOT, before any substitution — MatchEngine.PlayerIdsByAgentId
    // is one-to-one over its non-sentinel entries only at that moment (§4.3).
    public CardLedgerFold(int[] occupancyByAgentId, int onPitchAgentIdCount, int competitionId);
    public int  PendingCardCount { get; }        // cards folded so far this fixture; 0 for most
    // ERR-044-020: refuses a NON-CONSECUTIVE tap.CurrentTick (InvalidOperationException, naming both
    // the offending and the last-observed tick), except on the very first call to a fresh fold, which
    // anchors on whatever tick it is first given (a fixture need not begin at tick 0). Also latches a
    // "_faulted" state on any partial-tick failure — a later record in the SAME tick throwing (e.g. an
    // F1/F4 refusal) — and refuses every subsequent ObserveTick call thereafter, even an otherwise-
    // consecutive one, naming the fault. Commit is unaffected by the latch: it still applies whatever
    // was buffered before the failure (§3.1's atomicity is unchanged).
    public void ObserveTick(IDisciplineTickLedgerTap tap);
    public int  Commit(DisciplineRules rules);   // fallible under a bound [GT] — F6
    // The round-level pre-check (M8/§4.5): validates the same four bound [GT]s Commit would throw
    // on, WITHOUT a fold, so a caller can ask once, before the first fixture of a round is touched,
    // rather than discover a bad config only when a per-fixture Commit strands the round (§4.5).
    public static void RequireCommittableConfig();
}

// KD-4 — the availability view (pure; never mutates #27 state).
public static class Availability
{
    public static bool  IsAvailable(DisciplineState state, int playerId, int competitionId);
    // #30's composed-seam contribution (§3.3) — OWNS removed (M14). recoveryRemaining is
    // PlayerCareerStates.MarkUnavailable's own out parameter (#41's side of the seam), not this
    // method's — L16 corrected this comment, which had wrongly attributed it here.
    public static int   MarkSuspended(Squad squad, DisciplineState state, int competitionId, bool[] removed);
    // reduced VALUE COPY; FR-DC-009's OWN surface, not #44's production path — see FR-DC-009.
    public static Squad FilterAvailable(Squad squad, DisciplineState state, int competitionId);
}

// §3.2/§3.3/§3.4 — the sole mutating entry point onto DisciplineState.
public sealed class DisciplineRules
{
    public DisciplineRules(DisciplineState state);
    public DisciplineState State { get; }                                   // the state this instance writes
    public void ApplyCard(int playerId, int competitionId, byte cardKind);   // FR-DC-006 — F4/F6
    public void AddYellow(int playerId, int competitionId);                  // F6
    public void AddBan(int playerId, int competitionId, int matches);
    // KD-3 — every competition of that club. clubPlayerIds is the club's ROSTER and is REQUIRED
    // (never null, F2): membership is decided by presence in it, never derived from #27's id
    // packing (ERR-044-014), so there is one notion of "at this club" shared with MarkSuspended
    // above. It must be the UNFILTERED roster — every id being served is one the filter removed.
    // clubId is identity + the F2 gate only and takes part in no matching. fieldedPlayerIds is
    // REQUIRED on the same terms and exempts anyone in it from the decrement — ERR-044-003 stage 1:
    // a ban is served by the club playing WITHOUT the banned player, and #30 §2.3 F9's extremis
    // back-fill can put him on the pitch, so "the club played" is not by itself "his ban was served".
    public void OnClubFixturePlayed(int clubId, int[] clubPlayerIds, int[] fieldedPlayerIds);
    public void RollToNextSeason();                                         // FR-DC-017
    public void MigratePlayerId(int oldPlayerId, int newPlayerId);          // FR-DC-013 — F2
    public void DropPlayer(int playerId);                                   // FR-DC-013
}
```

## 2.3 Failure modes

| ID | Condition | Handling |
|---|---|---|
| **F1** | A fold record referencing an agent slot with no occupancy mapping (a card/sub for an unmapped id); a construction-time occupancy seed mapping two agent ids to the same non-empty player, i.e. not one-to-one (`ERR-044-021`); a `SubstitutionEvent` with `Outgoing == Incoming` (self-substitution, `ERR-044-021`); an `Incoming` naming an on-pitch agent id or an `Outgoing` naming a bench agent id, i.e. crossing the `onPitchAgentIdCount` boundary the wrong way (`ERR-044-022`); and an `onPitchAgentIdCount` constructor argument outside `(0, len(lineup)]` (`ERR-044-022`) | **Fail loud** in every case — the lineup seed is incomplete or ambiguous, or a substitution record names a boundary the seed cannot represent; silent misattribution is the trap. |
| **F2** | `OnClubFixturePlayed`/`FilterAvailable` naming a club/player outside the resolvable universe; a migration for an unknown source entry. **The player half is explicit: a negative or otherwise unresolvable `PlayerId` MUST be refused, not just a negative `clubId`** — C# integer division truncates toward zero, so every id in `[-CLUB_SQUAD_SIZE + 1, -1]` would otherwise derive to club 0 in `OnClubFixturePlayed` and be served, decremented and migrated as one of its players, silently (ERR-044-004). Refused at BOTH boundaries: `DisciplineEntry`'s constructor and `DisciplineSaveCodec.Decode` (F3). **`OnClubFixturePlayed`'s `fieldedPlayerIds` MUST also be refused when null (ERR-044-007)** — this is the same caller-contract posture, not a different one: a caller that cannot name who played cannot know whose ban was served, so the ignorance is refused rather than silently read as "serve everybody" (which would restore the free-appearance defect ERR-044-003 stage 1 exists to close). **Its `clubPlayerIds` MUST be refused when null on the identical grounds (`ERR-044-014`)**: since club membership is read from that roster rather than derived, a caller who cannot name it cannot name whose ban this fixture served, and both silent readings of the unknown case are wrong — "serve everybody" is the removed derivation's failure and "serve nobody" is a permanently suspended squad. *(The negative-`PlayerId` hazard described above is no longer reachable through `OnClubFixturePlayed`'s membership test, which does no division; the refusals at `DisciplineEntry`'s constructor and `DisciplineSaveCodec.Decode` stand unchanged — a negative id is still an unresolvable identity, and `MigratePlayerId` still refuses one.)* | **Fail loud** — identity validity is a caller-contract bug (the #31 F6 class); an unknown "who played" is the same bug one level up. |
| **F3** | Discipline sub-blob: bad version / out-of-bounds length / trailing bytes / non-ascending keys / negative values | **Fail loud** — the `SeasonSaveCodec` posture (FR-DC-015). |
| **F4** | A `CardKind` outside `{0, 1, 2}` on the tap | **Fail loud** — an unknown card kind is an engine-contract change #44 must not guess about (contrast F5-class unknown *ordinals*, which are ignored — a known event with an unknown *payload value* is different). |
| **F5** | *(WITHDRAWN as a fail-loud, ERR-044-003, August 13, 2026 — see the note below the table.)* `FilterAvailable` reducing a squad below the engine's minimum viable size (fewer than the 18 `ConfigureSquads` consumes) | #44 contributes **removals only**; the composed seam's viability rule is **#30 §2.3 F9** (Season & Competition Loop, approved after this row was written). #44's `FilterAvailable`/`MarkSuspended` implement no viability gate at all — see below. |
| **F6** | A bound config setting `YellowAccumulationThreshold < 1` (the residual subtraction in §3.2 can never terminate a crossing, so every single yellow would ban) or a negative `AccumBanMatches`/`SecondYellowBanMatches`/`StraightRedBanMatches` | **Fail loud** — guarded at the site that would otherwise WRITE the breach (`DisciplineRules.AddYellow`/`ApplyCard`, `CardLedgerFold.Commit`), not the constant catalogue: the catalogue's own lock runs config-unbound and sees the fallback forever (the ERR-041-003 class), so a shipped config could otherwise silently ban a player on his first yellow, or leave a mid-fixture `Commit` half-applied. |
| **F7** | A non-consecutive `tap.CurrentTick` on `ObserveTick` — a skipped or repeated tick, except the very first call on a fresh fold, which anchors on whatever tick it is first given (a fixture need not begin at tick 0); or any `ObserveTick` call after a prior call's partial-tick failure has latched the fold `faulted`, even one that is otherwise perfectly consecutive (`ERR-044-020`) | **Fail loud** — the tap read is contracted as a lossless, in-order pump (§3.1); a gap or a replay silently drops or double-counts cards, and a part-way failure leaves the buffer's completeness for that tick unknown. |

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

**Recorded, partially fixed — an owner decision, staged.** Preserving #30 §3.4's stated invariant
("the composed filter can never leave a club worse off than having no filter at all") means a
suspended player **is** reinstatable in extremis, which the Laws of the Game do not allow. The
implementation makes suspension a **stricter reinstatement tier** than injury — every injured player
is pressed back before any suspended one. **That tier order is unchanged.**

> **`ERR-044-019` — this paragraph used to say "and a suspended player plays only when the alternative
> is a club that cannot take the field at all", and that was FALSE of the implementation** (August 16,
> 2026; cross-filed at #30 as `ERR-030-044`, which owns the rule). The trigger for the back-fill is
> #30 §3.4's probe, `SquadRating.CanFieldStartingEleven`, which is the FULL selection walk — eleven
> position-matched starters PLUS the seven-slot bench — so the extremis tier fires for **bench depth**
> too, on a club that could field a perfectly legal XI. Reinstating by earliest roster position (the
> pre-fix within-tier key) then put a banned player into the pool the rating-greedy selector draws the
> starting eleven from, and it **started him**: this sentence's "only when the alternative is a club
> that cannot take the field" was true of neither the trigger nor the outcome. It is true only under
> `ERR-030-044`'s amended key, and even then in a **two-case** form that this spec must state rather
> than collapse:
>
> - **Benched — the common case, and the one the amended key exists to reach.** Tier 2 now prefers the
>   first candidate, in roster order, that the selector would **bench**. He is then not in
>   `fieldedPlayerIds`, FR-DC-011's decrement is not exempted, and **his ban advances normally** — the
>   suspension costs exactly what the Laws say it costs, even though the club was depleted enough to
>   need him in its eighteen.
> - **Forced to start — the residual.** When no candidate choice keeps every reinstated-suspended
>   player out of the eleven, he starts, the ERR-044-003 stage-1 exemption fires, and **his ban does not
>   advance for that fixture**. This is the compromise between #30's liveness invariant and the Laws,
>   and it is the ONLY case in which a ban stalls. It is what the two unbuilt tiers below delete.
>
> Neither case is a licence for the other: the stall is a property of being *forced* onto the pitch,
> never of being reinstated.
>
> **`ERR-030-045` (August 16, 2026) amends the second bullet — it was written as if only positional
> forcing could reach it, and that is narrower than the truth.** The forced-start case is reached two
> ways. The first is the canonical one: a **single** reinstatement with no benchable candidate, the
> club's only goalkeeper. The second is a **multi-player shortfall** in which every completing choice
> starts someone — a club short by more than one gets no useful probe on any reinstatement but the last
> (fieldability is monotone in adding players, so nothing is fieldable until the gap closes), and #30's
> pass-3 key can only make that pick *well*, not make it safe. #30 §3.4's key is therefore a **best-effort
> minimisation of this bullet, not a guarantee against it** — it presses the weakest banned players back
> first, so the strong ones the selector would start are reached only when nothing weaker completes the
> squad. Read the two bullets that way: benched *whenever any choice permits it*, forced start when none
> does. A mass-suspension club is the population that meets the second case, and #44 is the spec whose
> own subject makes that club common.

What **ERR-044-003 stage 1**
(August 15, 2026) fixed is the free-appearance half: an extremis appearance no longer serves the ban it
was fielded through — `OnClubFixturePlayed` now takes the club's fielded eleven and exempts anyone in
it (FR-DC-011), so a two-match red still costs a depleted club two full fixtures rather than one. The
fuller answer, agreed but not yet built, is two further tiers staged ahead of the suspended one — youth
call-ups first, then generated low-attribute cover — after which a banned man never reaches the pitch
at all and the suspended tier above becomes unreachable rather than merely costly. Both are blocked:
**#42 Youth has no `src/` assembly**, and generated cover needs the packed `PlayerId = clubId ×
CLUB_SQUAD_SIZE + local` id space widened, since it is fully packed at 25 and a 26th player for club N
collides with club N+1's first (#27 FR-SQ-010 as amended by ERR-027-004). §7.2 records the staged plan
and its blockers in full; the deferral queue previously recorded there as the alternative was **not**
chosen.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §2 (FR-DC-001..022, data structures, F1..F5), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1 (M): FR-DC-017 gains the **immediate `(0,0)`-drop canonical-minimality rule** (an all-zero entry and an absent entry must never both be encodable — a serialized-representation determinism hazard the v0.1 boundary-only phrasing left open). |
| 0.3 | 2026-07-24 | — | Cross-set AR pass 3 (M): FR-DC-010 pins **both-squads filter coverage** — the seam applies to each resolved squad of the engine-resolved fixture (managed club AND opponent); the unscoped v0.2 wording let a managed-squad-only implementation pass every test while banned opponents played through their bans. |
| 0.4 | 2026-08-13 | — | **C1/C2 landing back-prop.** **ERR-044-002:** FR-DC-010's "the engine-resolved fixture" contradicted FR-DC-011's "regardless of resolution path" one row below and #30 §3.4's LIVE both-paths seam; re-scoped to every resolved squad of every fixture on both resolution paths. **ERR-044-003:** F5's fail-loud withdrawn — #30 §2.3 F9 (approved after this spec) settles the same depleted-squad event by back-filling instead, and #44 contributes removals only; recorded that a suspended player is reinstatable in extremis under #30's never-worse-than-unfiltered invariant, making suspension a stricter reinstatement tier than injury rather than an absolute bar. |
| 0.5 | 2026-08-13 | — | **Adversarial-review back-prop.** **ERR-044-004:** F2 stated only "a club/player outside the resolvable universe" and the implementation had guarded the club half alone — a negative `PlayerId` truncation-derives to club 0 and was silently served, decremented and migrated; F2 now names the player half explicitly and cites both refusal sites (`DisciplineEntry`'s constructor, `DisciplineSaveCodec.Decode`/F3). **ERR-044-005:** FR-DC-009's "reduced value copy" requirement was total as written but unsatisfiable for an all-suspended squad (`Squad` cannot represent zero players); FR-DC-009 now states the `null`-return case and names `MarkSuspended`'s mask, consumed by #30's composed seam, as the actual production path — `FilterAvailable` is FR-DC-009's own surface, not #44's. |
| 0.6 | 2026-08-13 | — | **L12(c) + L13**, a third adversarial-review pass. **L12(c):** §2.2's code block — the first place an implementer looks — showed only `DisciplineState`/`CardLedgerFold`/two free-floating `Availability` methods/one free-floating `OnClubFixturePlayed`, none matching the landed signatures (`IsAvailable`/`FilterAvailable` took `in DisciplineState` with a default `competitionId`, neither of which the code has; `MarkSuspended`, `DisciplineRules` and `DisciplineEntry` were absent entirely). Replaced with the real surface. **L13:** the failure-mode table stopped at F4 while `DisciplineRules.RequireYellowThreshold`/`RequireBanLength` are enforced in production and unit-tested (the AR pass 9 #29/#41 F8 precedent for exactly this omission class); new **F6** row added, and the matching guard calls landed in `section-3.md` §3.2's `AddYellow` pseudocode. |
| 0.7 | 2026-08-15 | — | **ERR-044-003 stage 1**, owner decision: FR-DC-011 amended — a ban no longer decrements on a fixture the player appeared in via #30 §2.3 F9's extremis back-fill; `OnClubFixturePlayed` now takes the club's fielded eleven and exempts anyone in it. The §2.3 "recorded, not fixed" paragraph updated to state the free-appearance half is now FIXED (reinstatement tier order unchanged) and to name the staged three-tier plan (exempt-the-appearance now; youth call-ups; generated cover) with its two blockers — #42 Youth has no `src/` assembly, and generated cover needs the packed `PlayerId` id space widened (#27 FR-SQ-010 / ERR-027-004) — replacing the deferral-queue alternative, which was NOT chosen. |
| 0.8 | 2026-08-15 | — | **Reviewed-findings pass.** **`ERR-044-007`:** §2.2's `DisciplineRules` block corrected `OnClubFixturePlayed(int clubId)` → `OnClubFixturePlayed(int clubId, int[] fieldedPlayerIds)` (verified against `src/discipline/DisciplineRules.cs`'s `OnClubFixturePlayed` — the v0.7 signature amendment never reached this code block) and gained the `State` property (`src/discipline/DisciplineRules.cs:49`); `CardLedgerFold` gained `PendingCardCount` and the public static `RequireCommittableConfig()` — the round-level `[GT]` pre-check §3.1's own pseudocode calls and `SeasonLoop.PlayNextRound` enforces in production (`src/season-save/SeasonLoop.cs`), which had no §2.2 declaration at all. F2 (§2.3) extended to state the null-`fieldedPlayerIds` refusal explicitly — `OnClubFixturePlayed`'s null-`fieldedPlayerIds` guard throws `ArgumentNullException` there and §3.3's pseudocode already read `REQUIRE fieldedPlayerIds is not null  # F2`, but this table's F2 row described only the club/player-identity case. **`ERR-044-010`:** FR-DC-011 gains a note that the required "fielded eleven" is the eleven that played, not merely started, and that today's `SeasonLoop.FieldedXi` (the STARTING eleven) satisfies the row only because no `SubstitutePlayer` call site exists on the season path. See `spec-error-log.md` `ERR-044-007`, `ERR-044-010`. *(L6, August 16, 2026: this row's `DisciplineRules.cs:245`/`CardLedgerFold.cs:127`/`CardLedgerFold.cs:276`/`DisciplineRules.cs:254-261` line citations were verified-against-wrong-lines — replaced with member names above, since exact lines drift across later edits and a line number is not a stable citation.)* |
| 0.9 | 2026-08-15 | — | **Reviewed-findings pass.** **`ERR-044-007`:** FR-DC-009's `FilterAvailable(in Squad) → Squad` requirement corrected to the landed signature — `FilterAvailable(Squad squad, DisciplineState state, int competitionId)`, three parameters, no `in` — verified against `src/discipline/Availability.cs`; the old form misstated the method as a pure predicate over `Squad` alone, omitting the tally and competition partition it actually reads. **`ERR-044-013`** (new id): §2.2's `CardLedgerFold` block gains `NO_PLAYER` (verified against `src/discipline/CardLedgerFold.cs`'s `NO_PLAYER`, `[FIXED]`, value `-1`) — caller-facing (the constructor throws on any other negative occupancy value, F1's "any gap" language depends on it) and used normatively by Appendix C, with no §2.2 declaration and no Appendix A row until now; Appendix A gains the matching row. See `spec-error-log.md` `ERR-044-007`, `ERR-044-013`. *(L6, August 16, 2026: this row's `CardLedgerFold.cs:66` line citation was verified-against-wrong-line — replaced with the member name above.)* |
| 0.10 | 2026-08-16 | — | **`ERR-044-014`** (adversarial review, H1). FR-DC-011 amended: `OnClubFixturePlayed` MUST take the club's ROSTER alongside its fielded eleven, and "the player's club" MUST be read from that roster rather than derived from `PlayerId / CLUB_SQUAD_SIZE`. §2.2's signature becomes `OnClubFixturePlayed(int clubId, int[] clubPlayerIds, int[] fieldedPlayerIds)` (verified against `src/discipline/DisciplineRules.cs` v1.7), with `clubId` documented as identity + the F2 gate only and the roster documented as necessarily the UNFILTERED one — every id being served is one the filter has just removed. §2.3 **F2** extended with the null-`clubPlayerIds` refusal on the ERR-044-007 posture, and annotated: the negative-id-divides-to-club-0 hazard ERR-044-004 filed is no longer reachable through this method, which no longer divides, while the `DisciplineEntry`/`Decode` refusals stand. The retired derivation was a SECOND notion of club membership beside `Availability.MarkSuspended`'s roster walk, and the migration rule cited as keeping them in step (FR-DC-013) has no production caller. See `spec-error-log.md` `ERR-044-014`. |
| 0.11 | 2026-08-16 | — | **`ERR-044-019`** (adversarial review, H2; the rule itself is #30's and is amended at `ERR-030-044`). §2.3's ERR-044-003 note stated the extremis compromise as ONE case — "a suspended player plays only when the alternative is a club that cannot take the field at all" — and that was false of the implementation on both halves. The TRIGGER is #30 §3.4's probe `SquadRating.CanFieldStartingEleven`, which is `LineupSelector`'s full selection walk (eleven position-matched starters PLUS the seven-slot bench), so the tier fires on **bench depth** at a club that can field a perfectly legal XI; and the pre-fix within-tier ORDERING (earliest roster position) then put the reinstated man into the pool the rating-greedy selector draws the starting eleven from, which started him — after which ERR-044-003 stage 1's exemption stalled his ban for as long as the club stayed depleted. Corrected to the two-case form #30's amended key produces: **benched** (the common case, and what the amended key prefers) ⇒ not in `fieldedPlayerIds` ⇒ FR-DC-011's decrement is NOT exempted ⇒ the ban advances normally; **forced to start** (no candidate choice keeps a reinstated-suspended player out of the XI — the sole-goalkeeper case) ⇒ exempt ⇒ and only then does the ban stall, which is the residual §7.2's unbuilt tiers delete. No FR row changed: FR-DC-011 already says "did not appear in", which is exactly right in both cases — what was wrong was this section's account of when the appearance happens. §7.2's mirror corrected in the same commit (`section-7.md` v0.8); code at `src/season-save/AvailabilityComposition.cs` v1.5. |
| 0.12 | 2026-08-16, yet later | — | **Final fixer pass, four findings.** **`ERR-044-018`** (M8): §2.2's `DisciplineState` block declared — `Count`, `EntryAt(int)`, `EntryFor(int,int)`, `HasEntry(int,int)`, `FromEntries(DisciplineEntry[])` — replacing a bare `{ /* map ... */ }` comment; cross-referenced to F3 (§2.3) and FR-DC-017 for `FromEntries`' refusals, and notes `EntryFor`'s negative-key posture (the zero row, not a throw — `DisciplineState.cs` v1.1). **`ERR-044-020`** (M3): §2.2 gains the `IDisciplineTickLedgerTap` interface declaration (previously referenced, never declared), with `CurrentTick`, and `CardLedgerFold.ObserveTick`'s declaration gains a note on the consecutive-tick refusal and the partial-application poison latch — spec-side sync of a code addition the spec text had been silent on, not a contradiction. **M7** (`ERR-044-017`): FR-DC-006/FR-DC-007's four `[GT]` constant names renamed ALL_CAPS → PascalCase (`YellowAccumulationThreshold`/`AccumBanMatches`/`SecondYellowBanMatches`/`StraightRedBanMatches`) to match `DisciplineConstants.cs` and `src/CLAUDE.md` §3.2.3. **L6**: v0.8/v0.9's `DisciplineRules.cs`/`CardLedgerFold.cs` line-number citations replaced with member names in place, annotated. See `spec-error-log.md` `ERR-044-017`, `ERR-044-018`, `ERR-044-020`. |
| 0.13 | 2026-08-16, later still | — | **`ERR-030-045`** (an adversarially-reviewed High continuing `ERR-030-044`'s; filed at #30 `section-3.md` v2.8, which owns the rule; back-propagated here). §2.3's v0.11 note stated the extremis compromise as two cases and pinned the second — "forced to start" — to positional forcing, its parenthetical naming the club's only goalkeeper as *the* case. That is narrower than what the implementation can produce. A club short by **more than one** player gets no usable probe on any reinstatement but the last, because fieldability is monotone in adding players — nothing is fieldable until the gap closes — so #30's within-tier key decides those picks blind. Its amended pass-3 key (weakest banned player first, by the selector's own rating) makes them *well*, and that is all it can do: if every completing choice starts a suspended player, one starts. So #30's key is a **best-effort minimisation** of the forced-start case, not a guarantee against it, and #44 must say so, because a mass-suspension club is precisely the population its own subject creates. The goalkeeper parenthetical moves into a new `ERR-030-045` note and the bullet is reworded to the condition that actually holds ("no candidate choice keeps every reinstated-suspended player out of the eleven"). No FR row changed — FR-DC-011 says "did not appear in", which is right in every case; what was wrong, again, was this section's account of when the appearance happens. §7.2's mirror amended in the same commit (`section-7.md` v0.9); code `src/season-save/AvailabilityComposition.cs` v1.6, `src/match-engine/SquadRating.cs` v1.5. |
| 0.14 | 2026-08-16, latest | — | **Reviewed findings pass, finding A (`ERR-044-022`).** §2.2's `CardLedgerFold` constructor line gains a required `onPitchAgentIdCount` parameter (`int[] occupancyByAgentId, int onPitchAgentIdCount, int competitionId`), with an inline comment stating the boundary it marks (on-pitch agent ids end, the engine's synthetic bench ids begin — `MatchEngineConstants.SQUAD_SIZE` in production), what it is FOR (`ApplySubstitution`, §3.1, refusing an on-pitch `Incoming` or a bench `Outgoing`), and why the seed's own M1 one-to-one check could not close this gap alone (it runs once, over player ids, and never learns which agent ids are on-pitch versus bench). The same comment cross-references `ERR-044-023`'s boot-time seed precondition, declared in full at §4.3. Matches `CardLedgerFold.cs` v1.10 exactly. See `spec-error-log.md` `ERR-044-022`. |
| 0.15 | 2026-08-16, latest of all | — | **Reviewed-findings pass, finding M18.** §2.3's **F1** row extended to the four seed/substitution boundary refusals landed with `ERR-044-021` (the non-one-to-one construction seed, the self-substitution refusal) and `ERR-044-022` (an on-pitch `Incoming`, a bench `Outgoing`, and an out-of-range `onPitchAgentIdCount` constructor argument) — all enforced in production and unit-tested, none previously named by any F-row. New **F7** row declares the `ERR-044-020` lossless-pump refusals (the non-consecutive-tick refusal and the partial-application poison latch on `ObserveTick`), which had no failure-mode row at all despite §2.2/§3.1 both describing the mechanism normatively. `section-5.md` v0.8 in the same commit adds the matching §5.2 test-method citations and corrects §5.6's FR-DC-002 disposition from Construction to Test + Construction. |
#endregion
