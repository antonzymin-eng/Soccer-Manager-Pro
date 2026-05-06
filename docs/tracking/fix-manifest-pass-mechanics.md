# Pass Mechanics Spec #5 — Audit Fix Manifest

**Purpose:** Documents all fixes applied to resolve the 19 findings from the March 6, 2026 Comprehensive Audit.
**Date:** March 25, 2026, 11:00 PM PST
**Files Modified:** 12 of 12 Pass Mechanics spec files (Appendices unchanged — no findings)

---

## FIX SUMMARY

| ID | Severity | Status | Description |
|---|---|---|---|
| C-01 | CRITICAL | ✅ PREVIOUSLY RESOLVED | §3.3–§3.9 drafted (3 files, March 7, 2026) |
| C-02 | CRITICAL | ✅ FIXED | §9 pass type names rewritten against §3.1.2 actual enum |
| C-03 | CRITICAL | ✅ FIXED | Decision Tree #7→#8: 31 replacements across 10 files |
| C-04 | CRITICAL | ✅ FIXED | §2 FR-02 fatigue convention corrected (0=rested, 1=fatigued) |
| C-05 | CRITICAL | ✅ FIXED | Dual status replaced with APPROVAL SUSPENDED |
| M-01 | MAJOR | ✅ FIXED | Chip distMax 25m→20m in §3.2 boundary check and constants table |
| M-02 | MAJOR | ✅ FIXED | Chip velocity prose 8–16→5–14 m/s in §3.1.11 |
| M-03 | MAJOR | ✅ FIXED | §2.4.3 profile table marked SUPERSEDED by §3.1.4 |
| M-04 | MAJOR | ✅ FIXED | §9 test counts SV 6→8, TR 8→16 to match §5 actuals |
| M-05 | MAJOR | ✅ FIXED | V_OFFSET corrected to per-type [GT] constant (not derived formula) |
| M-06 | MAJOR | ✅ FIXED | §8 footer version 1.0→1.2 |
| M-07 | MAJOR | ✅ VERIFIED CORRECT | §5.14.4 "82 unit tests" confirmed correct (SV=8 + TR=16) |
| Mod-01 | MODERATE | ✅ FIXED | §2 FR-03 Lofted angle range 35°→45°, Cross Flat/High aligned with §3.1 |
| Mod-02 | MODERATE | ✅ FIXED | Chip/Lobbed synonym added to Outline taxonomy diagram |
| Mod-03 | MODERATE | ✅ FIXED | §4 prerequisites §3.1 v1.0→v1.1 |
| Mod-04 | MODERATE | ✅ FIXED | OQ-6 marked resolved — parameter-based ApplyKick, no KickType enum |
| Min-01 | MINOR | ✅ FIXED | §1 Spec Error Log v1.0→v1.4 |
| Min-02 | MINOR | ✅ FIXED | §5.1.5 performance targets aligned with §6 (was impossible: mean > p95) |
| Min-03 | MINOR | N/A | No action required (forward reference tracking only) |

**Additional fix discovered during execution:**
- §9 Known Limitations #8 falsely claimed HEADER is a PassType enum value. Corrected to state heading is excluded, owned by Spec #10.
- §9 file table: updated to include 3 new §3.3–§3.9 files, corrected §8 version v1.0→v1.2, corrected §9 self-reference, corrected §3.2 description (was falsely claiming §3.2–§3.9), updated file count 12→15.
- §9 consistency audit: Launch Angle and Spin Vector section headers referenced §3.7 (Weak Foot) — corrected to §3.1 (master table).
- Launch Angle check row: "ANGLE_MIN, ANGLE_MAX from §3.7" corrected to "from §3.1 master table."
- Heading Mechanics #9→#10: 3 instances fixed across Outline, §1 (2 files affected).
- Goalkeeper Mechanics #10→#11: 3 instances fixed across Outline, §1 (2 files affected).
- Fixed64 Math Library #8→#9: 4 instances fixed across §7, §9 (2 files affected).
- §7 dependency table: stale "Spec #8" in notes column corrected to "Spec #9."

---

## APPROVAL STATUS

Pass Mechanics #5 approval is **✅ APPROVED** as of **May 6, 2026**.

Resolution path completed:
1. ✅ All 19 audit findings (March 6, 2026) fixed per the FIX SUMMARY table above (March 25, 2026).
2. ✅ §3.3–§3.9 follow-up audit findings F-A01 and F-A02 resolved May 6, 2026 via **option-3 hybrid**:
   - **F-A01:** Added `spinBase` and `spinMax` columns to §3.1.4 Master Physical Profile Table (per-pass-type values reproduced from §3.4.7, which is now demoted to a `[CROSS]` reading aid). §3.1 v1.3.
   - **F-A02:** Localized `WINDUP_FRAMES` and `FOLLOWTHROUGH_FRAMES` in §3.8.10 as state-machine-owned constants (not pass-type physical intrinsics; do not appear in §3.1.4). §3.7–§3.9 v1.1.
3. ✅ Re-review packet (`pass-mechanics/re-review-packet.md`) walked top-to-bottom; all verification commands passed.
4. ✅ Lead developer (Anton) re-sign-off May 6, 2026; §9 v1.4.

---

## MANUAL ACTIONS REQUIRED (Anton) — ALL CLOSED

1. ✅ Project files replaced with corrected versions (March 25, 2026 sweep).
2. ✅ APPROVAL SUSPENDED status reviewed; re-approval path executed.
3. ✅ §3.3–§3.9 draft quality bar verified; 5 follow-up findings classified per `Section_3_3_to_3_9_Audit_Report.md`.
4. ✅ F-A01 and F-A02 resolved via option-3 hybrid (May 6, 2026); see fix locations above.

## BROADER RENUMBERING ISSUE — RESOLVED (April 26, 2026)

Three-pass sweep completed across all spec folders. Final verification grep returns zero remaining body-text stale references for Decision Tree #7→#8, Heading #9→#10, Goalkeeper #10→#11, and Fixed64 #8→#9.

| Wrong # | Correct # | Spec | Shot Mechanics | Agent Movement | Ball Physics | Collision System | First Touch | Pass Mechanics |
|---|---|---|---|---|---|---|---|---|
| Heading #9 | #10 | Heading Mechanics | ✅ Fixed | ✅ Fixed (Apr 26) | n/a | ✅ Fixed (Apr 26) | ✅ Fixed (Apr 26) | n/a |
| Goalkeeper #10 | #11 | Goalkeeper Mechanics | ✅ Fixed | ✅ Fixed (Apr 26) | n/a | ✅ Fixed (Apr 26) | ✅ Fixed (Apr 26) | n/a |
| Fixed64 #8 | #9 | Fixed64 Math Library | ✅ Fixed | ✅ Fixed (Apr 26) | ✅ Fixed (Apr 26) | ✅ Fixed (Apr 26) | n/a | n/a |
| Decision Tree #7 | #8 | Decision Tree | ✅ Fixed (residual L208) | ✅ Fixed (Apr 26) | n/a | ✅ Fixed (Apr 26) | ✅ Fixed | ✅ Fixed (residual §3.1:76) |

**Sweep methodology:**
- Pass 1 (commit `8d7f729`): Automated regex substitutions on patterns including spec name + "Spec" or "(#" — 55 substitutions across 23 files. Audit-report files and version-history rows excluded from match.
- Pass 2 (next commit): Context-dependent fixes for bare "(Spec #N)" references — 49 substitutions across 12 files via per-line exact-string replacement.
- Pass 3 (next commit): Remaining agent-movement context fixes — 9 substitutions; final two stragglers (`agent-movement/section-6-part-1.md:36` and `collision-system/section-7.md:712`) handled directly.

**Files intentionally NOT modified:**
- `docs/specs/*/audit-report.md` — document the historical bugs and their fix instructions; rewriting would erase the historical record.
- `docs/tracking/spec-error-log-err012-addendum.md` — documents ERR-012 closure with original "Spec #7" wording.
- `docs/specs/SPEC_INDEX.md` "FORMER NUMBERING" table — intentional old-to-new mapping.
- Version-history rows of the form `| 1.x | DATE | ... #7→#8 ... |` — historical records.

---

*End of Fix Manifest*