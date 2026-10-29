// File:     src/match-client-unity/S0UiFactory.cs
// Created:  2026-10-08
// Modified: 2026-10-29 (PR #518 owner report: fixed-height reason box for the playback reasons)
// Modified (prior): 2026-10-21 (Gate J J-04: H v0.2 panels, control chrome, reason boxes and borders)
// Author:   —
// Spec:     S0 binding contracts §3, H v0.2 reference (docs/design/s0-high-fidelity/prototype.css),
//           Code Standards #20
// Purpose:  Build persistent wrapped/scrolled UGUI views; layout owns measured sizes rather than truncation.

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TacticalDirector.MatchClientUnity
{
    /// <summary>Local UGUI construction helper; contains visual binding, never gameplay/navigation decisions.</summary>
    internal sealed class S0UiFactory
    {
        /// <summary>Text roles of the H v0.2 type scale.</summary>
        internal enum TextStyle
        {
            Body,
            Heading,
            Subheading,
            Score,
            Strong,
            Secondary,
            Chip
        }

        /// <summary>Control emphasis: primary progression is blue-filled; everything else is raised.</summary>
        internal enum ControlKind
        {
            Secondary,
            Primary
        }

        private readonly Dictionary<Button, Text> _buttonText = new Dictionary<Button, Text>();
        private readonly Dictionary<Button, S0ControlSkin> _skins = new Dictionary<Button, S0ControlSkin>();
        private readonly Font _font;
        private readonly float _scale;
        private Sprite _solidBorder, _dashedBorder;
        internal S0UiFactory(Font font, float scale)
        {
            _font = font;
            _scale = scale;
        }

        /// <summary>The text scale every size here is multiplied by.</summary>
        internal float Scale => _scale;

        internal RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal RectTransform Column(string name, Transform parent, int padding = S0UiConstants.PADDING)
        {
            RectTransform node = Node(name, parent);
            var layout = node.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = S0UiConstants.GAP;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return node;
        }

        /// <summary>A left-to-right group whose children keep their preferred widths (H v0.2 .row).</summary>
        internal RectTransform Row(string name, Transform parent, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            RectTransform node = Node(name, parent);
            var layout = node.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = S0UiConstants.GAP;
            layout.childAlignment = alignment;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return node;
        }

        /// <summary>A full-screen canvas page whose content column is centred within <paramref name="maxWidth"/>.</summary>
        internal RectTransform Page(Transform parent, out ScrollRect scroll, int maxWidth = S0UiConstants.MATCH_MAX_WIDTH, int sortingOrder = 0)
        {
            RectTransform canvasRoot = Canvas(parent, sortingOrder);
            var image = canvasRoot.gameObject.AddComponent<Image>();
            image.color = S0UiConstants.PAGE_COLOR;
            RectTransform content = ScrollColumn(canvasRoot, canvasRoot, out scroll);
            content.gameObject.AddComponent<S0MaxWidth>().MaxWidth = maxWidth * _scale;
            return content;
        }

        /// <summary>A screen-space canvas root stretched over <paramref name="parent"/>.</summary>
        internal RectTransform Canvas(Transform parent, int sortingOrder)
        {
            RectTransform canvasRoot = Node("S0 canvas", parent);
            Stretch(canvasRoot);
            var canvas = canvasRoot.gameObject.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            // One-pixel borders and rules must land on whole pixels.
            canvas.pixelPerfect = true;
            var scaler = canvasRoot.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasRoot.gameObject.AddComponent<GraphicRaycaster>();
            return canvasRoot;
        }

        /// <summary>A vertically scrolling content column filling <paramref name="area"/>; the ScrollRect lives on <paramref name="host"/>.</summary>
        internal RectTransform ScrollColumn(RectTransform host, RectTransform area, out ScrollRect scroll)
        {
            RectTransform viewport = Node("Viewport", area);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll = host.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = S0UiConstants.TEXT_SIZE * 2 * _scale;
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

        /// <summary>A bordered surface column (H v0.2 .panel); <paramref name="rule"/> adds the blue top rule.</summary>
        internal RectTransform Panel(string name, Transform parent, bool rule = false, bool focusBorder = false)
        {
            RectTransform node = Column(name, parent);
            var fill = node.gameObject.AddComponent<Image>();
            fill.color = S0UiConstants.SURFACE_COLOR;
            fill.raycastTarget = false;
            Border(node, focusBorder ? S0UiConstants.FOCUS_COLOR : S0UiConstants.LINE_COLOR, false);
            if (rule)
            {
                RectTransform top = Overlay("Top rule", node);
                top.anchorMin = new Vector2(0, 1);
                top.anchorMax = Vector2.one;
                top.pivot = new Vector2(0.5f, 1);
                top.sizeDelta = new Vector2(0, S0UiConstants.ACCENT_BAR_WIDTH);
                top.anchoredPosition = Vector2.zero;
                var image = top.gameObject.AddComponent<Image>();
                image.color = S0UiConstants.RULE_COLOR;
                image.raycastTarget = false;
            }

            return node;
        }

        internal Text Label(Transform parent, string text, bool heading = false) => Label(parent, text, heading ? TextStyle.Heading : TextStyle.Body);

        internal Text Label(Transform parent, string text, TextStyle style)
        {
            RectTransform node = Node("Text", parent);
            var label = node.gameObject.AddComponent<Text>();
            label.font = _font;
            int size;
            switch (style)
            {
                case TextStyle.Heading:
                    size = S0UiConstants.HEADING_SIZE;
                    break;
                case TextStyle.Subheading:
                    size = S0UiConstants.SUBHEADING_SIZE;
                    break;
                case TextStyle.Score:
                    size = S0UiConstants.SCORE_SIZE;
                    break;
                case TextStyle.Chip:
                    size = S0UiConstants.CHIP_SIZE;
                    break;
                default:
                    size = S0UiConstants.TEXT_SIZE;
                    break;
            }

            label.fontSize = Mathf.RoundToInt(size * _scale);
            label.fontStyle = style == TextStyle.Body || style == TextStyle.Secondary ? FontStyle.Normal : FontStyle.Bold;
            label.color = style == TextStyle.Secondary ? S0UiConstants.SECONDARY_COLOR : S0UiConstants.TEXT_COLOR;
            label.lineSpacing = 1.1f;
            label.supportRichText = false;
            label.resizeTextForBestFit = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        /// <summary>
        /// A persistent reason/notice box (H v0.2 .reason / .warning): sunken fill, coloured left bar,
        /// wrapped text. <see cref="SetBoxed"/> hides the whole box while its text is empty.
        /// </summary>
        internal Text Reason(Transform parent, bool warning = false)
        {
            RectTransform box = Row("Reason", parent, TextAnchor.UpperLeft);
            var layout = box.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, S0UiConstants.GAP + S0UiConstants.GAP / 2, S0UiConstants.GAP, S0UiConstants.GAP);
            var fill = box.gameObject.AddComponent<Image>();
            fill.color = S0UiConstants.SUNKEN_COLOR;
            fill.raycastTarget = false;
            if (warning)
                Border(box, S0UiConstants.FAILURE_COLOR, true);
            Bar(box, warning ? S0UiConstants.FAILURE_COLOR : S0UiConstants.FOCUS_COLOR);
            Text text = Label(box, "");
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            box.gameObject.SetActive(false);
            return text;
        }

        /// <summary>
        /// A <see cref="Reason"/> box that is always laid out at the height of its tallest
        /// <paramref name="candidates"/> text and only blanks while empty (<see cref="S0ReservedReason"/>).
        /// </summary>
        internal S0ReservedReason ReservedReason(Transform parent, params string[] candidates)
        {
            Text text = Reason(parent);
            GameObject box = text.transform.parent.gameObject;
            var reserved = text.gameObject.AddComponent<S0ReservedReason>();
            reserved.Bind(text, candidates, box.GetComponentsInChildren<Image>(true));
            box.SetActive(true);
            reserved.Set("");
            return reserved;
        }

        /// <summary>A fixed-width accent bar as the first child of a row.</summary>
        internal Image Bar(Transform row, Color color)
        {
            RectTransform bar = Node("Accent bar", row);
            var size = bar.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = S0UiConstants.ACCENT_BAR_WIDTH;
            size.flexibleHeight = 1;
            var image = bar.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        internal Button Button(Transform parent, string label, UnityAction action, ControlKind kind = ControlKind.Secondary)
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
            layout.padding = new RectOffset(S0UiConstants.PADDING, S0UiConstants.PADDING, S0UiConstants.GAP + S0UiConstants.GAP / 2, S0UiConstants.GAP + S0UiConstants.GAP / 2);
            layout.childControlHeight = layout.childControlWidth = true;
            // Not force-expanded: a forced child makes the control itself flexible, which would
            // stretch every button in a row (playback, dialog actions) across the whole width.
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = S0UiConstants.GAP / 2;
            Text text = Label(node, label, TextStyle.Strong);
            _buttonText.Add(button, text);
            button.onClick.AddListener(action);
            bool primary = kind == ControlKind.Primary;
            ColorBlock colors = button.colors;
            colors.normalColor = colors.selectedColor = primary ? S0UiConstants.PRIMARY_COLOR : S0UiConstants.RAISED_COLOR;
            colors.highlightedColor = primary ? S0UiConstants.PRIMARY_HOVER_COLOR : S0UiConstants.HOVER_COLOR;
            colors.pressedColor = S0UiConstants.PRESSED_COLOR;
            colors.disabledColor = S0UiConstants.SUNKEN_COLOR;
            colors.colorMultiplier = 1;
            colors.fadeDuration = 0;
            button.colors = colors;
            var skin = node.gameObject.AddComponent<S0ControlSkin>();
            skin.Initialize(button, text, Border(node, S0UiConstants.LINE_COLOR, false), Border(node, S0UiConstants.LINE_COLOR, true), primary);
            _skins.Add(button, skin);
            return button;
        }

        /// <summary>Marks a choice selected with the gold outline (H v0.2 checked choice).</summary>
        internal void SetSelected(Button button, bool selected) => _skins[button].Selected = selected;

        /// <summary>A one-pixel border overlay; dashed borders mark unavailable controls (H v0.2).</summary>
        internal Image Border(RectTransform target, Color color, bool dashed)
        {
            RectTransform node = Overlay(dashed ? "Dashed border" : "Border", target);
            Stretch(node);
            var image = node.gameObject.AddComponent<Image>();
            image.sprite = dashed ? DashedBorder() : SolidBorder();
            image.type = dashed ? Image.Type.Tiled : Image.Type.Sliced;
            image.fillCenter = false;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A child excluded from its parent's layout group (borders, rules, focus rings).</summary>
        internal RectTransform Overlay(string name, Transform parent)
        {
            RectTransform node = Node(name, parent);
            node.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return node;
        }

        private Sprite SolidBorder()
        {
            if (_solidBorder == null)
                _solidBorder = BorderSprite(3, _ => true);
            return _solidBorder;
        }

        // A 2-on/2-off dash along every edge. The inner 8 pixels tile, so dashes stay regular at any size.
        private Sprite DashedBorder()
        {
            if (_dashedBorder == null)
                _dashedBorder = BorderSprite(10, i => i == 0 || i == 9 || (i - 1) % 4 < 2);
            return _dashedBorder;
        }

        private static Sprite BorderSprite(int side, Func<int, bool> on)
        {
            var texture = new Texture2D(side, side, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            var pixels = new Color32[side * side];
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    bool edge = x == 0 || y == 0 || x == side - 1 || y == side - 1;
                    bool lit = edge && on(x == 0 || x == side - 1 ? y : x);
                    pixels[y * side + x] = lit ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, side, side), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(1, 1, 1, 1));
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
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

        /// <summary>Sets a <see cref="Reason"/> text and shows its box only while it has text.</summary>
        internal static void SetBoxed(Text label, string text)
        {
            Set(label, text);
            GameObject box = label.transform.parent.gameObject;
            bool show = !string.IsNullOrEmpty(text);
            if (box.activeSelf != show)
                box.SetActive(show);
        }

        internal void SetButton(Button button, string text) => Set(_buttonText[button], text);
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Persistent wrapped/scrolled neutral UGUI construction. |
// | 1.1     | 2026-10-08 | —      | Retain button text references at construction instead of repeated component searches. |
// | 1.2     | 2026-10-21 | —      | Gate J J-04: H v0.2 skin — navy page with centred max-width column, bordered panels with optional blue rule, raised/primary control chrome with dashed unavailable borders and gold selection (S0ControlSkin), reason/warning boxes hidden while empty, text style scale, generated pixel-perfect border sprites. |
// | 1.3     | 2026-10-29 | —      | PR #518 owner report: `ReservedReason` builds a reason box that keeps the height of its tallest candidate text and blanks while empty, so a reason appearing never moves the content below it. |
#endregion
