# Discipline & Suspensions #44 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 24, 2026
**Last Updated:** August 16, 2026, later still (v0.10 — **`ERR-044-019` EXTENDED, not re-filed**, by
**`ERR-030-046`** (an ESCALATED High filed at #30 `section-3.md` v2.9, which owns the rule). §7.2's
ban-serving-under-squad-shortfall bullet described #30's within-tier key as a **best-effort minimisation**
of the forced-start residual. #30's rule is no longer an ordering key: two successive keys were defeated
by the same class of defect — an element-wise greedy decision of a **set-valued**, per-position
constraint — so the third attempt was ruled as a **capped exhaustive search** over subsets of the
eligible candidates. The bullet now states the result as a guarantee: **a reinstated-suspended player
starts only in a probe-verified forced start, every completing choice within the search bound starting at
least as many**, so the composed eleven carries the MINIMUM achievable number of them and zero whenever
any completing choice benches them all. The residual becomes a two-item list — forced starts, and a
beyond-cap corner above `SeasonSaveConstants.EXTREMIS_SEARCH_CANDIDATE_CAP` = 12 — rather than an
enumeration of routes into a forced start, that form having now been falsified twice. The staging below
is unchanged and is still the only thing that deletes item (i). §2.3's mirror amended in the same commit
(`section-2.md` v0.17).)
**Last Updated (prior):** August 16, 2026, later (v0.9 — **`ERR-030-045`**, an adversarially-reviewed High
continuing `ERR-030-044`'s, filed at #30 which owns the rule; back-propagated here. §7.2's
ban-serving-under-squad-shortfall bullet, as amended at v0.8, named "the sole-goalkeeper case" as the
forced-start case. It is only one of two: a club short by **more than one** player gets no useful probe
on any reinstatement but the last (fieldability is monotone in adding players), so #30's amended pass-3
key decides those blind and can only make them well — if every completing choice starts someone, someone
starts. The bullet now states that the amended key is a **best-effort minimisation** of the residual, not
a guarantee, and that a mass-suspension club is exactly the population that reaches it. The staging below
is unchanged and is still the only thing that deletes the residual. §2.3's mirror amended in the same
commit (`section-2.md` v0.13).)
**Last Updated (prior):** August 16, 2026 (v0.8 — **`ERR-044-019`**, adversarial-review H2, cross-filed at #30
as `ERR-030-044`: §7.2's ban-serving-under-squad-shortfall bullet described the extremis reinstatement
as though its only outcome were a stalled ban. #30 §3.4's probe is the FULL selection walk (eleven
starters PLUS the seven-slot bench), so the tier fires on bench depth too, and #30's amended within-tier
key now prefers a candidate the selector would BENCH — which splits the outcome in two: benched, the ban
advances normally; forced to start, the stage-1 exemption fires and only then does it stall. The bullet
now states both cases and names the staging below as what deletes the residual. §2.3's mirror of the same
claim corrected in the same commit (`section-2.md` v0.11))
**Last Updated (prior):** August 15, 2026, yet later still (v0.7 — `ERR-044-008`, reviewed-findings pass:
§7.3's `#37` seam-contract bullet still read "one per-tick tap feeds both consumers when both are
built", a future condition that has already occurred — both #37 and #44 have had `src/` assemblies
since July 27, 2026 — and turned out false: `src/discipline/IDisciplineTickLedgerTap.cs` records that
§4.1's reference rule makes #37's identical interface unreachable from either #44 or the composition
root. Restated to state the fact and its cost — two reads per tick, not a shared adapter)
**Last Updated (prior):** August 15, 2026, later still (v0.6 — L21, the spec half of #44's adversarial-review
round 4 (`open-issues.md`): §7.1's T1 bullet still read "outer bump coordinated — exact version TBD,
§4.4" — a placeholder left unresolved since §4.4 itself was corrected to the real `5 → 6` bump at its
own v0.3 (August 13, 2026, ERR-030-035). Filled in with the landed figure and citation, verified by
reading `section-4.md` §4.4 directly rather than copied from this file's own stale claim)
**Last Updated (prior):** August 15, 2026 (v0.5 — ERR-044-003 stage 1, owner decision: the ban-serving deferral
bullet resolved — the deferral queue was NOT chosen; the chosen answer is the exempt-the-appearance fix
(now LANDED, FR-DC-011 / `OnClubFixturePlayed`) plus two further staged tiers, both blocked — #42 Youth
has no `src/` assembly, and generated cover needs the packed `PlayerId` id space widened (#27
FR-SQ-010 / ERR-027-004))
**Last Updated (prior):** August 13, 2026, later same day (v0.4 — M11 + L6, adversarial review over the C1/C2
landing: §7.1's T2 bullet marked LANDED (except the migrate/drop hygiene, which did not land with
it); new §7.2 bullet records FR-DC-013's re-key/drop delivery has zero production call site and the
id-reuse hazard that lands with it; §7.3's #30 seam contract corrected from "null seam" to LIVE)
**Last Updated (prior):** August 13, 2026 (v0.3 — ERR-044-003, C1/C2 landing back-prop: the ban-serving
deferral bullet flagged as a now-LIVE owner decision — #30 §2.3 F9 makes a suspended player
reinstatable in extremis rather than an absolute bar — with the deferral queue recorded as the
designed alternative)
**Last Updated (prior):** July 24, 2026 (v0.2 — cross-set AR; prior v0.1 — initial)
**Version:** 0.10
**Status:** APPROVED

---

## 7.1 T-phase implementation plan (post-APPROVED)

- **T0** — `TacticalDirector.Discipline` assembly: `DisciplineState`, `DisciplineRules`
  (thresholds/serving), `Availability` (`IsAvailable`/`FilterAvailable`), `DisciplineConstants`.
  Inert until wired (nothing calls it — behaviour-preserving by construction).
- **T1** — `DisciplineSaveCodec` (`DISCIPLINE_SAVE_FORMAT_VERSION` = 1) + composition into #30's
  season save (outer `SEASON_SAVE_FORMAT_VERSION` bump **5 → 6**, landed at ERR-030-035, §4.4).
  Fail-loud gates (F3).
- **T2** — **LANDED August 13, 2026 (roadmap C1/C2), except the migrate/drop hygiene below.** the live
  wiring: the tap-fed `CardLedgerFold` around engine-resolved fixtures (the
  #37-class read); the **ERR-030-009 filter** at the resolve→configure seam; `OnClubFixturePlayed`
  serving on both resolution paths; the `Incoming`-id semantics verified against the live engine
  (KD-2's absorbed assumption re-checked; ERR-044-001 corrected what the verification found). **The
  re-key migration / retirement drop** hygiene on the FR-TX-022 hook / #28 lifecycle coordination did
  **not** land with the rest of T2 — `DisciplineRules.MigratePlayerId`/`DropPlayer` exist and are
  unit-tested but have zero production callers; see the §7.2 bullet below.
- **T3** — deep: **#43 competition partitions** (per-`CompetitionId` tallies + per-competition
  serving — a partition activation over the FR-DC-012 key); the **#30-owned quick-sim card
  synthesis** coordination (keyed draws on #30's `0x22` stream, evening the minimal coverage
  asymmetry — never a #44 stream); varying ban lengths by offence class.

## 7.2 Deferred (recorded, not built)

- **Quick-sim card synthesis** — #30-owned (its stream, its model); #44 folds whatever summary
  #30's model emits, unchanged.
- **Competition-scoped accumulation / cup-vs-league carry rules** — the #43 partition (FR-DC-012
  pre-shapes it); carry rules between competitions are a partition-policy table, deep.
- **Offence classes / varying ban lengths** — richer `CardIssuedEvent` interpretation (e.g.
  violent conduct vs two bookings) requires engine-side offence data that does not exist; deferred
  until the engine emits it.
- **Ban-serving under squad shortfall — RESOLVED, staged.** *(ERR-044-003, August 13, 2026, stage 1
  August 15, 2026.)* §2.3's F5 fail-loud below the 18-player floor was **withdrawn**: #30 §2.3 F9 /
  §3.4 (ERR-030-029, approved after this section) settles a depleted squad by back-filling the
  least-injured (now least-suspended too) players back in until the engine's own selector can field
  the formation, never refusing until even the whole squad cannot. That means a suspended player **is**
  reinstatable in extremis — suspension is a stricter reinstatement tier than injury (pressed back
  only after every injured player), but not an absolute bar, which the Laws of the Game do not allow.
  **`ERR-044-019` (August 16, 2026) corrects what "in extremis" means here, and what it costs.** The
  probe #30 §3.4 uses is the FULL selection walk — eleven starters PLUS the seven-slot bench — so the
  back-fill fires for **bench depth** as well, on a club that could field a legal XI; and the pre-fix
  within-tier key (earliest roster position) then let the rating-greedy selector **start** the
  reinstated man. #30's `ERR-030-044` amends that key to prefer a candidate the selector would BENCH,
  which splits this bullet's outcome into two cases that must not be collapsed: **benched**, he is not
  in the fielded eleven, so FR-DC-011's decrement is not exempted and **his ban advances normally**;
  **forced to start** — no candidate choice avoids the eleven — he plays, the stage-1 exemption fires,
  and **only then does his ban stall**. The staging below is what deletes that residual; it is not a
  general licence to field banned players for free.
  **`ERR-030-046` (August 16, 2026, ESCALATED) makes "forced to start" a GUARANTEE rather than a best
  effort — superseding `ERR-030-045`'s widening of this same residual.** Two successive within-tier
  ordering keys were landed at #30 §3.4 and both were defeated by the same class of defect: an
  **element-wise greedy decision of a set-valued constraint**. A depleted club is completed by a *set*
  of reinstatements; whether that set puts a banned player in the eleven is a property of the set; and
  `LineupSelector` decides it **per position**, which is why the second key — ascending selector rating,
  a global scalar — failed on a squad thin in the globally *weakest* banned player's position, pressing
  back exactly the man no fit player could displace. The third attempt was ruled rather than iterated:
  #30's rule is now a **capped exhaustive search** over subsets of the eligible candidates. What this
  bullet costs is therefore: **a reinstated-suspended player starts only in a probe-verified forced
  start — every completing choice within the search bound starts at least as many.** Equivalently the
  composed eleven contains the **minimum achievable** number of them, **zero whenever any completing
  choice benches them all**. The residual is exactly two items — **(i)** those forced starts, where the
  minimum is positive (a banned sole goalkeeper is the smallest case, a `k ≥ 2` shortfall in which every
  completing subset starts someone its generalisation), and **(ii)** a **beyond-cap corner** above
  `SeasonSaveConstants.EXTREMIS_SEARCH_CANDIDATE_CAP` = 12 concurrent suspended candidates at one club,
  unreachable at measured card rates, where the pass degrades to an ascending-rank greedy with no
  minimality claim and self-heals as each commit lowers the count. Stated as a **list, not as an
  enumeration of the routes into a forced start** — that form has now been falsified twice. A
  mass-suspension club, the population this spec's own subject creates, is exactly the one that reaches
  (i), and the staging below is still the only thing that deletes it.
  **The deferral queue this bullet used to record as the alternative (excess bans postpone serving
  until the squad can field 18, refusing the fixture rather than fielding a banned player) was NOT
  chosen.** The owner's decision (August 15, 2026) is a three-tier staging instead, of which the first
  tier is now **LANDED**: (1) an extremis appearance no longer serves the ban it was fielded through —
  `OnClubFixturePlayed` now takes the club's fielded eleven and exempts anyone in it (FR-DC-011,
  §3.3), so the reinstatement stays possible but is no longer free; (2) **youth call-ups** ahead of
  any suspended player — **blocked: #42 Youth has no `src/` assembly**; (3) **generated low-attribute
  cover** ahead of that, after which a banned man never reaches the pitch at all and the suspended
  tier becomes unreachable rather than merely costly — **blocked: the packed `PlayerId = clubId ×
  CLUB_SQUAD_SIZE + local` id space needs widening** (fully packed at 25; a 26th player for club N
  collides with club N+1's first — #27 FR-SQ-010 as amended by ERR-027-004). Tiers 2 and 3 are
  recorded here, unbuilt, as the eventual answer once their blockers clear.
- **FR-DC-013's re-key/drop delivery has no call site (M11, recorded at the adversarial review over
  the C1/C2 landing).** `DisciplineRules.MigratePlayerId` and `DropPlayer` are built and unit-tested
  but referenced by nothing outside `src/discipline/` — the T-phase plan above named the FR-TX-022
  roster-move hook as the delivery point, and #29/#41's own T2 roster-sync landed at exactly that
  point in `SeasonLoop.RollToNextSeason` (`PlayerCareerStates.CommitRosterSync`) without also
  wiring #44's re-key/drop, and #44 has no membership of its own to reconcile independently. Inert
  today, because #28's boundary regen (retiree removal + 1:1 replacement) is itself deferred — but
  the consequence when it lands is not an orphan discipline row: player ids are `clubId *
  CLUB_SQUAD_SIZE + localIndex` (#27), so a regen filling a retiree's vacated slot **inherits the
  identical id**, and with it — silently — the retiree's outstanding ban and yellow tally. Recorded
  at `SeasonLoop.RollToNextSeason`'s roster-sync call site in `src/season-save/SeasonLoop.cs` so
  #28's boundary landing cannot miss it.
- **Appeals / suspension psychology (#33)** — out of scope entirely at Stage 2.
- **Suspension screens (#38) / news items (#46)** — deferred consumers of the availability view
  and ban events (FR-LW-031).

## 7.3 Seam contracts recorded for downstream authors

- **#30:** the ERR-030-009 resolve→*filter*→configure seam is #44's insertion point — LIVE since T2
  (C1/C2, August 13, 2026), not the null seam it was at approval; serving is reported per played
  fixture on both resolution paths; the sub-blob rides `SeasonSaveCodec`.
- **#37:** #44 reads the engine's per-tick ledger through its own `IDisciplineTickLedgerTap`
  rather than #37's identically-shaped tap interface — both now have `src/` assemblies (since
  July 27, 2026), and §4.1's reference rule still makes #37's interface unreachable from either
  #44 or the composition root that owns the engine (`ERR-044-008`), so a shared adapter type is
  not achievable even now. Neither references the other; the engine's own tap is still filled once
  per tick, so the cost of two accessor shapes is two reads, not two behaviours.
- **#43:** partitions activate over the `(PlayerId, CompetitionId)` key; #43's `CompetitionId` on
  fixtures/results is the scoping input.
- **#31/#28:** the roster re-key/retirement events deliver the migrate/drop hygiene — bans follow
  the player (the recorded contrast with #32's drop rule).
- **#38 (future):** renders availability/suspension view models (read-only value copies); MUST
  NOT mutate `DisciplineState` directly.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §7 (T-phase plan T0–T3, deferred extensions, downstream seam contracts), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Cross-set AR (L): §7.2 gains the **ban-serving deferral under squad shortfall** row — the F5 <18 fail-loud is coherent today (the engine's own gate, verified) but the pile-up is reachable in principle; the deferral queue is the recorded deep mitigation. |
| 0.3 | 2026-08-13 | — | **ERR-044-003** (C1/C2 landing back-prop): the deferral bullet corrected — §2.3's F5 fail-loud it was written against no longer exists (withdrawn in favour of #30 §2.3 F9's back-fill), so the squad-shortfall question is no longer hypothetical: a suspended player is reinstatable in extremis today, and the deferral queue is recorded as the alternative if the owner prefers refusing the fixture instead. |
| 0.4 | 2026-08-13 | — | **M11 + L6** (adversarial review over the C1/C2 landing): §7.1's T2 bullet marked LANDED, with the migrate/drop hygiene split out as the one T2 item that did NOT land; new §7.2 bullet records FR-DC-013's re-key/drop delivery has zero production call site today and the id-reuse hazard a #28 boundary regen would hit; §7.3's #30 seam-contract bullet corrected from "null seam" (stale since T2) to LIVE. |
| 0.5 | 2026-08-15 | — | **ERR-044-003 stage 1**, owner decision: the ban-serving-under-squad-shortfall bullet resolved from "now a live decision" to RESOLVED — the deferral queue was NOT chosen; the chosen answer is a three-tier staging, of which tier 1 (exempt the extremis appearance from serving, FR-DC-011) is LANDED, and tiers 2 (youth call-ups) and 3 (generated cover) are recorded unbuilt with their blockers (#42 has no `src/` assembly; the packed `PlayerId` id space needs widening, #27 FR-SQ-010 / ERR-027-004). |
| 0.6 | 2026-08-15 | — | **L21** (#44 adversarial-review round 4, `open-issues.md`): §7.1's T1 bullet filled in the "exact version TBD" placeholder with the actual landed bump (`SEASON_SAVE_FORMAT_VERSION` 5 → 6, ERR-030-035) — `section-4.md` §4.4 has carried this figure since its own v0.3 (August 13, 2026), so the placeholder had been stale for two days. |
| 0.7 | 2026-08-15 | — | **`ERR-044-008`**, reviewed-findings pass: §7.3's `#37` bullet corrected — "one tap feeds both when built" is no longer a future condition (both assemblies exist) and was never going to become true under §4.1's reference rule, verified against `src/discipline/IDisciplineTickLedgerTap.cs`'s own recorded finding. Restated with the two-reads cost named. See `spec-error-log.md` `ERR-044-008`. |
| 0.8 | 2026-08-16 | — | **`ERR-044-019`** (adversarial review, H2; cross-filed at #30 as `ERR-030-044`, which owns the rule). §7.2's ban-serving-under-squad-shortfall bullet treated the extremis reinstatement as having a single outcome — a suspended player on the pitch whose ban then stalls. Two corrections. The back-fill's TRIGGER is #30 §3.4's probe, the full selection walk (eleven starters PLUS the seven-slot bench), so it fires on **bench depth** at a club that could field a legal XI — "in extremis" was never as narrow as this bullet implied. And #30's amended within-tier key now prefers a candidate the selector would BENCH, which splits the outcome: benched ⇒ he is not in the fielded eleven ⇒ FR-DC-011's decrement is not exempted ⇒ **his ban advances normally**; forced to start ⇒ exempt ⇒ the ban stalls. Only the second case is the residual the staged tiers below exist to delete, and the bullet now says so rather than reading as a general licence. The staging itself (stage 1 LANDED; youth call-ups and generated cover both blocked) and the NOT-chosen deferral queue are unchanged. §2.3's mirror corrected in the same commit (`section-2.md` v0.11). |
| 0.9 | 2026-08-16, later | — | **`ERR-030-045`** (an adversarially-reviewed High continuing `ERR-030-044`'s; cross-filed at #30 `section-3.md` v2.8, which owns the rule). §7.2's ban-serving-under-squad-shortfall bullet, as amended at v0.8, gave the forced-start case a single shape — "no candidate choice avoids the eleven, the sole-goalkeeper case". That is one of two. A club short by more than one player gets no usable probe on any reinstatement but the last (fieldability is monotone in adding players), so #30's amended pass-3 key decides those blind; it presses the weakest banned player back first, which makes the blind picks good but cannot make them safe — where every completing choice starts a suspended player, one starts. The bullet now records the key as a **best-effort minimisation** of the residual rather than a guarantee against it, and names the mass-suspension club as the population that reaches it — which is this spec's own subject. The staging (stage 1 LANDED; youth call-ups and generated cover both blocked) and the NOT-chosen deferral queue are unchanged, and remain the only thing that deletes the residual. §2.3's mirror amended in the same commit (`section-2.md` v0.13). |
| 0.10 | 2026-08-16, later still | — | **`ERR-044-019` EXTENDED (annotated, not re-filed)** by **`ERR-030-046`**, an ESCALATED High filed at #30 `section-3.md` v2.9, which owns the rule. §7.2's ban-serving-under-squad-shortfall bullet carried `ERR-030-045`'s account: the forced-start residual reached two ways, and #30's within-tier key a **best-effort minimisation** of it. #30's rule is no longer an ordering key at all. Two successive keys — earliest roster position, then ascending selector rating — were defeated by the same class of defect: an **element-wise greedy decision of a set-valued constraint**, since a depleted club is completed by a SET of reinstatements, whether that set starts a banned player is a property of the SET, and `LineupSelector` decides it **per position**. The rating key failed on a squad thin in the globally *weakest* banned player's position — exactly the man it presses back first. Ruled rather than iterated (the no-third-identical-retry rule): a **capped exhaustive search** over subsets of the eligible candidates. The bullet now states the result as a **guarantee** — *a reinstated-suspended player starts only in a probe-verified forced start; every completing choice within the search bound starts at least as many* — i.e. the composed eleven carries the MINIMUM achievable number of them, zero whenever any completing choice benches them all. Residual restated as a two-item **list**: (i) forced starts, where the minimum is positive (banned sole goalkeeper the smallest case, a `k >= 2` every-completing-choice-starts-someone shortfall its generalisation); (ii) the **beyond-cap corner** above `[FIXED] SeasonSaveConstants.EXTREMIS_SEARCH_CANDIDATE_CAP` = 12 concurrent suspended candidates, unreachable at measured card rates, degrading to ascending-rank greedy with no minimality claim and self-healing as each commit lowers the count. Deliberately NOT an enumeration of routes into a forced start — that form has been falsified twice. The three-tier staging (stage 1 LANDED; youth call-ups and generated cover both blocked) and the NOT-chosen deferral queue are unchanged, and remain the only thing that deletes item (i). §2.3's mirror amended in the same commit (`section-2.md` v0.17). |
#endregion
