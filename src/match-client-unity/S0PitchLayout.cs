// File:     src/match-client-unity/S0PitchLayout.cs
// Created:  2026-10-08
// Modified: 2026-10-08
// Author:   —
// Spec:     S0 binding contracts §3, Code Standards #20
// Purpose:  Preserve the pitch aspect through UGUI's horizontal-then-vertical layout pass.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchClientUnity
{
    /// <summary>The parent layout controls both axes; no competing AspectRatioFitter drives the rectangle.</summary>
    internal sealed class S0PitchLayout : UIBehaviour, ILayoutElement
    {
        /// <inheritdoc/>
        public void CalculateLayoutInputHorizontal()
        {
        }

        /// <inheritdoc/>
        public void CalculateLayoutInputVertical()
        {
        }

        /// <inheritdoc/>
        public float minWidth => 0;
        /// <inheritdoc/>
        public float preferredWidth => 0;
        /// <inheritdoc/>
        public float flexibleWidth => 1;
        /// <inheritdoc/>
        public float minHeight => preferredHeight;
        /// <inheritdoc/>
        public float preferredHeight => ((RectTransform)transform).rect.width * MatchEngineConstants.PITCH_WIDTH_M / MatchEngineConstants.PITCH_LENGTH_M;
        /// <inheritdoc/>
        public float flexibleHeight => 0;
        /// <inheritdoc/>
        public int layoutPriority => 1;
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Width-derived pitch geometry in the parent layout pass. |
#endregion
