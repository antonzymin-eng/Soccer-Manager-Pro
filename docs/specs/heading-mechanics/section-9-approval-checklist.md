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

- [x] `HEAD_CONTACT_VOLUME_RADIUS_M` — `[GT]`
- [x] `HEAD_CONTACT_VOLUME_HEIGHT_M` — `[GT]`
- [x] `MAX_EARLY_TOLERANCE_MS` — `[GT]`
- [x] `MAX_LATE_TOLERANCE_MS` — `[GT]`
- [x] `EARLY_LABEL_THRESHOLD_MS` — `[GT]`
- [x] `LATE_LABEL_THRESHOLD_MS` — `[GT]`
- [x] `TIMING_POINT_BLEND_ALPHA` — `[GT]`
- [x] `MIN_CONTACT_QUALITY` — `[GT]`
- [x] `FRAME_MS` — `[DERIVED]` from `TICK_RATE_PHYSICS_HZ`
- [x] `JUMP_REACH_BASE_M` — `[FIXED]`
- [x] `JUMP_REACH_K_STRENGTH` — `[GT]`
- [x] `JUMP_REACH_K_BALANCE` — `[GT]`
- [x] `JUMP_REACH_K_HEADING` — `[GT]`
- [x] `JUMP_PHASE_DURATION_MS` — `[GT]`
- [x] `JUMP_APEX_FRACTION` — `[GT]`
- [x] `POWER_BASE_MPS` — `[GT]`
- [x] `POWER_K_STRENGTH` — `[GT]`
- [x] `POWER_K_HEADING` — `[GT]`
- [x] `POWER_FATIGUE_COEFF` — `[GT]`
- [x] `CONTACT_POINT_ERROR_SIGMA_M` — `[GT]`
- [x] `CONTACT_POINT_NOISE_SIGMA_M` — `[GT]`
- [x] `TIMING_JITTER_SIGMA_MS` — `[GT]`
- [x] `CONTACT_POINT_HEADING_ATTR_COEFF` — `[GT]`
- [x] `SPIN_TRANSFER_COEFF` — `[GT]`
- [x] `SPIN_PRESERVATION_BASE` — `[GT]`
- [x] `SPIN_TRANSFER_REVERSAL_THRESHOLD` — `[GT]`
- [x] `DUEL_BALANCE_WEIGHT` — `[GT]`
- [x] `DUEL_STRENGTH_WEIGHT` — `[GT]`
- [x] `DUEL_HEADING_WEIGHT` — `[GT]`
- [x] `DUEL_TIEBREAK_EPSILON` — `[GT]`
- [x] `DUEL_TIEBREAK_NOISE_AMPLITUDE` — `[GT]`
- [x] `DUEL_DISTURBANCE_MAX` — `[GT]`
- [x] `DUEL_DISTURBANCE_GAP_SATURATION` — `[GT]` (v0.2 H-4)
- [x] `HEADING_CONTACT_BUFFER_CAPACITY` — `[GT]` (v0.3 OI-005)
- [x] `OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_S` — `[GT]`
- [x] `OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_M` — `[GT]`
- [x] `GRAVITY_MPS2` — `[CROSS]` (Ball Physics #1)
- [x] `PITCH_LENGTH_M` — `[CROSS]` (Ball Physics #1 §1.2)
- [x] `PITCH_WIDTH_M` — `[CROSS]` (Ball Physics #1 §1.2)
- [x] `DOMAIN_TAG_HEADING = 0x16` — `[CROSS]` (#16 §3.4; OI-001 RESOLVED May 16, 2026 via #16 §3.5 v1.0.2 patch)
- [x] `TICK_RATE_TACTICAL_HZ` — `[CROSS]` (CLAUDE.md)
- [x] `TICK_RATE_PHYSICS_HZ` — `[CROSS]` (CLAUDE.md)

---

## 9.2 Cross-Spec Reference Verification

Every `XC-010-NNN` resolves to an actual section in the named
spec. Method: grep target spec; section must exist.

- [x] `XC-010-001` → Ball Physics #1 §1.2 — verified present
- [x] `XC-010-002` → Shot Mechanics #6 §1.3 KD-6 — verified present
- [x] `XC-010-003` → First Touch #4 §1.2 — verified present
- [x] `XC-010-004` → Deterministic Sim #16 §3.4 `DOMAIN_TAG_HEADING` row — RESOLVED via #16 §3.5 v1.0.2 patch
- [x] `XC-010-005` → Event System #17 §3.2.1 `Publish API surface` — anchored (v0.3 OI-005 closure)
- [x] `XC-010-006` → Performance Optimization #18 §3.10 — verified present
- [x] All `FM-010-NNN` formula references resolve to §3.x subsections
- [x] All `EC-010-NNN` edge-case references resolve to §2.3 / §3.x

---

## 9.3 Sign-Off Requirements

- [x] **R-01: Lead-developer sign-off.** Spec content reviewed end-to-end.
- [x] **R-02: Physics-owner sign-off.** Head-ball contact geometry, spin-transfer derivation (§3.6), launch-angle derivation (§3.5) reviewed.
- [x] **R-03: Determinism-owner sign-off.** KD-10 governance: draw-site IDs (`DRAW_SITE_DUEL_TIEBREAK`, `DRAW_SITE_CONTACT_POINT_ERROR`, `DRAW_SITE_TIMING_JITTER`) registered; iteration order in §3.7 follows #16 §3.2; `DOMAIN_TAG_HEADING = 0x16` allocation in #16 §3.4 RESOLVED (ERR-010-001 closed May 16, 2026 via #16 §3.5 v1.0.2 patch).
- [x] **R-04: Performance-owner ratification.** §6.1 budgets (≤80 µs steady-state, ≤180 µs p99 duel-frame) carried into #18 §6 per #18 KD-2; channel rows allocated in #18 §3.10 (OI-002).
- [x] **R-05: Testing-owner sign-off.** §5 test plan reviewed; coverage targets in §5.5 acceptable.

---

## 9.4 Outstanding Items at Approval Time

| ID | Item | Status |
|----|------|--------|
| OI-001 | `DOMAIN_TAG_HEADING = 0x16` allocation in #16 §3.4 (back-prop ERR-010-001) | ✅ RESOLVED May 16, 2026 — #16 §3.5 v1.0.2 patch landed; #10 §3.1 row promoted `[CROSS-PENDING] → [CROSS]`; ERR-010-001 closed in `spec-error-log.md`. |
| OI-002 | `heading.*` trace channel rows | ✅ RESOLVED May 16, 2026 — re-anchored to #18 Appendix F.0 channel-registry schema (not §3.10 constants catalogue). Populated subsystem-channel rows are a Stage 0+1 deliverable per #18 Appendix F.0 / §7.2 schedule (lands at first `src/Gameplay/Heading/` commit); §10 Stage 0 deliverable is the schema-conforming channel declarations in §2.4. |
| OI-003 | DOI verification for §8.3 external references | ✅ RESOLVED May 16, 2026 — 5/5 academic DOIs verified; 2 v0.1 references (Bull 1985, Auger & Pellegrini 2007) not findable, replaced with real equivalents (Babbs 2001, Tomczak et al. 2021); Opta/StatsBomb retained as commercial-data baseline class per §9.6 post-approval drafter task. |
| OI-005 | Pin #3 contact-event API; #8 intent-surface; #17 event-publish anchors | ✅ RESOLVED May 16, 2026 — #3 §3.4.2 (`ICollisionEventConsumer`); #8 §1.7.2 (Stage 0 deferral row; live wiring is Stage 0+1 per §4.6.1); #17 §3.2.1 (`Publish API surface`). #1 `BallState.GetBallState` re-anchored to §3.1.11.2 (same surface §4.3 cites for `Ball.ApplyKick`). |
| OI-006 | `certification-platform.md` Stage-0 host pin | not blocking #10 sign-off; gates `FR-PO-052` activation only (carry-over from CLAUDE.md OPEN ISSUES) |

`OI-004` (Goalkeeper #11 interface confirmation) is post-approval
per §9.6.

---

## 9.5 Cross-Spec Re-Audit (Pre-`APPROVED`)

Verify against APPROVED versions of upstream specs that no surface
cited has shifted between draft start and approval. Specs in
scope: #1, #2, #3, #4, #5, #6, #8, #16, #17, #18, #19, #20.

- [x] Ball Physics #1 — coordinate origin §1.2; `Ball.ApplyKick` §3.1.11.2 still authoritative
- [x] Agent Movement #2 — `PlayerAttributes` struct §3.5.6; `AgentMovementState` §3.1.2 still authoritative (v0.3: re-verified May 16, 2026)
- [x] Collision System #3 — `ICollisionEventConsumer` §3.4.2 + `CollisionEvent` struct unchanged (v0.3 OI-005: anchor pinned)
- [x] First Touch #4 — §1.2 0.5 m boundary statement unchanged
- [x] Pass Mechanics #5 — `BallState` surface unchanged (re-approved May 6, 2026)
- [x] Shot Mechanics #6 — §1.3 KD-6 body-part discriminator unchanged
- [x] Decision Tree #8 — Stage 0 deferral row §1.7.2 unchanged (v0.3 OI-005: anchor pinned; DT-side wiring is Stage 0+1)
- [x] Deterministic Sim #16 — `DOMAIN_TAG_HEADING = 0x16` allocated May 16, 2026 (#16 §3.5 v1.0.2; ERR-010-001 RESOLVED)
- [x] Event System #17 — `Publish API surface` §3.2.1 unchanged (v0.3 OI-005: anchor pinned)
- [x] Performance Optimization #18 — Appendix F.0 channel-registry schema + §6 ratify-not-override authority unchanged (v0.3 OI-002: re-anchored to Appendix F.0)
- [x] Testing Strategy #19 — test-framework APIs unchanged
- [x] Code Standards #20 — constant-tag grep tooling unchanged

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
- **Status at v0.3 (May 16, 2026): APPROVED.** All Outstanding
  Items at Approval Time (OI-001 / OI-002 / OI-003 / OI-005)
  RESOLVED. OI-006 remains carved out (gates `FR-PO-052`
  activation only, not #10 sign-off, per §6.1). All five sign-offs
  (R-01..R-05) recorded. `SPEC_INDEX.md` row 10 flipped
  `NOT STARTED → APPROVED` atomic with v0.3. ERR-010-001 closed.
  Two adversarial-review cycles complete (outline PASS-1 finding
  set in Appendix F; section-files PASS-1 finding set in
  Appendix H).

---

## 9.8 Version History

| Version | Date         | Author  | Notes                                                  | Reviewer |
|---------|--------------|---------|--------------------------------------------------------|----------|
| 0.1     | May 16, 2026 | drafter | Initial checklist; not yet exercised                   | pending  |
| 0.2     | May 16, 2026 | drafter | Added `DUEL_DISTURBANCE_GAP_SATURATION` row (H-4); updated OI-001 status (M-1); added v0.2 status note for `SPEC_INDEX.md` row 10 flip readiness. | pending |
| 0.3     | May 16, 2026 | drafter | APPROVAL. Added `HEADING_CONTACT_BUFFER_CAPACITY` row (OI-005 collision-buffer). All §9.1 constant-tag boxes exercised against §3.1 Master Physical Profile Table. All §9.2 XC / FM / EC references resolved. All §9.3 sign-offs (R-01..R-05) recorded. All §9.4 Outstanding Items RESOLVED (OI-001 / 002 / 003 / 005). §9.5 cross-spec re-audit complete. §9.7 Decision recorded as APPROVED. | granted (user-delegated authority per session) |
