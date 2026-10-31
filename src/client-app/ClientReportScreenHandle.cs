// File:     src/client-app/ClientReportScreenHandle.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Spec:     P5b lifecycle/identity plan §§4–7, S0 binding contracts §2, Code Standards #20
// Purpose:  Stable report source following the current match; cleared reports cannot leak into the next match.

using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>Registered once; unavailable until the current context finishes its full-time barrier.</summary>
    public sealed class ClientReportScreenHandle : IViewModelSource<ClientMatchReport>
    {
        private ClientMatchContext _context;
        internal void Install(ClientMatchContext context) => _context = context;
        /// <summary>Returns the frozen current report, or the unavailable default after clear.</summary>
        public ClientMatchReport Project() => _context?.Report ?? default;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Consumed P5b lifecycle/identity implementation. |
#endregion
