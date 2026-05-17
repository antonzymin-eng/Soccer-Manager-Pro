# Pressing AI Specification #13 — Section 9: Approval Checklist

**Created:** May 17, 2026
**Last Updated:** May 17, 2026
**Version:** 0.1
**Status:** DRAFT — section files v0.1 authored from `outline-detailed.md` v1.0. PASS-1 adversarial review and lead-developer sign-off pending. `SPEC_INDEX.md` row 13 remains `NOT STARTED` per outline NEXT STEPS step 14 (status flip is gated on PASS-1 review, not on section-file landing).

---

## 9.1 Self-Contained Spec Content

- [x] All 13 `outline.md` May-6 findings (5 H / 6 M / 2 L) resolved (cross-referenced in §9.4).
- [x] All 17 KDs (KD-1..KD-17) bound to §1.5 and the resolution locus.
- [x] All 44 FRs cross-referenced to KDs or §-references.
- [x] All constants tagged per KD-14 (`[EST]` for outline-stage placeholders flagged for §6.1 promotion).
- [ ] All cross-spec citations grep-verified at draft time (initial pass complete May 17, 2026 — §8.5; re-run required before lead-developer sign-off).
- [x] Every formula in §3 has a worked example (FR-PR-034 / §3.1.2, §3.1.3, §3.3, §3.5, §3.6, §3.7, §3.8).
- [x] Every failure mode F1–F6 has a unit test referenced (FR-PR-035..040 / T-U-070..075).

## 9.2 Cross-Spec Sign-Offs Required

- [ ] **#16 lead-developer ratification** of `DOMAIN_TAG_PRESSING_AI = 0x19` via `ERR-013-005` (inherits the ERR-012-001 Phase B/C block proposal — current ordering #10 / #11 / #12 / #13 / #14 / #15). Until ratified, the value is `[CROSS-PENDING]`.
- [ ] **#8 owner ratification** of the OI-001 mechanism choice (KD-3): Option A read-only accessor on `PressingAI` OR Option B `TacticalContext.PressDirective` field extension. Section files preserve both options.
- [ ] **#8 owner one-token patch** to fix `ERR-013-004` (stale "Fatigue System #13" reference at `decision-tree/section-3-1.md` L753).
- [ ] **#12 owner acknowledgement** of the `PressOverride` composition contract per #12 §7.3.
- [ ] **#11 owner acknowledgement** of the KD-13 negative invariant (GK never assigned press roles) — non-blocking; informational once #11 is APPROVED.

## 9.3 KD-Sequencing Preconditions

| ID | Precondition | Status |
|---|---|---|
| (a) | `ERR-013-001` mechanism choice ratified by lead developer (OI-001) | OPEN |
| (b) | `ERR-013-005` `DOMAIN_TAG_PRESSING_AI` allocation resolved | OPEN |
| (c) | All `[CROSS-PENDING]` tags promoted to `[CROSS]` | OPEN — depends on (b) |
| (d) | Hysteresis `[EST]` constants promoted to `[GT]` with derivation entries in Appendix A (`TRIGGER_DWELL_TICKS`, `TRIGGER_RELEASE_TICKS`, `ROLE_DWELL_TICKS`, `INTERCEPT_LOOKAHEAD_TICKS`) | OPEN |
| (e) | #5 subsection for `PassAttemptEvent` grep-verified — DONE (`pass-mechanics/section-2.md` L330 / FR-08 / `CONTACT`; §8.5) | DONE |
| (f) | #4 first-touch quality surface route confirmed (Q2: perception-propagated default; §2.3 note) | DONE (default preserved) |
| (g) | Lead-developer R-01..R-05 review pass | OPEN |

## 9.4 Finding-to-Resolution Map

| Review | Finding | Sev | Resolved by |
|---|---|---|---|
| outline.md May-6 | 1. Missing metadata header | H | `outline-detailed.md` "Metadata Header" + §1 (this section file) |
| outline.md May-6 | 2. Section plan misaligned with CLAUDE.md template | H | §1–§9 mapping (this section-file pack) |
| outline.md May-6 | 3. Trigger-detection upstream sources undeclared | H | KD-7 trigger catalog + §3.1 cited surfaces + §8.1 |
| outline.md May-6 | 4. Pass Mechanics #5 SUSPENDED-status risk | H | Stage-binding §1.8 + #5 re-approved May 6, 2026 + §8.5 grep |
| outline.md May-6 | 5. Cover-shadow / lane denial requires #7 | H | KD-7 `WEAK_RECEIVER` + §3.5 cites #7 + Boundary Matrix |
| outline.md May-6 | 6. Determinism plan absent | M | KD-10 + §3.11 / §4.6 + FR-PR-003..005 |
| outline.md May-6 | 7. Stamina / fatigue convention not pre-committed | M | KD-1 + FR-PR-008 + §3.7 (cite-not-redefine #8 §3.1.8.1) |
| outline.md May-6 | 8. Boundary with #14 / #12 unstated | M | KD-4 / KD-5 + Boundary Matrix §1.6 |
| outline.md May-6 | 9. Tick-rate split unstated | M | KD-2 + §1.7 / §4.1 + FR-PR-001 |
| outline.md May-6 | 10. Anti-chaos guardrails undefined | M | KD-16 (three measurable invariants) + FR-PR-018..021 + §5.6 |
| outline.md May-6 | 11. Exploit-resistance tests undefined | M | KD-17 + §5.6.2 (4-exploit corpus) + Appendix E |
| outline.md May-6 | 12. Constant-tag policy not invoked | L | KD-14 + §6.1 + FR-PR-041 |
| outline.md May-6 | 13. No event production declared | L | KD-11 + §7.5 (channels deferred to Stage 1 via `ERR-013-002` / `ERR-013-003`) |

## 9.5 Lead-Developer Sign-Off Lines (R-01..R-05)

- **R-01** Boundary discipline (KD-3..KD-6, KD-12) — verified that no Stage 0 / Stage 1 interface is produced against #14 / #15; KD-13 GK exclusion stated as negative invariant.
  Signed: ___________________________ Date: ___________
- **R-02** Determinism binding (KD-10, §3.11, §4.6) — `DOMAIN_TAG_PRESSING_AI = 0x19 [CROSS-PENDING]` acknowledged via `ERR-013-005`; digest scope confirmed against #16 §6.2 (`PressDirective` + `PressAssignment[22]` + `RoleHysteresisState` + `PressTrigger`).
  Signed: ___________________________ Date: ___________
- **R-03** Constant-tag discipline (KD-14, §6.1) — every constant carries a valid tag; `[EST]` values have an Appendix A derivation entry pending.
  Signed: ___________________________ Date: ___________
- **R-04** Performance budget (KD-15, §6.3) — reference-host caveat acknowledged (inherits #12 §6.3 pin); #18 §3.7 / §3.7.5 `[HotPathAllocExempt]` binding confirmed.
  Signed: ___________________________ Date: ___________
- **R-05** Cross-spec citation grep (§8.5) — re-run completed; no stale spec numbers in body text; `ERR-013-004` confirmed filed.
  Signed: ___________________________ Date: ___________

## 9.6 Outstanding Items

- **OI-001** — KD-3 mechanism choice for `ERR-013-001`: Option A (read-only accessor) vs Option B (`TacticalContext.PressDirective` field extension via #8 §2.2.6). Section-file draft preserves both options in §4.4.4 with non-binding recommendation toward Option B (cleaner #8 hot path; aligns with #12 freeze-then-amend pattern). Final selection gates lead-developer R-01 sign-off.
- **OI-002** — #17 channel registration at Stage 1: `ERR-013-002` (`PRESS_TRIGGERED`) and `ERR-013-003` (`PRESS_DISENGAGED`) channel-registry-schema rows land in #18 Appendix F.0 at the Stage 1 first commit per §7.5 / §6.6 (mirroring Heading Mechanics #10 / Goalkeeper #11 conventions).
- **OI-003** — `[EST]` → `[GT]` promotions: `TRIGGER_DWELL_TICKS`, `TRIGGER_RELEASE_TICKS`, `ROLE_DWELL_TICKS`, `INTERCEPT_LOOKAHEAD_TICKS` require Appendix A derivation entries before `IN REVIEW → APPROVED` advancement.
- **OI-004** — Lead-developer R-01..R-05 sign-off (§9.5).
- **OI-005** — #5 / #4 subsection grep finalisation (§8.5). Section-file draft greps completed May 17, 2026: `PassAttemptEvent` confirmed at `pass-mechanics/section-2.md` L330 (FR-08); first-touch `pressureScalar` confirmed at `first-touch/section-3-1-to-3-5.md` §3.5. **Q2 caveat**: First Touch publishes `q` per touch event, not per tick; the perception-propagated route in #2.3 assumes #7 §3.10 carries `lastTouch.q` forward — if section-file PASS-1 review finds #7 does NOT carry this field, OI-005 re-opens for a perception-schema patch request.
- **OI-006** — Domain-tag allocation finalisation: `DOMAIN_TAG_PRESSING_AI = 0x19` is the current proposal under ERR-012-001's `0x17…0x1C` shifted block (post-May 16, 2026, after #10 took `0x16`). If another spec in the block reaches `APPROVED` before #13 and disrupts the ordering, `ERR-013-005` is updated with the next-available slot per the first-to-APPROVED precedent.

OI-007 (`certification-platform.md` Stage-0 host pin) is shared with #12 OI-005 / #10 OI-006 / #11 OI-008 and remains carved out at the lead-developer level — does NOT block #13 sign-off; gates first per-tick budget cert run only.

## 9.7 Decision

- Status: section files v0.1 authored from `outline-detailed.md` v1.0; PASS-1 adversarial review pending. **`SPEC_INDEX.md` row 13 remains `NOT STARTED`** — the status flip to `IN REVIEW` is gated on PASS-1 adversarial review per outline NEXT STEPS step 14.
- Next gate: PASS-1 adversarial review on section files → v0.2 fix pass → `SPEC_INDEX.md` row 13 `NOT STARTED → IN REVIEW`.
- Final gate: `APPROVED` after R-01..R-05 sign-off and §9.3 preconditions all DONE (chiefly: `ERR-013-001` mechanism choice, `ERR-013-005` domain-tag ratification, and Appendix A derivation entries for `[EST]` constants).

## 9.8 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude/draft-ai-specification-5tvwH) | Initial draft from `outline-detailed.md` v1.0. |
