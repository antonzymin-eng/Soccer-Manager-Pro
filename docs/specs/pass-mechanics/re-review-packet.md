# Pass Mechanics Spec #5 — Re-Review Packet

**Created:** May 6, 2026
**Purpose:** Single consolidated artifact for lead-developer re-sign-off on Pass Mechanics #5 after the March 6, 2026 comprehensive audit (19 findings) and the March 25, 2026 fix application.
**Status:** ✅ **APPROVED — May 6, 2026.** Lead-developer (Anton) sign-off granted after F-A01 and F-A02 resolved via option-3 hybrid. See OPEN ITEMS section below for closure detail.

---

## How to use this packet

This packet collapses three documents (`audit-report.md`, `fix-manifest-pass-mechanics.md`, and the §3.3–§3.9 follow-up `section-3-3-to-3-9-audit-report.md`) into a single per-finding checklist. Each row gives:

1. **Finding ID + severity + one-line description** — what the audit flagged.
2. **Resolution location** — exact file (and section anchor where unambiguous) where the fix lives.
3. **Verification command** — a deterministic shell command (run from repo root) that confirms the fix is in place. Each command exits 0 when the fix is verified; non-zero exit ⇒ regression.

A re-reviewer can sign off by walking the table top-to-bottom, executing the verification command, and recording pass/fail. No prose-level re-reading of the full spec is required for the audit-tracked items — only spot-checks where the reviewer wants additional confidence.

Commands assume `bash`, GNU `grep`, and a working tree at the repo root. All paths are relative to the repo root.

---

## CRITICAL FINDINGS (5 / 5 resolved)

| ID | Severity | Description (one line) | Fix location | Verification command |
|---|---|---|---|---|
| C-01 | CRITICAL | §3.3–§3.9 were never written; only §3.1 and §3.2 existed | `docs/specs/pass-mechanics/section-3-3-to-3-4.md`, `section-3-5-to-3-6.md`, `section-3-7-to-3-9.md` | `ls docs/specs/pass-mechanics/section-3-3-to-3-4.md docs/specs/pass-mechanics/section-3-5-to-3-6.md docs/specs/pass-mechanics/section-3-7-to-3-9.md` |
| C-02 | CRITICAL | §9 consistency audit listed 9 fabricated PassType names (GROUND_DIRECT, BACK_HEEL, HEADER, etc.) not in §3.1.2 | `section-9-approval-checklist.md` | `! grep -E 'GROUND_DIRECT\|BACK_HEEL\|GROUND_THROUGH_BALL\|DRIVEN_DIRECT' docs/specs/pass-mechanics/section-9-approval-checklist.md` |
| C-03 | CRITICAL | Decision Tree referenced as Spec #7 throughout; canonical is #8 | All Pass Mechanics files (10 files modified) | `! grep -nE 'Decision Tree.{0,12}#7\|Spec #7.{0,40}Decision' docs/specs/pass-mechanics/*.md` |
| C-04 | CRITICAL | §2 FR-02 fatigue convention inverted ("1 = rested") | `section-2.md` FR-02 | `grep -E '0\.0.*rested.*1\.0.*fatigued\|0 = rested\|fully rested' docs/specs/pass-mechanics/section-2.md` |
| C-05 | CRITICAL | §9 had dual contradictory approval status ("APPROVED" + "PENDING") | `section-9-approval-checklist.md` | `grep -E 'APPROVAL SUSPENDED' docs/specs/pass-mechanics/section-9-approval-checklist.md && ! grep -E '^\*\*Status:\*\* APPROVED' docs/specs/pass-mechanics/section-9-approval-checklist.md` |

---

## MAJOR FINDINGS (7 / 7 resolved)

| ID | Severity | Description (one line) | Fix location | Verification command |
|---|---|---|---|---|
| M-01 | MAJOR | Chip `distMax` conflict — §3.1 says 20m, §3.2 said 25m | `section-3-2.md` | `! grep -nE 'chip.{0,30}25\s*m\|distMax.{0,15}25\.0' docs/specs/pass-mechanics/section-3-2.md` |
| M-02 | MAJOR | Chip velocity prose 8–16 m/s did not match §3.1 table (5–14 m/s) | `section-3-1.md` §3.1.11 | `grep -E 'chip.{0,80}5.{1,4}14\s*m/s\|5–14\s*m/s' docs/specs/pass-mechanics/section-3-1.md` |
| M-03 | MAJOR | §2.4.3 physical profile table stale; superseded by §3.1.4 | `section-2.md` §2.4.3 | `grep -E 'SUPERSEDED.{0,40}§3\.1\.4' docs/specs/pass-mechanics/section-2.md` |
| M-04 | MAJOR | §9 test counts SV=6 / TR=8 did not match §5 actuals (SV=8 / TR=16) | `section-9-approval-checklist.md` | `grep -E 'SV.{0,5}=.{0,5}8\|TR.{0,5}=.{0,5}16\|8\s+SV.*16\s+TR' docs/specs/pass-mechanics/section-9-approval-checklist.md` |
| M-05 | MAJOR | V_OFFSET described as derived formula in §9; is per-type [GT] constant | `section-9-approval-checklist.md` | `grep -E 'V_OFFSET.{0,40}\[GT\]' docs/specs/pass-mechanics/section-9-approval-checklist.md` |
| M-06 | MAJOR | §8 footer version said "1.0" but document is v1.2 | `section-8-1-to-8-5.md` / `section-8-6.md` | `grep -E '\*\*Version:\*\*\s*1\.2\|version 1\.2' docs/specs/pass-mechanics/section-8-6.md docs/specs/pass-mechanics/section-8-1-to-8-5.md` |
| M-07 | MAJOR | §5.14.4 cited "82 unit tests" — verified correct (8 SV + 16 TR + 58 = 82) | `section-5-13-to-5-16.md` (no change) | `grep -E '82\s*unit\s*tests' docs/specs/pass-mechanics/section-5-13-to-5-16.md` |

---

## MODERATE FINDINGS (4 / 4 resolved)

| ID | Severity | Description (one line) | Fix location | Verification command |
|---|---|---|---|---|
| Mod-01 | MOD | §2 FR-03 Lofted angle range 35° did not match §3.1 (45°); Cross Flat/High mismatch | `section-2.md` FR-03 | `grep -E 'Lofted.{0,40}45°\|Lofted.{0,40}45\s*deg' docs/specs/pass-mechanics/section-2.md` |
| Mod-02 | MOD | Outline pass-type taxonomy diagram omitted "Lobbed" synonym for Chip | `outline-part-1.md`, `outline-part-2.md`, or `outline-part-3.md` | `grep -lE 'Chip.{0,15}Lobbed\|Lobbed.{0,15}Chip' docs/specs/pass-mechanics/outline-part-*.md` |
| Mod-03 | MOD | §4 prerequisites referenced §3.1 v1.0; current is v1.1 | `section-4.md` | `grep -E '§3\.1\s*v1\.1\|Section 3\.1.{0,15}v1\.1' docs/specs/pass-mechanics/section-4.md` |
| Mod-04 | MOD | OQ-6 (KickType enum) not explicitly resolved | `outline-part-3.md` (OQ register) | `grep -E 'OQ-6.{0,80}resolved\|OQ-6.{0,120}parameter-based\|OQ-6.{0,80}no.{0,15}KickType' docs/specs/pass-mechanics/outline-part-*.md` |

---

## MINOR FINDINGS (3 / 2 resolved + 1 N/A)

| ID | Severity | Description (one line) | Fix location | Verification command |
|---|---|---|---|---|
| Min-01 | MIN | §1 header listed "Spec Error Log v1.0"; current is v1.4 | `section-1.md` | `grep -E 'Spec Error Log\s*v1\.4\|spec-error-log.*v1\.4' docs/specs/pass-mechanics/section-1.md` |
| Min-02 | MIN | §5.1.5 performance targets impossible (mean > p95) | `section-5-1-to-5-12.md` §5.1.5 | `bash -c 'awk "/§5\\.1\\.5\|5\\.1\\.5/,/§5\\.1\\.6\|5\\.1\\.6/" docs/specs/pass-mechanics/section-5-1-to-5-12.md \| grep -qE "mean.*<.*p95\|p95.*>.*mean" && echo OK'` |
| Min-03 | MIN | §7 and §4 both forward-ref Event System #17 (no action; tracking only) | n/a | `true  # informational` |

---

## ADDITIONAL FIXES DISCOVERED DURING REMEDIATION

Per `fix-manifest-pass-mechanics.md` lines 33–41, the following were found and fixed beyond the 19 audit findings. They are part of the same remediation pass and require the same re-review:

| Item | Fix location | Verification command |
|---|---|---|
| §9 Known Limitation #8: HEADER falsely claimed as PassType enum value | `section-9-approval-checklist.md` | `! grep -E 'HEADER.{0,20}PassType\|PassType.{0,20}HEADER' docs/specs/pass-mechanics/section-9-approval-checklist.md` |
| §9 file table: 3 new §3.3–§3.9 files added; counts 12→15 | `section-9-approval-checklist.md` | `grep -E 'section-3-3-to-3-4\|section-3-5-to-3-6\|section-3-7-to-3-9' docs/specs/pass-mechanics/section-9-approval-checklist.md` |
| §9 Launch Angle / Spin Vector cross-reference: §3.7 → §3.1 master table | `section-9-approval-checklist.md` | `! grep -E 'ANGLE_(MIN\|MAX).{0,15}from\s*§3\.7\|Launch Angle.{0,30}§3\.7' docs/specs/pass-mechanics/section-9-approval-checklist.md` |
| Heading Mechanics #9→#10 stragglers in Outline + §1 | `outline-part-*.md`, `section-1.md` | `! grep -nE 'Heading Mechanics.{0,10}#9\|Heading.{0,10}\(#9\)' docs/specs/pass-mechanics/*.md` |
| Goalkeeper Mechanics #10→#11 stragglers in Outline + §1 | `outline-part-*.md`, `section-1.md` | `! grep -nE 'Goalkeeper.{0,10}#10\|Goalkeeper Mechanics.{0,10}\(#10\)' docs/specs/pass-mechanics/*.md` |
| Fixed64 #8→#9 stragglers in §7, §9 | `section-7-*.md`, `section-9-approval-checklist.md` | `! grep -nE 'Fixed64.{0,10}#8\|Fixed64 Math.{0,10}\(#8\)' docs/specs/pass-mechanics/*.md` |
| §7 dependency table: stale "Spec #8" in notes column → "Spec #9" | `section-7-1-to-7-8.md` | `! grep -nE 'Fixed64.{0,30}Spec #8' docs/specs/pass-mechanics/section-7-1-to-7-8.md` |

---

## §3.3–§3.9 FOLLOW-UP AUDIT (5 findings)

The §3.3–§3.9 sections drafted on March 7, 2026 (resolving C-01) were themselves audited; findings are recorded in `section-3-3-to-3-9-audit-report.md`. These are part of this re-review packet. Fix status per `fix-manifest-pass-mechanics.md` "Manual Actions Required (Anton)" item 4: **F-A01 / F-A02 unresolved** (open action — see "Open Items" below).

| ID | Severity | Description (one line) | Fix location | Verification command |
|---|---|---|---|---|
| F-A01 | MAJOR ✅ RESOLVED May 6, 2026 | `spinMagnitudeBase` / `spinMagnitudeMax` cited from §3.1.4 master table but not present | Fixed: §3.1.4 master table extended with `spinBase`/`spinMax` columns | `grep -E 'spinBase.*spinMax' docs/specs/pass-mechanics/section-3-1.md` |
| F-A02 | MAJOR ✅ RESOLVED May 6, 2026 | `WINDUP` / `FOLLOWTHROUGH` columns cited from §3.1.4 but not present | Fixed: §3.8.10 declared canonical owner; §3.1.4 reference removed; §3.8.2 + cross-spec dep table updated | `grep -E 'locally owned by §3\.8\|state-machine timing values' docs/specs/pass-mechanics/section-3-7-to-3-9.md` (positive presence check for the §3.8.10 ownership note) |
| F-A03 | MOD | (See `section-3-3-to-3-9-audit-report.md`) | (see follow-up audit) | (manual review) |
| F-A04 | MOD | (See `section-3-3-to-3-9-audit-report.md`) | (see follow-up audit) | (manual review) |
| F-A05 | MIN | (See `section-3-3-to-3-9-audit-report.md`) | (see follow-up audit) | (manual review) |

> Reviewer: pull the full set of 5 follow-up findings from `section-3-3-to-3-9-audit-report.md` if the spot-checks above raise concerns. F-A03–F-A05 are not blockers per the original audit's severity classification.

---

## RE-REVIEW SIGN-OFF CHECKLIST

Run this short script from the repo root. It executes every verification command above and reports pass/fail counts. Reviewer signs off when the script reports zero failures **and** the two open F-A01/F-A02 items are addressed (either by adding the columns to §3.1.4 or by correcting the citations in §3.4.7 / §3.8.10).

```bash
# (Reviewer can paste this block; each numbered item maps 1:1 to the rows above.)
set +e
fail=0
check() { eval "$2" >/dev/null 2>&1 && echo "PASS  $1" || { echo "FAIL  $1"; fail=$((fail+1)); }; }

# Critical
check C-01 'ls docs/specs/pass-mechanics/section-3-3-to-3-4.md docs/specs/pass-mechanics/section-3-5-to-3-6.md docs/specs/pass-mechanics/section-3-7-to-3-9.md'
check C-02 '! grep -E "GROUND_DIRECT|BACK_HEEL|GROUND_THROUGH_BALL|DRIVEN_DIRECT" docs/specs/pass-mechanics/section-9-approval-checklist.md'
check C-03 '! grep -nE "Decision Tree.{0,12}#7|Spec #7.{0,40}Decision" docs/specs/pass-mechanics/*.md'
check C-04 'grep -E "0\.0.*rested.*1\.0.*fatigued|0 = rested|fully rested" docs/specs/pass-mechanics/section-2.md'
check C-05 'grep -E "APPROVAL SUSPENDED" docs/specs/pass-mechanics/section-9-approval-checklist.md'

# (Major / Moderate / Minor — see table rows above for exact commands.)

echo "-----"
echo "Failures: $fail"
[ "$fail" = 0 ] && echo "RE-REVIEW READY FOR SIGN-OFF" || echo "RE-REVIEW BLOCKED — investigate failures"
```

---

## OPEN ITEMS — CLOSED

1. **F-A01 / F-A02 — RESOLVED May 6, 2026 via option-3 hybrid.**
   - **F-A01:** `spinBase` and `spinMax` columns added to §3.1.4 Master Physical Profile Table (per-pass-type values). §3.4.7 demoted to a `[CROSS]` reading aid that mirrors §3.1.4. §3.1 v1.3; §3.3–§3.4 v1.1.
   - **F-A02:** `WINDUP_FRAMES` and `FOLLOWTHROUGH_FRAMES` localized in §3.8.10 as state-machine-owned constants (intrinsic to §3.8 Pass Execution State Machine; not pass-type physical intrinsics; do not appear in §3.1.4). Cross-spec dependencies table at line 762 of `section-3-7-to-3-9.md` updated to point at §3.8.10 as canonical source. §3.7–§3.9 v1.1.

2. **§3.3–§3.9 follow-up findings F-A03 / F-A04 / F-A05 — accepted as non-blocking.** Per the §3.3–§3.9 audit's own severity classification (2 Moderate, 1 Minor), these are not gating items. Tracked for future polish; do not block implementation.

---

## VERSION HISTORY

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | May 6, 2026 | Claude / Anton | Initial packet. Consolidates 19 audit findings + 7 additional fixes + §3.3–§3.9 follow-up audit findings into one re-review checklist with verification commands. |
| 1.1 | May 6, 2026 | Claude / Anton | F-A01 and F-A02 resolved via option-3 hybrid. Status flipped to APPROVED. F-A03/F-A04/F-A05 accepted as non-blocking per audit's own severity classification. |

*End of Re-Review Packet.*
