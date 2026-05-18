# Stress-Test Reports — Index

> **Created:** May 18, 2026
> **Purpose:** Index of all Tier A and Tier B stress-test probe reports per `docs/tracking/stress-test-strategy.md`.

---

## Tier A Runs

| Date | Report | Probes run | FAIL | WARN | Notes |
|------|--------|-----------|------|------|-------|
| 2026-05-18 | [2026-05-18-tier-a-run-1.md](2026-05-18-tier-a-run-1.md) | A-01, A-03–A-06, A-08, A-10–A-12 (manual) | 3 | 2 | Initial run. A-02/A-07/A-09/A-14–A-16 deferred; A-13 skipped (network). |
| 2026-05-18 | [2026-05-18-tier-a-run-2.md](2026-05-18-tier-a-run-2.md) | A-02 (partial), A-03 re-run, A-07, A-09, A-14, A-15, A-16 | 2 | 1 (×147) | Run 1 FAILs confirmed fixed. New: FAIL-4 (stale CROSS-PENDING body text in #11/12/13/14; #12 wrong value), FAIL-5 (file-manifest drift). A-13 skipped (network). |

---

## Tier B Reports

*(None yet — G-Code cadence has not started)*

---

## Open FAILs

| ID | Probe | Spec | Summary | Filed |
|----|-------|------|---------|-------|
| F-004 | A-03 / T-05 | #11 §1/§2/§4; #12 §1/§2/§3/§4; #13 §2; #14 §1/§2/§3/§5 | 12 stale `[CROSS-PENDING]` in body text (§6.1 catalogues are correct); #12 §3.9 and §4.6 also have wrong value `0x16` → `0x17` | 2026-05-18 |
| F-005 | A-09 / T-17 | `docs/tracking/file-manifest.md` | 3 new tracking files (stress-test-strategy.md, stress-reports/INDEX.md, stress-reports/tier-a-run-1.md) missing; rows 11/12/14 show stale status (NOT STARTED / IN REVIEW instead of APPROVED) | 2026-05-18 |

## Resolved FAILs

| ID | Probe | Spec | Summary | Resolved |
|----|-------|------|---------|---------|
| F-001 | A-06 / T-03 | Decision Tree #8 §3.2 | `PitchGeometry` class rewritten to corner-origin (0,0,0); `Vector3` constants; citation corrected to §1.2 | 2026-05-18 |
| F-002 | A-03 / T-05 | Attacking AI #15 §1/§2/§3/§4 | 7 stale `[CROSS-PENDING]` promoted to `[CROSS: #16 §3.4]` across all four body sections | 2026-05-18 |
| F-003 | A-04 / T-08 | Deterministic Sim #16 §3.4 | `_RESERVED_0x18_` and `_RESERVED_0x1C_` placeholder rows added | 2026-05-18 |
