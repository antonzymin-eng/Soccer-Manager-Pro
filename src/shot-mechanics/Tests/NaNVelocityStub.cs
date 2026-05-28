// File:     src/shot-mechanics/Tests/NaNVelocityStub.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §4.1.2, Code Standards #20
// Purpose:  TEST USE ONLY. Returns float.NaN to trigger ShotExecutor FM-05 recovery.
//           Used exclusively by EC-008. Must not be referenced from production code.

#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// TEST USE ONLY. Returns float.NaN from Calculate() to trigger FM-05 NaN recovery
    /// in ShotExecutor. Used exclusively by EC-008. §4.1.2.
    /// </summary>
    internal sealed class NaNVelocityStub : IShotVelocityCalculator
    {
        /// <summary>Returns float.NaN intentionally to trigger FM-05 in ShotExecutor.</summary>
        public float Calculate(
            float       powerIntent,
            float       distanceToGoal,
            float       finishing,
            float       longShots,
            int         kickPower,
            ContactZone contactZone,
            float       spinIntent,
            float       fatigue,
            float       contactQualityModifier)
        {
            return float.NaN;
        }
    }
}
#endif

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation. |
#endregion
