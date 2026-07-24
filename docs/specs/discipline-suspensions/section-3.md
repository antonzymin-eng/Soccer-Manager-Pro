# Discipline & Suspensions #44 — Section 3: Core Algorithms

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.2 — section-file AR PASS-1; prior v0.1 initial)
**Version:** 0.2
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
                              # F5 if the result drops below the 18 ConfigureSquads consumes
```

**Ordering (KD-3, the off-by-one lock):** fixture N resolves → the fold lands its cards → fixture
N+1's selection runs `FilterAvailable` at #30's resolve→configure seam (ERR-030-009) → the banned
player is excluded → after fixture N+1 is played, `OnClubFixturePlayed` decrements the ban → the
player is available for N+2 (a 1-match ban). Serving counts the club's fixtures on **any**
resolution path; only card *generation* is engine-fixture-only at minimal.

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
#endregion
