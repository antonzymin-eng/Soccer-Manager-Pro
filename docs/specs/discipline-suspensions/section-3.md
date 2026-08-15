# Discipline & Suspensions #44 — Section 3: Core Algorithms

**Created:** July 24, 2026
**Last Updated:** August 15, 2026, later still (v0.10 — M27, the spec half of #44's adversarial-review
round 4 (`open-issues.md`): §3.1's normative fold pseudocode showed each tap record calling `AddYellow`/
`AddBan` directly, with no buffer and no `Commit` — an implementer following it verbatim would reproduce
the pre-M13 half-fixture defect (`CardLedgerFold.cs` v1.2) that lets a bad `[GT]` leave cards `0..k-1`
applied and the rest silently lost. Rewritten to show `ObserveTick` only ever buffering
`(PlayerId, CardKind)` pairs and a separate `Commit(rules)` that validates every bound `[GT]` up front
(F6) and then applies the whole buffered list atomically, exactly matching `CardLedgerFold`'s real
`ObserveTick`/`Commit`/`CommitWithExplicitConfig` shape; section header retitled to cite FR-DC-010)
**Last Updated (prior):** August 15, 2026 (v0.9 — ERR-044-003 stage 1, owner decision: §3.3's `OnClubFixturePlayed`
pseudocode now takes `fieldedPlayerIds` and skips decrementing any player who appears in it, with a new
note explaining why — a banned player fielded through #30 §2.3 F9's extremis back-fill was previously
serving his ban for free; the ordering paragraph and FR-DC-011 citation updated to match)
**Last Updated (prior):** August 13, 2026, yet later still again (v0.8 — M20, a fifth adversarial-review pass
over the #44 C1/C2 landing: §3.1's occupancy-fold pseudocode still showed the pre-M4 order — kind-2
adding the yellow before the ban, both bans unguarded — one landing after L13 added the identical F6
guards to §3.2's `AddYellow` and stopped short of §3.1. Extends L13's ERR record rather than a new id)
**Last Updated (prior):** August 13, 2026, yet later still (v0.7 — L13, a third adversarial-review pass over
the #44 C1/C2 landing: §3.2's `AddYellow` pseudocode gains the `RequireYellowThreshold`/
`RequireBanLength` **F6** guard calls (§2.3), which had no normative source at all despite being
enforced in production and unit-tested — the AR pass 9 #29/#41 F8 precedent for this omission class)
**Last Updated (prior):** August 13, 2026, later still (v0.6 — ERR-044-005 back-prop, owed by the #44 C1/C2
adversarial review: §3.3's `FilterAvailable` pseudocode gains the all-suspended-squad `null`-return
case and names `MarkSuspended`'s mask, consumed by #30's composed seam, as the actual production path)
**Last Updated (prior):** August 13, 2026, later same day (v0.5 — ERR-030-037, adversarial review over the
#44 C1/C2 landing (M7): §3.3 gains normative text for the WITHIN-fixture half of the off-by-one
contract — `OnClubFixturePlayed` MUST run before that same fixture's fold commits its cards — which
the prior text only implied by describing separate fixtures)
**Last Updated (prior):** August 13, 2026 (v0.4 — ERR-044-002 + ERR-044-003, C1/C2 landing back-prop: §3.3's
ordering paragraph re-scoped to both resolution paths, and the `FilterAvailable` pseudocode comment
points its viability rule at #30 §2.3 F9 instead of a withdrawn F5)
**Last Updated (prior):** July 24, 2026 (v0.3 — cross-set AR pass 3; prior v0.2 PASS-1, v0.1 initial)
**Version:** 0.10
**Status:** APPROVED

---

## 3.1 The occupancy fold (FR-DC-002/004/005/006/010)

**Buffer, then commit once, atomically, at fixture resolution** (FR-DC-010: "the fold MUST complete
at fixture resolution"). Writing each record straight through to `DisciplineRules` as it arrives
would put half a fixture's cards into persisted state at any moment a save could be taken
mid-fixture, and would let a bad `[GT]` (F6) throw with cards `0..k-1` already applied and card `k`
onward silently discarded by the caller — the shape `CardLedgerFold.cs`'s own version history (v1.2,
M13) records finding and fixing in code, which an implementer following an earlier, buffer-free
draft of this section's pseudocode verbatim would have reproduced in a fresh implementation.
`ObserveTick` only ever buffers; nothing reaches `DisciplineRules` until `Commit` runs, and `Commit`
validates every `[GT]` it could throw on **before** applying the first buffered card, so a refusal
leaves the fold's buffer untouched and `DisciplineRules` unmodified — never applied-then-discarded.

```
CardLedgerFold(lineup /* slot -> PlayerId, incl. bench identities */):
    occupancy := lineup                                    # seeded by the root (F1 on any gap)
    pending := []                                           # buffered (PlayerId, CardKind) pairs
    committed := false                                       # Commit runs exactly once (FR-DC-010)

    ObserveTick(records):                                   # per tick, canonical publish order
        REQUIRE not committed
        for each record in records:
            switch record.ordinal:
                0x08 SubstitutionEvent:
                    occupancy.ApplySub(record.Outgoing, record.Incoming)  # occupant changes at this tick
                0x06 CardIssuedEvent:
                    pid := occupancy.OccupantAt(record.Recipient)          # F1 if unmapped
                    RequireKnownCardKind(record.CardKind)                  # F4 outside {0,1,2}
                    pending.Add((pid, record.CardKind))                    # BUFFERED — not applied yet
                else: ignore                                               # FR-DC-004 (unknown ordinals)

    Commit(rules):                                          # called ONCE, at fixture resolution
        REQUIRE not committed
        RequireCommittableConfig()   # F6 — validates ALL FOUR bound [GT]s up front, before any
                                      # buffered card is applied, so a bad config refuses the WHOLE
                                      # fixture atomically and never half of it (M13)
        for each (pid, kind) in pending, in buffered (= publish) order:
            switch kind:
                0: AddYellow(pid)                                  # first yellow (F6 inside, §3.2)
                2: ban := RequireBanLength(SECOND_YELLOW_BAN_MATCHES)   # already validated by
                   AddYellow(pid); AddBan(pid, ban)                     # RequireCommittableConfig —
                                                                         # re-derived here only to name
                                                                         # the value applied (ONE event
                                                                         # — KD-5)
                1: AddBan(pid, RequireBanLength(STRAIGHT_RED_BAN_MATCHES))   # straight red, no yellow
        committed := true
        return pending.Count
```

The engine's promoted second yellow arrives as a **single kind-2 event** (verified —
`ApplyCardAndCheckSentOff` returns the actual kind and exactly one `CardIssuedEvent` publishes),
so the fold never sees a yellow-then-red pair for one incident, and post-match per-slot state is
never read (the v1.33 substitution reset would lose a subbed-off player's cards).

## 3.2 Thresholds & bans (FR-DC-006/007)

```
AddYellow(pid):
    RequireYellowThreshold(YELLOW_ACCUMULATION_THRESHOLD)   # F6 — fail loud below 1: the residual
                                                              # subtraction below can never terminate
                                                              # a crossing otherwise, and every single
                                                              # yellow would ban, silently
    e := tally[pid, comp]; e.Yellows += 1
    if e.Yellows >= YELLOW_ACCUMULATION_THRESHOLD:
        e.Yellows -= YELLOW_ACCUMULATION_THRESHOLD         # residual kept
        e.BanMatchesRemaining += RequireBanLength(ACCUM_BAN_MATCHES)   # F6 — fail loud if negative;
                                                                         # stacks additively (FR-DC-007)

AddBan(pid, matches):  tally[pid, comp].BanMatchesRemaining += matches   # matches < 0 is a CALLER bug
                                                                           # (F2-class), not a [GT] guard
```

**Worked example** (`THRESHOLD = 5`, `ACCUM = 1`, `SECOND_YELLOW = 1`, `STRAIGHT_RED = 2` — all
`[GT]` illustrative): a player on 4 yellows receives a kind-0 ⇒ `Yellows 5 → 0`, ban 1. A player
on 4 yellows receives a kind-2 ⇒ `Yellows 5 → 0` **and** the dismissal: ban `1 + 1 = 2` (the
accumulation and the second-yellow bans stack). A kind-1 ⇒ ban +2, yellows untouched. All
integer; same events ⇒ same tallies, always.

## 3.3 Serving & the availability filter (FR-DC-008..011)

```
OnClubFixturePlayed(clubId, fieldedPlayerIds):             # called once per played fixture of the club
    REQUIRE fieldedPlayerIds is not null                   # F2 — see the note below
    for each entry of a player currently at clubId with BanMatchesRemaining > 0:
        if entry.PlayerId in fieldedPlayerIds: continue    # he PLAYED — this fixture is not his ban
        entry.BanMatchesRemaining -= 1                     # either resolution path (KD-3)
    # "currently at clubId" is DERIVABLE: PlayerId / CLUB_SQUAD_SIZE == clubId (#27's club-scoped
    # id formula), and the KD-6 migration rule keeps the id current across transfers — no roster
    # read is needed.
    #
    # ERR-044-003 stage 1 (August 15, 2026). The fielded-eleven exemption exists because a banned
    # player CAN reach the pitch: #30 §2.3 F9's depleted-squad back-fill presses removed players
    # back in until the club can field the formation, and ERR-044-003 made suspension a stricter
    # reinstatement TIER than injury rather than an absolute bar. Without the exemption that
    # appearance also served his ban, so it was strictly free. The eleven is REQUIRED, not optional:
    # a caller that does not know who played cannot know whose ban was served, and defaulting the
    # unknown case to "serve everybody" restores the defect silently.
    #
    # On every fixture that does NOT reach the extremis tier this changes nothing — the filter has
    # already removed every suspended player before selection, so no banned id can appear in
    # fieldedPlayerIds at all.
    #
    # GRANULARITY: the exemption matches on player id alone, the same granularity as the club walk
    # itself, which already serves EVERY competition's ban on any played fixture. Both are exact
    # while the league is the only competition; a real multi-competition calendar (#43) must revisit
    # them together, since a league fixture should serve a league ban and leave a cup ban alone.

IsAvailable(s, pid)        := s[pid, comp].BanMatchesRemaining == 0   # (absent entry => available)
FilterAvailable(squad, s)  := a reduced VALUE COPY of squad keeping available players only
                              # NO viability gate here (ERR-044-003 withdrew F5's fail-loud) — the
                              # composed seam's viability rule, including the depleted-squad
                              # back-fill and its own terminal refusal, is #30 §2.3 F9 / §3.4
                              # Returns NULL if every player is suspended (ERR-044-005) — Squad
                              # cannot represent a zero-player roster. This is FR-DC-009's OWN
                              # surface; #44's production path is MarkSuspended's removal mask,
                              # consumed directly by #30's composed seam (AvailabilityComposition),
                              # which never calls FilterAvailable.
```

**Ordering (KD-3, the off-by-one lock):** fixture N resolves → the fold lands its cards → fixture
N+1's selection runs `FilterAvailable` at #30's resolve→configure seam (ERR-030-009) → the banned
player is excluded → after fixture N+1 is played, `OnClubFixturePlayed` decrements the ban → the
player is available for N+2 (a 1-match ban). Serving counts the club's fixtures on **any**
resolution path; only card *generation* is engine-fixture-only at minimal. The filter covers
**both clubs' resolved squads of every fixture on both resolution paths** (FR-DC-010, re-scoped at
ERR-044-002 — the prior "engine-resolved fixture" wording would have let a quick-sim fixture
decrement a ban the banned player had just played through, since nearly every fixture of a career
is quick-simmed) — a banned opponent does not appear against the managed club mid-ban on either
path.

**WITHIN one fixture, `OnClubFixturePlayed` MUST run BEFORE that same fixture's fold commits its
cards (ERR-030-037)** — this is the half of the off-by-one contract the paragraph above states only
implicitly by describing fixtures N and N+1 as separate steps. Serving decrements the bans that
were **outstanding at kickoff**; the fold then adds the ones earned **during** the fixture just
played. Reversing the two — committing fixture N's cards before serving fixture N's bans — lets a
player sent off in fixture N serve one match of his own ban **during** the match he was dismissed
in, turning a two-match red into a one-match ban, and (worse) a fresh accumulation ban that reduces
to exactly the served amount is decremented straight to `(0, 0)` and dropped by FR-DC-017 before it
has cost the player a single fixture — a card that bans nobody. Both `#30`'s composition-root
implementation (`SeasonLoop.PlayNextRound`) and any future re-implementation of the loop MUST
preserve this order.

## 3.4 Boundary & hygiene (FR-DC-013/017)

- `RollToNextSeason`: every entry's `Yellows := 0`; `BanMatchesRemaining` **carries**. An entry
  reaching `(0, 0)` is dropped **immediately, whenever it occurs** (mid-season after a served
  ban with no residual yellows, or at the boundary sweep) — the canonical-minimality rule
  FR-DC-017 pins so an all-zero entry and an absent entry are never both encodable (a
  serialized-representation determinism hazard otherwise).
- Re-key (transfer): the `(oldPid, comp)` entry migrates to `(newPid, comp)` verbatim — tally and
  unserved bans both (bans follow the player; contrast #32's drop rule). Retirement: dropped.
  Both delivered by the T-phase roster-event wiring; a migration for an unknown source entry is a
  no-op only when the player had no entry (a *conflicting* target entry fails loud, F2).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §3 (occupancy fold, thresholds/bans + worked example, serving + the off-by-one lock, boundary/hygiene), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1 (M follow-through): §3.4 aligns the `(0,0)` drop to **immediate, wherever it occurs** (mid-season serve-out included), citing FR-DC-017. |
| 0.3 | 2026-07-24 | — | Cross-set AR pass 3 (M follow-through): §3.3's ordering paragraph states the both-squads filter coverage (FR-DC-010 — a banned opponent is excluded from the engine-resolved fixture). |
| 0.4 | 2026-08-13 | — | **C1/C2 landing back-prop.** **ERR-044-002:** §3.3's ordering paragraph re-scoped from "the engine-resolved fixture" to both clubs' resolved squads of every fixture on both resolution paths, matching #30 §3.4's LIVE seam. **ERR-044-003:** the `FilterAvailable` pseudocode's F5 fail-loud comment replaced — #44 implements no viability gate; the rule is #30 §2.3 F9. |
| 0.5 | 2026-08-13 | — | **ERR-030-037** (adversarial review over the C1/C2 landing, M7): §3.3 gains a normative paragraph for the WITHIN-fixture half of the off-by-one contract — `OnClubFixturePlayed` MUST run before that same fixture's fold commits its cards, never after — locked in code by `SeasonLoopDisciplineTests.ANewBanEarnedThisFixtureIsNotServedByThisSameFixture` (`src/season-save/`). |
| 0.6 | 2026-08-13 | — | **ERR-044-005** back-prop: §3.3's `FilterAvailable` pseudocode gains the `null`-return case for an all-suspended squad (`Squad` cannot represent zero players) and names `MarkSuspended`'s removal mask, consumed directly by #30's `AvailabilityComposition`, as #44's actual production path — `FilterAvailable` itself is FR-DC-009's own surface. |
| 0.7 | 2026-08-13 | — | **L13**, a third adversarial-review pass: §3.2's `AddYellow` pseudocode gains `RequireYellowThreshold(YELLOW_ACCUMULATION_THRESHOLD)` before the tally read and `RequireBanLength(ACCUM_BAN_MATCHES)` around the accumulation ban — the two `[GT]` fail-loud guards `DisciplineRules.AddYellow`/`ApplyCard` actually enforce (§2.3 **F6**), previously present in code and unit tests but nowhere in the normative text; an implementer following §3.2 verbatim would have shipped a config that silently bans on the first yellow (the #29/#41 AR pass 9 F8 lesson, recurring here). |
| 0.8 | 2026-08-13 | — | **M20**, extending L13's fix rather than a new id: §3.1's occupancy-fold pseudocode still read `2: AddYellow(pid); AddBan(pid, SECOND_YELLOW_BAN_MATCHES)` and `1: AddBan(pid, STRAIGHT_RED_BAN_MATCHES)` — L13 patched §3.2's `AddYellow` with the F6 guards but stopped one section short of §3.1, which an implementer following verbatim would have reproduced M4 (the yellow committed while the card is refused) in APPROVED text. Both branches now show `RequireBanLength(...)` and the kind-2 branch validates BEFORE `AddYellow` runs, matching `DisciplineRules.ApplySecondYellow`/`ApplyStraightRed` exactly. |
| 0.9 | 2026-08-15 | — | **ERR-044-003 stage 1**, owner decision: §3.3's `OnClubFixturePlayed(clubId)` pseudocode becomes `OnClubFixturePlayed(clubId, fieldedPlayerIds)` — a played fixture the banned player himself appeared in (reachable only through #30 §2.3 F9's depleted-squad back-fill) no longer decrements his ban, with a new comment block explaining why the exemption exists, that it changes nothing outside the extremis tier, and its granularity relative to the FR-DC-012 competition key. Ordering paragraph and FR-DC-011 cross-reference updated to match. |
| 0.10 | 2026-08-15 | — | **M27** (#44 adversarial-review round 4, `open-issues.md`): §3.1's normative fold pseudocode called `AddYellow`/`AddBan` straight from `OnTapRecord`, with no buffer and no `Commit` — verified against `src/discipline/CardLedgerFold.cs`, whose real shape is `ObserveTick` (buffers a `(PlayerId, CardKind)` pair per card, applying nothing) and a separate `Commit(rules)` (validates all four bound `[GT]`s via `RequireCommittableConfig` before the loop, then applies the whole buffered list, all-or-nothing — the M13 fix, v1.2). Rewritten to match: `ObserveTick`/`Commit` as two named steps, a `pending` list, and the F6 guard called once before any buffered card is applied. §0.7/§0.8's F6/kind-2-ordering fixes are preserved verbatim inside the new `Commit` body — this is a restructuring around them, not a second change to the guard logic. |
#endregion
