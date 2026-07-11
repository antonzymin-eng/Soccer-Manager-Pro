// File:     src/tactical-instructions/TeamTactic.cs
// Created:  2026-06-21
// Modified: 2026-07-10
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
    /// <remarks>
    /// HAZARD — <c>default(TeamTactic)</c> is NOT the identity (e.g. Mentality VeryDefensive, Passing
    /// Short, DefensiveLine 0.0). Use <see cref="Balanced"/>; treat default-valued instances as malformed.
    /// </remarks>
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

        /// <summary>
        /// Defensive marking orientation → #14 MAN_MARK candidate radius scalar (new §3.4 axis,
        /// cheap-item addition). APPENDED after <see cref="TimeWasting"/> so the pre-existing
        /// Appendix B field order is undisturbed; <see cref="TacticalInstructions.MarkingOrientation.Balanced"/>
        /// is identity (FR-TI-031).
        /// </summary>
        public MarkingOrientation MarkingOrientation { get; }

        /// <summary>
        /// Dismarking-intensity dial → #12 dismark offset stage + #8 FM-DM-03 (#23; back-prop
        /// ERR-021-005). APPENDED after <see cref="MarkingOrientation"/> per the pinned #23 → #24 →
        /// #25 approval order (Appendix B); <see cref="TacticalInstructions.DismarkIntensity.Off"/>
        /// (zero value) is identity (FR-DM-012). Serialization enters <c>WriteTeamTactic</c> with a
        /// schema bump only at #23's wiring stage.
        /// </summary>
        public DismarkIntensity DismarkIntensity { get; }

        /// <summary>
        /// Build-up structure dial → #12 build-up overlay stage (#24; back-prop ERR-021-006).
        /// APPENDED after <see cref="DismarkIntensity"/> (Appendix B);
        /// <see cref="TacticalInstructions.BuildUpStructure.None"/> (zero value) is identity
        /// (FR-BU-005). Serialization enters <c>WriteTeamTactic</c> only at #24's wiring stage.
        /// </summary>
        public BuildUpStructure BuildUpStructure { get; }

        /// <summary>
        /// Positional-rotation freedom dial → #12 <c>RotationController</c> (#25; back-prop
        /// ERR-021-007). APPENDED after <see cref="BuildUpStructure"/> (Appendix B);
        /// <see cref="TacticalInstructions.RotationFreedom.Off"/> (zero value) is identity
        /// (FR-RO-011). Serialization enters <c>WriteTeamTactic</c> only at #25's wiring stage.
        /// </summary>
        public RotationFreedom RotationFreedom { get; }

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
            byte timeWasting,
            MarkingOrientation markingOrientation = MarkingOrientation.Balanced,
            DismarkIntensity dismarkIntensity = DismarkIntensity.Off,
            BuildUpStructure buildUpStructure = BuildUpStructure.None,
            RotationFreedom rotationFreedom = RotationFreedom.Off)
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
            MarkingOrientation = markingOrientation;
            DismarkIntensity = dismarkIntensity;
            BuildUpStructure = buildUpStructure;
            RotationFreedom = rotationFreedom;
        }

        /// <summary>
        /// The balanced identity tactic (§2.2.1): Mentality.Balanced, F442, Tempo.Standard, Width.Standard,
        /// Passing.Mixed, Pressing.Medium, LineOfEngagement.Standard, DefensiveLine 0.5,
        /// DefensiveWidth.Standard, TransitionWon HoldShape / TransitionLost Regroup, OffsideTrap false,
        /// TriggerPressMask None, FocusPlay.Mixed, GkDistribution.SlowDown, TimeWasting 0,
        /// MarkingOrientation.Balanced, DismarkIntensity.Off, BuildUpStructure.None,
        /// RotationFreedom.Off (the three July-10 back-prop fields are zero-value identities, taken
        /// via the ctor defaults). Reproduces today's <c>Stage0Default</c> behaviour exactly (FR-TI-031).
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
            0,
            MarkingOrientation.Balanced);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).                               |
// | 1.1     | 2026-06-21 | —      | AR-1 L-1: <remarks> default(TeamTactic) is not the identity.   |
// | 1.2     | 2026-07-07 | —      | Cheap-item addition: + MarkingOrientation field (§3.4, #14),   |
// |         |            |        |   appended after TimeWasting via a defaulted ctor parameter so |
// |         |            |        |   every existing call site (WithLine/WithWidth factories etc.)|
// |         |            |        |   stays source-compatible without naming the new arg.         |
// | 1.3     | 2026-07-10 | —      | Back-props ERR-021-005/006/007 (#23/#24/#25 APPROVED):        |
// |         |            |        |   + DismarkIntensity/BuildUpStructure/RotationFreedom fields   |
// |         |            |        |   in pinned approval order after MarkingOrientation, via       |
// |         |            |        |   defaulted ctor params (all three zero-value identities).     |
// |         |            |        |   NOT serialized yet — WriteTeamTactic + schema bump land at   |
// |         |            |        |   each owning spec's wiring stage (#21 Appendix B v0.5).       |
#endregion
