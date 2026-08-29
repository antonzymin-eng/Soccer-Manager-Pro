# Discipline & Suspensions #44 — Section 3: Core Algorithms

**Created:** July 24, 2026
**Last Updated:** August 16, 2026, latest of all (v0.15 — reviewed-findings pass, finding L11: §3.2's
worked example named the four `[GT]` threshold/ban constants as backticked ALL_CAPS abbreviations
(`THRESHOLD`, `ACCUM`, `SECOND_YELLOW`, `STRAIGHT_RED`) that exist nowhere in code or in this spec's
own §2.1/§2.2 — `ERR-044-017` (August 15, 2026) had already renamed these constants ALL_CAPS →
PascalCase everywhere else. Corrected to `YellowAccumulationThreshold`/`AccumBanMatches`/
`SecondYellowBanMatches`/`StraightRedBanMatches`, values and the illustrative tag unchanged.)
**Last Updated (prior):** August 16, 2026, latest (v0.14 — reviewed findings pass, findings A/B. **`ERR-044-022`**
(finding A): the `CardLedgerFold` constructor pseudocode gains a required `onPitchAgentIdCount`
parameter and the `SubstitutionEvent` branch's `ApplySub` call gains an inline comment stating the two
new refusals — `Incoming < onPitchAgentIdCount` (an on-pitch id cannot come on) and
`Outgoing >= onPitchAgentIdCount` (only an on-pitch id can go off) — closing the gap the M1
one-to-one check (v0.13) could not see on its own: it knows only player ids, never which agent ids are
on-pitch versus bench. Matches `CardLedgerFold.cs` v1.10. **`ERR-044-023`** (finding B, doc only): the
constructor line's own comment now states the boot-only precondition on `lineup` explicitly — it must
be taken before any substitution, since the engine's slot→PlayerId map is one-to-one over non-sentinel
entries only at that moment (bound in full at §4.3).)
**Last Updated (prior):** August 16, 2026, later (v0.13 — final fixer pass over the reviewed-findings round.
**`ERR-044-021`** (M1): §3.1's normative pseudocode stated the substitution swap as
`occupancy.ApplySub(record.Outgoing, record.Incoming)` with no instruction to clear
`record.Incoming`'s own slot afterward, and the constructor line's seed contract named only "F1 on
any gap" with no one-to-one requirement — both the EXACT pre-fix shapes the reviewed findings pass
closed in code (`CardLedgerFold.cs` v1.8, M1): an implementer following either verbatim would
reproduce the dormant double-booking/duplicate-mapping hole. `ApplySub` now clears the vacated
incoming slot and refuses `Outgoing == Incoming`; the constructor line states the one-to-one
requirement alongside F1. **`ERR-044-020`** (M3): `ObserveTick`'s pseudocode gains the
consecutive-tick refusal and the partial-application poison latch (`faulted`), mirroring
`CardLedgerFold.cs`'s real `ObserveTick` exactly — spec-side sync of a code addition the prior text
was silent on, not a contradiction of it. **M7**: the four `[GT]` constant names in this section's own
pseudocode renamed ALL_CAPS → PascalCase (matching `section-2.md`'s and `DisciplineConstants.cs`'s
naming); the two v0.7/v0.8 version-history rows quoting the OLD pseudocode by name are left as-is,
describing the code's state at that landing (the `DisciplineConstants.cs` v1.5 / `appendices.md` v0.5
L3 precedent for historical quotes).)
**Last Updated (prior):** August 16, 2026 (v0.12 — `ERR-044-014`, adversarial-review H1: §3.3's
`OnClubFixturePlayed` takes the club's roster ids and decides membership by PRESENCE in them. The
retired text stated club membership was "DERIVABLE: `PlayerId / CLUB_SQUAD_SIZE == clubId` … no roster
read is needed", which gave #44 two notions of membership — that derivation and the roster walk its own
removal half performs — resting on a migration rule (FR-DC-013) that has no production caller. On the
first disagreement a banned player is removed from every squad he is really in while his ban never
decrements: suspended forever, silently. §2.2's signature, §2.3 F2 and §4.5's root contract updated in
the same commit)
**Last Updated (prior):** August 15, 2026, yet later still (v0.11 — `ERR-044-010`, reviewed-findings pass:
§3.3's `OnClubFixturePlayed` pseudocode comment block gains a SUBSTITUTION DEPENDENCY paragraph — the
`fieldedPlayerIds` the composition root supplies is the STARTING eleven (`SeasonLoop.FieldedXi`), not
a record of who actually played, correct today only because no `MatchEngine.SubstitutePlayer` call
site exists on the season path; a suspended player fielded as a substitute rather than a starter would
not be recognized once one does, reopening ERR-044-003's free-appearance defect at the substitution
boundary. FR-DC-011 (`section-2.md`) gains the matching note)
**Last Updated (prior):** August 15, 2026, later still (v0.10 — M27, the spec half of #44's adversarial-review
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
**Version:** 0.15
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
CardLedgerFold(lineup /* slot -> PlayerId, incl. bench identities */, onPitchAgentIdCount):
    # F1 on any gap; and ONE-TO-ONE (ERR-044-021) — no two agent ids may map to the same non-empty
    # player id, or a card at EITHER id attributes to that one player while whichever OTHER player the
    # seed actually intended for one of those ids is never attributed a card at all. Checked once here,
    # at construction, over the whole seed — not left as a runtime property nothing enforces.
    #
    # onPitchAgentIdCount (ERR-044-022): 0 < onPitchAgentIdCount <= len(lineup) — the boundary between
    # on-pitch agent ids [0, onPitchAgentIdCount) and the engine's synthetic bench ids
    # [onPitchAgentIdCount, len(lineup)). The one-to-one check above cannot by itself tell a bench id
    # from a pitch id — it only sees player ids — so ApplySub below uses this boundary directly.
    #
    # lineup MUST be taken AT BOOT, before any substitution (ERR-044-023, §4.3): the engine's own
    # slot->PlayerId map is one-to-one over non-sentinel entries only at that moment.
    occupancy := lineup
    pending := []                                           # buffered (PlayerId, CardKind) pairs
    committed := false                                       # Commit runs exactly once (FR-DC-010)
    faulted := false                                          # ERR-044-020 — poisoned by a part-way tick
    lastObservedTick := none                                  # ERR-044-020 — anchors on the first call

    ObserveTick(tap):                                       # per tick, canonical publish order
        REQUIRE not committed
        REQUIRE not faulted        # ERR-044-020 — a prior part-way failure poisons every later call,
                                    # even one that is otherwise perfectly consecutive (mirrors #37
                                    # MatchAnalyticsAggregator's F6)
        REQUIRE lastObservedTick is none OR tap.CurrentTick == lastObservedTick + 1   # ERR-044-020 —
                                    # the very FIRST call anchors on whatever tick it is given (a
                                    # fixture need not begin at tick 0); every later call must be
                                    # exactly one more than the last
        lastObservedTick := tap.CurrentTick
        faulted := true            # set BEFORE the loop runs; cleared only once the WHOLE tick applies
                                    # without throwing — a record partway through can still throw (F1/F4)
        for each record in tap.records:
            switch record.ordinal:
                0x08 SubstitutionEvent:
                    # ERR-044-021: ApplySub moves the outgoing slot's occupant to Incoming's identity
                    # AND clears Incoming's own slot to NO_PLAYER — without the clear, the incoming
                    # player would occupy TWO agent ids at once, and a later malformed record naming
                    # the stale incoming id would silently attribute a second card to him instead of
                    # failing loud (F1). Refuses Outgoing == Incoming (the write and the clear would
                    # target the same index, and the clear would erase the write an instant later).
                    #
                    # ERR-044-022: ALSO refuses Incoming < onPitchAgentIdCount (an on-pitch agent id
                    # cannot come ON) and Outgoing >= onPitchAgentIdCount (only an on-pitch agent id
                    # can go OFF) — BEFORE the write/clear pair above runs. Without this, Incoming
                    # naming an occupied ON-PITCH slot (the Appendix C "slot 19" family, ERR-044-001)
                    # reached the write unchecked: the write silently destroyed the OUTGOING slot's
                    # prior occupant's mapping and the clear then erased the (wrongly-named) Incoming
                    # slot's own mapping too. The one-to-one check at construction cannot catch this —
                    # it only ever sees player ids, never which agent ids are on-pitch versus bench.
                    occupancy.ApplySub(record.Outgoing, record.Incoming)
                0x06 CardIssuedEvent:
                    pid := occupancy.OccupantAt(record.Recipient)          # F1 if unmapped
                    RequireKnownCardKind(record.CardKind)                  # F4 outside {0,1,2}
                    pending.Add((pid, record.CardKind))                    # BUFFERED — not applied yet
                else: ignore                                               # FR-DC-004 (unknown ordinals)
        faulted := false           # reached only when every record in this tick applied cleanly

    Commit(rules):                                          # called ONCE, at fixture resolution
        REQUIRE not committed
        RequireCommittableConfig()   # F6 — validates ALL FOUR bound [GT]s up front, before any
                                      # buffered card is applied, so a bad config refuses the WHOLE
                                      # fixture atomically and never half of it (M13)
        for each (pid, kind) in pending, in buffered (= publish) order:
            switch kind:
                0: AddYellow(pid)                                  # first yellow (F6 inside, §3.2)
                2: ban := RequireBanLength(SecondYellowBanMatches)   # already validated by
                   AddYellow(pid); AddBan(pid, ban)                     # RequireCommittableConfig —
                                                                         # re-derived here only to name
                                                                         # the value applied (ONE event
                                                                         # — KD-5)
                1: AddBan(pid, RequireBanLength(StraightRedBanMatches))   # straight red, no yellow
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
    RequireYellowThreshold(YellowAccumulationThreshold)   # F6 — fail loud below 1: the residual
                                                              # subtraction below can never terminate
                                                              # a crossing otherwise, and every single
                                                              # yellow would ban, silently
    e := tally[pid, comp]; e.Yellows += 1
    if e.Yellows >= YellowAccumulationThreshold:
        e.Yellows -= YellowAccumulationThreshold         # residual kept
        e.BanMatchesRemaining += RequireBanLength(AccumBanMatches)   # F6 — fail loud if negative;
                                                                         # stacks additively (FR-DC-007)

AddBan(pid, matches):  tally[pid, comp].BanMatchesRemaining += matches   # matches < 0 is a CALLER bug
                                                                           # (F2-class), not a [GT] guard
```

**Worked example** (`YellowAccumulationThreshold = 5`, `AccumBanMatches = 1`,
`SecondYellowBanMatches = 1`, `StraightRedBanMatches = 2` — all `[GT]` illustrative): a player on 4
yellows receives a kind-0 ⇒ `Yellows 5 → 0`, ban 1. A player
on 4 yellows receives a kind-2 ⇒ `Yellows 5 → 0` **and** the dismissal: ban `1 + 1 = 2` (the
accumulation and the second-yellow bans stack). A kind-1 ⇒ ban +2, yellows untouched. All
integer; same events ⇒ same tallies, always.

## 3.3 Serving & the availability filter (FR-DC-008..011)

```
OnClubFixturePlayed(clubId, clubPlayerIds, fieldedPlayerIds):   # once per played fixture of the club
    REQUIRE clubPlayerIds is not null                      # F2 — see the note below
    REQUIRE fieldedPlayerIds is not null                   # F2 — see the note below
    for each entry with BanMatchesRemaining > 0 whose PlayerId is in clubPlayerIds:
        if entry.PlayerId in fieldedPlayerIds: continue    # he PLAYED — this fixture is not his ban
        entry.BanMatchesRemaining -= 1                     # either resolution path (KD-3)
    #
    # ERR-044-014 (August 16, 2026). "Currently at clubId" is READ FROM THE ROSTER the caller
    # resolved — presence in clubPlayerIds, matched exactly as MarkSuspended below matches the
    # squad it walks — and is NOT derived. This paragraph previously read "DERIVABLE:
    # PlayerId / CLUB_SQUAD_SIZE == clubId (#27's club-scoped id formula), and the KD-6 migration
    # rule keeps the id current across transfers — no roster read is needed". Both halves were
    # wrong to rely on. (a) It made #44 hold TWO notions of club membership — a derivation here and
    # a roster walk in the removal half — which agree only while #27's packing holds and nothing
    # anywhere checked that they did. (b) The migration rule that was supposed to keep them agreeing
    # is not applied: MigratePlayerId and DropPlayer have no production caller (recorded at
    # SeasonLoop.RollToNextSeason's roster-sync site). On the first disagreement — a #31 transfer, or
    # the §7.2 / ERR-044-003 stage 3 id-space widening that is already required — a banned player is
    # removed from every squad he is really in while his ban is never decremented: suspended
    # forever, with no throw, no log, and no test able to observe it. This is ERR-041-019's defect
    # one subsystem over, closed the same way: ONE notion of membership, taken from the roster,
    # enforced at the entry point.
    #
    # clubId is retained for the F2 caller-contract gate and for identity/diagnostics only; it takes
    # part in no matching decision, and it is deliberately NOT cross-checked against clubPlayerIds,
    # since any such check would have to re-derive a club from a player id.
    #
    # The roster passed MUST be the UNFILTERED one. Every id whose ban is being served is precisely
    # an id FilterAvailable / the composed seam has just removed, so serving against a filtered
    # squad makes every suspension unservable — the same permanently-suspended outcome by the
    # opposite route.
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
    #
    # SUBSTITUTION DEPENDENCY (recorded, not fixed — ERR-044-010). fieldedPlayerIds today IS the
    # STARTING eleven (SeasonLoop.FieldedXi, derived from the same pre-kickoff LineupSelector walk
    # ConfigureSquads consumes), not a record of who actually took the field. That is exact only
    # because no MatchEngine.SubstitutePlayer call site exists yet (Stage 0 fields a fixed XI —
    # CardLedgerFold's own recorded gap). #44 is otherwise scrupulously substitution-correct for card
    # ATTRIBUTION (the occupancy fold tracks synthetic bench ids through every SubstitutionEvent);
    # this exemption is not — the moment a production caller substitutes players, a suspended player
    # fielded by the extremis back-fill as a SUBSTITUTE rather than a starter will not appear in
    # fieldedPlayerIds, and his ban will decrement for a fixture he actually played, reopening
    # ERR-044-003's free-appearance defect at the substitution boundary. The fix belongs at the
    # #30-owned derivation site (§4.5), not here.

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
| 0.11 | 2026-08-15 | — | **`ERR-044-010`**, reviewed-findings pass: §3.3's `OnClubFixturePlayed` comment block gains a SUBSTITUTION DEPENDENCY paragraph recording that `fieldedPlayerIds` is today's STARTING eleven, not a played-eleven record, and stays correct only while no `MatchEngine.SubstitutePlayer` call site exists on the season path (verified against `src/discipline/CardLedgerFold.cs`'s own "the substitution branch has no production driver" remark and `src/season-save/SeasonLoop.cs`'s `FieldedXi`, which derives from the pre-kickoff `LineupSelector` walk). See `spec-error-log.md` `ERR-044-010`. |
| 0.12 | 2026-08-16 | — | **`ERR-044-014`** (adversarial review, H1): §3.3's `OnClubFixturePlayed` pseudocode becomes `OnClubFixturePlayed(clubId, clubPlayerIds, fieldedPlayerIds)`, matches club membership by presence in `clubPlayerIds`, and REQUIREs it non-null (F2). The retired "DERIVABLE: `PlayerId / CLUB_SQUAD_SIZE == clubId` … no roster read is needed" comment is replaced by the reason it was unsafe: it was a second notion of membership beside `MarkSuspended`'s roster walk, agreeing only while #27's packing holds, and its stated guarantee — FR-DC-013's migration rule keeping a transferred player's id current — is not in force, `MigratePlayerId`/`DropPlayer` having no production caller (verified in `src/season-save/SeasonLoop.cs`). Also states that `clubId` is now identity/F2 only and deliberately un-cross-checked, and that the roster passed MUST be the unfiltered one, since every id being served is one the filter has just removed. `src/discipline/DisciplineRules.cs` v1.7, `src/season-save/SeasonLoop.cs` v1.29, same commit. See `spec-error-log.md` `ERR-044-014`. |
| 0.13 | 2026-08-16, later | — | **Final fixer pass, two findings.** **`ERR-044-021`** (M1): §3.1's `CardLedgerFold` constructor line gains the one-to-one seed requirement alongside "F1 on any gap"; the `SubstitutionEvent` branch's `ApplySub` call gains an inline comment stating it clears `record.Incoming`'s vacated slot and refuses `Outgoing == Incoming` — both were previously silent on the exact shapes the reviewed findings pass closed in code (`CardLedgerFold.cs` v1.8), so an implementer following the OLD text verbatim would have reproduced the dormant hole. **`ERR-044-020`** (M3): `ObserveTick`'s pseudocode gains `faulted`/`lastObservedTick` state and the consecutive-tick + poison-latch refusals, matching `CardLedgerFold.cs`'s real `ObserveTick` (v1.8) and `IDisciplineTickLedgerTap.CurrentTick` (declared at `section-2.md` v0.12 §2.2). Also renamed the four `[GT]` constants in this section's active pseudocode ALL_CAPS → PascalCase (M7) — the v0.7/v0.8 version rows below, which quote the pre-rename pseudocode by name, are left as historical quotes per the `DisciplineConstants.cs`/`appendices.md` L3 precedent. See `spec-error-log.md` `ERR-044-017`, `ERR-044-020`, `ERR-044-021`. |
| 0.14 | 2026-08-16, latest | — | **Reviewed findings pass, findings A/B.** **`ERR-044-022`** (finding A): the `CardLedgerFold` constructor pseudocode's signature gains `onPitchAgentIdCount`, with a comment stating the on-pitch/bench boundary it marks and that the one-to-one check above it cannot by itself distinguish a bench id from a pitch id. The `SubstitutionEvent` branch's `ApplySub` call gains a second comment stating the two new refusals it now performs BEFORE the write/clear pair — `Incoming < onPitchAgentIdCount` and `Outgoing >= onPitchAgentIdCount` — and the exact pre-fix defect they close (an on-pitch `Incoming` reached the write unchecked, silently destroying the outgoing slot's prior occupant's mapping). Matches `CardLedgerFold.cs` v1.10. **`ERR-044-023`** (finding B, doc only): the constructor line's comment states the `lineup` boot-time precondition explicitly, cross-referencing §4.3 for the full statement. See `spec-error-log.md` `ERR-044-022`, `ERR-044-023`. |
| 0.15 | 2026-08-16, latest of all | — | **Reviewed-findings pass, finding L11.** §3.2's worked example renamed its four illustrative `[GT]` constants from backticked ALL_CAPS abbreviations that exist nowhere in code (`THRESHOLD`, `ACCUM`, `SECOND_YELLOW`, `STRAIGHT_RED`) to the actual PascalCase names (`YellowAccumulationThreshold`, `AccumBanMatches`, `SecondYellowBanMatches`, `StraightRedBanMatches`) — `ERR-044-017` renamed the constants themselves the prior day; this was the one surviving worked-example reference to the retired spelling. Values and the illustrative tag unchanged. |
#endregion
