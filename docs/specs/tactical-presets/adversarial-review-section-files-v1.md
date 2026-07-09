# Tactical Presets & AI-Manager Selection Specification #26 — Section-Files PASS-1 Adversarial Review

**Created:** July 8, 2026
**Reviewed set:** all 11 section files at v0.1
**Findings:** 0 High / 1 Medium / 2 Low — all resolved in the v0.2 fix pass, same day.
**Method:** numeric re-derivation of every Appendix B example and Appendix E sensitivity claim;
engine-substrate verification for every match-state input §3 consumes.

---

## M-1 — §3.2/§3.4 consume engine state that does not exist, with no recorded gate

The spec's own KD-2 verified there is no goal producer — but then §3.4 consumes `goalDiff`, §3.2
consumes a half-time boundary, and the `t01` formula divides by `MATCH_TICKS_TOTAL`, none of which
the engine has today: there is **no score state** (nothing can ever make `goalDiff ≠ 0`), **no
halves model**, and **no `MATCH_TICKS_TOTAL` constant** (absent from the §3.5 table entirely —
an untagged phantom). As written, T4 would ship as permanently-dead code against an eternal 0–0.

**Resolution (v0.2):** the same deferral honesty KD-2 applied to event triggers is now applied to
match state: §1.6's T2/T4 rows and §9.3 record explicit prerequisites — T2's half-time trigger and
every `goalDiff ≠ 0` path of T4 gate on the engine gaining score/halves state (expected alongside
goal-detection, §7.2's named first producer); until then T2 fires kickoff + interval only and T4's
ladder is exercised via test seams. `MATCH_TICKS_TOTAL` added to §3.5 as an engine-owned
`[CROSS-PENDING]` row citing this gate. FR-TP-006/019 carry the caveat.

## L-1 — Appendix E sensitivity derivations were wrong

Re-derived: the Aggressive archetype (0.8) crosses `ADAPT_STEP_THRESHOLD` 0.35 in a one-goal
deficit at `(1−t01) ≥ 0.4375`, i.e. from ~39.4 match-minutes — not "~35′". The Pragmatic archetype
(0.3) at a **two**-goal deficit crosses at `(1−t01) ≥ 0.5833`, i.e. from ~52.5′ — not "~85′" (the
one-goal "never" claim was correct). Appendix E corrected to the derived values.

## L-2 — A.1 enum-member pinning had no checklist gate

A.1's "Tempo fast rows / slow rows" phrasing defers exact member names to T0 but §9 carried no
gate ensuring they get pinned. Explicit unchecked §9.1 item added (pin A.1 member names against
the #21 enums at T0 latest, before any catalogue code lands).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-07-08 | — | PASS-1 filed and resolved (0H+1M+2L) — v0.2 fix pass same day. |
#endregion
