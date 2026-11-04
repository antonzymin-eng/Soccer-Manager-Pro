# Pre-C control protocol

Created November 4, 2026. Purpose: freeze the Linux B control and the supplementary
exact-touch/team instrument before W8 C. This protocol is written before the full
result-bearing captures; the later freeze manifest pins its bytes and all inputs.

The source is clean main `1d3cebf3b8d874715158067d9deb5fa48b309a30`, `src` tree
`65c8a8bfc533ac354a9a652a79cf6fb7cc94b2c8`. The unmodified arm is extracted from
`git archive`; the observed arm starts from the same archive and receives only
the retained scratch observation overlay. Shipping source is not patched.

Both executables run Debug, optimization disabled, DEVELOPMENT_BUILD, Linux x64,
.NET SDK 8.0.426 / CLR 8.0.32, NUnit assertions 3.14.0, the repository Unity shim,
and default JIT settings. Activation is `/workspace/.cloud-tools/activate.sh`;
`DOTNET_PROCESSOR_COUNT=4` is recorded, with a two-CPU container quota. Each seed
runs in its own process. There is no shared EventBus between processes and no
NUnit worker pool. These are direct CLR measurement executions, not VSTest,
repository policy checks, Unity tests or determinism/performance certification.

Population: six seeds in adopted order `0F1E2D3C4B5A6978`, `00000000D1A6D05E`,
`0000000000000001`, `00000000ABCDEF12`, `0000000099887766`, `000000005A5A5A5A`;
324,000 RunTick calls each; `new MatchEngine(seed)` then `EnableGkHeading()`;
default squads and attributes, no overrides or GameplayConfigHolder binding,
both teams' shipping SlowDown policy (asserted). No tuning or C implementation.
The adopted six expected v27 terminal digests come from
`docs/tracking/w8-b-implementation-results.md`, not a new reference run.

The observed arm also runs the exact frozen W8 Stage A observer blob
`520ba31c71e06ed408fd5c282b5fd5611cf74879`, using its original Census methods.
The new observer emits copies only. Callback counters, buffers and output are
outside canonical state. It does not invoke gameplay methods, consume RNG,
subscribe to gameplay events, or change gameplay/event payloads. Source patches
and binary identities are retained. No result may pass if only the two modified
arms agree: both terminal digests must also equal the adopted historical values.

Per tick, both arms retain all 32 digest bytes, all BallState floating-point
fields and state plus possessor, and the complete canonically ordered Tier A/B
ledger copy (count; ordinal, payload size, raw payload bytes per record).
Neutrality requires byte equality over every tick of all six complete matches,
not merely equal terminal digests. Any failure records the first differing tick.

Exact touch capture uses producer execution order, never end-of-frame control
or CollisionEvent overlap as a touch proxy:

- Applied collision body deflections are captured inside ProcessAgentBall only
  when OnAgentCollision returned true, in response order, before the mask loses
  that order. Mere overlaps are excluded. Contact and collision-snapshot agent
  positions are retained.
- FirstTouch is captured once before its applied result, including uncontrolled
  outcomes; its later possession assignment is not counted again.
- Loose pickup and modelled successful won/loose tackle outcomes are captured at
  their producer sites. Restart taker assignment is excluded.
- Successful hand-contact adapter gates are captured once, before claim/parry/
  spill consequences. Continuous carry geometry checks are not new touches.
  Rejected hand attempts are retained separately and are not applied touches.
- Pass/shot atomic CONTACTs and headers by any player are captured at the ball adapter.
  Keeper parry/deflect/spill ApplyKick is a response to the already captured hand
  contact and is retained separately, without double counting the touch. Response
  classification consumes a preceding successful hand contact on the same frame
  and agent; a claim consumes it too. It never infers hand versus head from the
  player's goalkeeper role.

All callbacks and authoritative event publishes share a monotonically increasing
order within each physics frame. Event publish copies are taken after the bus
writes headers; raw payload, tick, subsystem and draw identity are retained.
Publish order is distinct from canonical ledger order, which is separately saved.
Shot rows record shooter/team and world contact location. Goal rows record
ScoringTeam and credited scorer/team separately, with ball location. Release
rows retain keeper/team/position and current restriction. Restriction clearing
and restart teardown rows are distinct from the physical touch chronology.
Read-only copied values must never be written back into the match.

Window rule (same code for B and future C): for each actual hand release, find
the first subsequent **other-player applied touch**, ignoring intervening own
touches. A preceding restart or match end censors the search. A teammate as the
first other toucher excludes the release even if an opponent touches later.
Select if that first other toucher is an opponent and the frame difference is
in `[0,507]`. Preserve all releases, clearings, rejected and censored cases.
Follow-up events must be after the clearing touch in `(frame,order)` and no later
than `clearingFrame+600`, inclusive. Thus later same-frame events count, earlier
same-frame events do not. A full window has all 600 following physics frames;
run-end truncation is explicit. Window membership may overlap; report per-window
and unique raw event identities and releasing-team/opponent splits separately.
Corpus goals per 600 frames use all `6*324000=1944000` simulated frames (3,240
nominal periods), matching the adopted historical denominator. No causal or
balance conclusion follows from this selected small population.

Acceptance: frozen source/runtime/configuration/instrument identities; successful
capture exits; six adopted digests reproduced in both arms; full digest, ball,
and event stream equality; frozen W8 output retained; exact-touch/team raw data
and rule-boundary checks retained. A baseline mismatch blocks C interpretation.
No historical fingerprint may be silently replaced. W8 C implementation, its
paired result and host validation are separate later work.

Preflight correction: the initial v1 capture attempt was stopped and retained
under `superseded-v1` after source review found that a goalkeeper could also head
the ball through the shared adapter. The v1 classifier used roster role. The
accepted classifier above uses the actual preceding contact, including claim
consumption. No v1 result enters this control. Gameplay source/configuration and
the selection rules did not change.
