# W2 six-seed evidence pre-registration

**Date:** 2026-09-18  
**Status:** 1B-DESIGN PHASE 2 FROZEN — RESULT-BEARING RUN NOT YET OBSERVED  
**Production base:** `e8207f4c6f4d9d301872da869e3796163b8b26ad` (merged PR #416 mainline state)  
**Scope:** broader post-#416 W2 evidence only. The permanent PR gate remains the existing two adversarial seeds.

## Purpose

Revalidate production W2 over a broader six-seed corpus without expanding normal PR CI or reusing
the superseded pre-#416 W2 interpretation.

The evidence uses the permanent `MatchEngineInPossGateScenarios` final-third population, mirrored
phase test, run length, sample cadence, and `scenario.roster` squad recipe. The six seed identities
are frozen from the pre-existing six-seed same-population final-third diagnostic corpus. Only those
identities are reused; no close-chance metric, ranking, threshold, or conclusion is imported.

## Frozen corpus and floors

Full match per seed: **324,000 ticks**.  
Sampling cadence: **every 6 ticks**.  
Possession criterion: **strictly > 0.70**, independently in home and away mirrored views.  
Non-vacuity rule: **`floor(0.80 × corrected_baseline_samples(seed))`**, independently per seed.

| Seed | Corrected baseline samples | Frozen floor |
|---|---:|---:|
| `0x0F1E2D3C4B5A6978` | 15,830 | 12,664 |
| `0x00000000D1A6D05E` | 18,909 | 15,127 |
| `0x5EED000000000003` | 15,671 | 12,536 |
| `0x5EED000000000004` | 15,550 | 12,440 |
| `0x00000000D1A6D05F` | 18,162 | 14,529 |
| `0x1A2B3C4D5E6F7081` | 16,423 | 13,138 |

The first and last seeds are the two permanent-gate adversarial seeds. Their baseline counts exactly
reproduce the permanent detector's previously frozen **15,830 / 16,423**, an independent alignment
check on this evidence driver's population and roster recipe. The other four seeds broaden evidence
only; they do not enter the shipped gate.

## Phase 1 — governing baseline evidence

Governing run: **`35389373818`**, attempt 1.  
Governing Phase-1 head: **`a1f105c9baf2205877fc6f9852331576daa97b6e`**.  
Aggregate artifact: **`w2-six-seed-baseline-aggregate-35389373818`**, artifact id **10565680830**.

The aggregate artifact contains:

- `baseline-counts-and-floors.tsv` — SHA-256
  `976248616ac69ada2e3ffbc6c3eb2693a305e677f0193fd59234bafb91b307c4`;
- `provenance.txt` — SHA-256
  `4f50f7edc7380d6806e4c78f14897b6f367983a07abc8897e858cd6fb807626e`;
- `SHA256SUMS`, verified against both files after download.

All six matrix jobs and the aggregate verifier completed successfully. Phase 1 emitted sample counts
only; no six-seed possession-share result was used to derive or alter these floors.

### Pre-result correction record

The first driver commit (`3afe6baf7eb2c832371df2d06436a8f32e02788f`) used a different
roster registration-site label. Although the RNG key does not include that label, the driver did not
literally match the permanent scenario recipe and run `35388265274` is superseded.

The first matrix validator at `656b9300a43530d598b7ddbe1a42dfdad074f652` rejected the detailed
console logger's duplicate echo of an otherwise identical `TestContext` row. The test itself passed.
The parser was corrected to accept repeated identical rows while still rejecting conflicting rows;
the aggregate seed-set check was also made order-independent. Neither correction altered a seed,
run length, sampling rule, production configuration, threshold, or the already-frozen 0.80 rule.

Only run `35389373818` supplies the six governing baseline counts above.

## Phase 2 — activation revalidation frozen before result

This section, the six numeric floors above, the result driver, and the result workflow are frozen in
the same commit **before** the result-bearing workflow executes.

### Production arm — decisive leg

Production W2 is evaluated with:

- no tackle-radius override;
- production `TackleContactRadiusM = LooseBallPickupRadiusM = 1.0 m` on the pinned base;
- durable relationship `TackleContactRadiusM > 0` and
  `TackleContactRadiusM <= LooseBallPickupRadiusM`.

For every seed independently:

- `samples < frozen_floor` → **INSUFFICIENT**;
- sufficiently populated and both mirrored shares strictly `> 0.70` → **PASS**;
- sufficiently populated but either mirrored share `<= 0.70` → **LOCALIZE**.

Production is broadly revalidated only if all six seeds are `PASS`. A non-PASS seed does not by
itself authorize disabling W2; it triggers the causal localization rule below.

### Disarmed causal-control arm

The exact same six seeds, full-match run length, sample cadence, and frozen floors also execute with
`TestOnly_ArmTackleChallenge(0f)`.

This is a measurement-only negative control. The driver additionally requires **zero resolved tackle
outcomes** in this arm, proving that the control is actually disarmed. Its possession classification
is diagnostic and does not independently decide the production W2 state.

### Failure classification

If a sufficiently populated production seed violates the `> 0.70` mirrored criterion:

1. use the trajectory/ablation methodology that exposed PR #416;
2. if W2 itself causes the collapse, activation revalidation fails;
3. if an independent engine defect causes it, fix that defect and rerun this unchanged
   preregistered corpus;
4. do not lower the share threshold, sample floors, run length, or corpus after observing results.

A seed below its floor is insufficient evidence and likewise does not authorize post-hoc threshold
or corpus changes.

## Result evidence contract

The twelve result legs (six production + six disarmed) execute independently. Each retains:

- exact production/base/baseline/result SHAs and run ids;
- detailed console output;
- TRX;
- test exit status;
- parsed machine-readable result row;
- SHA-256 manifest.

An aggregate job runs even if a production predicate fails. It requires all twelve identity/floor
rows and retains:

- ordered `w2-six-seed-results.tsv`;
- `summary.txt` with `production_all_pass`, `localization_required`, and
  `disarmed_control_integrity`;
- aggregate provenance;
- SHA-256 manifest.

The aggregate is evidence. Workflow green/red alone is not the W2 interpretation.

## Sequencing boundary

This work blocks the later foul/card calibration pass. It does not block gate-integrity or Ball
Physics v2.10 characterization.
