#!/usr/bin/env python3
from pathlib import Path


def once(text: str, old: str, new: str, label: str) -> str:
    n = text.count(old)
    if n != 1:
        raise SystemExit(f"{label}: expected one match, found {n}")
    return text.replace(old, new, 1)

# BallCollision: non-kick release and ApplyKick must never create Stationary above the airborne threshold.
p = Path('src/ball-physics/BallCollision.cs')
s = p.read_text(encoding='utf-8')
s = once(s,
'''// Modified: 2026-09-14 (W6: production Controlled entry + explicit non-kick release transition)''',
'''// Modified: 2026-09-14 (W6: production Controlled entry + explicit non-kick release transition)
// Modified: 2026-09-15 (ERR-001-006: elevated uncontrolled balls cannot become Stationary)''',
'BallCollision header')
s = once(s,
'''            ball.State             = BallStateType.Stationary;
            ball.Velocity          = Vector3.zero;''',
'''            // ERR-001-006: Stationary is a ground-rest state. An elevated controlled ball
            // released without a kick must fall under gravity rather than freeze in mid-air.
            ball.State = ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold
                ? BallStateType.Airborne
                : BallStateType.Stationary;
            ball.Velocity          = Vector3.zero;''',
'ReleaseBallControl state')
s = once(s,
'''            if (velocity.z > 0f)
                ball.State = BallStateType.Airborne;
            else if (horizontalSpeed > BallPhysicsConstants.State.MinVelocity)''',
'''            // ERR-001-006: height is authoritative for an already-elevated ball. A zero,
            // horizontal, or downward kick cannot turn an elevated ball into a force-free
            // Stationary/Rolling state; it must remain Airborne so gravity can act.
            if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold || velocity.z > 0f)
                ball.State = BallStateType.Airborne;
            else if (horizontalSpeed > BallPhysicsConstants.State.MinVelocity)''',
'ApplyKick state selection')
s = once(s,
'''// | 1.9     | 2026-09-14 | —      | W6: SetBallControlled refreshes its recovery checkpoint; new                |
// |         |            |        | ReleaseBallControl provides explicit non-kick Controlled -> Stationary      |
// |         |            |        | transition while possession identity remains host-owned (Option B).         |''',
'''// | 1.9     | 2026-09-14 | —      | W6: SetBallControlled refreshes its recovery checkpoint; new                |
// |         |            |        | ReleaseBallControl provides explicit non-kick Controlled release while      |
// |         |            |        | possession identity remains host-owned (Option B).                          |
// | 2.0     | 2026-09-15 | —      | ERR-001-006: ApplyKick and non-kick Controlled release preserve Airborne    |
// |         |            |        | state whenever the ball centre is above AirborneEnterThreshold, preventing  |
// |         |            |        | force-free elevated Stationary balls.                                       |''',
'BallCollision history')
p.write_text(s, encoding='utf-8', newline='\n')

# BallStateMachine: altitude must win over low speed; direct state queries agree with the invariant.
p = Path('src/ball-physics/BallStateMachine.cs')
s = p.read_text(encoding='utf-8')
s = once(s,
'''// Modified: 2026-07-27 (shot-outcome pass)''',
'''// Modified: 2026-07-27 (shot-outcome pass)
// Modified: 2026-09-15 (ERR-001-006: elevated Stationary/Rolling states normalize to Airborne)''',
'BallStateMachine header')
s = once(s,
'''                case BallStateType.Stationary:
                    // Transitions handled externally by kick/touch events.
                    return BallStateType.Stationary;

                case BallStateType.Rolling:
                    if (ball.Velocity.magnitude < BallPhysicsConstants.State.MinVelocity)
                        return BallStateType.Stationary;
                    if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
                        return BallStateType.Airborne;''',
'''                case BallStateType.Stationary:
                    // ERR-001-006: Stationary is valid only at ground-rest height. Recover an
                    // elevated state so gravity can act rather than leaving the ball force-free.
                    if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
                        return BallStateType.Airborne;
                    return BallStateType.Stationary;

                case BallStateType.Rolling:
                    // Height wins over speed. The old order could turn a slow elevated Rolling
                    // ball into Stationary before noticing that it was airborne.
                    if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
                        return BallStateType.Airborne;
                    if (ball.Velocity.magnitude < BallPhysicsConstants.State.MinVelocity)
                        return BallStateType.Stationary;''',
'BallStateMachine cases')
s = once(s,
'''// | 1.3     | 2026-07-27 | —      | ERR-001-004 (shot-outcome design KD-5): IsOutOfBounds drops the   |
// |         |            |        | z < Diameter gate in the same commit as CheckBoundaries — the two |
// |         |            |        | predicates are pinned to agree, and an airborne crossing is out.  |''',
'''// | 1.3     | 2026-07-27 | —      | ERR-001-004 (shot-outcome design KD-5): IsOutOfBounds drops the   |
// |         |            |        | z < Diameter gate in the same commit as CheckBoundaries — the two |
// |         |            |        | predicates are pinned to agree, and an airborne crossing is out.  |
// | 1.4     | 2026-09-15 | —      | ERR-001-006: elevated Stationary and Rolling states normalize to  |
// |         |            |        | Airborne; Rolling checks altitude before the low-speed stop rule.  |''',
'BallStateMachine history')
p.write_text(s, encoding='utf-8', newline='\n')

# BallPhysicsCore: normalize the invalid state before selecting the force model, so restored/legacy states self-heal.
p = Path('src/ball-physics/BallPhysicsCore.cs')
s = p.read_text(encoding='utf-8')
s = once(s,
'''// Modified: 2026-06-12''',
'''// Modified: 2026-06-12
// Modified: 2026-09-15 (ERR-001-006: normalize elevated ground states before force selection)''',
'BallPhysicsCore header')
s = once(s,
'''            // Bouncing: apply impulse first, then continue to integration.
            if (ball.State == BallStateType.Bouncing)
                BallGroundInteraction.ApplyBounce(ref ball, surface, logger, matchTime);

            Vector3 netForce''',
'''            // ERR-001-006: choose the force model from a physically valid height/state pair.
            // This also recovers legacy/restored state that already contains the invalid combination.
            if ((ball.State == BallStateType.Stationary || ball.State == BallStateType.Rolling)
                && ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
            {
                ball.State = BallStateType.Airborne;
            }

            // Bouncing: apply impulse first, then continue to integration.
            if (ball.State == BallStateType.Bouncing)
                BallGroundInteraction.ApplyBounce(ref ball, surface, logger, matchTime);

            Vector3 netForce''',
'BallPhysicsCore normalization')
p.write_text(s, encoding='utf-8', newline='\n')

# W6 control tests: replace the test that encoded the invalid state and add kick-height coverage.
p = Path('src/ball-physics/tests/BallControlStateTests.cs')
s = p.read_text(encoding='utf-8')
s = once(s,
'''// Modified: 2026-09-14''',
'''// Modified: 2026-09-14
// Modified: 2026-09-15 (ERR-001-006 elevated release/kick regression locks)''',
'BallControlStateTests header')
s = once(s,
'''        public void ReleaseBallControl_FromControlled_ReturnsStationaryWithoutMovingBall()
        {
            var position = new Vector3(12f, 8f, 1.4f);
            var ball = BallState.CreateAtPosition(position);
            BallCollision.SetBallControlled(ref ball);

            BallCollision.ReleaseBallControl(ref ball);

            Assert.AreEqual(BallStateType.Stationary, ball.State);
            Assert.AreEqual(position, ball.Position);
            Assert.AreEqual(Vector3.zero, ball.Velocity);
            Assert.AreEqual(Vector3.zero, ball.AngularVelocity);
            Assert.AreEqual(position, ball.LastValidPosition);
            Assert.AreEqual(Vector3.zero, ball.LastValidVelocity);
        }
''',
'''        public void ReleaseBallControl_FromElevatedControlledBall_TransitionsToAirborneWithoutTeleporting()
        {
            var position = new Vector3(12f, 8f, 1.4f);
            var ball = BallState.CreateAtPosition(position);
            BallCollision.SetBallControlled(ref ball);

            BallCollision.ReleaseBallControl(ref ball);

            Assert.AreEqual(BallStateType.Airborne, ball.State,
                "ERR-001-006: an elevated released ball must receive gravity rather than freeze Stationary");
            Assert.AreEqual(position, ball.Position);
            Assert.AreEqual(Vector3.zero, ball.Velocity);
            Assert.AreEqual(Vector3.zero, ball.AngularVelocity);
            Assert.AreEqual(position, ball.LastValidPosition);
            Assert.AreEqual(Vector3.zero, ball.LastValidVelocity);
        }

        [Test]
        public void ApplyKick_ZeroVelocityWhileElevated_RemainsAirborne()
        {
            var ball = BallState.CreateAtPosition(new Vector3(12f, 8f, 0.973f));
            BallCollision.SetBallControlled(ref ball);

            KickResult result = BallCollision.ApplyKick(
                ref ball, Vector3.zero, Vector3.zero, agentId: 1, matchTime: 0f);

            Assert.AreEqual(KickResult.Applied, result);
            Assert.AreEqual(BallStateType.Airborne, ball.State,
                "ERR-001-006: kick state selection must account for current height, not only velocity");
        }
''',
'BallControl release test')
s = once(s,
'''// | 1.0     | 2026-09-14 | —      | W6 Controlled entry/non-kick release regression set. |''',
'''// | 1.0     | 2026-09-14 | —      | W6 Controlled entry/non-kick release regression set. |
// | 1.1     | 2026-09-15 | —      | ERR-001-006: elevated non-kick release and zero kick remain Airborne. |''',
'BallControl history')
p.write_text(s, encoding='utf-8', newline='\n')

# State-machine tests: altitude must dominate the stop rule and direct Stationary normalization.
p = Path('src/ball-physics/tests/BallStateMachineTests.cs')
s = p.read_text(encoding='utf-8')
s = once(s,
'''// Modified: 2026-06-02''',
'''// Modified: 2026-06-02
// Modified: 2026-09-15 (ERR-001-006 elevated-state transition locks)''',
'BallStateMachineTests header')
anchor = '''        [Test]
        public void Rolling_AboveEnterThreshold_TransitionsToAirborne()
'''
insert = '''        [Test]
        public void Rolling_AboveEnterThreshold_BelowMinVelocity_StillTransitionsToAirborne()
        {
            var ball = new BallState
            {
                State = BallStateType.Rolling,
                Position = new Vector3(50f, 34f, BallPhysicsConstants.State.AirborneEnterThreshold + 0.01f),
                Velocity = new Vector3(BallPhysicsConstants.State.MinVelocity * 0.5f, 0f, 0f)
            };

            Assert.AreEqual(BallStateType.Airborne, BallStateMachine.UpdateBallState(ball),
                "ERR-001-006: height must win over the low-speed stop rule");
        }

'''
s = once(s, anchor, insert + anchor, 'rolling low-speed test insertion')
s = once(s,
'''        public void Stationary_AlwaysReturnsStationary()
        {
            var ball = new BallState { State = BallStateType.Stationary };
            Assert.AreEqual(BallStateType.Stationary, BallStateMachine.UpdateBallState(ball));
        }
''',
'''        public void Stationary_OnGround_ReturnsStationary()
        {
            var ball = new BallState
            {
                State = BallStateType.Stationary,
                Position = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS)
            };
            Assert.AreEqual(BallStateType.Stationary, BallStateMachine.UpdateBallState(ball));
        }

        [Test]
        public void Stationary_AboveEnterThreshold_TransitionsToAirborne()
        {
            var ball = new BallState
            {
                State = BallStateType.Stationary,
                Position = new Vector3(50f, 34f, 0.973f)
            };
            Assert.AreEqual(BallStateType.Airborne, BallStateMachine.UpdateBallState(ball),
                "ERR-001-006: Stationary is not valid above the airborne threshold");
        }
''',
'Stationary tests')
s = once(s,
'''// | 1.3     | 2026-06-02 | —      | AR-1 fixes. H-2: file header path corrected to src/ball-physics/.  |''',
'''// | 1.4     | 2026-09-15 | —      | ERR-001-006: height dominates low-speed stop; elevated Stationary  |
// |         |            |        | transitions to Airborne.                                           |
// | 1.3     | 2026-06-02 | —      | AR-1 fixes. H-2: file header path corrected to src/ball-physics/.  |''',
'BallStateMachineTests history')
p.write_text(s, encoding='utf-8', newline='\n')

# Ball Physics #1 §3.1.3: back-propagate the state invariant.
p = Path('docs/specs/ball-physics/section-3-1.md')
s = p.read_text(encoding='utf-8')
s = once(s, '**Version:** 2.7  ', '**Version:** 2.8  ', 'spec version')
changes = '''**Changes from v2.7:**
- **ERR-001-006:** an uncontrolled ball above `AIRBORNE_ENTER_THRESHOLD` cannot be `STATIONARY` or remain `ROLLING`; altitude takes precedence over the low-speed stop rule so gravity cannot be disabled for an elevated ball.
- §3.1.3 `STATIONARY` now recovers to `AIRBORNE` when elevated, and `ROLLING` checks airborne height before `MIN_VELOCITY`.
- §3.1.11.2 `ApplyKick` is amended in the continuation file so current height participates in post-kick state selection.

'''
s = once(s, '**Changes from v2.6:**', changes + '**Changes from v2.6:**', 'spec changes block')
s = once(s,
'''        case BallStateType.STATIONARY:
            // Transitions handled externally by kick/touch events
            return BallStateType.STATIONARY;
            
        case BallStateType.ROLLING:
            // Check if ball has stopped
            if (ball.Velocity.magnitude < BallPhysicsConstants.State.MIN_VELOCITY)
            {
                return BallStateType.STATIONARY;
            }
            // Check if ball went airborne (use ENTER threshold)
            if (ball.Position.z > BallPhysicsConstants.State.AIRBORNE_ENTER_THRESHOLD)
            {
                return BallStateType.AIRBORNE;
            }''',
'''        case BallStateType.STATIONARY:
            // ERR-001-006: a ground-rest state is invalid above the airborne threshold.
            if (ball.Position.z > BallPhysicsConstants.State.AIRBORNE_ENTER_THRESHOLD)
            {
                return BallStateType.AIRBORNE;
            }
            return BallStateType.STATIONARY;
            
        case BallStateType.ROLLING:
            // ERR-001-006: altitude wins over the low-speed stop rule.
            if (ball.Position.z > BallPhysicsConstants.State.AIRBORNE_ENTER_THRESHOLD)
            {
                return BallStateType.AIRBORNE;
            }
            // Check if ball has stopped only after confirming ground contact.
            if (ball.Velocity.magnitude < BallPhysicsConstants.State.MIN_VELOCITY)
            {
                return BallStateType.STATIONARY;
            }''',
'spec state machine')
p.write_text(s, encoding='utf-8', newline='\n')

# Ball Physics #1 §3.1.11.2 ApplyKick continuation: height-aware post-condition and pseudocode.
p = Path('docs/specs/ball-physics/section-3-1-8-to-3-1-14.md')
s = p.read_text(encoding='utf-8')
s = once(s,
'''///   - ball.State = AIRBORNE if velocity.z > 0, else ROLLING if horizontal, else STATIONARY''',
'''///   - ERR-001-006: ball.State = AIRBORNE if current height exceeds AIRBORNE_ENTER_THRESHOLD
///     OR velocity.z > 0; otherwise ROLLING if horizontal speed exceeds MIN_VELOCITY; else STATIONARY''',
'ApplyKick postcondition')
s = once(s,
'''    // State selection:
    //   velocity.z > 0          → ball is kicked upward → AIRBORNE
    //   velocity.z <= 0 AND
    //   horizontal speed > MIN  → ball stays on ground  → ROLLING
    //   otherwise               → kick was essentially zero → STATIONARY
    float horizontalSpeed = new Vector2(velocity.x, velocity.y).magnitude;

    if (velocity.z > 0f)''',
'''    // State selection (ERR-001-006): current height is authoritative first.
    // An already-elevated ball must remain AIRBORNE even for a zero, horizontal,
    // or downward kick so gravity remains active.
    float horizontalSpeed = new Vector2(velocity.x, velocity.y).magnitude;

    if (ball.Position.z > BallPhysicsConstants.State.AIRBORNE_ENTER_THRESHOLD || velocity.z > 0f)''',
'ApplyKick pseudocode')
p.write_text(s, encoding='utf-8', newline='\n')

# Tracking record for the spec back-prop and live W6 failure; avoids silently burying the cause in code.
p = Path('docs/tracking/w6-elevated-stationary-ball-fix.md')
p.write_text('''# W6 Elevated Stationary Ball Fix — ERR-001-006\n\n**Date:** September 15, 2026\n**Status:** IMPLEMENTED ON FIX BRANCH; VALIDATION PENDING\n**Scope:** Ball Physics #1 state invariant exposed by W6 physical Controlled possession. W2 remains shipping-disabled.\n\n## Failure evidence\n\nExact merged W6 head `e4335f7ff059deb483b1aaa534f2190fd3762008`, shipping-disarmed W2, seed `0x0F1E2D3C4B5A6978`, regressed from the exact pre-W6 first parent's **0.975 / 0.975** per-seed mirrored InPoss share to **0.530 / 0.530**. The old pooled scenario remained near its floor because the second seed stayed healthy, masking the collapse.\n\nA whole-pitch 0.1 s state trace isolated one continuous no-holder/no-pass interval from tick **100,062** to the half-time reset at tick **162,000**. Throughout it the ball was `Stationary`, speed `0.000`, at exactly `(7.241, 30.676, 0.973)` m. `Stationary` receives no gravity; first-touch requires motion; loose-ball pickup rejects an elevated ball. The match therefore cannot recover until the restart resets the state.\n\n## Causal localization\n\nAblation against exact post-W6 `main` shows removing only W6's goalkeeper kick-side `ReleasePossessionOnKick` does **not** change the failure (`0.530`). Restoring either goalkeeper physical-Controlled acquisition or outfield physical-Controlled acquisition to legacy flag-only behavior changes the deterministic trajectory and avoids the dead state (`0.986` and `0.981` respectively); restoring all legacy acquisition reproduces exact pre-W6 `0.975`. W6 therefore exposes the latent Ball Physics invariant violation through its new physical-Controlled trajectories; reverting W6 is not the fix.\n\n## ERR-001-006\n\nBall Physics #1 encoded two mutually incompatible rules: `STATIONARY` applies no forces and is externally exited, while §3.1.11.2 selected post-kick state from velocity alone and §3.1.3 let a slow `ROLLING` ball become `STATIONARY` before checking airborne height. W6 also added a non-kick Controlled release that selected `STATIONARY` regardless of height. Together these permit an uncontrolled ball above `AIRBORNE_ENTER_THRESHOLD` to become a force-free `STATIONARY` ball indefinitely.\n\nThe corrected invariant is: **an uncontrolled ball whose centre is above `AIRBORNE_ENTER_THRESHOLD` is Airborne regardless of low/zero horizontal speed; altitude is evaluated before the ground-rest stop rule.**\n\n## Fix boundary\n\n- `BallCollision.ApplyKick`: current height participates in state selection.\n- `BallCollision.ReleaseBallControl`: elevated release becomes `Airborne`, ground release remains `Stationary`.\n- `BallStateMachine`: elevated `Stationary` recovers to `Airborne`; `Rolling` checks height before low speed.\n- `BallPhysicsCore`: normalizes an already-invalid elevated `Stationary`/`Rolling` state before choosing forces, covering restored/legacy state as well as new producers.\n- Direct Ball Physics regression locks replace the W6 test that previously asserted an elevated non-kick release becomes `Stationary`.\n\nNo W2 activation or tackle calibration is part of this fix. After the focused Ball Physics gate is green, the same strict two-seed post-W6 disarmed and armed measurements must be rerun on the fix head before W2 sequencing resumes.\n''', encoding='utf-8', newline='\n')
