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
        /// <c>PlayerTactic.Default</c>. When present, must be full roster length — the FR-TP-014
        /// gate is <see cref="ValidatePlayers"/>, run by the consuming applier seam (the T1
        /// preset→config projection), where the roster size is known; this bottom-of-graph
        /// assembly cannot reference a roster-size constant itself. The Stage-0+1 catalogue
        /// carries none (team-dial compositions only, #26 Appendix A.1 — test-locked), so
        /// FR-TP-014's library-construction mandate is vacuously satisfied at Stage 0.
        /// The array is snapshot-copied at construction (post-construction mutation of the
        /// caller's array never reaches the preset); the getter hands out the internal snapshot
        /// — treat it as read-only (the #18 AR-1 L-3 / match-viewer AR-1 L-1 hand-over convention).
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
            // Snapshot-copy BEFORE the preset exposes it: a retained live caller array would let
            // post-construction mutation bypass the FR-TP-014 gate (the living-world slice-2 AR-1
            // M-1 / match-viewer AR-3 M-1 defect class).
            Players = players == null ? null : (PlayerTactic[])players.Clone();
        }

        /// <summary>
        /// FR-TP-014 gate: when <see cref="Players"/> is present it must be exactly
        /// <paramref name="rosterSize"/> long. Run by the consuming applier before projection
        /// (fail loud, F1); a null <see cref="Players"/> always validates.
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
// | 1.1     | 2026-07-10 | —      | T0 AR-1 M-1: ctor snapshot-copies Players (retained live caller  |
// |         |            |        |   array bypassed the FR-TP-014 gate — the slice-2 AR-1 M-1 class);|
// |         |            |        |   L-2: Players/ValidatePlayers docs re-anchored to the consuming  |
// |         |            |        |   applier seam (the library performs no validation call and       |
// |         |            |        |   cannot know roster size; Stage-0 mandate vacuously satisfied).  |
#endregion
