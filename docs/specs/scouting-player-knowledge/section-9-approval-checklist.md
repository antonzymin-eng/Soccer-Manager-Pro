# Scouting & Player Knowledge #32 — Section 9: Approval Checklist

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.2 — section-file AR PASS-1 (3M+1L) → PASS-2 (1M+2L) → PASS-3 clean → CONVERGENCE; R-01..R-05 signed; APPROVED; prior v0.1 IN REVIEW)
**Version:** 0.2
**Status:** APPROVED

---

## 9.1 Evidence-anchored gate items

| # | Gate | Status | Evidence |
|---|---|---|---|
| G1 | Every constant carries exactly one source tag ([GT]/[FIXED]/[DERIVED]/[CROSS]) | ✅ | Appendix A catalogue |
| G2 | The `[GT]` width/cadence/relevance magnitudes are illustrative pending a Stage-3 balance pass (shapes/directions are the reviewed contract) | ✅ | §3.2/§3.4, Appendix A note (#21 G2 precedent) |
| G3 | Determinism: the minimal tier is **draw-free**; `_RESERVED_0x24_` / 86 stays reserved (no #16 change at approval) | ✅ | §1 KD-3, §8.2, #16 §3.4:268 |
| G4 | KD-2 view-not-mutation: read-only by construction (`in` value copies, no storage reference, readonly view types); the byte-identity lock (T-SC-VIEW-001) | ✅ | §2 FR-SC-001, §4.3, §5.1 |
| G5 | KD-1: overlay stores a band only; estimates derived on read; `BAND_MAX` collapses to `[truth, truth]` arithmetically; containment invariant proven | ✅ | §3.2, FR-SC-003/004/005/006 |
| G6 | KD-3: keyed draws on `(playerId, band, attrIdx, purpose)` — fixed radices, not `worldDay`; zero-width reads make no RNG call; no cursor exists or serializes | ✅ | §3.3, FR-SC-011/012/014 |
| G7 | KD-8 behaviour-neutral: a fog-off season is byte-identical to pre-#32; no consumer branches on the dial (`ResolveBand` only) | ✅ | §3.1, FR-SC-002/007, T-SC-NEU-001/002 |
| G8 | KD-2 own-squad omniscience + FR-SC-008 coverage boundary (external 31 attributes only; identity facts + `WeakFootRating` exact) | ✅ | §3.1, FR-SC-008/009, T-SC-OWN-001/002 |
| G9 | KD-1 freshness: the live-form window semantic pinned explicitly (width scouted, centre tracks truth; delta-visibility a named limitation; staleness a §7 extension) | ✅ | FR-SC-010, §7.2, T-SC-EST-005 |
| G10 | KD-4: scout quality scales speed only (`DaysPerBand`, `quality ≤ 0` fails loud); `SCOUT_QUALITY_NEUTRAL_PERMILLE = 1000` closes #34's open baseline with no #34 edit | ✅ | §3.4, FR-SC-023/024, XC-032-002 |
| G11 | KD-5: ranking is #32's own pure read-only query; #32 issues no offers; no display text (structured reports only) | ✅ | §3.5, FR-SC-026, XC-032-005/008 |
| G12 | KD-6: one `SCOUTING_SAVE_FORMAT_VERSION` season-save sub-blob; **no** `WORLD_STORE_FORMAT_VERSION` bump (the plan-§4 revision argued); codec fail-loud + canonical ascending-`PlayerId` order | ✅ | §4.4, Appendix B, FR-SC-015/016/017 |
| G13 | KD-6 hygiene: drop-on-roster-event (buy → own-squad, sell → reset); fail-loud view for unresolvable ids; delivery paths + T-phase sequencing recorded | ✅ | FR-SC-019, §7.1 T3, XC-032-006 |
| G14 | KD-7: commands (`AssignScout`/`CancelAssignment`) + the #30 slot-7 null seam (ERR-030-007, reserve-ahead, empty at minimal); managed-manager scope | ✅ | §3.4, FR-SC-020..022/025, §8.3 |
| G15 | Genesis-vs-load: the empty overlay is the genesis state (no seeding call); a load reconstructs and never resets a band; knowledge survives `RollToNextSeason` | ✅ | §4.5, FR-SC-018, T-SC-DET-001/002 |
| G16 | Integer posture: no float in #32; serialized block has no `RngCursor` | ✅ | §1.5, FR-SC-014/027, T-SC-INT-001/SHAPE-001 |
| G17 | FR-SC-001..027 each traceable to a T-SC-* test **or** a recorded §7 deferral | ✅ | §5.8 |
| G18 | FR prefix FR-SC unclaimed across `docs/specs/**`; XC-032-* allocated; the #34 FR-ST-021 / #31 FR-TX-010/011 / #38 FR-UI-002/004 producer/consumer sides named | ✅ | grep-verified; §8.1 |

## 9.2 Post-APPROVED follow-ups (non-blocking)

- **G2 balance pass** — the §3.2/§3.4/§3.5 `[GT]` magnitudes (`KNOWLEDGE_BAND_HALFWIDTH`,
  `DAYS_PER_BAND_BASE`, `KNOWLEDGE_BAND_MAX`, position-relevance sets) are illustrative; a
  numerical-mirror + balance review pins them at Stage-3 (the #21 G2 / #40 / #41 / #34 precedent).
- **T-phase back-props** — land with the code, not at approval: the #30 outer
  `SEASON_SAVE_FORMAT_VERSION` bump (T1); the roster-event hygiene delivery (T3, gated on #31's
  ERR-030-005 build + the #28 lifecycle coordination); the #16 `DOMAIN_TAG_SCOUTING = 0x24`
  promotion (ERR-016, T3 first draw).

## 9.3 Approval-time cross-spec back-props

**One:** **ERR-030-007** — #30 §3.3 `RunWorldTickInFixedOrder` gains the scouting tick-order
null-seam slot (the ERR-030-002 #41 / ERR-030-004 #31 / ERR-030-006 #34 precedent — an insertion,
since FR-SN-034 enumerated #28/#29/#33/#41/#31/#34 only, not #32; the slot is a **deep-tier position
reservation**, empty until #32 T2/T3; new step 7 after staff, `AdvanceDay` → step 8). `0x24`/86
stays reserved (draw-free minimal — no #16 change); #34/#31/#27/#38 unchanged (their existing seams
already record the #32-facing contracts). **Filed atomically at approval** (`spec-error-log.md`
v1.38; `season-competition-loop/section-2.md` v0.7 + `section-3.md` v0.7).

## 9.4 Sign-off

| Role | Decision | Date |
|---|---|---|
| R-01 Lead developer | ✅ APPROVED | Jul 24, 2026 |
| R-02 Determinism owner | ✅ APPROVED (draw-free minimal — every read short-circuits at zero width before any draw; `0x24`/86 stays reserved; deep draws keyed, not `worldDay`-keyed, no cursor) | Jul 24, 2026 |
| R-03 Save-format owner | ✅ APPROVED (`SCOUTING_SAVE_FORMAT_VERSION` sub-blob, canonical ascending-`PlayerId` order; no `WORLD_STORE` bump — the plan-§4 revision accepted with the KD-6 rationale) | Jul 24, 2026 |
| R-04 Data-layer (#27) owner | ✅ APPROVED (view-not-mutation structural — `in` value copies, no storage reference; the T-SC-VIEW-001 byte-identity lock; own-squad omniscience preserves the `LineupSelector` pipeline) | Jul 24, 2026 |
| R-05 Season-loop (#30) owner | ✅ APPROVED (scouting tick-order slot ERR-030-007 after staff; **no** roster-commit change — #32 only consumes the FR-TX-022 hook / FR-TX-028 lifecycle coordination for its entry drops) | Jul 24, 2026 |

## 9.5 Open gates before APPROVED — CLEARED

- Section-file adversarial review: PASS-1 (3M+1L — the fog-off command semantics; the fully-scouted
  gate enumeration; the codec-unperformable Appendix-B checks; the missing view name fields) →
  PASS-2 (1M regression — FR-SC-007's byte-identity claim vs the inert-loaded-assignment case; +2L)
  → PASS-3 clean at High/Medium → **CONVERGENCE**.
- R-01..R-05 lead-developer sign-off — **granted July 24, 2026**.
- ERR-030-007 (the #30 scouting tick-order null seam) — **filed atomically at approval**.
- G1..G18 evidence verification — complete.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial approval checklist (G1..G18, sign-off pending), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1 (3M+1L) → PASS-2 (1M+2L) → PASS-3 clean → CONVERGENCE; G1..G18 ✅; R-01..R-05 signed; ERR-030-007 filed (`spec-error-log.md` v1.38 — the v0.1 draft's v1.35 prediction corrected against the log's actual version); Status APPROVED. |
#endregion
