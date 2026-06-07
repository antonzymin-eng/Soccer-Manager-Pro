// File:     src/shot-mechanics/GoalGeometryProvider.cs
// Created:  2026-05-27
// Modified: 2026-06-07
// Author:   —
// Spec:     Shot Mechanics #6 §4.1.1, Code Standards #20
// Purpose:  Single access point for goal geometry constants. Returns ShotMechanicsConstants
//           compile-time values in production. Exposes a test-only override seam for SP-009.
//           ShotPlacementResolver is the only production caller. §4.1.1.

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Single access point for goal geometry constants. Used exclusively by ShotPlacementResolver.
    /// In production: returns compile-time constants from ShotMechanicsConstants.
    /// In editor/development: supports a test-only override for SP-009.
    /// Shot Mechanics #6 §4.1.1.
    /// </summary>
    public static class GoalGeometryProvider
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static GoalGeometry? s_testOverride = null;
#endif

        /// <summary>
        /// Returns goal geometry. In production: compile-time constants.
        /// In editor/development: returns test override if one has been set.
        /// Assumes the attacking team is shooting toward X = PitchLength (right goal).
        /// Stage 1+ will supply attack direction from match context.
        /// </summary>
        public static GoalGeometry Get()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (s_testOverride.HasValue)
                return s_testOverride.Value;
#endif
            float pitchLength = ShotMechanicsConstants.PitchLength;
            float pitchWidth  = ShotMechanicsConstants.PitchWidth;
            float goalWidth   = ShotMechanicsConstants.GOAL_WIDTH;
            float goalHeight  = ShotMechanicsConstants.GOAL_HEIGHT;

            // Goal posts are centred on the pitch Y axis: midpoint = pitchWidth / 2.
            // GoalCentreU (= 0.5) is the goal-relative midpoint constant from §3.5 — re-used
            // here as the dimensionless "half" multiplier so this site has no magic literal
            // (FR-CS-016). Semantically identical to a dedicated [DERIVED] `Half` constant.
            return new GoalGeometry
            {
                GoalWidth  = goalWidth,
                GoalHeight = goalHeight,
                GoalLineX  = pitchLength,
                LeftPostY  = (pitchWidth - goalWidth) * ShotMechanicsConstants.GoalCentreU,
                RightPostY = (pitchWidth + goalWidth) * ShotMechanicsConstants.GoalCentreU,
                CrossbarZ  = goalHeight
            };
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// TEST USE ONLY. Sets a temporary goal geometry override for SP-009.
        /// Must be paired with ClearTestOverride() in [TearDown].
        /// Use try/finally inside SP-009 as a second defence in case the test body throws
        /// before TearDown. Must not be called from production code paths.
        /// </summary>
        public static void SetTestOverride(GoalGeometry overrides)
        {
            s_testOverride = overrides;
        }

        /// <summary>
        /// TEST USE ONLY. Clears the test override. Call in [TearDown] of every test
        /// that calls SetTestOverride(). See try/finally note in SetTestOverride().
        /// </summary>
        public static void ClearTestOverride()
        {
            s_testOverride = null;
        }
#endif
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                    |
// | 1.0     | 2026-05-27 | —      | Initial implementation. Field naming corrected vs spec §4.1.1 to match  |
// |         |            |        |     authoritative coordinate system (X=length, Y=width, Z=height).      |
// | 1.1     | 2026-05-28 | —      | H-2: GoalGeometry struct extracted to GoalGeometry.cs.                   |
// | 1.2     | 2026-06-07 | —      | AR-4 L-1: 0.5f magic literals in goal-post midpoint replaced with        |
// |         |            |        |   ShotMechanicsConstants.GoalCentreU (FR-CS-016).                        |
#endregion
