// File:     src/match-engine/ManagerMode.cs
// Created:  2026-07-11
// Modified: 2026-07-11
// Author:   —
// Spec:     Tactical Presets #26 §2.2.4 (FR-TP-007/013, KD-4); Code Standards #20
// Purpose:  Per-team manager mode: Human (zero-value identity — no manager-AI selection,
//           adaptation, or engine calls) or AI (opts the team into the #26 decision loop).

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Per-team manager mode (#26 FR-TP-007 / KD-4). <see cref="Human"/> is the zero-value
    /// identity: no kickoff selection, no adaptation, no engine calls — a default match is
    /// byte-identical to pre-#26. <see cref="AI"/> opts a team into the manager-AI decision loop.
    ///
    /// ORDINAL STABILITY (FR-TP-013): byte-backed and serialized into the world-state snapshot
    /// (Appendix C, SNAPSHOT_SCHEMA_VERSION v13), so members are APPEND-only — inserting or
    /// reordering would silently re-map every persisted manager state.
    /// </summary>
    public enum ManagerMode : byte
    {
        /// <summary>No manager AI — the team's tactics are whatever the human/config set (identity).</summary>
        Human = 0,

        /// <summary>AI-managed: kickoff selection (T3) + in-match adaptation (T4) run for this team.</summary>
        AI = 1,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-07-11 | —      | Initial implementation (#26 §2.2.4). |
#endregion
