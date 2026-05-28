// File:     src/shot-mechanics/IShotVelocityCalculator.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §4.1.2, Code Standards #20
// Purpose:  Thin interface on ShotVelocityCalculator enabling NaN injection for EC-008
//           (FM-05 recovery path test). Only production implementation: ShotVelocityCalculator.
//           Only test stub: NaNVelocityStub (Tests/). §4.1.2.

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Thin interface on ShotVelocityCalculator for EC-008 NaN injection test only.
    /// Must not be used for any other purpose. Do not add further calculator interfaces
    /// without a formal amendment specifying the test case that requires them. §4.1.2.
    /// </summary>
    public interface IShotVelocityCalculator
    {
        /// <summary>
        /// Computes scalar kick speed (m/s) from §3.2 formula.
        /// Production implementation (ShotVelocityCalculator) never returns NaN or Infinity.
        /// NaNVelocityStub returns float.NaN intentionally to trigger FM-05 in ShotExecutor.
        /// Shot Mechanics #6 §3.2, §4.1.2.
        /// </summary>
        float Calculate(
            float       powerIntent,
            float       distanceToGoal,
            float       finishing,
            float       longShots,
            int         kickPower,
            ContactZone contactZone,
            float       spinIntent,
            float       fatigue,
            float       contactQualityModifier);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation. |
#endregion
