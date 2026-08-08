# Scripted Build-Up Structures Specification #24 — Section 5: Test Plan

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.2 — PASS-1: T-BU-U-013 added (M-1); T-BU-I-004 / T-BU-U-011 / T-BU-U-012 extended (M-1/L-2/L-1). 13 unit tests.)
**Version:** 0.2
**Status:** APPROVED

---

## 5.1 Unit

| ID | Locks |
|---|---|
| T-BU-U-001 | rawZone thresholds at exactly 35.0/70.0 m (boundary values classify per §3.1's `<` convention) |
| T-BU-U-002 | Hysteresis worked example (§3.1: 35.5 holds, 37.2 commits, 34.1 holds, 32.9 commits) |
| T-BU-U-003 | NaN ball X holds committed zone (F1) |
| T-BU-U-004 | Catalogue invariant: every row within `BUILDUP_OFFSET_MAX_M` (FR-BU-008/F4) |
| T-BU-U-005 | `None` structure ⇒ (0,0) for every (zone, line, lane) (FR-BU-005) |
| T-BU-U-006 | GK slot receives no offset at any structure/zone (FR-BU-009) |
| T-BU-U-007 | FinalThird zone ⇒ (0,0) for every structure (FR-BU-004) |
| T-BU-U-008 | Suppression: CounterAttack/CounterPress arm `REGAIN_SUPPRESS_TICKS`; HoldShape/Regroup arm 0 (FR-BU-006) |
| T-BU-U-009 | Window countdown reaches exactly 0 and stays (never negative) |
| T-BU-U-010 | `BuildUpStructure` ordinal stability (None=0..InvertedFullBacks=3) |
| T-BU-U-011 | Deserialization gates: negative/over-cap suppress ticks AND invalid `CommittedZone` byte throw (F2, PASS-1 L-2) |
| T-BU-U-012 | Lane-symmetric lateral sign resolves toward pitch centre on both flanks; exactly 0 at y = 34 (§3.2, PASS-1 L-1) |
| T-BU-U-013 | Intra-team possessor change never arms the suppression window; opponent→this-team settled transition does (FR-BU-006, PASS-1 M-1) |

## 5.2 Integration

| ID | Locks |
|---|---|
| T-BU-I-001 | Overlay stage order: applied before spacing and clamp; an 8 m offset near the touchline still clamps on-pitch |
| T-BU-I-002 | **Away-team mirror**: identical tactic on the away team produces the mirror-image world-frame targets (the ERR-008-002 class lock, composition-level) |
| T-BU-I-003 | Phase gate: overlay inactive in OutOfPoss/TransToAtk/TransToDef even with structure set |
| T-BU-I-004 | Team-level regain mid-match arms the window from the real possession-changed signal (not a stub); a completed intra-team pass through the same signal does not (PASS-1 M-1) |
| T-BU-I-005 | Phase-D routing per team via `TestOnly_BuildUpStructure`; teams independent |
| T-BU-I-006 | Schema probe: zone/suppression state + dial feed the snapshot digest (at wiring) |

## 5.3 Determinism / closed-loop

| ID | Locks |
|---|---|
| T-BU-DET-001 | Two same-seed runs with `BackThree` set: bitwise-identical digests |
| T-BU-DET-002 | Default (`None`) run digest-identical to pre-#24 (byte-identity lock) |
| T-BU-DET-003 | Save/restore inside an open suppression window resumes byte-identically |
| sim_buildup-backthree-shape | #19 scenario: with `BackThree` in OwnThird, fullback mean lateral distance from centreline decreases and pivot mean X drops vs `None` baseline over a 10 s window (envelope predicates) |

## 5.4 FR traceability

Matrix in Appendix C, completed as tests land.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial plan: 12 unit, 6 integration, 3 determinism + 1 scenario; away-mirror case mandatory per project history. |
| 0.2 | 2026-07-08 | — | PASS-1: T-BU-U-013 added (M-1); T-BU-I-004 / T-BU-U-011 / T-BU-U-012 extended (M-1/L-2/L-1). 13 unit tests. |
#endregion
