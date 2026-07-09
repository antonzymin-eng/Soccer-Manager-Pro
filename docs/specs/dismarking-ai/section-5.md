# Dismarking & Marker-Awareness AI Specification #23 — Section 5: Test Plan

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

Test IDs use the `T-DM-` prefix (`U` unit, `I` integration, `DET` determinism), registered with the
#19 framework conventions. Closed-loop scenarios ride the #19 `ScenarioRunner` per the project's
post-AR-7/AR-12 lesson that pure-function suites encode rather than catch composition defects.

## 5.1 Unit

| ID | Locks |
|---|---|
| T-DM-U-001 | FM-DM-01 worked example exactly (pressure 0.42 from §3.1 inputs) |
| T-DM-U-002 | proximity01 = 0 when no perceived opponent within `MARKING_RADIUS_M` |
| T-DM-U-003 | Unperceived marker ⇒ zero pressure (opponent present in ground truth, absent from `FilteredView`) — the KD-1 lock |
| T-DM-U-004 | Dwell increments to cap, never beyond; decay reaches exactly 0; `LastMarkerId` clears only at 0 |
| T-DM-U-005 | Marker hand-off (different `AgentId` inside radius) does NOT reset dwell |
| T-DM-U-006 | Phase ≠ InPoss ⇒ identity + decay-only dwell (FR-DM-006) |
| T-DM-U-007 | NaN/Infinity perceived position ⇒ decay path, no NaN propagation (F1) |
| T-DM-U-008 | Coincident marker (< eps) ⇒ offset skipped, pressure unaffected (F3) |
| T-DM-U-009 | Offset magnitude formula per §3.3 worked example (1.05 m); direction is unit-length away from marker |
| T-DM-U-010 | `Off` scalar ⇒ zero offset and ×1.0 penalty for every pressure value (FR-DM-012) |
| T-DM-U-011 | Penalty `mult` per §3.4 worked example (0.832); floor at `TARGET_MARKED_UTILITY_MULT` |
| T-DM-U-012 | awareness01 = 0 ⇒ mult = 1.0 (unaware passer ignores markedness) |
| T-DM-U-013 | `DismarkIntensity` ordinal stability (Off=0/Conservative=1/Aggressive=2) |
| T-DM-U-014 | Dwell-state deserialization gates: negative / above-cap `DwellTicks`, out-of-range `LastMarkerId`, and the `DwellTicks > 0 ∧ LastMarkerId = −1` incoherence all throw (F2, PASS-1 L-2) |

## 5.2 Integration

| ID | Locks |
|---|---|
| T-DM-I-001 | Offset stage position: composed target with offset still passes the pitch clamp; an offset that would exit the pitch is clamped (FR-DM-008) |
| T-DM-I-002 | Carrier and GK receive no offset at maximum pressure (FR-DM-007) |
| T-DM-I-003 | One-stride staleness contract (§3.2 PASS-1 M-1): the offset stage at stride N consumes the pressure computed in stride N−1's per-agent pass; the §3.4 penalty consumes the same-pass value |
| T-DM-I-004 | Phase-D routing: per-team `DismarkIntensity` reaches both consumers' fields; teams independent (via `TestOnly_DismarkIntensity`) |
| T-DM-I-005 | `SNAPSHOT_SCHEMA_VERSION` probe: dwell state + dial feed the snapshot digest (schema-pin test, at wiring) |

## 5.3 Determinism / closed-loop

| ID | Locks |
|---|---|
| T-DM-DET-001 | Two same-seed runs with `Aggressive` set: bitwise-identical digest chains |
| T-DM-DET-002 | Default (`Off`) run digest-identical to a pre-#23 build (byte-identity lock, FR-DM-012) |
| T-DM-DET-003 | Save/restore mid-dwell resumes byte-identically (dwell state round-trip) |
| sim_dismark-shakes-marker | #19 scenario: a marked off-ball attacker under `Aggressive` increases mean marker distance over a 10 s window vs the `Off` baseline (envelope predicate, not exact-value) |

## 5.4 FR traceability

Every FR-DM row in §2.1 maps to ≥1 test above; the traceability matrix lives in Appendix C and is
completed (not fabricated) as tests land — matching the honesty rule for checklists.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial test plan: 14 unit, 5 integration, 3 determinism + 1 scenario. |
| 0.2 | 2026-07-08 | — | PASS-1: T-DM-I-003 rewritten to the one-stride contract (M-1); T-DM-U-014 extended (L-2). |
#endregion
