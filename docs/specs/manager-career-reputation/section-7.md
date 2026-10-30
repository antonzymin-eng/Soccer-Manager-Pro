# Manager Career, Reputation & Job Market #54 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 27, 2026
**Last Updated:** October 30, 2026 (v0.2 — §7.5 records implementation-time findings under roadmap C6; prior: July 27, 2026 (v0.1 — initial section-file set))
**Version:** 0.2
**Status:** APPROVED

---

## 7.1 T-phase plan

| Phase | Content | Behaviour |
|---|---|---|
| **T0** | The assembly + `EndReason` + `Tenure` + `ManagerCareer` + `CareerStore` (`Appoint` / `Terminate`) + the pure `TenureRule` + `ReputationProjection`, and their tests — **including the structural no-reputation-field lock** (T-MC-BOUND-001). Nothing wired into #30. | **Inert** — no caller exists |
| **T1** | `CareerSaveCodec` + the round-trip / fail-loud / coherence suite. Still not composed into the season save. | **Inert** |
| **T2** | **First non-inert phase, and the one that carries the cost.** Wire `EvaluateTenure` at #30's tenure slot; compose the career block (bumps `SEASON_SAVE_FORMAT_VERSION`); land **ERR-030-021's optional `ManagedClubId`** (bumps `SEASON_STATE_FORMAT_VERSION`); wire the command-layer appointment join. | **Live.** A sacking now happens and is survivable — but with no vacancy source there is nothing to be appointed to (§7.2) |
| **T3 (S3)** | Vacancies generated from club state, interest and offers, moving clubs mid-career. **This is where the `0x2E` draw likely becomes real** and `_RESERVED_0x2E_` promotes. | **Named activation** — #54's first stochastic surface |

**T2 is where two format versions move at once**, and that is the sequencing fact worth planning around:
the outer `SEASON_SAVE_FORMAT_VERSION` (the block is composed in) and #30's
`SEASON_STATE_FORMAT_VERSION` (`ManagedClubId` becomes optional). **#45's `ERR-030-009` already queues a
bump on the same #30 block**, so the recommendation is to land both #30-side changes in **one** version
step — one #50 registry row, **one refusal boundary for existing saves** instead of two (§7.4 R-2).

**The honest statement of what T2 delivers** is *"the save survives a sacking"*, **not** *"the player
continues after one"*. With no vacancy source until T3, an unemployed manager has nowhere to go. That is
still the right thing to ship — it makes the missing floor exist, and #45's confidence and #30's objective
already produce the *inputs* to a termination decision every season that today flow into nothing — but
overselling it is how a tier gets called finished.

## 7.2 Deep-tier extensions (designed for, not built)

- **The job market** (T3/S3): vacancies from club state, interest, offers, and moving clubs mid-career.
  This is what turns *"survives a sacking"* into *"continues after one"*, and it is the first place a
  #54-owned draw appears.
- **Rival managers as entities** (S5), via **#22's phase-5 `BackgroundTierSim`** — the seam that is
  deliberately unbuilt because it *"summarises club-AI / transfer / **sacking** outcomes that do not exist
  yet."* When it lands, #54's vacancy **source** is replaced behind an unchanged surface (FR-MC-023): a
  producer swap, not a redesign.
- **Manager personality** via #33 (S5) — arriving as routed values, never a reference, and never by making
  a manager a player record.
- **International appointments** alongside **#36** (S5) — a national-team job is a tenure whose `ClubId`
  is a `NationTeamId` from #36's disjoint reserved range, which is one reason that range is disjoint.
- **Reputation as a #31 negotiation input** — a **value** the root routes, never a reference. Recorded so
  it is not implemented as a #31 → #54 dependency.
- **Reputation persistence beyond a single career** — #22's §7 anticipates it, and the APPEND-only career
  record is the durable substrate it needs (KD-2). No #54 change; a #22 read.

## 7.3 Explicitly not planned

- **A stored reputation scalar.** Not at any tier, not as a cache, not "for performance" (FR-MC-013). §6.2
  costs the recomputation precisely so the performance argument cannot be made without numbers.
- **A #54 write into #45's store.** The appointment join is the **command layer's** (FR-MC-016). Giving
  #54 the write would break its leaf position and #45's one-directional guarantee in one move.
- **Rival manager entities at any tier #54 owns** (FR-MC-020/021). Inventing them to make S3 vacancies
  feel alive would build the consumer #22's phase-5 is meant to **produce** — the phantom rule, in the
  exact place the project already documented it.
- **Ending the career on a sacking.** It is simpler and needs no #30 change, and it makes the game's
  answer to its own most dramatic event *"load your last save"* — and #45's whole confidence model a
  countdown to a game-over screen.
- **A `-1` sentinel for the unemployed state.** An explicit optional, so the compiler enumerates the read
  sites (KD-5 / FR-MC-034).
- **Manager attributes, coaching badges, or a media-relationship model.** All attach naturally to a career
  spec and none of them is one — see R-3.
- **Reusing `ManagerProfile` / `ManagerMode`.** #26 owns both, for something else (FR-MC-007).

## 7.4 Risks carried

- **R-1 — the unowned MUST is the reason to act.** `FR-BD-012` will otherwise keep pointing at #30, and
  the first person to implement #45's confidence will look for the sacking rule, not find it, and **put
  one somewhere convenient**. A MUST that names the wrong spec is worse than one that names none, because
  it reads as settled.
- **R-2 — KD-5's format bump is the expensive part**, and it lands on a block that **already has a queued
  bump** (`ERR-030-009`'s `JobSecurity` change). Combining them is the recommendation; if the tiers cannot
  align, the cost is **two refusal boundaries** for existing saves, which #50 handles but players
  experience. Re-verify the sequencing at T2 rather than assuming the July-2026 reading still holds.
- **R-3 — scope creep toward "career mode as a whole".** Manager attributes, coaching badges, media
  relationships and international jobs all attach naturally to this spec. §1.2 holds the line at **tenure
  + record/reputation + job market**, and it should be re-checked at each review, because every one of
  those will feel like it belongs here.
- **R-4 — rival managers are the tempting shortcut** (KD-3). The pressure arrives at T3, when vacancies
  exist and feel empty without someone to have vacated them. T-MC-BOUND-006 is the mechanical defence.
- **R-5 — the reserved slack is finite.** #54 claiming `0x2E` at S3 leaves `0x2F` / 97 as the **last**
  slot in the roadmap's determinism block. That is a fact for the roadmap to carry, not a reason to avoid
  the tag when a real draw exists — but it should be recorded at the promotion, not discovered afterwards.
- **R-6 — the appointment join is the one place a foreign write can hide.** #54 references nothing, so its
  own graph proves it writes nothing — but the **command-layer join** holds both #54 and #45, and a join
  that default-constructed a `BoardConfidence` or inherited the predecessor's standing would be invisible
  to #54's own tests. T-MC-I-002/003/004 assert the appointment path behaviourally for that reason.

## 7.5 Findings recorded against implemented code (roadmap C6, October 30, 2026)

Recorded, not applied. This spec has no `src/` assembly, so under roadmap C6 these findings are discharged
in the landing named in each bullet, in the same commit as the code they govern. Found by the October 30,
2026 wide check of assembly-less specs against implemented code on `main`. The normative text above is
unchanged.

- **Tag `0x2E` is taken (ERR-054-001).** KD-6, FR-MC-025, Appendix A and §7.1 T3 / R-5 reserve `0x2E` / 96
  for the S3 job-market draw. The FR-MC-025 placeholder never landed in #16 §3.4, and ERR-016-009 has since
  allocated `0x2E` to `DOMAIN_TAG_BUILD_IDENTITY`. Ordinal 96 is unaffected. Reconcile at #54's T0, before
  any draw: choose a free tag and land its placeholder in #16 §3.4 in the same commit. The build-identity tag
  does not move.

- **Skip tenure evaluation when no tenure is open.** After `Terminate`, `CurrentTenure = MC_UNEMPLOYED` and
  `EvaluateTenure`'s `RequireOpenTenure` (F2) throws. Slot 11 runs every day, and twice for the same day on
  fixture days (ERR-030-027). The T2 slot driver MUST skip evaluation whenever no tenure is open — on every
  later day, not only the second same-day call — with a test.

- **Save-block integration (#30 Appendix B rules added after this spec's approval).** From the phase that
  composes this block into the season frame (**T2**):
  (a) a guard against overwriting a populated destination block (ERR-030-036), keyed **per family**: on
  emptiness only if a destination that once carried this state can never legitimately present as empty
  (the #28 / #40 case); otherwise on an explicit wiring fact passed through **every** public write entry point
  (the #44 case, ERR-030-038 / ERR-030-039). Provisional reading: the tenure list is append-only and never empties after the first
  appointment, which suggests the emptiness key. Confirm the classification against Appendix B at landing.
  (b) A `SeasonLoop.Restore` resume parameter as soon as the frame can carry populated state (ERR-030-050).
  (c) Where the block is club-keyed, the equivalent of #40's current-season club-universe coherence rule
  (ERR-030-050).
  (d) Composition in `client-app`'s `CareerSession` (`StartNew` / `Resume`, landed October 18, 2026) as well as
  in `SeasonLoop` — a second composition root this plan predates.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §7 (T0–T3 with T2 identified as the phase that carries **two** #30-side format bumps and the recommendation to combine them with `ERR-030-009`'s; the honest statement of what T2 delivers — *"survives a sacking"*, not *"continues after one"*; deep-tier extensions incl. the #36 national-team tenure and the #22 reputation-persistence read; the not-planned list; risks R-1..R-6, with R-6 scoping the foreign-write risk to the command-layer join since #54's own graph proves the rest). Status IN REVIEW. |
| 0.2 | 2026-10-30 | — | §7.5: C6 findings recorded from the wide check against implemented code (0x2E tag, unemployed tenure evaluation, save-block integration). No normative text changed. |
#endregion
