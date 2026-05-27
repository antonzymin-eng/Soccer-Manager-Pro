// File:     src/pass-mechanics/PassTypeProfiles.cs
// Created:  2026-05-26
// Modified: 2026-05-27
// Author:   —
// Spec:     Pass Mechanics #5 §3.1.3, §3.1.4, Code Standards #20
// Purpose:  Static profile lookup returning a PhysicalProfile for each (PassType,
//           CrossSubType) pair. Values are the §3.1.4 master table — authoritative
//           over §3.2.9 where discrepancies exist. Loaded once at initialisation.

using UnityEngine;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// O(1) lookup of <see cref="PhysicalProfile"/> per pass type. All profile values
    /// are [GT] from §3.1.4 master table. Pass Mechanics #5 §3.1.3, §3.1.12.
    /// </summary>
    internal static class PassTypeProfiles
    {
        /// <summary>
        /// Returns the <see cref="PhysicalProfile"/> for the given pass type and cross
        /// sub-type. When passType != Cross, crossSubType is ignored (KD-6, §3.1.2).
        /// Logs a fatal error and returns a safe default if the type is unknown (FM-01).
        /// Pass Mechanics #5 §3.1.12.
        /// </summary>
        public static PhysicalProfile GetProfile(PassType passType, CrossSubType crossSubType = CrossSubType.Flat)
        {
            switch (passType)
            {
                case PassType.Ground:
                    return new PhysicalProfile
                    {
                        VMin             = 5.0f,
                        VOffset          = 8.0f,
                        VMax             = 18.0f,
                        AngleMin         = 2.0f,
                        AngleMax         = 5.0f,
                        DistMin          = 3.0f,
                        DistMax          = 30.0f,
                        SpinMagnitudeBase = 8.0f,
                        SpinMagnitudeMax  = 15.0f,
                        DominantSpin     = SpinType.Topspin,
                        IsAerial         = false,
                        IsSpaceTargeted  = false
                    };

                case PassType.Driven:
                    return new PhysicalProfile
                    {
                        VMin             = 10.0f,
                        VOffset          = 12.0f,
                        VMax             = 28.0f,
                        AngleMin         = 5.0f,
                        AngleMax         = 12.0f,
                        DistMin          = 15.0f,
                        DistMax          = 50.0f,
                        SpinMagnitudeBase = 10.0f,
                        SpinMagnitudeMax  = 18.0f,
                        DominantSpin     = SpinType.Topspin,
                        IsAerial         = false,
                        IsSpaceTargeted  = false
                    };

                case PassType.Lofted:
                    return new PhysicalProfile
                    {
                        VMin             = 8.0f,
                        VOffset          = 9.0f,
                        VMax             = 22.0f,
                        AngleMin         = 20.0f,
                        AngleMax         = 45.0f,
                        DistMin          = 20.0f,
                        DistMax          = 60.0f,
                        SpinMagnitudeBase = 8.0f,
                        SpinMagnitudeMax  = 15.0f,
                        DominantSpin     = SpinType.Topspin,
                        IsAerial         = true,
                        IsSpaceTargeted  = false
                    };

                case PassType.ThroughBall:
                    return new PhysicalProfile
                    {
                        VMin             = 6.0f,
                        VOffset          = 8.5f,
                        VMax             = 20.0f,
                        AngleMin         = 2.0f,
                        AngleMax         = 5.0f,
                        DistMin          = 10.0f,
                        DistMax          = 40.0f,
                        SpinMagnitudeBase = 8.0f,
                        SpinMagnitudeMax  = 15.0f,
                        DominantSpin     = SpinType.Topspin,
                        IsAerial         = false,
                        IsSpaceTargeted  = true
                    };

                case PassType.AerialThrough:
                    return new PhysicalProfile
                    {
                        VMin             = 8.0f,
                        VOffset          = 9.0f,
                        VMax             = 22.0f,
                        AngleMin         = 25.0f,
                        AngleMax         = 40.0f,
                        DistMin          = 20.0f,
                        DistMax          = 50.0f,
                        SpinMagnitudeBase = 8.0f,
                        SpinMagnitudeMax  = 15.0f,
                        DominantSpin     = SpinType.Topspin,
                        IsAerial         = true,
                        IsSpaceTargeted  = true
                    };

                case PassType.Cross:
                    return GetCrossProfile(crossSubType);

                case PassType.Chip:
                    return new PhysicalProfile
                    {
                        VMin             = 5.0f,
                        VOffset          = 6.0f,
                        VMax             = 14.0f,
                        AngleMin         = 45.0f,
                        AngleMax         = 65.0f,
                        DistMin          = 3.0f,
                        DistMax          = 20.0f,
                        SpinMagnitudeBase = 12.0f,
                        SpinMagnitudeMax  = 20.0f,
                        DominantSpin     = SpinType.Backspin,
                        IsAerial         = true,
                        IsSpaceTargeted  = false
                    };

                default:
                    Debug.LogError($"[PassTypeProfiles] FM-01: No PhysicalProfile for PassType={passType}. Returning Ground profile as safe default.");
                    return GetProfile(PassType.Ground);
            }
        }

        private static PhysicalProfile GetCrossProfile(CrossSubType subType)
        {
            switch (subType)
            {
                case CrossSubType.Whipped:
                    return new PhysicalProfile
                    {
                        VMin             = 8.0f,
                        VOffset          = 10.0f,
                        VMax             = 24.0f,
                        AngleMin         = 15.0f,
                        AngleMax         = 25.0f,
                        DistMin          = 25.0f,
                        DistMax          = 50.0f,
                        SpinMagnitudeBase = 18.0f,
                        SpinMagnitudeMax  = 28.0f,
                        DominantSpin     = SpinType.Sidespin,
                        IsAerial         = false,
                        IsSpaceTargeted  = true
                    };

                case CrossSubType.High:
                    return new PhysicalProfile
                    {
                        VMin             = 8.0f,
                        VOffset          = 10.0f,
                        VMax             = 20.0f,
                        AngleMin         = 25.0f,
                        AngleMax         = 40.0f,
                        DistMin          = 25.0f,
                        DistMax          = 50.0f,
                        SpinMagnitudeBase = 12.0f,
                        SpinMagnitudeMax  = 22.0f,
                        DominantSpin     = SpinType.Mixed,
                        IsAerial         = true,
                        IsSpaceTargeted  = true
                    };

                case CrossSubType.Flat:
                    return new PhysicalProfile
                    {
                        VMin             = 8.0f,
                        VOffset          = 10.0f,
                        VMax             = 26.0f,
                        AngleMin         = 8.0f,
                        AngleMax         = 15.0f,
                        DistMin          = 20.0f,
                        DistMax          = 45.0f,
                        SpinMagnitudeBase = 15.0f,
                        SpinMagnitudeMax  = 25.0f,
                        DominantSpin     = SpinType.Sidespin,
                        IsAerial         = false,
                        IsSpaceTargeted  = true
                    };

                default:
                    Debug.LogError($"[PassTypeProfiles] FM-01: Unknown CrossSubType={subType} for PassType.Cross. Returning Flat profile as safe default.");
                    return GetCrossProfile(CrossSubType.Flat);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                   |
// | 1.0     | 2026-05-26 | —      | Initial implementation.                                                 |
// | 1.1     | 2026-05-26 | —      | M1/M2: Lofted and AerialThrough DominantSpin corrected Backspin →      |
// |         |            |        |     Topspin to match SpinType enum doc and PassVelocityCalculator.      |
// | 1.2     | 2026-05-27 | —      | AR-1 H-1: class changed public → internal; CS0050 compile error        |
// |         |            |        |     (public GetProfile() returning internal PhysicalProfile).           |
// | 1.3     | 2026-05-27 | —      | AR-1 round-4 L-A: GetCrossProfile gains explicit CrossSubType.Flat      |
// |         |            |        |     case; default now logs FM-01 for unknown CrossSubType values         |
// |         |            |        |     (consistent with GetProfile FM-01 pattern).                         |
#endregion
