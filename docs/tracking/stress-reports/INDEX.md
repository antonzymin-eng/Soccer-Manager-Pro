# Stress-Test Reports — Index

> **Created:** May 18, 2026
> **Purpose:** Index of all Tier A and Tier B stress-test probe reports per `docs/tracking/stress-test-strategy.md`.

---

## Tier A Runs

| Date | Report | Probes run | FAIL | WARN | Notes |
|------|--------|-----------|------|------|-------|
| 2026-05-18 | [2026-05-18-tier-a-run-1.md](2026-05-18-tier-a-run-1.md) | A-01, A-03–A-06, A-08, A-10–A-12 (manual) | 3 | 2 | Initial run. A-02/A-07/A-09/A-14–A-16 deferred; A-13 skipped (network). |
| 2026-05-18 | [2026-05-18-tier-a-run-2.md](2026-05-18-tier-a-run-2.md) | A-02 (partial), A-03 re-run, A-07, A-09, A-14, A-15, A-16 | 2 | 1 (×147) | Run 1 FAILs confirmed fixed. New: FAIL-4 (stale CROSS-PENDING body text in #11/12/13/14; #12 wrong value), FAIL-5 (file-manifest drift). A-13 skipped (network). |
| 2026-05-18 | [2026-05-18-tier-a-run-3.md](2026-05-18-tier-a-run-3.md) | Full re-run A-01–A-12, A-14–A-16 | 1→0 | 1 (×147) | Run 2 FAILs confirmed fixed. New: FAIL-6 (A-02: 11 stale `[EST]` body-text tags in #12 §3; SPACING_MAX_PASSES in §6.1) — fixed in this pass. FIND-7/8/9/10 (stale headers + ERR-012-001 status) fixed. A-13 skipped (network). Zero open FAILs after this run. |
| 2026-05-18 | [2026-05-18-tier-a-run-4.md](2026-05-18-tier-a-run-4.md) | Full re-run A-01–A-16 + A-16 triage (10 entries) | 1→0 | 1 (×137) | Run 3 FAIL confirmed fixed. New: FAIL-7 (A-02: `ATTACK_DWELL_TICKS [EST]` in #15 §1.4 glossary) — fixed. FIND-12 (v0.1 DRAFT headers in #15 §§1–5), FIND-13 (defensive-ai §9 checklist item 4 evidence stale) — fixed. A-16 triage inaugurated: 10 entries, all XC- confirmed; 137 open. A-13 skipped (network). |
| 2026-05-19 | [2026-05-19-tier-a-run-5.md](2026-05-19-tier-a-run-5.md) | A-16 triage full corpus sweep | 0 | 0 | Run 4 FAIL confirmed fixed. A-16 triage COMPLETE: 167/167 entries, all XC- confirmed, 0 open. No new FAILs or FINDs. A-13 skipped (network). |

---

## Tier B Reports

*(None yet — G-Code cadence has not started)*

---

## Open FAILs

*(None — all FAILs resolved as of Run 4, May 18, 2026. A-16 triage COMPLETE as of Run 5, May 19, 2026: 167/167 entries closed, 0 open.)*

## Resolved FAILs

| ID | Probe | Spec | Summary | Resolved |
|----|-------|------|---------|---------|
| F-001 | A-06 / T-03 | Decision Tree #8 §3.2 | `PitchGeometry` class rewritten to corner-origin (0,0,0); `Vector3` constants; citation corrected to §1.2 | 2026-05-18 |
| F-002 | A-03 / T-05 | Attacking AI #15 §1/§2/§3/§4 | 7 stale `[CROSS-PENDING]` promoted to `[CROSS: #16 §3.4]` across all four body sections | 2026-05-18 |
| F-003 | A-04 / T-08 | Deterministic Sim #16 §3.4 | `_RESERVED_0x18_` and `_RESERVED_0x1C_` placeholder rows added | 2026-05-18 |
| F-004 | A-03 / T-05 | #11 §1/§2/§4; #12 §1/§2/§3/§4; #13 §2; #14 §1/§2/§3/§5 | 12 stale `[CROSS-PENDING]` in body text (§6.1 catalogues correct); #12 §3/§4 wrong value `0x16` → `0x17` | 2026-05-18 |
| F-005 | A-09 / T-17 | `docs/tracking/file-manifest.md` | 3 tracking files missing; rows 11/12/14 stale status | 2026-05-18 |
| F-006 | A-02 / T-04 | Positioning AI #12 §3 + §6.1 | 11 `[EST]` body-text tags and `SPACING_MAX_PASSES` in §6.1 not promoted to `[GT]` in v0.3 APPROVED patch | 2026-05-18 |
| F-007 | A-02 / T-04 | Attacking AI #15 §1.4 | `ATTACK_DWELL_TICKS [EST]` in §1.4 glossary — v0.2 fix pass swept §2/§3 but missed §1 | 2026-05-18 |
