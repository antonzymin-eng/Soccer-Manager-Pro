// File:     src/player-progression/TrainingInputBatch.cs
// Created:  2026-08-08
// Modified: 2026-08-08
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §2.2 / FR-PG-021 (the batch daily entry point), KD-2;
//           Training System #29 §3.5 (the slot-1 composition #30 performs); ERR-029-006; Code Standards #20
// Purpose:  The per-club, per-player TrainingInput batch #30 gathers at slot 1 and hands to
//           ProgressionEngine.AdvanceDay — the batch shape FR-PG-021 mandates and #29 §3.5 composes
//           against, and whose absence was ERR-029-006.

using System;

namespace TacticalDirector.PlayerProgression
{
    /// <summary>
    /// One club's per-player training contributions for a single world day. The ids are carried
    /// alongside the inputs so <see cref="ProgressionEngine.AdvanceDay"/> can verify the batch describes
    /// the players it is about to advance, rather than trusting two arrays to be in the same order.
    /// </summary>
    public readonly struct ClubTrainingInputs
    {
        /// <summary>The club these inputs are for.</summary>
        public readonly int ClubId;

        /// <summary>The player ids the inputs are keyed by. Index <c>i</c> pairs with <see cref="Inputs"/>[i]. Never null.</summary>
        public readonly int[] PlayerIds;

        /// <summary>The matching per-player contributions. Never null.</summary>
        public readonly TrainingInput[] Inputs;

        /// <summary>Binds a club id and its parallel id / input arrays.</summary>
        /// <exception cref="ArgumentNullException">Either array is null.</exception>
        /// <exception cref="ArgumentException">The arrays have different lengths.</exception>
        public ClubTrainingInputs(int clubId, int[] playerIds, TrainingInput[] inputs)
        {
            if (playerIds == null)
            {
                throw new ArgumentNullException(nameof(playerIds));
            }

            if (inputs == null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            if (playerIds.Length != inputs.Length)
            {
                throw new ArgumentException(
                    $"Club {clubId} supplied {playerIds.Length} player id(s) but {inputs.Length} "
                    + "training input(s) — the batch pairs one input with every id.",
                    nameof(inputs));
            }

            ClubId = clubId;
            PlayerIds = playerIds;
            Inputs = inputs;
        }
    }

    /// <summary>
    /// The whole league's training contributions for one world day — the <c>trainingInputs</c> argument
    /// of #28's batch daily entry point (FR-PG-021), gathered by #30 at slot 1 from #29's
    /// <c>ComputeTrainingInput</c> (#29 §3.5).
    /// <para>
    /// <b>Why a batch at all, when <see cref="TrainingInput"/> is presently empty.</b> Both sides of this
    /// seam are specified — FR-PG-021 mandates this signature and #29's APPROVED §3.5 composes against
    /// it — so it is a specified seam, not the phantom interface FR-LW-031 forbids. The struct gains its
    /// fields by APPEND at #28 T3, at which point every call site here already routes the right player's
    /// contribution to the right player. Landing the shape now is what closes ERR-029-006.
    /// </para>
    /// <para>
    /// <b><see cref="Neutral"/> is explicit, not a silent default.</b> An unbound batch means "no
    /// training anywhere", which FR-PG-009 requires to be byte-identical to no training input. A batch
    /// that IS bound must describe exactly the players being advanced — a partially-supplied batch is
    /// refused rather than having the gaps quietly filled with <see cref="TrainingInput.Neutral"/>,
    /// because a dropped player would then be indistinguishable from an untrained one.
    /// </para>
    /// </summary>
    public readonly struct TrainingInputBatch
    {
        /// <summary>The per-club contributions, or <c>null</c> for the <see cref="Neutral"/> batch.</summary>
        public readonly ClubTrainingInputs[] Clubs;

        /// <summary>Binds the per-club contributions.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="clubs"/> is null — use
        /// <see cref="Neutral"/> to say "no training anywhere", so that the empty case is stated rather
        /// than arrived at.</exception>
        public TrainingInputBatch(ClubTrainingInputs[] clubs)
        {
            Clubs = clubs ?? throw new ArgumentNullException(nameof(clubs),
                "Use TrainingInputBatch.Neutral for a day with no training contributions.");
        }

        /// <summary>
        /// The identity batch: no contribution for any player, byte-identical to no training input
        /// (FR-PG-009). This is what #30 supplies until #28 T3 gives <see cref="TrainingInput"/> fields.
        /// </summary>
        public static TrainingInputBatch Neutral => default;

        /// <summary>True when this batch carries no per-club contributions at all.</summary>
        public bool IsNeutral => Clubs == null;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-08 | —      | #28 T1 (ERR-029-006): the FR-PG-021 batch parameter #29 §3.5   |
// |         |            |        | composes against. Ids carried with the inputs so AdvanceDay    |
// |         |            |        | can verify the pairing rather than trust array order.          |
#endregion
