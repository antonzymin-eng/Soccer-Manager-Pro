# Stage 0 Specification Progress Tracker

**Created:** February 3, 2026, 10:35 PM PST  
**Last Updated:** May 13, 2026  
**Purpose:** Track specification writing progress for Stage 0 (Physics Foundation)  
**Started:** February 2, 2026  
**Note:** Scheduling milestones removed — passion project, no deadline.

---

## PROGRESS SUMMARY

**Total Specifications:** 20  
**Approved:** 10  
**Suspended:** 0  
**In Review:** 2  
**In Progress:** 1  
**Not Started:** 7

**Specifications approved:**
- Ball Physics (#1, Feb 8, 2026)
- Agent Movement (#2, Apr 27, 2026; §2.5 patch May 6, 2026 — `XC-002-001` EntityId no-reuse)
- Collision System (#3, Feb 19, 2026 — §9 back-filled Apr 26, 2026)
- First Touch (#4, Feb 22, 2026)
- Pass Mechanics (#5, re-approved May 6, 2026 — original Feb 22 → suspended Mar 25 → re-approved after F-A01/F-A02 resolution)
- Shot Mechanics (#6, Apr 27, 2026)
- Perception System (#7, Apr 22, 2026)
- Decision Tree (#8, Apr 27, 2026 — draft-level quality gate; comprehensive audit candidate before implementation; §1.7.3 patch May 6, 2026 — `XC-008-001` EntityId no-reuse)
- Code Standards & Style Guide (#20, May 11, 2026 — adversarial review pass-1 applied; lead-developer R-01..R-05 sign-off complete)
- Event System (#17, May 13, 2026 — section-files PASS 1 + PASS 2 adversarial review applied; lead-developer sign-off complete; all 10 section files + appendices at v0.3)

**Specifications suspended:** None. Stage 0 Priority 1–2 spec set is complete.

---

## Priority 1 — Physics Foundation (Weeks 1-4)

| # | Specification | Pages | Status | Started | Completed | Notes |
|---|---------------|-------|--------|---------|-----------|-------|
| 1 | Ball Physics | ~50 | ✅ APPROVED | Feb 2 | Feb 8 | All sections written, reviewed, checklist signed off; §3.1 updated to v2.5 (ApplyKick, Option B possession) |
| 2 | Agent Movement | ~130 | ✅ APPROVED | Feb 9 | Apr 27, 2026 | All sections + appendices complete; §9 v2.1 lead-developer signed off Apr 27, 2026; §3.5 v1.4 (ERR-007); §8 v1.4 |
| 3 | Collision System | ~50 | ✅ APPROVED | Feb 16 | Feb 19 | All sections + appendices v1.1; approval checklist signed off |
| 4 | First Touch Mechanics | ~70 | ✅ APPROVED | Feb 19 | Feb 22 | All sections + appendices complete; ERR-001, ERR-004 resolved; §8 updated to v1.1 |
| 5 | Pass Mechanics | ~65 | ✅ APPROVED | Feb 19 | May 6, 2026 | All sections + appendices complete; ERR-006/007/008 resolved; 19 audit findings (Mar 6) and follow-up F-A01/F-A02 (May 6) all resolved per `re-review-packet.md`. §9 v1.4 lead-developer signed off May 6, 2026. |

**Note:** Specs were renumbered in execution vs. original plan. First Touch completed as #4 (originally planned as #11), Pass Mechanics as #5, due to dependency priority. Collision System's original #3 slot retained.

**Priority 1 Progress:** 100% (5 of 5 approved)

---

## Priority 2 — Core Gameplay (Weeks 5-8)

| # | Specification | Pages | Status | Started | Completed | Notes |
|---|---------------|-------|--------|---------|-----------|-------|
| 6 | Shot Mechanics | ~70 | ✅ APPROVED | Feb 23 | Apr 27, 2026 | All 9 sections + appendices complete; §9 v1.4 lead-developer signed off Apr 27, 2026 (pre-migration void-file blockers reclassified as moot under folder layout). 104 tests (6.9× min). ShotType enum eliminated; parameter-based physics approach. |
| 7 | Perception System | ~80 | ✅ APPROVED | Feb 24 | Apr 22, 2026 | All 8 sections + 3 appendices complete; §8 v1.2 DOI verified; §5 v1.4 (95 tests, 1.8× min). Section 9 Approval Checklist v1.7 — ✅ APPROVED April 22, 2026. |
| 8 | Decision Tree | ~55+ | ✅ APPROVED | Feb 27 | Apr 27, 2026 | Sections 1–9 and appendices drafted; §9 v1.2 lead-developer signed off Apr 27, 2026 at draft-level quality gate. Comprehensive audit candidate before implementation. |
| 9 | Fixed64 Math Library | 25-30 | 🔍 IN REVIEW | Apr 28, 2026 | — | All sections (1–9 + appendices) drafted at v0.2–v0.3; Pass 2 adversarial critique fixes landed; awaiting lead developer sign-off per `fixed64-math/section-9-approval-checklist.md` (May 6, 2026 reclassification) |

**Priority 2 Progress:** 100% drafted (3 approved, 1 in review)

---

## Priority 3 — Advanced Physics (Weeks 9-12)

| # | Specification | Pages | Status | Started | Completed | Notes |
|---|---------------|-------|--------|---------|-----------|-------|
| 10 | Heading Mechanics | 15-18 | ⏳ NOT STARTED | — | — | Week 9 target (delayed; blocked on Priority 2 sign-offs) |
| 11 | Goalkeeper Mechanics | 20-25 | ⏳ NOT STARTED | — | — | Week 10 target |
| 12 | Positioning AI | 18-22 | ⏳ NOT STARTED | — | — | Week 12 target |

**Priority 3 Progress:** 0% (0 of 3 started)

---

## Priority 4 — Tactical AI (Weeks 13-16)

| # | Specification | Pages | Status | Started | Completed | Notes |
|---|---------------|-------|--------|---------|-----------|-------|
| 13 | Pressing AI | 18-22 | ⏳ NOT STARTED | — | — | Week 13 target |
| 14 | Defensive AI | 18-22 | ⏳ NOT STARTED | — | — | Week 14 target |
| 15 | Attacking AI | 18-22 | ⏳ NOT STARTED | — | — | Week 15 target |
| 16 | Deterministic Simulation | 20-25 | 📝 IN PROGRESS | May 2, 2026 | — | All 9 sections + appendices drafted; adversarial review + two fix passes complete (v0.7, v0.8); awaiting implementation-readiness gate §9.5 before IN REVIEW |

**Priority 4 Progress:** 25% (1 of 4 in progress)

---

## Priority 5 — Systems Architecture (Weeks 17-20)

| # | Specification | Pages | Status | Started | Completed | Notes |
|---|---------------|-------|--------|---------|-----------|-------|
| 17 | Event System | ~35 | ✅ APPROVED | May 13, 2026 | May 13, 2026 | All 10 section files + appendices authored May 13, 2026 from `outline-detailed.md` v1.1; section-files PASS 1 (20 findings) and PASS 2 (2 H / 6 M / 7 L findings) adversarial reviews both resolved same day; all section files at v0.3; lead-developer sign-off May 13, 2026. ERR-017-001 (`DOMAIN_TAG_EVENT_LEDGER`) open pending #16 approval. |
| 18 | Performance Optimization Strategy | 18-22 | ⏳ NOT STARTED | — | — | Week 18 target |
| 19 | Testing Strategy & Framework | 20-25 | 🔍 IN REVIEW | May 12, 2026 | — | Initial section-file draft authored May 12, 2026 from `outline-detailed.md` v1.1; v0.2 self-critique sweep applied (3 H / 6 M / 8 L findings, all resolved). Per KD-2 sequencing, advancement to `APPROVED` is gated on (a) #16 reaching Tier 2 APPROVED, (b) #18 outline-level draft, and (c) all `TBD-NORMATIVE` tags resolved. |
| 20 | Code Standards & Style Guide | 15-20 | ✅ APPROVED | May 7, 2026 | May 11, 2026 | All section files + appendices drafted from `outline-detailed.md` v1.3 (May 7–8); adversarial review pass-1 applied May 11; lead-developer R-01..R-05 sign-off complete May 11, 2026. §9 v1.1 APPROVED. |

**Priority 5 Progress:** 50% (2 of 4 approved, 1 in review)

---

## STATUS LEGEND

| Icon | Status | Description |
|------|--------|-------------|
| ⏳ | NOT STARTED | Specification not yet begun |
| 📝 | IN PROGRESS | Actively writing specification |
| 🔍 | IN REVIEW | All sections complete, awaiting approval |
| ⏸ | SUSPENDED | Was approved or in review; audit findings require re-review before sign-off |
| ✅ | APPROVED | Approved by lead developer, ready for implementation |
| 🔒 | LOCKED | Implementation has begun, spec is frozen |

---

## WEEKLY UPDATE LOG

### Week 1 (Feb 2-8, 2026)

**Completed:**
- Ball Physics Specification (#1) — all 8 sections + Appendices A-C written
- Section 3.1 iterated through v2.4; REV-001 rolling friction discrepancy resolved
- Formal Approval Checklist completed (14/14 required items PASS)
- Ball Physics Spec #1 formally approved by lead developer (Feb 8)

**Pending:**
- Commit approved files to repo; git tag: `spec-ball-physics-v1.0-approved`

---

### Week 2 (Feb 9-15, 2026)

**Completed:**
- Agent Movement Specification (#2) — all 14 files written (~760KB, ~130 pages)
- Sections 3.1–3.7, Section 4, 5, 6, 7, Appendices A-D, Section 9 Approval Checklist
- Multiple AI critique cycles; all CRITICAL/MAJOR issues resolved
- Section 9 Approval Checklist drafted — 32-item consistency audit

**In Review:**
- Agent Movement Spec #2 awaiting lead developer sign-off

---

### Week 3 (Feb 16-22, 2026)

**Completed:**
- Collision System Specification (#3) — all sections, appendices, approval checklist written and approved
- First Touch Mechanics Specification (#4) — all sections, appendices, approval checklist signed off; ERR-001, ERR-004 resolved
- Pass Mechanics Specification (#5) — all sections, appendices, approval checklist signed off; ERR-006, ERR-007, ERR-008 resolved; V_OFFSET correction applied
- Cross-specification error audit — spec-error-log.md; all critical errors resolved
- Ball Physics §3.1 updated to v2.5 (ApplyKick, Option B possession)
- Ball Physics §8 updated to v1.3 (cross-reference corrections); later updated to v1.4
- Agent Movement §3.5 updated to v1.3 (KickPower, WeakFootRating, Crossing added)

**In Review:**
- Agent Movement Spec #2 still awaiting lead developer sign-off

**Pending:**
- Commit all approved specs to repo; git tags for Collision System, First Touch, Pass Mechanics
- Remove superseded file versions (see file-manifest.md)

---

### Week 4 (Feb 23-26, 2026)

**Completed:**
- Shot Mechanics Specification (#6) — all 9 sections + appendices written
  - Outline v1.2: ShotType enum eliminated; parameter-based physics approach locked
  - Section 1 v1.1: Purpose, scope, 7 KDs, dependency flags
  - Section 2 v1.0: FR-01–FR-11; 13-step pipeline; data structures; 10 failure modes
  - Section 3.1–3.3 v1.1: Validation, velocity model, launch angle
  - Section 3.4–3.10 v1.2: Spin, placement, error model, body mechanics, weak foot, state machine, events
  - Section 4 v1.3: Architecture; 28 files; GoalGeometryProvider and IShotVelocityCalculator seams (Amendment 1)
  - Section 5 v1.3: 104 tests — 86 unit + 12 integration + 6 validation = 6.9× minimum; zero blocked
  - Section 6 v1.0: O(1) per evaluation; p95 < 0.050ms; Fixed64 migration notes
  - Section 7 v1.0: Future extensions; Stage 1+ deferrals
  - Section 8 v1.2: References; all 10 DOIs independently verified; WYSCOUT-VELOCITY removed
  - Appendices v1.1: Derivations, verification tables, sensitivity analysis
  - Section 9 Approval Checklist v1.2: 8/8 quality checks PASS; pending lead developer sign-off and void file removal
  - DOI cross-corrections issued to Pass Mechanics §8 v1.1 and Ball Physics §8 v1.4
- Perception System Specification (#7) — all 8 sections + 3 appendices written
  - Outline v1.1: all 5 open questions resolved; 7 KDs locked; interface boundary to Decision Tree defined
  - Section 1 v1.1: Purpose, scope, 7 KDs, hard/soft dependencies
  - Section 2 v1.1: FR-01–FR-13; 8-step pipeline; PerceptionSnapshot struct; 10 failure modes
  - Section 3 v1.2: FoV model, shadow cone occlusion, recognition latency, blind-side awareness, shoulder check, pressure scalar, snapshot assembly, forced refresh
  - Section 4 v1.1: Architecture; PerceptionSystem.cs; output contract to Decision Tree (#8); forced refresh events
  - Section 5 v1.3: 95 tests — 73 unit + 15 integration + 3 balance + 4 performance = 1.8× minimum (unit only); all deterministic
  - Section 6 v1.1: ~40,000 equiv-ops per heartbeat; <20% of 2ms budget estimated; 10Hz cadence analysis
  - Section 7 v1.1: Future extensions; per-entity fidelity variation (Stage 1); positional uncertainty deferred
  - Section 8 v1.2: References; all DOIs verified; ~62% gameplay-tuned constants documented
  - Appendix A v1.1: Formula derivations (FoV, occlusion, L_rec, blind-side)
  - Appendix B v1.1: Numerical verification tables
  - Appendix C v1.1: Sensitivity analysis
  - Section 9 Approval Checklist: v1.7 — ✅ APPROVED April 22, 2026. Lead developer signed off.

**In Review:**
- Agent Movement Spec #2 — still awaiting lead developer sign-off
- Shot Mechanics Spec #6 — awaiting lead developer sign-off (Section 9 checklist v1.2 complete)
- Perception System Spec #7 — ✅ APPROVED April 22, 2026

**Pending:**
- Resolve Perception System Section 9 Approval Checklist blockers (4 critical items) — required before sign-off
- Lead developer sign-off on Agent Movement, Shot Mechanics, Perception System
- Begin Decision Tree Specification #8 outline
- Remove superseded file versions (see file-manifest.md pending removal section)

---

### Weeks 5–8 (Feb 27 – Mar 26, 2026)

**Completed:**
- Decision Tree Specification (#8) — Sections 1–9 and appendices drafted (~55+ pages)
- CLAUDE.md created (March 26) — authoritative AI behavioral rules, conventions, and cross-reference hazards documented
- SPEC_INDEX.md created (March 26) — canonical spec registry established as single source of truth
- Pass Mechanics (#5) comprehensive audit completed (March 25): 19 findings (5 critical), all fixes applied
- spec-error-log.md updated through ERR-012+; cross-spec error tracking maintained

**Status Changes:**
- Pass Mechanics (#5): APPROVED → **SUSPENDED** (March 25, 2026) — audit findings require lead developer re-review
- Decision Tree (#8): NOT STARTED → **IN REVIEW** — all sections and appendices drafted

**Still Pending (carried forward):**
- Perception System Spec #7 — ✅ APPROVED April 22, 2026; Section 9 Approval Checklist v1.7 signed off by lead developer
- Lead developer sign-off: Agent Movement (#2), Shot Mechanics (#6), Perception System (#7), Decision Tree (#8)
- Pass Mechanics re-sign-off after audit fix verification
- Commit all approved specs and git tags

---

### Weeks 9–12 (Mar 27 – Apr 21, 2026)

**Status:**
- No new specifications started. Priority 2 sign-offs remain pending.
- Decision Tree (#8), Agent Movement (#2), Shot Mechanics (#6), Perception System (#7) all in review awaiting lead developer action.
- Fixed64 Math Library (#9) and Priority 3 specs blocked on Priority 2 completion.

**Still Pending:**
- Perception System Section 9 Approval Checklist blockers resolved (critical items); sign-off by lead developer
- All Priority 2 sign-offs
- Pass Mechanics re-sign-off
- Begin Fixed64 Math Library (#9) once Priority 2 is resolved

---

### Weeks 13–14 (Apr 22 – May 4, 2026)

**Completed:**
- Perception System (#7) lead-developer sign-off (Apr 22) — §9 v1.7 APPROVED.
- Lead-developer sign-off pass (Apr 27): Agent Movement (#2), Shot Mechanics (#6), Decision Tree (#8) all APPROVED. Decision Tree approved at draft-level quality gate; comprehensive audit a candidate before implementation.
- Renumber sweep complete (Apr 26): ~115 body-text substitutions across three commits; zero remaining stale body-text references after verification grep.
- Fixed64 stage-scope decision recorded (Apr 26): migration deferred to Stage 5+; Stage 0 uses `float` + state snapshots for single-machine determinism. CLAUDE.md and `master-development-plan.md` updated to match.
- Collision System (#3) §9 v2.1 back-fill (Apr 26) reconciled status disagreement with trackers.
- Annotated approval tags created locally for the seven approved specs; remote push blocked by branch-protection 403 — pending privileged credentials after merge to main.
- Deterministic Simulation (#16) drafted ahead of formal status update; status reclassified `NOT STARTED` → `IN PROGRESS` on May 2.
- Deterministic Simulation (#16): adversarial review + v0.7 and v0.8 fix passes (May 2). v0.8 resolves 21 issues from third-pass critique (critical KDF ambiguity, mislabeled constants, AI_NoOp underdocumentation, plus 18 medium/low). ERR-016-001 mitigated (phantom interface risk in §4.2 reframed as non-normative).
- Deterministic Simulation (#16): third-pass adversarial critique resolved at v0.9 (May 3). ERR-016-002 filed (EntityId no-reuse cross-spec back-propagation pending against approved specs #2 and #8).
- Deterministic Simulation (#16): fourth- and fifth-pass critique passes; Pass 6 resolves Pass 4/5 findings; §7/§8/appendices follow-up audit (5 fixes).

**Status:**
- Pass Mechanics (#5) remains the only outstanding Priority 1–2 spec (re-sign-off pending after Mar 25 audit fixes).
- Fixed64 Math Library (#9) drafting commenced; all sections v0.2–v0.3 present.

**Still Pending:**
- Pass Mechanics (#5) re-sign-off.
- ERR-016-002: reciprocal `XC-002-NNN` and `XC-008-NNN` references in Agent Movement (#2) and Decision Tree (#8).
- Push the seven approved-spec tags after merge to main.

---

### Week 15 (May 5 – May 6, 2026)

**Completed:**
- Fixed64 Math Library (#9) Pass 2 adversarial critique landed; all 20 validated findings resolved.
- Outline-level adversarial reviews of specs #11–#20 and Heading Mechanics (#10) completed.
- Fixed64 Math Library (#9) reclassified `NOT STARTED` → `IN REVIEW` (May 6).
- **ERR-016-002 resolved at spec-text level (May 6):** `XC-002-001` added to Agent Movement #2 §2.5 (v1.1.1 patch); `XC-008-001` added to Decision Tree #8 §1.7.3 (v1.1.1 patch); Det Sim #16 §3.2.5 prose updated to point at the back-propagated references.
- **Deterministic Simulation (#16) two-tier approval model introduced (§9 v1.1, May 6):** Tier 1 Conditional Approval covers self-contained spec content; Tier 2 Final Approval gated on #9/#17/#18/#19. `TBD-NORMATIVE` tag introduced for §8.3.1 placeholder rows.
- **Two golden-vector files authored (May 6):** `hkdf-sha256-kat.md` (RFC 5869 A.1–A.3) and `siphash-2-4-kat.md` (Aumasson & Bernstein 2012 Appendix A 64-vector table) committed under `docs/specs/deterministic-sim/golden-vectors/`. `serialize-canonical-corpus.md` deferred to Det Sim Tier 2.
- **Pass Mechanics (#5) re-approved (May 6):** F-A01 / F-A02 resolved via option-3 hybrid (`spinBase`/`spinMax` columns added to §3.1.4; `WINDUP_FRAMES`/`FOLLOWTHROUGH_FRAMES` localized in §3.8.10). Re-review packet (`pass-mechanics/re-review-packet.md`) walks all 19 audit findings + 7 additional fixes + 5 follow-up findings with verification commands. §9 v1.4 lead-developer signed off May 6, 2026. Status flipped SUSPENDED → APPROVED; Stage 0 Priority 1–2 spec set complete.

**Status:**
- **8 APPROVED, 0 SUSPENDED, 1 IN REVIEW, 1 IN PROGRESS, 10 NOT STARTED.**

---

### Week 16 (May 7 – May 12, 2026)

**Completed:**
- **Code Standards & Style Guide (#20) APPROVED (May 11, 2026):** Section files + appendices drafted May 7–8 from `outline-detailed.md` v1.3; adversarial review pass-1 applied May 11; lead-developer R-01..R-05 sign-off completed same day. §9 v1.1 APPROVED. SPEC_INDEX.md row 20 updated.
- **Testing Strategy & Framework (#19) IN REVIEW (May 12, 2026):** Initial section-file draft authored May 12 from `outline-detailed.md` v1.1. v0.2 self-critique sweep applied same day; 3 H / 6 M / 8 L findings resolved. §16 section-number citations corrected (§7→§5, §1.3.1→§1.1.1, §5→§3.2.4.1; deleted §8 "trace channels"). KD-2 sequencing: advancement to `APPROVED` gated on (a) #16 Tier 2 APPROVED, (b) #18 outline-level draft, (c) all `TBD-NORMATIVE` tags resolved.

**Status:**
- **9 APPROVED, 0 SUSPENDED, 2 IN REVIEW, 1 IN PROGRESS, 8 NOT STARTED.**

**Still Pending:**
- Fixed64 Math Library (#9) lead-developer sign-off (IN REVIEW since May 6).
- Testing Strategy (#19) approval — blocked by KD-2 preconditions (#16 Tier 2 APPROVED, #18 outline, all `TBD-NORMATIVE` tags resolved).
- Deterministic Simulation (#16) Tier 2 Final Approval.
- Spec #18 (Performance Optimization Strategy) outline-level draft — required to unblock #19 §9.3.6 precondition.

**Still Pending:**
- Fixed64 Math Library (#9) lead-developer sign-off.
- Deterministic Simulation (#16) Tier 2 Final Approval — gated on #9 / #17 / #18 / #19 reaching `IN REVIEW`. #17 is now APPROVED (beyond IN REVIEW); #9, #18, #19 remain outstanding.
- `serialize-canonical-corpus.md` (Det Sim Tier 2 golden-vector file).
- 7 approval tags created locally remain unpushed (HTTP 403 branch-protection); push after merge to main with squash-merge precondition check per CLAUDE.md OPEN ISSUES.
- Stage 0 host platform pin in `tracking/certification-platform.md` (placeholder; required before first Det Sim §5.5 certification run).

---

### Week 17 (May 13, 2026)

**Completed:**
- **Event System (#17) APPROVED (May 13, 2026):** Section files (section-1 through section-9-approval-checklist + appendices) authored May 13, 2026 from `outline-detailed.md` v1.1. Section-files PASS 1 adversarial critique (20 findings: 3 H / 5 M / 12 L) resolved in v0.2. Section-files PASS 2 adversarial critique (2 H / 6 M / 7 L findings) resolved in v0.3 — H-2-1 (TickHeartbeatEvent producer phase reverted to Snapshot), H-2-2 (ERR_EVT_TIER_MISMATCH removed from runtime namespace; 0x1702 slot recovered), M-2-1/M-2-2 (§2.3 prose + routing table), M-2-3 (FR-EVT-052 struct-ref carve-out), M-2-4 (IBootSubscriberRegistration phantom removed), M-2-5 (FR-EVT-046a/046b ordering), M-2-6 (PASS 3 label). All section files at v0.3. Lead-developer sign-off granted May 13, 2026. ERR-017-001 (DOMAIN_TAG_EVENT_LEDGER back-prop into #16 §3.4) remains open; [CROSS-PENDING] tag in §3.10 resolves atomically with #16 approval.

**Status:**
- **10 APPROVED, 0 SUSPENDED, 2 IN REVIEW, 1 IN PROGRESS, 7 NOT STARTED.**

**Still Pending:**
- Fixed64 Math Library (#9) lead-developer sign-off (IN REVIEW since May 6).
- Deterministic Simulation (#16) Tier 2 Final Approval — #17 precondition now satisfied; #9, #18, #19 remain.
- Testing Strategy (#19) approval — blocked by KD-2 preconditions (#16 Tier 2 APPROVED, #18 outline, all `TBD-NORMATIVE` tags resolved).
- Spec #18 (Performance Optimization Strategy) — NOT STARTED; blocks both #19 approval and #16 Tier 2.
- Heading Mechanics (#10), Goalkeeper Mechanics (#11), Positioning AI (#12) — NOT STARTED.
- Pressing AI (#13), Defensive AI (#14), Attacking AI (#15) — NOT STARTED.
- 8 approval tags created locally remain unpushed (including new `spec-event-system-v1.0-approved`; HTTP 403 branch-protection); push after merge to main per CLAUDE.md OPEN ISSUES.

---

## MILESTONES

| Milestone | Target Date | Status | Actual Date |
|-----------|-------------|--------|-------------|
| Ball Physics Spec Approved | Feb 8, 2026 | ✅ COMPLETE | Feb 8, 2026 |
| Agent Movement Spec Approved | Feb 15, 2026 | ✅ COMPLETE | Apr 27, 2026 |
| Collision System Spec Approved | Feb 22, 2026 | ✅ COMPLETE | Feb 19, 2026 |
| First Touch Mechanics Spec Approved | Feb 22, 2026 | ✅ COMPLETE | Feb 22, 2026 |
| Pass Mechanics Spec Approved | Feb 22, 2026 | ✅ COMPLETE | May 6, 2026 |
| Priority 1 Complete (5 specs) | Feb 29, 2026 | ✅ COMPLETE | May 6, 2026 |
| Shot Mechanics Spec Approved | Mar 7, 2026 | ✅ COMPLETE | Apr 27, 2026 |
| Perception System Spec Approved | Mar 7, 2026 | ✅ COMPLETE | Apr 22, 2026 |
| Decision Tree Spec Approved | — | ✅ COMPLETE | Apr 27, 2026 |
| Priority 2 Complete (9 specs) | Mar 31, 2026 | ⚠️ DELAYED | — |
| Priority 3 Complete (12 specs) | Apr 30, 2026 | ⏳ NOT STARTED | — |
| Priority 4 Complete (16 specs) | May 31, 2026 | ⏳ NOT STARTED | — |
| Priority 5 Complete (20 specs) | July 5, 2026 | ⏳ NOT STARTED | — |
| **Stage 0 Spec Phase Complete** | **July 5, 2026** | **⚠️ AT RISK** | **—** |

---

## QUALITY METRICS

**Target Metrics:**
- All specifications: 100% template compliance
- Average page count: 20 pages per spec
- Test scenarios: Minimum 15 per spec (10 unit + 5 integration)
- Review cycles: Maximum 3 per spec

**Actual Metrics (as of Week 12):**

| Spec | Pages (est.) | Test Scenarios | Review Cycles | Template Compliance |
|------|-------------|----------------|---------------|---------------------|
| Ball Physics | ~50 | 44 (2.9× min) | 1 | 100% |
| Agent Movement | ~130 | 83 (5.5× min) | 2 | 100% |
| Collision System | ~50 | ~70 (4.7× min) | 2 | 100% |
| First Touch | ~70 | 72 (4.8× min) | 2 | 100% |
| Pass Mechanics | ~65 | 100 (6.7× min) | 3+ (audit) | 100% |
| Shot Mechanics | ~70 | 104 (6.9× min) | 3 | 100% |
| Perception System | ~80 | 92 (1.8× unit min) | 3 | 100% |
| Decision Tree | ~55+ | n/a (draft-level) | 1 | Draft-level (comprehensive audit candidate before implementation) |
| **Average** | **~71** | **81** | **2.4** | **100%** |

**Notes:**
- All completed specs significantly exceed test scenario minimums (average 4.8×)
- Shot Mechanics adopted parameter-based physics — no shot type enum — consistent with project pattern
- Perception System uses 10Hz heartbeat, not 60Hz; lower minimum test count applies
- ~45–62% gameplay-tuned constants across all specs — documented and accepted as expected
- DOI verification now standard practice; two specs required post-draft DOI corrections

---

## NOTES

### Process Improvements
- File version discipline: superseded versions removed promptly
- Approval checklist template proven effective — reused across all specs
- Spec Error Log pattern: centralised cross-spec issue tracking prevents cascading failures
- Interface design principle confirmed: write interfaces only when both sides are specified
- Possession architecture decision (ERR-008): possession is agent state, not ball state
- Parameter-based physics pattern now project standard (Ball Physics → Shot Mechanics → all future specs)
- DOI verification must occur before Section 9 checklist is drafted, not after
- ~55–62% gameplay-tuned constants in AI-adjacent specs is expected and acceptable

### Lessons Learned
- Rolling friction discrepancy (REV-001) caught during numerical validation — validate early
- Cross-spec audit before Section 3 drafting catches blocking errors — essential practice
- Wave-based fix ordering (dependency-first) prevents cascading failures during error resolution
- Iterative numerical verification (Appendices B) consistently finds formula/test discrepancies
- Section 9 checklist should be the last file written, not drafted concurrently with sections
- Comprehensive post-completion audits (e.g. Pass Mechanics March 25) are valuable but costly — schedule explicitly

### Open Items (Non-Blocking)
- ERR-002: StringIDs in master-vol-4-tech-implementation.md — fix at convenience
- ERR-003: PerformanceContext violation language in Agent Movement §3.2 — fix at convenience
- Agent Movement §3.2 and §3.7 attribute references should be verified against PlayerAttributes v1.3 before implementation
- Ball Physics §8 v1.4: DOI corrections issued from Shot Mechanics §8 audit — verify incorporated
- Perception System Section 9 Approval Checklist v1.7 (April 22, 2026) — ✅ APPROVED; all blockers resolved

---

## NEXT ACTIONS

**Immediate:**
1. ~~Lead developer sign-off: Perception System (#7)~~ ✅ APPROVED April 22, 2026
2. ~~Lead developer sign-off: Shot Mechanics (#6)~~ ✅ APPROVED April 27, 2026
3. ~~Lead developer sign-off: Agent Movement (#2)~~ ✅ APPROVED April 27, 2026
4. Lead developer re-review and re-sign-off: Pass Mechanics (#5) — still SUSPENDED
5. ~~Lead developer review and sign-off: Decision Tree (#8)~~ ✅ APPROVED April 27, 2026 (draft-level)
6. ~~Systematic renumbering pass~~ ✅ COMPLETE April 26, 2026 (commits 8d7f729 + 75e9af5; ~115 substitutions across three passes)

**After Pass Mechanics re-sign-off:**
7. Commit all approved specs to repo (mostly done; tags created locally)
8. Push tags once branch-protection allows: `spec-ball-physics`, `spec-agent-movement`, `spec-collision-system`, `spec-first-touch`, `spec-shot-mechanics`, `spec-perception-system`, `spec-decision-tree` (all v1.0-approved). Pass Mechanics tag deferred until re-sign-off.
10. Remove superseded file versions (see docs/tracking/file-manifest.md)
11. Begin Fixed64 Math Library Specification (#9)
12. Begin Heading Mechanics Specification (#10)

---

**Last Updated:** May 6, 2026  
**Updated By:** AI Assistant (tracking-doc reconciliation pass)  
**Next Review:** After Pass Mechanics (#5) re-sign-off and Fixed64 Math (#9) sign-off

---

## CHANGELOG

**May 12, 2026:**
- Testing Strategy & Framework (#19) reclassified `NOT STARTED` → `IN REVIEW`. Initial section-file draft authored May 12 from `outline-detailed.md` v1.1; v0.2 self-critique sweep applied (3 H / 6 M / 8 L findings, all resolved). Approval gated on KD-2 preconditions (#16 Tier 2 APPROVED, #18 outline-level draft, all `TBD-NORMATIVE` tags resolved).
- Summary counts updated: **9 APPROVED, 0 SUSPENDED, 2 IN REVIEW, 1 IN PROGRESS, 8 NOT STARTED.**
- Week 16 update-log entry added.

**May 11, 2026:**
- Code Standards & Style Guide (#20) reclassified `NOT STARTED` → `APPROVED`. Section files + appendices authored May 7–8 from `outline-detailed.md` v1.3; adversarial review pass-1 applied May 11; lead-developer R-01..R-05 sign-off completed.

**May 6, 2026:**
- Fixed64 Math Library (#9) reclassified `NOT STARTED` → `IN REVIEW` after Pass 2 adversarial critique fixes landed. All sections (1–9 + appendices) present at v0.2–v0.3; awaiting lead developer sign-off.
- Summary counts updated: 7 APPROVED, 1 SUSPENDED, 1 IN REVIEW, 1 IN PROGRESS, 10 NOT STARTED.
- Tracking-doc reconciliation pass: Priority 2 progress line corrected; Decision Tree quality-metrics row marked draft-level; Weeks 13–15 update log added.

**May 3, 2026:**
- Deterministic Simulation (#16): third-pass adversarial critique resolved at v0.9. ERR-016-002 filed (EntityId no-reuse cross-spec back-propagation against approved specs #2 and #8 pending).
- Deterministic Simulation (#16) folder cleanup: `adversarial-review.md` and `third-pass-fix-log.md` superseded by consolidated `critique-log.md`.

**May 2, 2026:**
- Deterministic Simulation (#16): status updated from NOT STARTED to IN PROGRESS. All 9 sections + appendices drafted; v0.7 and v0.8 fix passes complete. v0.8 resolves 21 issues from third-pass critique (critical KDF ambiguity, mislabeled constants, AI_NoOp underdocumentation, and 18 medium/low issues). Gated at IN PROGRESS pending §9.5 implementation-readiness sign-off.

**April 27, 2026:**
- Lead developer sign-off pass: Agent Movement (#2), Shot Mechanics (#6), Decision Tree (#8) all APPROVED. Stage 0 spec count: 7 APPROVED, 1 SUSPENDED, 0 IN REVIEW, 12 NOT STARTED.
- Decision Tree (#8) approved at draft-level quality gate; comprehensive audit candidate before implementation.

**April 26, 2026:**
- Approval summary corrected to 4 approved (#1, #4, #7, plus #3 per registry); previous "3 approved" contradicted the table.
- Perception System (#7) status updated to ✅ APPROVED Apr 22, 2026 in summary lists.
- Collision System (#3) status disagreement flagged (§9 file says PENDING; trackers say APPROVED) — subsequently resolved by §9 back-fill v2.1.
- Scheduling milestones de-emphasized — passion project, no deadline.
- Renumber sweep complete (~115 body-text substitutions across three passes; zero remaining stale references).
- Fixed64 migration deferred to Stage 5+ (Stage 0 uses float + state snapshots for single-machine determinism).
