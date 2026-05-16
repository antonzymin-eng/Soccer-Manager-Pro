# Heading Mechanics Specification #10 — Section 9: Approval Checklist

**Created:** May 16, 2026
**Version:** 0.1
**Status:** DRAFT — checklist not yet exercised; pending pass-1
adversarial review of the section files, then pass-2 fix cycle,
then sign-offs.
**Purpose:** Quality gate for `IN REVIEW → APPROVED` transition.
All checks below MUST verify against actual file contents — no
fabricated entries (closes ERR-005 trap class per KD-11).

---

## 9.1 Constant-Tag Verification (KD-11)

Per-constant programmatic check against the §3.1 Master Physical
Profile Table and (once `src/Gameplay/Heading/HeadingConstants.cs`
exists) the constant-catalogue file.

**Rule:** every constant carries exactly one tag in
`{[GT], [EST], [FIXED], [DERIVED], [CROSS], [CROSS-PENDING]}`.

**Verification method:** grep `HeadingConstants.cs` (when present)
and §3.1 Master Physical Profile Table; cross-check each line.

- [ ] `HEAD_CONTACT_VOLUME_RADIUS_M` — `[GT]`
- [ ] `HEAD_CONTACT_VOLUME_HEIGHT_M` — `[GT]`
- [ ] `MAX_EARLY_TOLERANCE_MS` — `[GT]`
- [ ] `MAX_LATE_TOLERANCE_MS` — `[GT]`
- [ ] `EARLY_LABEL_THRESHOLD_MS` — `[GT]`
- [ ] `LATE_LABEL_THRESHOLD_MS` — `[GT]`
- [ ] `TIMING_POINT_BLEND_ALPHA` — `[GT]`
- [ ] `MIN_CONTACT_QUALITY` — `[GT]`
- [ ] `FRAME_MS` — `[DERIVED]` from `TICK_RATE_PHYSICS_HZ`
- [ ] `JUMP_REACH_BASE_M` — `[FIXED]`
- [ ] `JUMP_REACH_K_STRENGTH` — `[GT]`
- [ ] `JUMP_REACH_K_BALANCE` — `[GT]`
- [ ] `JUMP_REACH_K_HEADING` — `[GT]`
- [ ] `JUMP_PHASE_DURATION_MS` — `[GT]`
- [ ] `JUMP_APEX_FRACTION` — `[GT]`
- [ ] `POWER_BASE_MPS` — `[GT]`
- [ ] `POWER_K_STRENGTH` — `[GT]`
- [ ] `POWER_K_HEADING` — `[GT]`
- [ ] `POWER_FATIGUE_COEFF` — `[GT]`
- [ ] `CONTACT_POINT_ERROR_SIGMA_M` — `[GT]`
- [ ] `CONTACT_POINT_NOISE_SIGMA_M` — `[GT]`
- [ ] `TIMING_JITTER_SIGMA_MS` — `[GT]`
- [ ] `CONTACT_POINT_HEADING_ATTR_COEFF` — `[GT]`
- [ ] `SPIN_TRANSFER_COEFF` — `[GT]`
- [ ] `SPIN_PRESERVATION_BASE` — `[GT]`
- [ ] `SPIN_TRANSFER_REVERSAL_THRESHOLD` — `[GT]`
- [ ] `DUEL_BALANCE_WEIGHT` — `[GT]`
- [ ] `DUEL_STRENGTH_WEIGHT` — `[GT]`
- [ ] `DUEL_HEADING_WEIGHT` — `[GT]`
- [ ] `DUEL_TIEBREAK_EPSILON` — `[GT]`
- [ ] `DUEL_TIEBREAK_NOISE_AMPLITUDE` — `[GT]`
- [ ] `DUEL_DISTURBANCE_MAX` — `[GT]`
- [ ] `DUEL_DISTURBANCE_GAP_SATURATION` — `[GT]` (v0.2 H-4)
- [ ] `OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_S` — `[GT]`
- [ ] `OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_M` — `[GT]`
- [ ] `GRAVITY_MPS2` — `[CROSS]` (Ball Physics #1)
- [ ] `PITCH_LENGTH_M` — `[CROSS]` (Ball Physics #1 §1.2)
- [ ] `PITCH_WIDTH_M` — `[CROSS]` (Ball Physics #1 §1.2)
- [ ] `DOMAIN_TAG_HEADING = 0x16` — `[CROSS-PENDING]` (#16 §3.4; OI-001)
- [ ] `TICK_RATE_TACTICAL_HZ` — `[CROSS]` (CLAUDE.md)
- [ ] `TICK_RATE_PHYSICS_HZ` — `[CROSS]` (CLAUDE.md)

---

## 9.2 Cross-Spec Reference Verification

Every `XC-010-NNN` resolves to an actual section in the named
spec. Method: grep target spec; section must exist.

- [ ] `XC-010-001` → Ball Physics #1 §1.2 — verified present
- [ ] `XC-010-002` → Shot Mechanics #6 §1.3 KD-6 — verified present
- [ ] `XC-010-003` → First Touch #4 §1.2 — verified present
- [ ] `XC-010-004` → Deterministic Sim #16 §3.4 `DOMAIN_TAG_HEADING` row — pending OI-001 back-prop
- [ ] `XC-010-005` → Event System #17 own-goal adjudication — anchor pinned during pass-2
- [ ] `XC-010-006` → Performance Optimization #18 §3.10 — verified present
- [ ] All `FM-010-NNN` formula references resolve to §3.x subsections
- [ ] All `EC-010-NNN` edge-case references resolve to §2.3 / §3.x

---

## 9.3 Sign-Off Requirements

- [ ] **R-01: Lead-developer sign-off.** Spec content reviewed end-to-end.
- [ ] **R-02: Physics-owner sign-off.** Head-ball contact geometry, spin-transfer derivation (§3.6), launch-angle derivation (§3.5) reviewed.
- [ ] **R-03: Determinism-owner sign-off.** KD-10 governance: draw-site IDs (`DRAW_SITE_DUEL_TIEBREAK`, `DRAW_SITE_CONTACT_POINT_ERROR`, `DRAW_SITE_TIMING_JITTER`) registered; iteration order in §3.7 follows #16 §3.2; `DOMAIN_TAG_HEADING = 0x16` back-prop ERR-010-001 filed.
- [ ] **R-04: Performance-owner ratification.** §6.1 budgets (≤80 µs steady-state, ≤180 µs p99 duel-frame) carried into #18 §6 per #18 KD-2; channel rows allocated in #18 §3.10 (OI-002).
- [ ] **R-05: Testing-owner sign-off.** §5 test plan reviewed; coverage targets in §5.5 acceptable.

---

## 9.4 Outstanding Items at Approval Time

| ID | Item | Status |
|----|------|--------|
| OI-001 | `DOMAIN_TAG_HEADING = 0x16` allocation in #16 §3.4 (back-prop ERR-010-001) | ERR-010-001 filed in `spec-error-log.md` May 16, 2026 (v0.2 M-1); allocation in #16 §3.4 still pending — atomic with §3.1 row promotion `[CROSS-PENDING] → [CROSS]` |
| OI-002 | `#18 §3.10` trace channel rows for `heading.*` channels | pending — back-prop when §2.4 lands in #18 §3.10 |
| OI-003 | DOI verification for §8.3 external references (6 anchors) | pending — drafter task |
| OI-005 | Pin #3 contact-event API subsection; pin #8 §1.7.x intent-surface anchor; pin #17 event-publish subsection | pending — drafter task during pass-2 |
| OI-006 | `certification-platform.md` Stage-0 host pin | not blocking #10 sign-off; gates `FR-PO-052` activation only |

`OI-004` (Goalkeeper #11 interface confirmation) is post-approval
per §9.6.

---

## 9.5 Cross-Spec Re-Audit (Pre-`APPROVED`)

Verify against APPROVED versions of upstream specs that no surface
cited has shifted between draft start and approval. Specs in
scope: #1, #2, #3, #4, #5, #6, #8, #16, #17, #18, #19, #20.

- [ ] Ball Physics #1 — coordinate origin §1.2; `Ball.ApplyKick` §3.1.11.2 still authoritative
- [ ] Agent Movement #2 — `PlayerAttributes` struct §3.5.6; `AgentMovementState` §3.1.2 still authoritative
- [ ] Collision System #3 — contact-event API unchanged
- [ ] First Touch #4 — §1.2 0.5 m boundary statement unchanged
- [ ] Pass Mechanics #5 — `BallState` surface unchanged (re-approved May 6, 2026)
- [ ] Shot Mechanics #6 — §1.3 KD-6 body-part discriminator unchanged
- [ ] Decision Tree #8 — intent surface still at §1.7.x
- [ ] Deterministic Sim #16 — `DOMAIN_TAG` slot `0x16` still available (or re-pin if reallocated)
- [ ] Event System #17 — event-publish API unchanged
- [ ] Performance Optimization #18 — §3.10 channel registry, §6 ratify-not-override authority unchanged
- [ ] Testing Strategy #19 — test-framework APIs unchanged
- [ ] Code Standards #20 — constant-tag grep tooling unchanged

---

## 9.6 Post-Approval Follow-Ups (Not Gating)

- Comprehensive audit (per Decision Tree #8 precedent — draft-level approval is acceptable; comprehensive audit is a candidate but not required).
- Goalkeeper #11 interface confirmation once #11 reaches `IN REVIEW`.
- Migration follow-ups in §7.8 (AM #2 native Z), §7.9 (AM #2 head-segment API), §7.10 (dedicated jump-timing attribute) when the upstream surfaces arrive at Stage 1+.

---

## 9.7 Decision

- **Status at draft (v0.1):** `IN REVIEW` candidate — section files
  drafted from outline-detailed v1.1. Pass-1 adversarial review of
  section files pending before any `IN REVIEW` flip in
  `SPEC_INDEX.md`.
- **Status at v0.2 (May 16, 2026):** `IN REVIEW` candidate — v0.2
  PASS-1 fix pass landed across all section files + appendices,
  resolving the 21 findings (5 H / 9 M / 7 L) in
  `adversarial-review-section-files-v1.md`. `ERR-010-001` filed in
  `spec-error-log.md`. Ready for `SPEC_INDEX.md` row 10 flip
  `NOT STARTED → IN REVIEW` atomic with v0.2 landing.

---

## 9.8 Version History

| Version | Date         | Author  | Notes                                                  | Reviewer |
|---------|--------------|---------|--------------------------------------------------------|----------|
| 0.1     | May 16, 2026 | drafter | Initial checklist; not yet exercised                   | pending  |
| 0.2     | May 16, 2026 | drafter | Added `DUEL_DISTURBANCE_GAP_SATURATION` row (H-4); updated OI-001 status (M-1); added v0.2 status note for `SPEC_INDEX.md` row 10 flip readiness. | pending |
