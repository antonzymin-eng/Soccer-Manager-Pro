// File:     src/defensive-ai/TackleMode.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Defensive AI #14 §2.2.3, §3.6, Code Standards #20
// Purpose:  Enum declaring the three tackle-intent modes produced by TackleIntentEvaluator.

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Tackle intent mode for a HOLD_SHAPE agent within tackle-eligible range.
    /// Consumed by Decision Tree #8 which translates the intent into an AgentAction
    /// dispatched to Collision System #3. Defensive AI #14 §2.2.3 / §3.6.
    /// </summary>
    public enum TackleMode : byte
    {
        /// <summary>Agent holds shape without engaging; coverage depth and angle insufficient.</summary>
        Hold = 0,

        /// <summary>Agent shadows the opponent without committing to a lunge.</summary>
        Jockey = 1,

        /// <summary>Agent commits to a tackle; adequate teammates behind provide cover.</summary>
        Commit = 2,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
