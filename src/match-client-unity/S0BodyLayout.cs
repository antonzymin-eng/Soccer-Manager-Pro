// File:     src/match-client-unity/S0BodyLayout.cs
// Created:  2026-10-08
// Modified: 2026-10-21 (Gate J J-04: H v0.2 rail width; pitch sized from the page height)
// Modified (prior): 2026-10-08
// Author:   —
// Spec:     S0 binding contracts §3, Code Standards #20
// Purpose:  Apply the host-free measured columns/stack decision to the live pitch and changes rail.

using UnityEngine;
using UnityEngine.UI;
using TacticalDirector.ClientApp;

namespace TacticalDirector.MatchClientUnity
{
    /// <summary>UGUI geometry binding; full text metrics decide minimums, never font autoshrink.</summary>
    internal sealed class S0BodyLayout : LayoutGroup
    {
        internal Text PitchMeasure;
        internal Text RailMeasure;
        internal ScrollRect PageScroll;
        private float _pitchMinimum, _railMinimum;
        private bool IsColumns => S0ScreenLayout.CanUseColumns(rectTransform.rect.width, _pitchMinimum, _railMinimum, S0UiConstants.GAP);

        /// <inheritdoc/>
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            _pitchMinimum = PitchMeasure.preferredWidth + S0UiConstants.PADDING * 2;
            _railMinimum = RailMeasure.preferredWidth + S0UiConstants.PADDING * 2;
            SetLayoutInputForAxis(Mathf.Max(_pitchMinimum, _railMinimum), _pitchMinimum + _railMinimum + S0UiConstants.GAP, 1, 0);
        }

        /// <inheritdoc/>
        public override void CalculateLayoutInputVertical()
        {
            if (rectChildren.Count != 2)
                return;
            float pitch = LayoutUtility.GetPreferredHeight(rectChildren[0]);
            float rail = LayoutUtility.GetPreferredHeight(rectChildren[1]);
            float height = IsColumns ? Mathf.Max(pitch, rail) : pitch + rail + S0UiConstants.GAP;
            SetLayoutInputForAxis(height, height, 0, 1);
        }

        /// <inheritdoc/>
        public override void SetLayoutHorizontal()
        {
            if (rectChildren.Count != 2)
                return;
            float width = rectTransform.rect.width;
            if (!IsColumns)
            {
                SetChildAlongAxis(rectChildren[0], 0, 0, width);
                SetChildAlongAxis(rectChildren[1], 0, 0, width);
                return;
            }

            // H v0.2: the rail keeps its own width (27rem at 100% text); the pitch takes the rest up to
            // the width at which the whole pitch fits the page height; leftover space is split evenly.
            float scale = RailMeasure.fontSize / (float)S0UiConstants.TEXT_SIZE;
            float railTarget = Mathf.Max(_railMinimum, S0UiConstants.RAIL_WIDTH * scale);
            float pitch = Mathf.Max(_pitchMinimum, Mathf.Min(width - railTarget - S0UiConstants.GAP, WholePitchWidth()));
            float rail = Mathf.Max(_railMinimum, Mathf.Min(width - pitch - S0UiConstants.GAP, railTarget));
            float left = Mathf.Max(0, (width - pitch - S0UiConstants.GAP - rail) * 0.5f);
            SetChildAlongAxis(rectChildren[0], 0, left, pitch);
            SetChildAlongAxis(rectChildren[1], 0, left + pitch + S0UiConstants.GAP, rail);
        }

        private float WholePitchWidth()
        {
            if (PageScroll == null)
                return float.PositiveInfinity;
            float available = PageScroll.viewport.rect.height;
            VerticalLayoutGroup contentLayout = PageScroll.content.GetComponent<VerticalLayoutGroup>();
            available -= contentLayout.padding.vertical;
            int count = 0;
            foreach (Transform child in PageScroll.content)
            {
                if (!child.gameObject.activeSelf)
                    continue;
                count++;
                if (child != transform)
                    available -= LayoutUtility.GetPreferredHeight((RectTransform)child);
            }

            available -= Mathf.Max(0, count - 1) * contentLayout.spacing;
            VerticalLayoutGroup pitchLayout = rectChildren[0].GetComponent<VerticalLayoutGroup>();
            available -= pitchLayout.padding.vertical;
            count = 0;
            foreach (Transform child in rectChildren[0])
            {
                if (!child.gameObject.activeSelf)
                    continue;
                count++;
                if (child.GetComponent<S0PitchLayout>() == null)
                    available -= LayoutUtility.GetPreferredHeight((RectTransform)child);
            }

            available -= Mathf.Max(0, count - 1) * pitchLayout.spacing;
            // If the viewport is too short, retain the measured minimum and let the page scroll.
            return Mathf.Max(_pitchMinimum, available * TacticalDirector.MatchEngine.MatchEngineConstants.PITCH_LENGTH_M / TacticalDirector.MatchEngine.MatchEngineConstants.PITCH_WIDTH_M);
        }

        /// <inheritdoc/>
        public override void SetLayoutVertical()
        {
            if (rectChildren.Count != 2)
                return;
            float pitch = LayoutUtility.GetPreferredHeight(rectChildren[0]);
            float rail = LayoutUtility.GetPreferredHeight(rectChildren[1]);
            SetChildAlongAxis(rectChildren[0], 1, 0, pitch);
            SetChildAlongAxis(rectChildren[1], 1, IsColumns ? 0 : pitch + S0UiConstants.GAP, rail);
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Consumed measured pitch/rail reflow. |
// | 1.1     | 2026-10-21 | —      | Gate J J-04: the rail keeps the H v0.2 width and the leftover is centred; statistics now live in the rail, so the page-height pitch bound no longer shrinks when they open. |
#endregion
