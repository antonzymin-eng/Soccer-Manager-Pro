namespace TacticalDirector.Core.Physics.Ball
{
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
        public static float GetCoefficientOfRestitution(SurfaceType surface)
        {
            return surface switch
            {
                SurfaceType.GRASS_DRY    => 0.65f,
                SurfaceType.GRASS_WET    => 0.70f,
                SurfaceType.GRASS_LONG   => 0.55f,
                SurfaceType.ARTIFICIAL   => 0.72f,
                SurfaceType.FROZEN       => 0.80f,
                _                        => 0.65f
            };
        }

        public static float GetFrictionCoefficient(SurfaceType surface)
        {
            return surface switch
            {
                SurfaceType.GRASS_DRY    => 0.60f,
                SurfaceType.GRASS_WET    => 0.40f,
                SurfaceType.GRASS_LONG   => 0.70f,
                SurfaceType.ARTIFICIAL   => 0.55f,
                SurfaceType.FROZEN       => 0.20f,
                _                        => 0.60f
            };
        }

        public static float GetRollingResistance(SurfaceType surface)
        {
            return surface switch
            {
                SurfaceType.GRASS_DRY    => 0.13f,
                SurfaceType.GRASS_WET    => 0.07f,
                SurfaceType.GRASS_LONG   => 0.22f,
                SurfaceType.ARTIFICIAL   => 0.09f,
                SurfaceType.FROZEN       => 0.04f,
                _                        => 0.13f
            };
        }

        public static float GetSpinRetention(SurfaceType surface)
        {
            return surface switch
            {
                SurfaceType.GRASS_DRY    => 0.80f,
                SurfaceType.GRASS_WET    => 0.85f,
                SurfaceType.GRASS_LONG   => 0.70f,
                SurfaceType.ARTIFICIAL   => 0.75f,
                SurfaceType.FROZEN       => 0.90f,
                _                        => 0.80f
            };
        }
    }
}
