# W2 six-seed evidence pre-registration

**Date:** 2026-09-18  
**Status:** 1B-DESIGN PHASE 1 — BASELINE CAPTURE PRE-REGISTERED; SIX-SEED POSSESSION RESULT NOT YET OBSERVED  
**Production base:** `e8207f4c6f4d9d301872da869e3796163b8b26ad` (merged PR #416 mainline state)  
**Scope:** broader post-#416 W2 evidence only. The permanent PR gate remains the existing two adversarial seeds.

## Purpose

Revalidate the production W2 activation over a broader six-seed corpus without expanding normal PR CI or reusing the superseded pre-#416 W2 interpretation.

This evidence uses the same final-third population and squad-construction recipe as
`MatchEngineInPossGateScenarios`. The six seed identities are frozen from the pre-existing six-seed
same-population final-third diagnostic corpus. Only the seed identities are reused; no close-chance
metric, ranking, threshold, or conclusion is imported into this W2 evidence.

## Frozen corpus

Full match per seed: **324,000 ticks**.  
Sampling cadence: **every 6 ticks**.  
Population: samples where the ball is in either team's attacking final third, exactly as in
`MatchEngineInPossGateScenarios`.

| Seed |
|---|
| `0x0F1E2D3C4B5A6978` |
| `0x00000000D1A6D05E` |
| `0x5EED000000000003` |
| `0x5EED000000000004` |
| `0x00000000D1A6D05F` |
| `0x1A2B3C4D5E6F7081` |

The first and last seeds are the two permanent-gate adversarial seeds. The other four broaden
evidence only; they do not enter the shipped gate.

## Pre-result correction record

The first evidence-driver commit (`3afe6baf7eb2c832371df2d06436a8f32e02788f`) named its
roster RNG registration site `w2-six-seed-evidence.roster` rather than the permanent InPoss
scenario's `scenario.roster`. Although the current RNG key calculation does not include the site
label, that driver did not literally satisfy this preregistration's same-recipe requirement. Its
Actions run `35388265274` is therefore **superseded and must not supply baseline counts or floors**.
Commit `6adfd90e60cce00be6614f165392db4f12d62ecb` corrected the driver before any six-seed
possession-share result was observed.

## Phase 1 — corrected-baseline denominator capture

Before observing any six-seed possession-share result:

1. run all six seeds on the exact production base above with production W2 active;
2. emit **sample counts only** for each seed — the baseline driver deliberately does not print
   home/away possession shares;
3. derive one floor per seed as
   `floor(0.80 × corrected_baseline_samples(seed))`;
4. freeze those six numeric floors in a follow-up commit before enabling the result-bearing run.

The **0.80** fraction, mathematical floor rounding, full-match run length, sampling cadence, corpus,
and per-seed treatment are inherited unchanged from PR #416's pre-registered detector-hardening rule.

## Phase 2 — activation revalidation, frozen before result

After Phase 1 floors are frozen, the evidence run will evaluate every seed independently.

### Production arm

Production W2 is the decisive activation leg:

- no tackle-radius test override;
- current production fallback: `TackleContactRadiusM = LooseBallPickupRadiusM = 1.0 m` on this
  production base;
- durable production relationship: `TackleContactRadiusM > 0` and
  `TackleContactRadiusM <= LooseBallPickupRadiusM`.

For every seed independently:

- `samples < seed_floor` → **INSUFFICIENT EVIDENCE**;
- `samples >= seed_floor` and both mirrored shares are strictly `> 0.70` → **PASS**;
- `samples >= seed_floor` and either mirrored share is `<= 0.70` → **LOCALIZE CAUSE**.

Home and away mirrored views are separate predicates. Pooled values, if printed, are diagnostic only.

### Disarmed causal control

The same six seeds/run length may also run with the existing measurement seam
`TestOnly_ArmTackleChallenge(0f)`.

That is an explicit negative-control arm only. It does not mutate the production catalogue and does
not by itself decide whether W2 remains active. It exists to distinguish a W2-caused collapse from
an independent engine trajectory defect if localization is required.

## Failure classification

A single failing seed does **not** automatically prove W2 should be disabled.

If a sufficiently populated production seed violates the `> 0.70` mirrored criterion:

1. use the trajectory/ablation methodology that exposed PR #416;
2. if W2 itself causes the collapse, activation revalidation fails;
3. if an independent engine defect causes it, fix that defect and rerun this unchanged
   preregistered corpus;
4. do not lower the share threshold, sample floors, run length, or corpus after observing results.

## Evidence retention

Preserve for each phase/run:

- exact production base SHA and evidence-branch SHA;
- exact workflow and driver;
- TRX;
- complete detailed-console output;
- runner/test exit status;
- SHA-256 digests for retained evidence files.

The result-bearing run must be interpreted from retained artifacts, not workflow-green status alone.

## Sequencing boundary

This work blocks the later foul/card calibration pass. It does not block unrelated post-#416
workstreams, including gate-integrity or Ball Physics v2.10 characterization.
