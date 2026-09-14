#!/usr/bin/env python3
from pathlib import Path

match_engine = Path("src/match-engine/MatchEngine.cs")
s = match_engine.read_text(encoding="utf-8")

old = """        private void TryResolveTackles()
        {
            // W6: _possessingAgentId also designates a restart taker while the placed ball remains
            // Stationary. Only BallState.Controlled denotes a physical carrier that can be challenged.
            if (_ball.State != BallStateType.Controlled)
            {
                return;
            }

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (_tackleCooldown[i] > 0)
                {
                    _tackleCooldown[i]--;
                }
            }
"""
new = """        private void TryResolveTackles()
        {
            // Cooldown is elapsed in AI strides, not in carrier-present opportunities. Age it before
            // any physical-carrier gate so a long pass, loose-ball phase or restart cannot freeze a
            // defender's remaining tackle cooldown (PR #412 P2).
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (_tackleCooldown[i] > 0)
                {
                    _tackleCooldown[i]--;
                }
            }

            // W6: _possessingAgentId also designates a restart taker while the placed ball remains
            // Stationary. Only BallState.Controlled denotes a physical carrier that can be challenged.
            if (_ball.State != BallStateType.Controlled)
            {
                return;
            }
"""
if s.count(old) != 1:
    raise SystemExit(f"expected one TryResolveTackles target, found {s.count(old)}")
s = s.replace(old, new, 1)

hook = """        /// <summary>Test-only: this agent's remaining challenge cooldown in AI strides.</summary>
        internal int TestOnly_TackleCooldown(int agentId) => _tackleCooldown[agentId];
"""
hook_new = """        /// <summary>Test-only: this agent's remaining challenge cooldown in AI strides.</summary>
        internal int TestOnly_TackleCooldown(int agentId) => _tackleCooldown[agentId];

        /// <summary>Test-only: stage a remaining tackle cooldown without requiring a stochastic duel.</summary>
        internal void TestOnly_SetTackleCooldown(int agentId, int remainingStrides)
        {
            _tackleCooldown[agentId] = remainingStrides;
        }

        /// <summary>Test-only: execute the tackle resolver once at an AI-stride boundary.</summary>
        internal void TestOnly_RunTackleResolver() => TryResolveTackles();
"""
if s.count(hook) != 1:
    raise SystemExit(f"expected one tackle cooldown hook, found {s.count(hook)}")
s = s.replace(hook, hook_new, 1)

header = "// Modified: 2026-09-14 (W6 controlled ball — physical possession drives BallState.Controlled + carrier attachment; restart taker stays stationary; no schema/RNG change)"
replacement = "// Modified: 2026-09-14 (W6 review P2 — tackle cooldown now ages on every AI stride even without a physical carrier; regression seam only, no schema/RNG change)\n" + header
if header not in s:
    raise SystemExit("MatchEngine modified-header anchor not found")
s = s.replace(header, replacement, 1)

row = "// | 1.77    | 2026-09-14 | —      | W6: genuine open-play possession enters BallState.Controlled, follows the holder, |"
row_new = "// | 1.78    | 2026-09-14 | —      | W6 review P2: tackle cooldown ages before the physical-carrier gate, so loose/restart |\n// |         |            |        | strides cannot freeze elapsed cooldown time; test-only staging/invocation seams added. |\n" + row
if row not in s:
    raise SystemExit("MatchEngine version-history anchor not found")
s = s.replace(row, row_new, 1)
match_engine.write_text(s, encoding="utf-8", newline="\n")

test_file = Path("src/match-engine/tests/MatchEngineControlledBallW6Tests.cs")
s = test_file.read_text(encoding="utf-8")
anchor = """        [Test]
        public void ForcedKeeperRelease_ExitsControlled_AndDropsBallAtFeet()
"""
if anchor not in s:
    raise SystemExit("W6 test insertion anchor not found")
test = """        [Test]
        public void LooseBall_DoesNotFreezeElapsedTackleCooldown()
        {
            var engine = new MatchEngine(MatchSeed ^ 0x48UL);
            const int defender = 4;
            engine.TestOnly_SetTackleCooldown(defender, remainingStrides: 2);

            engine.TestOnly_ForceBallLoose(
                new Vector3(52.5f, 34f, MatchEngineConstants.BALL_REST_HEIGHT_M),
                new Vector3(1f, 0f, 0f));

            engine.TestOnly_RunTackleResolver();

            Assert.AreEqual(1, engine.TestOnly_TackleCooldown(defender),
                "Tackle cooldown is elapsed AI-stride time; a loose/restart interval must not freeze it.");
        }

"""
s = s.replace(anchor, test + anchor, 1)
old_hist = "// | 1.0     | 2026-09-14 | —      | W6 composed physical-control and release regression locks.    |"
new_hist = "// | 1.1     | 2026-09-14 | —      | P2 lock: loose ball still advances elapsed tackle cooldown.   |\n" + old_hist
if old_hist not in s:
    raise SystemExit("W6 test version-history anchor not found")
s = s.replace(old_hist, new_hist, 1)
test_file.write_text(s, encoding="utf-8", newline="\n")
