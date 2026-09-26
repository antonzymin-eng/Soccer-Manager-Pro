# W8 Stage A frozen six-seed baseline — recorded results

> **Created:** September 25, 2026  
> **Measured source:** `b06595fbd409396c3ebad445cecd2b215b8e9e2a` (PR #458 merge)  
> **Run:** `36212512965` — `W8 Stage A pinned baseline` — success  
> **Population:** frozen six seeds from `w8-stage-a-preregistration.md`; 324,000 frames per seed  
> **Status:** descriptive Stage A record only. No B numeric policy, delivery, timing, Law-12, or gameplay decision is inferred here.

The workflow branch was `measure/w8-stage-a-baseline`, but both measurement jobs checked out the pinned source commit above before running. The W8 job and unchanged `foul-rate` job both passed their test + sentinel verification.

## Evidence provenance

| Item | Value |
|---|---|
| Workflow run | `36212512965` |
| W8 job | `108321862991` |
| W8 artifact | `10896108016` — `w8-stage-a-w8-stage-a-36212512965` |
| W8 artifact ZIP SHA-256 | `2e94446fb02127b2b04d195706cf27658837fb95b903f18b061379198f1cdd2c` |
| W8 artifact size | 1,118,320 bytes |
| Foul-rate job | `108321863117` |
| Foul-rate artifact | `10896387320` — `w8-stage-a-foul-rate-36212512965` |
| Foul-rate artifact ZIP SHA-256 | `743548e6670de3627a71d849164c1ea8f99390f76a63e2d9f3c64b6541cb20db` |
| Foul-rate artifact size | 170,677 bytes |
| Artifact expiry | 2026-12-25 02:42:07Z |

## Per-seed results

| Seed | W8 hand claims | Keeper 0 | Keeper 11 | W3 successful claims | Delta | Old 360-frame drops | Reclaim <=300f | F/Y/R | Terminal digest |
|---|---:|---:|---:|---:|---:|---:|---:|---|---|
| `0x0F1E2D3C4B5A6978` | 101 | 29 | 72 | 91 | +10 | 0 | 0 | 4/1/0 | `6a0ef38092a424332483b0124d13429c9134c58eefdb421e7f4bbdee509cafc3` |
| `0x00000000D1A6D05E` | 224 | 154 | 70 | 196 | +28 | 0 | 0 | 5/1/0 | `c47c5d8fc993dfb0fd29164e33bc7770b4ee74857c1875c58f421f7ceec30d3e` |
| `0x0000000000000001` | 105 | 53 | 52 | 90 | +15 | 1 | 1 hand | 10/0/0 | `7f13a1adafac5b692c1757922f92d72eb12dc86137cfff7eb5ee415af358fe29` |
| `0x00000000ABCDEF12` | 136 | 25 | 111 | 121 | +15 | 0 | 0 | 5/2/0 | `0767c1fa25b77bd36168564944cae973b1a0c41899b9d84601203b57ad6dacb6` |
| `0x0000000099887766` | 148 | 44 | 104 | 130 | +18 | 0 | 0 | 10/1/0 | `802fa9a2d27319cd6c423730109604786c66b626f44e39f71ba9599574525008` |
| `0x000000005A5A5A5A` | 148 | 44 | 104 | 125 | +23 | 0 | 0 | 9/0/1 | `62d7ce6b7b0f1e9edd61271bdf6c899f3f1b2d5f8a4e9f1f0f2310fba5920088` |
| **Total** | **862** | **349** | **513** | **753** | **+109** | **1** | **1 hand** | **43/5/1** | — |

The W8 hand-claim population is therefore 109 above the earlier W3 `successfulKeeperClaims` population. This is recorded as the preregistered finding; it is not treated as a failed arm or a tuning signal.

The W8 Tier-A discipline census matches the unchanged source-complete `foul-rate` instrument **exactly per seed**: 43 fouls, 5 yellows, 1 straight red, 0 second-yellow dismissals in aggregate. The source-gap falsifier did not fire.

## Hand episodes

All 862 hand episodes closed in the run:

- 861 ended at canonical `PassKick` CONTACT.
- 1 ended at the engine's old 360-frame ground drop.
- No hand episode ended from a restart, takeover, other explicit release, replacement claim, or full-time censor.
- Hand hold duration: min 6 frames, max 359 frames, mean 32.086 frames.
- Same-keeper reclaim after the one old drop: 1 hand reclaim within the preregistered 300-frame window; 0 feet reclaims.

While a hand episode was live, keeper Decision Tree selections were: PASS 1,003; HOLD 2,036; DRIBBLE 12; SHOOT 0. The last selected action at episode end was PASS for 853 episodes, HOLD for 1, and none observed for 8. These selections are descriptive and are not assigned as the cause of release unless CONTACT establishes it.

The 861 hand-origin pass CONTACTs subsequently resolved as:

| Outcome | Count |
|---|---:|
| interrupted by a later hand claim | 501 |
| interrupted by another kick | 258 |
| other agent controlled | 89 |
| committed receiver controlled | 13 |

### Known A→B confound — keeper claim→pass→claim loop

The frozen corpus is dominated by a keeper claim/pass cycle and must not be read as a clean
distribution-policy baseline:

- 862 hand claims across six matches = 143.7 claims per match.
- 501/861 hand-origin passes (58.2%) were still pending when a later hand claim occurred.
- only 13/861 (1.5%) reached the committed receiver before the pending pass resolved.
- mean hand hold was 32.086 frames (~0.535 s at 60 Hz).
- 580 hand claims had `PassKick` as the immediately preceding observable touch.

A re-derivation from the preserved Stage A episode rows sharpens the last point: **576/580** of those
PassKick-preceded claims were the **same keeper reclaiming his own immediately preceding pass**,
**0/580** directly followed the opponent keeper's pass, and 4/580 followed an outfield team-mate's
pass. That direct-touch classification is not automatically the same population as the 501
`interrupted-by-claim` pass outcomes because a pending pass can survive intervening touches before
the claim resolves it.

Supplemental test-only run `36251412673` on the **exact pinned Stage A production tree** closes that
population directly:

- `pass-outcome-hand-interrupted-by-claim = 501`;
- `...-same-keeper = 501`;
- `...-opponent-keeper = 0`;
- all six frozen terminal digests reproduced exactly (6/6);
- artifact `10909441933`, `w8-stage-a-reclaim-split-36251412673`;
- artifact ZIP SHA-256
  `0e076292accbb43eb3b5bda7a0b27b9df37447764d9778f19055e9a707ad1ed6`;
- artifact size 1,118,448 bytes; expiry `2026-12-25T15:17:22Z`.

Therefore the measured 501-pass interruption population is **100% same-keeper self-reclaim and 0%
opponent-keeper reclaim**. This is the dominant Stage A confound that A→B must report separately
from the policy effect.

This loop is a **known comparison confound**, not a signal to tune B constants. A→B interpretation
must report it separately from the intended distribution-policy effect.

## Last touch before hand claim

| Preceding observable touch | Claims |
|---|---:|
| `PassKick` | 580 |
| `GkHeadingKick` | 177 |
| `first-touch-0` | 69 |
| `ShotKick` | 15 |
| `loose-pickup` | 13 |
| `first-touch-1` | 6 |
| `unattributed-collision-deflection` | 1 |
| preceding `hand-claim` | 1 |

The summary counter reported one unknown-last-touch claim because the unattributed collision deflection has no actor. Four claims followed a same-team touch; four were also deliberate-kick candidates under the preregistered narrow classification.

## Geometry and dry-run target selector

Keeper position at the 862 hand acquisitions: 842 inside own penalty area, 1 on the boundary, 19 outside. Ball contact position: 848 inside, 3 boundary, 11 outside. These are diagnostic classifications only; Stage A does not change claim legality or sanction behavior.

The dry selector found a receiver for 860/862 hand claims and used the fixed receiverless zone for 2/862. Across hand + feet acquisitions it found a receiver for 1,081/1,087 episodes and used the zone fallback for 6/1,087.

### Known B coverage gap — Stage A exercised only `SlowDown`

Every observed policy value was `SlowDown`. The frozen six-seed A→B corpus can therefore measure
only the SlowDown B path and **must not** be cited as execution coverage for `Quick`, `ShortKick`,
`LongKick`, `RollOut`, or `ThrowOut`. Before B merges, deterministic composed fixtures must
exercise all six policy rows. The exact receiver/zone fixture topology for each row is owned by the
**owner-approved B contract**; this Stage A evidence record does not pre-approve LongKick as
receiverless, or any other policy-specific receiver/fallback choice. The A→B corpus remains useful
as the frozen SlowDown comparison, not as six-policy coverage.

## Restart census

Applied restart cues across the six seeds: Corner 4; FreeKick 80; GoalKick 13; KickOff 47; ThrowIn 81. None was the first end cause of a hand episode.

## Fixed next step

The behavior-neutral possession helper has now landed in PR #460. Its final-head proof ran
`PossessionChangeSeamTests` + `MatchEngineSnapshotRestoreTests` 18/18 and reproduced all six
frozen terminal digests exactly. That evidence applies only to the assignment-only helper.

Before any B result-bearing measurement:

1. #462 is complete and merged as `a4056ea6371f6dcf72dcaa6ef51980f7bbeaabc7`: the writer guard
   now rejects `ldflda` address-taking as well as unlisted `stfld` stores, and the helper's omitted
   source history/changelog are recorded.
2. finish the spec-first B contract (#461), including the distance-sensitive #5 delivery formula,
   exact hash namespace, live CONTACT target semantics, typed possession-change completion/cancellation
   semantics, explicit guard arithmetic, and ERR-011-015/016/017; it remains draft pending owner
   approval of the W8 amendment.
3. wire B only after the owning specs are explicitly approved and merged;
4. prove deterministic composed execution of **all six** #21 policies;
5. rerun the frozen six seeds as the SlowDown A→B comparison, with the now-proven
   **501 same-keeper / 0 opponent-keeper** self-reclaim loop reported as a separate confound rather
   than folded into a single policy verdict.

The supplemental keeper-identity split and the possession-seam structural follow-up are complete;
neither remains a pending B-measurement gate.

Once B attaches hand-episode teardown, PR #460's digest equality is no longer neutrality evidence for
that new behavior. B must prove the new semantics directly.
