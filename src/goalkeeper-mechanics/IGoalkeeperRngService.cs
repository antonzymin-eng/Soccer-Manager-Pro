// File:     src/goalkeeper-mechanics/IGoalkeeperRngService.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §4.4.2, Deterministic Simulation #16 §4.1 / §4.5, Code Standards #20
// Purpose:  Deterministic RNG service interface for draw-site-registered Gaussian and uniform draws.
//           Mirrors IHeadingRngService from Heading Mechanics #10.

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Deterministic RNG service interface for Goalkeeper Mechanics draw sites.
    /// Each draw site is registered with Deterministic Simulation #16 §4.5.
    /// Mirrors Heading Mechanics #10 IHeadingRngService for consistency.
    /// Goalkeeper Mechanics #11 §4.4.2 / Deterministic Simulation #16 §4.1 / §4.5.
    /// </summary>
    public interface IGoalkeeperRngService
    {
        /// <summary>
        /// Returns a uniform-random float in [0, 1) from the given draw site.
        /// drawSiteId must be registered with #16 §4.5. domainTag = DOMAIN_TAG_GOALKEEPER (0x1D).
        /// </summary>
        float NextFloat(int drawSiteId, uint domainTag);

        /// <summary>
        /// Returns a zero-mean unit-variance Gaussian sample from the given draw site.
        /// Uses Box-Muller or equivalent deterministic method.
        /// drawSiteId must be registered with #16 §4.5. domainTag = DOMAIN_TAG_GOALKEEPER (0x1D).
        /// </summary>
        float NextGaussian(int drawSiteId, uint domainTag);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
