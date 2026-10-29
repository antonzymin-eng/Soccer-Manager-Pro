// File:     src/match-client-unity/S0UiConstants.cs
// Created:  2026-10-08
// Modified: 2026-10-21 (Gate J J-04: H v0.2 touchline / retro-blue skin)
// Modified (prior): 2026-10-08
// Author:   —
// Spec:     S0 binding contracts §3, H v0.2 reference (docs/design/s0-high-fidelity/prototype.css),
//           art direction v1, Code Standards #20
// Purpose:  One tagged catalogue for the S0 Unity skin's visual dimensions and colours.

using UnityEngine;
using TacticalDirector.MatchClientCore;

namespace TacticalDirector.MatchClientUnity
{
    internal static class S0UiConstants
    {
        /// <summary>[FIXED] Base body text size, client pixels at 100%.</summary>
        internal const int TEXT_SIZE = 16;
        /// <summary>[FIXED] Base page heading size, client pixels at 100%.</summary>
        internal const int HEADING_SIZE = 28;
        /// <summary>[FIXED] Panel heading size, client pixels at 100% (H v0.2 h2, 1.05rem).</summary>
        internal const int SUBHEADING_SIZE = 17;
        /// <summary>[FIXED] Scoreline size, client pixels at 100% (H v0.2 .score, 2.5rem).</summary>
        internal const int SCORE_SIZE = 40;
        /// <summary>[FIXED] Pitch shirt chip and outcome badge text size, client pixels at 100% (H v0.2 .agent, .8rem).</summary>
        internal const int CHIP_SIZE = 13;
        /// <summary>[FIXED] Control/content padding in client pixels.</summary>
        internal const int PADDING = 16;
        /// <summary>[FIXED] Visual separation in client pixels.</summary>
        internal const int GAP = 8;
        /// <summary>[FIXED] Reason/outcome accent bar width in client pixels (H v0.2 3px left border).</summary>
        internal const int ACCENT_BAR_WIDTH = 3;
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
        /// <summary>[FIXED] Entry-page column limit at 100% text (H v0.2 .entry, 55rem).</summary>
        internal const int ENTRY_MAX_WIDTH = 880;
        /// <summary>[FIXED] Match View page limit at 100% text (H v0.2 main, 112rem).</summary>
        internal const int MATCH_MAX_WIDTH = 1792;
        /// <summary>[FIXED] Changes/statistics rail width at 100% text (H v0.2 --rail, 27rem).</summary>
        internal const int RAIL_WIDTH = 432;
        /// <summary>[FIXED] Staging dialog width limit at 100% text (H v0.2 dialog, 46rem).</summary>
        internal const int DIALOG_MAX_WIDTH = 736;
        /// <summary>[FIXED] Dialog share of the screen width (H v0.2 95vw).</summary>
        internal const float DIALOG_WIDTH_SHARE = 0.95f;
        /// <summary>[FIXED] Dialog share of the screen height (H v0.2 90vh).</summary>
        internal const float DIALOG_HEIGHT_SHARE = 0.9f;

        // H v0.2 palette (prototype.css :root and component rules). Meaning is carried by words;
        // colour and glyphs only reinforce it.
        /// <summary>[FIXED] Page background (#061320).</summary>
        internal static readonly Color PAGE_COLOR = Hex(0x061320);
        /// <summary>[FIXED] Panel surface (#0b2238).</summary>
        internal static readonly Color SURFACE_COLOR = Hex(0x0b2238);
        /// <summary>[FIXED] Raised control surface (#0d2e4c).</summary>
        internal static readonly Color RAISED_COLOR = Hex(0x0d2e4c);
        /// <summary>[FIXED] Hovered control surface (#113c62).</summary>
        internal static readonly Color HOVER_COLOR = Hex(0x113c62);
        /// <summary>[FIXED] Pressed control surface (#164d79).</summary>
        internal static readonly Color PRESSED_COLOR = Hex(0x164d79);
        /// <summary>[FIXED] Primary action surface (#125590).</summary>
        internal static readonly Color PRIMARY_COLOR = Hex(0x125590);
        /// <summary>[FIXED] Primary action hover surface (#0d416f); also the home shirt chip.</summary>
        internal static readonly Color PRIMARY_HOVER_COLOR = Hex(0x0d416f);
        /// <summary>[FIXED] Unavailable control, reason and outcome surface (#071a2b).</summary>
        internal static readonly Color SUNKEN_COLOR = Hex(0x071a2b);
        /// <summary>[FIXED] Alternate statistics row (#081c2e).</summary>
        internal static readonly Color ZEBRA_COLOR = Hex(0x081c2e);
        /// <summary>[FIXED] Borders and rules (#607995).</summary>
        internal static readonly Color LINE_COLOR = Hex(0x607995);
        /// <summary>[FIXED] Cream body text (#f2f6ee).</summary>
        internal static readonly Color TEXT_COLOR = Hex(0xf2f6ee);
        /// <summary>[FIXED] Secondary text, unavailable text and pitch leaders (#bdccdc).</summary>
        internal static readonly Color SECONDARY_COLOR = Hex(0xbdccdc);
        /// <summary>[FIXED] Gold selection and away shirt chip (#f0cc4a).</summary>
        internal static readonly Color ACCENT_COLOR = Hex(0xf0cc4a);
        /// <summary>[FIXED] Keyboard focus, pending outcome and reason accent (#9ed6ff).</summary>
        internal static readonly Color FOCUS_COLOR = Hex(0x9ed6ff);
        /// <summary>[FIXED] Dialog/menu top rule (#2d7dc3).</summary>
        internal static readonly Color RULE_COLOR = Hex(0x2d7dc3);
        /// <summary>[FIXED] Applied outcome (#2ee572).</summary>
        internal static readonly Color SUCCESS_COLOR = Hex(0x2ee572);
        /// <summary>[FIXED] Refused / send-failure outcome and warnings (#ff9a9e).</summary>
        internal static readonly Color FAILURE_COLOR = Hex(0xff9a9e);
        /// <summary>[FIXED] Dark ink on light fills (#061320).</summary>
        internal static readonly Color INK_COLOR = Hex(0x061320);
        /// <summary>[FIXED] Dialog backdrop (#020b14 at 85%).</summary>
        internal static readonly Color BACKDROP_COLOR = new Color(0x02 / 255f, 0x0b / 255f, 0x14 / 255f, 0.85f);

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f, 1f);
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Tagged visual-only skin catalogue. |
// | 1.1     | 2026-10-21 | —      | Gate J J-04: H v0.2 touchline / retro-blue palette, panel/rail/dialog widths and text sizes replace the neutral white skin. |
#endregion
