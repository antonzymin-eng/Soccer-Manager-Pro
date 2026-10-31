# W2 evidence index

## September 18, 2026 — post-#416 six-seed revalidation

The broader Step-3.2 corpus is archived at
`docs/tracking/evidence/w2-six-seed/README.md`.

Its conclusion is deliberately narrow: all six production seeds avoid the historical
settled-possession collapse on post-#416 behavior, but the disarmed control is also healthy, so the
corpus does **not** establish W2 efficacy or tackle-outcome calibration. Production W2 resolves
25–37 challenges per full match with 0–3 clean wins and 0–2 tackle-outcome fouls; the existing
outcome `[GT]`s remain uncalibrated.

---

# W2 post-W6 paired evidence

**Captured:** September 16, 2026  
**Measurement run:** GitHub Actions `35096793576`  
**Measurement-driver head:** `682e7af9ed8cf5eed64d78defe7358e9e7bd81a6`  
**Production head under test:** `e4335f7ff059deb483b1aaa534f2190fd3762008` (merged W6 / PR #412)  
**Status:** historical paired-control artifact preserved; the `0.530 / 0.530` disarmed-control interpretation is superseded by `ERR-001-006` / PR #416.

## September 16, 2026 supersession note

This archive is immutable historical evidence and its measured values remain exactly as captured. However, PR #416 later localized seed `0x0F1E2D3C4B5A6978`'s disarmed **0.530 / 0.530** result to a persistent elevated-ball `Stationary` deadlock exposed by W6. It therefore was **not** a valid behavioral expectation for a healthy disarmed-W2 engine.

The original interpretation below is retained to preserve chronology, but is superseded for current reasoning. None of these pre-#418 sample counts may be reused as the governing non-vacuity baseline for the W2-active engine. PR #416 preregisters a fresh per-seed baseline/floor derivation on the reconciled production behavior.

## Purpose — historical intent at capture time

This archive closed the then-required W2 post-W6 paired-control obligation before production activation. Both matrix legs used the same production head, the same two governed seeds, and the same reporting/assertion transform. The only intended behavioral difference was whether the test-only tackle arm seam was applied at `LooseBallPickupRadiusM` (then 1.0 m).

The governed possession predicate was strict `> 0.70` for both mirrored views of every governed seed.

## Results

| Mode | Seed | Samples | Home share | Away share | Gate / test exit |
|---|---|---:|---:|---:|---|
| armed | `0x0F1E2D3C4B5A6978` | 14,751 | 0.983 | 0.983 | 0 / 0 |
| armed | `0x1A2B3C4D5E6F7081` | 14,203 | 0.985 | 0.985 | 0 / 0 |
| disarmed | `0x0F1E2D3C4B5A6978` | 22,413 | 0.530 | 0.530 | 1 / 1 |
| disarmed | `0x1A2B3C4D5E6F7081` | 14,507 | 0.979 | 0.979 | 1 / 1 |

**Historical interpretation at capture time — SUPERSEDED by PR #416:** the armed leg satisfied all four strict bounds; seed `0x0F1E2D3C4B5A6978` at 0.530 in both mirrored views was treated as an expected disarmed negative control, and the workflow treated that negative-control failure as valid evidence. PR #416 demonstrates that the 0.530 result was instead caused by `ERR-001-006`, an elevated-ball deadlock. The raw measurement remains valid; that causal interpretation does not.

The narrow activation conclusion recorded at the time was that, on exact post-W6 production head `e4335f7f`, arming W2 at the intended reach removed the observed InPoss blocker. That historical conclusion did not calibrate tackle outcomes or establish a W12 material-effect claim. Current W2-active evidence must be evaluated on the post-#418 engine after the Ball Physics correction.

## Immutable archives

The exact Actions artifacts are checked in beside this file:

- `w2-post-w6-armed-35096793576.zip` — SHA-256 `5772b980bb2378e4163c7063d0b7c6efde7b5b7704a66e9b0966aa42fc4dcb00`
- `w2-post-w6-disarmed-35096793576.zip` — SHA-256 `3b6dcd103aed546b03d4c524848fd4909c1b7fe3d16140559d0785fd02c5e9e9`

`artifact-SHA256SUMS` carries the same archive digests. Each ZIP includes provenance, the exact measurement-only patch, gate output, detailed instrument output, exit status, TRX, the parameterized driver, the workflow, and its own `SHA256SUMS`.

The parameterized driver and workflow are identical across both archives:

- `w2_post_w6_inposs_apply.py` — SHA-256 `a69d5f075d45d2c6515523bd37af02f474356a31b309325d2483f021e8871b03`
- `w2-post-w6-inposs-evidence.yml` — SHA-256 `7ffcf571bc3e447f447b054f3d3af2ec96d4c39d3c242c099852fd1667a2f9e9`

Additional internal evidence hashes are preserved inside each ZIP's `SHA256SUMS`.

## Governance boundaries at capture time

- Production `TackleContactRadiusM` was still `0` for this measurement; only the armed leg used the test seam. Production has since been activated by #418; this bullet is historical context, not current state.
- The activation value may be 1.0 m because that is the current `LooseBallPickupRadiusM` fallback. The durable correctness rule is `TackleContactRadiusM > 0` and `TackleContactRadiusM <= LooseBallPickupRadiusM`; 1.0 m is not an eternal invariant.
- The existing ten tackle-outcome `[GT]` constants remain explicitly **uncalibrated**. Foul/card and tackle-outcome calibration remain separate work.
- W2 receives **no T-DA-DET-005 credit**. That deferred #14 test requires `DefensiveAITick`'s own `DeterministicRngService` path; W2's keyed draw in `MatchEngine` does not satisfy that obligation.