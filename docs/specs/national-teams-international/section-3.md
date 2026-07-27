# National Teams & International Management #36 — Section 3: Algorithms

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

All arithmetic is **integer** (FR-NT-005), and **no formula below makes a stochastic draw at any tier
#36 owns** (FR-NT-029) — tournament draws are #43's, and selection is a deterministic ranking.

## 3.1 `NationOf` — pin-then-derive (FM-NT-01)

The function the whole spec rests on, and the one a naive implementation gets wrong on the most common
event in a career.

```
NationOf(int playerId) -> NationId:
    if pins.TryGet(playerId, out NationId pinned):
        RequireDefined(pinned)                       # F1 -- a corrupt pin fails loud, never silently derives
        return pinned                                # authored (#47) or re-key pin (FR-NT-010)
    return Derive(worldSeed, playerId)

Derive(ulong worldSeed, int playerId) -> NationId:
    z := SplitMix64Step(worldSeed ^ NT_NATION_SALT)
    z := SplitMix64Step(z ^ (ulong)(uint)playerId)
    r := (int)(z % (ulong)NT_WEIGHT_TOTAL)           # NT_WEIGHT_TOTAL = sum of the [GT] weights
    acc := 0
    foreach (nation, weight) in NationCatalogue:     # ordinal order -- APPEND-only (FR-NT-009)
        acc := acc + weight
        if r < acc:  return nation
    throw                                            # unreachable while the weights sum to the total
```

**This is a keyed mix, not a draw.** It reads no cursor, advances no stream, and is a **pure function of
its arguments** — the #32 fog-of-war pattern applied to a much larger problem. Nothing is serialized for
an unpinned player, which is the overwhelming majority at every moment of every career.

**The catalogue walk is inverse-transform over a `[GT]` weighting**, in **ordinal order**. Both halves
matter: the weighting is what lets a league be predominantly one nation with a realistic minority spread,
and the ordinal order is why FR-NT-009 makes the catalogue APPEND-only — **inserting a member shifts every
subsequent nation's acceptance band and re-nationalises the entire world**, in every existing career, with
no version gate to catch it.

**The final `throw` is an internal-invariant abort, not a runtime branch.** `NT_WEIGHT_TOTAL` is
`[DERIVED]` from the catalogue itself (Appendix A.2), so the loop always terminates inside — unless a
maintainer sets the total independently of the weights, which is exactly what the `[DERIVED]` tag forbids
and what the throw catches.

### 3.1.1 Why the derivation alone is insufficient

**`PlayerId` is not stable.** #31's KD-7 re-keys the club-scoped `PlayerId` on a transfer — which is why
#44 must *migrate* bans across it and #32 must *drop* knowledge at it. So a nationality derived from
`(worldSeed, playerId)` alone **changes when a player transfers**: a Brazilian signs for a new club and
becomes Italian.

**Nothing would detect it.** Both values are correct derivations of their respective keys, both are stable
across save/restore, and both pass every determinism test that does not involve a transfer. That is what
makes it a design defect rather than a bug — it is invisible to the entire class of test a determinism
spec naturally writes.

## 3.2 `OnPlayerReKeyed` — the only writer of a pin (FM-NT-02)

```
OnPlayerReKeyed(int oldPlayerId, int newPlayerId):
    # ORDER IS LOAD-BEARING: resolve BEFORE the old id becomes unresolvable (F4).
    nation := NationOf(oldPlayerId)                  # pin-then-derive on the OLD key
    RequireDefined(nation)                           # F4 -- refuse rather than pin a wrong value
    pins.Set(newPlayerId, nation)                    # FR-NT-012: stored even if == Derive(newPlayerId)
    pins.Remove(oldPlayerId)                         # the old key is dead
    MigrateCallUps(oldPlayerId, newPlayerId)         # a call-up MIGRATES (FR-NT-023)
    MigrateMinutes(oldPlayerId, newPlayerId)
```

Invoked from **#31's FR-TX-022 roster-move hook** — the same hook #44 uses to migrate bans. #36 does not
reference #31; the hook calls in.

**The ordering is the whole of F4.** Resolving *after* the re-key would pin the **post**-transfer
derivation — silently recording the wrong nationality **via the very mechanism meant to prevent it**. A
test that merely asserts "a pin exists after a transfer" passes against that bug; §5.1's transfer lock
asserts the *value*.

**A pin equal to its derivation is still written** (FR-NT-012). Skipping it is the obvious optimisation
and it is wrong: the pin's job is to survive a key change the derivation cannot, and a
`newPlayerId` whose derivation happens to coincide today will not coincide after the **next** transfer.

**`CallUp` migrates, `NationPin` migrates, and both for the same reason** — they are live facts about a
**person**, not stale facts about a squad slot. This is #44's ban rule, not #32's knowledge-drop rule, and
§5.4 asserts the contrast directly so a later "consistency" pass cannot unify them the wrong way.

## 3.3 `IsWindowDay` / `CurrentWindow` — the calendar derivation (FM-NT-03)

```
CurrentWindow(uint worldDay) -> (int index, uint openDay, uint closeDay) | none:
    foreach (i, w) in DeriveWindows(seasonCalendar):     # pure, read-only over #30's calendar
        if w.OpenDay <= worldDay and worldDay <= w.CloseDay:  return (i, w.OpenDay, w.CloseDay)
    return none

IsWindowDay(uint worldDay) -> bool  =  CurrentWindow(worldDay) != none
```

**#36 never writes the calendar** (FR-NT-015). `DeriveWindows` reads `SeasonCalendar` and returns
positions; it inserts no day, reorders no fixture, and holds no copy. This is the #31 FR-TX-019 precedent
verbatim — *"derived deterministically from #30's `SeasonCalendar` (read-only); #31 MUST NOT mutate the
calendar"* — with #36's window standing in the same relation.

**Advance and the F5 guard:**

```
AdvanceWindowDay(uint worldDay):
    if cursor.LastAdvancedWorldDay != NT_NOT_ADVANCED_SENTINEL:
        if worldDay == cursor.LastAdvancedWorldDay:      return       # no-op
        if worldDay != cursor.LastAdvancedWorldDay + 1:  throw        # day gap -- F5
    ... open/close transitions ...
    cursor.LastAdvancedWorldDay := worldDay                            # stamp LAST
```

**#36 needs this guard where #53 does not**, and the reason is specific: a window **open** is a
transition with a consequence (call-ups are selected, players are withdrawn), so skipping days would
select call-ups **on the wrong day** or skip a window entirely. The sentinel is `uint.MaxValue`, **not
`0`** — day `0` is a legal world day, and a `0` sentinel silently no-ops a day-0 advance instead of
failing (#33 FR-HS-008).

## 3.4 `SelectCallUps` — the draw-free ranking (FM-NT-04)

```
SelectCallUps(uint worldDay):
    RequireWindowOpen(worldDay)
    Clear(callUps)
    foreach nationTeamId in ActiveNationTeams():
        eligible := [ p in ManagedPool() where NationOf(p.PlayerId) == NationOfTeam(nationTeamId) ]
        # Deterministic ranking -- no RNG (FR-NT-021), the LineupSelector model.
        Sort(eligible, by: MeanAttributes desc, then: PlayerId asc)     # total order, tie-free

        perClub := {}
        foreach p in eligible:
            if Count(callUps, nationTeamId) >= NT_SQUAD_SIZE:            break
            if perClub[p.ClubId] >= NT_MAX_CALLUPS_PER_CLUB:             continue   # FR-NT-018
            Append(callUps, new CallUp { nationTeamId, p.PlayerId, worldDay })
            perClub[p.ClubId] += 1
    SortCanonical(callUps)                                              # FR-NT-024
```

**The `PlayerId` tie-break is what makes the ranking total.** Mean attributes tie constantly across a
generated league — every roster is drawn from one distribution — so without a deterministic tie-break the
selection would depend on enumeration order, which is exactly the kind of dependency that survives every
same-process test and breaks across a restore.

**The per-club cap is applied during the walk, not after** (FR-NT-018). Applying it afterwards — select
the best 23, then trim clubs over the cap — yields a *different squad* and, worse, a squad that depends on
the trim order. Capping inside the greedy walk means the next-best eligible player takes the place, which
is both the intended behaviour and order-free.

**Selection is idempotent within a window**: re-running it on the same `worldDay` clears and rebuilds from
the same inputs, producing the identical canonical list.

## 3.5 `FilterAvailable` — the withdrawal (FM-NT-05)

```
FilterAvailable(in Squad squad, uint worldDay) -> Squad:
    if not IsWindowDay(worldDay):  return squad                 # identity outside a window
    out := []
    foreach p in squad.Players:                                 # a VALUE-COPY reduction (FR-NT-016)
        if IsCalledUp(p.PlayerId):  continue                    # a pure REMOVAL (FR-NT-017)
        out.Append(p)
    return new Squad(squad.ClubId, out)
```

**A pure removal, which is what makes composition with #44's filter order-independent.** Filtering is set
subtraction, so *suspended ∪ called-up* is the same set whichever runs first, and neither filter reads the
other's output:

```
Filter44(Filter36(S)) = S \ (suspended ∪ calledUp) = Filter36(Filter44(S))
```

**This is a property, not an accident** — and it holds **only** while both filters remove. The moment a
future filter *adds* or *substitutes* a player, the seam stops being order-free and needs an explicit
order. ERR-030-016 files that note against the seam, where it belongs.

**The empty-squad floor is not resolved here** (FR-NT-019 / F7). Two independent filters can between them
reduce a squad below a fieldable eleven, and `LineupSelector` fails loud on an unfillable starter line.
#36 bounds its **own** contribution with `NT_MAX_CALLUPS_PER_CLUB` and no more; inventing a private policy
would be how the two filters end up disagreeing.

## 3.6 `TryResolveNationSquad` — the deep-tier seam (FM-NT-06, deferred)

```
TryResolveNationSquad(int nationTeamId, out Squad squad) -> bool:
    RequireNationRange(nationTeamId)                            # F2 -- >= NATION_TEAM_ID_BASE
    ids := [ c.PlayerId for c in callUps where c.NationTeamId == nationTeamId ]   # canonical order
    if ids.IsEmpty:  squad := default; return false
    squad := new Squad(nationTeamId, [ pool.Resolve(id) for id in ids ])          # a VIEW over #27
    return true
```

**#36 does not implement `ISquadProvider`** (FR-NT-004). That type is declared in `src/match-engine/`, so
implementing it would make #36 reference `TacticalDirector.MatchEngine` — collapsing the leaf DAG for one
method signature, and forcing §5.7's structural assertion down to *"true at the minimal tier only"*, which
is the class of erosion that makes a DAG claim worthless.

Instead the **root** composes:

```
# root-side; the root already references both League and #36
class CompositeSquadProvider : ISquadProvider {
    Squad ResolveByClubId(int id) =>
        id >= NATION_TEAM_ID_BASE
            ? (nationalTeams.TryResolveNationSquad(id, out var s) ? s : throw)
            : league.ResolveByClubId(id);
}
```

**#30 still sees exactly one provider and needs no branch.** The `League`-is-a-provider precedent applies
— to the **composite**, which is the thing #30 actually holds. `League` lives in `season-save`, which
already references `match-engine`; **#36 does not and should not.**

**The squad is a view, never a copy** (FR-NT-022): only the `PlayerId` list is stored, and the records are
resolved from #27's pool at read time. That is what keeps #36 free of a second truth about a player.

## 3.7 Arithmetic convention (pinned)

Every expression above is exact integer arithmetic — comparison, addition, array indexing, and one
`ulong` modulo inside `Derive`. **#36 performs no signed division and no rounding**, so no rounding
convention arises and none may be introduced without a spec change: `Math.Round` operates on `double` and
would violate FR-NT-005 outright.

## 3.8 Worked examples (hand-verifiable)

At `NT_SQUAD_SIZE = 23`, `NT_MAX_CALLUPS_PER_CLUB = 3`, `NT_NOT_ADVANCED_SENTINEL = uint.MaxValue`,
`NATION_TEAM_ID_BASE = 100000`, and a three-member catalogue weighted `{A: 70, B: 20, C: 10}`
(`NT_WEIGHT_TOTAL = 100`).

| # | Setup | Working | Result |
|---|---|---|---|
| (a) | Unpinned player, `r = 12` | `12 < 70` | `A` — no pin stored, nothing serialized |
| (b) | Unpinned player, `r = 85` | `85 ≮ 70`; `85 < 90` | `B` |
| (c) | Unpinned player, `r = 95` | `95 ≮ 90`; `95 < 100` | `C` |
| (d) | Catalogue gains a member **before** `B` | every band above it shifts | **every unpinned player in every career is re-nationalised** — why FR-NT-009 is APPEND-only |
| (e) | Player 41 derives `A`; transferred, re-keyed to 907 whose derivation is `C` | `OnPlayerReKeyed` resolves on the **old** id | pin `907 → A`. **`NationOf(907) == A`** — the transfer lock |
| (f) | (e) but the hook resolves **after** the re-key | `NationOf(907)` derives `C` | pin `907 → C` — **the silent wrong answer F4 exists to prevent** |
| (g) | Player whose new id derives the same nation as the pin | `Derive(newId) == A` | pin **still written** (FR-NT-012) — the "redundant" pin the next transfer needs |
| (h) | Retired player with a pin | boundary churn | pin **dropped** (FR-NT-013) — the table cannot outlive its pool |
| (i) | 5 eligible players from one club, cap 3 | capped **during** the walk | 3 taken, the next-best from **other** clubs fill the rest — not a post-hoc trim |
| (j) | Two eligible players with identical mean attributes | `PlayerId` asc | the lower id — **tie-free**, so enumeration order cannot matter |
| (k) | `AdvanceWindowDay(200)` twice | second: `worldDay == LastAdvanced` | **no-op** |
| (l) | `AdvanceWindowDay(200)` then `(210)` | `210 != 201` | **throws** (F5) — unlike #53, a gap here **skips a window** |
| (m) | Squad of 20; #44 suspends `{3, 7}`, #36 calls up `{7, 11}` | `S \ ({3,7} ∪ {7,11})` | 17 players — **identical in either filter order** (§3.5) |
| (n) | `TryResolveNationSquad(99999)` | `< NATION_TEAM_ID_BASE` | **throws** (F2) — it would have routed to `League` |
| (o) | `TryResolveNationSquad(100003)` with no call-ups | empty id list | **`false`** — a named legal state, not a throw |

Examples (e)/(f) are the pair the whole KD-1 design exists for, and (f) is what a test asserting only
*"a pin exists"* would miss. Example (d) is the one that makes the catalogue's APPEND-only contract a
save-correctness rule rather than a style rule.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §3 (FM-NT-01..06: pin-then-derive with the inverse-transform catalogue walk, the re-key hook with its load-bearing ordering, the calendar derivation and its guard, the draw-free capped ranking, the pure-removal filter with its order-independence proof, the deferred squad resolution and the root composite; arithmetic convention; fifteen worked examples). The `PlayerId` tie-break and the cap-during-the-walk are both argued rather than stated — the first because mean attributes tie constantly in a generated league, the second because a post-hoc trim yields a different and order-dependent squad. Status IN REVIEW. |
#endregion
