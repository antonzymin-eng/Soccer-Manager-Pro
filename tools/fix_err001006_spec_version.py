#!/usr/bin/env python3
from pathlib import Path

p = Path('docs/specs/ball-physics/section-3-1.md')
s = p.read_text(encoding='utf-8')
old = '**Version:** 2.8  \n'
new = '**Version:** 2.10  \n'
if s.count(old) != 1:
    raise SystemExit(f'header version anchor count={s.count(old)}')
s = s.replace(old, new, 1)
old = '**Changes from v2.7:**\n- **ERR-001-006:**'
new = '**Changes from v2.9:**\n- **ERR-001-006:**'
if s.count(old) != 1:
    raise SystemExit(f'changes anchor count={s.count(old)}')
s = s.replace(old, new, 1)
p.write_text(s, encoding='utf-8', newline='\n')

p = Path('docs/specs/ball-physics/section-3-1-8-to-3-1-14.md')
s = p.read_text(encoding='utf-8')
anchor = '| 2.9 | Jul 28, 2026 | AI | ERR-001-005 (shot-speed & woodwork design KD-4/KD-5): §3.1.10.3 goal-line adjudication moves to the segment\'s interpolated crossing of the out-plane (the detected position is up to ~0.42 m past the plane at shot speeds); §3.1.10.2 gains the swept frame detection paragraph — `ApplySweptGoalFrameCollision`, the six-cylinder segment test that finally calls `ApplyGoalPostCollision` in production (a discrete test tunnels a 0.12 m post at shot speeds). |\n'
row = '| 2.10 | Sep 15, 2026 | AI | ERR-001-006: elevated uncontrolled `STATIONARY`/`ROLLING` states are invalid; altitude takes precedence over the low-speed stop rule, `ApplyKick` includes current height in state selection, and elevated controlled release remains `AIRBORNE` so gravity cannot be disabled by a force-free mid-air state. |\n'
if s.count(anchor) != 1:
    raise SystemExit(f'history anchor count={s.count(anchor)}')
s = s.replace(anchor, anchor + row, 1)
p.write_text(s, encoding='utf-8', newline='\n')
