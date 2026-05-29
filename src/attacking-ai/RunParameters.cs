// File:     src/attacking-ai/RunParameters.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §2.2.3, FR-AT-011, Code Standards #20
// Purpose:  Exactly three-field run descriptor attached to RUNNER intents. No PatternType,
//           RunType, or OverlapType enum anywhere (KD-8 / FR-AT-010). Run angle is derived
//           at use-site as atan2(LateralOffsetM, DepthOffsetM); never stored here.

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Describes a timed depth run for a RUNNER agent. Contains exactly three fields
    /// (FR-AT-011). Carried as a value type inside <see cref="AttackIntent"/>.
    /// Attacking AI #15 §2.2.3 / §3.4.
    /// </summary>
    public readonly struct RunParameters
    {
        /// <summary>
        /// Forward distance from ball carrier in teamAttackAngle direction.
        /// Units: metres. Valid range: [5.0, 40.0] m (Clamp applied in §3.4).
        /// </summary>
        public float DepthOffsetM   { get; }

        /// <summary>
        /// Lateral offset perpendicular to attack direction; positive = toward y=68 touchline
        /// when attacking x=105 goal. Units: metres. Valid range: [−34.0, 34.0] m.
        /// </summary>
        public float LateralOffsetM { get; }

        /// <summary>
        /// Tick index at which the runner commits to the run. Always >= currentTick + 1.
        /// </summary>
        public int   RunTriggerTick { get; }

        /// <summary>Constructs a RunParameters value.</summary>
        public RunParameters(float depthOffsetM, float lateralOffsetM, int runTriggerTick)
        {
            DepthOffsetM   = depthOffsetM;
            LateralOffsetM = lateralOffsetM;
            RunTriggerTick = runTriggerTick;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
