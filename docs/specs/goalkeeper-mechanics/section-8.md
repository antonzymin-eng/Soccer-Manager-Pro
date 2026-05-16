# Goalkeeper Mechanics Specification #11 — Section 8: References, Citations, DOI Verification

**Created:** May 16, 2026
**Version:** 0.1
**Status:** DRAFT
**Purpose:** Catalogue project documents, upstream specs, external
academic references with DOI verification, and the typed
cross-reference IDs (`XC-011-NNN`, `FM-011-NNN`, `EC-011-NNN`)
emitted by Goalkeeper Mechanics #11.

---

## 8.1 Project Documents Cited

- `CLAUDE.md` — coordinate origin, fatigue convention, tick-rate
  split, constant-tag policy, deterministic-RNG invariant.
- `SPEC_INDEX.md` — canonical spec numbering authority.
- `docs/tracking/spec-error-log.md` — ERR-011-001 back-prop entry
  for `DOMAIN_TAG_GOALKEEPER` allocation; ERR-012-001
  collision-management policy reference (KD-7).
- `docs/tracking/PROGRESS.md` — schedule and milestone tracking.
- `docs/tracking/certification-platform.md` — Stage-0 host pin
  status (OI-008 carve-out; not blocking #11 sign-off).

---

## 8.2 Upstream Specs Cited (section-level)

| Spec | Section | Citation purpose | Consumed in |
|------|---------|------------------|-------------|
| Ball Physics #1 | §1.2 | Coordinate origin (corner-origin) | §1.1 / §3 |
| Ball Physics #1 | §3.1.11.2 | `Ball.ApplyKick(velocity, spin, agentId, matchTime)` | §3.5 / §3.8 / §3.9 |
| Ball Physics #1 | §3.1 possession surface | `Ball.SetPossessor(agentId)` (OI-006 verification posture) | §3.5 / §3.7 |
| Agent Movement #2 | §3.1.2 | `AgentMovementState`, `GroundedReason` enums | §3.1 / §3.3 |
| Agent Movement #2 | §3.5.1 | `Agent` class XY kinematics surface | §3.1 / §3.3 / §3.7 |
| Agent Movement #2 | §3.5.6 | `PlayerAttributes` field reads | §3.2 / §3.5 / §3.7 |
| Collision System #3 | §3.4.2 | `ICollisionEventConsumer` pattern | §3.5 / §3.6 |
| First Touch #4 | §1.2 | Boundary statement — head exception (#10 KD-7); foot save-attempts #11-owned | §1.2 |
| Pass Mechanics #5 | §1.7 / §3 intent surface | `PassIntent` consumer surface (KD-6) | §3.8 |
| Shot Mechanics #6 | §4.5 | `ShotExecutedEvent` | §3.2 |
| Shot Mechanics #6 | §1.3 KD-6 | Body-part discriminator authority | KD-4 / §3.6 |
| Perception System #7 | §3 visibility latency | `PERCEPTION_BASE_LATENCY_MS` consumption | §3.2 |
| Decision Tree #8 | §1.7 intent surface | GK-branch intent vocabulary extension | §3.1 / §3.2 / §3.7 / §3.8 |
| Heading Mechanics #10 | §3.7 | Contested-duel mechanism for head contacts | §3.6 |
| Heading Mechanics #10 | KD-7 | GK head-contact ownership inversion | §1.1 / KD-4 |
| Positioning AI #12 | §3.3.3 | GK baseline consumer contract (KD-13 ratification) | §3.3.0 |
| Deterministic Simulation #16 | §3.2 | Entity iteration order | §3.6 |
| Deterministic Simulation #16 | §3.4 | `DOMAIN_TAG` catalogue (pending `0x17` per ERR-011-001) | §3.4 / §4.4 |
| Deterministic Simulation #16 | §4.1 / §4.5 | RNG service + draw-site registry | §3.3 / §3.5 / §3.6 / §4.4 |
| Event System #17 | §3.2.1 | Publish API surface | §3.9 / §4.3 |
| Performance Optimization #18 | §3.10 / Appendix F.0 / §6 | Hot-path budget + channel registry + ratify-not-override | §2.4 / §4.5 / §6 |
| Testing Strategy #19 | §3 | Test framework conventions | §5 |
| Code Standards #20 | §3 | Constant-tag verification policy | §9.1 |

---

## 8.3 External References (Academic / Empirical)

DOI verification status as of 2026-05-16: marked `[CITATION-PENDING]`
where the DOI lookup has not yet been executed. OI-003 tracks
verification completion as a post-`IN REVIEW` follow-up; not
blocking.

| Reference | DOI / verification | Purpose |
|-----------|-------------------|---------|
| Dicks, M., Davids, K. & Button, C. (2010). Individual differences in the visual control of intercepting a penalty kick. *Human Movement Science*. | `[CITATION-PENDING]` DOI lookup | Visual perception and action in 1v1 GK (§3.2 reaction model anchor) |
| Savelsbergh, G. J. P., Williams, A. M., van der Kamp, J. & Ward, P. (2002). Visual search, anticipation and expertise in soccer goalkeepers. *Journal of Sports Sciences*, 20(3), 279–287. | DOI [10.1080/026404102320183319](https://doi.org/10.1080/026404102320183319) | Anticipation skill in penalty saves (§3.2 `Reflexes` modulation) |
| Spratford, W., Mellifont, R. & Burkett, B. (2009). The influence of dive direction on the movement characteristics of elite football goalkeepers. *Journal of Sports Sciences*. | `[CITATION-PENDING]` DOI lookup | Biomechanics of dive launch + peak reach (§3.3 anchor) |
| Suzuki, S., Togari, H., Isokawa, M., Ohashi, J. & Ohgushi, T. (1988). Analysis of the goalkeeper's diving motion. *Science and Football*. | `[CITATION-PENDING]` DOI lookup (conference proceedings; ISBN-based citation if no DOI) | Hand-ball contact geometry baselines (§3.5) |
| Williams, A. M. & Burwitz, L. (1993). Advance cue utilisation by soccer goalkeepers. In *Science and Football II*. | `[CITATION-PENDING]` DOI lookup (book chapter; ISBN-based citation if no DOI) | Anticipation modelling (§3.2 / KD-18 asymmetry rationale) |
| Opta / StatsBomb shots-on-target, saves-per-match, crosses-per-match, 1v1-conversion-rate baseline (commercial data class). | Commercial data; not DOI-verifiable. Retained per Heading #10 §9.6 commercial-data baseline class precedent. | §5.3 validation scenarios; §6.3 frequency anchors |

**Note (OI-003).** Per Heading #10 OI-003 closure pattern, fabricated
references are forbidden. If a `[CITATION-PENDING]` reference fails
verification during the OI-003 follow-up, it is replaced with a
verified equivalent or dropped (Heading #10 replaced two
unverifiable Bull 1985 / Auger & Pellegrini 2007 references with
Babbs 2001 / Tomczak 2021 equivalents under the same rule). The
Opta/StatsBomb commercial-data baseline class is retained.

---

## 8.4 Typed Cross-References

Allocated cross-reference IDs emitted by Goalkeeper Mechanics #11.

### 8.4.1 Cross-spec references (`XC-011-NNN`)

| ID | Target | Purpose |
|----|--------|---------|
| `XC-011-001` | Ball Physics #1 §1.2 | Coordinate origin |
| `XC-011-002` | Ball Physics #1 §3.1.11.2 | `Ball.ApplyKick` surface |
| `XC-011-003` | Ball Physics #1 §3.1 possession surface | `Ball.SetPossessor` / `BallState.PossessorId` (OI-006) |
| `XC-011-004` | Shot Mechanics #6 §4.5 | `ShotExecutedEvent` |
| `XC-011-005` | Heading Mechanics #10 KD-7 / §3.7 | GK head-contact ownership inversion + duel mechanism for head route |
| `XC-011-006` | Positioning AI #12 §3.3.3 | GK baseline consumer contract; ratification of three GK constants (KD-13) |
| `XC-011-007` | Deterministic Simulation #16 §3.4 | `DOMAIN_TAG_GOALKEEPER` catalogue row (`0x17` pending per ERR-011-001) |
| `XC-011-008` | Event System #17 §3.2.1 | Publish API surface |
| `XC-011-009` | Pass Mechanics #5 §3 | `PassIntent` consumer surface (KD-6) |
| `XC-011-010` | Collision System #3 §3.4.2 | `ICollisionEventConsumer` pattern (KD-5) |
| `XC-011-011` | Perception System #7 §3 | Visibility-cone latency surface |
| `XC-011-012` | Agent Movement #2 §3.5.6 | `PlayerAttributes` field-read contract |
| `XC-011-013` | Decision Tree #8 §1.7 | GK-branch intent surface |
| `XC-011-014` | Performance Optimization #18 Appendix F.0 | Channel registry schema (back-prop at Stage 0+1) |
| `XC-011-015` | Performance Optimization #18 §6 | Ratify-not-override authority |

### 8.4.2 Formula references (`FM-011-NNN`)

| ID | Formula | Section |
|----|---------|---------|
| `FM-011-001` | `requiredReactionMs` | §3.2.2 |
| `FM-011-002` | `reactionWindowAchieved` | §3.2.3 |
| `FM-011-003` | `handlingQualityScalar` | §3.5.1 |
| `FM-011-004` | `diveLaunchImpulse` / `peakHandZ_m` | §3.3.1 / §3.3.3 |
| `FM-011-005` | `crossClaimDuelScore` | §3.6.3 |
| `FM-011-006` | `parryVelocity` / `deflectVelocity` / `spillVelocity` | §3.5.3 |
| `FM-011-007` | `rushLaunchMps` | §3.7.1 |

### 8.4.3 Edge-case references (`EC-011-NNN`)

| ID | Failure mode | Section |
|----|--------------|---------|
| `EC-011-001` | F-01 Mistimed dive | §2.3 |
| `EC-011-002` | F-02 Wrong-direction dive | §2.3 |
| `EC-011-003` | F-03 Out-of-reach | §2.3 |
| `EC-011-004` | F-04 Cross-claim disturbed | §2.3 |
| `EC-011-005` | F-05 Missing receiver | §2.3 |
| `EC-011-006` | F-06 Stale `BallState` | §2.3 |
| `EC-011-007` | F-07 Non-eligible state | §2.3 |
| `EC-011-008` | F-08 Rush ball interception | §2.3 |
| `EC-011-009` | F-09 Out-of-bounds distribution | §2.3 |
| `EC-011-010` | F-10 Intent range-clamp | §2.3 |

---

## 8.5 Version History

| Version | Date | Author | Notes | Reviewer |
|---------|------|--------|-------|----------|
| 0.1 | May 16, 2026 | initial draft | First v0.1 from outline v1.2; 5 project-doc citations, 22 upstream-spec rows, 6 external academic references (2 verified DOI / 4 `[CITATION-PENDING]` per OI-003 follow-up), 32 typed cross-references (15 XC / 7 FM / 10 EC) | self-pass-1 in `adversarial-review-section-files-v1.md` |
