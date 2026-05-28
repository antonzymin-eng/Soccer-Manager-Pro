// File:     src/heading-mechanics/IHeadingRngService.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §4.2, §4.4, Deterministic Simulation #16 §4.1, §4.5, Code Standards #20
// Purpose:  Draw-site-aware deterministic RNG boundary consumed by §3.4 and §3.7.

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Deterministic RNG service boundary consumed by Heading Mechanics #10.
    /// All randomness routes through registered draw-site IDs per Deterministic Simulation #16 §4.1 / §4.5.
    /// Heading Mechanics #10 §4.2 / §4.4 / FR-HE-008 / KD-10.
    /// Three draw sites declared in HeadingMechanicsConstants: DRAW_SITE_TIMING_JITTER,
    /// DRAW_SITE_CONTACT_POINT_ERROR, DRAW_SITE_DUEL_TIEBREAK.
    /// </summary>
    public interface IHeadingRngService
    {
        /// <summary>
        /// Returns a uniform float in [0, 1) for the given registered draw site.
        /// Used by §3.7 DRAW_SITE_DUEL_TIEBREAK.
        /// Deterministic Simulation #16 §4.1.
        /// </summary>
        /// <param name="drawSiteId">Registered draw-site ID. Must be one of the three constants in HeadingMechanicsConstants.</param>
        float NextFloat(int drawSiteId);

        /// <summary>
        /// Returns a standard Gaussian sample (mean 0, σ 1) for the given registered draw site.
        /// Used by §3.4 DRAW_SITE_TIMING_JITTER and DRAW_SITE_CONTACT_POINT_ERROR.
        /// Deterministic Simulation #16 §4.1 / §4.5.
        /// </summary>
        /// <param name="drawSiteId">Registered draw-site ID. Must be one of the three constants in HeadingMechanicsConstants.</param>
        float NextGaussian(int drawSiteId);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
