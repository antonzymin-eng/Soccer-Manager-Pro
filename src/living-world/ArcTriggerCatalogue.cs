// File:     src/living-world/ArcTriggerCatalogue.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Living World System #22 §3.4, §6.2, FR-LW-017/021/028, arc-triggers-design KD-3, Code Standards #20
// Purpose:  The Stage-0 in-code arc-trigger table: which canon signal fires which arc, at what
//           threshold, for what liveness bound. APPEND-only (the InteractionTextCorpus precedent).

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// The Stage-0 in-code arc-trigger catalogue (#22 §3.4, arc-triggers-design KD-3). One row per
    /// (canon signal → arc kind) mapping. APPEND-only (the <see cref="InteractionTextCorpus"/>
    /// precedent — inserting or reordering rows would change spawn/RNG-draw order and break replay
    /// parity, FR-LW-028). The rows are illustrative Stage-0 mappings; the real vol-2/vol-3 producer
    /// supplies the signals these thresholds read.
    ///
    /// Two channels, matching the FR-LW-017/021 evaluation order the <see cref="ArcTriggerEvaluator"/>
    /// walks: <see cref="EntityTriggers"/> (evaluated per entity, ascending entity id, then in this
    /// listed order) and <see cref="BoardTriggers"/> (evaluated once per tick — <b>authored in ArcKind
    /// ordinal order</b> so iterating the list in order IS the required ordinal order).
    /// </summary>
    public static class ArcTriggerCatalogue
    {
        // Entity-scoped triggers (per-contact). Inner-loop order below is the fixed evaluation order.
        private static readonly ArcTrigger[] s_entityTriggers =
        {
            new ArcTrigger(
                triggerId: 1,
                kind: ArcKind.WonderkidVsVeteran,
                isEntityScoped: true,
                signalKey: LivingWorldConstants.ARC_SIGNAL_KEY_EGO_CLASH,
                threshold: LivingWorldConstants.ARC_TRIGGER_EGO_CLASH_THRESHOLD,
                maxLifetimeDays: LivingWorldConstants.ARC_TRIGGER_WONDERKID_LIFETIME_DAYS),

            new ArcTrigger(
                triggerId: 2,
                kind: ArcKind.MediaVendetta,
                isEntityScoped: true,
                signalKey: LivingWorldConstants.ARC_SIGNAL_KEY_MEDIA_HOSTILITY,
                threshold: LivingWorldConstants.ARC_TRIGGER_MEDIA_HOSTILITY_THRESHOLD,
                maxLifetimeDays: LivingWorldConstants.ARC_TRIGGER_MEDIA_LIFETIME_DAYS),
        };

        // Board/squad-level triggers. AUTHORED IN ArcKind ORDINAL ORDER (DressingRoomSplit=0 then
        // BoardPatienceCollapse=2) so list order == the FR-LW-017/021 ordinal evaluation order.
        private static readonly ArcTrigger[] s_boardTriggers =
        {
            new ArcTrigger(
                triggerId: 3,
                kind: ArcKind.DressingRoomSplit,
                isEntityScoped: false,
                signalKey: LivingWorldConstants.ARC_SIGNAL_KEY_PULSE_DIVERGENCE,
                threshold: LivingWorldConstants.ARC_TRIGGER_PULSE_DIVERGENCE_THRESHOLD,
                maxLifetimeDays: LivingWorldConstants.ARC_TRIGGER_DRESSING_ROOM_LIFETIME_DAYS),

            new ArcTrigger(
                triggerId: 4,
                kind: ArcKind.BoardPatienceCollapse,
                isEntityScoped: false,
                signalKey: LivingWorldConstants.ARC_SIGNAL_KEY_BOARD_IMPATIENCE,
                threshold: LivingWorldConstants.ARC_TRIGGER_BOARD_IMPATIENCE_THRESHOLD,
                maxLifetimeDays: LivingWorldConstants.ARC_TRIGGER_BOARD_LIFETIME_DAYS),
        };

        private static readonly ReadOnlyCollection<ArcTrigger> s_entityView =
            Array.AsReadOnly(s_entityTriggers);

        private static readonly ReadOnlyCollection<ArcTrigger> s_boardView =
            Array.AsReadOnly(s_boardTriggers);

        /// <summary>Entity-scoped trigger rows, in fixed inner-loop evaluation order.</summary>
        public static IReadOnlyList<ArcTrigger> EntityTriggers => s_entityView;

        /// <summary>Board/squad-level trigger rows, in ArcKind ordinal order (FR-LW-017/021).</summary>
        public static IReadOnlyList<ArcTrigger> BoardTriggers => s_boardView;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial implementation (arc-triggers Slice 1): the Stage-0    |
// |         |            |        | in-code trigger table — two entity-scoped rows (Wonderkid,    |
// |         |            |        | Media) + two board rows (DressingRoom, BoardPatience) in      |
// |         |            |        | ArcKind ordinal order. APPEND-only (FR-LW-028).               |
#endregion
