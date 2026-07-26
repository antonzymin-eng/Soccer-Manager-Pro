# National Teams & International Management #36 — Design Supplement

> **Created:** July 26, 2026
> **Last Updated:** July 26, 2026 (v0.6 — AR-5 sweep: 0H+0M+2L, **CONVERGENCE**; prior v0.5 AR-4, v0.4 AR-3, v0.3 AR-2, v0.2 AR-1, v0.1 initial)
> **Version:** 0.6
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row)
> **Candidate spec:** **#36** · **FR prefix:** `FR-NT` · **Wave:** 6 · **Tier:** S5
> **Promoted from:** `docs/tracking/spec-plans/spec-36-national-teams-international.md` v0.1
> **Wave 6 siblings:** `media-press-interactions-design.md` (#35), `news-inbox-man-management-design.md` (#46)

---

## 0. Purpose and posture

This supplement resolves the five key decisions the #36 plan defers, against **verified** upstream source
rather than assumption. Design only — no code, no section files, no registry row.

**The plan's hardest problem is not the one it names.** Its §9 leads with the Stage-5 global-sim dependency;
verification turns up something more immediate and entirely unmentioned — **the game has no concept of a
player's nationality** (§2(a)), and the obvious way to add one silently rewrites every existing save's
rosters (§2(b)). KD-1 resolves both without touching `PlayerRecord`, and the global-sim gate turns out to be
the *easier* of the two (KD-5).

## 1. Scope

**#36 owns:** **eligibility and call-up selection** for national teams, the **international-window schedule**,
and the national-team **entrant identities** — plus, at the deep tier, the manager's own national-team job.

**#36 does not own:**

| Not owned | Owner | How #36 relates |
|---|---|---|
| Canonical player records | **#27** | #36 **reads**; it adds no field and mutates nothing (KD-1) |
| Fixtures, brackets, draws, tables | **#43** | an international tournament **is** a #43 competition instance (KD-3) |
| The calendar | **#30** (FR-SN-009/010/011) | the window schedule is a read-only derivation over `SeasonCalendar` (KD-2) |
| Squad availability filtering | **#30**'s resolve→configure seam (FR-SN-013) | #36 is a **second consumer** of #44's seam, not a new one (KD-2) |
| Fatigue / condition | **#29** / **#41** | international minutes arrive as committed values on their existing inputs (KD-4) |
| The global sim that populates other nations | **Stage 5** | explicitly gated; KD-5 carves what is authorable now |

## 2. What already exists (verified)

**(a) Nationality does not exist — anywhere.** `src/player-database/PlayerRecord.cs` is, in full:

```csharp
public struct PlayerRecord {
    public int PlayerId; public string FirstName; public string LastName;
    public int Age;      public PlayerPosition Position; public PlayerAttributes Attributes;
}
```

and a case-insensitive search for *nationality* / *nation* across `docs/specs/` and `src/` returns **no
owner and no field** — the hits are unrelated uses of the word. #27's own §4 deferrals (persistence,
transfers, aging) do not mention it either.

**Consequence:** the single fact #36's entire premise rests on — *which country is this player eligible
for* — has no producer. This is not a small gap: it is #36's KD-1, and it lands on #27, a spec that is
**built, shipped and golden-vector-locked**.

**(b) …and adding it as a drawn field would silently rewrite every existing save.**
`src/player-database/RosterGenerator.cs`: each player consumes *exactly*
`PlayerDatabaseConstants.FIELDS_PER_PLAYER` draws under an explicit **ORDINAL STABILITY** contract on the
draw order, and the additive `Generate(rng, streamIndex, clubId, PlayerPosition[])` overload was written so
that *"the position draw still runs and is discarded, so the budget, the stream layout, and the
drawn-position path stay byte-identical."*

That discipline exists because of the root `CLAUDE.md` **KD-10 / H-1** finding: club rosters are
**regenerated from the world seed, never saved**, so a change to draw order or count *"would silently
rewrite every club in every existing save with the whole suite green"* — which is why
`LeagueBootstrapGoldenVectorTests` pins a golden digest.

**Consequence:** a nationality **draw** costs `FIELDS_PER_PLAYER + 1`, a golden-vector rebaseline, and a
break of every existing career — the most expensive shape a new field can take in this codebase. KD-1 takes
neither that nor a stored field.

**(c) #43 already owns every piece of tournament machinery #36 would otherwise duplicate**, and its
entrant type is verifiably id-agnostic (`FixtureScheduler.Generate(int[] clubIds, ulong seed)` —
`src/season-save/FixtureScheduler.cs`).
`competition-structure/section-2.md`: `CompetitionFormat { RoundRobin, Knockout, GroupThenKnockout }`
(FR-CP-001); entrant sets in canonical ascending order feeding every draw (FR-CP-005); round-robin instances
reusing `FixtureScheduler.Generate(clubIds, seed)` with a **draw-free** per-instance seed derivation
(FR-CP-006); keyed position-independent knockout draws on `competition.draws`, `entityId = competitionId`,
**no cursor** (FR-CP-007/009); persisted brackets with fail-loud coherence (FR-CP-010/011).

**Consequence:** the plan's KD-3 ("does #36 reuse #43's draw machinery or define its own?") has an
unambiguous answer, and the entrant type is the only real question — see KD-3.

**(d) The withdrawal mechanism already exists, and #36 is its second consumer.** FR-SN-013 pins a
**resolve → *filter* → configure** null seam on the managed fixture's squad path, and #44's FR-DC-010 makes
it *"a value-copy reduction"* applied to **both** clubs of an engine-resolved fixture. A called-up player is
exactly a player reduced out of a squad for a fixture.

**Consequence:** #36 needs **no new #30 seam** for the plan's central minimal behaviour. It composes at a
seam already specified, already justified, and already carrying one consumer — with the composition order
between two filters being the only new question (KD-2).

**(e) The window-schedule pattern is settled precedent.** `transfers-contracts-negotiation/section-2.md`
**FR-TX-019** — *"The transfer window MUST be a #31-owned `TransferWindow [OpenWorldDay, CloseWorldDay]`
derived deterministically from #30's `SeasonCalendar` (read-only); minimal = one summer window. #31 MUST NOT
mutate the calendar."* The dependency runs into #30, never out of it, and the window belongs to the spec
that cares about it.

**(f) `_RESERVED_0x28_` / `SubsystemOrdinals 90` already exists for #36** (`deterministic-sim/section-3.md`
§3.4, added by the v1.0.13 A-04 sweep). **Nothing to file** if #36 proves draw-free — and per KD-3 it may
stay reserved permanently, since the draws #36 needs are #43's.

## 3. Staging (minimal-first → deep)

| Tier | Content |
|---|---|
| **Minimal (the identity)** | Derived eligibility, a deterministic call-up selection for the managed league's own nations, one international window per season that **withdraws** called-up players from club fixtures and returns them. **No international match is played**, no tournament, no draw, no stream, no new #30 seam. |
| **Deep** | International fixtures and tournaments as **#43 instances** (group + knockout, #43's draws); qualification; a national-team job for the manager; opponent nations populated by the Stage-5 global sim. |

The minimal tier is deliberately *"withdrawal without a match"* — see KD-5, which is what makes #36
authorable at all before Stage 5.

## 4. Key decisions

### KD-1 — Nationality is a **derived read**, not a stored field and not a drawn one

`NationOf(playerId)` is a **pure keyed function** of `(worldSeed, playerId)` against an ordinal-stable
`NationCatalogue`, evaluated on read. It is:

- **not a `PlayerRecord` field** — #27's struct, its file format, and its consumers are untouched;
- **not an RNG draw** — `FIELDS_PER_PLAYER` is unchanged, the draw order is unchanged, and the
  `LeagueBootstrapGoldenVectorTests` digest is unchanged, so **no existing career is disturbed** (§2(b));
- **not persisted per player** — the only stored nationality is a pin for a re-keyed or authored player,
  which is the exception rather than the representation (see below).

This is #32's KD-1 pattern reused exactly: scouting derives per-attribute ranges on read from stateless
keyed noise rather than storing them, *"dissolving the save-bloat and re-roll risks by construction."* The
same move dissolves #36's much larger problem, because the thing #36 would have stored is an attribute of a
player who is himself regenerated from a seed.

**The distribution is a `[GT]` weighting over the catalogue**, so a league can be predominantly one nation
with a realistic minority spread, and the weighting is a pure function — changing it changes derived
nationalities for everyone, which is a **balance change with a save-visible effect** and must be treated as
one (§11 R-1).

*Rejected alternative:* add `Nationality` to `PlayerRecord` and draw it. Rejected on §2(b) — it is a
`FIELDS_PER_PLAYER` bump, a golden-vector rebaseline, and a silent rewrite of every existing save's rosters,
bought for a field that is a pure function of data the seed already determines.

*Rejected alternative:* add the field but derive its value (no draw), so it is stored yet costless to
generate. Rejected — it puts a **second copy** of a derived truth in a serialized struct, and the moment
`RosterGenerator` and `NationOf` disagree (a catalogue edit, a reordered enum) the save and the function
diverge with nothing to detect it. Deriving on read has one truth by construction.

**The derivation alone is not sufficient, because `PlayerId` is not stable.** #31's KD-7 **re-keys** the
club-scoped `PlayerId` on a transfer — which is why #44's FR-DC-013 must *migrate* bans across it and #32
must *drop* knowledge at it. A nationality derived from `(worldSeed, playerId)` would therefore **change
when a player transfers**: a Brazilian signs for a new club and becomes Italian, silently, on the most
common event in a career. Nothing would detect it, because both values are "correct" derivations of their
respective keys.

**So `NationOf` is a pin-then-derive lookup:**

```
NationOf(playerId) = NationPins[playerId]  ?? Derive(worldSeed, playerId)
```

`NationPins` is a small #36-owned table written **only** on a re-key, by the same #31 roster-move hook
(FR-TX-022) that #44 uses to migrate bans: at the moment the id changes, the *pre-transfer* nation is
resolved and pinned to the new id. An untransferred player — the overwhelming majority, at every moment of
every career — has no entry and costs nothing. The table is bounded by transfer volume, not by pool size,
and it is the only #36 state that is not a selection.

This is what keeps the KD-1 property intact rather than merely nearly-intact: **still no `PlayerRecord`
field, no draw, no `FIELDS_PER_PLAYER` change, no golden-vector rebaseline** — the pin is #36's own state in
#36's own sub-blob, and #27 remains untouched.

*Rejected alternative:* key the derivation on something transfer-invariant instead of pinning. Rejected —
**there is no such key today.** #28's `PlayerLifecycle` (`BirthWorldDay`) is itself an overlay keyed by
`PlayerId`, so it re-keys identically; introducing a global immutable player identity would be a change to
#27's core model far larger than the field KD-1 declines to add, and it would land on every spec that keys
by `PlayerId`.

**#47's authoring lands in this same table** — an authored entry is a pin like any other, consulted before
the derivation. The re-key mechanism and the authoring mechanism are one surface, not two, and because the
table ships at approval (for re-keys) #47 adds no #36 surface at all (§11 R-2).

### KD-2 — The window is a read-only calendar derivation; withdrawal reuses #44's seam

**Schedule:** `IsWindowDay(worldDay)` / `CurrentWindow(worldDay)` derived read-only from #30's
`SeasonCalendar` — the #31 precedent verbatim (FR-TX-019: *"a #31-owned `TransferWindow [OpenWorldDay,
CloseWorldDay]` derived deterministically from #30's `SeasonCalendar` (read-only); #31 MUST NOT mutate the
calendar"*), with #36's window standing in the same relation. #36 never writes the calendar, never inserts a
day, and never reorders a fixture — the dependency is one-directional into #30, which is the plan's KD-2
requirement, satisfied without a new #30 surface.

**Withdrawal:** #36 exposes an availability reduction with the **same shape as #44's** — a value-copy squad
filter — consumed at the FR-SN-013 resolve→configure seam (§2(d)).

**Two filters now share one seam, so their composition must be pinned, and it is: order is irrelevant
because both are removals.** Filtering is set subtraction, so suspended ∪ called-up is the same set
whichever runs first, and neither filter reads the other's output. That is worth stating as a property
rather than an accident — the moment a future filter *adds* or *substitutes* a player, the seam stops being
order-free and needs an explicit order. #36's spec carries that as a note on the seam, not as a silent
assumption.

**The empty-squad floor is a real risk here and belongs to the seam, not to #36.** Two independent filters
can, between them, reduce a squad below a fieldable eleven — `LineupSelector` fails loud on an unfillable
starter line (the league-bootstrap KD-6 finding). #36 does not invent a policy for that; it records that the
seam needs one (fail loud, or a defined backfill), names it as a **shared** obligation of #44/#36/#30 rather
than either filter's private business, and bounds its own contribution with `NT_MAX_CALLUPS_PER_CLUB` `[GT]`
so a single club is never gutted.

### KD-3 — An international tournament **is a #43 instance**; #36 supplies squads and identities

#36 defines **no** fixture generator, table, bracket, or draw. A tournament is a `CompetitionFormat`
instance in #43's registry (`GroupThenKnockout` for a finals, `RoundRobin` for a qualifying group), and every
draw is #43's keyed, cursor-free `competition.draws` draw (§2(c)).

**The one genuine seam question is the entrant type**, and the answer is that #43's entrant set is a set of
**`int` ids** — FR-CP-005 orders them, FR-CP-006 hands them to `FixtureScheduler.Generate(clubIds, seed)`,
which is id-agnostic. So national teams take ids from a **disjoint reserved range** (`NATION_TEAM_ID_BASE`,
above any `ClubId`), and #43 needs no change at all: FR-CP-016's *"`ClubId`s never re-key"* holds trivially
for ids that are never re-keyed either.

**Resolution reuses the same path a club fixture takes — but #36 must not implement the interface itself.**
#30 resolves squads through `ISquadProvider.ResolveByClubId`, and the league-bootstrap `League` **is** an
`ISquadProvider` rather than having an adapter written for it. The tempting move is to make #36's registry
one too. **It cannot be:** `ISquadProvider` is declared in `src/match-engine/`, so implementing it would make
#36 reference `TacticalDirector.MatchEngine` — collapsing the leaf DAG §10 asserts and coupling an off-pitch
selection spec to the match engine for the sake of one method signature.

Instead #36 exposes `TryResolveNationSquad(nationTeamId, out Squad) → bool` — a `PlayerDatabase.Squad`,
a type #36 already depends on via #27 — and the **root** supplies #30 with a composite `ISquadProvider` that
routes ids in the national range to #36 and everything else to `League`. The root is the assembly that
already references both, which is the same layering that puts #46's projectors and #49's boundary adapters
there. #30 still sees exactly one provider and needs no branch; #36 stays a leaf.

*(The `League`-is-a-provider precedent still applies — to the composite at the root, which is the thing #30
actually holds. What does not transfer is the assumption that every squad source should implement the
interface directly: `League` lives in `season-save`, which already references `match-engine`; #36 does not
and should not.)*

**Consequence for determinism:** the draws #36 needs are #43's, so **`_RESERVED_0x28_` stays RESERVED at
every tier described here** — possibly permanently. #36 files nothing against #16 (§2(f)). If a genuinely
#36-owned stochastic surface ever appears (an injury-forced replacement call-up, say), that is its first
draw site and the promotion happens there.

### KD-4 — Fatigue and minutes travel as **committed values on existing inputs**

International minutes reach #29 conditioning and #41 injury risk the way every other cross-system quantity
in this project does: as integers the root routes into their existing per-day inputs, never by #36 writing
their state or by them referencing #36. #36 records minutes played per called-up player and exposes them;
whether they *feed* those systems at the minimal tier is a **deferred** wiring, because at minimal no
international match is played (KD-5) and there are no minutes to route. Building the route before there are
minutes would be the phantom-consumer class FR-LW-031 forbids.

### KD-5 — The Stage-5 gate: **withdrawal without a match** is the authorable minimum

The plan's §9 leads with *"most of the spec is not authorable until nations are simulated."* The carve is
sharper than that:

**Authorable now** (needs only the managed league's own player pool): eligibility derivation, call-up
selection, the window schedule, withdrawal and return, and the persistence and determinism contracts for all
of it. Every one of these is exercisable and testable against a single generated league.

**Gated on Stage-5 global sim** (needs rosters for nations #30 does not simulate): playing an international
fixture, tournaments, qualification, and the national-team job. Not because the *machinery* is missing —
KD-3 shows #43 already has it — but because an opponent nation has no players to field.

**Why the minimal tier is still worth shipping:** withdrawal is the half that touches the player's actual
career. A squad losing three starters to an international window for a fixture is a real, felt consequence
with no international match rendered anywhere, and it exercises the whole eligibility/selection/persistence
path that the deep tier then reuses unchanged. The alternative — defer #36 entirely to Stage 5 — leaves
`_RESERVED_0x28_` and the nationality question open across every intervening spec, and #47's database editor
would land with no owner for the nationality field it must edit.

**Call-up selection is draw-free**: a deterministic ranking (mean attributes, `PlayerId` tie-break) over the
eligible pool, capped per club — the `LineupSelector` model (greedy by rating, no RNG), which is also why
the minimal tier registers no stream.

### KD-6 — Persistence: an opaque, independently version-gated sub-blob

`NATIONAL_TEAM_SAVE_FORMAT_VERSION` [FIXED] = 1 — the current call-up selection (a list of `PlayerId`s per
national team), the window cursor, per-player international minutes, and the **`NationPin` table** (KD-1 —
without it a transferred player's nationality reverts to the derivation of his new id on the next load,
which is precisely the defect the pin exists to prevent) — composed into #30's
`SeasonSaveCodec`, **not** a `WORLD_STORE_FORMAT_VERSION` bump (the #40/#42/#43/#44/#45 pattern). Version gate
first; overflow-safe `Require(offset, need, total)` bounds; trailing-byte guard; fail loud on all three;
APPEND-only layout.

**Deliberately absent:** any RNG cursor (KD-3/KD-5 leave none), any **national squad roster** (the squad is a
selection **view** over #27's pool — the plan's KD-1, preserved: only the *selection* is stored, never
copies of the records), and any **per-player nationality** (derived — KD-1; only re-key/authored pins are
stored, and a pinned value that equals its derivation is still stored, because the pin's job is to survive a
key change the derivation cannot). Tournament and bracket state is **#43's**
sub-blob, not #36's.

### KD-7 — Behaviour-neutral identity

With no window configured: no player is ever withdrawn ⇒ every squad reaching `ConfigureSquads` is
byte-identical to pre-#36, and no stream is registered ⇒ every existing cursor is byte-identical. A season
advanced with #36 present is byte-identical to the same season pre-#36 **except** #36's own sub-blob (the
#44 FR-DC-018 formulation, adopted verbatim because it is the honest one).

**Nationality is read at the minimal tier** — eligibility is the minimal tier — but reading it moves no byte
outside #36: `NationOf` is a pure function plus a lookup in #36's own table, so no roster, no golden vector,
and no `PlayerRecord` byte is touched by it. That is the claim worth making; "nothing reads it" would be
both false and weaker.

## 5. Persistent state (shape)

```
CallUp        : { NationTeamId (int), PlayerId (int), CalledWorldDay (uint) }   # the SELECTION only
WindowCursor  : { CurrentWindowIndex (int), LastAdvancedWorldDay (uint, sentinel uint.MaxValue) }
IntlMinutes   : { PlayerId (int), MinutesTotal (int) }                          # deep tier; empty at minimal
NationPin     : { PlayerId (int), NationId (int) }        # KD-1: written ONLY on a #31 re-key (or by #47
                                                          # authoring). Absent for every untransferred player.
```

All integer. `CallUp` stores **ids, never records** — the KD-1 view discipline. Entries are canonically
ordered (`NationTeamId` then `PlayerId`, ascending, no duplicates) with a fail-loud decode gate, so two
equivalent states cannot serialize differently. An `IntlMinutes` entry at `0` is never recorded (the #44
canonical-drop rule).

**Roster lifecycle:** a `CallUp` for a player who retires or leaves the pool is **dropped** in lockstep with
#28's boundary churn (#33's FR-HS-027 model); on a #31 re-key it **migrates**, following #44's ban rule
rather than #32's drop rule — a call-up is a live selection of a person, not a stale fact about a squad
slot. A `NationPin` **migrates** for the same reason and by the same hook (it exists *because* of the
re-key), and is **dropped on retirement** with the player's other entries — an unpruned pin table would
outlive its pool and grow monotonically across a career.

## 6. Determinism posture

- World tick + the #30 resolve→configure seam; never the match loops.
- **Draw-free at every tier #36 owns** — selection is a deterministic ranking (KD-5), and tournament draws
  are #43's (KD-3). No stream, no tag; `_RESERVED_0x28_` stays reserved (§2(f)).
- Nationality is a pure keyed function of `(worldSeed, playerId)` **under a pin table** (KD-1): unpinned
  players have no stored copy at all; a pinned player has exactly one, written only at a re-key, which is
  what makes nationality survive an id change. Both paths are deterministic; the pin is the reason the
  *derivation* alone is not.
- All-integer; no float.
- Window advance is a `worldDay` comparison off `LastAdvancedWorldDay`: same-day re-run is a **no-op**, a day
  gap **fails loud** (the #33 F6 guard).

## 7. Primary surfaces (proposed)

| Surface | Direction | Notes |
|---|---|---|
| `NationOf(playerId) → int` | #36 → callers | the KD-1 derivation; pure, no state, no draw |
| `IsWindowDay(worldDay) → bool` / `CurrentWindow(worldDay)` | #36 → #30 root | derived from `SeasonCalendar`; #36 never writes it (KD-2) |
| `SelectCallUps(worldDay)` | root → #36 | deterministic ranking, capped per club (KD-5) |
| `FilterAvailable(in squad, worldDay) → squad` | #30 seam → #36 | the value-copy reduction at resolve→configure, composed with #44's (KD-2) |
| `TryResolveNationSquad(nationTeamId, out Squad) → bool` | root → #36 | **deep tier**; the root's composite `ISquadProvider` delegates here. #36 does **not** implement `ISquadProvider` — it is declared in `match-engine` (KD-3) |
| `MinutesOf(playerId) → int` | #36 → root | **deep tier**; routed into #29/#41 as committed values (KD-4) |
| `OnPlayerReKeyed(oldPlayerId, newPlayerId)` | #31 hook → #36 | the **only** writer of a `NationPin` (KD-1); resolves the pre-transfer nation and pins it to the new id, then migrates the player's `CallUp` — the FR-TX-022 hook #44 uses for bans |

## 8. Cross-spec back-props

### 8.1 At approval (must land atomically with the status flip)

| ID | Target | Change |
|---|---|---|
| **ERR-030-016** | #30 §3.4 / FR-SN-013 | Record that the resolve→configure filter seam admits **more than one** consumer (#44 suspensions, #36 call-ups), that the current consumers compose order-independently **because both are removals**, and that a future non-removal filter would require an explicit order. Also names the shared **empty-squad floor** obligation (KD-2). No new seam — a contract note on an existing one. |

### 8.2 Deferred (land at the named tier, not at approval)

- The `SEASON_SAVE_FORMAT_VERSION` bump, at T2 when the sub-blob is first composed in.
- #43 instance registration for international competitions (deep tier) — a #36-side use of #43's existing
  API, not a #43 change (KD-3).
- Routing international minutes into #29/#41 (KD-4) — no minutes exist until the deep tier.
- Promotion of `_RESERVED_0x28_`, **only** if a #36-owned stochastic surface ever appears (§2(f)).

### 8.3 Explicitly **not** back-props

- **#27** — **nothing to change**, which is KD-1's whole point: no `PlayerRecord` field, no
  `RosterGenerator` draw, no `FIELDS_PER_PLAYER` bump, no golden-vector rebaseline, no save break (§2(b)).
- **#43** — nothing to change. Entrant sets are `int`s, `FixtureScheduler` is id-agnostic, and national
  teams take a disjoint id range (KD-3). #36 uses the API as specified.
- **#16** — `_RESERVED_0x28_` already exists and is already correct for a draw-free spec (§2(f)).
- **#44** — nothing to change. The two filters compose at a seam #44 does not own; the composition note is
  filed against #30, where the seam lives.
- **#30's calendar** — read-only derivation, the #31 precedent (§2(e)).

## 9. Test focus

**The KD-1 transfer lock, which is the one a naive implementation fails:** a called-up player transferred
mid-season keeps his nationality across the re-key (assert `NationOf` before and after are equal, and that
the pin — not the derivation — is what carries it). Then **the KD-1 golden-vector lock:** `LeagueBootstrapGoldenVectorTests` and every
`RosterGenerator` digest are **unchanged** by #36 (assert the golden vector explicitly in #36's own suite, so
a later maintainer who adds a nationality draw fails #36's tests as well as #27's); `NationOf` determinism
and distribution over a generated league; `NationOf` stability across a save/restore **for a pinned player
as well as an unpinned one** (the unpinned case is free — nothing is stored; the pinned case is the one that
can regress, and it is exactly the case a transfer creates). Identity (no window ⇒ every `ConfigureSquads` squad byte-identical to pre-#36); filter
composition (suspended ∪ called-up is order-independent — assert both orders); the `NT_MAX_CALLUPS_PER_CLUB`
cap and the empty-squad floor's defined behaviour; round-trip determinism over the sub-blob including a
mid-window save; the window no-op / day-gap guard pair; call-up selection determinism and its `PlayerId`
tie-break; canonical ordering + fail-loud decode of `CallUp` entries; roster-lifecycle drop-on-retire /
migrate-on-re-key; and **structural** assertions that #36 references neither #43, #44, #29, #41, nor `MatchEngine` — the last
being the one a deep-tier implementer would break first, by implementing `ISquadProvider` directly (KD-3).

## 10. Reference DAG

```
root → {#30, #43, #36, #44, …}        #36 → {#27}        #43 → {#16}
root owns the composite ISquadProvider → {League, #36, match-engine}
```

**Acyclic, and #36 is a leaf over #27.** It reads the player pool and derives over it. It does not reference
#43 (the root registers instances), #30 (the calendar arrives as a value; the filter is invoked *by* the
seam), #44, #29, #41, or `MatchEngine`.

**The deep tier does not weaken this** — which it would have, silently, under the obvious design. The
composite `ISquadProvider` lives at the root; #36 exposes `TryResolveNationSquad` returning a
`PlayerDatabase.Squad`, a type it already depends on. Had #36 implemented `ISquadProvider` itself it would
have taken a `match-engine` reference for one signature, and the structural assertion in §9 would have had
to be weakened to "true at the minimal tier only" — the class of erosion that makes a DAG claim worthless.

## 11. Risks and standing options

- **R-1 — the nationality distribution is a save-visible `[GT]`.** Changing the catalogue or its weighting
  changes `NationOf` for every existing player, in every existing career. It is a `[GT]` whose edits behave
  like a schema change, and #36's §3 must say so; the golden-vector discipline #27 uses for rosters is the
  right model if it ever needs pinning.
- **R-2 — #47's authored nationalities land in the table #36 already ships.** After AR-2 the `NationPin`
  table exists at approval (for re-keys), and an authored entry is a pin like any other — so #47 needs no new
  #36 surface, and the derivation stays the default for every unpinned player. What #47 *will* need is a
  policy for authored-vs-derived precedence when both a pin and an edit exist for one player; that is #47's
  to decide, and it is cheap only because the surface is already one table rather than two.
- **R-3 — the empty-squad floor is genuinely shared** (KD-2). If it is not resolved at the seam, each filter
  will grow its own guard and they will disagree. Filed as ERR-030-016 for that reason.
- **R-4 — "just add the field" will be proposed again.** It is the obvious move and it is expensive for
  reasons that live in #27's *test* discipline rather than its API. §9's explicit golden-vector assertion in
  #36's own suite is what makes the cost visible to whoever tries.
- **R-5 — Stage-5 scope creep.** The deep tier is genuinely blocked (KD-5); the risk is that its absence
  makes the minimal tier look pointless and it gets padded with a fake international match against
  synthesised opponents. That would be canon invented by a consumer — the thing FR-LW-031 exists to prevent.

## 12. Promotion pipeline

1. **This supplement, AR-converged** — **DONE at v0.6.** AR-1 (1H+2M+1L) → v0.2, AR-2 (1H+0M+1L) → v0.3,
   AR-3 (0H+2M) → v0.4, AR-4 (0H+2M) → v0.5, AR-5 (0H+0M+2L) → v0.6 = **CONVERGENCE** (an L-only round
   closes the cycle, per the project convention).
2. **Author 11 section files** at `Status: IN REVIEW` under `docs/specs/national-teams-international/`, FR
   prefix `FR-NT`.
3. **Section-file PASS-1 adversarial review** + a fix pass, recorded in §9.4.1 of the checklist.
4. **`SPEC_INDEX.md` registry row** at promotion.
5. **Lead-developer R-01..R-05 sign-off** — a human authority, not self-grantable.
6. **Flip to `APPROVED`**, landing the §8.1 back-prop atomically.

## Version History

| Version | Date | Change |
|---|---|---|
| v0.1 | July 26, 2026 | Initial supplement promoted from the one-page plan. **Verification surfaced a gap the plan never mentions and which outranks the Stage-5 dependency it does:** nationality — the fact #36's entire premise rests on — **does not exist in any spec or source file**, and adding it as a drawn `PlayerRecord` field would cost a `FIELDS_PER_PLAYER` bump, a `LeagueBootstrapGoldenVectorTests` rebaseline, and a **silent rewrite of every existing save's rosters** (the root `CLAUDE.md` KD-10/H-1 defect class, since rosters are regenerated from the seed rather than saved). **KD-1** makes nationality a **pure keyed derivation on read** — #32's fog-of-war pattern — so #27 is untouched at every level: no field, no draw, no golden-vector change, no save break. **KD-3** kills the plan's draw-duplication risk outright: an international tournament **is** a #43 competition instance (its entrant sets are plain `int`s and `FixtureScheduler` is id-agnostic, so national teams take a disjoint id range and #43 needs no change), and #36's registry is an `ISquadProvider` over its call-ups — the `League`-is-an-`ISquadProvider` precedent — so `_RESERVED_0x28_` may stay reserved permanently. **KD-2** finds the withdrawal mechanism already specified: #30's FR-SN-013 resolve→configure filter seam, of which #36 is the **second** consumer after #44 — needing no new #30 surface, but requiring a contract note on multi-consumer composition and the shared empty-squad floor (the one back-prop, ERR-030-016). **KD-5** carves the Stage-5 gate at *"withdrawal without a match"*, which is authorable now against a single generated league and exercises the whole path the deep tier reuses. |
| v0.2 | July 26, 2026 | **AR-1 fix pass: 1H + 2M + 1L, all resolved.** **H-1** — KD-3 made #36’s registry **an `ISquadProvider`**, citing the `League`-is-a-provider precedent, but `ISquadProvider` is declared in `src/match-engine/`: #36 would have referenced `TacticalDirector.MatchEngine` for one method signature, collapsing the leaf DAG §10 asserts and forcing §9’s structural assertion down to “true at the minimal tier only”. Resolved by inverting it — #36 exposes `TryResolveNationSquad(…, out Squad)` over the `PlayerDatabase` type it already depends on, and the **root** owns the composite provider that routes national ids to it (the same layering as #46’s projectors and #49’s adapters). The precedent still holds, for the composite; what does not transfer is the assumption that every squad source implements the interface — `League` lives in `season-save`, which already references `match-engine`. **M-1** — §2(e) cited the #31 window precedent from a summary rather than source; replaced with FR-TX-019 verbatim. **M-2** — §2(c)’s claim that #43’s entrant type is id-agnostic was the load-bearing premise of KD-3 and was asserted, not shown; now cites the verified `FixtureScheduler.Generate(int[] clubIds, ulong seed)` signature. **L** — §9’s structural assertion now names `MatchEngine` as the one a deep-tier implementer breaks first, and why. |
| v0.3 | July 26, 2026 | **AR-2 fix pass: 1H + 1L, all resolved.** **H-1** — KD-1 derived nationality from `(worldSeed, playerId)`, but **`PlayerId` is not stable**: #31’s KD-7 re-keys it on a transfer, which is precisely why #44 must *migrate* bans across it and #32 must *drop* knowledge at it. The derivation would therefore have **changed a player’s nationality when he transferred** — the most common event in a career — with nothing to detect it, since both values are correct derivations of their own keys. Resolved by making `NationOf` a **pin-then-derive** lookup: a small #36-owned `NationPin` table written only on a re-key, by the same FR-TX-022 hook #44 uses for bans, bounded by transfer volume rather than pool size. KD-1’s actual property survives intact — still no `PlayerRecord` field, no draw, no `FIELDS_PER_PLAYER` change, no golden-vector rebaseline. The alternative (find a transfer-invariant key) is rejected: **none exists** — #28’s lifecycle overlay is itself `PlayerId`-keyed, so a global immutable identity would be a far larger #27 change than the field KD-1 declines to add. The pin also **subsumes** the #47 authoring override: one table, not two. **L-1** — §9 gained the transfer lock (the test a naive implementation fails) ahead of the golden-vector one, and §5 gained the pin’s own drop-on-retire rule so the table cannot outlive its pool. |
| v0.4 | July 26, 2026 | **AR-3 fix pass: 0H + 2M, both regressions from the AR-2 fix.** **M-1** — AR-2 added the `NationPin` table to §5 but **not to KD-6’s enumeration of the sub-blob’s contents**, so an implementer following KD-6 would not serialize it — and a transferred player’s nationality would revert to the derivation of his new id on the next load, which is exactly the defect AR-2 introduced the pin to prevent. **M-2** — KD-7 still claimed nationality is *“derived and read by nothing”*, which contradicts §3: eligibility **is** the minimal tier, so it is read there. Replaced with the true and stronger claim — reading it moves no byte outside #36. |
| v0.5 | July 26, 2026 | **AR-4 fix pass: 0H + 2M, both stale against the AR-2 fix.** **M-1** — §9 still justified the save/restore test with *“it must be, since nothing is stored”*, which stopped being true when AR-2 introduced the pin table; the unpinned case is the free one and the **pinned** case is the one that can regress, so the test now names both. **M-2** — §8.2 still deferred a nationality *“override table”* to #47 and §11 R-2 called it a standing option, but AR-2 already ships that table at approval for re-keys: #47 adds no #36 surface, and what it actually needs is a precedence policy between an authored edit and a re-key pin — recorded as its decision, cheap only because the surface is one table rather than two. |
| v0.6 | July 26, 2026 | **AR-5 sweep: 0H + 0M + 2L, both resolved — CONVERGENCE** (an L-only round closes the cycle). **L-1** — §6’s determinism bullet still read *“no stored copy to drift”*, the last of four places stale against AR-2’s pin table; restated so both paths (unpinned = nothing stored, pinned = exactly one entry written at a re-key) are covered, and so it is clear the pin is *why* the derivation alone is insufficient. **L-2** — §7’s surface table had no writer for `NationPin`: the mechanism was fully described in KD-1 but the one surface that creates the state fixing AR-2’s High was missing from the list. Added `OnPlayerReKeyed`, which also migrates the `CallUp`. |
