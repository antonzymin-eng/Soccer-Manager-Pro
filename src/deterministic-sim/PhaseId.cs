// File:     src/deterministic-sim/PhaseId.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Deterministic Simulation #16 §3.1.2, §3.6.1, Code Standards #20
// Purpose:  Canonical 7-phase tick pipeline enumeration. Ordinal values are part of the digest protocol
//           and MUST remain stable. Reordering or renumbering requires a SNAPSHOT_SCHEMA_VERSION bump.
//           AR-1 H-4 fix: added Events=5, corrected ordinals after AI, removed AI_NoOp (AI phase
//           runs as no-op on non-stride ticks but occupies the same ordinal slot 2 in the digest stream).

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Canonical phase identifiers for the 60 Hz tick pipeline.
    /// Ordinals are embedded in phase digests; they MUST NOT be reordered without a schema version bump.
    /// Canonical phase order: Input(0) → Intent(1) → AI(2) → Physics(3) → Resolve(4) → Events(5) → Snapshot(6).
    /// On non-AI-stride ticks, the AI phase runs as a no-op (no subsystem writes) but still occupies
    /// ordinal 2 in the digest chain so the stream remains invariant. §3.1.2.
    /// Deterministic Simulation #16 §3.1.2 / §3.6.1.
    /// </summary>
    public enum PhaseId : byte
    {
        /// <summary>Phase 0: Input collection. Writes: input buffer only. §3.6.1.</summary>
        Input = 0,

        /// <summary>Phase 1: Intent assembly. Writes: intent queue. §3.6.1.</summary>
        Intent = 1,

        /// <summary>Phase 2: AI evaluation (or AI no-op on non-stride ticks).
        /// Active on ticks where tick % AI_PHASE_STRIDE == 0; emits empty phase digest on no-op ticks.
        /// Writes: decision buffers (stride ticks only). §3.1.2 / §3.6.1.</summary>
        AI = 2,

        /// <summary>Phase 3: Physics integration. Writes: transforms, velocities. §3.6.1.</summary>
        Physics = 3,

        /// <summary>Phase 4: Conflict resolution. Writes: conflict resolution outputs; DespawnLog append-only. §3.6.1.</summary>
        Resolve = 4,

        /// <summary>Phase 5: Event commitment. Writes: event ledger in canonical sequence-id order. §3.6.1.</summary>
        Events = 5,

        /// <summary>Phase 6: Snapshot serialization. Writes: serialized payload bytes and digest chain.
        /// Also the EndOfSnapshot boundary ordinal used by ReplayCursor step-7 assertion (§4.2.2 step 7).</summary>
        Snapshot = 6,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                              |
// | 1.1     | 2026-05-29 | —      | AR-1 H-4: added Events=5, corrected Physics=3/Resolve=4, removed    |
// |         |            |        | AI_NoOp separate ordinal (no-op uses same ordinal 2 as AI).          |
#endregion
