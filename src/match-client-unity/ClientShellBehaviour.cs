// File:     src/match-client-unity/ClientShellBehaviour.cs
// Created:  2026-09-04
// Modified: 2026-09-07 (PR #361 review follow-up)
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P5b),
//           UI / Client Framework #38 §3.2 (FR-UI-009/010/011), Code Standards #20 §12 rule 1
// Purpose:  Thin Unity binding for the client screen shell. It collects host facts, forwards them to
//           gate-compiled client-app decisions, applies the resulting visibility, and forwards UI events.

using System;

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
    /// <para>
    /// This first P5b slice intentionally exposes only Main Menu → Tactics Setup and its cancel edge.
    /// The Tactics Setup → Match View edge stays withheld until match lifecycle is extracted host-free.
    /// The shell deliberately knows nothing about the Match View implementation type or lifecycle host.
    /// P4b's temporary demo self-boot is owned by <see cref="MatchClientBehaviour"/> and is opt-in,
    /// default-off; the later <c>Attach(MatchSession)</c> refactor removes that scaffolding entirely.
    /// </para>
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
        [Header("P5b screen roots — mutually exclusive children of an always-active shell")]
        [SerializeField] private GameObject _mainMenuRoot;
        [SerializeField] private GameObject _tacticsSetupRoot;
        [SerializeField] private GameObject _matchViewRoot;
        [SerializeField] private GameObject _postMatchReportRoot;

        private ClientScreenFlow _flow;
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

            // P5b foundation STUB registrations: this slice binds only screen identity/visibility.
            // Null handles are legal per ScreenRegistration. They are intentionally temporary rather
            // than a product contract: the lifecycle extraction that precedes StartMatch will replace
            // MatchView's registration with its real MatchViewModelSource/MatchTacticsDispatcher, and
            // later screens receive their own real sources as their producers are bound.
            ScreenRegistration mainMenuRegistration =
                new ScreenRegistration(ClientScreens.MainMenu, null, null);
            ScreenRegistration tacticsSetupRegistration =
                new ScreenRegistration(ClientScreens.TacticsSetup, null, null);
            ScreenRegistration matchViewRegistration =
                new ScreenRegistration(ClientScreens.MatchView, null, null);
            ScreenRegistration postMatchReportRegistration =
                new ScreenRegistration(ClientScreens.PostMatchReport, null, null);

            _flow = new ClientScreenFlow(
                in mainMenuRegistration,
                in tacticsSetupRegistration,
                in matchViewRegistration,
                in postMatchReportRegistration);

            ApplyCurrentScreen();
        }

        /// <summary>Forwards Main Menu's New Demo Match action to the host-free navigation graph.</summary>
        public void OpenTacticsSetup()
        {
            RequireReady(nameof(OpenTacticsSetup));
            _flow.OpenTacticsSetup();
            ApplyCurrentScreen();
        }

        /// <summary>Forwards Tactics Setup's Cancel action to the host-free navigation graph.</summary>
        public void CancelTacticsSetup()
        {
            RequireReady(nameof(CancelTacticsSetup));
            _flow.CancelTacticsSetup();
            ApplyCurrentScreen();
        }

        /// <summary>
        /// Collects only host facts: instance identity, <c>activeSelf</c>, and ancestor identities.
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

            return new ClientShellRootSnapshot(target.GetInstanceID(), target.activeSelf, ancestorIds);
        }

        private void ApplyCurrentScreen()
        {
            ClientScreenVisibility visibility;
            try
            {
                visibility = ClientScreenVisibility.From(_flow.Current);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                RejectWiring("host-free screen visibility rejected the current screen: " + exception.Message);
                return;
            }

            _mainMenuRoot.SetActive(visibility.MainMenu);
            _tacticsSetupRoot.SetActive(visibility.TacticsSetup);
            _matchViewRoot.SetActive(visibility.MatchView);
            _postMatchReportRoot.SetActive(visibility.PostMatchReport);
        }

        private void RequireReady(string action)
        {
            if (_wiringRejected || _flow == null)
            {
                throw new InvalidOperationException(
                    action + " cannot run because ClientShellBehaviour did not complete valid Awake wiring.");
            }
        }

        private void RejectWiring(string reason)
        {
            _wiringRejected = true;
            enabled = false;

            // Rejection is a terminal presentation state, not "whatever the scene happened to contain".
            DeactivateIfAssigned(_mainMenuRoot);
            DeactivateIfAssigned(_tacticsSetupRoot);
            DeactivateIfAssigned(_matchViewRoot);
            DeactivateIfAssigned(_postMatchReportRoot);

            Debug.LogError("ClientShellBehaviour rejected wiring: " + reason, this);
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
#endregion
