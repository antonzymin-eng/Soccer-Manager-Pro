// File:     src/training-system/TrainingViewModel.cs
// Created:  2026-07-31
// Modified: 2026-07-31
// Author:   —
// Spec:     Training System #29 §2.2 (KD-7) / FR-TR-022; Code Standards #20
// Purpose:  Read-only value-copy observer surface over one player's training state, for #31/#38.

namespace TacticalDirector.TrainingSystem
{
    /// <summary>
    /// The KD-7 read-only observer projection of one player's <see cref="TrainingState"/> (FR-TR-022),
    /// for Transfers #31 and the UI / Client Framework #38.
    /// </summary>
    /// <remarks>
    /// VALUE COPIES, not a handle. The observer surface deliberately hands out copies of the three
    /// player-facing fields rather than a reference into (or a <c>ref</c> onto) the club's live state:
    /// a viewer must not be able to mutate the simulation it is observing, and a stale copy is a
    /// display artefact where a live handle would be a determinism defect. <see cref="TrainingState"/>
    /// itself is a mutable struct with public fields, so exposing it directly would hand a caller a
    /// writable copy whose writes silently go nowhere — which reads as an observer bug either way.
    /// <para>
    /// The idempotency cursor (<c>LastAdvancedWorldDay</c>) is deliberately NOT projected: it is
    /// internal bookkeeping for the F6 guard, not a player-facing quantity, and surfacing it would
    /// invite a consumer to reason about the day cursor #29 alone owns.
    /// </para>
    /// </remarks>
    public readonly struct TrainingViewModel
    {
        /// <summary>Initializes the value-copy projection.</summary>
        /// <param name="playerId">The #27 <c>PlayerRecord.PlayerId</c> this projection describes.</param>
        /// <param name="focus">A copy of the player's persistent training focus.</param>
        /// <param name="condition">A copy of the conditioning cursor.</param>
        /// <param name="trainingFatigue">A copy of the world-tick training-fatigue accumulator.</param>
        public TrainingViewModel(int playerId, TrainingFocus focus, int condition, int trainingFatigue)
        {
            PlayerId = playerId;
            Focus = focus;
            Condition = condition;
            TrainingFatigue = trainingFatigue;
        }

        /// <summary>The player this projection describes.</summary>
        public int PlayerId { get; }

        /// <summary>The player's persistent training focus (FR-TR-003 — the single source of truth is the state).</summary>
        public TrainingFocus Focus { get; }

        /// <summary>The conditioning cursor at projection time.</summary>
        public int Condition { get; }

        /// <summary>The world-tick training-fatigue accumulator at projection time (never match fatigue).</summary>
        public int TrainingFatigue { get; }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-31 | —      | AR-1 H-2: the FR-TR-022 / KD-7 observer surface (value copies). |
#endregion
