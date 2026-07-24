# Competition Structure #43 — Section 9: Approval Checklist

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.2 — section-file AR PASS-1 (1M+1L) → PASS-2 clean → CONVERGENCE; R-01..R-05 signed; APPROVED; prior v0.1 IN REVIEW)
**Version:** 0.2
**Status:** APPROVED

---

## 9.1 Evidence-anchored gate items

| # | Gate | Status | Evidence |
|---|---|---|---|
| G1 | Every constant carries exactly one source tag ([GT]/[FIXED]/[DERIVED]/[CROSS]) | ✅ | Appendix A catalogue |
| G2 | The `[GT]` counts/spacing magnitudes are illustrative pending a balance pass (shapes are the reviewed contract) | ✅ | Appendix A note (#21 G2 precedent) |
| G3 | Determinism: minimal is **draw-free**; `_RESERVED_0x2C_`/94 created by ERR-043-001 and stays reserved; deep draws keyed (no cursor, nothing serialized) | ✅ | §1 KD-2, §8.2/8.3, FR-CP-007/008/014 |
| G4 | KD-1: a league is a degenerate instance; instance 0 is a **binding row** (no stored #30 object; FR-SN-032/033 respected) | ✅ | FR-CP-001/002, §4.3 |
| G5 | KD-7: canonical ascending-`ClubId` order at every draw-feeding surface; keyed Fisher–Yates; shuffled-input equivalence locked | ✅ | FR-CP-005/009, §3.2, T-CP-DET-005 |
| G6 | KD-3: brackets persisted (serialize-don't-regenerate) with fail-loud coherence gates; a restore never re-rolls | ✅ | FR-CP-010/011/025, §3.3 |
| G7 | KD-4: promotion/relegation at the pre-declared (a'), before #40's (b'); membership-only (no re-key); the code-side hook named as the T-phase ERR-030-008 coordination | ✅ | FR-CP-015..018, §3.4, XC-043-002/003 |
| G8 | KD-5: merged fixture-day view; #30's `SeasonCalendar` untouched; queried only when >1 competition | ✅ | FR-CP-019, §3.5 |
| G9 | KD-6: one `COMPETITION_SAVE_FORMAT_VERSION` sub-blob; no `WORLD_STORE` bump; instance 0 never duplicated; canonical-order + coherence decode gates | ✅ | FR-CP-012/013, §4.4, Appendix B |
| G10 | KD-8 behaviour-neutral: a singleton-collection season is byte-identical to bare #30; #43 executes no code on the minimal season path | ✅ | FR-CP-003, §4.3, T-CP-NEU-001 |
| G11 | #43 reuses #30's `FixtureScheduler`/`LeagueTable` per instance (no re-implementation); the #30 §7 generalization row honoured | ✅ | FR-CP-006, XC-043-001 |
| G12 | `CompetitionId` genesis-assigned, deterministic, never reused; fixtures/results carry it (the #44 scoping surface) | ✅ | FR-CP-004/020 |
| G13 | Integer posture; no float; no RNG state serialized | ✅ | FR-CP-014/023, T-CP-SHAPE/INT-001 |
| G14 | FR-CP-001..025 each traceable to a T-CP-* test **or** a recorded §7 deferral | ✅ | §5.8 |
| G15 | FR prefix FR-CP unclaimed across `docs/specs/**`; XC-043-* allocated; the #30 FR-SN-031 / #40 §1 / #44-facing sides named | ✅ | grep-verified; §8.1 |

## 9.2 Post-APPROVED follow-ups (non-blocking)

- **G2 balance pass** — `PROMOTION_COUNT`/`RELEGATION_COUNT`, cup-day spacing, and format configs
  are illustrative; pinned at the Stage-5 balance pass (the #21 G2 precedent).
- **T-phase back-props** — the #30 outer `SEASON_SAVE_FORMAT_VERSION` bump (T1); the ERR-030-008
  code-side coordinations (T2 (a') hook, T3 deep driver); the #16 `0x2C` promotion (T3 first
  draw).

## 9.3 Approval-time cross-spec back-props

**One:** **ERR-043-001** — the #16 §3.4 A-04 placeholder sweep (`_RESERVED_0x2B_` #42 /
`_RESERVED_0x2C_` #43 / `_RESERVED_0x2D_` #45; the catalogue ended at `0x2A`), completing the
roadmap §6 block. Pure namespace reservation; no `DETERMINISM_DIGEST_VERSION` bump. **No
#30/#40/#27 spec-text change** — FR-SN-031's (a') and #40's (b') ordering pre-exist. **Filed
atomically at approval** (`spec-error-log.md` v1.39; `deterministic-sim/section-3.md` §3.4
v1.0.14).

## 9.4 Sign-off

| Role | Decision | Date |
|---|---|---|
| R-01 Lead developer | ✅ APPROVED | Jul 24, 2026 |
| R-02 Determinism owner | ✅ APPROVED (draw-free minimal; `_RESERVED_0x2C_`/94 created by ERR-043-001 and stays reserved; deep draws keyed on `competition.draws`, no cursor; `DeriveInstanceSeed` pure) | Jul 24, 2026 |
| R-03 Save-format owner | ✅ APPROVED (`COMPETITION_SAVE_FORMAT_VERSION` sub-blob; no `WORLD_STORE` bump; instance 0 never duplicated; canonical-order + coherence decode gates) | Jul 24, 2026 |
| R-04 Season-loop (#30) owner | ✅ APPROVED (the (a') point + FR-SN-031 honoured with no #30 spec-text change; the code-side (a') hook + deep fixture-day driver soft-reserved as ERR-030-008 T-phase coordinations; #30's pure types reused per instance) | Jul 24, 2026 |
| R-05 Finances (#40) owner | ✅ APPROVED (#43 owns no money; the (b')-after-(a') ordering preserved exactly as #40 pinned it) | Jul 24, 2026 |

## 9.5 Open gates before APPROVED — CLEARED

- Section-file adversarial review: PASS-1 (1M — `DeriveInstanceSeed` was normative machinery with
  no FR/test coverage; +1L — a worked-example test asserting non-derivable illustrative values) →
  PASS-2 clean at High/Medium → **CONVERGENCE**.
- R-01..R-05 sign-off — **granted July 24, 2026**.
- ERR-043-001 (the #16 placeholder sweep) — **filed atomically at approval**.
- G1..G15 evidence verification — complete.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial approval checklist (G1..G15, sign-off pending), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1 (1M+1L) → PASS-2 clean → CONVERGENCE; G1..G15 ✅; R-01..R-05 signed; ERR-043-001 filed (`spec-error-log.md` v1.39, `deterministic-sim/section-3.md` v1.0.14); Status APPROVED. |
#endregion
