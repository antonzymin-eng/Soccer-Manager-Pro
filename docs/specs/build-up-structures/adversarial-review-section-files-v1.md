# Scripted Build-Up Structures Specification #24 — Section-Files PASS-1 Adversarial Review

**Created:** July 8, 2026
**Reviewed set:** all 11 section files at v0.1
**Findings:** 0 High / 3 Medium / 2 Low — all resolved in the v0.2 fix pass, same day.
**Method:** formula re-derivation; event-payload verification against
`src/event-system/PossessionChangedEvent.cs`; lane-occupancy check against #12's formation-table
conventions.

---

## M-1 — Suppression window armed on every teammate reception

§3.3/FR-BU-006 v0.1 armed the post-regain window "on possession regained (possession-changed
signal, new possessor = this team)". Verified against the actual payload:
`PossessionChangedEvent` carries per-**agent** `PreviousHolder`/`NewHolder` entity ids and fires
on intra-team transfers too. As written, every completed pass inside the team re-armed the window
— under a `CounterAttack` plan the overlay would be suppressed for the entire possession, i.e.
the feature would never activate for exactly the managers who set an aggressive transition plan.

**Resolution (v0.2):** arming requires a **team-level regain** — the possessing *team* derived
from the holder ids transitions opponent → this team. Intra-team possessor changes and
opponent-side changes never arm; loose-ball (`−1`) interludes resolve on the settled outcome (the
window arms when this team settles possession that was last settled with the opponent). FR-BU-006,
§3.3, T-BU-I-004 updated; new T-BU-U-013 locks the intra-team no-op.

## M-2 — Zone-hysteresis rule ill-defined for non-adjacent jumps

"each boundary shifted H away from the committed zone" does not define which way the *far*
boundary shifts (committed OwnThird, long ball to x = 80 m). **Resolution:** reformulated as
committed-zone expansion — the committed zone's own interval expands by H at each of its
boundaries; any position outside the expanded interval classifies by the raw thresholds. The v0.1
worked example is unchanged under the new formulation; a long-ball example added.

## M-3 — Catalogue lane keys inconsistent with formation lane occupancy

A.1 keyed the fullback tuck to DEF-line **LH/RH** (half-space) lanes, but fullbacks occupy the
wide **L/R** lanes (A.3 keyed them correctly, exposing the inconsistency); and §3.2's worked
example cited a "MID-line central slot" in a 4-4-2, which has no C-lane midfielder.
**Resolution:** A.1 re-keyed (DEF L/R for the tuck; MID LH/RH for the drop — a lane-keyed table
cannot single out one pivot, recorded as a §7 deferral for slot-specific rows); §3.2 worked
example corrected.

## L-1 — Toward-centre sign unpinned at exactly y = 34

Pinned: lateral sign = +1 for y < 34, −1 for y > 34, **0 at y = 34** (a slot on the centreline
takes no lateral overlay displacement).

## L-2 — `CommittedZone` restore gate missing

F2 gated only `SuppressTicksRemaining`. Added byte-validity gate (`≤ FinalThird`) + test coverage
in T-BU-U-011.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-07-08 | — | PASS-1 filed and resolved (0H+3M+2L) — v0.2 fix pass same day. |
#endregion
