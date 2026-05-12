# Testing Strategy & Framework Specification #19 — Section 8: References & Citation Audit

**Created:** May 12, 2026
**Last Updated:** May 12, 2026
**Purpose:** Source register, verification notes, cross-spec citation
audit, and constant-provenance summary.

---

## 8.1 Source Register

### 8.1.1 Internal Sources

- **Root `CLAUDE.md`** — project invariants, "When Writing Code"
  rules, coordinate system, fatigue convention, constant-tag
  taxonomy, Interface Design Principle. Authoritative for KD-7
  (SplitMix64), banned-API list (`System.Random`, `DateTime.Now`),
  Stage 0 `float` posture.
- **Spec #16 (Deterministic Simulation)** — `[TBD-NORMATIVE]`.
  - §1.3 / §1.3.1 — tier classification (Tier A / B / C). Consumed by
    KD-9 (§3.6.2) and FR-TS-029, FR-TS-050, FR-TS-060.
  - §4 — `EnvironmentFingerprint` (FR-TS-061, FR-TS-071).
  - §5 — canonical binary layout for golden traces and fixtures.
    Consumed by KD-10 (§3.3.4, §3.8, §4.2, FR-TS-026, FR-TS-034,
    FR-TS-069).
  - §7 — determinism regression suite. Consumed by KD-2 (§3.2,
    FR-TS-011 … 020).
  - §8 — trace channels.
  - Status: `IN PROGRESS`. Section numbers MUST be re-grepped at next
    revision per §3.6.1 cite-precision guard.
- **Spec #18 (Performance Optimization)** — `[TBD-NORMATIVE]`.
  - §4 — performance regression gates. Consumed by KD-3 (§6.2,
    FR-TS-080).
  - §7 — performance budget enforcement.
  - Status: `NOT STARTED`. All citations are placeholder names; #19
    cannot advance past `IN REVIEW` until #18 has at least an
    outline-level draft confirming the cited section numbers (§1.4,
    §9.3).
- **Spec #20 (Code Standards)** — APPROVED May 11, 2026.
  - §2.1 — conformance levels (consumed by §2.1).
  - §3.4.2 — `System.Random` ban (cited by FR-TS-036).
  - §3.5.5 — IoC anti-pattern (cited by §4.3.1).
  - §3.9.4 — test-fixture rule carve-outs (cited by §1.4, §3.9.4).
  - §4.1 — dependency-arrow shape (cited by §4.1).
  - §5.1 — Stage 0 manual-review acknowledgement (cited by §5.1).
  - §5.2 — Roslyn analyzer pin (cited by §6.1.2).
  - §5.3 — Stage 1 numeric-threshold revisit (cited by §3.1.2).
  - §5.5 — degenerate Stage 0 traceability acknowledgement (cited by
    §5.6).
  - §7.3 — Stage 5+ posture (cited by §7.3).
- **`docs/planning/development-best-practices.md`** — general project
  conventions; consulted at draft time.
- **`docs/planning/master-development-plan.md`** — stage-gating
  rationale; consulted for KD-5.
- **`docs/tracking/certification-platform.md`** — placeholder at
  draft time per CLAUDE.md OPEN ISSUES; cited in §1.4 for Stage 0+1
  activation precondition.
- **`docs/tracking/spec-error-log.md`** — destination for
  `ERR-019-NNN` rows logged by §3.5.4 acknowledged dilution policy.

### 8.1.2 External Sources

- **RFC 2119** — MUST / SHOULD / MAY keywords. Consumed by §2.1.
- **NUnit / xUnit / FsCheck / Coverlet / Stryker.NET** — framework
  candidates pinned at Stage 0+1. URLs + retrieval dates are a Stage
  0+1 deliverable (§7.1); current text uses framework names only.

## 8.2 Verification Notes

- Every CLAUDE.md citation in §3 was verified against the current
  CLAUDE.md text on this spec's drafting date (May 12, 2026).
- Every Spec #16 citation is tagged `[TBD-NORMATIVE]` per KD-2 status
  caveat. Section numbers (e.g., #16 §1.3.1, §5, §7, §8) MUST be
  re-grepped against current `deterministic-sim/section-1.md`,
  `section-5.md`, `section-7.md` at next revision; §3.6.1
  cite-precision guard mandates this for any author touching §3.2,
  §3.6, §5.7.
- Every Spec #18 citation is tagged `[TBD-NORMATIVE]` per KD-3 status
  caveat. Resolution of these tags is a §9.3 precondition gated on
  #18 reaching at least outline-level draft status.
- Every Spec #20 citation was verified against
  `code-standards/section-N.md` at draft time.

## 8.3 Cross-Spec Citation Audit

- **Cited by:** every per-spec §5 (downstream). The cited content is
  Spec #19's taxonomy, naming convention, coverage-tier policy, and
  Appendix C schema.
- **Cites:**
  - #16 (substantive): tier classification (§3.6.1), canonical save
    format (§3.3.4, §3.8), regression suite (§3.2), trace channels
    (§3.8.4 fingerprint reference). `[TBD-NORMATIVE]`.
  - #18 (boundary): performance gates (§6.2, §6.6). `[TBD-NORMATIVE]`.
  - #20 (boundary): test-fixture carve-outs (§3.9.4), IoC anti-pattern
    (§4.3.1), `System.Random` ban (§3.4.5).
- **No `[CROSS]` constants imported.** Spec #19 declares no physical
  constants. Tier vocabulary cited from #16 §1.3.1 by reference only
  (KD-1).

## 8.4 Constant Provenance Summary

Spec #19 declares **no physical constants**. The numeric thresholds
it publishes are governance values (`[GT]`); the full table is in
§3.10. Provenance summary:

| Constant | Tag | Provenance |
|---------|-----|------------|
| Pyramid percentages (60% / 25% / 12% / 3%) | `[GT]` | Standard pyramid heuristic; revisited Stage 1 (§3.1.2). |
| Tier A coverage (98% / 95%) | `[GT]` | KD-9 authoritative-tier target (§3.6.2). |
| Tier B coverage (90% / 80%) | `[GT]` | KD-9 bounded-authoritative-tier target (§3.6.2). |
| Unit wall-time bound (1 ms) | `[GT]` | Sub-millisecond fast-feedback bound (§3.1.1). |
| Quarantine auto-expiry (14 days) | `[GT]` | Two-week resolution window (§3.7.3). |
| Eviction threshold (3 quarantines / 90 days) | `[GT]` | Three-strikes rule (§3.7.4). |

KD-6 evidence artifact for each value: the section-file citation that
publishes the number (§3.10 evidence-artifact convention; §9.4
checklist row).

## 8.5 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.1     | May 12, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1. Source register populated; constant-provenance summary references §3.10. |
