# Stress-Test Reports — Index

> **Created:** May 18, 2026
> **Purpose:** Index of all Tier A and Tier B stress-test probe reports per `docs/tracking/stress-test-strategy.md`.

---

## Tier A Runs

| Date | Report | Probes run | FAIL | WARN | Notes |
|------|--------|-----------|------|------|-------|
| 2026-05-18 | [2026-05-18-tier-a-run-1.md](2026-05-18-tier-a-run-1.md) | A-01, A-03–A-06, A-08, A-10–A-12 (manual) | 3 | 2 | Initial run. A-02/A-07/A-09/A-14–A-16 deferred; A-13 skipped (network). |

---

## Tier B Reports

*(None yet — G-Code cadence has not started)*

---

## Open FAILs

| ID | Probe | Spec | Summary | Filed |
|----|-------|------|---------|-------|
*(all FAILs from run 1 resolved — see below)*

## Resolved FAILs

| ID | Probe | Spec | Summary | Resolved |
|----|-------|------|---------|---------|
| F-001 | A-06 / T-03 | Decision Tree #8 §3.2 | `PitchGeometry` class rewritten to corner-origin (0,0,0); `Vector3` constants; citation corrected to §1.2 | 2026-05-18 |
| F-002 | A-03 / T-05 | Attacking AI #15 §1/§2/§3/§4 | 7 stale `[CROSS-PENDING]` promoted to `[CROSS: #16 §3.4]` across all four body sections | 2026-05-18 |
| F-003 | A-04 / T-08 | Deterministic Sim #16 §3.4 | `_RESERVED_0x18_` and `_RESERVED_0x1C_` placeholder rows added | 2026-05-18 |
