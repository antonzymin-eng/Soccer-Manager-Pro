// File:     src/match-client-unity/ClientShellBehaviour.cs
// Created:  2026-09-04
// Modified: 2026-10-08 (P5b screens)
// Modified (prior): 2026-10-05 (PR #470 review: report ancestor activation to the validator)
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P5b),
//           UI / Client Framework #38 §3.2 (FR-UI-009/010/011), Code Standards #20 §12 rule 1
// Purpose:  Thin Unity binding for the client screen shell. It collects host facts, forwards them to
//           gate-compiled client-app decisions, applies the resulting visibility, and forwards UI events.

using System;

using Unity.Profiling;
using UnityEngine;

using TacticalDirector.ClientApp;
using TacticalDirector.UiFramework;

namespace TacticalDirector.MatchClientUnity
{
    /// <summary>
    /// Binds the host-free <see cref="ClientScreenFlow"/> onto four Unity scene roots. The navigation
    /// graph, structural wiring rules, initial-active hygiene, and exhaustive visibility mapping all
    /// live in gate-compiled <c>client-app</c>; this type only collects Unity instance/ancestor facts,
    /// applies booleans, and forwards UI events (§12 rule 1).
    /// <para>The localized screens consume the coordinator for Start, explicit report acknowledgement
    /// and Return; the renderer owns only its attached generated visuals.</para>
    /// <para>
    /// Place this component on an always-active GameObject outside all four screen roots. The pure
    /// validator refuses a shell on or beneath a root, roots nested inside each other, and any
    /// non-Main-Menu root saved active. That last rule is UI hygiene only: it prevents an authored
    /// stacked-canvas state before the shell applies authoritative visibility; it is not a lifecycle
    /// guard for any screen implementation. A rejected shell deactivates all assigned roots and logs
    /// the reason, so the intentional player-visible failure state is blank rather than arbitrary UI.
    /// </para>
    /// </summary>
    public sealed class ClientShellBehaviour : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("ClientShellBehaviour.Update");
        [Header("P5b screen roots — mutually exclusive children of an always-active shell")]
        [SerializeField] private GameObject _mainMenuRoot;
        [SerializeField] private GameObject _tacticsSetupRoot;
        [SerializeField] private GameObject _matchViewRoot;
        [SerializeField] private GameObject _postMatchReportRoot;

        [SerializeField] private MatchClientBehaviour _matchRenderer;
        [SerializeField] private S0ScreensBehaviour _screens;

        private ClientMatchCoordinator _coordinator;
        private bool _wiringRejected;

        private void Awake()
        {
            ClientShellRootSnapshot shell = CaptureSnapshot(gameObject);
            ClientShellRootSnapshot mainMenu = CaptureSnapshot(_mainMenuRoot);
            ClientShellRootSnapshot tacticsSetup = CaptureSnapshot(_tacticsSetupRoot);
            ClientShellRootSnapshot matchView = CaptureSnapshot(_matchViewRoot);
            ClientShellRootSnapshot postMatchReport = CaptureSnapshot(_postMatchReportRoot);

            ClientShellWiringFault fault = ClientShellWiringValidator.Validate(
                in shell,
                in mainMenu,
                in tacticsSetup,
                in matchView,
                in postMatchReport);
            if (fault != ClientShellWiringFault.None)
            {
                RejectWiring("host-free shell validation returned " + fault + ".");
                return;
            }

            if (_matchRenderer == null || !(_matchRenderer.transform == _matchViewRoot.transform ||
                _matchRenderer.transform.IsChildOf(_matchViewRoot.transform)))
            {
                RejectWiring("pitch renderer must be assigned on or beneath Match View.");
                return;
            }
            _coordinator = new ClientMatchCoordinator(_matchRenderer, S0DemoFixture.CreateApproved());
            ApplyCurrentScreen();
            if (_screens == null) { RejectWiring("localized screen binding is required."); return; }
            try { _screens.Initialize(_coordinator, _mainMenuRoot, _tacticsSetupRoot, _matchViewRoot, _postMatchReportRoot, _matchRenderer, ApplyCurrentScreen); }
            catch (Exception exception) { RejectWiring("screen construction failed: " + exception.Message); Debug.LogException(exception, this); }
        }

        /// <summary>Forwards Main Menu's New Demo Match action to the host-free navigation graph.</summary>
        public void OpenTacticsSetup()
        {
            RequireReady(nameof(OpenTacticsSetup));
            _coordinator.OpenTacticsSetup();
            ApplyCurrentScreen();
        }

        /// <summary>Forwards Tactics Setup's Cancel action to the host-free navigation graph.</summary>
        public void CancelTacticsSetup()
        {
            RequireReady(nameof(CancelTacticsSetup));
            _coordinator.CancelTacticsSetup();
            ApplyCurrentScreen();
        }

        private void Update()
        {
            using var updateScope = UpdateMarker.Auto();
            if (_wiringRejected || _coordinator == null) return;
            try
            {
                _coordinator.Refresh(Time.time);
                if (_coordinator.IsRejected) RejectWiring("required pitch renderer was lost.");
                else { ApplyCurrentScreen(); _screens.Refresh(); }
            }
            catch (Exception exception)
            {
                RejectWiring("match refresh failed: " + exception.Message);
                Debug.LogException(exception, this);
            }
        }

        private void OnDestroy() { _coordinator?.Dispose(); _screens?.DisposeViews(); }
        private void OnApplicationQuit() => _coordinator?.Dispose();

        /// <summary>
        /// Collects only host facts: instance identity, <c>activeSelf</c>, whether every ancestor is
        /// active, and ancestor identities.
        /// All interpretation of those facts lives in <see cref="ClientShellWiringValidator"/>.
        /// A null serialized reference becomes the validator's documented zero-id missing sentinel.
        /// </summary>
        private static ClientShellRootSnapshot CaptureSnapshot(GameObject target)
        {
            if (target == null)
            {
                return default;
            }

            int ancestorCount = 0;
            Transform cursor = target.transform.parent;
            while (cursor != null)
            {
                ancestorCount++;
                cursor = cursor.parent;
            }

            int[] ancestorIds = new int[ancestorCount];
            cursor = target.transform.parent;
            for (int i = 0; i < ancestorIds.Length; i++)
            {
                ancestorIds[i] = cursor.gameObject.GetInstanceID();
                cursor = cursor.parent;
            }

            Transform parent = target.transform.parent;
            bool areAncestorsActive = parent == null || parent.gameObject.activeInHierarchy;

            return new ClientShellRootSnapshot(
                target.GetInstanceID(),
                target.activeSelf,
                areAncestorsActive,
                ancestorIds);
        }

        private void ApplyCurrentScreen()
        {
            ClientScreenVisibility visibility;
            try
            {
                visibility = ClientScreenVisibility.From(_coordinator.Flow.Current);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                RejectWiring("host-free screen visibility rejected the current screen: " + exception.Message);
                return;
            }

            if (!visibility.MainMenu) _mainMenuRoot.SetActive(false);
            if (!visibility.TacticsSetup) _tacticsSetupRoot.SetActive(false);
            if (!visibility.MatchView) _matchViewRoot.SetActive(false);
            if (!visibility.PostMatchReport) _postMatchReportRoot.SetActive(false);
            if (visibility.MainMenu) _mainMenuRoot.SetActive(true);
            if (visibility.TacticsSetup) _tacticsSetupRoot.SetActive(true);
            if (visibility.MatchView) _matchViewRoot.SetActive(true);
            if (visibility.PostMatchReport) _postMatchReportRoot.SetActive(true);
        }

        private void RequireReady(string action)
        {
            if (_wiringRejected || _coordinator == null)
            {
                throw new InvalidOperationException(
                    action + " cannot run because ClientShellBehaviour did not complete valid Awake wiring.");
            }
        }

        private void RejectWiring(string reason)
        {
            _wiringRejected = true;
            _screens?.DisposeViews();
            try { _coordinator?.Dispose(); }
            finally
            {
                enabled = false;
                DeactivateIfAssigned(_mainMenuRoot);
                DeactivateIfAssigned(_tacticsSetupRoot);
                DeactivateIfAssigned(_matchViewRoot);
                DeactivateIfAssigned(_postMatchReportRoot);
                Debug.LogError("ClientShellBehaviour rejected wiring: " + reason, this);
            }
        }

        private static void DeactivateIfAssigned(GameObject root)
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-09-04 | —      | P5b foundation: four-root visibility + Main Menu ⇄ Tactics.   |
// | 1.1     | 2026-09-07 | —      | PR #361 review: structural/exhaustiveness decisions stay in    |
// |         |            |        | gated client-app; rejection deactivates all roots; visibility  |
// |         |            |        | failures route through rejection; P4b lifecycle containment    |
// |         |            |        | coupling removed in favour of P4b-owned default-off demo boot. |
// | 1.2     | 2026-10-04 | —      | PR #470 refresh: existing lifecycle awaits its shell consumer; |
// |         |            |        | renderer Attach remains TO BUILD. Documentation only.       |
// | 1.3     | 2026-10-05 | —      | PR #470 review: reports parent activeInHierarchy so roots under|
// |         |            |        | an inactive ancestor are refused rather than shown blank.      |
// | 1.4     | 2026-10-06 | —      | Consume stable lifecycle coordinator; renderer containment and outgoing-first visibility. |
// | 1.5     | 2026-10-08 | —      | Compose the localized UGUI screens and apply visibility before view/focus refresh. |
#endregion
