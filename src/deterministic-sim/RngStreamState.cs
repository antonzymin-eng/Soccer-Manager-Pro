// File:     src/deterministic-sim/RngStreamState.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Deterministic Simulation #16 §3.2.5, §3.4, Code Standards #20
// Purpose:  Per-stream RNG state for the DeterministicRngService reservation API.
//           Tracks StreamKey, RngCursor, ActionOrdinal, and the current reserved budget.

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Mutable per-stream state for the DeterministicRngService.
    /// One instance per registered draw-site stream. Serialised into SnapshotPayload for replay.
    /// Deterministic Simulation #16 §3.2.5.
    /// </summary>
    public struct RngStreamState
    {
        /// <summary>SipHash-2-4 stream key derived from subsystemId ‖ entityId ‖ streamVersion. §3.2.5.</summary>
        public ulong StreamKey;

        /// <summary>Draw counter: number of values consumed from this stream so far. §3.2.5.</summary>
        public ulong RngCursor;

        /// <summary>Reservation evaluation index: the per-evaluation draw-site call order counter. §3.2.5 / §3.2.5.1.</summary>
        public ulong ActionOrdinal;

        /// <summary>Remaining draws in the current Reserve() budget. 0 when no reservation is active.</summary>
        public int BudgetRemaining;

        /// <summary>Total budget declared by the current Reserve() call (for mismatch detection). §3.4 / FR-DS-012.</summary>
        public int DeclaredBudget;

        /// <summary>Draw index within the current reservation window (advances in DrawReserved). §3.2.5.</summary>
        public int DrawIndex;

        /// <summary>Stable draw-site string identifier. Stored for ERR_DS_RNG_BUDGET_MISMATCH diagnostics.</summary>
        public string SiteId;

        /// <summary>Stream version — bumped on any reordering of draw sites per §3.2.5.1 declarationOrdinal rules.</summary>
        public ushort StreamVersion;

        /// <summary>Subsystem ordinal owning this stream. §3.1.1.</summary>
        public int SubsystemOrdinal;

        /// <summary>EntityId owning this stream. Negative for subsystem-wide (non-entity) streams.</summary>
        public int EntityId;

        /// <summary>Resets the reservation window after all draws in a budget are consumed or skipped.</summary>
        public void ClearReservation()
        {
            BudgetRemaining = 0;
            DeclaredBudget  = 0;
            DrawIndex       = 0;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
