#!/usr/bin/env python3
"""Apply the preserved PR #416 narrow elevated-Rolling counterfactual.

This is an evidence-only workspace patch reconstructed from
bb501a2128f9efbef5e98bffadb0d98214493e78 onto the current Step-3.3 baseline.
It intentionally edits exactly two behavioral blocks.
"""

from pathlib import Path


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"{path}: expected one counterfactual anchor, found {count}")
    path.write_text(text.replace(old, new), encoding="utf-8")


core = Path("src/ball-physics/BallPhysicsCore.cs")
replace_once(
    core,
    """            // ERR-001-006: choose the force model from a physically valid height/state pair.
            // This also recovers legacy/restored state that already contains the invalid combination.
            if ((ball.State == BallStateType.Stationary || ball.State == BallStateType.Rolling)
                && ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
            {
                ball.State = BallStateType.Airborne;
            }
""",
    """            // COUNTERFACTUAL ONLY — reconstructed from preserved bb501a2.
            // Recover states that would otherwise disable gravity indefinitely, while preserving
            // moving elevated Rolling trajectories.
            bool elevated = ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold;
            bool rollingWouldStop = ball.State == BallStateType.Rolling
                                 && ball.Velocity.magnitude < BallPhysicsConstants.State.MinVelocity;
            if (elevated && (ball.State == BallStateType.Stationary || rollingWouldStop))
            {
                ball.State = BallStateType.Airborne;
            }
""",
)

machine = Path("src/ball-physics/BallStateMachine.cs")
replace_once(
    machine,
    """                case BallStateType.Rolling:
                    // Height wins over speed. The old order could turn a slow elevated Rolling
                    // ball into Stationary before noticing that it was airborne.
                    if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
                        return BallStateType.Airborne;
                    if (ball.Velocity.magnitude < BallPhysicsConstants.State.MinVelocity)
                        return BallStateType.Stationary;
                    if (IsOutOfBounds(ball.Position))
                        return BallStateType.OutOfPlay;
                    return BallStateType.Rolling;
""",
    """                case BallStateType.Rolling:
                    // COUNTERFACTUAL ONLY — reconstructed from preserved bb501a2.
                    // Altitude constrains the stop transition rather than every moving Rolling state.
                    if (ball.Velocity.magnitude < BallPhysicsConstants.State.MinVelocity)
                    {
                        return ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold
                            ? BallStateType.Airborne
                            : BallStateType.Stationary;
                    }
                    if (IsOutOfBounds(ball.Position))
                        return BallStateType.OutOfPlay;
                    return BallStateType.Rolling;
""",
)
