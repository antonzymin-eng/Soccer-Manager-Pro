// File:     src/goalkeeper-mechanics/GoalkeeperPositioningContract.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.3.0, KD-13, Code Standards #20
// Purpose:  Consumer contract façade for Positioning AI #12 GK baseline read.
//           Defines the reactive-position bounds around the baseline slot.

using UnityEngine;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Consumer contract façade for Positioning AI #12 §3.3.3 GK baseline read (KD-13).
    /// Holds the most-recently-read gkBaselineSlot and provides reactive-bound helpers.
    /// Spec #11 reserves micro-adjustment within GK_REACTIVE_RADIUS_M in Resting/Set states.
    /// Goalkeeper Mechanics #11 §3.3.0.
    /// </summary>
    public struct GoalkeeperPositioningContract
    {
        /// <summary>
        /// Latest GK baseline slot from Positioning AI #12 §3.3.3.
        /// Read-only at every 10 Hz tactical tick per §4.6.1 / KD-13.
        /// </summary>
        public Vector2 GkBaselineSlot;

        /// <summary>
        /// True if the given GK XY position is within GK_REACTIVE_RADIUS_M of the baseline slot.
        /// Used by §3.1 Recovering → Set transition check. §3.3.0.2.
        /// </summary>
        public bool IsWithinReactiveRadius(Vector2 gkPosition)
        {
            float distSq = (gkPosition - GkBaselineSlot).sqrMagnitude;
            float radiusSq = GoalkeeperConstants.GkReactiveRadiusM * GoalkeeperConstants.GkReactiveRadiusM;
            return distSq <= radiusSq;
        }

        /// <summary>
        /// Returns the clamped XY position for a micro-adjustment attempt.
        /// If the requested position exceeds GK_REACTIVE_RADIUS_M from the baseline, it is clamped to the circle boundary.
        /// Used during Resting/Set states where micro-adjustment is bounded per §3.3.0.2.
        /// </summary>
        /// <param name="requestedPosition">Requested XY position for the micro-adjustment. §3.3.0.2.</param>
        /// <returns>XY position clamped to the reactive circle centred on GkBaselineSlot.</returns>
        public Vector2 ClampToReactiveBounds(Vector2 requestedPosition)
        {
            Vector2 delta    = requestedPosition - GkBaselineSlot;
            float   sqLen    = delta.sqrMagnitude;
            float   radiusSq = GoalkeeperConstants.GkReactiveRadiusM * GoalkeeperConstants.GkReactiveRadiusM;

            if (sqLen <= radiusSq)
            {
                return requestedPosition;
            }

            if (sqLen < GoalkeeperConstants.DEGENERACY_EPSILON_SQ)
            {
                return GkBaselineSlot;
            }

            Vector2 dir = delta / Mathf.Sqrt(sqLen);
            return GkBaselineSlot + dir * GoalkeeperConstants.GkReactiveRadiusM;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
