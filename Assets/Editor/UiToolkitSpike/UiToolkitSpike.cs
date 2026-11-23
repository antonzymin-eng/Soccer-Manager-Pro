// File:     Assets/Editor/UiToolkitSpike/UiToolkitSpike.cs
// Created:  2026-11-20
// Modified: 2026-11-23 (EventSystem alias inside the namespace; CS0118 on the pinned host)
// Author:   —
// Spec:     UI Toolkit presentation design §4, T0a (PR #560)
// Purpose:  Attach a temporary comparison rail to the running client and record host observations.

using System;
using System.Collections;
using System.Linq;

using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

using TacticalDirector.MatchClientUnity;
using Object = UnityEngine.Object;
using UguiSelectable = UnityEngine.UI.Selectable;

namespace TacticalDirector.UiToolkitSpike
{
    using EventSystem = UnityEngine.EventSystems.EventSystem;

    internal sealed class UiToolkitSpike
    {
        private const string ASSET_DIRECTORY = "Assets/Editor/UiToolkitSpike/";
        private const string MENU = "Tools/UI Toolkit T0a/";
        private const string PREFIX = "[UI Toolkit T0a] ";

        private enum TextScale
        {
            Baseline,
            Uss200,
            Panel200
        }

        private static UiToolkitSpike _active;

        private readonly ClientShellBehaviour _shell;
        private readonly S0ScreensBehaviour _screens;
        private readonly EventSystem _eventSystem;
        private readonly Transform _mainMenu;
        private readonly bool _isolate;

        private GameObject _host;
        private PanelSettings _settings;
        private FontAsset _font;
        private VisualElement _rail;
        private ScrollView _scroll;
        private Label _status;
        private Button _scaleButton;
        private GameObject _previousSelection;
        private int? _focusRequestFrame;
        private bool _sawFocusIn;
        private bool _ownsFocus;
        private TextScale _scale;
        private int _submits;

        private UiToolkitSpike(ClientShellBehaviour shell, S0ScreensBehaviour screens,
            EventSystem eventSystem, Transform mainMenu, bool isolate)
        {
            _shell = shell;
            _screens = screens;
            _eventSystem = eventSystem;
            _mainMenu = mainMenu;
            _isolate = isolate;
        }

        [MenuItem(MENU + "Attach with stock S0 input")]
        private static void AttachStock() => Attach(false);

        [MenuItem(MENU + "Attach with isolated Toolkit focus")]
        private static void AttachIsolated() => Attach(true);

        [MenuItem(MENU + "Detach")]
        private static void Detach()
        {
            EditorApplication.playModeStateChanged -= StopOnPlayModeExit;
            EditorApplication.delayCall -= Detach;
            AssemblyReloadEvents.beforeAssemblyReload -= Detach;
            _active?.Dispose();
            _active = null;
        }

        [MenuItem(MENU + "Attach with stock S0 input", true)]
        [MenuItem(MENU + "Attach with isolated Toolkit focus", true)]
        private static bool CanAttach() => EditorApplication.isPlaying && _active == null;

        [MenuItem(MENU + "Detach", true)]
        private static bool CanDetach() => _active != null;

        private static void Attach(bool isolate)
        {
            EventSystem eventSystem = EventSystem.current;
            ClientShellBehaviour shell = eventSystem != null
                ? eventSystem.GetComponentInParent<ClientShellBehaviour>()
                : null;
            S0ScreensBehaviour screens = shell != null ? shell.GetComponent<S0ScreensBehaviour>() : null;
            Transform mainMenu = shell != null ? shell.transform.Find("Main Menu") : null;
            if (shell == null || !shell.isActiveAndEnabled || screens == null || !screens.isActiveAndEnabled ||
                !(eventSystem.currentInputModule is StandaloneInputModule) ||
                Object.FindObjectsByType<EventSystem>().Length != 1 ||
                (isolate && (mainMenu == null || !mainMenu.gameObject.activeInHierarchy)))
            {
                Debug.LogError(PREFIX + "Open the tracked client scene in Play Mode and wait for its S0 EventSystem. " +
                    "Isolated focus is available on Main Menu.");
                return;
            }

            _active = new UiToolkitSpike(shell, screens, eventSystem, mainMenu, isolate);
            try
            {
                _active.CreatePanel();
                EditorApplication.playModeStateChanged += StopOnPlayModeExit;
                AssemblyReloadEvents.beforeAssemblyReload += Detach;
                Debug.Log(PREFIX + $"Attached using {(isolate ? "isolated" : "stock")} S0 input. " +
                    "Use F8 to transfer focus, F9 to cycle scale and F10 to log a layout sample.");
            }
            catch (Exception exception)
            {
                Detach();
                Debug.LogException(exception);
            }
        }

        private static void StopOnPlayModeExit(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
                Detach();
        }

        private void CreatePanel()
        {
            var layout = Load<VisualTreeAsset>("Spike.uxml");
            var theme = Load<ThemeStyleSheet>("Spike.tss");
            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _settings.name = "T0a temporary panel settings";
            _settings.hideFlags = HideFlags.DontSave;
            _settings.themeStyleSheet = theme;
            _settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            _settings.scale = 1f;
            _settings.sortingOrder = 10;

            _host = new GameObject("T0a UI Toolkit rail") { hideFlags = HideFlags.DontSave };
            _host.transform.SetParent(_shell.transform, false);
            _host.SetActive(false);
            var document = _host.AddComponent<UIDocument>();
            document.visualTreeAsset = layout;
            document.panelSettings = _settings;
            _host.SetActive(true);

            _rail = Require<VisualElement>(document.rootVisualElement, "spike-rail");
            _scroll = Require<ScrollView>(_rail, "spike-scroll");
            _status = Require<Label>(_rail, "spike-status");
            _scaleButton = Require<Button>(_rail, "cycle-scale");
            _scaleButton.clicked += CycleScale;
            Require<Button>(_rail, "submit-probe").clicked += () =>
            {
                _submits++;
                UpdateStatus();
                Debug.Log(PREFIX + $"Probe activations: {_submits}");
            };
            Require<Button>(_rail, "return-focus").clicked += () => ReturnToUgui("return button");
            _rail.RegisterCallback<FocusInEvent>(OnRailFocus);
            _rail.RegisterCallback<KeyDownEvent>(ScrollWithKeyboard);
            BindFont(Require<Label>(_rail, "font-sample"));
            UpdateStatus();
            document.StartCoroutine(ObserveInput());
        }

        private void BindFont(Label sample)
        {
            Font source = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _font = FontAsset.CreateFontAsset(source);
            if (_font == null)
                throw new InvalidOperationException(PREFIX + "Unity could not create the sample SDF font asset.");

            _font.hideFlags = HideFlags.DontSave;
            sample.style.unityFontDefinition = FontDefinition.FromSDFFont(_font);
            bool complete = _font.HasCharacters(sample.text, out uint[] missing, tryAddCharacter: true);
            Debug.Log(PREFIX + $"Runtime SDF asset from Unity's built-in {source.name}: " + (complete ? "all probe glyphs available" :
                "missing " + string.Join(", ", missing?.Select(codePoint => $"U+{codePoint:X4}") ?? Array.Empty<string>())));
        }

        private static T Load<T>(string name) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(ASSET_DIRECTORY + name) ??
            throw new InvalidOperationException(PREFIX + "Could not load " + name + ".");

        private static T Require<T>(VisualElement root, string name) where T : VisualElement =>
            root.Q<T>(name) ?? throw new InvalidOperationException(PREFIX + "Missing UXML element " + name + ".");

        private VisualElement ToolkitFocus
        {
            get
            {
                var focused = _rail?.focusController?.focusedElement as VisualElement;
                return focused != null && _rail.Contains(focused) ? focused : null;
            }
        }

        private IEnumerator ObserveInput()
        {
            VisualElement lastToolkitFocus = null;
            GameObject lastUguiSelection = null;
            while (true)
            {
                // The running UIDocument hosts the coroutine, so the probe adds no player component.
                yield return null;
                if (_eventSystem == null || _shell == null || _rail.panel == null)
                {
                    EditorApplication.delayCall += Detach;
                    yield break;
                }

                if (Input.GetKeyDown(KeyCode.F8))
                {
                    if (_focusRequestFrame.HasValue || _ownsFocus || ToolkitFocus != null)
                        ReturnToUgui("F8");
                    else
                        EnterToolkit();
                }

                CheckFocusHandoff();
                if (Input.GetKeyDown(KeyCode.F9))
                    CycleScale();
                if (Input.GetKeyDown(KeyCode.F10))
                    RecordLayout();

                VisualElement focus = ToolkitFocus;
                GameObject selection = _eventSystem.currentSelectedGameObject;
                if (lastToolkitFocus != focus || lastUguiSelection != selection)
                {
                    Debug.Log(PREFIX + $"Frame {Time.frameCount}: Toolkit focus {focus?.name ?? "(none)"}, " +
                        $"EventSystem selection {(selection == null ? "(none)" : selection.name)}.");
                    lastToolkitFocus = focus;
                    lastUguiSelection = selection;
                }
            }
        }

        private void EnterToolkit()
        {
            if (_isolate && (_mainMenu == null || !_mainMenu.gameObject.activeInHierarchy || !_shell.enabled ||
                _screens == null || !_screens.enabled))
            {
                Debug.LogWarning(PREFIX + "Return to the running Main Menu before testing isolated focus.");
                return;
            }

            _previousSelection = _eventSystem.currentSelectedGameObject;
            _focusRequestFrame = Time.frameCount;
            _sawFocusIn = false;
            if (_isolate)
            {
                // Shell.Update calls S0.Refresh directly. S0 also recovers focus in LateUpdate.
                // Pause both on the idle menu to isolate Unity's input integration for comparison.
                _shell.enabled = false;
                _screens.enabled = false;
                _ownsFocus = true;
            }

            Debug.Log(PREFIX + $"Frame {Time.frameCount}: requested UGUI → Toolkit focus.");
            _scaleButton.Focus();
        }

        private void CheckFocusHandoff()
        {
            if (_ownsFocus && (_mainMenu == null || !_mainMenu.gameObject.activeInHierarchy))
            {
                ReturnToUgui("Main Menu was left");
                return;
            }

            VisualElement focus = ToolkitFocus;
            if (_focusRequestFrame is int requestFrame)
            {
                // Allow the entry frame's queued focus events and S0 LateUpdate to settle.
                if (Time.frameCount == requestFrame)
                    return;

                if (focus == null)
                {
                    string observed = _sawFocusIn ? "FocusIn was observed, then focus was lost" : "no FocusIn was observed";
                    Debug.LogWarning(PREFIX + $"Frame {Time.frameCount}: handoff requested on frame {requestFrame} " +
                        $"did not settle inside Toolkit ({observed}).");
                    ReturnToUgui("focus entry did not settle");
                    return;
                }

                _focusRequestFrame = null;
                _sawFocusIn = false;
                Debug.Log(PREFIX + $"Frame {Time.frameCount}: Toolkit focus confirmed on {focus.name}.");
            }
            else if (_ownsFocus && focus == null)
            {
                ReturnToUgui("Toolkit lost focus");
            }
        }

        private void ReturnToUgui(string reason)
        {
            VisualElement focus = ToolkitFocus;
            bool hadProbeFocus = _focusRequestFrame.HasValue || _ownsFocus || focus != null || _previousSelection != null;
            _focusRequestFrame = null;
            _sawFocusIn = false;
            focus?.Blur();
            if (_ownsFocus)
            {
                if (_screens != null) _screens.enabled = true;
                if (_shell != null) _shell.enabled = true;
                _ownsFocus = false;
            }

            if (hadProbeFocus && _eventSystem != null)
            {
                if (!IsUguiSelection(_eventSystem.currentSelectedGameObject))
                    _eventSystem.SetSelectedGameObject(IsUguiSelection(_previousSelection) ? _previousSelection : null);

                GameObject selection = _eventSystem.currentSelectedGameObject;
                Debug.Log(PREFIX + $"Frame {Time.frameCount}: returned to UGUI ({reason}). " +
                    $"EventSystem selection {(selection == null ? "(none)" : selection.name)}.");
            }

            _previousSelection = null;
        }

        private static bool IsUguiSelection(GameObject candidate) => candidate != null && candidate.activeInHierarchy &&
            candidate.TryGetComponent<UguiSelectable>(out var selectable) && selectable.IsInteractable();

        private void OnRailFocus(FocusInEvent evt)
        {
            if (_focusRequestFrame.HasValue)
                _sawFocusIn = true;

            if (evt.target is VisualElement target && _scroll.contentContainer.Contains(target))
            {
                _scroll.ScrollTo(target);
                Debug.Log(PREFIX + $"Frame {Time.frameCount}: Toolkit focus entered {target.name}.");
            }
        }

        private void ScrollWithKeyboard(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.PageUp && evt.keyCode != KeyCode.PageDown)
                return;

            float direction = evt.keyCode == KeyCode.PageDown ? 1f : -1f;
            _scroll.verticalScroller.value += direction * _scroll.contentViewport.layout.height * 0.75f;
            evt.StopPropagation();
        }

        private void CycleScale()
        {
            _scale = _scale switch
            {
                TextScale.Baseline => TextScale.Uss200,
                TextScale.Uss200 => TextScale.Panel200,
                _ => TextScale.Baseline
            };
            _rail.EnableInClassList("spike-large-text", _scale == TextScale.Uss200);
            _settings.scale = _scale == TextScale.Panel200 ? 2f : 1f;
            UpdateStatus();
            Debug.Log(PREFIX + "Scale: " + _scale);
        }

        private void UpdateStatus()
        {
            string scale = _scale switch
            {
                TextScale.Uss200 => "200% text via USS",
                TextScale.Panel200 => "200% via panel scale",
                _ => "100% text"
            };
            _status.text = $"{scale} · {_submits} test activations";
        }

        private void RecordLayout()
        {
            Label sample = Require<Label>(_rail, "long-copy");
            Rect viewport = _scroll.contentViewport.layout;
            Debug.Log(PREFIX + $"{Screen.width}×{Screen.height}, {_scale}, panel scale {_settings.scale}, " +
                $"fixture font {sample.resolvedStyle.fontSize:0.#}, text box {sample.layout.width:0.#}×{sample.layout.height:0.#}, " +
                $"viewport {viewport.width:0.#}×{viewport.height:0.#}, " +
                $"content height {_scroll.contentContainer.layout.height:0.#}, scroll {_scroll.verticalScroller.value:0.#}.");
        }

        private void Dispose()
        {
            ReturnToUgui("probe detached");
            if (_host != null)
            {
                _host.SetActive(false);
                Object.Destroy(_host);
            }
            if (_settings != null) Object.Destroy(_settings);
            // A newly created TextCore FontAsset destroys its own atlas textures and material.
            if (_font != null) Object.Destroy(_font);
        }
    }
}

// | Version | Date       | Author | Change |
// | 0.1     | 2026-11-20 | —      | Editor-only mixed-renderer feasibility probe. |
// | 0.2     | 2026-11-20 | —      | Allow focus entry to settle and log handoff and restoration outcomes. |
