# New-Game Setup & Database Editor #47 — Section 3: Algorithms

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

#47 has fewer algorithms than any spec in the wave, and that is the design working: the validation is
#27's, the generation is `LeagueBootstrap`'s, the construction is `season-save`'s, and the layout is
#38's. What remains is a **writer**, a **handoff**, a **construction path**, and a **precedence rule**.

**Nothing below makes a stochastic draw** (FR-ED-029). The `worldSeed` is an input #47 collects and hands
on; every draw made from it belongs to #27/#30.

## 3.1 `SquadFileWriter.Write` — the missing half (FM-ED-01)

`SquadFileLoader` exposes exactly `Parse(string, int) → Squad` and has **no writer anywhere in the tree**
(§1.4(b)). This is it.

```
Write(Squad squad) -> string:                    # Squad is a sealed CLASS -- no 'in' modifier
    RequireNotNull(squad)
    sb := new StringBuilder()
    foreach p in squad.Players:                  # in ascending PlayerId -- canonical (FR-ED-014)
        sb.AppendLine($"[player {LocalIndexOf(p, squad.ClubId)}]")
        sb.AppendLine($"firstName = {p.FirstName}")
        sb.AppendLine($"lastName  = {p.LastName}")
        sb.AppendLine($"age       = {p.Age}")
        sb.AppendLine($"position  = {p.Position}")
        foreach (key, value) in AttributeKeysOf(p.Attributes):   # the loader's own key roster
            sb.AppendLine($"{key} = {value}")
    return sb.ToString()
```

**The correctness condition is a round-trip, not a review** (FR-ED-018):

```
for every Squad s that Parse accepts:    Parse(Write(s), s.ClubId) == s        # field-for-field
```

**That single property covers the encode/decode asymmetry class** this project has already been bitten by
— #30 T1's `SeasonState`, which was **constructible but not decodable**. It is also the test that caught
`SquadFileLoader`'s club-scoping defect at #27 T0, where the loader computed `PlayerId` from a raw
section-local index instead of the club-scoped formula.

**The loader's other historical defect is the argument for the lock.** Its `age` key was unbounded against
its own *"out-of-range int all throw"* contract, and that escaped to a **later adversarial review** rather
than to a test. A round-trip lock over a corpus that includes every gated boundary value is what turns
that class of finding from a review outcome into a build failure.

**`LocalIndexOf` is the one place the writer must not guess.** The loader's identity default computes
`PlayerId` from a **club-scoped** formula, not from the section index, so the writer must emit the index
that reproduces the player's actual id — which is exactly what the round-trip asserts, and exactly the
defect it caught before.

**The writer emits nothing outside the documented grammar** (FR-ED-020). In particular it does not rely on
the parser tolerating a construct the grammar does not specify: the grammar is explicitly *"NOT a
determinism-pinned wire format"* and is expected to be replaced, so a writer bound to parser **behaviour**
rather than to the **grammar** would break at the swap.

## 3.2 The setup handoff (FM-ED-02)

```
BuildConfig(ulong worldSeed, int clubCount, int managedClubId, bool hasAuthoredDb) -> NewGameConfig:
    return new NewGameConfig(worldSeed, clubCount, managedClubId, hasAuthoredDb)
    # NOTE: no validation. Every gate already exists downstream (FR-ED-022/023).
```

and, at the **root**, the two branches this config selects:

```
# root-side -- the root already references season-save, #47 and #30
StartNewGame(in NewGameConfig cfg, AuthoredDatabase authored /* null when !cfg.HasAuthoredDb */):
    League league = cfg.HasAuthoredDb
        ? SeasonSave.LeagueFactory.FromAuthored(authored.ToClubs(), authored.Squads)   # ERR-030-018
        : LeagueBootstrap.Generate(cfg.WorldSeed, cfg.ClubCount);                      # UNCHANGED
    var season = league.CreateSeason(cfg.ManagedClubId);
```

**#47 adds no gate**, and that is a decision rather than an omission (KD-3): `clubCount` is validated by
`LeagueBootstrap.Generate` — which fails loud with messages **naming the constant to change**, including
its name-catalogue and `MaxRngStreams` refusals — the managed club by `League.CreateSeason`, and
`worldSeed` by nothing, because **every `ulong` is valid**.

**Where #47 must explain a refusal, it surfaces the consumer's exception** (FR-ED-023). Pre-checking would
create the second authority KD-2 forbids in the validation case, and would drift the moment a downstream
bound moved.

**The generated branch is byte-identical to pre-#47** (FR-ED-012 / KD-7): the same call, the same
parameters, no sub-blob, no stream, no change to the draw budget or the golden vector. #47's entire
save-format footprint is **conditional on `HasAuthoredDb`**.

## 3.3 Authored-`League` construction (FM-ED-03)

The factory itself is **`season-save`'s** (ERR-030-018) — `League`'s constructor is `internal` there — but
its contract is #47's to state, because #47 supplies its inputs:

```
# in season-save, NOT in #47
FromAuthored(Club[] clubs, Squad[] squads) -> League:
    RequireAscendingUnique(clubs, by: ClubId)                     # F5
    RequireAscendingUnique(squads, by: ClubId)
    RequireOneSquadPerClub(clubs, squads)
    foreach c in clubs:  Require(c.StrengthDelta == 0)            # FR-ED-009 -- NO ramp is applied
    return new League(clubs, squads)                              # internal ctor, same invariants
```

**Three properties, each load-bearing:**

- **Source, not patch** (FR-ED-007). Generation **does not run** for an authored game. The rejected
  alternative — generate then overlay — would run the generator only to discard 100% of it at database
  scale, **and** would make the authored result depend on the generator's **draw order**, re-coupling
  authored data to the very thing the golden vector exists to freeze.
- **Source-agnostic downstream** (FR-ED-008). Everything below already talks to `League` through
  `ISquadProvider` and `CreateSeason`, so nothing downstream branches on origin.
- **No strength ramp** (FR-ED-009). `Club.StrengthDelta` is the seeded ramp that stops a *generated* table
  being *"20 statistically identical teams"*. An authored database specifies attributes **directly**, so
  applying a ramp on top would **silently re-tune every authored player away from what the author typed**.
  The guard is in the factory, so an authored club carrying a non-zero delta fails loud rather than being
  quietly accepted.

**#47 does not call this.** It produces the values; the **root** calls the factory (FR-ED-003). Had #47
constructed the `League` itself it would have needed a `season-save` reference — and `season-save`
references `MatchEngine` and `LivingWorld`, so **an editor would transitively depend on the whole
simulation to author a text file**.

## 3.4 Nationality-pin precedence (FM-ED-04)

#36 makes nationality a **pin-then-derive** lookup and states that *"#47's authoring lands in this same
table — an authored entry is a pin like any other."* It also names the one thing #47 must decide.

```
# The rule, stated as an ordering over writes to #36's table -- NOT a new mechanism.
authored pin      : written once, at world genesis, from the AuthoredDatabase
re-key pin        : written on every #31 transfer, by #36's OnPlayerReKeyed

PRECEDENCE: a later re-key pin OVERWRITES an authored pin for the same PlayerId.
```

**Why that direction** (FR-ED-026): the re-key is a **live event about a player who has moved**, while the
authored value described his **starting** state. Preserving the authored pin across a transfer would mean
an authored nationality that the transfer machinery cannot correct — and #36's pin exists precisely so
nationality survives a re-key with the *right* value.

**Note what this is not.** It is not a merge, a priority field, or a second table: it is a plain
last-write-wins over #36's existing table, which is why **#36 needs no new surface at all** and #47 files
nothing against it. The authored entry and the re-key entry are the same kind of row.

**The pins are the one place #47 uses an overlay rather than a source** (KD-1), and the distinction is
worth stating: nationality is a **sparse** fact over a pool that is otherwise derived, so an overlay is
right. A roster is a **whole-database replacement**, so an overlay is wrong. Same spec, two shapes, chosen
by the density of what is being authored.

## 3.5 Arithmetic and encoding conventions (pinned)

#47 performs **no arithmetic** beyond index bookkeeping — no scaling, no division, no rounding, and no
float anywhere. There is consequently no rounding convention to pin, and none may be introduced: any
future computation over authored values would belong to the consumer that interprets them, not to the
editor that records them.

**Text encoding is the loader's**, not the writer's to redefine. `Write` emits what `Parse` reads, and the
round-trip is the only statement of what that means (FR-ED-018/020).

## 3.6 Worked examples (hand-verifiable)

| # | Setup | Working | Result |
|---|---|---|---|
| (a) | `Write` a 25-player squad, then `Parse` it back | field-for-field compare | **identical** — the FR-ED-018 contract |
| (b) | A squad whose `age` sits at the loader's boundary | round-trip over a boundary corpus | identical — the case the loader's own `age` defect escaped review on |
| (c) | A writer that emits section-local indices instead of club-scoped ones | `Parse` reconstructs different `PlayerId`s | **round-trip fails** — the exact #27 T0 defect (§3.1) |
| (d) | `Parse` rejects an authored value; an editor check had accepted it | commit goes through `Parse` regardless | **fails loud** (F1); the check is the bug (F2), not a second gate |
| (e) | `clubCount = 1` | `LeagueBootstrap.Generate` | **throws from the consumer**, surfaced by #47 — #47 pre-checks nothing (F4) |
| (f) | `worldSeed = 0` | no gate | **accepted** — every `ulong` is valid |
| (g) | Generated game, saved | `HasAuthoredDb = false` | **no authored sub-blob at all** — not an empty one (FR-ED-012) |
| (h) | Authored game, saved, then loaded **with the source file deleted** | the sub-blob carries the rosters | **loads correctly** — the self-containment the rejected hash-reference design would have failed (FR-ED-013) |
| (i) | Authored save whose sub-blob is missing | F7 | **throws** — never a silent fall back to generation, which would load a *wrong world* that looks merely odd |
| (j) | Authored club with `StrengthDelta = 3` | the factory's guard | **throws** (§3.3) — no ramp is applied, and a stray delta is not quietly accepted |
| (k) | Authored clubs written in descending `ClubId` | canonical-order gate | **throws** (F5) — two equivalent databases must not serialize differently |
| (l) | Player authored as Brazilian, then transferred | re-key pin overwrites | **the re-key pin wins** (FR-ED-026 / §3.4) |
| (m) | Authored club name "Deportivo", two display locales | stored as authored, routed as a slot value | **"Deportivo" in both** — correct for a proper noun (KD-6) |

Examples (c), (i) and (j) are the three a plausible implementation gets wrong: guessing the index formula,
falling back to generation, and applying the ramp uniformly to both origins.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §3 (FM-ED-01..04: the writer and its round-trip contract, the setup handoff with its two root branches, the authored-`League` factory contract, the pin-precedence rule; the §3.5 no-arithmetic note; thirteen worked examples). The `LocalIndexOf` hazard is called out explicitly — the loader's identity default is club-scoped, and guessing it is the defect the round-trip caught at #27 T0. §3.4 states the sparse-overlay-vs-whole-database distinction, which is why the same spec uses two different shapes for pins and rosters. Status IN REVIEW. |
#endregion
