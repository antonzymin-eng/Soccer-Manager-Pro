# Performance Optimization Strategy Specification #18 — Section 8: References & Citation Audit

**Created:** May 13, 2026
**Last Updated:** May 14, 2026 (v0.3 PASS-2 adversarial-review fix pass)
**Purpose:** Authoritative source register, verification notes,
cross-spec citation audit, and constant-provenance summary. Every
citation in §3 / §4 / §5 / §6 / §7 / Appendices resolves to a row in
§8.1 and (where applicable) carries a `TBD-NORMATIVE` tag per the
KD-3 / KD-4 status caveats.

---

## 8.1 Source Register

### 8.1.1 CLAUDE.md (project root)

- Tick rates: 10 Hz tactical, 60 Hz physics ("Heartbeat Tick Rate").
- Zero-allocation mandate ("When Writing Code: zero-allocation
  architecture in the game loop").
- Deterministic-replay hard requirement.
- Stage 0 host platform pin posture ("Fixed64 stage scope decision").
- "Interface Design Principle" (write interfaces only when both sides
  are specified — ERR-001 / ERR-004 hazard).
- "Constant Tags" taxonomy (`[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]`,
  `[CROSS]`, `[CROSS-PENDING]`).
- "Things That Have Gone Wrong Before" reference table (drives KD-1
  cite-not-redefine and Appendix D survey caution).

### 8.1.2 Spec #16 — Deterministic Simulation Specification

**Status:** `IN PROGRESS` per `SPEC_INDEX.md` (May 13, 2026).
All citations below tagged `TBD-NORMATIVE` per KD-3 status caveat.

- §1.3.1 determinism tier classification (Tier A / B / C).
- §3.1.2 canonical tick pipeline (emission-veto surface for §3.8.3
  FR-PO-058a).
- §3.2.4.1 canonical record format (binding for FR-PO-058 and §3.8.4
  / §4.2 / Appendix A).
- §4.8 `EnvironmentFingerprint` (consumed by §3.3.2 session contract).
- §5 regression-scenario corpus / test catalogue (consumed by §3.3.3
  scenario binding).

**Citation drift history:** outline v1.0 incorrectly cited §7 for
regression scenarios, §5 for record format, and §8 for "trace
channels". Outline v1.1 (May 13, 2026) corrected against
`deterministic-sim/section-*.md`: regression scenarios live at §5;
record format at §3.2.4.1; no §8 trace-channel section exists (trace
channels are now owned by #18 per inverted KD-3). Section authors MUST
re-grep at draft / re-review time per §1.4 caveat.

### 8.1.3 Spec #19 — Testing Strategy & Framework Specification

**Status:** `IN REVIEW` per `SPEC_INDEX.md` (May 13, 2026).
All citations below tagged `TBD-NORMATIVE` per KD-4 status caveat.

- §3.1 test taxonomy.
- §3.3.3 `ScenarioRunner` (consumer of `IPerfHarness` per §4.3.1 /
  §4.4).
- §3.7 flake handling.
- §6 CI orchestration (composition rule per §6.3 / §3.5.3).
- KD-7 (determinism-aware fuzz testing — parallels #18 KD-6 for
  profiling).
- KD-8 (cross-spec scenario authority).

### 8.1.4 Spec #20 — Code Standards & Style Guide

**Status:** `APPROVED` (May 11, 2026).

- §2.1 exception-with-sign-off semantics (cited by §2.1 / §3.5.5).
- §3 zero-allocation rules (cited by §3.7, FR-PO-050 … 053).
- §3.5.5 anti-pattern list (cited by §4.3.1 single-implementation
  rule).
- §4.1 dependency-arrow rule (cited by §3.8.5 / FR-PO-060 / §4.3.4
  dashboard-helper placement).

### 8.1.5 Approved-spec §6 (or §4.5) per-subsystem budgets

Cited by reference only (KD-2 cite-not-redefine; per-spec numbers
never republished here):

| Spec | Section | Status | Verified date |
|------|---------|--------|---------------|
| #1 Ball Physics | §6 | APPROVED | per `SPEC_INDEX.md` |
| #2 Agent Movement | §6 | APPROVED | per `SPEC_INDEX.md` |
| #3 Collision System | §6 | APPROVED | per `SPEC_INDEX.md` |
| #4 First Touch | §6 | APPROVED | per `SPEC_INDEX.md` |
| #5 Pass Mechanics | §6 | APPROVED (re-approved May 6, 2026) | per `SPEC_INDEX.md` |
| #6 Shot Mechanics | §4.5 | APPROVED | 0.05 ms total / ~0.017 ms estimated, verified per outline.md adversarial review |
| #7 Perception | §6 | APPROVED | per `SPEC_INDEX.md` |
| #8 Decision Tree | §6 | APPROVED (draft-level) | per `SPEC_INDEX.md` |
| #17 Event System | §6 | APPROVED | per `SPEC_INDEX.md` (May 13, 2026) |

Subsection numbers for each spec MUST be grep-verified at draft time
per §1.4 caveat.

### 8.1.6 Tracking documents

- `docs/tracking/certification-platform.md` — Stage 0 row placeholder
  per CLAUDE.md OPEN ISSUES; binding target for KD-9 platform pin.
- `docs/tracking/spec-error-log.md` — `ERR-018-NNN` rows logged here.
- `docs/tracking/PROGRESS.md` — Stage 0 monthly reporting target
  (FR-PO-075).
- `docs/tracking/file-manifest.md` — updated atomically with section-
  file creation.

### 8.1.7 Planning documents

- `docs/planning/development-best-practices.md`.
- `docs/planning/master-development-plan.md`.

### 8.1.8 External standards

- RFC 2119 — MUST / SHOULD / MAY conformance levels (cited by §2.1).
- (Stage 0+1 deliverable) profiler / allocation-tracker / benchmark-
  framework documentation URLs with retrieval dates — placeholders at
  draft.

## 8.2 Verification Notes

- **CLAUDE.md citations** in §3 verified against current CLAUDE.md
  text on this spec's drafting date (May 13, 2026).
- **Spec #16 / #19 / #20 citations** verified against current
  approved-or-draft text and section number per `SPEC_INDEX.md` (May
  13, 2026).
- **Per-spec §6 citations** verified for existence; subsection
  numbers may need re-grep at draft time per §1.4 status caveats.
- **External-standards URLs** — Stage 0+1 deliverable; placeholders
  at draft.

## 8.3 Cross-Spec Citation Audit

- **Spec #18 is cited by:**
  - Spec #19 §6 (CI orchestration boundary; KD-3 there / KD-4 here).
  - Spec #19 KD-2 sequencing precondition (b) "Spec #18 having at
    least an outline-level draft with §4 and §7 headers" — satisfied
    by `outline-detailed.md` v1.0 and by this section-file set.
  - (Downstream) every per-spec §6 once the per-spec §6 schema is
    ratified.

- **Spec #18 cites:**
  - **Spec #16** (substantive): determinism tiers §1.3.1; regression
    scenarios §5; canonical record format §3.2.4.1; canonical tick
    pipeline §3.1.2 (emission-veto authority); `EnvironmentFingerprint`
    §4.8. **Trace pipeline is now owned by Spec #18** per inverted
    KD-3 — #18 cites #16 for record-format compatibility (§3.2.4.1) and
    emission constraints (§3.1.2), not for trace-channel architecture.
  - **Spec #19** (boundary): CI orchestration §6; flake handling
    §3.7; cross-spec scenario authority KD-8.
  - **Spec #20** (boundary): zero-allocation rules §3;
    `[HotPathAllocExempt]` §3; exception-with-sign-off §2.1.

- **No `[CROSS]` constants are imported.** Spec #18 declares none.
  Tier *vocabulary* cited from #16 §1.3 by reference only (KD-1
  cite-not-redefine).

- **Per-spec budget numbers cited by reference only** (KD-2); never
  republished. The citation list is the §3.1.3 / Appendix C roll-up
  table.

## 8.4 Constant Provenance Summary

Spec #18 declares **no physical constants** (per §3.10). Governance
numerics are tagged `[GT]` or `[EST]` and their evidence-artifact
convention is recorded inline:

| Value | Tag | Defined in | Evidence-artifact citation |
|-------|-----|------------|----------------------------|
| Per-PR regression threshold = +5% | `[GT]` | §3.5.2 | `section-3.md §3.5.2` |
| Absolute-threshold guard = +10% | `[GT]` | §3.5.6 | `section-3.md §3.5.6` |
| Hot-path allocation budget = 0 bytes/tick | `[FIXED]` | §3.7.3 | `section-3.md §3.7.3` |
| Sampling-profiler default = 1 kHz | `[EST]` | §3.3.4 | `section-3.md §3.3.4` |
| Statistical-significance N = 30 runs / 95% CI | `[EST]` | §3.4.3 | `section-3.md §3.4.3` |
| Headroom multiplier (per spec) | `[GT]` | §3.1.2 | `section-3.md §3.1.2` |
| First-tick warmup count N | `[EST]` | §3.9.4 | `section-3.md §3.9.4` |
| `[EST]`→`[GT]` promotion tolerance = ±20% | `[GT]` | §3.9.1 | `section-3.md §3.9.1` (rationale recorded in §3.10) |
| Per-spec p50/p99 rolling window N = 100 captures | `[GT]` | Appendix F.1 | `appendices.md` Appendix F.1 (rationale recorded in §3.10) |
| Flake-rate boundary-defect routing threshold = 1% | `[GT]` | Appendix F.5 | `appendices.md` Appendix F.5 (rationale recorded in §3.10) |

Each evidence-artifact citation is the body-text section that
introduces the literal number. The §5.3 / §9 auditor (or the Spec
#19 §5.3 checklist auditor under #19 KD-6) confirms the cited file
path contains the literal number claimed. No separate
`tools/governance-numbers.md` file is created.

## 8.5 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.3     | May 14, 2026 | Claude Code | PASS-2 adversarial-review fix pass (`ERR-018-014`). Duplicate v0.2 version-history row consolidated. No content changes — §8 was clean of PASS-2 H/M findings (§8.4 mirror table was already correct after v0.2 — the §3.10 duplicate rows ERR-018-013 removed were absent from §8.4). |
| 0.1     | May 13, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1 §8. Source register, verification notes, cross-spec citation audit, and constant-provenance summary authored. Spec #16 citation drift history recorded (outline v1.0 → v1.1 correction: regression scenarios §5 not §7; record format §3.2.4.1 not §5; no §8 trace channels exist — #18 owns trace pipeline per inverted KD-3). All #16 / #19 citations tagged `TBD-NORMATIVE` per KD-3 / KD-4 status caveats. |
| 0.2     | May 14, 2026 | Claude Code | PASS-1 adversarial-review fix pass (`ERR-018-002` / 006 / 008 / 010). §8.1.4 Spec #20 register entry no longer claims `[HotPathAllocExempt]` is declared in #20 §3 (ownership relocated to #18 §3.7.5). §8.4 mirror rows updated: hot-path allocation budget `[GT]` → `[FIXED]`; new rows added for ±20% promotion tolerance, N=100 rolling-window, 1% flake-rate routing threshold. Also: §8.1.2/§8.3 #16 §3.1→§3.1.2 canonical tick pipeline; §8.1.2/§8.3 #16 §4→§4.8 EnvironmentFingerprint. |
