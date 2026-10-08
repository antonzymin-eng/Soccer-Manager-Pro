// File:     src/match-client-unity/S0UiConstants.cs
// Created:  2026-10-08
// Modified: 2026-10-08
// Author:   —
// Spec:     S0 binding contracts §3, Code Standards #20
// Purpose:  One tagged catalogue for the S0 Unity skin's neutral visual dimensions.

using UnityEngine;
using TacticalDirector.MatchClientCore;

namespace TacticalDirector.MatchClientUnity
{
    internal static class S0UiConstants
    {
        /// <summary>[FIXED] Base body text size, client pixels at 100%.</summary>
        internal const int TEXT_SIZE = 16;
        /// <summary>[FIXED] Base heading size, client pixels at 100%.</summary>
        internal const int HEADING_SIZE = 28;
        /// <summary>[FIXED] Control/content padding in client pixels.</summary>
        internal const int PADDING = 16;
        /// <summary>[FIXED] Visual separation in client pixels.</summary>
        internal const int GAP = 8;
        /// <summary>[FIXED] Render-target depth-buffer bits.</summary>
        internal const int TEXTURE_DEPTH_BITS = 24;
        /// <summary>[DERIVED] White substitute annulus radius between the marker and possession annulus, metres.</summary>
        internal static float SubstituteOutlineRadiusM => (MatchClientConstants.AgentMarkerRadiusM + MatchClientConstants.PossessionRingRadiusM) * 0.5f;

        /// <summary>[FIXED] Focus outline thickness in client pixels.</summary>
        internal const int FOCUS_WIDTH = 3;
        /// <summary>[FIXED] Most recent three feedback rows remain expanded.</summary>
        internal const int EXPANDED_FEEDBACK = 3;
        /// <summary>[FIXED] Maximum render-target side in pixels; presentation only.</summary>
        internal const int MAX_TEXTURE_SIDE = 2048;
        /// <summary>[FIXED] Dark body text on a white page; color-independent meaning comes from words.</summary>
        internal static readonly Color TEXT_COLOR = new Color(0.086f, 0.086f, 0.086f, 1);
        /// <summary>[FIXED] Neutral page background.</summary>
        internal static readonly Color PAGE_COLOR = Color.white;
        /// <summary>[FIXED] Unavailable control surface, retaining dark readable text.</summary>
        internal static readonly Color DISABLED_COLOR = new Color(0.93f, 0.93f, 0.93f, 1);
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Tagged visual-only skin catalogue. |
#endregion
