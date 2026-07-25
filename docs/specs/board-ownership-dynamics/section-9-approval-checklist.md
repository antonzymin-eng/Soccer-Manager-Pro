# Board & Ownership Dynamics #45 — Section 9: Approval Checklist

**Created:** July 25, 2026
**Last Updated:** July 25, 2026 (v0.2 — G1 closed: PASS-1 + AR-2 recorded)
**Version:** 0.2
**Status:** IN REVIEW

---

## 9.1 Content completeness

- [x] §1 scope / out-of-scope table / dependencies + acyclic DAG / KD-1..KD-8 / determinism posture /
      §1.6 folded-in lessons.
- [x] §2 FR-BD-001..030 **+ FR-BD-005a**, data structures, failure modes F1..F7 **+ F4a**.
- [x] §3 FM-BD-01..05 with the daily step, the target assembly (and why the obvious alternative is
      wrong), the #40 projection with its overflow argument, the band derivation, the deferred keyed
      takeover draw, the §3.6 division-convention lock, and eight hand-verifiable worked examples.
- [x] §4 assembly + reference direction (acyclic, with a CS0104 pre-check), file layout, the
      `OwnershipProfile` seam, the #30 and #40 seams and their deliberate asymmetry, save composition,
      neighbour contracts.
- [x] §5 test plan across identity / units / determinism / save / seams / fail-loud / structural + the
      T-phase closed-loop scenario.
- [x] §6 loop classification (world tick only, no hot path), cost profile, `[GT]` budget ceilings.
- [x] §7 T0–T3 plan, deep-tier extensions, the not-planned list, risks R-1..R-4.
- [x] §8 XC-045-001..016 + the back-prop table + the explicit not-a-back-prop list.
- [x] Appendices A (constants), B (save layout), C (band table).

## 9.2 Constant-tag discipline

- [x] Every constant in Appendix A carries exactly **one** of `[FIXED]` / `[DERIVED]` / `[CROSS]` /
      `[CROSS-PENDING]` / `[GT]`.
- [x] No `[EST]` remains (none was introduced).
- [x] Empty regions omitted (#20 prohibits them) — #45 has no `[DERIVED]` and no `[EST]` constants.
- [x] `[CROSS]` rows name their authority and are consumed read-only — #45 re-declares none of #40's or
      #27's types, and specifically does not shadow `BoardModifier` (FR-BD-017 / T-BD-BOUND-003).
- [x] `_RESERVED_0x2D_` / `SubsystemOrdinals.BoardOwnership` are `[CROSS-PENDING]` pending the T3
      promotion (§8.2) — the ERR-040-001 / ERR-030-001 spec-text-first precedent.
- [x] The `[GT]` magnitudes are declared **illustrative pending the T3 balance pass**, and §5 asserts
      only shape/identity, never magnitude.

## 9.3 Verification of load-bearing claims (checked against source, not asserted)

- [x] #40 defines `BoardModifier` with `BudgetMultiplierMillPermille`, `Identity => new(1000)`, and a
      fail-loud `default()`. *(`club-finances-economy/section-2.md` §2.2, FR-FN-018)*
- [x] #40 FR-FN-019 states **"#45 becomes the producer of a non-identity `BoardModifier` when it
      exists"** — the fact §8.2's "not a back-prop" rests on. *(`club-finances-economy/section-2.md`)*
- [x] #40 FR-FN-027 pins the `#45 → #40` direction; §7 forbids a second budget-multiplier path.
- [x] #40 FR-FN-025 fails loud for a `ClubId` with no finance entry — the fact the FR-BD-018 `Try`
      asymmetry rests on.
- [x] #30 `BoardState` = `{ Objective, JobSecurity (float/enum) }`, serialized by `WriteBoard`, evaluated
      at boundary-roll step (b) — the fact KD-5 rests on. *(`season-competition-loop/section-2.md` §2.2,
      `section-3.md` §3.5/§3.6)*
- [x] #30 FR-SN-015 already mandates the running "on track?" projection #45 consumes.
- [x] #30's tick order today ends: #34 staff = 6, #42 academy = 7, `AdvanceDay()` = 8 — so the board seam
      is step 8 and `AdvanceDay` becomes 9. *(`season-competition-loop/section-3.md` §3.3)*
- [x] #30's boundary roll already has a step (b') where `SettleFinances` is invoked per club (ERR-030-003)
      — #45 needs **no new insertion point**. *(`season-competition-loop/section-3.md` §3.5)*
- [x] #33 §3.1 defines `DriftPermille` and the `LastAdvancedWorldDay` guard; FR-HS-008 pins the sentinel
      at `uint.MaxValue`, **not** `0`. *(`personalities-morale-dynamics/section-3.md`, `section-2.md`)*
- [x] #33 FR-HS-024 already lists **#45** among its deferred read-only morale consumers.
- [x] #33 `HumanSystemsDayInput.BoardObjectiveDeltaPermille` exists today **with no producer**.
- [x] `RegisterStream` appends into a bounded, never-shrinking table; `MaxRngStreams` = 64 — the shared
      bound FR-BD-022 avoids contributing to. *(`src/deterministic-sim/DeterministicRngService.cs`,
      #42 §7.4 R-1)*
- [x] `ERR-030-005` is soft-reserved by #31, `-006` = #34, `-007` = #42 — making **`-008`** and
      **`-009`** the next free numbers. *(`docs/tracking/spec-error-log.md` v1.36)*
- [x] Roadmap §6 assigns #45 the `0x2D` / ordinal 95 slot.
      *(`docs/tracking/management-layer-spec-roadmap.md` §6)*

## 9.4 Gates

| Gate | Owner | Status |
|---|---|---|
| **G1** — section-file PASS-1 adversarial review + a v0.2 fix pass. | drafter | ✅ **CLOSED** — see §9.4.1 |
| **G2** — file **ERR-030-008**, **ERR-030-009**, **ERR-045-001** atomically with the status flip. | drafter | ⏳ **OPEN** |
| **G3** — lead-developer R-01..R-05 sign-off. | lead developer | ⏳ **OPEN** |
| **G4** — `SPEC_INDEX.md` registry row + Registry-Changes entry, added at promotion. | drafter | ⏳ **OPEN** |

**Not gating (deferred by design, recorded so they are not mistaken for omissions):** the `0x2D`
promotion (T3, first draw — FR-LW-031 forbids registering it earlier); the outer
`SEASON_SAVE_FORMAT_VERSION` bump and ERR-030-009's `SEASON_STATE_FORMAT_VERSION` effect (both T2); the
#33 morale read and the #33 board-delta production (both T3); and the T3 `[GT]` balance pass (§A.3).

### 9.4.1 PASS-1 adversarial review record (G1)

**PASS-1: 0H + 3M + 3L, all resolved in the v0.2 fix pass.**

| # | Sev | Finding | Resolution |
|---|---|---|---|
| M-1 | M | The **`default(BoardConfidence)` zero-value trap was unaddressed** — FR-BD-005 covered only `OwnershipProfile`. It is the *more* dangerous of the two: every field of a default `BoardConfidence` is **in range** (`0` is a legal per-mille and a legal world day), so no range check can catch it, yet it means `ConfidencePermille = 0` — the **`Critical` band, "dismissal imminent"** — and `LastAdvancedWorldDay = 0` (not the sentinel), which silently **no-ops** a day-0 advance instead of failing loud. | New **FR-BD-005a** + **F4a**: the pair must be factory-built and the **enforced** guard is at *record insertion*, not the consuming seam (the #33 FR-HS-005 posture, which exists for exactly this reason). New locks T-BD-FAIL-008/009. `section-2.md` / `section-5.md` v0.2. |
| M-2 | M | §6.3 declared two `[GT]` constants (`BD_BUDGET_ADVANCE_US`, `BD_BUDGET_SEASON_PROJECTION_US`) that were **absent from the Appendix A catalogue** — which is meant to be the single catalogue, and is what a reader greps for tag discipline. | Added to A.3 carrying their ceiling-not-measurement caveat. `appendices.md` v0.2. |
| M-3 | M | **No migration posture for the T2 version bumps.** T2 bumps two versions at once (the outer frame gains #45's sub-blob; #30's season block changes `JobSecurity`'s representation), so pre-T2 saves become unloadable — and the spec said nothing about what happens to them. | §4.6 now states it plainly: **rejected fail-loud, no migration, no silent upgrade** (the living-world slice-2 precedent). Cross-version migration is **#50's** subject; stating the position means #50 inherits it rather than discovering an assumption. `section-4.md` v0.2. |
| L-1 | L | The outline's section map cited `XC-045-001..012`; §8 defines **001..016**. | Corrected. `outline.md` v0.2. |
| L-2 | L | T-BD-ID-005 read as an unqualified "byte-identical season" claim, which is false from T2 onward (the save gains #45's sub-blob). | Scoped to **T0/T1**, with the distinction stated: KD-8's identity is about #40's settled budgets and existing RNG cursors, never the save frame. `section-5.md` v0.2. |
| L-3 | L | FR-BD-026 said a takeover "mutates" the `OwnershipProfile` — which is a `readonly struct`. | Reworded to **replaces** the stored value. `section-2.md` v0.2. |

**AR-2 sweep: 0H + 0M + 2L, both resolved — CONVERGENCE** (an L-only round closes the cycle, per the
project convention). **L-1:** §3.3's overflow argument was stated at `sensitivity ≤ 1000`, but that bound
was **declared nowhere** — the argument rested on an unstated premise; `BD_BUDGET_SENSITIVITY_PERMILLE`
now carries an explicit `[0,1000]` bound in A.3, flagged as load-bearing rather than cosmetic. **L-2:**
§9.1's completeness list still read `FR-BD-001..030` / `F1..F7` after PASS-1 added FR-BD-005a and F4a —
a wrong claim in the *approval checklist* being worse than the same slip elsewhere.


## 9.5 Sign-off

| Role | Criterion | Signed |
|---|---|---|
| R-01 | Scope and out-of-scope boundaries are unambiguous; no model #45 does not own is duplicated, and the #30 reconciliation is explicit rather than implied. | ⏳ |
| R-02 | Every formula has units, ranges, and at least one worked example; no fabricated verification values. | ⏳ |
| R-03 | Determinism posture is complete: stream ownership, the keyed ordinal, the draw-free minimal tier, and the no-cursor claim are each justified. | ⏳ |
| R-04 | Persistence is version-gated, opaque, fail-loud, and bumps no format version it does not own; the three independent versions are distinguished. | ⏳ |
| R-05 | Cross-spec back-props are enumerated with owners and timing, and the one non-additive change (ERR-030-009) is called out rather than buried. | ⏳ |

## 9.6 Decision

**PENDING** — G1 is closed (§9.4.1); **G2/G3/G4 remain open**. #45 stays `IN REVIEW` until the three
back-props are filed atomically with the status flip, the registry row is added, and lead-developer
R-01..R-05 sign-off is granted. Sign-off is a human authority and is not self-grantable.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-25 | — | Initial §9 (completeness, tag discipline, the §9.3 source-verified claims table, the four open gates + the explicitly-not-gating list, R-01..R-05). Status IN REVIEW. |
| 0.2 | 2026-07-25 | — | G1 CLOSED: §9.4.1 records the section-file PASS-1 (0H+3M+3L, all resolved) and the AR-2 convergence sweep (0H+0M+2L). §9.1 completeness updated for FR-BD-005a / F4a. G2/G3/G4 remain open. |
#endregion
