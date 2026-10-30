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
    /// <summary>Scrollable labels own the surface height; its child image always keeps the true pitch aspect.</summary>
    internal sealed class S0PitchLayout : UIBehaviour, ILayoutElement, ILayoutSelfController
    {
        internal RectTransform Image;
        internal float LabelHeight;

        /// <inheritdoc/>
        public void SetLayoutHorizontal()
        {
            Image.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ((RectTransform)transform).rect.width);
        }

        /// <inheritdoc/>
        public void SetLayoutVertical()
        {
            Image.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                ((RectTransform)transform).rect.width * MatchEngineConstants.PITCH_WIDTH_M / MatchEngineConstants.PITCH_LENGTH_M);
        }

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
        public float preferredHeight => Mathf.Max(LabelHeight, ((RectTransform)transform).rect.width * MatchEngineConstants.PITCH_WIDTH_M / MatchEngineConstants.PITCH_LENGTH_M);
        /// <inheritdoc/>
        public float flexibleHeight => 0;
        /// <inheritdoc/>
        public int layoutPriority => 1;
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Width-derived pitch geometry in the parent layout pass. |
// | 1.1     | 2026-10-08 | —      | Keep the image aspect within a vertically growable full-label surface. |
#endregion
