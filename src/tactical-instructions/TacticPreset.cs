// File:     src/tactical-instructions/TacticPreset.cs
// Created:  2026-07-10
// Modified: 2026-07-10
// Author:   —
// Spec:     Tactical Presets #26 §2.2.1 (FR-TP-001/014), Code Standards #20
// Purpose:  An immutable named tactic bundle: Name + one TeamTactic + optional roster-indexed
//           PlayerTactic[]. The name is authoring metadata only — never serialized into the
//           world-state digest, never read by any AI tick.

using System;

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// One tactic preset (#26 §2.2.1, FR-TP-001): a named point in the existing #21 parameter
    /// space. Composes only #21 value types — a preset introduces no new tunable magnitude beyond
    /// selecting existing enum members / pinned dial values (FR-TP-003 / KD-7).
    /// <see cref="Name"/> is authoring metadata only (never digest-serialized, never read by an AI
    /// tick); serialized identity is the preset's ordinal in <see cref="TacticPresetLibrary"/>.
    /// </summary>
    public readonly struct TacticPreset
    {
        /// <summary>Authoring/display name (FR-TP-001). Metadata only.</summary>
        public string Name { get; }

        /// <summary>The team tactic this preset applies (#21 value type, as-is).</summary>
        public TeamTactic Team { get; }

        /// <summary>
        /// Optional roster-indexed per-agent tactics; null = every agent keeps the identity
        /// <c>PlayerTactic.Default</c>. When present, must be full roster length — validated via
        /// <see cref="ValidatePlayers"/> at library construction (FR-TP-014 / F1). The Stage-0+1
        /// catalogue carries none (team-dial compositions only, #26 Appendix A.1).
        /// </summary>
        public PlayerTactic[] Players { get; }

        /// <summary>Constructs a preset; refuses a null/empty name (fail loud — F1 class).</summary>
        public TacticPreset(string name, in TeamTactic team, PlayerTactic[] players = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Preset name must be non-empty (FR-TP-001).", nameof(name));
            }
            Name = name;
            Team = team;
            Players = players;
        }

        /// <summary>
        /// FR-TP-014 gate: when <see cref="Players"/> is present it must be exactly
        /// <paramref name="rosterSize"/> long. Called at library construction (fail loud, F1).
        /// </summary>
        /// <param name="rosterSize">The roster length the consumer expects (e.g. SQUAD_SIZE).</param>
        public void ValidatePlayers(int rosterSize)
        {
            if (Players != null && Players.Length != rosterSize)
            {
                throw new ArgumentException(
                    $"Preset '{Name}': Players length {Players.Length} != roster size {rosterSize} (FR-TP-014).");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                   |
// | 1.0     | 2026-07-10 | —      | Initial implementation (#26 T0). |
#endregion
