// File:     src/injuries-medical/InjuryState.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Injuries & Medical #41 §2.2 (FR-MD-002/018/020), F1/F6; Code Standards #20
// Purpose:  The #41-owned per-player medical state — the single source of truth for a player's injury
//           status. Every field is serialized at T1; no RNG cursor exists to serialize.

namespace TacticalDirector.InjuriesMedical
{
    /// <summary>
    /// Per-player world-tick medical state (§2.2) — the single source of truth for injury status
    /// (FR-MD-002). Mutated only by <see cref="MedicalStep.AdvanceMedicalDay"/> (FR-MD-003).
    /// <para>
    /// <b><c>default(InjuryState)</c> is NOT a valid runtime state.</b> Its
    /// <see cref="LastAdvancedWorldDay"/> would be 0 — a legitimate world day — so the first advance of
    /// world day 0 would read as "already advanced" and that player would never be evaluated for
    /// injury again. Construct through <see cref="Create"/>, which seeds
    /// <see cref="InjuriesMedicalConstants.MEDICAL_NOT_ADVANCED_SENTINEL"/>. FR-MD-025 requires
    /// exactly this when #30 inserts a regen's state at the season boundary.
    /// </para>
    /// <para>
    /// <b>Nothing beyond these four fields needs persisting.</b> #41's occurrence draw is keyed on
    /// <c>(playerId, worldDay, purpose)</c> rather than advanced from a cursor (KD-1), so there is no
    /// RNG stream state to save and no way for a save/restore boundary to shift a future draw
    /// (FR-MD-007).
    /// </para>
    /// </summary>
    public struct InjuryState
    {
        /// <summary>The active injury tier; <see cref="InjurySeverity.None"/> means available.</summary>
        public InjurySeverity Severity;

        /// <summary>
        /// World-days of recovery left, in <c>[0, InjuriesMedicalConstants.RecoveryMax]</c>. Coherent
        /// with <see cref="Severity"/> by invariant: it is zero if and only if the severity is
        /// <see cref="InjurySeverity.None"/> (F1). A violation is a bug and fails loud at the consuming
        /// seam rather than being repaired.
        /// </summary>
        public int RecoveryRemaining;

        /// <summary>Cumulative career injuries — history, and the deep-tier recurrence input. Never reset by recovery.</summary>
        public int InjuryCount;

        /// <summary>
        /// The idempotency cursor (F6): the last world day <see cref="MedicalStep.AdvanceMedicalDay"/>
        /// advanced, or <see cref="InjuriesMedicalConstants.MEDICAL_NOT_ADVANCED_SENTINEL"/> when the
        /// state has never been advanced.
        /// </summary>
        public uint LastAdvancedWorldDay;

        /// <summary>
        /// The only valid way to construct a runtime state (§2.2): healthy, no recovery outstanding, no
        /// injury history, and the never-advanced sentinel. FR-MD-025 requires this — never
        /// <c>default</c> — when #30 inserts a regen's state.
        /// </summary>
        public static InjuryState Create()
        {
            return new InjuryState
            {
                Severity = InjurySeverity.None,
                RecoveryRemaining = 0,
                InjuryCount = 0,
                LastAdvancedWorldDay = InjuriesMedicalConstants.MEDICAL_NOT_ADVANCED_SENTINEL,
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                  |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#41 T0, §2.2). |
#endregion
