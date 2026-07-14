// File:     src/match-engine/SubstitutionReason.cs
// Created:  2026-07-14
// Modified: 2026-07-14
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-flow-completion-design.md) §6, Code Standards #20
// Purpose:  Substitution-reason ordinal for MatchEngine.SubstitutePlayer / SubstitutionEvent.

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Reason a substitution was made (<see cref="MatchEngine.SubstitutePlayer"/>).
    /// ORDINAL STABILITY: embedded in the Tier A <c>SubstitutionEvent</c> payload
    /// (<c>SubstitutionReason</c> byte field) — future additions MUST be appended at the end.
    /// </summary>
    public enum SubstitutionReason
    {
        Tactical = 0,
        Injury   = 1
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-14 | —      | Initial implementation. |
#endregion
