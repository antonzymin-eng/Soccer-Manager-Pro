#!/usr/bin/env python3
from pathlib import Path
p = Path('src/ball-physics/tests/BallPhysicsCoreTests.cs')
s = p.read_text(encoding='utf-8')
old = '// Modified: 2026-06-09 (AR-7 fix pass)\n'
new = old + '// Modified: 2026-09-15 (ERR-001-006 elevated Stationary recovery lock)\n'
assert s.count(old) == 1
s = s.replace(old, new, 1)
anchor = '        // ── Validation ───────────────────────────────────────────────────────────\n'
test = '''        [Test]\n        public void UpdateBallPhysics_ElevatedStationaryState_RecoversToAirborneAndFalls()\n        {\n            const float dt = 1f / 60f;\n            float initialZ = 0.973f;\n            var ball = new BallState\n            {\n                Position = new Vector3(7.241f, 30.676f, initialZ),\n                Velocity = Vector3.zero,\n                AngularVelocity = Vector3.zero,\n                State = BallStateType.Stationary,\n                LastValidPosition = new Vector3(7.241f, 30.676f, initialZ),\n                LastValidVelocity = Vector3.zero\n            };\n\n            BallPhysicsCore.UpdateBallPhysics(\n                ref ball, dt, SurfaceType.GrassDry, Vector3.zero, logger: null, matchTime: 0f);\n\n            Assert.AreEqual(BallStateType.Airborne, ball.State,\n                \"ERR-001-006: elevated legacy/restored Stationary state must self-heal before force selection\");\n            Assert.That(ball.Velocity.z, Is.LessThan(0f), \"gravity must act on the recovered state\");\n            Assert.That(ball.Position.z, Is.LessThan(initialZ), \"the ball must begin falling in the same tick\");\n        }\n\n'''
assert s.count(anchor) == 1
s = s.replace(anchor, test + anchor, 1)
p.write_text(s, encoding='utf-8', newline='\n')
