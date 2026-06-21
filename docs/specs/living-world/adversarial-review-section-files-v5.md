# Living World #22 — Section-File Adversarial Review PASS-5

**Created:** June 21, 2026
**Reviewer:** fresh-eyes pass over `docs/specs/living-world/` section files v0.5
**Result:** 1 M + 2 L (no High). All resolved in the v0.6 fix pass (same day).

> Despite the PASS-4 prediction of L-only churn, PASS-5 found a real FR-vs-data-model inconsistency
> (M-1) — worth the pass. The two L findings are a worked-example arithmetic error and an inapplicable
> invariant row.

---

## Medium

**M-1 — FR-LW-016 mandates interaction provenance the data model cannot hold.** FR-LW-016 / §3.6 / KD-8
require "every arc **and every generated interaction**" to record a `SpawnCause` "at creation (§3.4 step
1)." But §2.2 has **no interaction record type** — only `Arc` carries `Cause`, and §3.4 step 1 is
arc-spawn only. A generated interaction is a transient string (§3.3), with nowhere to persist a
`SpawnCause`. The contract is unsatisfiable as written. **Fix:** scope the durable `SpawnCause` to
**arcs**; a generated interaction's provenance is **implicit** — it is a deterministic function of
`(InteractionIntent, RNG cursor, snapshotRef)` and is reconstructable from the snapshot + cursor, so no
separate persisted record is required (an optional inspector interaction-log may store that lightweight
tuple). Reword FR-LW-016 and §3.6.

## Low

**L-1 — §3.1 decay worked example uses the linear approximation.** "Over 30 idle days … relaxes ~0.018"
is the linear estimate (`0.01·30·0.06`); the stated **geometric** recurrence `x' = x + r(b−x)` gives
`0.06·(0.99)^30 ≈ 0.044` retained, i.e. **~0.016** closed toward baseline. **Fix:** correct to ~0.016 and
show the geometric form.

**L-2 — §8.2 fatigue invariant row is inapplicable.** The row claims fatigue is "consumed from vol-2
H-Gate," but the H-Gate models *happiness* (`Narrative_Stress`/`Mental_Noise`/…), not fatigue; this layer
consumes no fatigue value. **Fix:** mark the row n/a (listed for completeness) rather than implying a
consumed dependency.
