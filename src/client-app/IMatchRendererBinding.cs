// File:     src/client-app/IMatchRendererBinding.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Spec:     P5b lifecycle/identity plan §§4–7, S0 binding contracts §2, Code Standards #20
// Purpose:  Consumed renderer boundary; host-free lifecycle owns playback and cleanup ordering.

using System;

using TacticalDirector.MatchClientCore;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>Unity and test renderer implementations attach externally owned sessions without starting them.</summary>
    public interface IMatchRendererBinding
    {
        /// <summary>Attaches synchronously, including on inactive roots before Awake. Failure throws.
        /// onFailure is called only on the client thread for later render/wiring destruction failures.</summary>
        void Attach(MatchSession session, MatchIdentityContext identity, ILiveFrameSource frames, Action onFailure);
        /// <summary>Clears references and hides/destroys only generated visuals; safe after partial attach.</summary>
        void Detach();
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Consumed P5b lifecycle/identity implementation. |
#endregion
