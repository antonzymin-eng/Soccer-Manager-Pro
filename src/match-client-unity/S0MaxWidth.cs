// File:     src/match-client-unity/S0MaxWidth.cs
// Created:  2026-10-21
// Modified: 2026-10-21
// Author:   —
// Spec:     S0 binding contracts §3, H v0.2 reference (main / .entry max widths), Code Standards #20
// Purpose:  Gate J J-04: centre a page column within a maximum width by widening its side padding.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TacticalDirector.MatchClientUnity
{
    /// <summary>
    /// Keeps a full-width VerticalLayoutGroup's content no wider than <see cref="MaxWidth"/>, centred.
    /// Narrower screens keep the ordinary padding, so text still wraps rather than truncates.
    /// </summary>
    [RequireComponent(typeof(VerticalLayoutGroup))]
    internal sealed class S0MaxWidth : UIBehaviour
    {
        internal float MaxWidth = float.PositiveInfinity;

        private VerticalLayoutGroup _layout;

        protected override void OnEnable()
        {
            base.OnEnable();
            Apply();
        }

        // Polled outside the layout rebuild: changing padding from a dimension callback can land
        // inside a rebuild loop, which UGUI rejects.
        private void LateUpdate() => Apply();

        private void Apply()
        {
            if (_layout == null)
                _layout = GetComponent<VerticalLayoutGroup>();
            VerticalLayoutGroup layout = _layout;
            float width = ((RectTransform)transform).rect.width;
            int side = Mathf.Max(S0UiConstants.PADDING, Mathf.FloorToInt((width - MaxWidth) * 0.5f));
            if (layout.padding.left == side && layout.padding.right == side)
                return;
            layout.padding = new RectOffset(side, side, layout.padding.top, layout.padding.bottom);
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-21 | —      | Gate J J-04: centred maximum-width page column. |
#endregion
