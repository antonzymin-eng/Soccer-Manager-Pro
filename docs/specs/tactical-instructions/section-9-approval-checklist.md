# Tactical Instructions Specification #21 — Section 9: Approval Checklist

**Created:** June 20, 2026
**Last Updated:** June 20, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

This checklist is the normative quality gate for transitioning Tactical Instructions #21 from
`IN REVIEW` to `APPROVED`. Each item is verifiable against the section files in
`docs/specs/tactical-instructions/`. No checklist entries are fabricated (CLAUDE.md).

## 9.1 Self-contained spec content

| # | Item | Status | Evidence |
|---|---|---|---|
| 1 | All 32 FRs (FR-TI-001..032) present and numbered | [x] | `section-2.md` §2.1 |
| 2 | Every FR traces to a test **or a named verification** (4 structural FRs verify by asmdef-grep/inspection) | [x] | `section-5.md` §5.7 traceability |
| 3 | Data structures defined with field-level typing (3 structs + 16 enums) | [x] | `section-2.md` §2.2 |
| 4 | Identity-factory contract defined (`Balanced`/`Default`) | [x] | `section-2.md` §2.2.1–2.2.3; FR-TI-031 |
| 5 | Failure modes F1–F5 with detection/recovery/test | [x] | `section-2.md` §2.4 |
| 6 | Every constant carries exactly one tag; no `[EST]` remain | [x] | Appendix A |
| 7 | All `[GT]` constants have Appendix-A derivation/rationale | [x] | `appendices.md` Appendix A |
| 8 | Every §3 mapping has units, ranges, and a worked example | [x] | `section-3.md` §3.1–§3.5 |
| 9 | Mentality→(profile,risk,line) table pinned | [x] | `section-3.md` §3.2 |
| 10 | Role-weight model + default-row identity defined | [x] | `section-3.md` §3.3 |
| 11 | Enum-translation seams + clamp rule (F5) defined | [x] | `section-3.md` §3.1 |
| 12 | Man-mark override precedence (KD-9) explicit | [x] | `section-3.md` §3.5; FR-TI-023 |
| 13 | Assembly placement + acyclic-graph argument | [x] | `section-4.md` §4.1; FR-TI-002/003 |
| 14 | File layout (one type per file) | [x] | `section-4.md` §4.2 |
| 15 | Routing contract (two paths; sole populator) | [x] | `section-4.md` §4.4; KD-4 |
| 16 | Translate-once / no hot-path mapping | [x] | `section-4.md` §4.4; FR-TI-025 |
| 17 | Determinism boundaries (no RNG; stride apply; schema bump) | [x] | `section-4.md` §4.6; FR-TI-026/027/028 |
| 18 | Test counts ≥ 78 with layer breakdown | [x] | `section-5.md` §5.1 |
| 19 | FR-to-test traceability matrix (all 32) | [x] | `section-5.md` §5.7 |
| 20 | Performance: cold-path-only model + ≤0.01 ms #8 charge | [x] | `section-6.md` |
| 21 | Future extensions + stage gating | [x] | `section-7.md` |
| 22 | Cross-refs allocated XC-021-001..014 | [x] | `section-8.md` §8.1 |
| 23 | ERR-021-001..004 declared with target/stage/status | [x] | `section-8.md` §8.3 |
| 24 | CLAUDE.md invariants bound | [x] | `section-8.md` §8.2 |
| 25 | Naming reconciled (`tactical-instructions/`; supplement superseded) | [x] | `section-1.md` §1.8 |

## 9.2 Outstanding gates (block `IN REVIEW → APPROVED`)

| # | Gate | Status |
|---|---|---|
| G1 | Formal PASS-1 adversarial review of the section files + fix pass | DONE — `adversarial-review-section-files-v1.md` (2H+4M+4L), all resolved in the v0.2 fix pass |
| G2 | `RoleWeightModifiers` + §3.2 value **balance pass** (numerical mirror + adversarial) before `[GT]` values are pinned | OPEN — values currently illustrative; tests assert shape, not magnitude (§5.6) |
| G3 | Lead-developer R-01..R-05 sign-off | OPEN |
| G4 | `SPEC_INDEX.md` row 21 reflects status | DONE (added v0.1, IN REVIEW) |

## 9.3 Non-blocking (Stage-1 implementation-time, per §8.3)

ERR-021-001..004 land at their named stage (T0–T3); none gate spec approval (parallel to the #13/#14/#15
deferred #17 channel-row precedent). **Runtime activation** remains gated on KD-8 prerequisites
(config-loader + match-engine Phase C/D) — a Stage-1 dependency, not a spec-approval gate.

## 9.4 Sign-off (pending)

R-01 (lead developer) … R-05: **not yet signed** — gated on G1–G3.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-06-20 | — | Initial checklist; §9.1 self-contained items satisfied; G1–G3 open (PASS-1, balance pass, sign-off). |
| 0.2 | 2026-06-20 | — | PASS-1 fix pass: item 2 reworded for named-verification FRs (M-4); G1 marked DONE. |
#endregion
