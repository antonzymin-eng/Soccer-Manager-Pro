# Dismarking & Marker-Awareness AI Specification #23 — Section 6: Performance Budget

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

## 6.1 Cost model

Per 10 Hz heartbeat, per team (11 agents):

| Item | Cost | Notes |
|---|---|---|
| Dwell update + FM-DM-01 | O(V) per agent, V = `VisibleOpponentsCount` ≤ 11 | one linear scan of already-built `FilteredView`; no new perception work |
| §3.3 offset stage | O(1) per eligible agent | one sqrt (normalize) when active; zero when `Off`/floor-gated |
| §3.4 penalty | O(V) per PASS option | reuses the passer's `FilteredView` scan; only for generated PASS options |

Upper bound ≈ 11 × 11 + 11 × 11 × options ≈ low-thousands of float ops per team-tick — well inside
the #12 (≤0.10 ms class) and #8 existing per-tick budgets; no budget line item changes. Formal
ratification per #18 KD-2 (per-spec §6 authority) at implementation, with the FR-PO-052 gate
covering regressions.

## 6.2 Allocation

Zero per-tick heap allocation (dwell state is a pre-allocated per-agent array; evaluator is static;
no LINQ/closures), per #20 hot-path rules.

## 6.3 Off-state cost

With `DismarkIntensity.Off` the offset stage and penalty exit before any distance math (dial check
first), so the default-configuration cost is a branch per agent/option — preserving the
"default-neutral is also default-cheap" property of the July 7 cheap items.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial budget analysis. |
#endregion
