# W8 B implementation and October 13 anchor comparison

**Created:** October 15, 2026
**Purpose:** Record roadmap step 7 under the September 26 owner-approved W8 B contract.
**Status:** Implemented on this branch; final measurement and governing Unity validation pending.

## Contract and implementation

`MatchEngine.GoalkeeperDistribution.cs` produces all six #21 policies on the tactical heartbeat.
The selector uses the live eligible outfield roster, inclusive 25 m radius, deterministic distance /
width / roster-index tie-breaks and the shared own-goal/attack-direction fallback. LongKick is
receiverless by design. The approved delays, powers, delivery mapping and zero spin are unchanged.
New `[GT]` inputs use the existing config loader; no value was fitted to these matches.

The producer suppresses the holding keeper's Decision Tree dispatch, holds possession through #5
windup and gates every attempt against both inherited deadlines. Only accepted requests enter
`Distributing`. CONTACT revalidates the live receiver, launches through the canonical #5 adapter,
uses a typed normal-completion possession cause, arms the existing receiver latch when applicable,
and publishes `PassAttemptEvent` for W5. Resolve publishes `DistributionExecutedEvent` and settles
#11 exactly once. Loss/restart cancels; retained cancellation preserves the original hand clocks.
The old #11 local release/event approximation is removed. Snapshot body schema v26 adds the
accepted-request wait latch; v24 request/terminal storage is reused. Match save format remains v3.

The retained six-second ground-drop backstop is unchanged. Current-law hand timeout, the separate
feet-only guard and outside-area handling remain W8 C work. This branch does not claim C completion.

## Frozen measurement protocol

The comparison anchor is **October 13, `3974b78b0383adca3e9546bb47a7b93a62b3cf92`**, with evidence in
`evidence/pre-b-anchor-3974b78-20261013/`. The pre-wire bridge is `b60cc532` (includes #505). All arms
use `new MatchEngine(seed)`, `EnableGkHeading()`, no squad override and all six preregistered seeds
for 324,000 ticks each, on Linux .NET Debug. The common W8 instrument is the exact frozen blob
`520ba31c71e06ed408fd5c282b5fd5611cf74879`, including the same/opponent-keeper reclaim split.
The unchanged source-complete foul diagnostic is run separately on B.

Before interpreting B, reproduce the archived anchor on this host and verify the bridge's observed
outcome counters against it. Runtime versions are recorded rather than assumed interchangeable.
Terminal digests are fingerprints across schema v24/v25/v26, not cross-schema equality criteria.

The supplemental B diagnostic is frozen before its result-bearing run: it records every commit,
CONTACT, cancellation/rejection, executed event, canonical release, W5 receiver/zone count, F-05,
hand duration, guard margin, discipline and per-tick stalls. It applies the existing preregistered
structural falsifiers without changing their thresholds. The common instrument retains per-keeper
exposure, end reasons, pass outcomes and the self-reclaim confound; all six frozen matches are kept.
Only SlowDown is exercised by this corpus. Separate composed LongKick fixtures at both ends supply
landing evidence and are explicitly excluded from frozen-corpus totals.

For landing evidence, observe the first settled tick after CONTACT with `Bouncing` state or ball
height at/below the physical radius. Report its XY position, requested and actual radial distance,
and signed radial shortfall (negative means overshoot). End earlier with an explicit reason if a
kick, hand claim, restart or controlled acquisition intervenes; otherwise censor at the run end.
A receiverless launch is a valid zone execution, with no receiver-completion denominator or ghost
receiver latch. These observations describe the existing trajectory; they do not tune it or certify
realistic punt length.

## Validation record

- 24 composed all-policy / both-end / receiver-or-zone locks fail on `b60cc532` with the same intended
  reason: the approved request never reaches #5 (execution mode remains ordinary). The scratch copy
  removes only references to the newly added latch and analytical maximum-delay constant so it builds
  against pre-B APIs; the selected policy test body is unchanged.
- 144 targeted Linux tests pass, including policy mapping, zero RNG stream/cursor movement, removed
  receivers, real W5 feed, canonical mid-windup restore, exactly-once F-05, retained cancellation,
  repeated rejection, deadline cancellation, snapshot schema and possession-writer inventory.
- Full policy gate, final frozen comparisons and Unity 6000.4.9f1 compile: pending.

## Results

Pending final runs. No outcome or realism claim is made from an incomplete run.

## Version history

| Version | Date | Change |
|---|---|---|
| 1.0 | October 15, 2026 | Implementation and measurement protocol recorded before final result-bearing B runs. |
