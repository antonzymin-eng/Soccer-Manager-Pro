// File:     src/heading-mechanics/HeadingContactQuality.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §3.4, KD-2, FR-HE-002, FR-HE-020, FR-HE-022, FR-HE-024, Code Standards #20
// Purpose:  Contact-quality scalar computation (FM-010-002). Piecewise asymmetric timing + point error.

using UnityEngine;

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Contact-quality scalar computation per §3.4 (FM-010-002).
    /// Produces a continuous value in [0, 1] from asymmetric timing offset and 2-D point error.
    /// Labels (Early/OnTime/Late) are assigned for telemetry only — never consumed by physics formulas (KD-2).
    /// Heading Mechanics #10 §3.4.
    /// </summary>
    public static class HeadingContactQuality
    {
        /// <summary>
        /// Computes the contact-quality scalar and telemetry label for a header contact.
        /// FM-010-002. Heading Mechanics #10 §3.4 / KD-2 / FR-HE-002.
        /// </summary>
        /// <param name="actualContactFrame">60 Hz frame at which contact occurred.</param>
        /// <param name="idealContactFrame">Apex-aligned ideal contact frame from EligibilityResult.</param>
        /// <param name="contactPointActual">Actual 2-D contact point in head-local coordinates (m).</param>
        /// <param name="contactPointIntent">Intended 2-D contact point in head-local coordinates (m).</param>
        /// <param name="attrs">Agent attribute snapshot (Heading in [1, 20]).</param>
        /// <param name="rng">Deterministic RNG service (draw sites: DRAW_SITE_TIMING_JITTER, DRAW_SITE_CONTACT_POINT_ERROR).</param>
        /// <param name="timingOffsetMs">Output: signed timing offset in ms (positive = late). §3.4.</param>
        /// <param name="contactPointError">Output: 2-D head-local contact point error (m). §3.4.</param>
        /// <param name="label">Output: telemetry label (Early/OnTime/Late) — never consumed by formulas. §3.4 / FR-HE-020.</param>
        /// <returns>Contact quality scalar in [0, 1].</returns>
        public static float Compute(
            int actualContactFrame,
            int idealContactFrame,
            Vector2 contactPointActual,
            Vector2 contactPointIntent,
            HeadingAgentAttributes attrs,
            IHeadingRngService rng,
            out float timingOffsetMs,
            out Vector2 contactPointError,
            out ContactQualityLabel label)
        {
            // ── Timing quality ───────────────────────────────────────────────────────
            float timingJitterMs = HeadingMechanicsConstants.TimingJitterSigmaMs
                                 * rng.NextGaussian(HeadingMechanicsConstants.DrawSiteTimingJitter);

            timingOffsetMs = (actualContactFrame - idealContactFrame)
                           * HeadingMechanicsConstants.FrameMs
                           + timingJitterMs;

            float timingQuality;
            if (timingOffsetMs <= 0.0f)
            {
                // Early or on-time: normalise against early tolerance.
                timingQuality = 1.0f - Clamp01(-timingOffsetMs / HeadingMechanicsConstants.MaxEarlyToleranceMs);
            }
            else
            {
                // Late: normalise against (numerically smaller) late tolerance.
                timingQuality = 1.0f - Clamp01(timingOffsetMs / HeadingMechanicsConstants.MaxLateToleranceMs);
            }

            // ── Point quality ────────────────────────────────────────────────────────
            float headingAttrScale = ComputeHeadingAttrScale(attrs);

            float pointNoiseM = HeadingMechanicsConstants.ContactPointNoiseSigmaM
                              * rng.NextGaussian(HeadingMechanicsConstants.DrawSiteContactPointError)
                              / headingAttrScale;

            Vector2 rawError  = contactPointActual - contactPointIntent;
            contactPointError = rawError;

            float systematicErrorM = rawError.magnitude / headingAttrScale;
            float pointError       = systematicErrorM + pointNoiseM;

            // pointQuality denominator is the bare sigma constant — mapping is player-invariant (§3.4).
            float pointQuality = 1.0f - Clamp01(pointError / HeadingMechanicsConstants.ContactPointErrorSigmaM);

            // ── Convex combination ───────────────────────────────────────────────────
            float alpha  = HeadingMechanicsConstants.TimingPointBlendAlpha;
            float scalar = alpha * timingQuality + (1.0f - alpha) * pointQuality;

            // ── Telemetry label (KD-2 / FR-HE-020) ──────────────────────────────────
            label = AssignLabel(timingOffsetMs);

            return scalar;
        }

        /// <summary>
        /// Returns the heading-attribute scale factor used to normalise point errors.
        /// headingAttrScale = 1 + CONTACT_POINT_HEADING_ATTR_COEFF × (Heading_norm − 0.5).
        /// Higher Heading → smaller effective pointError → higher quality. §3.4 v0.2 H-2.
        /// </summary>
        public static float ComputeHeadingAttrScale(HeadingAgentAttributes attrs)
        {
            float headingNorm = Mathf.Clamp(attrs.Heading,
                HeadingMechanicsConstants.ATTR_MIN,
                HeadingMechanicsConstants.ATTR_MAX)
                / HeadingMechanicsConstants.ATTR_MAX;

            return 1.0f + HeadingMechanicsConstants.ContactPointHeadingAttrCoeff * (headingNorm - 0.5f);
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        private static ContactQualityLabel AssignLabel(float timingOffsetMs)
        {
            if (timingOffsetMs < -HeadingMechanicsConstants.EarlyLabelThresholdMs)
            {
                return ContactQualityLabel.Early;
            }

            if (timingOffsetMs > HeadingMechanicsConstants.LateLabelThresholdMs)
            {
                return ContactQualityLabel.Late;
            }

            return ContactQualityLabel.OnTime;
        }

        private static float Clamp01(float v)
        {
            return v < 0.0f ? 0.0f : v > 1.0f ? 1.0f : v;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
