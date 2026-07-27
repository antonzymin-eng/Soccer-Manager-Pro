# National Teams & International Management #36 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 1.1 Purpose

#36 is what makes a player belong to a country as well as a club: **eligibility**, **call-up selection**,
the **international window** that takes him away from his club for a fixture, and — at the deep tier — the
tournaments he plays in and the national-team job the manager might take.

**The spec's hardest problem is not the one its plan names.** The plan leads with the Stage-5 global-sim
dependency; verification turned up something more immediate and entirely unmentioned — **the game has no
concept of a player's nationality at all**, and the obvious way to add one **silently rewrites every
existing save's rosters**. §1.4 records both facts, KD-1 resolves them without touching #27, and the
global-sim gate turns out to be the *easier* of the two.

## 1.2 Scope

**In scope**

- **Eligibility** — which nation a player may represent.
- **Call-up selection** — a deterministic, capped ranking over the eligible pool.
- The **international-window schedule**, derived read-only from #30's calendar.
- **Withdrawal and return** — a squad reduction at the seam #44 already consumes.
- National-team **entrant identities** in a disjoint id range, so #43 can host international competitions.

**Out of scope** — each already has an owner; duplicating it is the failure this section prevents:

| Not owned | Owner | #36's relation |
|---|---|---|
| Canonical player records | **#27** | #36 **reads**; it adds **no field** and mutates nothing (KD-1) |
| Fixtures, brackets, draws, tables | **#43** | an international tournament **is** a #43 competition instance (KD-3) |
| The calendar | **#30** (FR-SN-009/010/011) | the window schedule is a **read-only derivation** over `SeasonCalendar` (KD-2) |
| Squad availability filtering | **#30**'s resolve→configure seam (FR-SN-013) | #36 is a **second consumer** of #44's seam, not a new one (KD-2) |
| Fatigue / condition | **#29** / **#41** | international minutes arrive as committed values on their existing inputs (KD-4) |
| The global sim that populates other nations | **Stage 5** | explicitly gated; KD-5 carves what is authorable now |
| Cross-version save migration | **#50** | #36 declares its own version and fails loud |

## 1.3 Dependencies

**Upstream (consumed):**

- **#27 Squad / Player Data** — `PlayerId`, `PlayerRecord`, `Squad`, read-only. **The only assembly #36
  references.**
- **#30 Season & Competition Loop** — the calendar arrives as a **value**; the availability filter is
  invoked **by** the seam. **Reference direction is `#30 → #36`; #36 never references #30.**

**Downstream (consumers):**

- **#30**, at the resolve→configure seam, and (deep tier) via the root's composite `ISquadProvider`.
- **#43 Competition Structure** — the **root** registers international instances; #36 supplies entrant
  ids and squads. **No reference in either direction.**
- **#29 / #41** *(deep tier)* — international minutes as routed committed values.
- **#38 UI** — value copies.

**Reference DAG**

```
root → {#30, #43, #36, #44, …}        #36 → {#27}        #43 → {#16}
root owns the composite ISquadProvider → {League, #36, match-engine}
```

**Acyclic, and #36 is a leaf over #27** — at **every** tier. It does not reference #43, #30, #44, #29,
#41, `SeasonSave` or `MatchEngine`. §1.5 KD-3 records the one design move that keeps this true at the deep
tier, where the obvious alternative would have broken it silently.

## 1.4 What verification changed

**(a) Nationality does not exist — anywhere.** `PlayerRecord` is, in full,
`{ PlayerId, FirstName, LastName, Age, Position, Attributes }`, and a case-insensitive search for
*nationality* / *nation* across `docs/specs/` and `src/` returns **no owner and no field**. #27's own
deferrals (persistence, transfers, aging) do not mention it either.

**Consequence:** the single fact #36's entire premise rests on — *which country is this player eligible
for* — has no producer. And it lands on **#27, a spec that is built, shipped and golden-vector-locked.**

**(b) …and adding it as a drawn field would silently rewrite every existing save.** `RosterGenerator`
consumes *exactly* `FIELDS_PER_PLAYER` draws per player under an explicit **ORDINAL STABILITY** contract
on the draw order — a discipline that exists because club rosters are **regenerated from the world seed,
never saved**, so a change to draw order or count *"would silently rewrite every club in every existing
save with the whole suite green."* That is why `LeagueBootstrapGoldenVectorTests` pins a golden digest,
and why the additive `Generate(…, PlayerPosition[])` overload was written to keep the draw budget
byte-identical.

**Consequence:** a nationality **draw** costs a `FIELDS_PER_PLAYER` bump, a golden-vector rebaseline, and
a **break of every existing career** — the most expensive shape a new field can take in this codebase.
KD-1 takes neither that nor a stored field.

**(c) Everything else #36 needs already exists.** #43 owns the tournament machinery and its entrant sets
are plain `int`s (`FixtureScheduler.Generate(int[] clubIds, ulong seed)` is id-agnostic); #30's FR-SN-013
resolve→**filter**→configure seam already carries #44's suspension filter, and a called-up player is
exactly a player reduced out of a squad; #31's FR-TX-019 window is the settled precedent for a
spec-owned window derived read-only from #30's calendar; and `_RESERVED_0x28_` / `SubsystemOrdinals 90`
already exists for #36 in #16 §3.4.

**Consequence:** three of the plan's five key decisions have answers waiting upstream. The work is
concentrated entirely on (a) and (b).

## 1.5 Key decisions

### KD-1 — Nationality is a pin-then-derive read, not a stored field and not a drawn one

`NationOf(playerId)` is a **pure keyed function** of `(worldSeed, playerId)` against an ordinal-stable
`NationCatalogue`, evaluated on read. It is:

- **not a `PlayerRecord` field** — #27's struct, its file format, and its consumers are untouched;
- **not an RNG draw** — `FIELDS_PER_PLAYER` is unchanged, the draw order is unchanged, and the
  `LeagueBootstrapGoldenVectorTests` digest is unchanged, so **no existing career is disturbed**;
- **not persisted per player** — the only stored nationality is a **pin** for a re-keyed or authored
  player, which is the exception rather than the representation.

This is **#32's KD-1 pattern reused exactly**: scouting derives per-attribute ranges on read from
stateless keyed noise rather than storing them, *"dissolving the save-bloat and re-roll risks by
construction."* The same move dissolves #36's much larger problem, because the thing #36 would have stored
is an attribute of a player who is himself regenerated from a seed.

**The distribution is a `[GT]` weighting over the catalogue**, so a league can be predominantly one nation
with a realistic minority spread. Note what that implies: changing the weighting changes derived
nationalities **for everyone, in every existing career** — a balance change with a save-visible effect,
which §7.4 R-1 records as such.

**The derivation alone is not sufficient, because `PlayerId` is not stable.** #31's KD-7 **re-keys** the
club-scoped `PlayerId` on a transfer — which is why #44 must *migrate* bans across it and #32 must *drop*
knowledge at it. A nationality derived from `(worldSeed, playerId)` would therefore **change when a player
transfers**: a Brazilian signs for a new club and becomes Italian, silently, on the most common event in a
career. **Nothing would detect it**, because both values are correct derivations of their respective keys.

**So `NationOf` is a pin-then-derive lookup:**

```
NationOf(playerId) = NationPins[playerId] ?? Derive(worldSeed, playerId)
```

`NationPins` is a small #36-owned table written **only** on a re-key, by the same #31 roster-move hook
(FR-TX-022) that #44 uses to migrate bans: at the moment the id changes, the **pre-transfer** nation is
resolved and pinned to the new id. An untransferred player — the overwhelming majority, at every moment of
every career — has **no entry and costs nothing**. The table is bounded by **transfer volume**, not by
pool size.

This keeps KD-1's property intact rather than merely nearly-intact: **still no `PlayerRecord` field, no
draw, no `FIELDS_PER_PLAYER` change, no golden-vector rebaseline** — the pin is #36's own state in #36's
own sub-blob, and #27 remains untouched.

*Rejected:* add `Nationality` to `PlayerRecord` and draw it. §1.4(b) — the most expensive shape available,
bought for a field that is a pure function of data the seed already determines.

*Rejected:* add the field but **derive** its value, so it is stored yet costless to generate. It puts a
**second copy** of a derived truth in a serialized struct, and the moment `RosterGenerator` and `NationOf`
disagree — a catalogue edit, a reordered enum — the save and the function diverge with nothing to detect
it. Deriving on read has one truth by construction.

*Rejected:* key the derivation on something transfer-invariant instead of pinning. **There is no such key
today.** #28's `PlayerLifecycle` (`BirthWorldDay`) is itself an overlay keyed by `PlayerId`, so it re-keys
identically; introducing a global immutable player identity would be a change to #27's core model **far
larger** than the field KD-1 declines to add, landing on every spec that keys by `PlayerId`.

**#47's authoring lands in this same table.** An authored entry is a pin like any other, consulted before
the derivation — so the re-key mechanism and the authoring mechanism are **one surface, not two**, and
because the table ships at approval (for re-keys) #47 adds no #36 surface at all (§7.4 R-2).

### KD-2 — The window is a read-only calendar derivation; withdrawal reuses #44's seam

**Schedule:** `IsWindowDay(worldDay)` / `CurrentWindow(worldDay)`, derived read-only from #30's
`SeasonCalendar` — the #31 FR-TX-019 precedent verbatim, with #36's window standing in the same relation.
**#36 never writes the calendar, never inserts a day, and never reorders a fixture.**

**Withdrawal:** #36 exposes an availability reduction with the **same shape as #44's** — a value-copy
squad filter — consumed at the FR-SN-013 resolve→configure seam. **No new #30 seam is needed** (§1.4(c)).

**Two filters now share one seam, so their composition must be pinned — and it is: order is irrelevant
because both are removals.** Filtering is set subtraction, so *suspended ∪ called-up* is the same set
whichever runs first, and neither filter reads the other's output.

That is worth stating as a **property rather than an accident**: the moment a future filter *adds* or
*substitutes* a player, the seam stops being order-free and needs an explicit order. ERR-030-016 files
that note against the seam.

**The empty-squad floor is a real risk and belongs to the seam, not to #36.** Two independent filters can
between them reduce a squad below a fieldable eleven, and `LineupSelector` **fails loud** on an unfillable
starter line (the league-bootstrap KD-6 finding). #36 does not invent a policy for that: it records that
the seam needs one — fail loud, or a defined backfill — names it as a **shared** obligation of #44/#36/#30
rather than either filter's private business, and **bounds its own contribution** with
`NT_MAX_CALLUPS_PER_CLUB` `[GT]` so a single club is never gutted by #36 alone.

### KD-3 — An international tournament is a #43 instance; #36 supplies squads and identities

#36 defines **no** fixture generator, table, bracket, or draw. A tournament is a `CompetitionFormat`
instance in #43's registry (`GroupThenKnockout` for a finals, `RoundRobin` for a qualifying group), and
every draw is #43's keyed, cursor-free `competition.draws` draw.

**The one genuine seam question is the entrant type, and it is already answered:** #43's entrant sets are
sets of **`int` ids**, ordered canonically and handed to an **id-agnostic** `FixtureScheduler.Generate`.
So national teams take ids from a **disjoint reserved range** (`NATION_TEAM_ID_BASE`, above any `ClubId`),
and **#43 needs no change at all** — FR-CP-016's *"`ClubId`s never re-key"* holds trivially for ids that
are never re-keyed either.

**Resolution reuses the same path a club fixture takes — but #36 must not implement the interface
itself.** #30 resolves squads through `ISquadProvider.ResolveByClubId`, and the league-bootstrap `League`
**is** an `ISquadProvider` rather than having an adapter written for it. The tempting move is to make
#36's registry one too. **It cannot be:** `ISquadProvider` is declared in `src/match-engine/`, so
implementing it would make #36 reference `TacticalDirector.MatchEngine` — collapsing the leaf DAG §1.3
asserts, and coupling an off-pitch selection spec to the match engine for the sake of one method
signature.

Instead #36 exposes `TryResolveNationSquad(nationTeamId, out Squad) → bool` — a `PlayerDatabase.Squad`, a
type #36 already depends on — and the **root** supplies #30 with a **composite** `ISquadProvider` that
routes ids in the national range to #36 and everything else to `League`. The root is the assembly that
already references both, the same layering that puts #46's projectors and #49's boundary adapters there.
**#30 still sees exactly one provider and needs no branch; #36 stays a leaf.**

The `League`-is-a-provider precedent still applies — to the **composite at the root**, which is the thing
#30 actually holds. What does not transfer is the assumption that every squad source should implement the
interface directly: `League` lives in `season-save`, which already references `match-engine`; **#36 does
not and should not.**

### KD-4 — Fatigue and minutes travel as committed values on existing inputs

International minutes reach #29 conditioning and #41 injury risk the way every other cross-system quantity
in this project does: as **integers the root routes** into their existing per-day inputs, never by #36
writing their state or by them referencing #36.

#36 records minutes played per called-up player and exposes them; whether they *feed* those systems at the
minimal tier is a **deferred wiring**, because at minimal **no international match is played** (KD-5) and
there are no minutes to route. Building the route before there are minutes would be the phantom-consumer
class FR-LW-031 forbids.

### KD-5 — The Stage-5 gate: "withdrawal without a match" is the authorable minimum

**Authorable now** (needs only the managed league's own player pool): eligibility derivation, call-up
selection, the window schedule, withdrawal and return, and the persistence and determinism contracts for
all of it. Every one is exercisable and testable against a single generated league.

**Gated on the Stage-5 global sim** (needs rosters for nations #30 does not simulate): playing an
international fixture, tournaments, qualification, and the national-team job. **Not because the machinery
is missing** — KD-3 shows #43 already has it — but because an opponent nation has **no players to field**.

**Why the minimal tier is still worth shipping.** Withdrawal is the half that touches the player's actual
career: a squad losing three starters to an international window is a real, felt consequence with no
international match rendered anywhere, and it exercises the whole eligibility/selection/persistence path
the deep tier then reuses unchanged. The alternative — defer #36 entirely to Stage 5 — leaves
`_RESERVED_0x28_` and the nationality question **open across every intervening spec**, and #47's database
editor would land with no owner for the nationality field it must edit.

**Call-up selection is draw-free**: a deterministic ranking (mean attributes, `PlayerId` tie-break) over
the eligible pool, capped per club — the `LineupSelector` model (greedy by rating, no RNG).

### KD-6 — Persistence: an opaque, independently version-gated sub-blob

`NATIONAL_TEAM_SAVE_FORMAT_VERSION` [FIXED] = 1 — the current call-up **selection** (a list of `PlayerId`s
per national team), the window cursor, per-player international minutes, and the **`NationPin` table** —
composed into #30's `SeasonSaveCodec`, **not** a `WORLD_STORE_FORMAT_VERSION` bump. Version gate first;
overflow-safe length prefixes against `total − offset`; trailing-byte guard; fail loud on all three;
**APPEND-only** layout.

**The `NationPin` table is in that list for a reason worth stating.** Without it a transferred player's
nationality **reverts to the derivation of his new id on the next load** — which is precisely the defect
the pin exists to prevent, re-introduced by the save layer.

**Deliberately absent:** any RNG cursor (KD-8 leaves none); any **national squad roster** — the squad is a
**selection view** over #27's pool, so only the *selection* is stored, never copies of the records; any
**per-player nationality** for an unpinned player (derived); and any **tournament or bracket state**,
which is **#43's** sub-blob, not #36's.

Note that a pinned value **equal to its derivation is still stored**: the pin's job is to survive a key
change the derivation cannot, so "optimising away" a redundant-looking pin re-opens the transfer defect.

### KD-7 — Behaviour-neutral identity, stated honestly

With no window configured: no player is ever withdrawn ⇒ every squad reaching `ConfigureSquads` is
byte-identical to pre-#36, and no stream is registered ⇒ every existing cursor is byte-identical. A season
advanced with #36 present is byte-identical to the same season pre-#36 **except #36's own sub-blob** — the
#44 FR-DC-018 formulation, adopted verbatim because it is the honest one.

**Nationality is read at the minimal tier** — eligibility *is* the minimal tier — but reading it moves no
byte outside #36: `NationOf` is a pure function plus a lookup in #36's own table, so no roster, no golden
vector, and no `PlayerRecord` byte is touched by it. That is the claim worth making; *"nothing reads it"*
would be both false and weaker.

### KD-8 — Draw-free at every tier #36 owns

Selection is a deterministic ranking (KD-5) and tournament draws are #43's (KD-3), so **#36 registers no
stream and promotes no domain tag at any tier described here**. `_RESERVED_0x28_` / `SubsystemOrdinals 90`
**already exists** in #16 §3.4 and is already correct, so there is **nothing to file** — and it may stay
reserved **permanently**.

Stated as its own decision rather than as a clause inside KD-3 because the property spans two decisions
(KD-3's tournament draws and KD-5's selection) and is what a reviewer checking determinism will look for
directly. If a genuinely #36-owned stochastic surface ever appears — an injury-forced replacement call-up,
say — that is its first draw site and the promotion happens **there**, on the record.

## 1.6 Determinism posture

- **World tick + the #30 resolve→configure seam**; never the 10 Hz tactical or 60 Hz physics loops. #36
  feeds no digest.
- **Draw-free at every tier #36 owns** (KD-8). No stream, no tag; `_RESERVED_0x28_` stays reserved.
- **Nationality is a pure keyed function under a pin table** (KD-1): an unpinned player has **no stored
  copy at all**; a pinned player has **exactly one**, written only at a re-key. Both paths are
  deterministic — and the pin is the reason the derivation *alone* is not.
- **All-integer**; no float.
- Window advance is a `worldDay` comparison off `LastAdvancedWorldDay`: same-day re-run is a **no-op**, a
  day **gap** **fails loud** (the #33 F6 guard).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §1 (scope, out-of-scope table, leaf DAG, §1.4's three verification findings — the missing nationality concept and the golden-vector cost of adding it — KD-1..KD-7 from supplement v0.6 plus **KD-8** promoted to its own decision, determinism posture). KD-8 is separated because the draw-free property spans KD-3 and KD-5 and is what a determinism reviewer looks for directly; reachable only through the tournament decision, it would be easy to miss. Status IN REVIEW. |
#endregion
