# Living World System Specification #22 — Section 9: Approval Checklist

**Created:** June 21, 2026
**Last Updated:** June 21, 2026 (v0.8 — PASS-7 fix pass landed; G1 cites all seven passes; G2/G3 remain open)
**Version:** 0.8
**Status:** IN REVIEW

---

This checklist is the normative quality gate for transitioning Living World #22 from `IN REVIEW` to
`APPROVED`. Each item is verifiable against the section files in `docs/specs/living-world/`. No
checklist entries are fabricated (CLAUDE.md).

## 9.1 Self-contained spec content

| # | Item | Status | Evidence |
|---|---|---|---|
| 1 | All 34 FRs (FR-LW-001..034) present and numbered | [x] | `section-2.md` §2.1 |
| 2 | Every FR traces to a test or a named verification (structural FRs verify by asmdef-grep/inspection) | [x] | `section-5.md` §5.7 |
| 3 | Data structures defined with field-level typing (5 structs + 4 enums) | [x] | `section-2.md` §2.2 |
| 4 | Neutral/identity state + additive-only identity FR (empty world reproduces canon behaviour) | [x] | `section-2.md` §2.3 / FR-LW-034; T-LW-DET-007 |
| 5 | Failure modes F1–F6 with detection/recovery/test | [x] | `section-2.md` §2.4 |
| 6 | Canon consumed-as-is matrix (no redesign of vol-2/vol-3) | [x] | `section-1.md` §1.3 |
| 7 | Every constant carries exactly one tag; no `[EST]` remain | [x] | `appendices.md` Appendix A |
| 8 | Every §3 mapping has units, ranges, and a worked example | [x] | `section-3.md` §3.1–§3.5 |
| 9 | Episodic-memory model (episodeId, salience, eviction, pinning) | [x] | `section-3.md` §3.2; FR-LW-008..010/018 |
| 10 | Deterministic procedural-text model; no runtime model inference | [x] | `section-3.md` §3.3; FR-LW-011/012 |
| 11 | Arc lifecycle + provenance-at-spawn | [x] | `section-3.md` §3.4/§3.6; FR-LW-014/016/018 |
| 12 | Two-tier LOD + cold-store/rehydration; deterministic transitions | [x] | `section-3.md` §3.5; FR-LW-023..025 |
| 13 | Assembly placement + acyclic-graph argument | [x] | `section-4.md` §4.1; FR-LW-002/003 |
| 14 | Season-calendar loop distinct from `MatchClock`; tick order | [x] | `section-4.md` §4.2; FR-LW-019 |
| 15 | Determinism boundaries (RNG stream, iteration order, snapshot, no write-back) | [x] | `section-4.md` §4.4; FR-LW-020/021/027 |
| 16 | Save-size budget + eviction; per-class split deferred | [x] | `section-4.md` §4.5; FR-LW-026 |
| 17 | Test counts ≥ 74 with layer breakdown | [x] | `section-5.md` §5.1 |
| 18 | FR-to-test traceability matrix (all 34) | [x] | `section-5.md` §5.7 |
| 19 | Performance posture (slow-loop / cold-path; no hot-path budget) | [x] | `section-6.md` §6.0 |
| 20 | Verification harnesses (fuzz/soak/coverage/replay) | [x] | `section-6.md` §6.1–§6.5 |
| 21 | Future extensions + stage gating + recorded decisions | [x] | `section-7.md` |
| 22 | Cross-refs allocated XC-022-001..014 | [x] | `section-8.md` §8.1 |
| 23 | ERR-022-001..004 back-props declared with target/stage/status (season-loop is a §7.1 forward deliverable, not an ERR) | [x] | `section-8.md` §8.3 |
| 24 | CLAUDE.md invariants bound | [x] | `section-8.md` §8.2 |
| 25 | Naming reconciled (`living-world/`; supplement superseded) | [x] | `section-1.md` §1.7 |

## 9.2 Approval gates

| # | Gate | Status |
|---|---|---|
| G1 | Formal adversarial review of the section files (PASS-1..7) + fix passes | **DONE** — `…-v1.md` (4M+3L), `…-v2.md` (3M+3L), `…-v3.md` (2M+2L), `…-v4.md` (1M+3L), `…-v5.md` (1M+2L), `…-v6.md` (1M+2L), `…-v7.md` (1M+1L, no High → v0.8), all resolved; no High in any pass — convergence reached |
| G2 | `[GT]` value **balance pass** (numerical mirror + adversarial) before values are pinned | **CARRIED FORWARD (post-APPROVED, non-blocking)** — §3/Appendix A values are illustrative; tests assert shape/direction not magnitude (§5). Precedent: #21 G2, #8 draft-level, #9 §9.8, #16 post-approved |
| G3 | Lead-developer R-01..R-05 sign-off | **OPEN** |
| G4 | `SPEC_INDEX.md` row 22 reflects status | DONE — IN REVIEW, Jun 21, 2026 |

## 9.3 Non-blocking (Stage-1 implementation-time, per §8.3)

ERR-022-001..005 land at their named stage; none gate spec approval. **Runtime activation** remains
gated on KD-10 (world store + season loop; vol-2/vol-3 impl.; `[GT]` config-loader; match-outcome
events) — a Stage-1 dependency, not a spec-approval gate.

## 9.4 Sign-off

Pending. Advancement to `APPROVED` requires G1 (section-file PASS-1 + fix pass) and G3 (lead-developer
R-01..R-05). G2 is explicitly carried forward as a post-APPROVED Stage-1 follow-up — the `[GT]`
magnitudes are illustrative and are not relied on for sign-off (the contract is the shapes/directions).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-06-21 | — | Initial checklist on promotion from design supplement v0.7; §9.1 self-contained items satisfied; G1 (section-file PASS-1), G2 (balance pass), G3 (sign-off) open. |
| 0.2 | 2026-06-21 | — | PASS-1 fix pass (4M+3L resolved): FR-LW-034 added; counts updated (34 FRs / ≥73 tests); ERR back-props 001..004; G1 marked DONE. G2/G3 remain open. |
| 0.3 | 2026-06-21 | — | PASS-2 fix pass (3M+3L resolved): NaN sentinel → `ActiveLayers` bitmask; FR-LW-034 scoped to world-state-subset digest; cold-summary eviction; `Arc.State` stability; G1 cites both passes. G2/G3 remain open. |
| 0.4 | 2026-06-21 | — | PASS-3 fix pass (2M+2L resolved): `PlayerEdge` pinned read-only (no double-authority); O(active-set²) edge bound; `ActiveLayers` bit-stability + `ColdSummary` retention. G1 cites all three passes. G2/G3 remain open. |
| 0.5 | 2026-06-21 | — | PASS-4 fix pass (1M+3L resolved): T-LW-U-035 verifies read-only `PlayerEdge`; FR-LW-027/KD-9 no-write-back extended to vol-2 §2.1; tests ≥74. G1 cites all four passes. G2/G3 remain open. |
| 0.6 | 2026-06-21 | — | PASS-5 fix pass (1M+2L resolved): FR-LW-016 scoped — durable `SpawnCause` on arcs, interaction provenance implicit (no interaction record type); §3.1 decay example corrected to geometric ~0.016; §8.2 fatigue row marked n/a. G1 cites all five passes. G2/G3 remain open. |
| 0.7 | 2026-06-21 | — | PASS-6 fix pass (1M+2L resolved): RNG split into `world.arcs`/`world.text` sub-streams (periodic/aperiodic draw-interleaving hazard); inspector interaction-log marked determinism-neutral; §3.3 cites T-LW-DET-003. G1 cites all six passes. G2/G3 remain open. |
| 0.8 | 2026-06-21 | — | PASS-7 fix pass (1M+1L resolved): FR-LW-021 extended to selection/eviction with stable tiebreaks (episode salience → worldTick → episodeId; cold summary → EntityId); XC-022-008 sub-stream wording. G1 cites all seven passes. G2/G3 remain open. |
#endregion
