// File:     src/client-app/ClientMatchScreenHandle.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Spec:     P5b lifecycle/identity plan §§4–7, S0 binding contracts §2, Code Standards #20
// Purpose:  Stable registered source/dispatcher that follows the privately installed match context.

using System;

using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>Registered once per shell; empty/refusing before attach and after clear.</summary>
    public sealed class ClientMatchScreenHandle : IViewModelSource<MatchFrameView>, ICommandDispatcher
    {
        private ClientMatchContext _context;
        internal void Install(ClientMatchContext context) => _context = context;
        /// <summary>Projects the current match's accepted frame; never a previous session's cache.</summary>
        public MatchFrameView Project() => _context?.Source?.Project() ?? MatchFrameView.Empty;
        /// <summary>Enqueues only through the current valid live match context.</summary>
        public void Dispatch(in ManagerIntent intent)
        {
            if (_context == null || !_context.IsValid) throw new InvalidOperationException("No attached match dispatcher.");
            _context.Dispatch(in intent);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Consumed P5b lifecycle/identity implementation. |
#endregion
