# Youth Academy & Intake #42 — Section 9: Approval Checklist

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.2 — PASS-1 record + G1 closed)
**Version:** 0.2
**Status:** IN REVIEW

---

## 9.1 Content completeness

- [x] §1 scope / out-of-scope seams / dependencies / KD-1..KD-8 / determinism posture.
- [x] §2 FR-YA-001..028, data structures, failure modes F1..F7.
- [x] §3 FM-YA-01..04 with the anchor derivation, both transforms (incl. the §3.3.1 monotonicity proof),
      promotion, four hand-verifiable worked examples, and the §3.6 division-convention lock.
- [x] §4 assembly + reference direction (acyclic, with a CS0104 pre-check), file layout, the
      `AcademyQuality` seam, the §4.4 anchor-call decision, save composition, root/#30/#28/#27 contracts.
- [x] §5 test plan across identity / units / determinism / save / promotion / fail-loud + the T-phase
      closed-loop scenario, with the explicit rationale for pinning cohorts relationally.
- [x] §6 loop classification (world tick only, no hot path), cost profile, `[GT]` budget ceilings.
- [x] §7 T0–T3 plan, deferrals, the not-planned list, four carried risks.
- [x] §8 XC-042-001..014 + the back-prop table + the no-external-citation rationale.
- [x] Appendices A (constants), B (save layout), C (why no cohort table).

## 9.2 Constant-tag discipline

- [x] Every constant in Appendix A carries exactly one of `[FIXED]` / `[DERIVED]` / `[CROSS]` /
      `[CROSS-PENDING]` / `[GT]`.
- [x] No `[EST]` remains (none was introduced).
- [x] `[CROSS]` rows name their authoritative spec and are consumed read-only — #42 re-declares none of
      #28's or #27's constants.
- [x] `DOMAIN_TAG_YOUTH_ACADEMY` / `SubsystemOrdinals.YouthAcademy` are `[CROSS-PENDING]` pending the T2
      promotion (§8.2), matching the ERR-030-001 / ERR-028-001 spec-text-first precedent.
- [x] The `[GT]` magnitudes are declared **illustrative pending the T3 balance pass**, and §5
      asserts only shape/identity, never magnitude.

## 9.3 Verification of load-bearing claims (checked against source, not asserted)

- [x] `RegenGenerator.GenerateRegen` is `static`, pure, and takes `streamIndex` as a parameter —
      the fact KD-1 rests on. *(`src/player-progression/RegenGenerator.cs`)*
- [x] `PlayerLifecycle.CurrentAbility` is a derived cache of `AbilityModel.ComputeCA(attributes,
      position)` — the fact KD-2 rests on. *(`src/player-progression/AbilityModel.cs`)*
- [x] The §3.3.1 clamp floor reproduces `RegenGenerator`'s own `paFloor` expression verbatim.
- [x] `PA_MIN` = 4000, `ABILITY_MAX` = 10000, `REGEN_PA_HEADROOM` = 1000, `REGEN_AGE_MIN/MAX` = 16/20,
      `DAYS_PER_YEAR` = 365. *(`src/player-progression/PlayerProgressionConstants.cs`)*
- [x] `CLUB_SQUAD_SIZE` = 25. *(`src/player-database/PlayerDatabaseConstants.cs`)*
- [x] `RegisterStream` appends into a bounded, never-shrinking table; `MaxRngStreams` default = 64 — the
      fact KD-7 rests on. *(`src/deterministic-sim/DeterministicRngService.cs`, `DeterministicSimConstants.cs`)*
- [x] #30's tick order today ends: staff = step 6, `WorldStore.AdvanceDay()` = step 7 — so the academy
      seam is step 7 and `AdvanceDay` becomes step 8. *(`season-competition-loop/section-3.md` §3.3)*
- [x] **#30 exposes no season-year field** — the fact KD-4 rests on. *(`season-competition-loop/section-2.md` §2.2)*
- [x] #34 publishes a staff-quality projection and **built no #42 interface** by design.
      *(`staff-backroom/section-2.md` FR-ST-021, `section-4.md` §4.3)*
- [x] #28 §7 already records the reciprocal ("#28 provides the machinery, #42 the quality dial").
      *(`player-progression-lifecycle/section-7.md`)*
- [x] `ERR-030-005` is soft-reserved by #31 and `-006` is #34's, making **`-007`** the next free number.
      *(`docs/tracking/spec-error-log.md`)*

## 9.4 Open gates (must close before `IN REVIEW → APPROVED`)

| Gate | Owner | Status |
|---|---|---|
| **G1** — section-file PASS-1 adversarial review + a v0.2 fix pass. | drafter | ✅ **CLOSED** — see §9.4.1 |
| **G2** — file **ERR-030-007** (#30 academy tick-order null seam) atomically with the status flip. | drafter | ⏳ **OPEN** |
| **G3** — lead-developer R-01..R-05 sign-off. | lead developer | ⏳ **OPEN** |
| **G4** — `SPEC_INDEX.md` registry row + Registry-Changes entry, added at promotion. | drafter | ⏳ **OPEN** |

### 9.4.1 PASS-1 adversarial review record (G1)

**PASS-1: 0H + 3M + 1L, all resolved in the v0.2 fix pass.**

| # | Sev | Finding | Resolution |
|---|---|---|---|
| M-1 | M | §5 T-YA-I-009 asserted the no-`Squad`-write property **structurally** ("the assembly has no reference through which one is reachable") — **false**: #42 references #27 for `PlayerRecord` / `CLUB_SQUAD_SIZE`, so `Squad` *is* reachable. A false structural claim is worse than none, because it invites skipping the behavioural check. | Re-stated as a behavioural assertion (a `Squad` handed to the root is field-unchanged across intake / promotion / save-restore) plus a standing §4.6 review item. `section-5.md` v0.2. |
| M-2 | M | Appendix A double-tagged `ACADEMY_INTAKE_PERIOD_DAYS` and `ACADEMY_AGE_MIN`/`_MAX` as `[DERIVED]` *and* "`[GT]`-overridable" — Spec #20 permits exactly one tag, and `[DERIVED]` specifically means a designer must never set the value, which is the opposite of what §7.2 does with these three. | Retagged `[GT]` (defaults that happen to equal a `[CROSS]` value), with the consequence stated plainly: the minimal identity holds **at the defaults**, and moving the band off #28's is the intended deep tier. `appendices.md` v0.2. |
| M-3 | M | Appendix A cited **FR-YA-008** (the age band) as the bound on cohort size — the wrong requirement; the composed `size` bound lives only in §3.3 pseudocode, and the *dial* bound is FR-YA-011. | Citation corrected to "§3.3 / F2 composed bound", with the dial bound attributed to FR-YA-011. `appendices.md` v0.2. |
| L-1 | L | §3.2's anchor had a `purpose` bound guard but **no `clubId` guard**, though injectivity requires `clubId < ACADEMY_CLUB_STRIDE`. An out-of-stride club id would carry into the day/purpose digits and silently alias two clubs onto one anchor — same cohort, no error, no divergence signal. | Guard added with its rationale + an overflow range check; new lock T-YA-U-014. `section-3.md` v0.2, `section-5.md` v0.2. |

**AR-2 sweep: 0H + 0M + 2L, both resolved — CONVERGENCE** (an L-only round closes the cycle, per the
project convention). L-1: §3's preamble still pointed at a "§3.6 asymmetry note" that the §3.6 rewrite had
replaced with a convention lock. L-2: removing the `[DERIVED]` rows left an **empty** Appendix A.3
region, which Spec #20 explicitly prohibits — the region is now omitted and the regions renumbered, with
the omission rule stated in the appendix preamble.

**Not gating (deferred by design, recorded so they are not mistaken for omissions):** the #16 `0x2B`/93
promotion (T2, first draw — FR-LW-031 forbids registering it earlier); the outer
`SEASON_SAVE_FORMAT_VERSION` bump (T2); the conditional #16 `SeekStream` seam (§4.4); the T3 `[GT]`
balance pass (§A.3); and the world-wide-academy extension, which is gated on the shared `MaxRngStreams`
bound (§7.4 R-1) owned by #28/#16.

## 9.5 Sign-off

| Role | Criterion | Signed |
|---|---|---|
| R-01 | Scope and out-of-scope boundaries are unambiguous; no model #42 does not own is duplicated. | ☐ |
| R-02 | Every formula has units, ranges, and at least one worked example; no fabricated verification values. | ☐ |
| R-03 | Determinism posture is complete: stream ownership, anchor, draw budget, and the no-cursor claim are each justified. | ☐ |
| R-04 | Persistence is version-gated, opaque, fail-loud, and bumps no format version it does not own. | ☐ |
| R-05 | Cross-spec back-props are enumerated with owners and timing; exactly one is approval-time. | ☐ |

## 9.6 Decision

**PENDING** — G1 closed; **G2 / G3 / G4 open**. G3 (lead-developer R-01..R-05 sign-off) is a
human authority and is not self-grantable by the drafter; G2 files atomically with the flip G3 authorises.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §9 (completeness, tag discipline, the §9.3 source-verified claims table, the four open gates + the explicitly-not-gating list, R-01..R-05). Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | G1 CLOSED: §9.4.1 records the section-file PASS-1 (0H+3M+1L, all resolved) and the AR-2 convergence sweep (0H+0M+2L). G2/G3/G4 remain open. |
#endregion
