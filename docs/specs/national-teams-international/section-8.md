# National Teams & International Management #36 — Section 8: Cross-References & Back-Propagations

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 8.1 Typed cross-references

| ID | Target | Contract |
|---|---|---|
| XC-036-001 | #27 `PlayerRecord` | `{ PlayerId, FirstName, LastName, Age, Position, Attributes }` — **no nationality field exists**, and #36 adds none (FR-NT-001). The fact §1.4(a) records and KD-1 answers. |
| XC-036-002 | #27 `RosterGenerator` / `PlayerDatabaseConstants.FIELDS_PER_PLAYER` | Each player consumes **exactly** `FIELDS_PER_PLAYER` draws under an ORDINAL STABILITY contract. #36 changes neither the order nor the count (FR-NT-002). |
| XC-036-003 | `LeagueBootstrapGoldenVectorTests` | The pinned digest that exists because rosters are **regenerated from the world seed, never saved** — so a generation-path change *"would silently rewrite every club in every existing save with the whole suite green."* **Unchanged by #36**, asserted inside #36's own suite (T-NT-DET-001). |
| XC-036-004 | #27 `Squad`, `PlayerAttributes` | Consumed read-only. `Squad` is the return type of `TryResolveNationSquad`; a national squad is a **view**, never a copy (FR-NT-022). |
| XC-036-005 | #31 KD-7 / FR-TX-022 | The club-scoped `PlayerId` **re-keys on transfer**, and FR-TX-022 is the roster-move hook. **The fact that makes the derivation alone insufficient** (§3.1.1) and the mechanism the pin uses. |
| XC-036-006 | #44 FR-DC-013 | #44 **migrates** bans across the same re-key. #36 follows that rule for `CallUp` and `NationPin` — **not** #32's drop rule (FR-NT-023). |
| XC-036-007 | #32 KD-1 | The derive-on-read pattern #36 reuses: stateless keyed derivation instead of stored state, *"dissolving the save-bloat and re-roll risks by construction."* |
| XC-036-008 | #32's drop-on-transfer knowledge rule | The deliberate **contrast** with XC-036-006. A call-up is a live selection of a person; scouting knowledge is a stale fact about a squad slot. |
| XC-036-009 | #30 FR-SN-013 | The **resolve → filter → configure** null seam. #36 is its **second** consumer after #44 — no new #30 seam is needed (KD-2). |
| XC-036-010 | #44 FR-DC-010 | Makes the filter *"a value-copy reduction"* applied to **both** clubs of an engine-resolved fixture — the shape #36's filter copies exactly. |
| XC-036-011 | #30 FR-SN-009/010/011 `SeasonCalendar` | Read-only. #36 derives its window from it and **never mutates it** (FR-NT-015). |
| XC-036-012 | #31 FR-TX-019 | *"The transfer window MUST be a #31-owned `TransferWindow` derived deterministically from #30's `SeasonCalendar` (read-only) … #31 MUST NOT mutate the calendar."* The **precedent** #36's window stands in exactly. |
| XC-036-013 | #43 FR-CP-001/005/006 | `CompetitionFormat { RoundRobin, Knockout, GroupThenKnockout }`; entrant sets in canonical ascending order; round-robin instances reusing `FixtureScheduler.Generate(clubIds, seed)`. |
| XC-036-014 | `FixtureScheduler.Generate(int[] clubIds, ulong seed)` | **Verifiably id-agnostic** — the signature that makes KD-3's disjoint-range answer work, and the reason **#43 needs no change**. |
| XC-036-015 | #43 FR-CP-007/009 | Keyed, position-independent `competition.draws` with **no cursor**. The draws #36 needs are **#43's** — which is why #36 stays draw-free (KD-8). |
| XC-036-016 | `ISquadProvider` (declared in `src/match-engine/`) | **Never implemented and never named by #36** (FR-NT-004). The root composes; `League` may implement it because `season-save` already references `match-engine`, and #36 does not. |
| XC-036-017 | #16 §3.4 | `_RESERVED_0x28_` / `SubsystemOrdinals 90` — **already present and already correct** for a draw-free spec. **Nothing to file**, and possibly nothing ever to promote (KD-8). |
| XC-036-018 | #19 §3.1.4 | Test-ID prefixes; the §5.10 closed-loop scenario registration under `SCENARIO_PATH_CROSS_SPEC_PREFIX`. |

## 8.2 At approval — land **atomically** with the status flip

| ID | Target | Change | Kind |
|---|---|---|---|
| **ERR-030-016** | `season-competition-loop/section-3.md` §3.4 + `section-2.md` FR-SN-013 | Record that the resolve→configure **filter seam admits more than one consumer** (#44 suspensions, #36 call-ups); that the current consumers **compose order-independently because both are removals**, with the property stated as a property rather than an accident; and that a future **non-removal** filter (one that adds or substitutes a player) would require an **explicit order**. Also names the shared **empty-squad floor** obligation — a squad reduced below a fieldable eleven by the *composition* — as a #44/#36/#30 concern at the seam rather than either filter's private business. **No new seam**: a contract note on an existing one. | Doc-only contract note |

**One back-prop, and that is the headline.** A spec that introduces a concept the game did not have —
nationality — and a whole new class of entity — national teams — files **exactly one doc-only note**
against one neighbour. That is the measure of how much of #36 was already waiting upstream (§1.4(c)), and
it is why §8.3's list of things **not** filed is longer than this table.

## 8.3 Deferred — land at the named tier, **not** at approval

- The outer `SEASON_SAVE_FORMAT_VERSION` bump, at **T2** when the sub-blob is first composed in.
- **#43 instance registration** for international competitions (T3) — a #36-side *use* of #43's existing
  API by the **root**, not a #43 change (KD-3).
- **Routing international minutes into #29/#41** (T3) — no minutes exist until an international match is
  played, and building the route first would be the phantom-consumer class FR-LW-031 forbids (FR-NT-028).
- The root's **composite `ISquadProvider`** (T3) — root-side code, not a #36 surface (FR-NT-004).
- Promotion of `_RESERVED_0x28_`, **only** if a #36-owned stochastic surface ever appears (FR-NT-030) —
  an injury-forced replacement call-up being the plausible candidate. Recorded so the reserved slot is not
  treated as pre-authorised.

## 8.4 Explicitly **not** back-props (recorded so their absence is not read as an omission)

- **#27 — nothing to change, and that is KD-1's entire point.** No `PlayerRecord` field, no
  `RosterGenerator` draw, no `FIELDS_PER_PLAYER` bump, no golden-vector rebaseline, **no save break**.
  The one place this spec could have been expensive, it is free — and T-NT-DET-001 asserts it inside
  #36's own suite so it stays free.
- **#43 — nothing to change.** Entrant sets are plain `int`s, `FixtureScheduler` is id-agnostic, and
  national teams take a **disjoint reserved id range**, so FR-CP-016's *"`ClubId`s never re-key"* holds
  trivially for ids that are never re-keyed either. #36 uses the API exactly as specified.
- **#44 — nothing to change.** The two filters compose at a seam **#44 does not own**, so the composition
  note is filed against **#30**, where the seam lives. Filing it against #44 would put a multi-consumer
  contract inside one of the consumers.
- **#31 — nothing to change.** FR-TX-022's roster-move hook already exists and already carries #44's ban
  migration; #36 is a second subscriber to it, not a new mechanism.
- **#16 — nothing to change.** `_RESERVED_0x28_` / `SubsystemOrdinals 90` already exists and is already
  correct for a draw-free spec (§2(f) of the supplement, verified). Unlike #45 — which had to file its own
  placeholder — and unlike **#46**, which has **no reserved row at all**, #36's row is present, correct,
  and may stay reserved permanently.
- **#30's calendar — nothing to change.** A read-only derivation, the #31 FR-TX-019 precedent
  (FR-NT-015). The one #30 change is ERR-030-016, and it touches the *filter seam*, not the calendar.
- **#47 — nothing imposed.** Authored nationalities land in the `NationPin` table #36 already ships, so
  #47 gains **no new #36 surface**. What #47 owns is the authored-vs-pin precedence policy (§7.5 R-2).

## 8.5 References

#36 introduces **no external citation**. Its content is an eligibility model, a selection rule, and a set
of boundaries composed from this project's own approved specs; there is no published result it rests on,
and inventing a citation to decorate the section would be the fabrication the project's rules forbid.

Note in particular that the **nation catalogue is not a citation surface**: it is a `[GT]` roster of
in-game nations with `[GT]` weights, authored for balance (FR-NT-014), not a demographic claim about the
real world. Tabulating real-world player-nationality distributions here would give the weights a false
authority they should not carry — they are tuning values, and §5.2's distribution test asserts *shape*
against the weights, never against any external figure.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §8 (XC-036-001..018, the single approval-time back-prop, the deferred set, the not-a-back-prop list — deliberately longer than the back-prop table, since three of the plan's five decisions had answers waiting upstream — and the no-external-citation rationale, extended to record that the nation catalogue's weights are tuning values rather than demographic claims). Status IN REVIEW. |
#endregion
