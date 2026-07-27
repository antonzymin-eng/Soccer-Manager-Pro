# Club Infrastructure & Facilities #53 — Section 9: Approval Checklist

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.3 — APPROVED: R-01..R-05 sign-off granted; back-props filed atomically)
**Last Updated (prior):** July 27, 2026 (v0.2 — G1 CLOSED; PASS-1 + AR-2 recorded)
**Version:** 0.3
**Status:** APPROVED

---

## 9.1 Content completeness

- [x] §1 scope / out-of-scope table / dependencies + leaf DAG / KD-1..KD-9 / determinism posture / §1.6
      folded-in lessons / §1.7 the scoped identity claim.
- [x] §2 FR-IN-001..035 (incl. FR-IN-006a), data structures, failure modes F1..F8 (incl. F4a) and the
      explicit *"a day gap is not a failure mode"* note.
- [x] §3 FM-IN-01..05 with the pure predicate and its refuse-vs-throw distinction, the latch and its
      overflow guard, the cursor-free day advance, the two-convention projections, capacity, the §3.6
      no-division rule, and thirteen hand-verifiable worked examples.
- [x] §4 leaf assembly + reference direction with a CS0104 pre-check, file layout, the command-layer
      purchase sequence, the #30 slot seam, the root-assembled projection seams, save composition,
      neighbour contracts.
- [x] §5 test plan across identity / units / determinism / save / seams / the double-count lock /
      structural / fail-loud + the T-phase closed-loop scenario.
- [x] §6 loop classification (world tick only, no hot path), cost profile, `[GT]` budget ceilings.
- [x] §7 T0–T3 plan, deep-tier extensions, the not-planned list, risks R-1..R-6.
- [x] §8 XC-053-001..018 + the back-prop table + the not-a-back-prop list + §8.4's no-#16-row rationale.
- [x] Appendices A (constants), B (save layout), C (roster + dial-mapping table).

## 9.2 Constant-tag discipline

- [x] Every constant in Appendix A carries exactly **one** of `[FIXED]` / `[DERIVED]` / `[CROSS]` /
      `[CROSS-PENDING]` / `[GT]`.
- [x] No `[EST]` remains (none was introduced).
- [x] Empty regions omitted (#20 prohibits them) — #53 has no `[EST]` and no `[CROSS-PENDING]`
      constants, so neither region appears.
- [x] `[CROSS]` rows name their authority and are consumed read-only — #53 re-declares none of #42's,
      #41's, #28's or #27's types (T-IN-BOUND-002).
- [x] `[DERIVED]` rows document their formula: `FACILITY_TYPE_COUNT` and `FACILITY_LEVEL_SPAN_MAX` are
      both derived and neither is independently settable.
- [x] The `[GT]` magnitudes are declared **illustrative pending the T3 balance pass**, and §5 asserts
      only shape, identity and direction — never magnitude — so the balance pass cannot invalidate a
      passing suite.
- [x] **No `_RESERVED_` row is claimed in #16 §3.4** and the reason is documented (§8.4), rather than
      left as an apparent A-04 omission.

## 9.3 Verification of load-bearing claims (checked against source, not asserted)

- [x] #42 defines `AcademyQuality { CeilingShiftPerMille, CohortSizeDelta }` with `Neutral => default`
      (all-zero **is** the identity). *(`youth-academy-intake/section-2.md` §2.2, FR-YA-010)*
- [x] #42 FR-YA-009 makes `AcademyQuality` **caller-supplied**, with #42 referencing neither producer —
      the fact XC-053-002's "no #42 shape change" rests on.
- [x] #42 FR-YA-011 **fails loud** on an out-of-bounds dial, and `ACADEMY_CEILING_SHIFT_ABS_MAX = 300‰` —
      the bound §3.4 clamps against. *(`youth-academy-intake/section-2.md`, `appendices.md`)*
- [x] #42 `section-1.md` and `section-4.md` name *"#40 facility spend"* as an `AcademyQuality` input —
      the mis-attribution ERR-042-001 corrects.
- [x] #41 defines `MedicalModifier { OccurrenceRiskMillMult, RecoverySpeedMillMult }` with
      `Identity => new(1000, 1000)` as an **explicit factory**, and FR-MD-016 makes `default()` (all-zero)
      fail loud — the two facts KD-8 and §3.4's unmodelled-club return rest on.
      *(`injuries-medical/section-2.md` §2.2, FR-MD-016)*
- [x] #29 FR-TR-004/005: `ComputeTrainingInput` is **pure**, and **#29 is the sole writer** of #28's
      `TrainingInput` — *"it MUST NOT add a second path"*. The fact KD-9 rests on.
      *(`training-system/section-2.md`)*
- [x] #28 declares `TrainingInput` with `Neutral => default` and **no fields at Stage 0** — Stage-3 #29
      fields append. *(`player-progression-lifecycle/section-2.md` §2.2)*
- [x] #34 names *"#40 facilities"* as reaching shared consumers by their own separate seam — the
      mis-attribution ERR-034-001 corrects; the double-count rule itself is unchanged.
      *(`staff-backroom/section-1.md`, `section-3.md`)*
- [x] #28 `section-1.md` / `section-7.md` describe the academy structure *"(facilities → intake
      quality)"* **without naming an owner** — the gap ERR-028-002 closes.
- [x] **#40's approved spec contains no facility model:** a `grep` for `facilit` across
      `docs/specs/club-finances-economy/` returns nothing, and its outline scopes it to budgets, the wage
      ledger, revenue and FFP. This is the fact that makes §1.1 a **mis-assignment** rather than a pending
      deliverable, and therefore the fact the whole spec rests on.
- [x] #40 §7.2 defers a Stage-3 matchday-attendance accrual — the deferred-not-absent consumer that makes
      holding `StadiumCapacity` now a value rather than a phantom (§3.5).
- [x] #40's `ApplyTransaction` is the existing debit path the command layer sequences (§4.3).
- [x] #30's day-advance order is a **pinned sequence** whose two prior insertions (#41's
      `AdvanceMedicalDay`; #45's `ERR-030-008`, which renumbered `AdvanceDay` 8 → 9) both landed **at
      approval** — the precedent ERR-030-020 follows. *(`season-competition-loop/section-3.md` §3.3)*
- [x] `ERR-028-001` is filed and `-002` is next free; `ERR-040-001` is filed and `-002` is next free;
      `ERR-034-*` and `ERR-042-*` are entirely unfiled and unproposed.
      *(`docs/tracking/spec-error-log.md`)*
- [x] **`ERR-029-001` is filed and RESOLVED** (July 23, 2026, at #29's own approval) and **`-002` is
      soft-reserved by #34** — so #53's #29 back-prop is **`-003`**. *(`spec-error-log.md` v1.34;
      `docs/tracking/staff-backroom-design.md`)* — this is the PASS-1 M-1 correction.
- [x] Proposed `ERR-030-*` ids across the pre-promotion supplements reach `-019`; `-020` is #53's.
- [x] `FR-IN-*` is **unclaimed** — verified by enumerating every `FR-[A-Z]{2,3}-` prefix in
      `docs/specs/`; the 37 in use do not include `FR-IN`.
- [x] The roadmap §6 determinism block is **exactly full** at `0x20`–`0x2D` / 82–95, with `0x2E`–`0x2F` /
      96–97 held back as slack — the fact KD-6 and §8.4 rest on.
      *(`docs/tracking/management-layer-spec-roadmap.md` §6)*

## 9.4 Gates

| Gate | Owner | Status |
|---|---|---|
| **G1** — section-file PASS-1 adversarial review + a fix pass, to convergence. | drafter | ✅ **CLOSED** — see §9.4.1 |
| **G2** — file **ERR-034-001**, **ERR-042-001**, **ERR-028-002**, **ERR-040-002**, **ERR-029-003**, **ERR-030-020** atomically with the status flip. | drafter | ✅ **CLOSED** — filed and RESOLVED July 27, 2026, atomically with the flip (`spec-error-log.md` v1.47) |
| **G3** — lead-developer R-01..R-05 sign-off. | lead developer | ✅ **CLOSED** — R-01..R-05 granted by the lead developer, July 27, 2026 |
| **G4** — `SPEC_INDEX.md` registry row + Registry-Changes entry, added at promotion. | drafter | ✅ **CLOSED** — row + Registry-Changes entry landed July 27, 2026 |
| **G5** — `management-layer-spec-roadmap.md` row + §3 scope sketch + §7 wave placement for #53, and `spec-plans/spec-53-club-infrastructure-facilities.md` — the v0.2 gap-fill / v0.4 Amendment-01 precedent for adding a **new** candidate number. | drafter | ✅ **CLOSED** — roadmap v0.7 + registry row landed July 27, 2026 |

**Not gating (deferred by design, recorded so they are not mistaken for omissions):** the outer
`SEASON_SAVE_FORMAT_VERSION` bump (T2); #41's and #40's consumption of non-identity values (each at its
own Stage-3 tier); the `ScoutingInfrastructure` roster append (gated on #32 declaring a dial); and the T3
`[GT]` balance pass (§A.4).

**G5 is specific to #53 and has no analogue in the sibling promotions.** #53 is a **new** candidate
number proposed by its own supplement rather than one the roadmap already carried, so the governance rows
that other specs inherited must land with it.

### 9.4.1 PASS-1 adversarial review record (G1)

**PASS-1: 0H + 6M + 8L, all resolved in the v0.2 fix pass.** The M findings clustered in two places —
the identity conventions and the split-surface safety argument — which is where a spec whose whole value
proposition is *"it fits the seams that already exist"* is most likely to be wrong.

| # | Sev | Finding | Resolution |
|---|---|---|---|
| M-1 | M | **`ERR-029-001` is already filed and RESOLVED**, and `-002` is soft-reserved by #34. The supplement proposed `-001` for #53's #29 back-prop; filing against a used id is exactly the collision the project's numbering discipline exists to prevent, and it would have landed silently because nothing cross-checks a proposed id. | Renumbered to **ERR-029-003**, with both facts verified at source and recorded in §9.3. `outline.md` / `section-1.md` / `section-8.md` v0.2. |
| M-2 | M | **The training-ground term cannot be a `TrainingInput`.** The supplement's §4 table routed #53's `TrainingGround` level into #28's `TrainingInput`, but #29 FR-TR-005 makes **#29 the sole writer** of that type — *"it MUST NOT add a second path"*. A #53-returned `TrainingInput` would be precisely the second path, and functionally the same double-source defect KD-4 rules out one paragraph earlier. | New **KD-9** + **FR-IN-024**: the term is a root-assembled *input* to `ComputeTrainingInput`, alongside #34's `CoachingModifier`; #29 folds both into the single `TrainingInput` it emits. Back-prop **ERR-029-003** files the parameter. |
| M-3 | M | **The combination point is the root, not the consumer.** KD-4 said the *consumer* owns how #34's and #53's terms combine — but #41 takes a single already-assembled `MedicalModifier` and #42 a single already-assembled `AcademyQuality`. Neither consumer ever *sees* two sources, so neither **can** own the combination, and a reader implementing KD-4 literally would have to add a second parameter to two approved specs. | KD-4 and §4.5 corrected to the **composition root**, which is what #42's and #41's own approved seams already specify. #29 is named as the one exception, and only because it is itself a producer (M-2). |
| M-4 | M | **`default(ClubFacilities)` is a live-build state that no range check can catch.** Its `InProgressFacility` is `0` — a **valid** `FacilityType` ordinal (`TrainingGround`) — with `TargetLevel = 0` and `CompletionWorldDay = 0`, so the next advance would "complete" a build by setting the training ground to level `0`. The level fields are caught by F1; this field is not. | New **FR-IN-006a** + **F4a**: the enforced guard is at **record insertion**, following #45's F4a and #33's FR-HS-005. Locked by T-IN-FAIL-002, which must assert at the insertion seam specifically. |
| M-5 | M | **`StartUpgrade` had no re-validation**, so the split surface's entire safety argument rested on the unstated premise that nothing mutates #53 between the check and the latch. If that premise is ever broken — a second command in one tick, a future concurrent-build tier — a build starts from a stale check **silently**, which is worse than the debit-first ordering KD-1 rejects. | New **FR-IN-013** + **F6**: the latch re-runs the predicate and throws. The premise is now *defended* rather than assumed, and §4.3 states it explicitly. Locked by T-IN-I-009. |
| M-6 | M | **`ProjectMedicalModifier` for an unmodelled club returned `default`**, which for #41 is an **all-zero, ×0 recovery multiplier** that FR-MD-016 rejects fail-loud. A *legal* absent-club case would have become a hard failure at a neighbour's seam — and the plausible "return default for absent" simplification is how it would have been written. | Returns **`MedicalModifier.Identity`**. Generalised into new **KD-8**: the consumer dials genuinely carry **two** identity conventions (zero-identity for `AcademyQuality`/`TrainingInput`, 1000-per-mille with a fail-loud `default()` for `MedicalModifier`), so a single unified convention is impossible, not merely inconvenient. Locked by T-IN-ID-005. |
| L-1 | L | No overflow guard on `worldDay + days`; a wrapped `uint` yields a completion day in the **past** and a build that completes instantly. | `RequireNoOverflow` added (§3.2) + T-IN-FAIL-008. Also recorded why `uint.MaxValue` cannot be a #53 sentinel, unlike the sibling specs' *last-advanced* cursors. |
| L-2 | L | §3.1's pseudocode was ambiguous about refuse-vs-throw: a corrupt stored level returning `false` would present a data-integrity bug as an ordinary *"you can't build that"*. | The distinction is now explicit, with worked example (m) locking it and T-IN-FAIL-001 asserting it. |
| L-3 | L | **KD-7 existed only as an absence.** #53 carries no `LastAdvancedWorldDay` cursor while four sibling specs do; nothing said why. The predictable outcome is a later "consistency fix" adding a gap guard — which would then **fail loud on a legitimate multi-day advance**, a regression dressed as an improvement. | Promoted to **KD-7** with both properties argued, **FR-IN-018** stating them, and **T-IN-U-016** asserting the field's absence *structurally* so the fix cannot land silently. §2.3 also lists the day gap as a deliberate **non**-failure-mode. |
| L-4 | L | The `AcademyQuality` clamp was against a #53 bound; #42 fails loud on an out-of-bounds dial, so #53 could produce a value its own consumer rejects. | Clamped against **#42's** `ACADEMY_CEILING_SHIFT_ABS_MAX` (XC-053-003), with T-IN-U-007 sweeping the worst case. |
| L-5 | L | **Where the upgrade price lives was unstated** — not #53 (no currency), not #40 (no facility price list). An unstated third quantity is how a fourth spec ends up owning a price list nobody assigned. | §4.3 names it: the command layer's `[GT]` table, beside the command handler. |
| L-6 | L | #16 getting **no row at all** looks like a missed A-04 placeholder next to seven sibling `_RESERVED_` rows. | New **§8.4** explains it: A-04 is a rule about gaps in the *allocated sequence*, and #53 is not in that sequence; filing one would consume the roadmap's held-back slack **and** assert an expectation FR-IN-031 deliberately leaves open. |
| L-7 | L | The identity claim read as unqualified, which is false from T2 (the save frame gains #53's sub-blob) and false for `Stadium` (an absolute value, not a deviation dial). | §1.7 scopes it on both axes; T-IN-ID-004 is scoped to T0/T1 accordingly. |
| L-8 | L | No lock made the APPEND-only ordinal contract or the roster-append shape mechanically enforceable. | Added **T-IN-I-005** (ordinal stability + member count) and **F8 / T-IN-I-006 / T-IN-FAIL-009** (a `Levels` length mismatch — the shape a roster append against an un-bumped version produces). |

**AR-2 sweep: 0H + 0M + 3L, all resolved — CONVERGENCE** (an L-only round closes the cycle, per the
project convention). **L-1:** §6.2's cost profile did not note that the completion branch is the **rare**
one, which is the performance half of KD-3's argument and the reason the dated latch is also the cheapest
daily step. **L-2:** §7.2's *"multiple grounds"* and *"seed-varied genesis"* extensions were listed
alongside additive ones without flagging that both are **structural** — a keying change and a
generation-version enrolment respectively — so a future reader could have taken either as a tweak.
**L-3:** §5's double-count lock (T-IN-I-012) asserted independence but nothing prevented a future
*overload* from accepting a #34 type as an input, which is how the path would actually be reopened;
**T-IN-I-014** now asserts over the public surface.

## 9.5 Sign-off

| Role | Criterion | Signed |
|---|---|---|
| R-01 | Scope and out-of-scope boundaries are unambiguous; no model #53 does not own is duplicated, and the #40 reconciliation is explicit rather than implied. | ⏳ pending |
| R-02 | Every formula has units, ranges, and at least one worked example; no fabricated verification values. | ⏳ pending |
| R-03 | Determinism posture is complete: the draw-free claim, the genesis-uniformity property, the `WORLD_GENERATION_VERSION` exclusion, and the absence of both an RNG cursor and an idempotency cursor are each justified rather than asserted. | ⏳ pending |
| R-04 | Persistence is version-gated, opaque, fail-loud, APPEND-only, and bumps no format version it does not own. | ⏳ pending |
| R-05 | Cross-spec back-props are enumerated with owners and timing, every proposed ERR id is verified free, and the one genuine surface addition (ERR-029-003) is called out rather than presented as a pointer fix. | ⏳ pending |

## 9.6 Decision

**APPROVED — July 27, 2026.** Lead-developer **R-01..R-05 sign-off granted**, and the back-props filed and RESOLVED **atomically with the flip** per this spec's own promotion pipeline step 6: **ERR-034-001**, **ERR-042-001**, **ERR-028-002**, **ERR-040-002**, **ERR-029-003**, **ERR-030-020** (`spec-error-log.md` v1.47). All 11 section files carry `Status: APPROVED`; the `SPEC_INDEX.md` row records the date.

**What approval does and does not mean here.** It approves the **forward design** — the #21–#30 pre-T0 precedent — not an implementation: #53 has **no `src/` assembly**, and its §7 T-phase plan is the sequence for building one. Items listed as *not gating* above remain open by design and are named at their tiers.

**The prior decision text is retained below, because the reasoning it records is what the sign-off was granted against.**

**(prior, recorded at `IN REVIEW`)** — G1 closed (PASS-1 0H+6M+8L → AR-2 0H+0M+3L convergence, §9.4.1). G2–G5 remain open by
design: back-props land atomically with the status flip, sign-off is a human authority, and the registry
and roadmap rows are added at promotion.

**The claim this spec rests on, restated at the decision point.** #53 is worth opening not because the
master plan names infrastructure, but because **four approved specs already consume a facility model and
all four attribute it to #40, whose own scope excludes it** — verified by source in §9.3. Nothing is
broken today, because every one of those consumers was built to the neutral value-input pattern. The
failure mode is slower and worse: four Stage-3 tiers each finding the dial still neutral and improvising,
which is the parallel-surface trap this project has already hit three times.

**The one genuine surface addition** is ERR-029-003 — one root-assembled input parameter on #29's
`ComputeTrainingInput` at its Stage-3 tier. Every other back-prop is a doc-only pointer fix. That
asymmetry is the honest summary of what #53 costs the specs around it.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §9 (completeness, tag discipline, the §9.3 source-verified claims table, the five gates incl. the #53-specific G5, R-01..R-05). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | G1 CLOSED: §9.4.1 records the section-file PASS-1 (0H+6M+8L, all resolved) and the AR-2 convergence sweep (0H+0M+3L). §9.1 completeness updated for FR-IN-006a / F4a / F8 and KD-7..KD-9; §9.2 gained the no-`_RESERVED_`-row line; §9.3 gained the verified `ERR-029-001`-is-taken row (the PASS-1 M-1 correction), the `FR-IN` prefix check, and the #29/#28/#41 source rows the new KDs rest on. G2–G5 remain open. |
| 0.3 | 2026-07-27 | — | **`IN REVIEW → APPROVED`.** Lead-developer R-01..R-05 sign-off granted. Back-props **ERR-034-001**, **ERR-042-001**, **ERR-028-002**, **ERR-040-002**, **ERR-029-003**, **ERR-030-020** filed and RESOLVED atomically with the flip (`spec-error-log.md` v1.47). Gates G2–G5 closed; §9.6 decision updated. All 11 section files flip to `Status: APPROVED`. |
#endregion
