# Staff & Backroom #34 — Section 1: Introduction

**Created:** July 23, 2026
**Last Updated:** July 27, 2026 (v0.2 — back-prop landed atomically with the ten-spec approval wave; see the version-history row)
**Last Updated (prior):** July 23, 2026 (v0.1 — initial)
**Version:** 0.2
**Status:** APPROVED

---

## 1.1 Scope

The **backroom staffing layer**: coaches, scouts, and physios as **attributed entities** with roles/skills
and (deep) hiring, that **modulate** the systems they support. All #34 state advances on the **world tick**
(`WorldClock`, one day = one `worldTick` — never the 10 Hz/60 Hz match loops, living-world KD-4), is
constrained by **#40's club budgets** (staff wages), and is persisted alongside **#30's season/career save**.

**Identity scaffold (always present, behaviour-neutral)** = the managed club holds a **real neutral-baseline
staff roster** (all-neutral `StaffAttributes`) filling per-club **role slots**, and #34's **projections** map
that roster to each consumer's **exact identity** (`MedicalModifier.Identity`, `CoachingModifier.Identity`,
`staffMult = 1000‰`, `MentoringPlan.None`), so #29/#41 tick byte-identical to pre-#34 and the only new state
is #34's own save sub-blob. **No hiring, no candidate pool, no wage post, no draw** — `0x26`/88 stays
reserved.

**Stage-3 deep** = real staff with **attribute-derived, non-identity** projections (a distinct-staff club
diverges deterministically), a **stochastic candidate pool** (the first draw), **hiring** via the #31
negotiation pattern with **`StaffWage`** posts + a `WageBudget` gate, **#33 judgement**, and the **#29
`CoachingModifier`** field-shape — all on **one code path**, each defaulting to its scaffold identity via
`deepStaffEnabled`.

**#34 supplies only the staff-quality input; it never owns the models it feeds.** The training model is
#29's, the injury/recovery model #41's, the mentoring model #33's, the valuation model #31's — #34 produces
the **modifier** each already reads and adds **no second path** into any of them.

## 1.2 Out of scope (owned elsewhere, referenced as seams)

- **The training model (#29 Training System).** #29 owns `AdvanceTrainingDay`/`ComputeTrainingInput`; #34
  produces the `CoachingModifier` #29 already takes `in` (default `Identity`). #34 **MUST NOT** add a second
  training-effectiveness path (#29 §7 KD-3).
- **The injury/medical model (#41 Injuries & Medical).** #41 owns `AdvanceMedicalDay`/`ComputeInjuryRisk`;
  #34 produces the `MedicalModifier` #41 already takes `in` (default `Identity`). #34 **MUST NOT** add a
  second occurrence-risk or recovery-speed path (#41 §7 KD-5).
- **Player development (#28 Player Progression & Lifecycle).** #28 is **schema-untouched**: coaching reaches
  growth **only** through #29 (`CoachingModifier → TrainingInput → #28`), never a direct #34→#28 seam.
- **The economy (#40 Club Finances & Economy).** #40 owns budgets/wages; #34 posts staff wages **only**
  through `ApplyTransaction` (`LineItem = StaffWage`, deep) and reads `WageBudget`/`WageBillAggregate`
  read-only. #34 **MUST NOT** write `ClubFinances` fields or hold a parallel wage total (FR-FN-015).
- **The morale/personality model (#33 Personalities, Morale & Squad Dynamics).** #34 produces the
  `MentoringPlan` override #33 already reads (default `MentoringPlan.None`, FR-HS-022) and reads
  `MoraleOf`/`PersonalityProfile` read-only (deep). No morale path is built.
- **The negotiation seam (#31 Transfers, Contracts & Negotiation).** #31 owns the reusable
  `NegotiationOutcome` + the valuation-driven accept/reject **pattern**; #34 reuses that enum and pattern for
  hiring (deep) but authors a thin staff-specific offer/evaluator (KD-1). #34 is also the producer of #31's
  own deferred `staffMult` (×1000‰ identity today).
- **Scouting / fog-of-war (#32 Scouting & Player Knowledge).** #32 does **not exist yet**; #34 publishes a
  scout-quality projection #32 will consume (scouts are staff) and builds no scouting (FR-LW-031).
- **The season loop + save codec + tick order (#30 Season & Competition Loop).** #30 owns
  `RunWorldTickInFixedOrder`, `SeasonSaveCodec`, and the outer `SEASON_SAVE_FORMAT_VERSION`; it **invokes**
  #34 at a new pre-declared tick-order slot and composes #34's opaque save sub-blob. #34 never references #30.

## 1.3 Dependencies

**Upstream (needs):** #41 (`MedicalModifier` seam), #29 (`CoachingModifier` seam), #40 (staff-wage constraint
+ commit path), #30 (day-advance loop, season-save root), #16 (determinism namespace, deep draws). **Deep-tier
upstream:** #33 (`MentoringPlan` override; morale/personality read), #31 (`NegotiationOutcome` reuse).

**Downstream (consumers, deferred — no interface built, FR-LW-031):** #32 (scouts are staff — scout-quality
projection), #42 (academy coaching → intake quality), #31 (the `staffMult` #31 already reserves), #33 (the
`MentoringPlan` #33 already reserves).

Reference DAG: `compositionRoot → {#30, #34}`, `#34 → {#41, #29, #33, #31, #40, #16}` (scaffold subset
`{#41, #29, #16}`). **Acyclic.** #34 does **not** reference #30, #27, or #28.

## 1.4 Key decisions

- **KD-1 (hiring reuses #31's pattern + enum, not its `Offer` struct).** The offer/response **pattern** and
  the `NegotiationOutcome` enum are genuinely generic and reused; the **`Offer` struct is not**, because
  #31's `EvaluateOffer` tests **`Fee`** and `Offer` carries `CounterpartyClubId`/`IsBuy`/transfer-`PlayerId`,
  whereas a staff hire negotiates a **wage** (accept iff `offeredWage ≥ wageDemand`), with no fee, no selling
  club, no buy/sell duality. #34 authors a thin `StaffOffer` + `EvaluateStaffOffer`. All hiring is deep-tier
  and **year-round** (no transfer-style window). This is the reuse-vs-parallel fork resolved: reuse the
  pattern + type, parallel the wage-shaped data + predicate.
- **KD-2 (a fresh staff data layer, organised as role slots).** `StaffRecord`/`StaffAttributes` are new
  #34-owned value types mirroring #27's record **shape** but with a **distinct staff-skill vocabulary** (not
  #27's 31 player attributes). The per-club store is a set of **role slots** (one `StaffRecord` per role);
  each consumer reads its assigned slot-holder. Staff are keyed by a **stable, serialized, monotonic
  `StaffId`** (`StaffState.NextStaffId` high-water, never reused).
- **KD-3 (projections into each consumer's own identity type).** #34's quality projections **return each
  consumer's pre-existing identity type**, so the neutral baseline is exactly that type's `Identity` and #34
  invents no multiplier convention. #34's projection is the **sole** staff path into each consumer (FR-MD /
  FR-TR forbid a second path), so staff modulation cannot double-count with #33 morale or **#53 facilities**.
  *(ERR-034-001, at #53's approval: this read "#40 facilities". **#53** owns facility levels; #40 owns the
  funding. The double-count rule is unchanged and still correct — only the producer's identity was wrong.)*
- **KD-4 (persistence + determinism — one season-save sub-blob, draw-free scaffold).**
  `STAFF_SAVE_FORMAT_VERSION` [FIXED] = 1 opaque sub-blob composed into `SeasonSaveCodec` (the #41/#33
  precedent) — **not** a `WORLD_STORE_FORMAT_VERSION` bump. The scaffold is draw-free, so `0x26`/88 stays
  `_RESERVED_0x26_`; deep candidate-pool draws are position-independent keyed draws on `(clubId, worldDay,
  purpose)`, no serialized cursor.
- **KD-5 (the neutral baseline is a real entity, not an absence sentinel).** An unfilled slot is a **real
  neutral-baseline house-staff `StaffRecord`** projecting each consumer's explicit `Identity` — mandated by
  the existing seams (#41/#29 take `in`-modifiers defaulting to `Identity`; they never branch on "no staff").
  `default(StaffRecord)`/`default(StaffAttributes)` all-zero fails loud (F4).
- **KD-6 (staff wages — deep; the gate reads #40's running wage bill, not a #34 counter).** The scaffold posts
  no `StaffWage`, so FR-FN-015 (`WageBillAggregate ≡ 0` at Stage 2) is preserved verbatim, no #40 back-prop at
  approval. The deep affordability gate is `WageBillAggregate + wage ≤ WageBudget` (both read from #40) — #34
  keeps **no** wage counter, because a `committedStaffWage` accumulator would be exactly the parallel wage
  total FR-FN-015 forbids (unlike #31's transfer-fee counter against the *static* `TransferBudget`).
- **KD-7 (staff ownership — stable `StaffId`, no re-key, no #30 roster-commit).** #34 owns the staff store
  entirely; a staff member has a **stable `StaffId`** and a **mutable `EmployerClubId`**; a hire changes the
  employer field within #34's state, so there is **no re-key, no cross-system migration hook, and no #30
  roster-commit** — a deliberate, simpler divergence from #31's KD-7 (players re-key because `PlayerId` is
  club-scoped and #28/#33/#41 key by it; staff are not in #27's `Squad`).
- **KD-8 (behaviour-neutral identity + the reserve-ahead tick slot + the command boundary).** A
  neutral-baseline-staff season is byte-identical to pre-#34 (identity projections; no stream registered);
  the one approval-time back-prop is the reserve-ahead #30 staff tick-order null seam (ERR-030-006); a
  manager-initiated hire is an explicit command (`HireStaff`, the `SetTeamTactic` discipline, deep).

## 1.5 Determinism & coordinate posture

All arithmetic is **integer** (staff attributes `[1,20]`; projections per-mille `int`; wages `long`). There
is **no float in #34**. All state advances on the world clock at #30's pre-declared slot; the scaffold is
draw-free (KD-4). This is the #40/#41/#31 off-pitch integer + world-tick posture.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §1 (scope, out-of-scope seams, dependencies, KD-1..KD-8, determinism posture), promoted from design supplement v0.4. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | **ERR-034-001** (at #53's approval): the double-count rule's third producer re-attributed *"#40 facilities"* → **#53**. #40 funds facilities; #53 owns their level. The rule itself is unchanged and was always correct — only the producer's identity was wrong. |
#endregion
