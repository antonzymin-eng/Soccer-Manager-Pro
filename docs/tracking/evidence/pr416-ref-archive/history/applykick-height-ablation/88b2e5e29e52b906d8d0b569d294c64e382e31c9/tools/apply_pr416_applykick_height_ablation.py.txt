#!/usr/bin/env python3
from pathlib import Path

path = Path("src/ball-physics/BallCollision.cs")
text = path.read_text(encoding="utf-8")
old = "if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold || velocity.z > 0f)"
new = "if (velocity.z > 0f)"
count = text.count(old)
if count != 1:
    raise SystemExit(f"ApplyKick predicate: expected one match, found {count}")
path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")
