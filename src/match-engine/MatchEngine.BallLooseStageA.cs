// File:     src/match-engine/MatchEngine.BallLooseStageA.cs
// Created:  2026-11-09
// Modified: 2026-11-10
// Author:   —
// Spec:     #14 §3.6.5; w-440-ball-loose-design.md Stage A (observation only)
// Purpose:  Optional development-only observer with no simulation writes or RNG.

#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS
using System;

namespace TacticalDirector.MatchEngine
{
    public sealed partial class MatchEngine
    {
        // Test-side consumer attaches only under TD_W440_OBSERVE=1. No production environment reads.
        internal Action<BallLooseStageAEvent> TestOnly_BallLooseStageAObserver;

        private void ObserveBallLooseStageA(BallLooseStageAKind kind, int actor = -1,
            int other = -1, int detail = 0, int previous = -1, int next = -1)
        {
            TestOnly_BallLooseStageAObserver?.Invoke(new BallLooseStageAEvent(kind,
                (int)_clock.CurrentTick, actor, other, detail, previous, next,
                _ball.Position, _ball.Velocity));
        }
    }
}
#endif

#region VersionHistory
// | Version | Date | Author | Notes |
// | 1.0 | 2026-11-10 | — | Diagnostic delegate only, stripped from release; no persistent gameplay state. |
#endregion
