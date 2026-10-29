// File:     src/match-client-unity/S0ControlSkin.cs
// Created:  2026-10-21
// Modified: 2026-10-21
// Author:   —
// Spec:     S0 binding contracts §3, H v0.2 reference (prototype.css button rules), Code Standards #20
// Purpose:  Gate J J-04: keep one control's border and text in step with its availability and selection.

using UnityEngine;
using UnityEngine.UI;

namespace TacticalDirector.MatchClientUnity
{
    /// <summary>
    /// Presentation only. Available controls carry a solid border (blue for primary, gold when
    /// selected); unavailable controls switch to a dashed border and secondary text, as in H v0.2.
    /// Re-skins only when availability or selection changes.
    /// </summary>
    internal sealed class S0ControlSkin : MonoBehaviour
    {
        private Button _button;
        private Text _text;
        private Image _solid, _dashed;
        private bool _primary, _selected, _applied, _lastAvailable, _lastSelected;

        internal bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                Apply();
            }
        }

        internal void Initialize(Button button, Text text, Image solid, Image dashed, bool primary)
        {
            _button = button;
            _text = text;
            _solid = solid;
            _dashed = dashed;
            _primary = primary;
            Apply();
        }

        private void LateUpdate() => Apply();

        private void Apply()
        {
            if (_button == null)
                return;
            bool available = _button.IsInteractable();
            if (_applied && available == _lastAvailable && _selected == _lastSelected)
                return;
            _applied = true;
            _lastAvailable = available;
            _lastSelected = _selected;
            _solid.enabled = available;
            _dashed.enabled = !available;
            _solid.color = _selected ? S0UiConstants.ACCENT_COLOR : _primary ? S0UiConstants.FOCUS_COLOR : S0UiConstants.LINE_COLOR;
            _text.color = available ? S0UiConstants.TEXT_COLOR : S0UiConstants.SECONDARY_COLOR;
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-21 | —      | Gate J J-04: availability/selection-driven control border and text skin. |
#endregion
