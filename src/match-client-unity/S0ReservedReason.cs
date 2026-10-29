// File:     src/match-client-unity/S0ReservedReason.cs
// Created:  2026-10-29
// Modified: 2026-10-29
// Author:   —
// Spec:     S0 binding contracts §3, Code Standards #20
// Purpose:  PR #518 owner report: a reason box that keeps its height while empty, so the playback reasons
//           appearing or clearing never resize the page-height-sized pitch below them.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TacticalDirector.MatchClientUnity
{
    /// <summary>
    /// Sits on a <see cref="S0UiFactory.Reason"/> label. Its height is the tallest of every text the box can show,
    /// measured at the label's current width and text scale, and an empty box hides its chrome instead of
    /// deactivating, so content below it never moves.
    /// </summary>
    [RequireComponent(typeof(Text))]
    internal sealed class S0ReservedReason : UIBehaviour, ILayoutElement
    {
        private Text _label;
        private string[] _candidates;
        private Image[] _chrome;

        internal void Bind(Text label, string[] candidates, Image[] chrome)
        {
            _label = label;
            _candidates = candidates;
            _chrome = chrome;
        }

        /// <summary>Shows <paramref name="text"/>, or blanks the box while it is empty; the height never changes.</summary>
        internal void Set(string text)
        {
            S0UiFactory.Set(_label, text);
            bool show = !string.IsNullOrEmpty(text);
            for (int i = 0; i < _chrome.Length; i++)
                if (_chrome[i].enabled != show)
                    _chrome[i].enabled = show;
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
        public float minWidth => -1;
        /// <inheritdoc/>
        public float preferredWidth => -1;
        /// <inheritdoc/>
        public float flexibleWidth => -1;
        /// <inheritdoc/>
        public float minHeight => preferredHeight;
        /// <inheritdoc/>
        public float preferredHeight
        {
            get
            {
                if (_label == null)
                    return -1;
                // The same measurement Text.preferredHeight makes, over every candidate rather than the current text.
                TextGenerationSettings settings = _label.GetGenerationSettings(new Vector2(_label.GetPixelAdjustedRect().size.x, 0f));
                TextGenerator generator = _label.cachedTextGeneratorForLayout;
                float height = generator.GetPreferredHeight(_label.text, settings);
                for (int i = 0; i < _candidates.Length; i++)
                    height = Mathf.Max(height, generator.GetPreferredHeight(_candidates[i], settings));
                return height / _label.pixelsPerUnit;
            }
        }
        /// <inheritdoc/>
        public float flexibleHeight => -1;
        /// <inheritdoc/>
        public int layoutPriority => 1;
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-29 | —      | Fixed-height reason box for the playback reasons (PR #518 owner report: the pitch resized on the first and last Faster). |
#endregion
