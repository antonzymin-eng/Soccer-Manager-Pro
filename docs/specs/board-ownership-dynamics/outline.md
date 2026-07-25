# Board & Ownership Dynamics #45 — Outline

**Created:** July 25, 2026
**Last Updated:** July 25, 2026 (v0.2 — section-file PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## Purpose

Spec #45 owns the **manager↔board relationship**: a persistent per-club board-confidence scalar, the
club's ownership profile, and — at the deep tier — takeover events. It is the spec that makes "the
board" a thing that *remembers*, rather than a pass/fail verdict evaluated once per season.

It is deliberately **not** the owner of the season objective (#30), the budget numbers (#40), the
sacking decision (#30), or the morale mechanics (#33). #45 supplies a value; other specs decide with it.

**Promoted from:** `docs/tracking/board-ownership-dynamics-design.md` v0.3 (AR-1 0H+4M+2L → AR-2
0H+0M+3L, CONVERGENCE).

## Section map

| § | Content |
|---|---|
| 1 | Scope, out-of-scope seams, dependencies + DAG, KD-1..KD-8, determinism posture |
| 2 | FR-BD-001..030, data structures, failure modes F1..F7 |
| 3 | FM-BD-01..04 — the daily drift, the target assembly, the `BoardModifier` projection, takeovers; worked examples |
| 4 | Assembly, file layout, the #30/#40 seams, save composition, reference contracts |
| 5 | Test plan — identity / units / determinism / save / boundary / fail-loud |
| 6 | Performance — world tick only, no hot path |
| 7 | T0–T3 plan, deferrals, the not-planned list, risks |
| 8 | Cross-references XC-045-001..016, back-prop table |
| 9 | Approval checklist + gates |
| A | Constant catalogue, save layout, band table |

## Key decisions (summary — full text in §1.5)

- **KD-1** — Board confidence is a **morale-model analogue**, not #33 state: #33's integer-per-mille
  drift shape, but club-scoped and #45-owned. #45 declares its own drift helper rather than taking a
  dependency on #33 for a three-operation function.
- **KD-2** — Takeovers are **deep-tier**, so `0x2D` / ordinal 95 stays **RESERVED, not promoted** at
  approval. When it promotes: **one** subsystem-wide stream + position-independent **keyed** action
  ordinals, so no cursor is ever persisted and #45 never contributes to the `MaxRngStreams` bound.
- **KD-3** — #45 supplies confidence; **#30 decides the sacking**. Strictly one-directional; #45
  exposes no sacking API and references #30 at no tier.
- **KD-4** — Ownership types are **dials on one code path**. `OwnershipProfile.Identity` is an explicit
  factory; `default(OwnershipProfile)` (×0) fails loud.
- **KD-5** — Reconciliation with #30: #30 keeps the **objective** and its evaluation; #45 owns the
  **confidence**; #30's `JobSecurity` becomes a **derived band** over that confidence (ERR-030-009).
- **KD-6** — Persistence is an opaque, independently version-gated sub-blob under #30's season save.
- **KD-7** — World-tick cadence at #30 slot **8**; carries a pinned **one-day-stale** board→morale
  contract (#33 sits at slot 3).
- **KD-8** — Behaviour-neutral identity: identity dials ⇒ #40's budget unchanged, no stream registered,
  every existing cursor byte-identical.

## Back-props (land atomically at APPROVED)

| ID | Target |
|---|---|
| ERR-030-008 | #30 tick order — board null seam as step 8; `AdvanceDay` → 9 |
| ERR-030-009 | #30 `BoardState.JobSecurity` → derived band (carries a `SEASON_STATE_FORMAT_VERSION` bump at T2) |
| ERR-045-001 | #16 §3.4 — `_RESERVED_0x2D_` + `SubsystemOrdinals.BoardOwnership = 95`, reserved not promoted |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-25 | — | Initial outline from supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-25 | — | PASS-1 fix (L): section map cited `XC-045-001..012`; §8 defines **001..016**. |
#endregion
