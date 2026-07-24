// File:     src/tactical-instructions/TacticPresetLibrary.cs
// Created:  2026-07-10
// Modified: 2026-07-10
// Author:   —
// Spec:     Tactical Presets #26 §2.2.2 (FR-TP-002/013), Appendix A.1 (pinned July 10, 2026),
//           Code Standards #20
// Purpose:  The static in-code Stage-0+1 preset catalogue. Ordinal = array index = serialized
//           identity + ladder position ([FIXED] ordering contract, defensive → attacking).

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// The tactic-preset catalogue (#26 §2.2.2, FR-TP-002): static, ordered, APPEND-only. The
    /// ordinal is both the serialized identity and the §3.4/§3.5 <c>StepToward</c> ladder position
    /// (defensive → attacking — a [FIXED] ordering contract, #26 Appendix A.1); removing or
    /// reordering a preset would dangle serialized ordinals (FR-TP-013). Contents pinned per
    /// Appendix A.1 v0.3: every composition value is a verified #21 enum member / pinned dial and
    /// every non-listed dial stays at its <see cref="TeamTactic.Balanced"/> identity value (KD-7 —
    /// no new tunable magnitudes). Stage-0+1 presets carry no per-agent tactics (Players = null).
    /// There is no disk format at this stage (KD-6 parser-swap deferral).
    /// </summary>
    public static class TacticPresetLibrary
    {
        /// <summary>Ordinal 0 — the catalogue's defensive end of the ladder.</summary>
        public const byte ParkTheBusOrdinal = 0;

        /// <summary>Ordinal 1.</summary>
        public const byte CounterAttackOrdinal = 1;

        /// <summary>Ordinal 2 — the catalogue midpoint and kickoff default for a no-affinity profile (A.1).</summary>
        public const byte BalancedOrdinal = 2;

        /// <summary>Ordinal 3.</summary>
        public const byte PossessionOrdinal = 3;

        /// <summary>Ordinal 4 — the attacking end of the ladder.</summary>
        public const byte GegenpressOrdinal = 4;

        // Each preset carries its own A.3 kickoff-scoring row (BaseFit/AggrAffinity/CautAffinity),
        // seeded here from the ordinal-indexed TacticalPresetsConstants tables (WS-1 — completes the
        // injectable catalogue so KickoffScore reads the row off the resolved preset, never a fixed
        // 5-element side-table). The values are unchanged, so the default kickoff selection is
        // byte-identical. TacticalPresetsConstants is the same assembly; the cross-type static read
        // triggers its cctor first (no ordering hazard, no cycle).
        private static readonly TacticPreset[] s_presets = new TacticPreset[]
        {
            // 0 — ParkTheBus (A.1): VeryDefensive, low press, low engagement, deep line, heavy
            // time-wasting, HoldShape on regain. Non-listed dials = Balanced identity.
            new TacticPreset("ParkTheBus", Compose(
                mentality: Mentality.VeryDefensive,
                pressing: TacticPressing.Low,
                lineOfEngagement: LineOfEngagement.Low,
                defensiveLine: 0.30f,
                timeWasting: 3,
                transitionWon: TransitionPlan.HoldShape),
                TacticalPresetsConstants.BaseFit[ParkTheBusOrdinal],
                TacticalPresetsConstants.AggrAffinity[ParkTheBusOrdinal],
                TacticalPresetsConstants.CautAffinity[ParkTheBusOrdinal]),

            // 1 — CounterAttack (A.1): Defensive, counter on regain, fast+direct, deep-ish line.
            new TacticPreset("CounterAttack", Compose(
                mentality: Mentality.Defensive,
                transitionWon: TransitionPlan.CounterAttack,
                tempo: Tempo.Fast,
                passing: TacticPassing.Direct,
                defensiveLine: 0.40f),
                TacticalPresetsConstants.BaseFit[CounterAttackOrdinal],
                TacticalPresetsConstants.AggrAffinity[CounterAttackOrdinal],
                TacticalPresetsConstants.CautAffinity[CounterAttackOrdinal]),

            // 2 — Balanced (A.1): TeamTactic.Balanced verbatim (the FR-TI-031 identity).
            new TacticPreset("Balanced", TeamTactic.Balanced,
                TacticalPresetsConstants.BaseFit[BalancedOrdinal],
                TacticalPresetsConstants.AggrAffinity[BalancedOrdinal],
                TacticalPresetsConstants.CautAffinity[BalancedOrdinal]),

            // 3 — Possession (A.1): Positive, short+slow, wide, slightly advanced line.
            new TacticPreset("Possession", Compose(
                mentality: Mentality.Positive,
                passing: TacticPassing.Short,
                tempo: Tempo.Slow,
                width: TacticWidth.Wide,
                defensiveLine: 0.55f),
                TacticalPresetsConstants.BaseFit[PossessionOrdinal],
                TacticalPresetsConstants.AggrAffinity[PossessionOrdinal],
                TacticalPresetsConstants.CautAffinity[PossessionOrdinal]),

            // 4 — Gegenpress (A.1): Attacking, high press, high engagement, counter-press on loss,
            // advanced line.
            new TacticPreset("Gegenpress", Compose(
                mentality: Mentality.Attacking,
                pressing: TacticPressing.High,
                lineOfEngagement: LineOfEngagement.High,
                transitionLost: TransitionPlan.CounterPress,
                defensiveLine: 0.65f),
                TacticalPresetsConstants.BaseFit[GegenpressOrdinal],
                TacticalPresetsConstants.AggrAffinity[GegenpressOrdinal],
                TacticalPresetsConstants.CautAffinity[GegenpressOrdinal]),
        };

        private static readonly ReadOnlyCollection<TacticPreset> s_presetsRo =
            new ReadOnlyCollection<TacticPreset>(s_presets);

        /// <summary>
        /// The catalogue in pinned ladder order. Read-only wrapper — callers cannot cast back to
        /// the mutable array (the #18/#19 AR-1 L-3 pattern).
        /// </summary>
        public static IReadOnlyList<TacticPreset> Presets => s_presetsRo;

        /// <summary>Catalogue length (the F2 restore-seam bound for serialized ordinals).</summary>
        public static int Count => s_presets.Length;

        /// <summary>
        /// Builds a TeamTactic from the Balanced identity with only the named dials overridden —
        /// the KD-7 "named point in the existing #21 parameter space" composition rule. Every
        /// optional-parameter default below IS the corresponding <see cref="TeamTactic.Balanced"/>
        /// identity value, so an unnamed dial stays at identity by construction. Locked per preset
        /// by the T0 suite's composition tests, which assert every dial a preset's A.1 row does
        /// NOT list equals its Balanced value (T0 AR-1 L-1 — the earlier "identity test" claim
        /// covered only globally-untouched dials).
        /// </summary>
        private static TeamTactic Compose(
            Mentality mentality = Mentality.Balanced,
            TacticFormation formation = TacticFormation.F442,
            Tempo tempo = Tempo.Standard,
            TacticWidth width = TacticWidth.Standard,
            TacticPassing passing = TacticPassing.Mixed,
            TacticPressing pressing = TacticPressing.Medium,
            LineOfEngagement lineOfEngagement = LineOfEngagement.Standard,
            float defensiveLine = 0.5f,
            TacticDefWidth defensiveWidth = TacticDefWidth.Standard,
            TransitionPlan transitionWon = TransitionPlan.HoldShape,
            TransitionPlan transitionLost = TransitionPlan.Regroup,
            bool offsideTrap = false,
            TacticTriggerMask triggerPressMask = TacticTriggerMask.None,
            FocusPlay focusPlay = FocusPlay.Mixed,
            GkDistributionPolicy gkDistribution = GkDistributionPolicy.SlowDown,
            byte timeWasting = 0,
            MarkingOrientation markingOrientation = MarkingOrientation.Balanced,
            DismarkIntensity dismarkIntensity = DismarkIntensity.Off,
            BuildUpStructure buildUpStructure = BuildUpStructure.None,
            RotationFreedom rotationFreedom = RotationFreedom.Off)
        {
            return new TeamTactic(
                mentality, formation, tempo, width, passing, pressing, lineOfEngagement,
                defensiveLine, defensiveWidth, transitionWon, transitionLost, offsideTrap,
                triggerPressMask, focusPlay, gkDistribution, timeWasting, markingOrientation,
                dismarkIntensity, buildUpStructure, rotationFreedom);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                   |
// | 1.0     | 2026-07-10 | —      | Initial implementation (#26 T0): the five Appendix A.1 presets in pinned ladder order. |
// | 1.1     | 2026-07-10 | —      | T0 AR-1 L-1: Compose doc no longer overclaims the identity-test coverage — the |
// |         |            |        |   per-preset inherited-dial == Balanced locks now live in the composition tests. |
// | 1.2     | 2026-07-24 | —      | WS-1 (#26 KD-6): each preset ctor now seeds its A.3 kickoff-scoring row        |
// |         |            |        |   (BaseFit/AggrAffinity/CautAffinity) from TacticalPresetsConstants by ordinal — |
// |         |            |        |   the values are unchanged, so the default kickoff selection is byte-identical.  |
#endregion
