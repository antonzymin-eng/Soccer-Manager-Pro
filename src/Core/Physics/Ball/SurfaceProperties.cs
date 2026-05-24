// File:     src/Core/Physics/Ball/SurfaceProperties.cs
// Created:  2026-05-24
// Modified: 2026-05-24
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Returns per-surface coefficients (restitution, friction, rolling resistance,
//           spin retention) for use in ground-contact physics calculations.

namespace TacticalDirector.BallPhysics
{
    /// <summary>Surface types supported by the Stage 0 physics model.</summary>
    public enum SurfaceType
    {
        GRASS_DRY,
        GRASS_WET,
        GRASS_LONG,
        ARTIFICIAL,
        FROZEN
    }

    /// <summary>
    /// Returns surface coefficients for a given surface type.
    /// Stage 0: single global surface. Stage 3+: per-position surface queries.
    /// </summary>
    public static class SurfaceProperties
    {
        /// <summary>Returns the coefficient of restitution (bounciness) for the surface.</summary>
        public static float GetCoefficientOfRestitution(SurfaceType surface)
        {
            return surface switch
            {
                SurfaceType.GRASS_DRY  => BallPhysicsConstants.SurfaceCoR.GrassDry,
                SurfaceType.GRASS_WET  => BallPhysicsConstants.SurfaceCoR.GrassWet,
                SurfaceType.GRASS_LONG => BallPhysicsConstants.SurfaceCoR.GrassLong,
                SurfaceType.ARTIFICIAL => BallPhysicsConstants.SurfaceCoR.Artificial,
                SurfaceType.FROZEN     => BallPhysicsConstants.SurfaceCoR.Frozen,
                _                      => BallPhysicsConstants.SurfaceCoR.GrassDry
            };
        }

        /// <summary>Returns the tangential friction coefficient for impulse-based bounce.</summary>
        public static float GetFrictionCoefficient(SurfaceType surface)
        {
            return surface switch
            {
                SurfaceType.GRASS_DRY  => BallPhysicsConstants.SurfaceFriction.GrassDry,
                SurfaceType.GRASS_WET  => BallPhysicsConstants.SurfaceFriction.GrassWet,
                SurfaceType.GRASS_LONG => BallPhysicsConstants.SurfaceFriction.GrassLong,
                SurfaceType.ARTIFICIAL => BallPhysicsConstants.SurfaceFriction.Artificial,
                SurfaceType.FROZEN     => BallPhysicsConstants.SurfaceFriction.Frozen,
                _                      => BallPhysicsConstants.SurfaceFriction.GrassDry
            };
        }

        /// <summary>Returns the rolling-resistance coefficient (μ_r) for the surface.</summary>
        public static float GetRollingResistance(SurfaceType surface)
        {
            return surface switch
            {
                SurfaceType.GRASS_DRY  => BallPhysicsConstants.Rolling.ResistanceGrassDry,
                SurfaceType.GRASS_WET  => BallPhysicsConstants.Rolling.ResistanceGrassWet,
                SurfaceType.GRASS_LONG => BallPhysicsConstants.Rolling.ResistanceGrassLong,
                SurfaceType.ARTIFICIAL => BallPhysicsConstants.Rolling.ResistanceArtificial,
                SurfaceType.FROZEN     => BallPhysicsConstants.Rolling.ResistanceFrozen,
                _                      => BallPhysicsConstants.Rolling.ResistanceGrassDry
            };
        }

        /// <summary>Returns the spin-retention multiplier applied after ground contact.</summary>
        public static float GetSpinRetention(SurfaceType surface)
        {
            return surface switch
            {
                SurfaceType.GRASS_DRY  => BallPhysicsConstants.SurfaceSpinRetention.GrassDry,
                SurfaceType.GRASS_WET  => BallPhysicsConstants.SurfaceSpinRetention.GrassWet,
                SurfaceType.GRASS_LONG => BallPhysicsConstants.SurfaceSpinRetention.GrassLong,
                SurfaceType.ARTIFICIAL => BallPhysicsConstants.SurfaceSpinRetention.Artificial,
                SurfaceType.FROZEN     => BallPhysicsConstants.SurfaceSpinRetention.Frozen,
                _                      => BallPhysicsConstants.SurfaceSpinRetention.GrassDry
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-24 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-24 | —      | Fix pass: namespace → TacticalDirector.BallPhysics; GetRolling     |
// |         |            |        | Resistance now routes grass values through BallPhysicsConstants     |
// |         |            |        | so a single GT constant drives both catalogues; file header added   |
// |         |            |        | per FR-CS-056/057.                                                  |
// | 1.2     | 2026-05-24 | —      | All literal surface coefficients replaced by named constants        |
// |         |            |        | (SurfaceCoR, SurfaceFriction, SurfaceSpinRetention, Rolling) per   |
// |         |            |        | FR-CS-016 (no literals in formula/system files).                   |
#endregion
