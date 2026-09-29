# W6 controlled-ball W12 pre-registration

**Status:** PRE-REGISTERED BEFORE W6 MERGE  
**Date:** September 14, 2026  
**Baseline source:** `docs/tracking/evidence/w12/w12-gate-firing-census.json`  
**Required post-W6 lane:** canonical `.github/workflows/measure.yml` W12 gate-firing measurement

## Purpose

W6 changes possession/ball-attachment semantics, so a post-W6 run is not expected to preserve the pre-W6 trajectory or absolute heartbeat counts. Evaluation therefore uses shares of the non-`InPossession` population and the full relevant exit distribution. The purpose is to test the specific hypothesis that logical/physical possession divergence materially contributes to the existing pressing rejection wall.

## Locked baseline

Non-`InPossession` population: **164,193 heartbeats**.

| Exit / outcome | Count | Share of non-`InPossession` |
|---|---:|---:|
| InvariantRejected | 139,309 | 84.845% |
| Cooldown (Pressing AI / `DisengageResolver`) | 22,671 | 13.808% |
| Active | 140 | 0.085% |
| NoPrimaryPresser | 84 | 0.051% |
| Disengaged | 1,890 | 1.151% |
| NoCommittedTrigger | 99 | 0.060% |
| Combined suppression mass¹ | 163,954 | 99.854% |

¹ `InvariantRejected + Cooldown + NoPrimaryPresser + Disengaged`. It is tracked so an apparent improvement cannot be declared merely because rejected heartbeats move into another suppression bucket.

## Pre-registered prediction

If W6's logical/physical possession divergence is a **material** cause of the rejection wall, the canonical post-W6 W12 run must satisfy all of the following:

1. `InvariantRejected / non-InPossession` falls by **at least 5.0 percentage points**, from 84.845% to **≤ 79.845%**.
2. Combined suppression mass falls by **at least 3.0 percentage points**, from 99.854% to **≤ 96.854%**.
3. `Active / non-InPossession` does **not decrease** from 0.085%. This is directional only; no minimum increase is pre-claimed.
4. No compensating bucket absorbs the result: versus baseline, `NoPrimaryPresser` may rise by **< 1.0 pp**, Pressing-AI `Cooldown` by **< 2.0 pp**, and `Disengaged` by **< 1.0 pp**.

The scorelines and absolute counts are observation fields only; they are not acceptance criteria because W6 is expected to change trajectory.

## Falsifier

The W6-causation hypothesis is **falsified for material effect in this corpus** if:

- W6 production controlled-ball wiring and its regression tests are green, and the canonical post-W6 measurement runs successfully; **and**
- `InvariantRejected` share changes by **less than 1.0 pp in magnitude** **and** combined suppression mass changes by **less than 1.0 pp in magnitude or increases**.

If falsified, do not reinterpret the run as partial confirmation; retain W6 as a correctness fix and investigate a different cause of the pressing rejection wall. Results between the falsifier band and the material-effect threshold are **inconclusive**, not success.

## Separate recovered cooldown defect

PR #412 has an unresolved Codex P2 at `src/match-engine/MatchEngine.cs`: the physical-carrier early return can occur before `_tackleCooldown` decrement, freezing **tackle cooldown** while the ball is loose/airborne or at a restart. That must be fixed before W6 merge. This is **not** the W12 Pressing-AI `Cooldown` bucket above, which is driven by `DisengageResolver` / `_cooldownTicks`; the two mechanisms must not be conflated.
