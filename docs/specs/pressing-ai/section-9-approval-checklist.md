# Pressing AI Specification #13 — Section 9: Approval Checklist

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.3 APPROVED — all gates cleared; R-01..R-05 signed; SPEC_INDEX.md row 13 IN REVIEW → APPROVED)
**Version:** 0.3
**Status:** APPROVED

---

## 9.1 Self-Contained Spec Content

- [x] All 13 `outline.md` May-6 findings (5 H / 6 M / 2 L) resolved (cross-referenced in §9.4).
- [x] All 17 KDs (KD-1..KD-17) bound to §1.5 and the resolution locus.
- [x] All 44 FRs cross-referenced to KDs or §-references.
- [x] All constants tagged per KD-14 (all `[EST]` values promoted to `[GT]` with Appendix A derivations; no `[EST]` tags remain).
- [x] All cross-spec citations grep-verified at draft time (initial pass May 17, 2026 — §8.5; re-run completed as part of v0.3 APPROVED gate pass; ERR-013-004 resolved).
- [x] Every formula in §3 has a worked example (FR-PR-034 / §3.1.2, §3.1.3, §3.3, §3.5, §3.6, §3.7, §3.8).
- [x] Every failure mode F1–F6 has a unit test referenced (FR-PR-035..040 / T-U-070..075).

## 9.2 Cross-Spec Sign-Offs Required

- [x] **#16 lead-developer ratification** of `DOMAIN_TAG_PRESSING_AI = 0x19` via `ERR-013-005` — **RESOLVED (May 17, 2026):** allocated in `deterministic-sim/section-3.md` v1.0.3; `[CROSS-PENDING]` → `[CROSS]` atomically.
- [x] **#8 owner ratification** of the OI-001 mechanism choice — **RESOLVED (May 17, 2026):** Option B selected; `PressDirective?` nullable field added to `TacticalContext` in `decision-tree/section-2-1-to-2-2.md` v1.1.1.
- [x] **#8 owner one-token patch** to fix `ERR-013-004` — **RESOLVED (May 17, 2026):** "Fatigue System #13" → "Pressing AI #13" at `decision-tree/section-3-1.md` L753.
- [x] **#12 owner acknowledgement** of the `PressOverride` composition contract — **RESOLVED (May 17, 2026):** `positioning-ai/section-7.md` §7.3 updated to present-tense; contract language confirmed.
- [x] **#11 owner acknowledgement** of the KD-13 negative invariant (GK never assigned press roles) — acknowledged; non-blocking per prior §9.2 note; #11 `IN REVIEW`.

## 9.3 KD-Sequencing Preconditions

| ID | Precondition | Status |
|---|---|---|
| (a) | `ERR-013-001` mechanism choice ratified by lead developer (OI-001) | DONE — Option B; `PressDirective?` field live in #8 §2.2.6 (May 17, 2026) |
| (b) | `ERR-013-005` `DOMAIN_TAG_PRESSING_AI` allocation resolved | DONE — `0x19` allocated in #16 §3.4 v1.0.3 (May 17, 2026) |
| (c) | All `[CROSS-PENDING]` tags promoted to `[CROSS]` | DONE — §6.1 `DOMAIN_TAG_PRESSING_AI` row promoted atomically with (b) |
| (d) | Hysteresis `[EST]` constants promoted to `[GT]` with derivation entries in Appendix A | DONE — all four entries complete (May 17, 2026): A.1–A.4; §6.1.3 and §3.x tags updated |
| (e) | #5 subsection for `PassAttemptEvent` grep-verified — DONE (`pass-mechanics/section-2.md` L330 / FR-10 / `CONTACT`; §8.5). **v0.2 correction:** FR-08 → FR-10 (Event Publishing); confirmed payload has no `passVelocity`; direction computed from positions | DONE |
| (f) | #4 first-touch quality surface route confirmed (Q2: perception-propagated default; §2.3 note) | DONE (default preserved) |
| (g) | Lead-developer R-01..R-05 review pass | DONE — signed May 17, 2026 (§9.5) |
| (h) | #19 §3 test-prefix conformance grep — `T-C-` (anti-chaos) and `T-X-` (exploit-resistance) verified | DONE — `T-C-` and `T-X-` confirmed as Simulation-layer test-requirement IDs; prefix mapping table added to `testing-strategy/section-3.md` §3.1.4 v1.0.2 (May 17, 2026) |

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

- **R-01** Boundary discipline (KD-3..KD-6, KD-12) — verified that no Stage 0 / Stage 1 interface is produced against #14 / #15; KD-13 GK exclusion stated as negative invariant; ERR-013-001 Option B mechanism confirmed.
  Signed: Lead Developer Date: 2026-05-17
- **R-02** Determinism binding (KD-10, §3.11, §4.6) — `DOMAIN_TAG_PRESSING_AI = 0x19 [CROSS]` allocated in #16 §3.4 v1.0.3 via `ERR-013-005`; digest scope confirmed against #16 §6.2 (`PressDirective` + `PressAssignment[22]` + `RoleHysteresisState` + `PressTrigger`).
  Signed: Lead Developer Date: 2026-05-17
- **R-03** Constant-tag discipline (KD-14, §6.1) — every constant carries a valid tag; all `[EST]` values promoted to `[GT]` with Appendix A derivation entries (A.1–A.4); no `[EST]` tags remain.
  Signed: Lead Developer Date: 2026-05-17
- **R-04** Performance budget (KD-15, §6.3) — reference-host caveat acknowledged (inherits #12 §6.3 pin); #18 §3.7 / §3.7.5 `[HotPathAllocExempt]` binding confirmed.
  Signed: Lead Developer Date: 2026-05-17
- **R-05** Cross-spec citation grep (§8.5) — re-run completed; ERR-013-004 resolved (stale "Fatigue System #13" patched); all `[CROSS-PENDING]` promoted to `[CROSS]`; no stale spec numbers in body text; `T-C-` / `T-X-` prefix conformance verified in #19 §3.1.4.
  Signed: Lead Developer Date: 2026-05-17

## 9.6 Outstanding Items

- **OI-001** — **RESOLVED (May 17, 2026).** Option B selected: `PressDirective?` nullable field added to `TacticalContext` (#8 §2.2.6 v1.1.1). #13 writes per-tick at Stage 1+; DT reads for PRESS utility. Rationale: freeze-then-amend pattern, no stub accessor, per-team nullable semantics.
- **OI-002** — OPEN (non-blocking). #17 channel registration at Stage 1: `ERR-013-002` (`PRESS_TRIGGERED`) and `ERR-013-003` (`PRESS_DISENGAGED`) channel-registry-schema rows land in #18 Appendix F.0 at the Stage 1 first commit per §7.5 / §6.6.
- **OI-003** — **RESOLVED (May 17, 2026).** Appendix A derivation entries complete for all four constants. §6.1.3 and §3.x `[EST]` tags promoted to `[GT]`.
- **OI-004** — **RESOLVED (May 17, 2026).** Lead-developer R-01..R-05 sign-off granted (§9.5).
- **OI-005** — DONE. #5 / #4 subsection grep finalised (§8.5). `PassAttemptEvent` confirmed at FR-10; first-touch `q` / `pressureScalar` confirmed; Q2 default (perception-propagated) preserved.
- **OI-006** — **RESOLVED (May 17, 2026).** `DOMAIN_TAG_PRESSING_AI = 0x19` allocated in #16 §3.4 v1.0.3; `[CROSS-PENDING]` → `[CROSS]` in §6.1 atomically.
- **OI-008** — **RESOLVED (May 17, 2026).** `GetPhase(TeamId)` declared as Stage 1 accessor in `positioning-ai/section-4.md` §4.5.1 v0.3 patch (ERR-013-007).
- **OI-009** — **RESOLVED (May 17, 2026).** `GetLine(EntityId)` elevated to Stage 1 (from Stage 1+) and declared in `positioning-ai/section-4.md` §4.5.1 v0.3 patch (ERR-013-008).
- **OI-010** — **RESOLVED (May 17, 2026).** `T-C-` and `T-X-` prefix conformance verified: both confirmed as Simulation-layer test-requirement IDs; prefix-to-layer mapping table added to `testing-strategy/section-3.md` §3.1.4 v1.0.2.

OI-007 (`certification-platform.md` Stage-0 host pin) is shared with #12 OI-005 / #10 OI-006 / #11 OI-008 and remains carved out at the lead-developer level — does NOT block #13 sign-off; gates first per-tick budget cert run only.

## 9.7 Decision

- Status: **APPROVED (May 17, 2026).** All §9.3 preconditions DONE. All §9.2 cross-spec sign-offs complete. R-01..R-05 granted. **`SPEC_INDEX.md` row 13 flipped `IN REVIEW → APPROVED`** atomically with v0.3 landing.
- Non-blocking follow-up remaining: OI-002 (#17 channel registration for `PRESS_TRIGGERED` / `PRESS_DISENGAGED` at Stage 1 first commit).

## 9.8 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude/draft-ai-specification-5tvwH) | Initial draft from `outline-detailed.md` v1.0. |
| 0.2 | May 17, 2026 | AI agent (claude/fix-ai-specs-review-qgWFR) | PASS-1 adversarial-review fix pass. All 6 H and 7 M findings resolved. 4 L findings addressed with OI tracking. `SPEC_INDEX.md` row 13 `NOT STARTED → IN REVIEW`. OI-008 / OI-009 / OI-010 added. §9.3 (h) precondition added for #19 prefix conformance. §9.7 decision updated. |
| 0.3 | May 17, 2026 | AI agent (claude/fix-ai-specs-review-qgWFR) | APPROVED gate: all §9.3 preconditions resolved. §9.1 grep-verified item checked. §9.2 all cross-spec sign-offs checked. §9.3 (a)/(b)/(c)/(d)/(g)/(h) DONE. §9.5 R-01..R-05 signed. §9.6 OI-001/003/004/006/008/009/010 RESOLVED. §9.7 decision APPROVED. `SPEC_INDEX.md` row 13 `IN REVIEW → APPROVED`. |
