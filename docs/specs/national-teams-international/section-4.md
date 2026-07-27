# National Teams & International Management #36 — Section 4: Architecture

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 4.1 Assembly and reference direction

New assembly **`TacticalDirector.NationalTeams`** at `src/national-teams/`, referencing **only**
`TacticalDirector.PlayerDatabase` (#27) — at **every** tier.

```
root ──▶ {#30, #43, #36, #44, …}
  │                │
  │                └──▶ #36 ──▶ {#27}          (a leaf, at minimal AND deep)
  │
  └──▶ CompositeSquadProvider ──▶ {League, #36, match-engine}
```

**Acyclic, and #36 is a leaf over #27.** It does not reference #30 (the calendar arrives as a value and
the filter is invoked *by* the seam), #43 (the root registers instances), #44, #29, #41, `SeasonSave`, or
`MatchEngine`.

**The deep tier does not weaken this — which it would have, silently, under the obvious design.** Making
#36's registry an `ISquadProvider` is the natural move, and the `League`-is-a-provider precedent appears
to endorse it. But `ISquadProvider` is declared in `src/match-engine/`, so #36 would have taken a
`MatchEngine` reference **for one method signature**, and §5.7's structural assertion would have had to be
weakened to *"true at the minimal tier only"* — the class of erosion that makes a DAG claim worthless.

The precedent still applies, to the **composite at the root** (§4.4), which is the thing #30 actually
holds. What does not transfer is the assumption that every squad source should implement the interface:
`League` lives in `season-save`, which **already** references `match-engine`; #36 does not and should not.

**CS0104 pre-check.** #36 introduces `NationId`, `NationCatalogue`, `CallUp`, `WindowCursor`,
`IntlMinutes`, `NationPin`, `NationalTeamStore`, `NationalTeamSaveCodec`, `CallUpSelector`. Each was
checked against every name that could be in scope with it before authoring, because this project has hit
CS0104 twice (`TacticTranslation`, `PlayerAttributes`). None collides — note in particular that `Squad`
is **#27's**, consumed and never re-declared.

## 4.2 File layout

```
src/national-teams/
├── NationalTeamConstants.cs      # the Appendix A catalogue — no magic numbers in formula code
├── NationId.cs                   # the APPEND-only nation roster (FR-NT-009)
├── NationCatalogue.cs            # the [GT] weighting + NT_WEIGHT_TOTAL derivation
├── Nationality.cs                # FM-NT-01 — pin-then-derive; pure, no state beyond the pin table
├── NationPin.cs                  # the re-key / authoring pin
├── CallUp.cs                     # the SELECTION record (ids, never PlayerRecords)
├── WindowCursor.cs               # the F5 guard's state
├── IntlMinutes.cs                # deep tier; empty at minimal
├── InternationalWindow.cs        # FM-NT-03 — the read-only SeasonCalendar derivation
├── CallUpSelector.cs             # FM-NT-04 — the draw-free capped ranking
├── NationalTeamStore.cs          # the SINGLE writer; FilterAvailable (FM-NT-05); the re-key hook
├── NationSquadResolver.cs        # FM-NT-06 — deep tier ONLY; absent at minimal (FR-LW-031)
├── NationalTeamSaveCodec.cs      # KD-6 sub-blob, version gate first
└── tests/
```

**`NationSquadResolver.cs` is not created at the minimal tier** — at minimal no international match is
played, so a resolver with no consumer is the phantom surface FR-LW-031 forbids.

**`CompositeSquadProvider` is deliberately absent from this tree.** It references both `#36` and
`match-engine`, so it lives at the **root** — the same layering that puts #46's projectors and #49's
boundary adapters there. Placing it here would break FR-NT-003 (§4.1).

**`NationCatalogue.cs` is separate from `NationId.cs` on purpose.** The enum is the ordinal contract
(FR-NT-009); the catalogue is the `[GT]` weighting over it (FR-NT-014). Keeping them apart means a
balance edit to the weights never touches the file whose ordinals are save-load-bearing.

## 4.3 The #30 seams (the caller side)

#36 attaches to **two** existing #30 surfaces and introduces **neither**:

```
# (1) THE AVAILABILITY FILTER — inside #30's resolve -> FILTER -> configure seam (FR-SN-013)
squad = suspensions.FilterAvailable(squad, worldDay);      # #44 — already specified
squad = nationalTeams.FilterAvailable(squad, worldDay);    # #36 — the SECOND consumer

# (2) THE WINDOW ADVANCE — at #36's own position in RunWorldTickInFixedOrder
nationalTeams.AdvanceWindowDay(worldDay);
```

**Seam (1) needs no new #30 surface**, which is the point of KD-2: a called-up player is exactly a player
reduced out of a squad for a fixture, which is the shape #44's filter already has. **The order shown is
arbitrary and provably so** (§3.5) — both are removals, so the composition is set subtraction.

**What ERR-030-016 files is the contract, not the seam.** The seam exists; what does not exist is a
written statement that it admits **more than one** consumer, that the current two compose
order-independently **because both are removals**, and that a future *non-removal* filter would need an
explicit order. Without that note, the order-freedom is an accident that a third filter silently breaks.

**The empty-squad floor is named in the same back-prop** and is deliberately **not** #36's to resolve
(FR-NT-019 / F7). It is a property of the **composition**, so if it is not resolved at the seam each
filter grows its own guard and the two disagree.

**Provenance is enforced at #30's call seam, not inside #36.** #36 cannot verify that the squad it is
handed is the right club's, only that its own call-ups are excluded from it — the same division of
responsibility #33 uses for its committed inputs, and what keeps #36 free of a #30 reference.

## 4.4 The composite `ISquadProvider` (deep tier, KD-3)

```
# root-side — the root already references League, #36, and match-engine
sealed class CompositeSquadProvider : ISquadProvider
{
    Squad ResolveByClubId(int id) =>
        id >= NATION_TEAM_ID_BASE
            ? (nationalTeams.TryResolveNationSquad(id, out var s)
                 ? s
                 : throw new InvalidOperationException($"no squad for national team {id}"))
            : league.ResolveByClubId(id);
}
```

**#30 holds exactly one provider and needs no branch.** The routing is a single comparison against a
`[FIXED]` base, and it is total: every id is either in the national range or below it.

**The disjoint id range is what makes the composite safe** (FR-NT-026 / F2). If national ids could
collide with `ClubId`s, the composite would route a national team to `League` — which would either throw
or, worse, resolve some club's squad for an international fixture. F2 makes an out-of-range
`nationTeamId` fail loud **inside #36** as well, so the guard exists on both sides of the seam rather than
only at the router.

## 4.5 Save composition (KD-6)

#36's sub-blob is composed into #30's `SeasonSaveCodec` alongside #40's, #33's, #44's, #43's and the rest,
as a length-prefixed **opaque** block: the outer codec never parses it, so
`NATIONAL_TEAM_SAVE_FORMAT_VERSION` and `SEASON_SAVE_FORMAT_VERSION` move independently (FR-NT-031).
Layout in Appendix B.

**#36 changes no existing spec's serialized representation.** It adds no `PlayerRecord` field (KD-1), no
`SEASON_STATE_FORMAT_VERSION` bump, and — critically — **no change to any `RosterGenerator` digest**.
#36's landing is purely additive to the save layer, and that is a stronger statement than it sounds:
because club rosters are **regenerated from the world seed rather than saved**, a spec that touched the
generation path would rewrite every existing career's rosters (§1.4(b)).

**Tournament and bracket state is #43's sub-blob, not #36's** (KD-6). #36 stores who was called up, never
what competition they played in.

**Migration posture at T2: none — pre-T2 saves are rejected fail-loud.** The living-world slice-2
precedent; cross-version migration is **#50's** subject.

## 4.6 Contracts with neighbours

| Neighbour | Contract |
|---|---|
| **#27** | Read-only: `PlayerId`, `PlayerRecord`, `PlayerAttributes`, `Squad`. **Nothing is added, changed, or written** — no field, no draw, no `FIELDS_PER_PLAYER` change, no golden-vector rebaseline (FR-NT-001/002). The **entire** KD-1 design exists to make this true. |
| **#30** | Invokes #36's filter at the existing FR-SN-013 seam and its window advance at #36's tick position. The calendar arrives as a **value**; #36 never mutates it. #36 references #30 never. |
| **#44** | **No reference in either direction.** Both are pure-removal filters at one seam, composing order-independently (§3.5). The composition note is filed against **#30**, where the seam lives — not against #44. |
| **#43** | **No reference in either direction.** The **root** registers international instances; #36 supplies entrant ids from a disjoint range and (deep tier) squads. **#43 needs no change**: its entrant sets are plain `int`s and `FixtureScheduler` is id-agnostic. |
| **#31** | Calls **in** through the FR-TX-022 roster-move hook (the same one #44 uses for bans). #36 references #31 never. |
| **#29 / #41** | *(Deep tier.)* Receive international minutes as **routed committed integers**. No reference either way, and the routing is **not built until minutes exist** (FR-NT-028). |
| **#47** | Authored nationalities land in the **`NationPin` table #36 already ships** for re-keys — one surface, not two. #47 owns the authored-vs-pin precedence policy (§7.4 R-2). |
| **#16** | **Untouched.** `_RESERVED_0x28_` / `SubsystemOrdinals 90` already exists and is already correct for a draw-free spec — nothing to file, and possibly nothing ever to promote. |
| **#50** | Registers `NATIONAL_TEAM_SAVE_FORMAT_VERSION` in the version registry. |

**Standing review item:** #36 performs **no** write to `PlayerRecord`, `Squad`, `SeasonCalendar`, or any
#43/#44 state. The reference graph proves most of this — #36 references only #27 — but **#27's types are
reachable**, so the no-write property against `PlayerRecord` and `Squad` specifically cannot be inferred
and is asserted behaviourally in §5.7.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §4 (leaf-at-every-tier assembly with the `ISquadProvider` trap spelled out, file layout with its three deliberate absences, the two existing #30 seams and what ERR-030-016 actually files, the root composite and why the disjoint id range makes it safe, save composition with the regenerated-rosters point, neighbour contracts). The standing review item is scoped to `PlayerRecord`/`Squad` specifically — the one thing #36's own reference graph cannot prove, since #27 is legitimately referenced. Status IN REVIEW. |
#endregion
