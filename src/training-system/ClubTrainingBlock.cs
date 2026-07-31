// File:     src/training-system/ClubTrainingBlock.cs
// Created:  2026-07-31
// Modified: 2026-07-31
// Author:   —
// Spec:     Training System #29 §2.2 / §4.2 / §4.3 (the FR-TR-025 insert/remove seam);
//           FR-TR-002/003/019/022/023/025; F2/F4/F7; Code Standards #20
// Purpose:  The per-club TrainingState container keyed by PlayerId — the structure SetFocus, the
//           derived schedule view, the observer projection, and (at T1) the save codec attach to.

using System;
using System.Collections.Generic;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.TrainingSystem
{
    /// <summary>
    /// One club's set of per-player <see cref="TrainingState"/>s, keyed by <c>PlayerId</c> (§2.2). This
    /// is the #29-owned block FR-TR-019 persists (per club, per player) once the T1
    /// <c>TrainingSaveCodec</c> lands, and the surface #30 drives: the FR-TR-025 insert/remove
    /// lifecycle at the season boundary, the FR-TR-023 <see cref="SetFocus"/> command, and the daily
    /// slot-2 advance.
    /// </summary>
    /// <remarks>
    /// ORDERING IS PART OF THE CONTRACT. Entries are held in two parallel lists kept sorted by ascending
    /// <c>PlayerId</c>, NOT in a <see cref="Dictionary{TKey,TValue}"/>. A dictionary's enumeration order
    /// is unspecified and, in practice, a function of insertion and removal history — so a club whose
    /// roster churned across a season boundary could iterate its players in a different order than an
    /// identically-seeded career that reached the same roster by a different route. #29 is required to
    /// be fully deterministic (FR-TR-008) and #30 iterates clubs' players to build #28's batch, so the
    /// iteration order must be a function of the CONTENT alone. Ascending <c>PlayerId</c> is that
    /// function; membership tests are a binary search over the same ordering.
    /// <para>
    /// SINGLE SOURCE OF TRUTH. Focus lives on <see cref="TrainingState.Focus"/> and nowhere else
    /// (FR-TR-003). <see cref="Schedule"/> is a derived read-only view over these states — never a
    /// second stored copy — and is not separately serialized (FR-TR-019).
    /// </para>
    /// <para>
    /// STATES ARE HANDED OUT AS COPIES. <see cref="GetState"/> returns a value copy and there is no
    /// <c>ref</c> accessor, so the block is the only writer of the states it owns (the FR-TR-025
    /// lifecycle and the F6 idempotency cursor cannot be side-stepped). Mutation therefore goes through
    /// <see cref="AdvanceTrainingDay"/>, which routes to <see cref="TrainingStep"/>'s pure step — the
    /// block adds membership resolution, never a second training model. The pure slot-1 read
    /// (<see cref="TrainingStep.ComputeTrainingInput"/>) deliberately has NO wrapper here: it needs no
    /// mutation, so a caller composes it with a <see cref="GetState"/> copy and there is exactly one
    /// call shape for it.
    /// </para>
    /// <para>
    /// PERSISTENCE IS DEFERRED (T1). §4.2 marks <c>TrainingSaveCodec.cs</c> as the one file that lands
    /// at T1: an opaque, independently version-gated sub-blob under #30's <c>SeasonSaveCodec</c>, gated
    /// by <see cref="TrainingSystemConstants.TRAINING_SAVE_FORMAT_VERSION"/> with the F3/F5 fail-loud
    /// posture. This block is the structure that codec will walk, in this same ascending-id order.
    /// </para>
    /// </remarks>
    public sealed class ClubTrainingBlock
    {
        private readonly List<int> _playerIds = new List<int>();
        private readonly List<TrainingState> _states = new List<TrainingState>();

        /// <summary>Initializes an empty block for one club.</summary>
        /// <param name="clubId">The owning club's id (#27 <c>Squad.ClubId</c>).</param>
        public ClubTrainingBlock(int clubId)
        {
            ClubId = clubId;
        }

        /// <summary>The club this block belongs to.</summary>
        public int ClubId { get; }

        /// <summary>The number of players with a training state in this block.</summary>
        public int Count => _playerIds.Count;

        /// <summary>
        /// The FR-TR-003 derived read-only view over this block's per-player focus values. Recreated on
        /// each read — it holds no focus of its own.
        /// </summary>
        public TrainingSchedule Schedule => new TrainingSchedule(this);

        /// <summary>True when this block holds a training state for <paramref name="playerId"/>.</summary>
        public bool Contains(int playerId) => IndexOf(playerId) >= 0;

        /// <summary>
        /// The FR-TR-025 insert half of the roster-membership lifecycle: seeds a fresh player's state
        /// via <see cref="TrainingState.Create"/> with <see cref="TrainingFocus.Balanced"/>. #30 calls
        /// this at the season boundary for each fresh <c>PlayerId</c> in a #28 <c>RegenResult</c>.
        /// </summary>
        /// <remarks>
        /// Always <see cref="TrainingState.Create"/>, never <c>default(TrainingState)</c> — a default
        /// state carries <c>LastAdvancedWorldDay = 0</c>, which reads as "day 0 already advanced" and
        /// reintroduces the day-0 trap the sentinel exists to prevent (F7). A duplicate insert fails
        /// loud rather than resetting a live player's conditioning to the new-player seed.
        /// </remarks>
        /// <exception cref="ArgumentException">A state already exists for <paramref name="playerId"/>.</exception>
        public void Insert(int playerId)
        {
            int index = IndexOf(playerId);
            if (index >= 0)
            {
                throw new ArgumentException(
                    $"Player {playerId} already has a training state in club {ClubId}.",
                    nameof(playerId));
            }

            int insertAt = ~index; // BinarySearch's complement — the sorted insertion point.
            _playerIds.Insert(insertAt, playerId);
            _states.Insert(insertAt, TrainingState.Create(TrainingFocus.Balanced));
        }

        /// <summary>
        /// The FR-TR-025 remove half: drops a departed player's state. #30 calls this at the season
        /// boundary for each retiree in a #28 <c>RetirementResult</c>, so the block cannot leak entries
        /// across seasons.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// No state exists for <paramref name="playerId"/> — a missing entry is a lifecycle bug, not a
        /// no-op (F7): silently ignoring it would hide the insert that never happened.
        /// </exception>
        public void Remove(int playerId)
        {
            int index = RequireIndex(playerId);
            _playerIds.RemoveAt(index);
            _states.RemoveAt(index);
        }

        /// <summary>
        /// The FR-TR-023 weekly focus command — the ONLY way focus changes after
        /// <see cref="TrainingState.Create"/>.
        /// </summary>
        /// <remarks>
        /// The two failure modes are deliberately asymmetric, exactly as §2.3 draws them. An
        /// out-of-catalogue focus is a BUG in the caller and fails loud (F4 / FR-TR-021) — it is
        /// validated FIRST, so a corrupt enum is refused whether or not the player happens to exist. An
        /// unknown player is a bounded, legitimate race against the authoritative roster (a UI command
        /// arriving after a transfer or retirement) and is REFUSED as a no-op (F2), reported through the
        /// return value so the refusal is observable rather than silent.
        /// </remarks>
        /// <returns>True when the focus was applied; false when the player is not in this block (F2).</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="focus"/> is out of catalogue (F4).</exception>
        public bool SetFocus(int playerId, TrainingFocus focus)
        {
            TrainingStep.ValidateFocus(focus);

            int index = IndexOf(playerId);
            if (index < 0)
            {
                return false;
            }

            TrainingState state = _states[index];
            state.Focus = focus;
            _states[index] = state;
            return true;
        }

        /// <summary>Returns a value copy of one player's state.</summary>
        /// <exception cref="ArgumentException">No state exists for <paramref name="playerId"/> (F7).</exception>
        public TrainingState GetState(int playerId) => _states[RequireIndex(playerId)];

        /// <summary>Returns a value copy of the state at <paramref name="index"/> in ascending-id order.</summary>
        public TrainingState StateAt(int index) => _states[index];

        /// <summary>Returns the <c>PlayerId</c> at <paramref name="index"/> in ascending-id order.</summary>
        public int PlayerIdAt(int index) => _playerIds[index];

        /// <summary>Projects one player's state onto the FR-TR-022 read-only observer surface.</summary>
        /// <exception cref="ArgumentException">No state exists for <paramref name="playerId"/> (F7).</exception>
        public TrainingViewModel GetViewModel(int playerId) => ViewModelAt(RequireIndex(playerId));

        /// <summary>Projects the entry at <paramref name="index"/> onto the observer surface.</summary>
        public TrainingViewModel ViewModelAt(int index)
        {
            TrainingState state = _states[index];
            return new TrainingViewModel(_playerIds[index], state.Focus, state.Condition, state.TrainingFatigue);
        }

        /// <summary>
        /// Advances one player's stored state by one world day — #30's slot-2 step, routed to
        /// <see cref="TrainingStep.AdvanceTrainingDay"/> (the block resolves membership and owns the
        /// write; it does not re-implement the model).
        /// </summary>
        /// <exception cref="ArgumentException">
        /// No state exists for <paramref name="playerId"/> (F7 — a regen never inserted per FR-TR-025),
        /// or the call skips a world day (F7 / FR-TR-026).
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="worldDay"/> is the never-advanced sentinel, or the stored focus is out of
        /// catalogue (F4).
        /// </exception>
        public void AdvanceTrainingDay(
            int playerId,
            in PlayerAttributes attributes,
            in CoachingModifier coach,
            uint worldDay)
        {
            int index = RequireIndex(playerId);
            TrainingState state = _states[index];
            TrainingStep.AdvanceTrainingDay(ref state, in attributes, in coach, worldDay);

            // Written back only on success: every throwing path above leaves the stored state untouched
            // (the "a refused call changes nothing" posture — TrainingState is a struct, so `state` is a
            // working copy until this assignment).
            _states[index] = state;
        }

        private int RequireIndex(int playerId)
        {
            int index = IndexOf(playerId);
            if (index < 0)
            {
                throw new ArgumentException(
                    $"Player {playerId} has no training state in club {ClubId}.",
                    nameof(playerId));
            }

            return index;
        }

        // Negative result is the bitwise complement of the sorted insertion point (List.BinarySearch).
        private int IndexOf(int playerId) => _playerIds.BinarySearch(playerId);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-31 | —      | AR-1 H-2: the per-club PlayerId-keyed container (§2.2/§4.3) — the
// |         |            |        | FR-TR-025 insert/remove lifecycle seam, the FR-TR-023 SetFocus command
// |         |            |        | (F4 fail-loud / F2 refusal), the derived FR-TR-003 schedule view, the
// |         |            |        | FR-TR-022 observer projection, and the slot-2 advance routed to
// |         |            |        | TrainingStep. Deterministic ascending-PlayerId ordering rather than a
// |         |            |        | Dictionary. The T1 TrainingSaveCodec stays deferred per §4.2.        |
#endregion
