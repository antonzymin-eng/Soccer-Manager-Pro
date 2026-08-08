// File:     src/season-save/AppearanceState.cs
// Created:  2026-08-07
// Modified: 2026-08-07
// Author:   —
// Spec:     Season & Competition Loop #30 §3.4 / Appendix B (the appearance block, ERR-041-010(b));
//           Injuries & Medical #41 FR-MD-010 (the AppearanceDays window unit); Code Standards #20
// Purpose:  The #30-owned per-player recent-appearance record — the persisted source of the FR-MD-010
//           MatchLoad.AppearanceDays occurrence-risk input. A rolling day-window as a bitmask, shifted
//           lazily at read time so no daily mutation step exists.

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// One player's recent match appearances, as a day-indexed bitmask (#30 Appendix B / ERR-041-010(b)):
    /// bit <c>k</c> of <see cref="RecentBits"/> is set iff the player was fielded on world day
    /// <c>BitsAsOfWorldDay − k</c>.
    /// <para>
    /// <b>This is #30-side state.</b> #41 §3.5 sources <c>MatchLoad</c> from "#30's fixture result", and
    /// neither #29's nor #41's save block may describe the other's domain — so the appearance record
    /// lives here, in its own sub-blob (<see cref="AppearanceSaveCodec"/>), and reaches #41 only as the
    /// caller-supplied <c>MatchLoad</c> value FR-MD-010 mandates. Recomputing it from the fixture list
    /// instead is NOT equivalent: the availability filter changes who actually played.
    /// </para>
    /// <para>
    /// <b>Lazily shifted, deliberately.</b> The obvious encoding — shift one bit per world day — would
    /// add a daily mutation to #30's pinned KD-2 tick order, with its own slot, its own idempotency
    /// cursor, and its own "which side of the round" question of exactly the ERR-030-026 class. Storing
    /// the bits together with the day they are anchored to (<see cref="BitsAsOfWorldDay"/>) and shifting
    /// at read time (<see cref="AppearanceWindow"/>) deletes all three: recording twice on one day is
    /// idempotent by construction (bit-OR), a multi-day gap costs nothing, and no day step exists to
    /// order against the round.
    /// </para>
    /// <para>
    /// <b><c>default(AppearanceState)</c> IS a valid fresh state</b>, unlike its #29/#41 siblings: zero
    /// bits anchored at day 0 reads as "no recent appearances" on every later day, and the first
    /// <see cref="AppearanceWindow.Record"/> re-anchors unconditionally — so there is no day-0 trap and
    /// no sentinel. <see cref="AppearanceWindow"/> is the only production writer.
    /// </para>
    /// <para>
    /// <b>"Fielded" currently means "in the STARTING eleven"</b> — Stage 0 has no substitutions, so
    /// the recorded ids come from the one <c>LineupSelector</c> walk
    /// (<c>SquadRating.StartingElevenPlayerIds</c>) and the two are the same set. When substitutions
    /// land (deep tier / #38 Wave-7's manager XI), FR-MD-010's load reading wants everyone who took
    /// the pitch, so the RECORDING call sites widen — this type and its window arithmetic already
    /// carry a bare "was fielded" bit per day and need no change. Noted so the widening lands where
    /// the ids are derived, not as a second record.
    /// </para>
    /// </summary>
    public struct AppearanceState
    {
        /// <summary>The appearance bitmask: bit <c>k</c> = fielded on day <see cref="BitsAsOfWorldDay"/> − <c>k</c>. Bits older than the FR-MD-010 window are dead weight and fall off the top as the anchor advances.</summary>
        public uint RecentBits;

        /// <summary>The world day bit 0 of <see cref="RecentBits"/> refers to — the last day an appearance was recorded (or 0 for a fresh state, which carries no set bits).</summary>
        public uint BitsAsOfWorldDay;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-07 | —      | Initial implementation (balance pass D2, ERR-041-010(b)): the     |
// |         |            |        | per-player appearance record, lazily shifted so no KD-2 day slot  |
// |         |            |        | is added.                                                         |
// | 1.1     | 2026-08-07 | —      | Balance-pass AR pass 2 (L, doc only): "fielded" pinned to "in the |
// |         |            |        | starting eleven" for Stage 0, with the substitution widening      |
// |         |            |        | assigned to the recording call sites — the bitmask itself needs   |
// |         |            |        | no change when it lands.                                          |
#endregion
