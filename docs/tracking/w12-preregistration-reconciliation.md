# W12 Post-W6 Pre-registration Reconciliation

**Created:** September 15, 2026
**Status:** PRE-MEASUREMENT RECONCILIATION — historical pre-registrations remain unchanged
**Scope:** W6 contribution to the W12 gate-firing rejection wall only. This document does not govern W2 activation.

## Why this record exists

Two pre-registered documents on `main` assign different support thresholds to the same post-W6 W12 `InvariantRejected` metric and baseline:

- `docs/tracking/w6-controlled-ball-preregistration.md`, created on `wiring/w12-evidence-repair` by commit `add310c94adbe0b4f452dfa5ec7108df1a63eab8` at 2026-09-14T22:19:39Z, requires `InvariantRejected <= 79.845%` — a reduction of at least 5.0 percentage points from the 84.845% baseline — together with its suppression-mass and compensating-bucket conditions.
- `docs/tracking/w6-post-wiring-measurement-preregistration.md`, created 12 minutes later by commit `735c68962a98337cd8cddd76dd175da54212ceed` at 2026-09-14T22:31:57Z, defines P-W6-2 support at `InvariantRejected <= 82.8447%` — a reduction of at least 2.0 percentage points from the same baseline.

The later file does not explicitly supersede the earlier file, and the W6 closeout subsequently carried `w6-controlled-ball-preregistration.md` forward unchanged and explicitly described its locked thresholds and falsifier as still pre-registered. The two historical files therefore remain evidence of what was written before measurement; neither is edited retroactively here.

## Governing interpretation before measurement

No new hypothesis name or post-hoc threshold is introduced.

For any claim that **W6 materially causes or materially reduces the W12 rejection wall**, the governing support bar is the stricter earlier pre-registration:

1. `InvariantRejected <= 79.845%` — at least a 5.0 percentage-point reduction from the 84.845% baseline;
2. combined suppression mass satisfies the threshold in `w6-controlled-ball-preregistration.md` (at least a 3.0 percentage-point reduction from its locked baseline);
3. Active share does not decrease from the locked 0.085% baseline; and
4. no compensating exit-bucket increase violates that document's locked bounds.

This choice is made **before** the canonical post-W6 W12 result is observed. It is conservative because ambiguous supersession between two preregistered support bands cannot be resolved after seeing data without creating a threshold-shopping risk.

The 2.0 pp P-W6-2 band remains part of the historical record. Its result must be reported alongside the stricter test as **the looser of two conflicting preregistered support bands**. Passing it alone is not sufficient to support a material-causation claim.

## Falsifiers and mixed outcomes

The two historical documents also contain different falsifier bands. The canonical measurement record must therefore report each document's result independently rather than collapsing them into one post-hoc pass/fail label.

If the strict material-effect conditions fail, the repository must not claim that W6 materially caused or removed the W12 rejection wall even if the looser 2.0 pp support band passes. Conversely, a result outside one document's falsifier band does not erase the other document's independently pre-registered falsifier.

## Measurement boundary

The canonical measurement remains the existing W12 gate-firing lane and its governed census/accounting fields. The armed/disarmed W2 `sim_match_engine_inposs_gate` evidence is a separate measurement and cannot be substituted for W12 exit-distribution evidence.

This reconciliation must land before the canonical post-W6 W12 measurement is interpreted.