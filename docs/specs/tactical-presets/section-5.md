# Tactical Presets & AI-Manager Selection Specification #26 — Section 5: Test Plan

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

## 5.1 Unit

| ID | Locks |
|---|---|
| T-TP-U-001 | Library invariants: APPEND-only ordinal order; `Players` length/null validation (F1) |
| T-TP-U-002 | Projection: team fills managed team only; missing `Players` ⇒ Identity rows (FM-TP-01) |
| T-TP-U-003 | Gate: interval arithmetic per §3.2 worked example; half-time fires independent of interval; kickoff fires once |
| T-TP-U-004 | Gate never fires off-stride or in Human mode (F5/FR-TP-007) |
| T-TP-U-005 | Kickoff scoring worked examples (Appendix B.1: Aggressive→Gegenpress, Pragmatic→Balanced); tie → lowest ordinal |
| T-TP-U-006 | Adaptation worked example (Appendix B.2: 0.624 steps, 0.234 holds) |
| T-TP-U-007 | Ladder saturation at both ends; one rung per decision |
| T-TP-U-008 | Hold: no switch evaluable while `HoldIntervalsRemaining > 0`; PatienceIntervals multiplier |
| T-TP-U-009 | `URGENCY_DIFF_CAP`: −3 goals scores as −2 |
| T-TP-U-010 | Profile NaN-gate (F4); affinity-row shape tests (bounded [−1,1], FR-TP-020) |
| T-TP-U-011 | Ordinal stability: `ManagerMode`, preset ordinals |
| T-TP-U-012 | Restore gates: out-of-range ordinals throw (F2) |

## 5.2 Integration

| ID | Locks |
|---|---|
| T-TP-I-001 | Boot path: kickoff selection reaches agents via the real appliers + stride commit (observed via `TestOnly_Mentality` etc.) |
| T-TP-I-002 | Mid-match path: adaptation applies via `SetTeamTactic` and commits at the stride boundary; the appliers have no post-kickoff call site (mechanical audit, F3) |
| T-TP-I-003 | Two AI managers (both teams) decide independently from their own state |
| T-TP-I-004 | AI vs Human: the Human team's tactics never change |
| T-TP-I-005 | Schema probe: `ManagerState` feeds the digest (at wiring) |

## 5.3 Determinism / closed-loop

| ID | Locks |
|---|---|
| T-TP-DET-001 | Two same-seed runs with both managers AI: bitwise-identical digests |
| T-TP-DET-002 | Default (Human/Human) run digest-identical to pre-#26 (FR-TP-007) |
| T-TP-DET-003 | Save/restore between two decision points resumes byte-identically (incl. hold + last-decision tick) |
| sim_manager-chases-deficit | #19 scenario: scripted 0–1 deficit; envelope asserts the Aggressive AI manager steps exactly one rung at the first eligible decision point and holds through the next (no churn), and the applied Mentality changes on-stride only |

## 5.4 FR traceability

Matrix in Appendix D, completed as tests land.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial plan: 12 unit, 5 integration, 3 determinism + 1 scenario. |
#endregion
