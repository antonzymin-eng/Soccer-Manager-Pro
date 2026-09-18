# Ball Physics v2.10 elevated-Rolling characterization

**Captured:** September 18, 2026  
**Production baseline:** `33cf81443e2c5ab43d7a1befcb22e6c0cfea7289`  
**Original governing characterization:** run `35395955770` at `e6223dbe26f9fabad3f5cce00e9f2984bcb7156d`  
**Supplementary transition census:** run `35396676058` at `38cb052a664fca3dae34d88009b0b0f775965721`  
**Counterfactual provenance:** preserved `evidence/pr416-narrow-rolling-candidate` head
`bb501a2128f9efbef5e98bffadb0d98214493e78`  
**Status:** Step 3.3 complete. The distributional debt is characterized; no v2.10 contract defect was found.

> The original preregistration and the supplementary transition-census preregistration remain
> frozen verbatim in their owning files. This README is post-result interpretation only.

## Why two result runs exist

The first successful result run (`35395955770`) correctly captured the frozen downstream metrics,
but its direct-frequency probe observed state only at the MatchEngine Physics boundary. v2.10 can
take a `Rolling` ball from at/below `AirborneEnterThreshold` to above the threshold during
integration and reclassify it to `Airborne` inside that same update. The next Physics boundary
therefore sees `Airborne`, not elevated `Rolling`.

The supplementary preregistration added six counters around the same
`BallPhysicsCore.UpdateBallPhysics` call to count that same-tick transition directly. It changed no
semantic arm, seed, run length, roster recipe, downstream metric or counterfactual.

After the supplementary run, all **12/12** original downstream rows were mechanically compared with
the first run after deleting only the six inserted census columns. Every row was byte-identical,
including all 12 trajectory fingerprints. The supplementary observation counters are therefore
observer-neutral for the previously frozen metrics.

The failed setup run `35395886287` is non-governing: its simulations emitted rows but a parser
expected 42 columns where the schema contained 41. No measurement definition changed in response.

## Direct transition frequency

On current v2.10, the original pre-Physics elevated-`Rolling` residency counter is **zero for every
seed**, because the broadened semantic removes the state within the same Physics tick.

The supplementary same-tick census resolves that ambiguity:

| Seed | v2.10 moving Rolling height-crosses | v2.10 → Airborne | narrow moving height-crosses | narrow → Rolling | narrow elevated-Rolling boundary ticks |
|---|---:|---:|---:|---:|---:|
| `0x0F1E2D3C4B5A6978` | 1,427 | 1,427 | 489 | 489 | 71,768 |
| `0x00000000D1A6D05E` | 1,302 | 1,302 | 498 | 498 | 68,452 |
| `0x5EED000000000003` | 1,395 | 1,395 | 450 | 450 | 63,211 |
| `0x5EED000000000004` | 1,456 | 1,456 | 494 | 494 | 68,983 |
| `0x00000000D1A6D05F` | 1,603 | 1,603 | 502 | 502 | 69,889 |
| `0x1A2B3C4D5E6F7081` | 1,355 | 1,355 | 450 | 450 | 61,053 |

Thus the broadened v2.10 surface is not rare: **1,302–1,603 moving height-cross transitions per
full match** in this six-seed population, and every one observed on the v2.10 arm is reclassified to
`Airborne` on that same update.

The narrow counterfactual does the opposite for its corresponding moving-cross population: every
one remains `Rolling`. It then accumulates **61,053–71,768 elevated-`Rolling` boundary ticks per
match** (about 17.0–19.9 match-minutes cumulatively). Those elevated-`Rolling` observations have
mean height about 0.47–0.51 m and mean speed about 3.50–3.67 m/s. This is exactly the distributional
surface v2.10 removed: moving elevated balls no longer continue under the ground-contact `Rolling`
force model.

The two arms diverge after the semantic difference, so their later event counts are not paired
counterfactual events. The frequency rows describe each deterministic arm's own resulting
population.

## Downstream trajectory effect

Every seed has a different one-second-sampled trajectory fingerprint between v2.10 and the narrow
counterfactual.

Relative to v2.10, the narrow arm shows:

- speed-integral distance proxy: **+19.625 to +437.397 m** per match;
- mean ball height: **+0.068679 to +0.092138 m**;
- mean ball speed: **+0.003635 to +0.080999 m/s**;
- possession-change count: **7 to 251 fewer** per match;
- total-goal delta: **−6 to +5**, with no stable direction.

These are deterministic resampling effects, not quality scores. The goal sign instability is
especially strong evidence against treating one arm as globally "better" from this characterization.

## Downstream action-selection effect

All six Decision Tree action vectors change. The direction is seed-dependent, but some counts move
substantially:

- PASS delta: −162 to +27;
- SHOOT: −8 to +9;
- DRIBBLE: −620 to +203;
- HOLD: −87 to +236;
- MOVE_TO_POSITION: −23,258 to +27,714;
- PRESS: −27,252 to +25,154;
- INTERCEPT: −1,277 to +840;
- SAVE: −41 to +194.

This establishes the open issue's requested downstream-action sensitivity. It does not attribute
those changes to a defect in Decision Tree, Positioning, Pressing, or any other downstream system:
once the ball trajectory changes, the deterministic match is a different trajectory.

## Decision

**Preserve Ball Physics v2.10.**

The evidence confirms the #416 ablation warning: elevated-`Rolling` ordering materially resamples
normal-play trajectories. It does not identify an actual v2.10 contract defect. Instead, the narrow
counterfactual produces substantial cumulative residency in an elevated `Rolling` state while the
`Rolling` ground-contact force model is active—the state/force pairing v2.10's height-first rule was
designed to prevent.

Therefore:

- no v2.11 rollback or narrowing is justified;
- no new Ball Physics ERR is opened;
- future trajectory calibration must treat v2.10 as the baseline rather than inheriting pre-v2.10
  deterministic trajectories;
- this evidence does not set gameplay tuning targets or acceptance bands.

## Evidence retention

Durable repository evidence:

- `all-results.tsv` — exact 12-row aggregate from supplementary run `35396676058`;
- `deltas.tsv` — mechanically derived v2.10→narrow comparison;
- `provenance.txt` — run/head/artifact identities;
- `SHA256SUMS` — hashes of the two durable TSVs.

The original successful aggregate artifact is `10567792073`; the supplementary aggregate artifact
is `10567717931`. Per-arm TRX/detailed logs remain GitHub Actions artifacts with the workflow's
90-day retention and are expected to expire around **2026-12-17**. The exact aggregate rows needed
for this conclusion are therefore committed here rather than relying on artifact retention.

The temporary MatchEngine counters/accessors, env-gated test, workspace counterfactual helper and
workflow are removed in the closeout commit. The two frozen evidence heads remain reproducible from
Git history.
