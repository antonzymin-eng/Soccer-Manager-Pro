# Pre-C control protocol — analysis rule 1.1

Created November 4, 2026. Purpose: freeze the B control and the exact-touch/team
measurement used before W8 C. Version 1.1 corrects contact classification during
review, before any C capture. The original pre-run protocol, tools and outputs
are preserved byte for byte in `as-run-v1.tar.xz` and Git commit `7accb592`.

## Immutable capture inputs

`freeze.json` is the original as-run pin, not a hash manifest for the revised
portable wrappers. It records source `1d3cebf3b8d874715158067d9deb5fa48b309a30`,
`src` tree `65c8a8bfc533ac354a9a652a79cf6fb7cc94b2c8`, SDK 8.0.426, CLR 8.0.32,
Linux x64, Debug with optimization disabled and DEVELOPMENT_BUILD, NUnit 3.14.0
assertions and the repository Unity shim. No gameplay source or tuning changed.

The observed build adds only `observer-only.patch` and the unchanged frozen W8
observer blob `520ba31c71e06ed408fd5c282b5fd5611cf74879`. `Program.cs.txt` is the exact
capture runner. The plain build has no producer overlay or attached observer.
Each arm uses all six seeds in `expected.json`, 324,000 RunTick calls per seed,
`new MatchEngine(seed); EnableGkHeading()`, default squads/attributes, no config
binding or overrides, and shipping SlowDown asserted for both teams.

One seed runs per process; two processes ran concurrently with a two-CPU container
quota and DOTNET_PROCESSOR_COUNT=4. The EventBus is never shared across processes.
Selected JIT overrides are unset as recorded in the freeze. These are direct CLR
measurements, not Unity, canonical CI or performance/determinism certification.

Both arms retain every 32-byte snapshot digest, every BallState field plus
possessor, and the complete canonical Tier A/B ledger. All of these bytes are
now in the committed `capture-traces.tar.xz`, including both digest arrays.
Matching terminal summaries alone cannot pass the full analyzer.

## Physical-contact eligibility

The capture records producer order within each 60 Hz frame. The analysis uses
the explicit `APPLIED_TOUCH_KINDS` set in `analyze.py`:

- Applied collision-body responses, captured at successful OnAgentCollision,
  before its mask loses order. Mere collision overlaps do not count.
- FirstTouch before its applied result, including uncontrolled results; subsequent
  possession assignment is not a second touch.
- Loose-ball pickup and a won-tackle acquisition. The latter calls
  TakeControlledPossession and the engine's RecordPlayerBallTouch bookkeeping.
- Successful hand-contact gates. Claim/parry/deflect/spill consequences consume
  that contact and do not count it twice. Carry checks are not new contacts.
- Atomic pass/shot CONTACT and headers by any player. Classification follows an
  actual preceding hand contact and consumes it on a response or claim; it does
  not infer hand versus head from roster role.

**Correction from rule 1.0:** raw rows named `touch:tackle-loose` are retained as
historical diagnostic outcomes, but are not physical touches. That branch only
releases control and clears the flight latch: it neither applies ball contact nor
calls RecordPlayerBallTouch. No physical touch is inferred from dispossession.
There are 37 such rows. Rule 1.1 excludes them; `review.py` verifies that every one
of the 126 release decisions and all 12 selected windows is unchanged. No match,
raw observer, gameplay event, ball state or digest was changed or recaptured.

Unknown future `touch:` producers fail closed, requiring explicit classification.
Rejected hand attempts remain separate observations, not applied touches.
Restart-taker assignment is excluded. The frozen C policy is rule 1.1 in
`policy-freeze.json`; B and C must use this same eligibility set.

## Ordering, attribution and window rule

Callbacks and authoritative event publishes share a monotonically increasing
order within each frame. Publish copies are taken after the bus writes headers;
raw payload/tick/subsystem/draw identity is retained. Publish order and canonical
ledger order are distinct; every publish must match that ledger exactly once.

Release rows retain keeper/team and positions. Touches retain toucher/team,
position and producer kind. Restriction clearing and restart teardown are distinct
from physical-contact chronology. Shot rows retain shooter/team; goal rows retain
ScoringTeam separately from credited scorer/team, plus scoring location and raw
identity. None of these copied values may be written into the match.

For each release, find the first subsequent **other-player applied touch**,
ignoring intervening own touches. A prior restart or recorded full-time transition
censors the search. A teammate-first release is excluded even if an opponent
touches later. Select an opponent-first touch if its delay is in `[0,507]` frames.

Follow-up events must be after the clearing touch in `(frame,order)` and at or
before `clearingFrame+600`, inclusive. Later same-frame events count; earlier ones
do not. A complete window contains all 600 following live physics frames; full
time and run-end truncation are explicit. Preserve all releases, decisions and
own touches, per-window memberships, overlaps and unique event identities, with
releasing-team/opponent splits. Same-frame clearings are retained under the same
rule; the two observed cases are explained and checked in `review.json`.

The historical rate denominator remains all 1,944,000 simulated frames / 600 =
3,240 nominal periods. Report the 1,943,994 live pre-full-time frames separately.
This selected small population supports no causal or balance conclusion.

## Acceptance and coverage limits

Full PASS requires six adopted terminal digests in both arms, full per-tick digest,
ball and ledger equality, complete publish reconciliation, successful captures,
and the frozen source/runtime/instrument/policy identities. `--skip-digests` is
explicit **PARTIAL** validation with `passed: null`, never a full control pass.

No header contact and no rejected hand-contact gate occurred in this corpus.
The classifier/selection unit checks cover those decisions, not their unexecuted
physical producer paths. C must retain the required composed boundary, rejection,
restart and replay checks; these ordinary matches cannot establish rare behavior.

W8 C implementation and its result-bearing paired comparison are separate work.
Preserve this B control and lane, explain the first gameplay divergence, and
report snapshot fingerprints separately across C's schema change. A new runtime
or machine needs reproduced B control or a fresh paired capture.

## Version history

| Version | Date | Change |
| --- | --- | --- |
| 1.1 | November 4, 2026 | Review correction before C: exclude abstract BallLoose outcome, fail closed on new touch kinds, enforce full-time censoring, retain complete digest evidence and freeze the analysis separately. All release decisions unchanged. |
| 1.0 | November 4, 2026 | Original pre-run capture protocol, preserved in as-run-v1.tar.xz; keeper/header classifier corrected before accepted captures. |
