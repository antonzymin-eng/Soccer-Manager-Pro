// File:     src/ball-physics/SurfaceProperties.cs
// Created:  2026-05-24
// Modified: 2026-06-02
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Returns per-surface coefficients (restitution, friction, rolling resistance,
//           spin retention) for use in ground-contact physics calculations.

namespace TacticalDirector.BallPhysics
{
    /// <summary>Surface types supported by the Stage 0 physics model.</summary>
    public enum SurfaceType
    {
        GrassDry,
        GrassWet,
        GrassLong,
        Artificial,
        Frozen
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
                SurfaceType.GrassDry   => BallPhysicsConstants.SurfaceCoR.GrassDry,
                SurfaceType.GrassWet   => BallPhysicsConstants.SurfaceCoR.GrassWet,
                SurfaceType.GrassLong  => BallPhysicsConstants.SurfaceCoR.GrassLong,
                SurfaceType.Artificial => BallPhysicsConstants.SurfaceCoR.Artificial,
                SurfaceType.Frozen     => BallPhysicsConstants.SurfaceCoR.Frozen,
                _ => throw new System.ArgumentOutOfRangeException(
                        nameof(surface), surface, "Unknown SurfaceType — extend SurfaceProperties when adding a new surface.")
            };
        }

        /// <summary>Returns the tangential friction coefficient for impulse-based bounce.</summary>
        public static float GetFrictionCoefficient(SurfaceType surface)
        {
            return surface switch
            {
                SurfaceType.GrassDry   => BallPhysicsConstants.SurfaceFriction.GrassDry,
                SurfaceType.GrassWet   => BallPhysicsConstants.SurfaceFriction.GrassWet,
                SurfaceType.GrassLong  => BallPhysicsConstants.SurfaceFriction.GrassLong,
                SurfaceType.Artificial => BallPhysicsConstants.SurfaceFriction.Artificial,
                SurfaceType.Frozen     => BallPhysicsConstants.SurfaceFriction.Frozen,
                _ => throw new System.ArgumentOutOfRangeException(
                        nameof(surface), surface, "Unknown SurfaceType — extend SurfaceProperties when adding a new surface.")
            };
        }

        /// <summary>Returns the rolling-resistance coefficient (μ_r) for the surface.</summary>
        public static float GetRollingResistance(SurfaceType surface)
        {
            return surface switch
            {
                SurfaceType.GrassDry   => BallPhysicsConstants.Rolling.ResistanceGrassDry,
                SurfaceType.GrassWet   => BallPhysicsConstants.Rolling.ResistanceGrassWet,
                SurfaceType.GrassLong  => BallPhysicsConstants.Rolling.ResistanceGrassLong,
                SurfaceType.Artificial => BallPhysicsConstants.Rolling.ResistanceArtificial,
                SurfaceType.Frozen     => BallPhysicsConstants.Rolling.ResistanceFrozen,
                _ => throw new System.ArgumentOutOfRangeException(
                        nameof(surface), surface, "Unknown SurfaceType — extend SurfaceProperties when adding a new surface.")
            };
        }

        /// <summary>Returns the spin-retention multiplier applied after ground contact.</summary>
        public static float GetSpinRetention(SurfaceType surface)
        {
            return surface switch
            {
                SurfaceType.GrassDry   => BallPhysicsConstants.SurfaceSpinRetention.GrassDry,
                SurfaceType.GrassWet   => BallPhysicsConstants.SurfaceSpinRetention.GrassWet,
                SurfaceType.GrassLong  => BallPhysicsConstants.SurfaceSpinRetention.GrassLong,
                SurfaceType.Artificial => BallPhysicsConstants.SurfaceSpinRetention.Artificial,
                SurfaceType.Frozen     => BallPhysicsConstants.SurfaceSpinRetention.Frozen,
                _ => throw new System.ArgumentOutOfRangeException(
                        nameof(surface), surface, "Unknown SurfaceType — extend SurfaceProperties when adding a new surface.")
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
// | 1.3     | 2026-06-02 | —      | AR-1 fixes. H-2: file header path corrected to src/ball-physics/.  |
// |         |            |        | M-4: SurfaceType members renamed to PascalCase. L-4 parallel: switch|
// |         |            |        | expressions now throw ArgumentOutOfRangeException for unknown enum  |
// |         |            |        | values instead of silently returning GrassDry — fails fast on stale|
// |         |            |        | cast-from-int callers.                                              |
#endregion
