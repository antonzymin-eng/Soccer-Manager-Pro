# Heading Mechanics Specification #10 — Section 8: References, Citations, DOI Verification

**Created:** May 16, 2026
**Version:** 0.1
**Status:** DRAFT
**Purpose:** Authoritative reference register for the spec.
Project-document cites, upstream-spec cites, external academic /
empirical references, and the typed cross-reference catalogue
(`XC-010-NNN`, `FM-010-NNN`, `EC-010-NNN`).

---

## 8.1 Project Documents Cited

| Document | Subject | Authority |
|----------|---------|-----------|
| `CLAUDE.md` | Coordinate origin (corner-origin), fatigue convention (0 = rested), tick-rate split (10 Hz / 60 Hz), constant-tag taxonomy `[GT]/[EST]/[FIXED]/[DERIVED]/[CROSS]/[CROSS-PENDING]` | Project root |
| `docs/specs/SPEC_INDEX.md` | Spec numbering / status authority | Project root |
| `docs/tracking/spec-error-log.md` | `ERR-010-001` back-prop entry for `DOMAIN_TAG_HEADING = 0x16` allocation (KD-10 / OI-001) | Tracking |
| `docs/tracking/PROGRESS.md` | Schedule context | Tracking |
| `docs/tracking/certification-platform.md` | Stage-0 host pin (gates `FR-PO-052` activation; does not gate #10 sign-off) | Tracking |

---

## 8.2 Upstream Specs Cited (Section-Level)

Anchors verified during section drafting where pinnable; remaining
"subsection anchor TBD; pinned during pass-2 review" entries
tracked as OI-005.

| Spec | Subsection | Consuming subsection of #10 | Citation purpose |
|------|-----------|----------------------------|------------------|
| #1 Ball Physics | §1.2 | §1.4, §3.1, §3.8 | Coordinate-system authority (corner-origin); `GRAVITY_MPS2`, `PITCH_LENGTH_M`, `PITCH_WIDTH_M` `[CROSS]` |
| #1 Ball Physics | §3.1.11.2 | §4.3 | `Ball.ApplyKick(velocity, spin, agentId, matchTime)` output API |
| #2 Agent Movement | §3.1.2 | §3.2, §4.2 | `AgentMovementState`, `GroundedReason.DIVING_HEADER` |
| #2 Agent Movement | §3.5.1 | §3.6, §4.2 | `Agent` class — `facing` XY-plane yaw, kinematic state exposure |
| #2 Agent Movement | §3.5.6 | §3.3, §3.5, §3.7, §4.2 | `PlayerAttributes` struct: `Heading`, `Strength`, `Balance` fields |
| #3 Collision System | (subsection anchor TBD; pinned during pass-2 review) | §3.7, §4.2 | Contact-event API for contested-duel resolution |
| #4 First Touch | §1.2 | §1.3 KD-3, §3.10 | 0.5 m height-threshold boundary statement — DOES NOT apply to head contacts |
| #5 Pass Mechanics | (consumed via `BallState` only, no #5 subsection coupling per KD-5) | §3.2, §3.4 | Cross-delivery `BallState` snapshot |
| #6 Shot Mechanics | §1.3 KD-6 | §1.3 KD-3, §3.10 | Body-part discriminator routing authority |
| #6 Shot Mechanics | §4 / §4.5 | §2.2, §4.3 | `ShotExecutedEvent` analogue pattern for `HeaderExecutedEvent` |
| #8 Decision Tree | §1.7.x (subsection anchor TBD; pinned during pass-2 review) | §3.2, §4.2 | `HeaderIntent` (target / power / contact-point intent surface) |
| #16 Deterministic Sim | §3.2 | §3.7 | Entity-iteration ordering for duel resolution |
| #16 Deterministic Sim | §3.4 | §3.1, §4.4 | `DOMAIN_TAG` catalogue (pending `0x16` allocation per KD-10) |
| #16 Deterministic Sim | §4.1 | §3.4, §3.7, §4.4 | `DeterministicRngService` interface |
| #16 Deterministic Sim | §4.5 | §4.4 | Draw-site registry (`DRAW_SITE_*` IDs) |
| #17 Event System | (subsection anchor TBD; pinned during pass-2 review) | §4.3 | `EventBus.Publish<T>` API |
| #18 Performance Optimization | §3.10 | §2.4, §6.2, §6.4 | Trace channel registry; 0-bytes-per-tick hot-path budget |
| #18 Performance Optimization | §6 | §6.1 | Ratify-not-override authority over #10 budgets (KD-2 of #18) |
| #19 Testing Strategy | §3.x | §5.x | Test-framework APIs, coverage tooling |
| #20 Code Standards | §3.x | §5.4.2, §9.1 | Constant-tag verification grep |

---

## 8.3 External References (Academic / Empirical)

Six anchor references pre-identified at outline stage per pass-1
L-6. DOIs to be verified during pass-2 review (OI-003).

- **Bull (1985).** Coefficient of restitution for head-ball impacts.
  Relevant for §3.4 / §3.5 power model and §3.6 spin-transfer
  baseline. DOI: TBD.

- **Auger & Pellegrini (2007).** Head kinematics under jumping
  contact. Relevant for §3.3 `JumpReach` derivation and apex-timing
  envelope. DOI: TBD.

- **Shewchenko, Withnall, Keown, Gittens & Dvorak (2005).** Heading
  in soccer: dynamic, mechanical, and player-perception data.
  Relevant for §3.4 timing-tolerance and contact-point error
  scales. DOI: TBD.

- **Naunheim, Bayly, Standeven, Neubauer, Lewis & Genin (2003).**
  Linear and angular head accelerations during heading. Relevant
  for §3.6 `headAngularVelocity` magnitude calibration. DOI: TBD.

- **Kirkendall & Garrett (2001).** Heading in adult soccer.
  Relevant for §5.3.1 and §6.3.3 header-frequency baseline (pass-1
  M-3 recalibration source). DOI: TBD.

- **Opta / StatsBomb match-level header statistics** (modern
  empirical baseline). Relevant for §5.3.1 expected-header-count
  target (~28 headers per 90-minute match; ~10 % contested). Source
  citation pinned during pass-2 review.

---

## 8.4 Typed Cross-References

### 8.4.1 Cross-Spec References (`XC-010-NNN`)

Pass-1 M-6 dropped the former `XC-010-001` (AM #2 EntityId no-reuse)
as unmotivated — #10 consumes `agentId` only within single contact
frames, never caches across despawn boundaries. Remaining entries
renumbered 001–006.

| ID | Target | Citation purpose |
|----|--------|------------------|
| `XC-010-001` | Ball Physics #1 §1.2 | Coordinate-system origin (corner-origin), gravity & pitch dimensions |
| `XC-010-002` | Shot Mechanics #6 §1.3 KD-6 | Body-part discriminator routing authority (KD-3 of #10) |
| `XC-010-003` | First Touch #4 §1.2 | 0.5 m height-threshold boundary reaffirmation — does NOT apply to head |
| `XC-010-004` | Deterministic Sim #16 §3.4 | `DOMAIN_TAG_HEADING = 0x16` catalogue row (`[CROSS-PENDING]` per KD-10) |
| `XC-010-005` | Event System #17 §3.4.2 (`EventBus.Publish` surface for `HeaderExecutedEvent`) | Own-goal-shape trajectory FLAG transport. Adjudication (whether the trajectory actually crossed the goal-line) is a future Match Referee concern; #10 publishes the flag via the existing #17 event-publish API, no Match Referee anchor is required at Stage 0 (v0.2 L-4). |
| `XC-010-006` | Performance Optimization #18 §3.10 | Trace channel registry for `heading.*` channels |

### 8.4.2 Formula References (`FM-010-NNN`)

| ID | Formula | Section |
|----|---------|---------|
| `FM-010-001` | `JumpReach_m` derivation | §3.3 |
| `FM-010-002` | `contactQualityScalar` (asymmetric timing + point-error blend) | §3.4 |
| `FM-010-003` | `outgoingSpeed` (power × intent × quality, fatigue-modulated) | §3.5 |
| `FM-010-004` | `outgoingSpin` (head angular velocity + preservation + reversal) | §3.6 |
| `FM-010-005` | `duelScore` (weighted Balance + Strength + Heading) | §3.7 |

### 8.4.3 Edge-Case References (`EC-010-NNN`)

| ID | Failure mode | Section |
|----|--------------|---------|
| `EC-010-001` | F-01: Mistimed jump (`timingOffsetMs > MAX_LATE_TOLERANCE_MS`) | §2.3 / §3.9 |
| `EC-010-002` | F-02: Jump apex below ball altitude | §2.3 / §3.9 |
| `EC-010-003` | F-03: Ball position outside `HEAD_CONTACT_VOLUME` | §2.3 / §3.9 |
| `EC-010-004` | F-04: Multi-way contested duel (winner-only emits executed event) | §2.3 / §3.7 |
| `EC-010-005` | F-05: `targetIntent` outside pitch bounding box (clamped) | §2.3 |
| `EC-010-006` | F-06: Stale `BallState` snapshot (re-query, do not extrapolate) | §2.3 |
| `EC-010-007` | F-07: `contactPointIntent` outside head-local envelope (clamp + penalty) | §2.3 / §3.4 |

---

## 8.5 DOI / URL Verification Status

| Reference | Status |
|-----------|--------|
| Bull (1985) | DOI not yet verified — OI-003 |
| Auger & Pellegrini (2007) | DOI not yet verified — OI-003 |
| Shewchenko et al. (2005) | DOI not yet verified — OI-003 |
| Naunheim et al. (2003) | DOI not yet verified — OI-003 |
| Kirkendall & Garrett (2001) | DOI not yet verified — OI-003 |
| Opta / StatsBomb | Source URL pinned during pass-2 review |

Verification is a §9 Approval Checklist gate; not blocking
section-file draft completion.

---

## 8.6 Version History

| Version | Date         | Author  | Notes                                                  | Reviewer |
|---------|--------------|---------|--------------------------------------------------------|----------|
| 0.1     | May 16, 2026 | drafter | Initial section draft from outline-detailed v1.1       | pending  |
| 0.2     | May 16, 2026 | drafter | v0.2 PASS-1 fix pass: `XC-010-005` anchored to Event System #17 §3.4.2 `EventBus.Publish` surface for `HeaderExecutedEvent` transport — adjudication framed as a future Match Referee concern not requiring an anchor at Stage 0 (L-4). | pending |
