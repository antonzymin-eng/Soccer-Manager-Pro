// File:     src/match-client-unity/S0ScreensBehaviour.cs
// Created:  2026-10-08
// Modified: 2026-10-28 (J-04 owner review: no repeated statistics caption; rail table fits and is revealed on open)
// Modified (prior): 2026-10-21
// Author:   —
// Spec:     P5b / S0 journey §14, binding contracts §§3–4, Code Standards #20
// Purpose:  Thin persistent UGUI binding for four localized screens, dialogs and accepted pitch labels.

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TacticalDirector.ClientApp;
using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.TacticalInstructions;
using TacticalDirector.UiFramework;

namespace TacticalDirector.MatchClientUnity
{
    /// <summary>Builds views once under the four authored roots. The shell owns lifecycle and this view's refresh.</summary>
    public sealed class S0ScreensBehaviour : MonoBehaviour
    {
        private static readonly ProfilerMarker RefreshMarker = new ProfilerMarker("S0ScreensBehaviour.Refresh");
        [SerializeField]
        private Font _font;
        [SerializeField]
        private Camera _matchCamera;
        [SerializeField]
        private float _textScale = 1f;
        private S0ScreenPresenter _view;
        private Action _applyVisibility;
        private S0UiFactory _ui;
        private readonly List<GameObject> _owned = new List<GameObject>();
        private readonly Dictionary<Selectable, GameObject> _focusRings = new Dictionary<Selectable, GameObject>();
        private readonly Dictionary<Selectable, ScrollRect> _scrolls = new Dictionary<Selectable, ScrollRect>();
        private readonly Dictionary<Selectable, S0FocusNavigation.Role> _focusRoles = new Dictionary<Selectable, S0FocusNavigation.Role>();
        private readonly Selectable[] _pitchControls = new Selectable[MatchEngineConstants.SQUAD_SIZE];
        private Predicate<Selectable> _isAllowed, _isTabStop;
        // Journey §9.3 MV-L/P order, then the report action and the single pitch inspection entry.
        private Selectable[] _matchTabOrder;
        private int _pitchEntryIndex = -1;
        private readonly Button[] _setupChoices = new Button[7];
        private readonly Button[] _mentalityChoices = new Button[7];
        private readonly Text[] _comparisonCurrent = new Text[7];
        private readonly Text[] _comparisonRequested = new Text[7];
        private readonly List<Button> _outgoing = new List<Button>();
        private readonly List<Button> _incoming = new List<Button>();
        private readonly List<Selectable> _feedbackRows = new List<Selectable>();
        private readonly List<RectTransform> _statRows = new List<RectTransform>();
        private readonly Dictionary<RectTransform, Text[]> _statText = new Dictionary<RectTransform, Text[]>();
        private readonly Dictionary<Selectable, Text> _feedbackText = new Dictionary<Selectable, Text>();
        private readonly Dictionary<Button, Text> _selectionText = new Dictionary<Button, Text>();
        private readonly List<Text> _pitchLabels = new List<Text>();
        private readonly RectTransform[] _pitchChips = new RectTransform[MatchEngineConstants.SQUAD_SIZE];
        private readonly Image[] _chipFill = new Image[MatchEngineConstants.SQUAD_SIZE];
        private readonly Image[] _chipBorder = new Image[MatchEngineConstants.SQUAD_SIZE];
        private readonly Text[] _incomingReason = new Text[MatchEngineConstants.SUBSTITUTES_PER_TEAM];
        private readonly Dictionary<Selectable, Image> _feedbackBar = new Dictionary<Selectable, Image>();
        private readonly Dictionary<Selectable, Image> _feedbackBadge = new Dictionary<Selectable, Image>();
        private readonly Dictionary<Selectable, Text> _feedbackGlyph = new Dictionary<Selectable, Text>();
        private RectTransform _dialogFrame, _dialogCanvas;
        private Text _scoreMentality, _matchLooseBall, _reportLooseBall;
        private ScrollRect _menuScroll, _setupScroll, _matchScroll, _reportScroll, _dialogScroll;
        private Selectable _menuHeading, _setupHeading, _matchHeading, _reportHeading, _dialogHeading;
        private Button _pause, _slower, _faster, _mentality, _substitution, _statisticsToggle, _report, _partialToggle, _earlierToggle;
        private Text _setupReady, _matchScore, _matchClock, _matchPeriod, _speed, _current, _used, _controlReason, _slowerReason, _fasterReason, _mentalityReason, _substitutionReason;
        private Text _notice, _caption, _reportScore, _reportResult, _reportNotice, _reportCaption, _dialogCurrent, _choiceNotice;
        private RectTransform _reportFeedback, _matchFeedback;
        private RectTransform _matchStatistics, _reportStatistics, _feedbackRoot, _earlierRoot, _statisticsPanel;
        private bool _revealStatistics;
        private GameObject _dialog, _mentalityDraft, _substitutionDraft, _compare;
        private Button _submit, _cancel;
        private Selectable _invoker;
        private RawImage _pitch;
        private RectTransform _pitchBounds;
        private S0PitchLayout _pitchLayout;
        private bool _pitchLayoutDirty = true;
        private int _screenWidth, _screenHeight;
        private float _measuredWidth;
        private readonly string[] _measuredText = new string[MatchEngineConstants.SQUAD_SIZE];
        private readonly bool[] _labelReserved = new bool[MatchEngineConstants.SQUAD_SIZE];
        private bool _layoutFailureReported;
        private RenderTexture _pitchTexture;
        private ScreenId _lastScreen;
        private bool _wasFullTime;
        private bool _keyboardFocus;
        private int _keyboardFrame = -1;
        private readonly Vector2[] _markerPoints = new Vector2[MatchEngineConstants.SQUAD_SIZE];
        private readonly Vector2[] _labelSizes = new Vector2[MatchEngineConstants.SQUAD_SIZE];
        private readonly Vector2[] _labelCentres = new Vector2[MatchEngineConstants.SQUAD_SIZE];
        private readonly bool[] _labelVisible = new bool[MatchEngineConstants.SQUAD_SIZE];
        private readonly RectTransform[] _leaderLines = new RectTransform[MatchEngineConstants.SQUAD_SIZE];
        private Text _pitchDescription, _pitchWaiting;
        private MatchClientBehaviour _renderer;
        private int _revision = -1;
        private bool _disposed;
        /// <summary>Actual admitted compiled role identity for host evidence; not a separately constructed fixture.</summary>
        public string LoadedContentSha256 => _view?.Text.ContentSha256;
        /// <summary>Host QA must treat a nonzero count as failed label-layout acceptance, without stopping play.</summary>
        public int PitchLayoutFailureCount { get; private set; }

        /// <summary>Called explicitly after the shell admits roots and creates its coordinator, before playback.</summary>
        public void Initialize(ClientMatchCoordinator coordinator, GameObject menu, GameObject setup, GameObject match, GameObject report, MatchClientBehaviour renderer, Action applyVisibility)
        {
            if (_view != null)
                throw new InvalidOperationException("S0 screen binding already initialized.");
            if (_matchCamera == null)
                throw new InvalidOperationException("S0 pitch camera is required.");
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
                throw new InvalidOperationException("S0 fallback font is unavailable.");
            _view = new S0ScreenPresenter(coordinator, S0TextFormatter.WithGlyphCoverage(_font.HasCharacter), new S0PresentationConfiguration(_textScale));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _applyVisibility = applyVisibility ?? throw new ArgumentNullException(nameof(applyVisibility));
            _isAllowed = IsAllowed;
            _isTabStop = IsTabStop;
            _ui = new S0UiFactory(_font, _view.Configuration.TextScale);
            // The S0 pitch image promises the whole pitch with both goals (I-C08), not the follow view.
            _renderer.WholePitchView = true;
            RectTransform m = _ui.Panel("Menu entry", Page(menu, out _menuScroll, S0UiConstants.ENTRY_MAX_WIDTH), true);
            _menuHeading = Heading(m, L("heading.menu"), _menuScroll);
            _ui.Label(m, L("context.menu"));
            Control(m, L("action.demo"), _view.OpenSetup, _menuScroll, S0UiFactory.ControlKind.Primary);
            RectTransform s = _ui.Panel("Setup entry", Page(setup, out _setupScroll, S0UiConstants.ENTRY_MAX_WIDTH));
            _setupHeading = Heading(s, L("heading.setup"), _setupScroll);
            _ui.Label(s, L("context.setup"));
            _ui.Label(s, L("context.mentality_choice"), S0UiFactory.TextStyle.Strong);
            for (int i = 0; i < S0ScreenPresenter.Mentalities.Count; i++)
            {
                Mentality value = S0ScreenPresenter.Mentalities[i];
                _setupChoices[i] = Control(s, _view.MentalityChoice(value), () => _view.SelectSetupMentality(value), _setupScroll);
            }

            _setupReady = _ui.Label(s, "");
            RectTransform setupActions = _ui.Row("Setup actions", s);
            Control(setupActions, L("action.start"), _view.StartMatch, _setupScroll, S0UiFactory.ControlKind.Primary);
            Control(setupActions, L("action.back"), _view.CancelSetup, _setupScroll);
            BuildMatch(Page(match, out _matchScroll));
            BuildReport(Page(report, out _reportScroll, S0UiConstants.ENTRY_MAX_WIDTH));
            _matchTabOrder = new Selectable[8 + _pitchControls.Length];
            _matchTabOrder[0] = _slower;
            _matchTabOrder[1] = _pause;
            _matchTabOrder[2] = _faster;
            _matchTabOrder[3] = _mentality;
            _matchTabOrder[4] = _substitution;
            _matchTabOrder[5] = _statisticsToggle;
            _matchTabOrder[6] = _earlierToggle;
            _matchTabOrder[7] = _report;
            Array.Copy(_pitchControls, 0, _matchTabOrder, 8, _pitchControls.Length);
            BuildDialog();
            var eventSystem = new GameObject("S0 EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(transform, false);
            _owned.Add(eventSystem);
            if (EventSystem.current != null && EventSystem.current != eventSystem.GetComponent<EventSystem>())
                throw new InvalidOperationException("S0 requires one scene EventSystem.");
            Refresh();
        }

        private string L(string role) => _view.Text.Label(role);
        private RectTransform Page(GameObject root, out ScrollRect scroll, int maxWidth = S0UiConstants.MATCH_MAX_WIDTH)
        {
            RectTransform content = _ui.Page(root.transform, out scroll, maxWidth);
            _owned.Add(scroll.gameObject);
            return content;
        }

        private Selectable Heading(Transform parent, string text, ScrollRect scroll, S0UiFactory.TextStyle style = S0UiFactory.TextStyle.Heading)
        {
            Text label = _ui.Label(parent, text, style);
            Selectable anchor = label.gameObject.AddComponent<Selectable>();
            anchor.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            anchor.transition = Selectable.Transition.None;
            Register(anchor, scroll);
            return anchor;
        }

        private Button Control(Transform parent, string text, Action action, ScrollRect scroll, S0UiFactory.ControlKind kind = S0UiFactory.ControlKind.Secondary)
        {
            Button button = _ui.Button(parent, text, () =>
            {
                try
                {
                    action();
                    _applyVisibility();
                    Refresh();
                }
                catch (Exception exception)
                {
                    _renderer.RejectPresentation(exception);
                }
            }, kind);
            Register(button, scroll, S0FocusNavigation.Role.Action);
            return button;
        }

        private void Register(Selectable control, ScrollRect scroll, S0FocusNavigation.Role role = S0FocusNavigation.Role.Anchor)
        {
            _scrolls.Add(control, scroll);
            _focusRoles.Add(control, role);
            RectTransform ring = _ui.Node("Keyboard focus", control.transform);
            // An overlay must not become content in the button's VerticalLayoutGroup.
            ring.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            S0UiFactory.Stretch(ring);
            // Four geometry strips: focus surrounds the control, never outlines individual text glyphs.
            for (int i = 0; i < 4; i++)
            {
                RectTransform edge = _ui.Node("Edge", ring);
                var image = edge.gameObject.AddComponent<Image>();
                image.color = S0UiConstants.FOCUS_COLOR;
                image.raycastTarget = false;
                if (i < 2)
                {
                    edge.anchorMin = new Vector2(0, i);
                    edge.anchorMax = new Vector2(1, i);
                    edge.sizeDelta = new Vector2(0, S0UiConstants.FOCUS_WIDTH);
                }
                else
                {
                    edge.anchorMin = new Vector2(i - 2, 0);
                    edge.anchorMax = new Vector2(i - 2, 1);
                    edge.sizeDelta = new Vector2(S0UiConstants.FOCUS_WIDTH, 0);
                }

                edge.anchoredPosition = Vector2.zero;
            }

            ring.gameObject.SetActive(false);
            _focusRings.Add(control, ring.gameObject);
        }

        private void BuildMatch(RectTransform parent)
        {
            _matchHeading = Heading(parent, L("heading.match"), _matchScroll);
            // Scoreboard (H v0.2): Home | score, period and minute, speed and Mentality | Away.
            RectTransform board = _ui.Panel("Scoreboard", parent);
            RectTransform sides = _ui.Row("Scoreboard row", board, TextAnchor.MiddleCenter);
            sides.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            Text home = _ui.Label(sides, L("team.home"), S0UiFactory.TextStyle.Subheading);
            home.alignment = TextAnchor.MiddleLeft;
            Flexible(home, 1);
            RectTransform centre = _ui.Column("Score block", sides, 0);
            Flexible(centre, 2);
            Text away = _ui.Label(sides, L("team.away"), S0UiFactory.TextStyle.Subheading);
            away.alignment = TextAnchor.MiddleRight;
            Flexible(away, 1);
            // A team identity never breaks mid-word: its column keeps at least its own width (narrow, 200%).
            home.GetComponent<LayoutElement>().minWidth = home.preferredWidth;
            away.GetComponent<LayoutElement>().minWidth = away.preferredWidth;
            _matchScore = _ui.Label(centre, "", S0UiFactory.TextStyle.Score);
            _matchScore.alignment = TextAnchor.UpperCenter;
            Pair(centre, out _matchPeriod, out _matchClock, S0UiFactory.TextStyle.Subheading);
            Pair(centre, out _speed, out _scoreMentality, S0UiFactory.TextStyle.Secondary);
            _speed.fontStyle = _scoreMentality.fontStyle = FontStyle.Bold;
            RectTransform playback = _ui.Row("Playback", parent);
            _slower = Control(playback, L("action.slower"), () =>
            {
                _view.Slower();
                if (!_view.CanSlower)
                    Focus(_pause);
            }, _matchScroll);
            _pause = Control(playback, L("action.pause"), _view.TogglePause, _matchScroll);
            _faster = Control(playback, L("action.faster"), () =>
            {
                _view.Faster();
                if (!_view.CanFaster)
                    Focus(_pause);
            }, _matchScroll);
            _slowerReason = _ui.Reason(parent);
            _fasterReason = _ui.Reason(parent);
            _controlReason = _ui.Reason(parent);
            _ui.Label(parent, L("context.speed_steps"), S0UiFactory.TextStyle.Secondary);
            RectTransform body = _ui.Node("Match body", parent);
            var bodyLayout = body.gameObject.AddComponent<S0BodyLayout>();
            bodyLayout.PageScroll = _matchScroll;
            RectTransform pitchColumn = _ui.Column("Pitch column", body, 0);
            RectTransform rail = _ui.Column("Changes rail", body, 0);
            Text direction = _ui.Label(pitchColumn, L("pitch.direction"), S0UiFactory.TextStyle.Secondary);
            direction.fontStyle = FontStyle.Bold;
            direction.alignment = TextAnchor.UpperRight;
            bodyLayout.PitchMeasure = direction;
            RectTransform pitch = _ui.Node("Pitch", pitchColumn);
            _pitchBounds = pitch;
            _pitchLayout = pitch.gameObject.AddComponent<S0PitchLayout>();
            RectTransform imageRect = _ui.Node("Pitch image", pitch);
            imageRect.anchorMin = imageRect.anchorMax = new Vector2(0.5f, 1);
            imageRect.pivot = new Vector2(0.5f, 1);
            imageRect.anchoredPosition = Vector2.zero;
            _pitchLayout.Image = imageRect;
            _pitch = imageRect.gameObject.AddComponent<RawImage>();
            _pitch.color = Color.white;
            _pitch.raycastTarget = false;
            _ui.Border(imageRect, S0UiConstants.LINE_COLOR, false);
            _pitchWaiting = _ui.Reason(pitchColumn);
            S0UiFactory.SetBoxed(_pitchWaiting, L("pitch.waiting"));
            _ui.Label(pitchColumn, L("pitch.legend"), S0UiFactory.TextStyle.Secondary);
            _pitchDescription = _ui.Label(pitchColumn, L("pitch.description"), S0UiFactory.TextStyle.Secondary);
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                // A shirt chip (H v0.2 .agent): filled tag, legible text, tethered to its marker.
                RectTransform chip = _ui.Node("Shirt chip", pitch);
                chip.sizeDelta = new Vector2(S0UiConstants.CHIP_SIZE * _textScale * 4, S0UiConstants.CHIP_SIZE * _textScale * 2);
                _pitchChips[i] = chip;
                _chipFill[i] = chip.gameObject.AddComponent<Image>();
                _chipFill[i].raycastTarget = true;
                _chipBorder[i] = _ui.Border(chip, S0UiConstants.FOCUS_COLOR, false);
                Text label = _ui.Label(chip, "", S0UiFactory.TextStyle.Chip);
                label.alignment = TextAnchor.MiddleCenter;
                S0UiFactory.Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(S0UiConstants.GAP / 2, 0);
                label.rectTransform.offsetMax = new Vector2(-S0UiConstants.GAP / 2, 0);
                _pitchLabels.Add(label);
                _labelReserved[i] = true;
                RectTransform line = _ui.Node("Marker tether", pitch);
                var image = line.gameObject.AddComponent<Image>();
                image.color = S0UiConstants.SECONDARY_COLOR;
                image.raycastTarget = false;
                line.pivot = new Vector2(0, 0.5f);
                _leaderLines[i] = line;
                int index = i;
                var hover = chip.gameObject.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry
                {
                    eventID = EventTriggerType.PointerEnter
                };
                entry.callback.AddListener(_ => S0UiFactory.Set(_pitchDescription, _view.PitchPlayerDescription(index)));
                hover.triggers.Add(entry);
                Selectable markerControl = chip.gameObject.AddComponent<Selectable>();
                markerControl.targetGraphic = _chipFill[i];
                markerControl.transition = Selectable.Transition.None;
                markerControl.navigation = new Navigation
                {
                    mode = Navigation.Mode.None
                };
                _pitchControls[i] = markerControl;
                Register(markerControl, _matchScroll, S0FocusNavigation.Role.PitchMarker);
                var selected = new EventTrigger.Entry
                {
                    eventID = EventTriggerType.Select
                };
                selected.callback.AddListener(_ => S0UiFactory.Set(_pitchDescription, _view.PitchPlayerDescription(index)));
                hover.triggers.Add(selected);
                var clicked = new EventTrigger.Entry
                {
                    eventID = EventTriggerType.PointerClick
                };
                clicked.callback.AddListener(_ => FocusPitch(index));
                hover.triggers.Add(clicked);
            }

            for (int i = 0; i < _leaderLines.Length; i++)
                _leaderLines[i].SetSiblingIndex(i + 1); // image, tethers, chips: tethers remain visible over the pitch.

            RectTransform changes = _ui.Panel("Home team changes", rail);
            Heading(changes, L("heading.changes"), _matchScroll, S0UiFactory.TextStyle.Subheading);
            _current = _ui.Label(changes, "");
            bodyLayout.RailMeasure = _current;
            _mentality = Control(changes, L("action.mentality"), () => OpenDialog(_mentality, _view.OpenMentality), _matchScroll);
            _mentalityReason = _ui.Reason(changes);
            _substitution = Control(changes, L("action.substitution"), () => OpenDialog(_substitution, _view.OpenSubstitution), _matchScroll);
            _used = _ui.Label(changes, "");
            _substitutionReason = _ui.Reason(changes);
            _matchFeedback = _ui.Column("Feedback region", changes, 0);
            Heading(_matchFeedback, L("heading.feedback"), _matchScroll, S0UiFactory.TextStyle.Subheading);
            _feedbackRoot = _ui.Column("Latest feedback", parent, 0);
            _earlierToggle = Control(_matchFeedback, "", _view.ToggleEarlierFeedback, _matchScroll);
            _earlierRoot = _ui.Column("Earlier feedback", parent, 0);
            _notice = _ui.Reason(rail, true);
            _statisticsPanel = _ui.Panel("Match statistics", rail);
            Heading(_statisticsPanel, L("heading.statistics"), _matchScroll, S0UiFactory.TextStyle.Subheading);
            _statisticsToggle = Control(_statisticsPanel, L("action.statistics_open"), () =>
            {
                _view.ToggleStatistics();
                _revealStatistics = _view.IsStatisticsOpen;
            }, _matchScroll);
            _matchStatistics = _ui.Column("Statistics", _statisticsPanel, 0);
            // Rail rows abut, separated by their rules, so the table fits beside the pitch at 1920×1080.
            _matchStatistics.GetComponent<VerticalLayoutGroup>().spacing = 0;
            _caption = _ui.Label(_matchStatistics, "", S0UiFactory.TextStyle.Secondary);
            _caption.fontStyle = FontStyle.Bold;
            _matchLooseBall = _ui.Label(_matchStatistics, L("statistics.loose_ball"));
            _report = Control(rail, L("action.report"), _view.ShowReport, _matchScroll, S0UiFactory.ControlKind.Primary);
        }

        private static void Flexible(Component item, float weight)
        {
            var size = item.gameObject.GetComponent<LayoutElement>();
            if (size == null)
                size = item.gameObject.AddComponent<LayoutElement>();
            size.flexibleWidth = weight;
            size.minWidth = 0;
        }

        // Two centred values sharing one line where width allows; each wraps within its half.
        private void Pair(Transform parent, out Text first, out Text second, S0UiFactory.TextStyle style)
        {
            RectTransform row = _ui.Row("Pair", parent, TextAnchor.UpperCenter);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.spacing = S0UiConstants.PADDING + S0UiConstants.GAP / 2;
            first = _ui.Label(row, "", style);
            first.alignment = TextAnchor.UpperRight;
            Flexible(first, 1);
            second = _ui.Label(row, "", style);
            second.alignment = TextAnchor.UpperLeft;
            Flexible(second, 1);
        }

        private void BuildReport(RectTransform page)
        {
            RectTransform parent = _ui.Panel("Report entry", page, true);
            _reportHeading = Heading(parent, L("heading.report"), _reportScroll);
            _reportResult = _ui.Label(parent, "", S0UiFactory.TextStyle.Subheading);
            _reportScore = _ui.Label(parent, "", S0UiFactory.TextStyle.Score);
            _reportNotice = _ui.Reason(parent, true);
            _partialToggle = Control(parent, L("action.partial"), _view.TogglePartialReport, _reportScroll);
            _reportStatistics = _ui.Column("Report statistics", parent, 0);
            _reportCaption = _ui.Label(_reportStatistics, "", S0UiFactory.TextStyle.Secondary);
            _reportCaption.fontStyle = FontStyle.Bold;
            _reportLooseBall = _ui.Label(_reportStatistics, L("statistics.loose_ball"));
            _reportFeedback = _ui.Column("Report feedback region", parent, 0);
            Heading(_reportFeedback, L("heading.feedback"), _reportScroll, S0UiFactory.TextStyle.Subheading);
            // Navigation is outside the partial disclosure and remains available through observer failure.
            Control(parent, L("action.return"), _view.ReturnToMenu, _reportScroll, S0UiFactory.ControlKind.Primary);
        }

        private void BuildDialog()
        {
            var root = new GameObject("S0 dialog", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            _owned.Add(root);
            // Modal staging (H v0.2 dialog): dimmed backdrop over the match, centred bordered frame.
            _dialogCanvas = _ui.Canvas(root.transform, 1);
            var backdrop = _dialogCanvas.gameObject.AddComponent<Image>();
            backdrop.color = S0UiConstants.BACKDROP_COLOR;
            _dialogFrame = _ui.Node("Dialog frame", _dialogCanvas);
            _dialogFrame.anchorMin = _dialogFrame.anchorMax = _dialogFrame.pivot = new Vector2(0.5f, 0.5f);
            var frameFill = _dialogFrame.gameObject.AddComponent<Image>();
            frameFill.color = S0UiConstants.SURFACE_COLOR;
            _ui.Border(_dialogFrame, S0UiConstants.FOCUS_COLOR, false);
            RectTransform rule = _ui.Overlay("Top rule", _dialogFrame);
            rule.anchorMin = new Vector2(0, 1);
            rule.anchorMax = Vector2.one;
            rule.pivot = new Vector2(0.5f, 1);
            rule.sizeDelta = new Vector2(0, S0UiConstants.ACCENT_BAR_WIDTH);
            rule.anchoredPosition = Vector2.zero;
            var ruleImage = rule.gameObject.AddComponent<Image>();
            ruleImage.color = S0UiConstants.RULE_COLOR;
            ruleImage.raycastTarget = false;
            RectTransform content = _ui.ScrollColumn(_dialogFrame, _dialogFrame, out _dialogScroll);
            _dialog = root;
            _dialogHeading = Heading(content, "", _dialogScroll, S0UiFactory.TextStyle.Subheading);
            _dialogCurrent = _ui.Label(content, "");
            _ui.Label(content, L("context.submit"), S0UiFactory.TextStyle.Secondary);
            _mentalityDraft = _ui.Column("Mentality draft", content, 0).gameObject;
            _ui.Label(_mentalityDraft.transform, L("field.requested_mentality"), S0UiFactory.TextStyle.Strong);
            for (int i = 0; i < S0ScreenPresenter.Mentalities.Count; i++)
            {
                Mentality value = S0ScreenPresenter.Mentalities[i];
                _mentalityChoices[i] = Control(_mentalityDraft.transform, _view.MentalityChoice(value), () => _view.SelectRequestedMentality(value), _dialogScroll);
            }

            Control(_mentalityDraft.transform, L("action.compare"), () => _compare.SetActive(!_compare.activeSelf), _dialogScroll);
            _compare = _ui.Column("Comparison", _mentalityDraft.transform, 0).gameObject;
            for (int i = 0; i < S0ScreenPresenter.Mentalities.Count; i++)
            {
                RectTransform row = _ui.Column("Comparison row", _compare.transform, S0UiConstants.GAP);
                _ui.Label(row, _view.MentalityChoice(S0ScreenPresenter.Mentalities[i]));
                _comparisonCurrent[i] = _ui.Label(row, L("tag.current"), S0UiFactory.TextStyle.Strong);
                _comparisonCurrent[i].color = S0UiConstants.SECONDARY_COLOR;
                _comparisonRequested[i] = _ui.Label(row, L("tag.requested"), S0UiFactory.TextStyle.Strong);
                _comparisonRequested[i].color = S0UiConstants.ACCENT_COLOR;
            }
            _compare.SetActive(false);
            _substitutionDraft = _ui.Column("Substitution draft", content, 0).gameObject;
            _ui.Label(_substitutionDraft.transform, L("context.substitution"));
            _ui.Label(_substitutionDraft.transform, L("field.outgoing"), S0UiFactory.TextStyle.Strong);
            for (int i = 0; i < MatchEngineConstants.PLAYERS_PER_TEAM; i++)
            {
                int index = i;
                _outgoing.Add(Control(_substitutionDraft.transform, "", () => _view.SelectOutgoing(_view.Outgoing[index].Index), _dialogScroll));
            }

            _ui.Label(_substitutionDraft.transform, L("field.incoming"), S0UiFactory.TextStyle.Strong);
            for (int i = 0; i < MatchEngineConstants.SUBSTITUTES_PER_TEAM; i++)
            {
                int index = i;
                Button choice = Control(_substitutionDraft.transform, "", () => _view.SelectIncoming(_view.Incoming[index].Index), _dialogScroll);
                // Gate J J-01: an unavailable bench choice stays listed with its reason in words.
                _incomingReason[i] = _ui.Label(choice.transform, "", S0UiFactory.TextStyle.Secondary);
                _incomingReason[i].gameObject.SetActive(false);
                _incoming.Add(choice);
            }

            _choiceNotice = _ui.Reason(content, true);
            RectTransform actions = _ui.Row("Dialog actions", content);
            _submit = Control(actions, L("action.submit"), SubmitDialog, _dialogScroll, S0UiFactory.ControlKind.Primary);
            _cancel = Control(actions, L("action.cancel"), CancelDialog, _dialogScroll);
            _dialog.SetActive(false);
        }

        // Frame width min(46rem, 95vw) and height min(content, 90vh), as in H v0.2.
        private void LayoutDialogFrame()
        {
            if (!_dialog.activeSelf)
                return;
            Vector2 screen = _dialogCanvas.rect.size;
            float width = Mathf.Min(S0UiConstants.DIALOG_MAX_WIDTH * _ui.Scale, screen.x * S0UiConstants.DIALOG_WIDTH_SHARE);
            float height = Mathf.Min(LayoutUtility.GetPreferredHeight(_dialogScroll.content), screen.y * S0UiConstants.DIALOG_HEIGHT_SHARE);
            Vector2 size = new Vector2(Mathf.Round(width), Mathf.Round(height));
            if (_dialogFrame.sizeDelta != size)
                _dialogFrame.sizeDelta = size;
        }

        private void OpenDialog(Selectable invoker, Action open)
        {
            open();
            _invoker = invoker;
            _dialog.SetActive(true);
            _compare.SetActive(false);
            Refresh();
            Canvas.ForceUpdateCanvases();
            LayoutDialogFrame();
            Focus(DialogInitialFocus());
        }

        private Selectable DialogInitialFocus()
        {
            IReadOnlyList<Button> choices = _view.Dialog == S0ScreenPresenter.DialogKind.Mentality ? _mentalityChoices : _outgoing;
            int first = S0FocusNavigation.FirstAvailable(choices, _isAllowed);
            return first >= 0 ? choices[first] : _cancel;
        }

        private void CancelDialog()
        {
            _view.CancelDialog();
            _dialog.SetActive(false);
            Refresh();
            Focus(_invoker);
        }

        private void SubmitDialog()
        {
            ClientChangeRecord record = _view.Dialog == S0ScreenPresenter.DialogKind.Mentality ? _view.SubmitMentality() : _view.SubmitSubstitution();
            Refresh();
            if (record != null)
            {
                int latest = S0FocusNavigation.LatestFeedbackIndex(_view.Feedback.Count, _feedbackRows.Count);
                if (latest >= 0)
                    Focus(_feedbackRows[latest]);
            }
        }

        /// <summary>Called after the coordinator's accepted-frame refresh; retains authored/generated view roots.</summary>
        public void Refresh()
        {
            using (RefreshMarker.Auto())
            {
                if (_view == null || _disposed)
                    return;
                _view.Refresh();
                if (_revision != _view.Revision)
                {
                    _revision = _view.Revision;
                    Bind();
                    if (_lastScreen != _view.Screen)
                    {
                        _lastScreen = _view.Screen;
                        Focus(_view.Screen == ClientScreens.MainMenu ? _menuHeading : _view.Screen == ClientScreens.TacticsSetup ? _setupHeading : _view.Screen == ClientScreens.MatchView ? _matchHeading : _reportHeading);
                    }

                    if (!_wasFullTime && _view.IsFullTime)
                    {
                        _dialog.SetActive(false);
                        Focus(_report);
                    }

                    _wasFullTime = _view.IsFullTime;
                }

                Keyboard();
            }
        }

        private void Bind()
        {
            _pitchLayoutDirty = true;
            for (int i = 0; i < S0ScreenPresenter.Mentalities.Count; i++)
            {
                Mentality value = S0ScreenPresenter.Mentalities[i];
                MarkChoice(_setupChoices[i], value == _view.CurrentMentality && _view.Screen == ClientScreens.TacticsSetup, L("tag.current"));
                MarkChoice(_mentalityChoices[i], value == _view.RequestedMentality, L("tag.requested"));
            }

            S0UiFactory.Set(_setupReady, _view.Text.Format("setup.ready", "setup.ready", _view.MentalityLabel(_view.CurrentMentality)));
            S0UiFactory.Set(_matchScore, _view.Score);
            S0UiFactory.Set(_matchClock, _view.Clock);
            S0UiFactory.Set(_matchPeriod, _view.Period);
            S0UiFactory.Set(_speed, _view.Speed);
            string current = _view.Text.Format("current", "mentality.current", _view.MentalityLabel(_view.CurrentMentality));
            S0UiFactory.Set(_current, current);
            S0UiFactory.Set(_scoreMentality, _view.Frame.IsEmpty ? "" : current);
            S0UiFactory.Set(_used, _view.SubstitutionsUsed);
            S0UiFactory.SetBoxed(_controlReason, _view.ControlReason);
            S0UiFactory.SetBoxed(_slowerReason, _view.SlowerReason);
            S0UiFactory.SetBoxed(_fasterReason, _view.FasterReason);
            S0UiFactory.SetBoxed(_mentalityReason, _view.MentalityReason);
            S0UiFactory.SetBoxed(_substitutionReason, _view.SubstitutionReasonText);
            _slower.interactable = _view.CanSlower;
            _faster.interactable = _view.CanFaster;
            _pause.interactable = _view.CanPause;
            _ui.SetButton(_pause, L(_view.IsPaused ? "action.resume" : "action.pause"));
            _mentality.interactable = _view.CanChangeMentality;
            _substitution.interactable = _view.CanSubstitute;
            _report.gameObject.SetActive(_view.CanReport);
            _statisticsToggle.gameObject.SetActive(!_view.IsFullTime);
            _statisticsToggle.interactable = !_view.Frame.IsEmpty;
            _ui.SetButton(_statisticsToggle, L(_view.IsStatisticsOpen ? "action.statistics_close" : "action.statistics_open"));
            _matchStatistics.gameObject.SetActive(_view.IsStatisticsOpen && _view.HasStatistics);
            S0UiFactory.SetBoxed(_notice, _view.StatisticsNotice);
            // The panel heading already names complete statistics; only a partial cutoff caption adds to it.
            _caption.gameObject.SetActive(_view.IsStatisticsIncomplete);
            S0UiFactory.Set(_caption, _view.StatisticsCaption);
            S0UiFactory.Set(_reportScore, _view.Score);
            S0UiFactory.Set(_reportResult, _view.Result);
            S0UiFactory.SetBoxed(_reportNotice, _view.StatisticsNotice);
            S0UiFactory.Set(_reportCaption, _view.StatisticsCaption);
            _partialToggle.gameObject.SetActive(_view.IsStatisticsIncomplete && _view.HasStatistics);
            _ui.SetButton(_partialToggle, L(_view.IsPartialReportOpen ? "action.statistics_close" : "action.partial"));
            _reportStatistics.gameObject.SetActive(_view.HasStatistics && (!_view.IsStatisticsIncomplete || _view.IsPartialReportOpen));
            BindStatistics();
            BindFeedback();
            BindDialog();
            _matchCamera.enabled = _view.Screen == ClientScreens.MatchView && !_view.Frame.IsEmpty;
            _pitchWaiting.transform.parent.gameObject.SetActive(_view.Frame.IsEmpty);
            _pitchBounds.gameObject.SetActive(!_view.Frame.IsEmpty);
            if (_view.Screen == ClientScreens.MainMenu)
            {
                _pitchEntryIndex = -1;
                foreach (Text label in _pitchLabels)
                    S0UiFactory.Set(label, "");
                Array.Clear(_measuredText, 0, _measuredText.Length);
                _measuredWidth = 0;
                _pitchLayout.LabelHeight = 0;
                foreach (Button button in _outgoing)
                    _ui.SetButton(button, "");
                foreach (Button button in _incoming)
                    _ui.SetButton(button, "");
                S0UiFactory.Set(_pitchDescription, L("pitch.description"));
            }

            if (_revealStatistics)
            {
                // Opening statistics scrolls just far enough to show the whole table, never past its toggle.
                _revealStatistics = false;
                Reveal(_matchScroll, _statisticsPanel);
                Reveal(_matchScroll, (RectTransform)_statisticsToggle.transform);
            }

            RecoverFocus();
        }

        private void MarkChoice(Button button, bool selected, string tag)
        {
            if (!_selectionText.TryGetValue(button, out Text label))
            {
                label = _ui.Label(button.transform, tag, S0UiFactory.TextStyle.Chip);
                label.color = S0UiConstants.ACCENT_COLOR;
                label.gameObject.name = "Selection";
                _selectionText.Add(button, label);
            }
            label.gameObject.SetActive(selected);
            _ui.SetSelected(button, selected);
        }

        private void BindDialog()
        {
            bool open = _view.Dialog != S0ScreenPresenter.DialogKind.None;
            _dialog.SetActive(open);
            if (!open)
                return;
            bool mentality = _view.Dialog == S0ScreenPresenter.DialogKind.Mentality;
            _mentalityDraft.SetActive(mentality);
            _substitutionDraft.SetActive(!mentality);
            S0UiFactory.Set(_dialogHeading.GetComponent<Text>(), L(mentality ? "dialog.mentality" : "dialog.substitution"));
            S0UiFactory.Set(_dialogCurrent, _view.Text.Format("dialog.current", "mentality.current", _view.MentalityLabel(_view.CurrentMentality)));
            _dialogCurrent.gameObject.SetActive(mentality);
            if (mentality)
            {
                for (int i = 0; i < S0ScreenPresenter.Mentalities.Count; i++)
                {
                    Mentality value = S0ScreenPresenter.Mentalities[i];
                    _comparisonCurrent[i].gameObject.SetActive(value == _view.CurrentMentality);
                    _comparisonRequested[i].gameObject.SetActive(value == _view.RequestedMentality);
                }
            }
            S0UiFactory.SetBoxed(_choiceNotice, _view.ChoiceNotice);
            _submit.interactable = mentality ? _view.CanChangeMentality : _view.CanSubmitSubstitution;
            for (int i = 0; i < _outgoing.Count; i++)
            {
                bool exists = i < _view.Outgoing.Count;
                _outgoing[i].gameObject.SetActive(exists);
                if (exists)
                {
                    _ui.SetButton(_outgoing[i], _view.Outgoing[i].Label);
                    MarkChoice(_outgoing[i], _view.Outgoing[i].Index == _view.SelectedOutgoing, L("tag.requested"));
                }
            }

            for (int i = 0; i < _incoming.Count; i++)
            {
                bool exists = i < _view.Incoming.Count;
                _incoming[i].gameObject.SetActive(exists);
                if (exists)
                {
                    S0ScreenPresenter.PlayerChoice choice = _view.Incoming[i];
                    _ui.SetButton(_incoming[i], choice.Label);
                    _incoming[i].interactable = choice.IsAvailable;
                    S0UiFactory.Set(_incomingReason[i], choice.UnavailableReason);
                    _incomingReason[i].gameObject.SetActive(!choice.IsAvailable);
                    MarkChoice(_incoming[i], choice.Index == _view.SelectedIncoming, L("tag.requested"));
                }
            }
        }

        private void BindFeedback()
        {
            bool report = _view.Screen == ClientScreens.PostMatchReport;
            RectTransform target = report ? _reportFeedback : _matchFeedback;
            ScrollRect scroll = report ? _reportScroll : _matchScroll;
            if (_feedbackRoot.parent != target)
                _feedbackRoot.SetParent(target, false);
            if (_earlierToggle.transform.parent != target)
                _earlierToggle.transform.SetParent(target, false);
            if (_earlierRoot.parent != target)
                _earlierRoot.SetParent(target, false);
            // Chronological visual and explicit-anchor order survives every reparent/repeat match.
            _earlierToggle.transform.SetSiblingIndex(1);
            _earlierRoot.SetSiblingIndex(2);
            _feedbackRoot.SetSiblingIndex(3);
            _scrolls[_earlierToggle] = scroll;
            while (_feedbackRows.Count < _view.Feedback.Count)
                AddFeedbackRow();
            int earlier = Mathf.Max(0, _view.Feedback.Count - S0UiConstants.EXPANDED_FEEDBACK);
            for (int i = 0; i < _feedbackRows.Count; i++)
            {
                Selectable row = _feedbackRows[i];
                _scrolls[row] = scroll;
                bool exists = i < _view.Feedback.Count;
                row.gameObject.SetActive(exists && (i >= earlier || _view.IsEarlierFeedbackOpen));
                if (!exists)
                {
                    S0UiFactory.Set(_feedbackText[row], "");
                    continue;
                }

                Transform parent = i < earlier ? _earlierRoot : _feedbackRoot;
                if (row.transform.parent != parent)
                    row.transform.SetParent(parent, false);
                row.transform.SetSiblingIndex(i < earlier ? i : i - earlier);
                S0UiFactory.Set(_feedbackText[row], _view.Feedback[i]);
                StyleOutcome(row, _view.FeedbackStatus[i]);
            }

            _earlierToggle.gameObject.SetActive(earlier > 0);
            _earlierRoot.gameObject.SetActive(_view.IsEarlierFeedbackOpen);
            _ui.SetButton(_earlierToggle, _view.Text.Format("feedback.earlier", "feedback.earlier", earlier));
        }

        // H v0.2 outcome row: sunken surface, status-coloured left bar and a glyph badge. The row's words
        // carry the outcome; colour and glyph only reinforce it (I-C11).
        private void AddFeedbackRow()
        {
            RectTransform row = _ui.Row("Feedback row", _feedbackRoot, TextAnchor.UpperLeft);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, S0UiConstants.GAP + S0UiConstants.GAP / 2, S0UiConstants.GAP, S0UiConstants.GAP);
            var fill = row.gameObject.AddComponent<Image>();
            fill.color = S0UiConstants.SUNKEN_COLOR;
            fill.raycastTarget = false;
            Image bar = _ui.Bar(row, S0UiConstants.FOCUS_COLOR);
            // The cell takes the row's height; the badge keeps its own, at the top (H v0.2 .outcome).
            RectTransform cell = _ui.Column("Outcome cell", row, 0);
            cell.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
            RectTransform badge = _ui.Row("Outcome badge", cell, TextAnchor.MiddleCenter);
            badge.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(S0UiConstants.GAP / 2 + 2, S0UiConstants.GAP / 2 + 2, 1, 1);
            var badgeFill = badge.gameObject.AddComponent<Image>();
            badgeFill.raycastTarget = false;
            Text glyph = _ui.Label(badge, "", S0UiFactory.TextStyle.Strong);
            glyph.alignment = TextAnchor.MiddleCenter;
            Text body = _ui.Label(row, "");
            body.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Selectable anchor = row.gameObject.AddComponent<Selectable>();
            anchor.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            anchor.transition = Selectable.Transition.None;
            Register(anchor, _matchScroll);
            _feedbackRows.Add(anchor);
            _feedbackText.Add(anchor, body);
            _feedbackBar.Add(anchor, bar);
            _feedbackBadge.Add(anchor, badgeFill);
            _feedbackGlyph.Add(anchor, glyph);
        }

        private void StyleOutcome(Selectable row, ClientChangeStatus status)
        {
            Color accent, badge, ink;
            string glyph;
            switch (status)
            {
                case ClientChangeStatus.Applied:
                    accent = badge = S0UiConstants.SUCCESS_COLOR;
                    ink = S0UiConstants.INK_COLOR;
                    glyph = Glyph('✓', '√');
                    break;
                case ClientChangeStatus.Refused:
                case ClientChangeStatus.SendFailure:
                    accent = badge = S0UiConstants.FAILURE_COLOR;
                    ink = S0UiConstants.INK_COLOR;
                    glyph = Glyph('✕', '×');
                    break;
                case ClientChangeStatus.NotApplied:
                    accent = ink = S0UiConstants.SECONDARY_COLOR;
                    badge = S0UiConstants.SUNKEN_COLOR;
                    glyph = Glyph('–', '-');
                    break;
                default:
                    accent = ink = S0UiConstants.FOCUS_COLOR;
                    badge = S0UiConstants.RAISED_COLOR;
                    glyph = Glyph('…', '.');
                    break;
            }

            _feedbackBar[row].color = accent;
            _feedbackBadge[row].color = badge;
            _feedbackGlyph[row].color = ink;
            S0UiFactory.Set(_feedbackGlyph[row], glyph);
        }

        // The packaged font decides: the H v0.2 glyph when it has one, else a covered substitute.
        private string Glyph(char preferred, char substitute) =>
            _font.HasCharacter(preferred) ? preferred.ToString() : _font.HasCharacter(substitute) ? substitute.ToString() : "";

        private void BindStatistics()
        {
            RectTransform parent = _view.Screen == ClientScreens.PostMatchReport ? _reportStatistics : _matchStatistics;
            while (_statRows.Count < _view.Statistics.Count + 1)
            {
                RectTransform row = _ui.Node("Statistic row", parent);
                var fill = row.gameObject.AddComponent<Image>();
                fill.raycastTarget = false;
                RectTransform rule = _ui.Overlay("Rule", row);
                rule.anchorMin = Vector2.zero;
                rule.anchorMax = new Vector2(1, 0);
                rule.pivot = new Vector2(0.5f, 0);
                rule.sizeDelta = new Vector2(0, 1);
                rule.anchoredPosition = Vector2.zero;
                var ruleImage = rule.gameObject.AddComponent<Image>();
                ruleImage.color = S0UiConstants.LINE_COLOR;
                ruleImage.raycastTarget = false;
                var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(S0UiConstants.GAP, S0UiConstants.GAP, S0UiConstants.GAP, S0UiConstants.GAP);
                layout.spacing = S0UiConstants.GAP;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                var labels = new Text[3];
                for (int i = 0; i < labels.Length; i++)
                {
                    Text label = _ui.Label(row, "");
                    labels[i] = label;
                    label.alignment = i == 0 ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
                    var size = label.gameObject.AddComponent<LayoutElement>();
                    size.flexibleWidth = 1;
                }

                _statRows.Add(row);
                _statText.Add(row, labels);
            }

            for (int i = 0; i < _statRows.Count; i++)
            {
                RectTransform row = _statRows[i];
                if (row.parent != parent)
                    row.SetParent(parent, false);
                row.gameObject.SetActive(i <= _view.Statistics.Count);
                if (i > _view.Statistics.Count)
                    continue;
                string label, home, away;
                if (i == 0)
                {
                    label = L("statistics.column");
                    home = L("team.home");
                    away = L("team.away");
                }
                else
                {
                    var stat = _view.Statistics[i - 1];
                    label = stat.Label;
                    home = stat.Home;
                    away = stat.Away;
                }

                S0UiFactory.Set(_statText[row][0], label);
                S0UiFactory.Set(_statText[row][1], home);
                S0UiFactory.Set(_statText[row][2], away);
                Color ink = i == 0 ? S0UiConstants.SECONDARY_COLOR : S0UiConstants.TEXT_COLOR;
                foreach (Text cell in _statText[row])
                    cell.color = ink;
                row.GetComponent<Image>().color = i > 0 && i % 2 == 1 ? S0UiConstants.ZEBRA_COLOR : Color.clear;
            }

            (_view.Screen == ClientScreens.PostMatchReport ? _reportLooseBall : _matchLooseBall).transform.SetAsLastSibling();
        }

        private void BindPitch()
        {
            if (_view.Screen != ClientScreens.MatchView || _view.Frame.IsEmpty)
                return;
            if (!PreparePitchLayout())
                return;
            MatchFrameView frame = _view.Frame;
            int width = Mathf.Clamp(Mathf.RoundToInt(_pitch.rectTransform.rect.width), 1, S0UiConstants.MAX_TEXTURE_SIDE);
            int height = Mathf.Max(1, Mathf.RoundToInt(width * MatchEngineConstants.PITCH_WIDTH_M / MatchEngineConstants.PITCH_LENGTH_M));
            if (_pitchTexture == null || _pitchTexture.width != width)
            {
                _matchCamera.targetTexture = null;
                if (_pitchTexture != null)
                {
                    _pitchTexture.Release();
                    Destroy(_pitchTexture);
                }

                _pitchTexture = new RenderTexture(width, height, S0UiConstants.TEXTURE_DEPTH_BITS);
                _pitchTexture.Create();
                _pitch.texture = _pitchTexture;
                _matchCamera.targetTexture = _pitchTexture;
            }

            Vector2 viewportSize = _pitchBounds.rect.size;
            float imageBottom = viewportSize.y - _pitch.rectTransform.rect.height;
            for (int i = 0; i < _pitchLabels.Count; i++)
            {
                var cue = frame.AgentCues[i];
                Vector3 world = default;
                _labelVisible[i] = !cue.IsSentOff && _renderer.TryGetRenderedAgentPosition(i, cue.PlayerId, out world);
                if (!_labelVisible[i])
                    continue;
                Vector3 projected = _matchCamera.WorldToViewportPoint(world);
                _labelVisible[i] = projected.z > 0 && projected.x >= 0 && projected.x <= 1 && projected.y >= 0 && projected.y <= 1;
                _markerPoints[i] = new Vector2(projected.x * viewportSize.x,
                    imageBottom + projected.y * _pitch.rectTransform.rect.height);
            }

            if (viewportSize.x <= 0 || viewportSize.y <= 0)
                return;
            if (!S0ScreenLayout.TryPlacePitchLabels(viewportSize, _markerPoints, _labelSizes, _labelVisible, _labelCentres, S0UiConstants.GAP))
            {
                RecordLayoutFailure();
                return;
            }
            _layoutFailureReported = false;
            for (int i = 0; i < _pitchLabels.Count; i++)
            {
                RectTransform rect = _pitchChips[i];
                rect.gameObject.SetActive(_labelVisible[i]);
                _leaderLines[i].gameObject.SetActive(_labelVisible[i]);
                if (!_labelVisible[i])
                    continue;
                StyleChip(i, frame.AgentCues[i].IsSubstitute);
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.sizeDelta = _labelSizes[i];
                rect.anchoredPosition = _labelCentres[i];
                RectTransform line = _leaderLines[i];
                line.anchorMin = line.anchorMax = Vector2.zero;
                line.anchoredPosition = _markerPoints[i];
                Vector2 delta = _labelCentres[i] - _markerPoints[i];
                line.sizeDelta = new Vector2(delta.magnitude, 1);
                line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
        }

        // H v0.2 .agent: home navy chip with white text, away gold chip with dark ink; an applied
        // substitute keeps a white outline.
        private void StyleChip(int agent, bool substitute)
        {
            bool home = _view.PitchMarkerIsHome(agent);
            _chipFill[agent].color = home ? S0UiConstants.PRIMARY_HOVER_COLOR : S0UiConstants.ACCENT_COLOR;
            _pitchLabels[agent].color = home ? Color.white : S0UiConstants.INK_COLOR;
            _chipBorder[agent].color = substitute ? Color.white : home ? S0UiConstants.FOCUS_COLOR : S0UiConstants.ACCENT_COLOR;
        }

        private bool PreparePitchLayout()
        {
            if (_screenWidth != Screen.width || _screenHeight != Screen.height)
            {
                _screenWidth = Screen.width;
                _screenHeight = Screen.height;
                _pitchLayoutDirty = true;
            }
            // UGUI normally lays out at render, after LateUpdate. Activation/geometry changes must
            // complete that pass before we read the rect, never the default first-frame 100x100.
            if (_pitchLayoutDirty)
                Canvas.ForceUpdateCanvases();
            float width = _pitchBounds.rect.width;
            if (width <= S0UiConstants.GAP)
                return false;
            bool changed = _measuredWidth != width;
            for (int i = 0; i < _pitchLabels.Count; i++)
            {
                Text label = _pitchLabels[i];
                string text = _view.PitchMarker(i);
                if (_measuredText[i] == text && _measuredWidth == width)
                    continue;
                S0UiFactory.Set(label, text);
                // Constrain the wrapping rectangle, never the string; then measure full text at that width.
                // The text is inset by GAP/2 on each side of its chip.
                float textWidth = Mathf.Min(width - S0UiConstants.GAP * 2, label.preferredWidth);
                _pitchChips[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth + S0UiConstants.GAP);
                _labelSizes[i] = new Vector2(textWidth + S0UiConstants.GAP, label.preferredHeight + S0UiConstants.GAP / 2);
                _measuredText[i] = text;
                changed = true;
            }
            _measuredWidth = width;
            if (changed)
            {
                float height = S0ScreenLayout.RequiredPitchLabelHeight(width, _labelSizes, _labelReserved, S0UiConstants.GAP);
                if (float.IsPositiveInfinity(height))
                {
                    RecordLayoutFailure();
                    return false;
                }
                _pitchLayout.LabelHeight = height;
                LayoutRebuilder.MarkLayoutForRebuild(_pitchBounds);
                Canvas.ForceUpdateCanvases();
            }
            _pitchLayoutDirty = false;
            return _pitchBounds.rect.height > 0;
        }

        private void RecordLayoutFailure()
        {
            if (_layoutFailureReported)
                return;
            _layoutFailureReported = true;
            PitchLayoutFailureCount++;
            Debug.LogWarning("S0 pitch label layout is incomplete; record I-Q01/I-Q16 as failed. Playback remains available.");
        }

        private void LateUpdate()
        {
            using (RefreshMarker.Auto())
            {
                if (_view == null || _disposed)
                    return;
                try
                {
                    LayoutDialogFrame();
                    BindPitch();
                    // Marker visibility is applied in LateUpdate, after structural binding/focus recovery.
                    RecoverFocus();
                    Selectable focused = EventSystem.current?.currentSelectedGameObject?.GetComponent<Selectable>();
                    int marker = Array.IndexOf(_pitchControls, focused);
                    if (marker >= 0 && IsAllowed(focused))
                        S0UiFactory.Set(_pitchDescription, _view.PitchPlayerDescription(marker));
                }
                catch (Exception exception)
                {
                    _renderer.RejectPresentation(exception);
                }
            }
        }

        private bool IsAllowed(Selectable item) => item != null && item.gameObject.activeInHierarchy && item.IsInteractable() && (!_dialog.activeSelf || item.transform.IsChildOf(_dialog.transform));
        private bool IsTabStop(Selectable item)
        {
            if (!IsAllowed(item) || !_focusRoles.TryGetValue(item, out S0FocusNavigation.Role role))
                return false;
            int marker = role == S0FocusNavigation.Role.PitchMarker ? Array.IndexOf(_pitchControls, item) : -1;
            return S0FocusNavigation.IsTabStop(role, marker, _pitchEntryIndex);
        }

        private void FocusPitch(int index)
        {
            if (index < 0 || !IsAllowed(_pitchControls[index]))
                return;
            _pitchEntryIndex = index;
            Focus(_pitchControls[index]);
        }
        private void Focus(Selectable item)
        {
            if (!IsAllowed(item) || EventSystem.current == null)
                return;
            EventSystem.current.SetSelectedGameObject(item.gameObject);
            if (_scrolls.TryGetValue(item, out ScrollRect scroll))
                Reveal(scroll, item.transform as RectTransform);
        }

        private void RecoverFocus()
        {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
                return;
            Selectable current = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
            if (!IsAllowed(current))
            {
                int marker = Array.IndexOf(_pitchControls, current);
                if (!_dialog.activeSelf && marker >= 0 && !_view.IsFullTime)
                {
                    int next = S0FocusNavigation.Move(_pitchControls, marker, 1, _isAllowed);
                    if (next >= 0)
                    {
                        FocusPitch(next);
                        return;
                    }
                }
                Focus(_dialog.activeSelf ? DialogInitialFocus() : _view.Screen == ClientScreens.MainMenu ? _menuHeading : _view.Screen == ClientScreens.TacticsSetup ? _setupHeading : _view.Screen == ClientScreens.PostMatchReport ? _reportHeading : _view.IsFullTime ? _report : _pause.IsInteractable() ? _pause : _matchHeading);
            }
        }

        private void Keyboard()
        {
            // A choice arrow invokes a Control, whose Refresh re-enters here in the same frame while
            // GetKeyDown is still true; without this guard that recursion overflowed the stack.
            if (_keyboardFrame == Time.frameCount)
                return;
            _keyboardFrame = Time.frameCount;
            if (!_dialog.activeSelf && _view.Screen == ClientScreens.MatchView &&
                (_pitchEntryIndex < 0 || !IsAllowed(_pitchControls[_pitchEntryIndex])))
                _pitchEntryIndex = S0FocusNavigation.FirstAvailable(_pitchControls, _isAllowed);
            if (Input.GetMouseButtonDown(0))
                _keyboardFocus = false;
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _keyboardFocus = true;
                int current = -1;
                // Hierarchy order follows the visible layout, including retained/reparented history.
                Selectable[] order = GetComponentsInChildren<Selectable>(false);
                GameObject selected = EventSystem.current?.currentSelectedGameObject;
                for (int i = 0; i < order.Length; i++)
                    if (order[i].gameObject == selected)
                        current = i;
                int step = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? -1 : 1;
                if (!_dialog.activeSelf && _view.Screen == ClientScreens.MatchView)
                {
                    // Hierarchy places the history disclosure before statistics; §9.3 orders statistics first.
                    int next = S0FocusNavigation.MoveInLogicalOrder(_matchTabOrder, order, current, step, _isTabStop);
                    if (next >= 0)
                        Focus(_matchTabOrder[next]);
                }
                else
                {
                    int next = S0FocusNavigation.Move(order, current, step, _isTabStop);
                    if (next >= 0)
                        Focus(order[next]);
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape) && _dialog.activeSelf)
            {
                _keyboardFocus = true;
                CancelDialog();
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow))
                ChoiceArrow(1);
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
                ChoiceArrow(-1);
            GameObject focused = EventSystem.current?.currentSelectedGameObject;
            foreach (var pair in _focusRings)
                pair.Value.SetActive(_keyboardFocus && pair.Key.gameObject == focused && IsAllowed(pair.Key));
        }

        private void ChoiceArrow(int step)
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (selected == null)
                return;
            int marker = Array.IndexOf(_pitchControls, selected.GetComponent<Selectable>());
            if (marker >= 0)
            {
                int next = S0FocusNavigation.Move(_pitchControls, marker, step, _isAllowed);
                if (next >= 0)
                {
                    _keyboardFocus = true;
                    FocusPitch(next);
                }
                return;
            }
            IReadOnlyList<Button> group = null;
            foreach (Button b in _setupChoices)
                if (b.gameObject == selected)
                    group = _setupChoices;
            foreach (Button b in _mentalityChoices)
                if (b.gameObject == selected)
                    group = _mentalityChoices;
            foreach (Button b in _outgoing)
                if (b.gameObject == selected)
                    group = _outgoing;
            foreach (Button b in _incoming)
                if (b.gameObject == selected)
                    group = _incoming;
            if (group == null)
                return;
            int index = 0;
            for (int i = 0; i < group.Count; i++)
                if (group[i].gameObject == selected)
                    index = i;
            int choice = S0FocusNavigation.Move(group, index, step, _isAllowed);
            if (choice >= 0)
            {
                _keyboardFocus = true;
                group[choice].onClick.Invoke();
                Focus(group[choice]);
            }
        }

        private static void Reveal(ScrollRect scroll, RectTransform item)
        {
            if (scroll == null || item == null)
                return;
            Canvas.ForceUpdateCanvases();
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, item);
            Rect visible = scroll.viewport.rect;
            Vector2 offset = scroll.content.anchoredPosition;
            if (bounds.max.y > visible.yMax)
                offset.y -= bounds.max.y - visible.yMax;
            else if (bounds.min.y < visible.yMin)
                offset.y += visible.yMin - bounds.min.y;
            scroll.content.anchoredPosition = offset;
        }

        /// <summary>Destroy only owned UI/render resources; authored scene roots and renderer survive.</summary>
        public void DisposeViews()
        {
            if (_disposed)
                return;
            _disposed = true;
            if (_matchCamera != null)
                _matchCamera.targetTexture = null;
            if (_pitchTexture != null)
            {
                _pitchTexture.Release();
                Destroy(_pitchTexture);
            }

            foreach (GameObject item in _owned)
                if (item != null)
                {
                    item.SetActive(false);
                    Destroy(item);
                }

            _owned.Clear();
        }

        private void OnDestroy() => DisposeViews();
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Persistent four-screen UGUI binding and keyboard/dialog/resource lifecycle. |
// | 1.1     | 2026-10-08 | —      | Complete first-frame layout, grow full-label scroll surface, cache metrics/controls and stabilize chronological history. |
// | 1.2     | 2026-10-08 | —      | Clarify full-string wrapping and subsequent measured height for narrow label rectangles. |
// | 1.3     | 2026-10-08 | —      | Separate action Tab stops from explicit anchors, focus first dialog selector and current feedback row across repeat matches. |
// | 1.4     | 2026-10-08 | —      | Consume tested focus policy; retain one roving pitch Tab entry with read-only arrow inspection and visibility recovery. |
// | 1.5     | 2026-10-08 | —      | Exclude focus overlays from layout and retain/rebind both comparison tags to current and requested Mentality. |
// | 1.6     | 2026-10-08 | —      | Match View Tab follows journey §9.3 explicitly: statistics before earlier feedback, then report and pitch entry. |
// | 1.7     | 2026-10-19 | —      | Pinned-host I-Q15: process keyboard input once per frame; a choice arrow's Control refresh re-entered Keyboard() and overflowed the stack. |
// | 1.8     | 2026-10-21 | —      | Gate J J-01: a goalkeeper-mismatched bench choice is unavailable and shows its reason. J-04 (H v0.2): scoreboard, changes and statistics panels (statistics now in the rail, so opening them no longer shrinks the pitch), primary/secondary control chrome with dashed unavailable borders and gold selection, reason/warning boxes, outcome rows with status bar and ✓ ✕ – … badge, zebra statistics table, navy/gold shirt chips with leaders, whole-pitch top-down view, centred modal staging dialog over a dimmed backdrop, centred entry pages. Scoreboard team labels keep their own width so narrow 200% never breaks them mid-word. |
// | 1.9     | 2026-10-28 | —      | J-04 owner review: the rail statistics caption shows only for a partial cutoff (the panel heading already says "Match statistics"); rail table rows abut with no extra spacing; opening statistics scrolls the page just enough to show the whole panel, keeping its toggle in view. |
#endregion
