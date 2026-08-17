# Heading Mechanics Specification #10 — Section 8: References, Citations, DOI Verification

**Created:** May 16, 2026
**Version:** 0.3
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

All upstream-spec anchors pinned May 16, 2026 (v0.3 OI-005 closure).

| Spec | Subsection | Consuming subsection of #10 | Citation purpose |
|------|-----------|----------------------------|------------------|
| #1 Ball Physics | §1.2 | §1.4, §3.1, §3.8 | Coordinate-system authority (corner-origin); `GRAVITY_MPS2`, `PITCH_LENGTH_M`, `PITCH_WIDTH_M` `[CROSS]` |
| #1 Ball Physics | §3.1.11.2 | §4.3 | `Ball.ApplyKick(velocity, spin, agentId, matchTime)` output API |
| #2 Agent Movement | §3.1.2 | §3.2, §4.2 | `AgentMovementState`, `GroundedReason.DIVING_HEADER` |
| #2 Agent Movement | §3.5.1 | §3.6, §4.2 | `Agent` class — `facing` XY-plane yaw, kinematic state exposure |
| #2 Agent Movement | §3.5.6 | §3.3, §3.5, §3.7, §4.2 | `PlayerAttributes` struct: `Heading`, `Strength`, `Balance` fields |
| #3 Collision System | §3.4.2 (`ICollisionEventConsumer` + `CollisionEvent`) | §3.7, §4.2 | Contact-event consumer interface; #10 implements and buffers per-frame (v0.3 OI-005) |
| #4 First Touch | §1.2 | §1.3 KD-3, §3.10 | 0.5 m height-threshold boundary statement — DOES NOT apply to head contacts |
| #5 Pass Mechanics | (consumed via `BallState` only, no #5 subsection coupling per KD-5) | §3.2, §3.4 | Cross-delivery `BallState` snapshot |
| #6 Shot Mechanics | §1.3 KD-6 | §1.3 KD-3, §3.10 | Body-part discriminator routing authority |
| #6 Shot Mechanics | §4 / §4.5 | §2.2, §4.3 | `ShotExecutedEvent` analogue pattern for `HeaderExecutedEvent` |
| #8 Decision Tree | §1.7.2 (Stage 0 deferral row; live HEADER dispatch is Stage 0+1) | §3.2, §4.2, §4.6.1 | `HeaderIntent` surface — Stage 0+1 activation atomic with #8 §7 stub promotion (v0.3 OI-005) |
| #16 Deterministic Sim | §3.2 | §3.7 | Entity-iteration ordering for duel resolution |
| #16 Deterministic Sim | §3.4 | §3.1, §4.4 | `DOMAIN_TAG_HEADING = 0x16` (`[CROSS]` post #16 §3.5 v1.0.2 patch, May 16, 2026; ERR-010-001 RESOLVED) |
| #16 Deterministic Sim | §4.1 | §3.4, §3.7, §4.4 | `DeterministicRngService` interface |
| #16 Deterministic Sim | §4.5 | §4.4 | Draw-site registry (`DRAW_SITE_*` IDs) |
| #17 Event System | §3.2.1 (`Publish API surface`) | §4.3 | `EventBus.Publish<T>` typed overloads — `IEventB` for `HeaderExecutedEvent`, `IEventC` for `HeaderAttemptFailedEvent` (v0.3 OI-005) |
| #18 Performance Optimization | §3.10 | §2.4, §6.2, §6.4 | Trace channel registry; 0-bytes-per-tick hot-path budget |
| #18 Performance Optimization | §6 | §6.1 | Ratify-not-override authority over #10 budgets (KD-2 of #18) |
| #19 Testing Strategy | §3.x | §5.x | Test-framework APIs, coverage tooling |
| #20 Code Standards | §3.x | §5.4.2, §9.1 | Constant-tag verification grep |

---

## 8.3 External References (Academic / Empirical)

Six anchor references. DOIs verified May 16, 2026 (v0.2 OI-003
closure); two v0.1 references (Bull 1985, Auger & Pellegrini 2007)
were not findable in standard databases (PubMed, DOI registry,
Google Scholar) and have been replaced with real, citable
equivalents covering the same physics.

- **Babbs, C. F. (2001).** "Biomechanics of Heading a Soccer Ball:
  Implications for Player Safety." *The Scientific World Journal* 1,
  281–322. DOI: [10.1100/tsw.2001.56](https://doi.org/10.1100/tsw.2001.56).
  Replaces v0.1 "Bull (1985)" (not findable). Provides
  spring-damper head-ball impact model with COR analysis;
  relevant to §3.4 / §3.5 power model and §3.6 spin-transfer
  baseline.

- **Tomczak, M., Walczak, M., Walczak, A. & Pelka, A. (2021).**
  "Heading in Soccer: Does Kinematics of the Head-Neck-Torso
  Alignment Influence Head Acceleration?" *Journal of Human
  Kinetics* 77, 175–187. DOI: [10.2478/hukin-2021-0012](https://doi.org/10.2478/hukin-2021-0012).
  Replaces v0.1 "Auger & Pellegrini (2007)" (not findable).
  Documents standing-vs-jumping head-kinematics differences in
  60 male players; relevant to §3.3 `JumpReach` derivation and
  apex-timing envelope.

- **Shewchenko, N., Withnall, C., Keown, M., Gittens, R. & Dvorak,
  J. (2005).** "Heading in football. Part 2: Biomechanics of ball
  heading and head response." *British Journal of Sports Medicine*
  39 (suppl 1), i26–i32. DOI:
  [10.1136/bjsm.2005.019042](https://doi.org/10.1136/bjsm.2005.019042).
  Relevant for §3.4 timing-tolerance and contact-point error scales.

- **Naunheim, R. S., Bayly, P. V., Standeven, J., Neubauer, J. S.,
  Lewis, L. M. & Genin, G. M. (2003).** "Linear and Angular Head
  Accelerations during Heading of a Soccer Ball." *Medicine &
  Science in Sports & Exercise* 35 (8), 1406–1412. DOI:
  [10.1249/01.MSS.0000078933.84527.AE](https://doi.org/10.1249/01.MSS.0000078933.84527.AE).
  Relevant for §3.6 `headAngularVelocity` magnitude calibration
  (peak angular acceleration ≈ 1302–1457 rad·s⁻² at 9–12 m·s⁻¹
  incoming).

- **Kirkendall, D. T., Jordan, S. E. & Garrett, W. E. (2001).**
  "Heading and Head Injuries in Soccer." *Sports Medicine* 31 (5),
  369–386. DOI:
  [10.2165/00007256-200131050-00006](https://doi.org/10.2165/00007256-200131050-00006).
  (Authoritative match-frequency review; cited in spec body as
  "Kirkendall & Garrett 2001" for brevity but the full author list
  is Kirkendall, Jordan, Garrett.) Relevant for §5.3.1 and §6.3.3
  header-frequency baseline (pass-1 M-3 recalibration source).

- **Opta / StatsBomb match-level header statistics** (modern
  empirical baseline). Relevant for §5.3.1 expected-header-count
  target (~28 headers per 90-minute match; ~10 % contested).
  Cited as a commercial-data baseline class rather than a single
  DOI — concrete source URL will be pinned by the validation
  drafter at Stage 0 calibration time when the specific Opta /
  StatsBomb match-corpus is licensed.

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
| `XC-010-004` | Deterministic Sim #16 §3.4 | `DOMAIN_TAG_HEADING = 0x16` catalogue row (`[CROSS]` — ERR-010-001 RESOLVED via #16 §3.5 v1.0.2 patch, May 16, 2026) |
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
| Babbs (2001) | ✅ Verified — DOI [10.1100/tsw.2001.56](https://doi.org/10.1100/tsw.2001.56). Replaces v0.1 "Bull (1985)" (not findable). |
| Tomczak et al. (2021) | ✅ Verified — DOI [10.2478/hukin-2021-0012](https://doi.org/10.2478/hukin-2021-0012). Replaces v0.1 "Auger & Pellegrini (2007)" (not findable). |
| Shewchenko et al. (2005) | ✅ Verified — DOI [10.1136/bjsm.2005.019042](https://doi.org/10.1136/bjsm.2005.019042). |
| Naunheim et al. (2003) | ✅ Verified — DOI [10.1249/01.MSS.0000078933.84527.AE](https://doi.org/10.1249/01.MSS.0000078933.84527.AE). |
| Kirkendall, Jordan & Garrett (2001) | ✅ Verified — DOI [10.2165/00007256-200131050-00006](https://doi.org/10.2165/00007256-200131050-00006). |
| Opta / StatsBomb | Commercial-data baseline class — concrete corpus URL pinned at Stage 0 calibration when license acquired (post-approval drafter task per §9.6). |

OI-003 RESOLVED May 16, 2026: 5/5 academic DOIs verified; commercial-data class accepted as a class citation per §9.6 post-approval pattern.

---

## 8.6 Version History

| Version | Date         | Author  | Notes                                                  | Reviewer |
|---------|--------------|---------|--------------------------------------------------------|----------|
| 0.1     | May 16, 2026 | drafter | Initial section draft from outline-detailed v1.1       | pending  |
| 0.2     | May 16, 2026 | drafter | v0.2 PASS-1 fix pass: `XC-010-005` anchored to Event System #17 §3.4.2 `EventBus.Publish` surface for `HeaderExecutedEvent` transport — adjudication framed as a future Match Referee concern not requiring an anchor at Stage 0 (L-4). | pending |
| 0.3     | May 16, 2026 | drafter | APPROVAL. §8.2 Upstream Specs Cited: all "subsection anchor TBD" entries resolved (#3 §3.4.2 / #8 §1.7.2 / #17 §3.2.1 / #1 §3.1.11.2). §8.3 External References: 5/5 academic DOIs verified May 16, 2026; v0.1 "Bull (1985)" replaced with Babbs (2001) DOI 10.1100/tsw.2001.56; v0.1 "Auger & Pellegrini (2007)" replaced with Tomczak et al. (2021) DOI 10.2478/hukin-2021-0012 (both v0.1 refs not findable in standard databases); Kirkendall, Jordan & Garrett (2001) DOI 10.2165/00007256-200131050-00006; Shewchenko et al. (2005) DOI 10.1136/bjsm.2005.019042; Naunheim et al. (2003) DOI 10.1249/01.MSS.0000078933.84527.AE. Opta/StatsBomb retained as commercial-data class. §8.4 `XC-010-004` promoted `[CROSS-PENDING] → [CROSS]` post #16 §3.5 v1.0.2 patch. §8.5 verification table updated to RESOLVED. OI-003 / OI-005 RESOLVED. | granted |
