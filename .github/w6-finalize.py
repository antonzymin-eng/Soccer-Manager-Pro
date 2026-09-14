from pathlib import Path
import re


def read(path): return Path(path).read_text()
def write(path, text): Path(path).write_text(text)
def one(text, old, new, path):
    n = text.count(old)
    if n != 1:
        raise SystemExit(f'{path}: expected one match, found {n}: {old[:100]!r}')
    return text.replace(old, new, 1)

# BallCollision: correct stale caller contract.
p = 'src/ball-physics/BallCollision.cs'
s = read(p)
s = one(s,
'''        /// Transitions the ball to Controlled state.
        /// Called by the agent system after CheckPossession returns a valid agent.
''',
'''        /// Transitions the ball to Controlled state.
        /// Called by the host after its possession mechanic adjudicates a successful touch;
        /// CheckPossession remains available to hosts that use Ball Physics acquisition geometry.
''', p)
write(p, s)

# MatchEngine: add W6 version-history row at the current newest row.
p = 'src/match-engine/MatchEngine.cs'
s = read(p)
if '| 1.77' not in s:
    m = re.search(r'(?m)^// \| 1\.76.*$', s)
    if not m:
        raise SystemExit('MatchEngine: 1.76 version row not found')
    row = ('// | 1.77    | 2026-09-14 | —      | W6: genuine open-play possession enters BallState.Controlled, follows the holder, |\n'
           '// |         |            |        | and exits explicitly on non-kick release; restart-taker designation remains       |\n'
           '// |         |            |        | Stationary. No snapshot-schema or RNG change.                                     |\n')
    s = s[:m.start()] + row + s[m.start():]
write(p, s)

# W6 owner/design status.
p = 'docs/tracking/w6-controlled-ball-design.md'
s = read(p)
s = one(s, '**Status:** IMPLEMENTATION / VALIDATION', '**Status:** WIRED / VALIDATED (PR #412)', p)
write(p, s)

# Wiring backlog closeout.
p = 'docs/tracking/match-engine-wiring-backlog.md'
s = read(p)
s = one(s,
'> **UPDATED September 14, 2026 (v1.16):** W4 and W12 are on `main`; PR #398 is reconciled on top with W5 and W7 production wiring preserved. W12 establishes the pre-#398 runtime baseline and the separate unread-serialized-field sweep; W5 is now the required post-baseline comparison, not an assumed-live trigger. Five Class-A wiring items remain: W3, W6, W8, W9, W10. **After the post-#398 W12 comparison passes, W6 is next.**',
'> **UPDATED September 14, 2026 (v1.17):** W4, W5, W7, and W12 are on `main`; the post-#398 W12 comparison is GREEN. W6 is now wired on PR #412: genuine open-play possession enters Ball Physics `Controlled`, follows its holder, and exits explicitly on non-kick release, while restart-taker designation remains a placed `Stationary` ball. Four Class-A wiring items remain: W3, W8, W9, W10. **Next: rerun the W2 armed tackle evidence against the W6 state model before any tackle activation, then continue the remaining Class-A items.**', p)
pattern = re.compile(r'### W6 — `BallStateType\.Controlled` has no producer\n.*?(?=### W7 —)', re.S)
m = pattern.search(s)
if not m:
    raise SystemExit('backlog: W6 section not found')
replacement = '''### W6 — `BallStateType.Controlled` has no producer — ✅ **WIRED September 14, 2026 (PR #412)**
**Pre-fix evidence:** `ball-physics/BallCollision.cs` exposed `CheckPossession` and
`SetBallControlled`, but no production MatchEngine possession grant called the physical-control
transition. `_possessingAgentId` was only a logical flag, so a claimed ball could move independently
of its holder.

**Resolved:** genuine open-play possession now enters `BallStateType.Controlled` through one
MatchEngine ownership boundary and the ball is kinematically attached to the live holder at the end
of Physics. Outfield control uses the canonical ball-rest/foot height; goalkeeper control follows the
keeper while preserving the actual claim/contact height. First-touch control/interception,
loose-ball pickup, tackle ball-won, and goalkeeper possession all use this boundary. Existing
higher-level acquisition mechanics remain authoritative; W6 does not silently shrink their live
geometry by re-running Ball Physics' narrower `CheckPossession` predicate.

`_possessingAgentId` also designates restart takers, so restart awards deliberately remain a placed
`Stationary` ball and are excluded from physical control and tackle resolution. Kicks already leave
`Controlled` through `ApplyKick`; W6 adds an explicit non-kick `ReleaseBallControl` path for
knocked-loose possession and the six-second goalkeeper release. The forced goalkeeper release drops
the ball to foot height and preserves the existing re-collect cooldown. No new durable latch,
snapshot field/schema, RNG stream/domain/draw site, or draw-order change.

**Regression locks:** `MatchEngineControlledBallW6Tests` (pickup/control, holder following, restart
exception, goalkeeper carry, forced-loose exit, six-second release) and `BallControlStateTests`
(Controlled entry/release recovery checkpoints). Targeted validation passed: 3 BallPhysics + 47
MatchEngine affected tests, including first-touch, possession-bootstrap, goalkeeper, and tackle
coverage. Owner/design record: `docs/tracking/w6-controlled-ball-design.md`.

**W2 remains disabled.** W6 removes the leading carrier/ball-drift blocker but does not change
`TackleContactRadiusM` from its governed disabled value. The next action is to rerun the armed W2
corpus/composed-match evidence and make the tackle-activation decision separately; W6 does not smuggle
in a balance change.

'''
s = s[:m.start()] + replacement + s[m.end():]
write(p, s)

# src changelog: prepend header-chain entry + history row.
p = 'docs/tracking/CHANGELOG-src.md'
s = read(p)
s = one(s, '> **Last Updated:** September 14, 2026 (v2.136', '> **Last Updated (prior):** September 14, 2026 (v2.136', p)
anchor = '## Header chain\n\n'
entry = ('> **Last Updated:** September 14, 2026 (v2.137 — **W6 controlled-ball wiring (PR #412).** Genuine open-play possession now enters `BallStateType.Controlled` and follows its holder; restart-taker designation remains a placed `Stationary` ball; tackle-won, first-touch/interception, loose pickup, and goalkeeper possession share the physical-control boundary; non-kick release explicitly exits `Controlled`. New composed W6 and Ball Physics regression suites; no snapshot-schema or RNG change. W2 remains disabled pending post-W6 armed evidence.)\n>\n')
s = one(s, anchor, anchor + entry, p)
if '| 2.137' not in s:
    idx = s.find('| 2.136')
    if idx < 0: raise SystemExit('CHANGELOG-src: 2.136 history row not found')
    s = s[:idx] + '| 2.137   | 2026-09-14 | W6 controlled-ball wiring: physical open-play control + explicit release; PR #412. |\n' + s[idx:]
write(p, s)

# Root changelog chain.
p = 'docs/tracking/CHANGELOG.md'
s = read(p)
s = one(s, '> **Last Updated:** September 14, 2026 — **PR #398 W5 + W7', '> **Last Updated (prior):** September 14, 2026 — **PR #398 W5 + W7', p)
anchor = '---\n\n'
entry = ('> **Last Updated:** September 14, 2026 — **W6 controlled-ball wiring is implemented and validated on PR #412.** Genuine open-play possession now enters Ball Physics `Controlled` and follows the holder; restart-taker designation remains a stationary placed-ball abstraction; first touch, interception, loose pickup, tackle-won, and goalkeeper possession share the physical-control boundary. Kicks retain their existing release transition and W6 adds explicit non-kick release for tackle-loose and the six-second goalkeeper backstop. New composed regressions cover pickup/control, carrier following, restart placement, goalkeeper carry/release, and direct Ball Physics state transitions. Targeted affected-suite validation passed (3 BallPhysics + 47 MatchEngine tests). No new snapshot schema or RNG change. W2 remains disabled pending a fresh armed evidence run against the repaired state model.\n>\n')
s = one(s, anchor, anchor + entry, p)
write(p, s)

# File manifest latest record.
p = 'docs/tracking/file-manifest.md'
s = read(p)
s = one(s, '**Last Updated:** September 14, 2026 — **PR #398 W5 + W7', '**Last Updated (prior):** September 14, 2026 — **PR #398 W5 + W7', p)
anchor = '# File Manifest (Post-Migration Baseline)\n\n'
entry = ('**Last Updated:** September 14, 2026 — **W6 controlled-ball wiring / PR #412.**\n'
         '**NEW (5):** `docs/tracking/w6-controlled-ball-design.md`; `src/ball-physics/tests/BallControlStateTests.cs` (+ `.meta`); `src/match-engine/tests/MatchEngineControlledBallW6Tests.cs` (+ `.meta`). **Modified production (2):** `src/ball-physics/BallCollision.cs` v1.8 → v1.9 and `src/match-engine/MatchEngine.cs` v1.76 → v1.77. **Modified tracking (5):** `docs/tracking/match-engine-wiring-backlog.md` v1.16 → v1.17, `docs/tracking/CHANGELOG-src.md` v2.136 → v2.137, `docs/tracking/CHANGELOG.md`, `docs/tracking/open-issues.md`, and this manifest. Real open-play possession now enters/follows `Controlled`; restart takers remain stationary; non-kick release exits physical control explicitly. No new assembly edge, durable latch, snapshot schema, save format, RNG stream/domain/draw site/order, or `[GT]` change. W2 remains disabled pending post-W6 evidence.\n\n')
s = one(s, anchor, anchor + entry, p)
write(p, s)

# Maintained open-issues pointer/status.
p = 'docs/tracking/open-issues.md'
s = read(p)
s = s.replace('5 remaining Class-A wiring items (W3, W6, W8–W10)', '4 remaining Class-A wiring items (W3, W8–W10)')
s = s.replace('5 remaining Class-A wiring items (W3, W6, W8-W10)', '4 remaining Class-A wiring items (W3, W8-W10)')
s = s.replace('now v1.16', 'now v1.17')
write(p, s)
