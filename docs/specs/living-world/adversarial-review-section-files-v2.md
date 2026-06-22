# Living World #22 — Section-File Adversarial Review PASS-2

**Created:** June 21, 2026
**Reviewer:** fresh-eyes pass over `docs/specs/living-world/` section files v0.2
**Result:** 3 M + 3 L (no High). All resolved in the v0.3 fix pass (same day).

> Two of the three M findings are consequences of **PASS-1 fixes** (the NaN sentinel from M-3, the
> identity FR from M-1) — the canonical reason a second pass exists.

---

## Medium

**M-1 — NaN sentinel (PASS-1 M-3 fix) breaks determinism and contradicts F6/§6.1.** §2.2.1 stores
inactive layers as sentinel `NaN`. But `NaN != NaN`, so it breaks the F5 bitwise round-trip equality
check (T-LW-FAIL-005) and the snapshot/DET digest (NaN has multiple bit patterns); and F6 / §6.1 assert
"all edge/layer values ∈ [0,1]" with no exclusion, so a legitimate sentinel trips the invariant. **Fix:**
replace the NaN sentinel with an explicit `ActiveLayers` bitmask; inactive layers hold a defined `0.0`
default excluded by mask. Propagate the active-layer qualifier to F6 and §6.1.

**M-2 — FR-LW-034 / DET-007 (PASS-1 M-1 fix) overclaims a full-payload identical digest.** Once
FR-LW-022 / §4.6 adds the living-world block to the canonical snapshot (a `SNAPSHOT_SCHEMA_VERSION`
bump), the **full payload digest necessarily differs** from a pre-living-world baseline. FR-LW-034 ("bit-
identical to the human-systems baseline") and T-LW-DET-007 cannot hold on the full payload — exactly the
subtlety #21 carved out (FR-TI-031 / DET-002: identity on the world-state subset, not the full payload).
**Fix:** assert additive identity on the **human-systems/world-state subset**, not the full snapshot.

**M-3 — Budget eviction is episode-only but the budget is three-class.** §4.5 caps live edges + live
episodes + cold summaries together, but the eviction policy only evicts episodes (§3.2). Overflow
dominated by cold summaries has no relief path. **Fix:** add a cold-summary compaction/eviction path; note
live edges are bounded by the active-set cap (FR-LW-023) so they are not the unbounded class.

## Low

**L-1 — "bounded ring buffer" vs. "grow transiently."** §3.2 step 2 lets the buffer grow when all
episodes are pinned, contradicting FR-LW-008's "bounded." Clarify the transient growth is bounded by the
max simultaneous arc pins and capped by the §4.5 budget.

**L-2 — Per-arc `State` byte not in the FR-LW-028 stability list.** `Arc.State` is persisted and
"APPEND-only per kind" (§2.2.4) but FR-LW-028 enumerates only the four enums. Add per-arc state to the
ordinal-stability contract.

**L-3 — §4.2 human-systems daily-update sequencing.** Step 2 reads "the canonical human-systems state for
this tick" but does not say who runs the vol-2/vol-3 daily update or that it precedes step 2. Note it is
a prior season-loop phase owned outside this layer whose committed output step 2 consumes.
