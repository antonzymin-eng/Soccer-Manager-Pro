# Discipline & Suspensions #44 — Section 3: Core Algorithms

**Created:** July 24, 2026
**Last Updated:** August 13, 2026, later still (v0.6 — ERR-044-005 back-prop, owed by the #44 C1/C2
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
**Version:** 0.6
**Status:** APPROVED

---

## 3.1 The occupancy fold (FR-DC-002/004/005/006)

```
CardLedgerFold(lineup /* slot -> PlayerId, incl. bench identities */):
    occupancy := lineup                                    # seeded by the root (F1 on any gap)

    OnTapRecord(record):                                   # per tick, canonical publish order
        switch record.ordinal:
            0x08 SubstitutionEvent:
                occupancy.ApplySub(record.Outgoing, record.Incoming)   # occupant changes at this tick
            0x06 CardIssuedEvent:
                pid := occupancy.OccupantAt(record.Recipient)          # F1 if unmapped
                switch record.CardKind:                                # F4 outside {0,1,2}
                    0: AddYellow(pid)                                  # first yellow
                    2: AddYellow(pid); AddBan(pid, SECOND_YELLOW_BAN_MATCHES)   # ONE event (KD-5)
                    1: AddBan(pid, STRAIGHT_RED_BAN_MATCHES)           # straight red, no yellow
            else: ignore                                               # FR-DC-004 (unknown ordinals)
```

The engine's promoted second yellow arrives as a **single kind-2 event** (verified —
`ApplyCardAndCheckSentOff` returns the actual kind and exactly one `CardIssuedEvent` publishes),
so the fold never sees a yellow-then-red pair for one incident, and post-match per-slot state is
never read (the v1.33 substitution reset would lose a subbed-off player's cards).

## 3.2 Thresholds & bans (FR-DC-006/007)

```
AddYellow(pid):
    e := tally[pid, comp]; e.Yellows += 1
    if e.Yellows >= YELLOW_ACCUMULATION_THRESHOLD:
        e.Yellows -= YELLOW_ACCUMULATION_THRESHOLD         # residual kept
        e.BanMatchesRemaining += ACCUM_BAN_MATCHES         # stacks additively (FR-DC-007)

AddBan(pid, matches):  tally[pid, comp].BanMatchesRemaining += matches
```

**Worked example** (`THRESHOLD = 5`, `ACCUM = 1`, `SECOND_YELLOW = 1`, `STRAIGHT_RED = 2` — all
`[GT]` illustrative): a player on 4 yellows receives a kind-0 ⇒ `Yellows 5 → 0`, ban 1. A player
on 4 yellows receives a kind-2 ⇒ `Yellows 5 → 0` **and** the dismissal: ban `1 + 1 = 2` (the
accumulation and the second-yellow bans stack). A kind-1 ⇒ ban +2, yellows untouched. All
integer; same events ⇒ same tallies, always.

## 3.3 Serving & the availability filter (FR-DC-008..011)

```
OnClubFixturePlayed(clubId):                               # called once per played fixture of the club
    for each entry of a player currently at clubId with BanMatchesRemaining > 0:
        entry.BanMatchesRemaining -= 1                     # either resolution path (KD-3)
    # "currently at clubId" is DERIVABLE: PlayerId / CLUB_SQUAD_SIZE == clubId (#27's club-scoped
    # id formula), and the KD-6 migration rule keeps the id current across transfers — no roster
    # read is needed.

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
#endregion
