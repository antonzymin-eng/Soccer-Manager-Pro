# W2 post-W6 paired evidence

**Captured:** September 16, 2026  
**Measurement run:** GitHub Actions `35096793576`  
**Measurement-driver head:** `682e7af9ed8cf5eed64d78defe7358e9e7bd81a6`  
**Production head under test:** `e4335f7ff059deb483b1aaa534f2190fd3762008` (merged W6 / PR #412)  
**Status:** paired control validated; production remained shipping-disabled during measurement.

## Purpose

This archive closes the W2 post-W6 control obligation before any production activation. Both matrix legs use the same production head, the same two governed seeds, and the same reporting/assertion transform. The only intended behavioral difference is whether the test-only tackle arm seam is applied at `LooseBallPickupRadiusM` (currently 1.0 m).

The unchanged governed predicate is strict `> 0.70` for both mirrored views of every governed seed.

## Results

| Mode | Seed | Samples | Home share | Away share | Gate / test exit |
|---|---|---:|---:|---:|---|
| armed | `0x0F1E2D3C4B5A6978` | 14,751 | 0.983 | 0.983 | 0 / 0 |
| armed | `0x1A2B3C4D5E6F7081` | 14,203 | 0.985 | 0.985 | 0 / 0 |
| disarmed | `0x0F1E2D3C4B5A6978` | 22,413 | 0.530 | 0.530 | 1 / 1 |
| disarmed | `0x1A2B3C4D5E6F7081` | 14,507 | 0.979 | 0.979 | 1 / 1 |

The armed leg satisfies all four strict bounds. The disarmed negative control does not: seed `0x0F1E2D3C4B5A6978` is 0.530 in both mirrored views, and both the gate and direct test return non-zero. The corrected workflow treats that expected negative-control result as valid evidence while still failing if the control unexpectedly satisfies the armed predicate.

This supports the narrow activation conclusion: on exact post-W6 production head `e4335f7f`, arming W2 at the intended current reach removes the preregistered InPoss blocker. It is **not** tackle-outcome calibration and does not establish a W12 material-effect claim.

## Immutable archives

The exact Actions artifacts are checked in beside this file:

- `w2-post-w6-armed-35096793576.zip` — SHA-256 `5772b980bb2378e4163c7063d0b7c6efde7b5b7704a66e9b0966aa42fc4dcb00`
- `w2-post-w6-disarmed-35096793576.zip` — SHA-256 `3b6dcd103aed546b03d4c524848fd4909c1b7fe3d16140559d0785fd02c5e9e9`

`artifact-SHA256SUMS` carries the same archive digests. Each ZIP includes provenance, the exact measurement-only patch, gate output, detailed instrument output, exit status, TRX, the parameterized driver, the workflow, and its own `SHA256SUMS`.

The parameterized driver and workflow are identical across both archives:

- `w2_post_w6_inposs_apply.py` — SHA-256 `a69d5f075d45d2c6515523bd37af02f474356a31b309325d2483f021e8871b03`
- `w2-post-w6-inposs-evidence.yml` — SHA-256 `7ffcf571bc3e447f447b054f3d3af2ec96d4c39d3c242c099852fd1667a2f9e9`

Additional internal evidence hashes are preserved inside each ZIP's `SHA256SUMS`.

## Governance boundaries

- Production `TackleContactRadiusM` was still `0` for this measurement; only the armed leg used the test seam.
- The current activation value may be 1.0 m because that is the current `LooseBallPickupRadiusM` fallback. The durable correctness rule is `TackleContactRadiusM > 0` and `TackleContactRadiusM <= LooseBallPickupRadiusM`; 1.0 m is not an eternal invariant.
- The existing ten tackle-outcome `[GT]` constants remain explicitly **uncalibrated**. W2 activation accepts that technical debt unchanged; foul/card and tackle-outcome calibration follow activation.
- W2 receives **no T-DA-DET-005 credit**. That deferred #14 test requires `DefensiveAITick`'s own `DeterministicRngService` path; W2's keyed draw in `MatchEngine` does not satisfy that obligation.
