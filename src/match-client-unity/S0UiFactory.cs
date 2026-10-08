// File:     src/match-client-unity/S0UiFactory.cs
// Created:  2026-10-08
// Modified: 2026-10-08
// Author:   —
// Spec:     S0 binding contracts §3, Code Standards #20
// Purpose:  Build persistent wrapped/scrolled UGUI views; layout owns measured sizes rather than truncation.

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TacticalDirector.MatchClientUnity
{
    /// <summary>Local UGUI construction helper; contains visual binding, never gameplay/navigation decisions.</summary>
    internal sealed class S0UiFactory
    {
        private readonly Font _font;
        private readonly float _scale;
        internal S0UiFactory(Font font, float scale)
        {
            _font = font;
            _scale = scale;
        }

        internal RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal RectTransform Column(string name, Transform parent)
        {
            RectTransform node = Node(name, parent);
            var layout = node.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = S0UiConstants.GAP;
            layout.padding = new RectOffset(S0UiConstants.PADDING, S0UiConstants.PADDING, S0UiConstants.PADDING, S0UiConstants.PADDING);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return node;
        }

        internal RectTransform Page(Transform parent, out ScrollRect scroll)
        {
            RectTransform canvasRoot = Node("S0 canvas", parent);
            Stretch(canvasRoot);
            var canvas = canvasRoot.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasRoot.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasRoot.gameObject.AddComponent<GraphicRaycaster>();
            var image = canvasRoot.gameObject.AddComponent<Image>();
            image.color = S0UiConstants.PAGE_COLOR;
            RectTransform viewport = Node("Viewport", canvasRoot);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll = canvasRoot.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            RectTransform content = Column("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            return content;
        }

        internal Text Label(Transform parent, string text, bool heading = false)
        {
            RectTransform node = Node("Text", parent);
            var label = node.gameObject.AddComponent<Text>();
            label.font = _font;
            label.fontSize = Mathf.RoundToInt((heading ? S0UiConstants.HEADING_SIZE : S0UiConstants.TEXT_SIZE) * _scale);
            label.color = S0UiConstants.TEXT_COLOR;
            label.supportRichText = false;
            label.resizeTextForBestFit = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        internal Button Button(Transform parent, string label, UnityAction action)
        {
            RectTransform node = Node("Control", parent);
            var image = node.gameObject.AddComponent<Image>();
            image.color = Color.white;
            var button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            var layout = node.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(S0UiConstants.PADDING, S0UiConstants.PADDING, S0UiConstants.GAP, S0UiConstants.GAP);
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Label(node, label);
            button.onClick.AddListener(action);
            ColorBlock colors = button.colors;
            colors.normalColor = colors.highlightedColor = colors.selectedColor = colors.pressedColor = Color.white;
            colors.disabledColor = S0UiConstants.DISABLED_COLOR;
            button.colors = colors;
            return button;
        }

        internal static void Stretch(RectTransform node)
        {
            node.anchorMin = Vector2.zero;
            node.anchorMax = Vector2.one;
            node.offsetMin = node.offsetMax = Vector2.zero;
        }

        internal static void Set(Text label, string text)
        {
            if (label.text != text)
                label.text = text;
        }

        internal static void Set(Button button, string text) => Set(button.GetComponentInChildren<Text>(true), text);
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Persistent wrapped/scrolled neutral UGUI construction. |
#endregion
