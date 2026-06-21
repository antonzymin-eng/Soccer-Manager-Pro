// File:     src/tactical-instructions/TeamTactic.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.1, §3.2, §3.4, Appendix B, Code Standards #20
// Purpose:  One team's tactic — the manager input layer. Immutable-per-match input
//           carrier with the Balanced identity factory that reproduces today's
//           Stage0Default behaviour (FR-TI-031). Property order is the canonical
//           snapshot order (Appendix B) once FR-TI-028 activates.

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// One team's tactic (§2.2.1). Property order matches the Appendix B canonical snapshot order
    /// (digest-load-bearing once FR-TI-028 serializes it). <see cref="Balanced"/> reproduces the
    /// current no-instruction baseline exactly (FR-TI-031 / KD-10).
    /// </summary>
    public readonly struct TeamTactic
    {
        /// <summary>Master risk dial (§3.2).</summary>
        public Mentality Mentality { get; }

        /// <summary>Formation family; translated → #12 FormationFamily (§3.1).</summary>
        public TacticFormation Formation { get; }

        /// <summary>Forward-vs-retain tempo; NEW #8 branch, not a threshold (§3.3).</summary>
        public Tempo Tempo { get; }

        /// <summary>In-possession width → #12 compactness (§3.4).</summary>
        public TacticWidth Width { get; }

        /// <summary>Passing style; translated → #8 PassingStyle (§3.1).</summary>
        public TacticPassing Passing { get; }

        /// <summary>Pressing intensity; translated → #8 PressingMode (§3.1).</summary>
        public TacticPressing Pressing { get; }

        /// <summary>Line of engagement → #13 trigger distances (§3.4).</summary>
        public LineOfEngagement LineOfEngagement { get; }

        /// <summary>
        /// Defensive-line manager input dial [0,1]. INPUT ONLY — the assembly layer recomputes the
        /// authoritative <c>DefensiveLineDepth</c> each tick as <c>Clamp01(DefensiveLine +
        /// MentalityLineBias[mentality])</c>; this is the only depth surface serialized (§3.4 / Appendix B).
        /// </summary>
        public float DefensiveLine { get; }

        /// <summary>Out-of-possession defensive width → #12 OOP compactness (§3.4).</summary>
        public TacticDefWidth DefensiveWidth { get; }

        /// <summary>Plan on winning the ball; overrides only the transition dimension (§3.2 / FR-TI-020).</summary>
        public TransitionPlan TransitionWon { get; }

        /// <summary>Plan on losing the ball; overrides only the transition dimension (§3.2 / FR-TI-020).</summary>
        public TransitionPlan TransitionLost { get; }

        /// <summary>Enables #14 MarkDirective.OffsideTrapActive (§3.4).</summary>
        public bool OffsideTrap { get; }

        /// <summary>Active-press-trigger mask; translated → #13 TriggerFlags (§3.1).</summary>
        public TacticTriggerMask TriggerPressMask { get; }

        /// <summary>Lateral attacking focus; NEW branch (#8/#15) (§3.3).</summary>
        public FocusPlay FocusPlay { get; }

        /// <summary>Default goalkeeper distribution policy → #11 DistributeIntent (§3.4).</summary>
        public GkDistributionPolicy GkDistribution { get; }

        /// <summary>Time-wasting dial [0..4] (0 = never … 4 = always). §2.2.1.</summary>
        public byte TimeWasting { get; }

        /// <summary>Constructs a <see cref="TeamTactic"/> in canonical field order (Appendix B).</summary>
        public TeamTactic(
            Mentality mentality,
            TacticFormation formation,
            Tempo tempo,
            TacticWidth width,
            TacticPassing passing,
            TacticPressing pressing,
            LineOfEngagement lineOfEngagement,
            float defensiveLine,
            TacticDefWidth defensiveWidth,
            TransitionPlan transitionWon,
            TransitionPlan transitionLost,
            bool offsideTrap,
            TacticTriggerMask triggerPressMask,
            FocusPlay focusPlay,
            GkDistributionPolicy gkDistribution,
            byte timeWasting)
        {
            Mentality = mentality;
            Formation = formation;
            Tempo = tempo;
            Width = width;
            Passing = passing;
            Pressing = pressing;
            LineOfEngagement = lineOfEngagement;
            DefensiveLine = defensiveLine;
            DefensiveWidth = defensiveWidth;
            TransitionWon = transitionWon;
            TransitionLost = transitionLost;
            OffsideTrap = offsideTrap;
            TriggerPressMask = triggerPressMask;
            FocusPlay = focusPlay;
            GkDistribution = gkDistribution;
            TimeWasting = timeWasting;
        }

        /// <summary>
        /// The balanced identity tactic (§2.2.1): Mentality.Balanced, F442, Tempo.Standard, Width.Standard,
        /// Passing.Mixed, Pressing.Medium, LineOfEngagement.Standard, DefensiveLine 0.5,
        /// DefensiveWidth.Standard, TransitionWon HoldShape / TransitionLost Regroup, OffsideTrap false,
        /// TriggerPressMask None, FocusPlay.Mixed, GkDistribution.SlowDown, TimeWasting 0. Reproduces
        /// today's <c>Stage0Default</c> behaviour exactly (FR-TI-031).
        /// </summary>
        public static TeamTactic Balanced => new TeamTactic(
            Mentality.Balanced,
            TacticFormation.F442,
            Tempo.Standard,
            TacticWidth.Standard,
            TacticPassing.Mixed,
            TacticPressing.Medium,
            LineOfEngagement.Standard,
            0.5f,
            TacticDefWidth.Standard,
            TransitionPlan.HoldShape,
            TransitionPlan.Regroup,
            false,
            TacticTriggerMask.None,
            FocusPlay.Mixed,
            GkDistributionPolicy.SlowDown,
            0);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
#endregion
