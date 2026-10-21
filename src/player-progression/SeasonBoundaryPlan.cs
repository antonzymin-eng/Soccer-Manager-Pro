// File:     src/player-progression/SeasonBoundaryPlan.cs
// Created:  2026-10-19
// Modified: 2026-10-20 (AR round 4 L11: the immutability claim scoped to the PUBLIC surface; doc-only — v1.2)
//           2026-10-20 (AR round 1 L4: PlannedSquadFor doc — post-commit snapshot semantics; doc-only — v1.1)
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §3.4 (RunSeasonBoundary as amended by ERR-028-025),
//           FR-PG-012/015/019/021/024; Season & Competition Loop #30 §3.5 step (d); Code Standards #20
// Purpose:  The staged result of #28's season boundary — retirees removed, 1:1 regens inserted, the id
//           cursor advanced — computed by ProgressionEngine.PrepareSeasonBoundary and installed only by
//           CommitSeasonBoundary, so #30's roll can stage it before its one fallible write and install
//           it after. Plays the role §3.4 names RetirementResult / RegenResult (FR-PG-012/015).

using System;
using System.Collections.ObjectModel;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.PlayerProgression
{
    /// <summary>
    /// One season boundary's roster mutation, staged (#28 §3.4, ERR-028-025). Immutable through its PUBLIC
    /// surface, which is read-only copies. The replacement arrays and the constructor are <c>internal</c>
    /// and only <see cref="ProgressionEngine.CommitSeasonBoundary"/> installs them — but the assembly's
    /// <c>InternalsVisibleTo</c> grants are assembly-wide (AR round 4 L11), so TacticalDirector.SeasonSave and
    /// the two test assemblies can construct a plan and read those arrays directly; "immutable" binds
    /// every other caller, not those three.
    /// <para>
    /// <b>Why staged.</b> #30's <c>RollToNextSeason</c> has exactly one write that can fail
    /// (<c>BeginNextSeason</c>). A boundary that mutated the store in place before it would leave a refused
    /// roll with retirees already gone — a half-rolled career that saves cleanly. The plan is what the
    /// retired §3.4 <c>boundaryAlreadyApplied</c> marker was for, achieved structurally instead: nothing is
    /// applied until the roll can no longer fail, so there is never a partially applied boundary to resume.
    /// </para>
    /// </summary>
    public sealed class SeasonBoundaryPlan
    {
        private readonly int[] _clubIds;
        private readonly int[] _retiredPlayerIds;
        private readonly int[] _regenPlayerIds;
        private readonly int[] _regenClubIds;

        internal SeasonBoundaryPlan(
            ProgressionEngine owner,
            int generation,
            int[] clubIds,
            PlayerRecord[][] records,
            PlayerLifecycle[][] lifecycles,
            int nextPlayerId,
            int[] retiredPlayerIds,
            int[] regenPlayerIds,
            int[] regenClubIds)
        {
            Owner = owner;
            Generation = generation;
            _clubIds = clubIds;
            Records = records;
            Lifecycles = lifecycles;
            NextPlayerId = nextPlayerId;
            _retiredPlayerIds = retiredPlayerIds;
            _regenPlayerIds = regenPlayerIds;
            _regenClubIds = regenClubIds;
        }

        /// <summary>The store this plan was prepared from; the only one that may install it.</summary>
        internal ProgressionEngine Owner { get; }

        /// <summary>The store's mutation generation at preparation — a later mutation makes the plan stale.</summary>
        internal int Generation { get; }

        /// <summary>Per club (index-aligned with the store's ascending club ids), the replacement records.</summary>
        internal PlayerRecord[][] Records { get; }

        /// <summary>Per club, the replacement lifecycles.</summary>
        internal PlayerLifecycle[][] Lifecycles { get; }

        /// <summary>The id cursor after every regen this plan allocated (FR-PG-011).</summary>
        public int NextPlayerId { get; }

        /// <summary>
        /// Every retiree this boundary removes, in processing order — clubs ascending, then each club's
        /// retirees by ascending PlayerId (the §3.4 order). Index <c>i</c> is replaced by
        /// <see cref="RegenPlayerIds"/>[i].
        /// </summary>
        public ReadOnlyCollection<int> RetiredPlayerIds => Array.AsReadOnly(_retiredPlayerIds);

        /// <summary>The fresh regen ids, parallel to <see cref="RetiredPlayerIds"/>.</summary>
        public ReadOnlyCollection<int> RegenPlayerIds => Array.AsReadOnly(_regenPlayerIds);

        /// <summary>The club each regen joins (its retiree's club), parallel to <see cref="RegenPlayerIds"/>.</summary>
        public ReadOnlyCollection<int> RegenClubIds => Array.AsReadOnly(_regenClubIds);

        /// <summary>
        /// The club's roster as it WILL stand once the plan is committed — what #30's roster-membership
        /// reconciliation for #29/#41 must stage against, since it cannot read the store until the plan is
        /// installed. A fresh snapshot each call.
        /// <para>
        /// <b>After commit this is a historical snapshot.</b> <see cref="ProgressionEngine.CommitSeasonBoundary"/>
        /// installs <i>copies</i> of the plan's arrays, so the plan keeps describing the boundary it staged
        /// while the store diverges as days advance (AR round 1 P-L4); a retained plan is no longer a view of
        /// the live roster.
        /// </para>
        /// </summary>
        /// <returns>The planned squad, or <c>null</c> for a club the store does not carry (the
        /// <c>ISquadProvider.ResolveByClubId</c> contract).</returns>
        public Squad PlannedSquadFor(int clubId)
        {
            int c = Array.BinarySearch(_clubIds, clubId);
            return c < 0 ? null : new Squad(clubId, Records[c]);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-19 | —      | Initial implementation (ERR-028-025): the staged season-boundary |
// |         |            |        | roster mutation — #28 §3.4's RetirementResult/RegenResult as one |
// |         |            |        | plan installed only after #30's roll can no longer fail.         |
// | 1.1     | 2026-10-20 | —      | AR round 1 L4 (ERR-028-025), doc-only: PlannedSquadFor states that after CommitSeasonBoundary (which installs copies) the plan is a historical snapshot, not a live view. |
// | 1.2     | 2026-10-20 | —      | AR round 4 L11, doc-only: the "Immutable to every caller" claim is scoped to the public surface — the assembly-wide InternalsVisibleTo grants expose the internal constructor and arrays to SeasonSave and the test assemblies. |
#endregion
