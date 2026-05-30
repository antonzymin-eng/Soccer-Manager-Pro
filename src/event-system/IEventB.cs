// File:     src/event-system/IEventB.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Event System #17 §3.1.3, §4.2.1, Code Standards #20
// Purpose:  Tier B marker interface. Declared at Stage 0 to prevent Stage 5+ Tier-B traffic
//           being silently routed onto Tier A paths. No Tier B events exist at Stage 0.

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Tier B event marker — bounded-authoritative; uses #16 §3.5 tolerance for continuous fields.
    /// Declared at Stage 0 per KD-3 rationale (§4.2.1): omitting it would silently push Stage 5+
    /// Tier-B traffic onto Tier A paths and break the per-tier digest contract.
    /// No Tier B events are registered at Stage 0. Activation: Stage 5+.
    /// Event System #17 §3.1.3 / §4.2.1.
    /// </summary>
    public interface IEventB { }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation. |
#endregion
