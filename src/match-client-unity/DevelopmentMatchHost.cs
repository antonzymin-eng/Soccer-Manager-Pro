// File:     src/match-client-unity/DevelopmentMatchHost.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Spec:     P5b lifecycle/identity plan §§4–7, S0 binding contracts §2, Code Standards #20
// Purpose:  Explicit isolated scene fixture consuming the production S0 lifecycle; no implicit renderer boot.

using System;

using Unity.Profiling;
using UnityEngine;

using TacticalDirector.ClientApp;

namespace TacticalDirector.MatchClientUnity
{
    /// <summary>Explicit scene-only developer host. Does not expose an incomplete player Start/report journey.</summary>
    public sealed class DevelopmentMatchHost : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("DevelopmentMatchHost.Update");
        [SerializeField] private MatchClientBehaviour _renderer;
        private ClientMatchCoordinator _coordinator;
        private void Start()
        {
            try
            {
                if (_renderer == null) throw new InvalidOperationException("Development renderer is not assigned.");
                _coordinator = new ClientMatchCoordinator(_renderer, S0DemoFixture.CreateApproved());
                _coordinator.OpenTacticsSetup();
                _coordinator.StartMatch();
            }
            catch (Exception exception) { Reject(exception); }
        }
        private void Update()
        {
            using var updateScope = UpdateMarker.Auto();
            if (_coordinator == null) return;
            try
            {
                _coordinator.Refresh(Time.time);
                if (_coordinator.IsRejected) throw new InvalidOperationException("Development attachment was rejected.");
            }
            catch (Exception exception) { Reject(exception); }
        }
        private void Reject(Exception exception)
        {
            _coordinator?.Dispose();
            enabled = false;
            Debug.LogException(exception, this);
        }
        private void OnDestroy() => _coordinator?.Dispose();
        private void OnApplicationQuit() => _coordinator?.Dispose();
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Consumed P5b lifecycle/identity implementation. |
#endregion
