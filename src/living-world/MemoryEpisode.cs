// File:     src/living-world/MemoryEpisode.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Living World System #22 §2.2.2, §3.2, Code Standards #20
// Purpose:  MemoryEpisode value type: one entry in a RelationshipEdge's bounded ring buffer.

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// One episodic memory on a relationship edge. #22 §2.2.2 / §3.2.
    /// Plain serialisable value type (FR-LW-022). EntityId-style ids are int per project convention
    /// (no shared EntityId type exists; -1 is the loose/none sentinel elsewhere in the codebase).
    /// </summary>
    public readonly struct MemoryEpisode
    {
        /// <summary>Stable per-edge handle, monotonic; the durable id an arc pins (FR-LW-009).</summary>
        public readonly uint EpisodeId;

        /// <summary>The off-pitch event this episode records.</summary>
        public readonly EventKind Kind;

        /// <summary>Salience in [0,1]; decays per [GT] rate, lowest evicts first (FR-LW-010).</summary>
        public readonly float Salience;

        /// <summary>Calendar day of the episode (one worldTick = one day; FR-LW-019).</summary>
        public readonly uint WorldTick;

        /// <summary>Which manager response produced it (stable persisted id).</summary>
        public readonly ushort ManagerChoiceId;

        public MemoryEpisode(uint episodeId, EventKind kind, float salience, uint worldTick, ushort managerChoiceId)
        {
            EpisodeId = episodeId;
            Kind = kind;
            Salience = salience;
            WorldTick = worldTick;
            ManagerChoiceId = managerChoiceId;
        }

        /// <summary>Returns this episode with salience decayed by one tick: s' = s · (1 − rate). §3.2.</summary>
        public MemoryEpisode WithDecayedSalience(float decayRate)
            => new MemoryEpisode(EpisodeId, Kind, Salience * (1f - decayRate), WorldTick, ManagerChoiceId);
    }
}
