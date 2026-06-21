// File:     src/living-world/SpawnCause.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Living World System #22 §2.2.3, §3.6, KD-8, Code Standards #20
// Purpose:  SpawnCause value type: provenance captured inline at arc creation (KD-8).

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// Provenance recorded at every arc creation (FR-LW-016 / KD-8): the trigger and the input values
    /// that caused it, plus a reference to the world-state snapshot. Captured inline at spawn — it
    /// cannot be reconstructed later. Generated interactions carry NO SpawnCause: their provenance is
    /// implicit in (InteractionIntent, RNG cursor, snapshotRef) and reconstructable from the snapshot.
    /// </summary>
    public readonly struct SpawnCause
    {
        /// <summary>A single (key, value) input that fed the trigger.</summary>
        public readonly struct Input
        {
            public readonly short Key;
            public readonly float Value;

            public Input(short key, float value)
            {
                Key = key;
                Value = value;
            }
        }

        /// <summary>Which rule/threshold fired.</summary>
        public readonly ushort TriggerId;

        /// <summary>The input values at spawn (may be empty).</summary>
        public readonly Input[] Inputs;

        /// <summary>Reference to the world-state snapshot at spawn.</summary>
        public readonly ulong SnapshotRef;

        /// <summary>Calendar day of the spawn.</summary>
        public readonly uint WorldTick;

        public SpawnCause(ushort triggerId, Input[] inputs, ulong snapshotRef, uint worldTick)
        {
            TriggerId = triggerId;
            Inputs = inputs;
            SnapshotRef = snapshotRef;
            WorldTick = worldTick;
        }
    }
}
