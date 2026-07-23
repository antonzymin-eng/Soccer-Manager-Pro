# Localization & Accessibility #49 — Section 6: Performance

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file PASS-1 (1H+1M+1L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 6.1 Cost model

Localization is a **display-time transform**, not game-loop code (Code Standards #20 §3.5.2). It runs when
the UI needs a surface string — a menu opens, an inbox item renders, an interaction line is shown — never on
the 60 Hz physics or 10 Hz AI path. It is therefore **not** subject to the zero-allocation game-loop budget
(FR-CS-066); string allocation is expected and acceptable off the hot path.

Per call:
- `Resolve(key)` — one dictionary lookup + a null-coalesce fallback (KD-5). O(1).
- `Render(req)` — one `variantCount` lookup, one `ulong` modulo, one template lookup + fallback, one
  `Expand` (named-placeholder substitution over a bounded placeholder set), and an optional clause lookup +
  concat. O(placeholders) — the same order as today's `InteractionTextGenerator.Expand`.

There is no per-frame work: nothing is rendered speculatively; strings are produced on demand and are the
UI's to cache if it chooses.

## 6.2 No persistent sim state

#49 holds no persistent sim state and bumps no save format (FR-LC-017 / KD-7). The catalogue is a content
artifact loaded at boot; locale + a11y selections are client-local settings outside the determinism save
(FR-LC-018). There is nothing to serialize, so there is no serialization cost and no save-size impact.

## 6.3 Determinism cost: none

The transform draws from no RNG stream and advances no tick (FR-LC-005 / T-LC-DET-002). The one relevant
draw — #22's `world.text` reservation — stays sim-side and is unchanged by localization, so #49 adds **zero**
cost to the determinism-relevant path and cannot perturb a digest (KD-2).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial performance analysis: display-time transform (off the sim loops), O(1)/O(placeholders) per call, zero persistent state, zero determinism cost. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (1H+1M+1L; H-1 generic-core / per-producer boundary-adapter split, M-1 FR-LC-008a construction-time roster-coverage invariant, L-1 `{score}` derived) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
