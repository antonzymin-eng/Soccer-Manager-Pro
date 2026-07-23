// File:     src/decision-tree/TacticalContext.cs
// Created:  2026-05-29
// Modified: 2026-07-23
// Author:   —
// Spec:     Decision Tree #8 §2.2.6, new §3.2/§7.7, Tactical Instructions #21 §3.2, Code Standards #20
// Purpose:  Team tactical instructions delivered to each agent's Decision Tree.
//           Stage 0: hardcoded defaults via Stage0Default(formationSlot).
//           Stage 1+: Formation System (Positioning AI #12) populates live values.

using UnityEngine;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Team tactical instructions for one agent at one heartbeat.
    /// Stage 0: both teams use Stage0Default (MEDIUM pressing, MIXED passing, 0.5 depth).
    /// Stage 1+: Positioning AI #12 populates live per-team values.
    /// Decision Tree #8 §2.2.6.
    /// </summary>
    public struct TacticalContext
    {
        // ── Team Instructions ─────────────────────────────────────────────────

        /// <summary>Team pressing intensity. Governs PRESS/INTERCEPT/HOLD multipliers (§3.4.3).</summary>
        public PressingMode Pressing;

        /// <summary>Team passing style. Governs long/short PASS and HOLD multipliers (§3.4.4).</summary>
        public PassingStyle Passing;

        /// <summary>
        /// Defensive line depth [0.0 = deepest, 1.0 = highest line]. Default 0.5.
        /// Adjusts formation slot Y positions (§3.4.5).
        /// </summary>
        public float DefensiveLineDepth;

        /// <summary>
        /// Team mentality (#21 §3.2 master risk dial). <see cref="Stage0Default"/> seeds this to
        /// <see cref="Mentality.Balanced"/> (risk ×1.00, line bias 0.00) so the seam is a no-op
        /// until the match-engine Phase-D writer routes a live tactic in (FR-TI-031). NOTE: the
        /// zero-value struct default is <see cref="Mentality.VeryDefensive"/> (×0.80), NOT Balanced —
        /// like <see cref="Pressing"/> and <see cref="DefensiveLineDepth"/>, this field is only valid
        /// when the struct is built via the factory; never consume a <c>default(TacticalContext)</c>.
        /// Its utility multiplier is applied per scored option in UtilityScorer via
        /// <see cref="TacticTranslation.MentalityRiskMultiplier"/>.
        /// </summary>
        public Mentality Mentality;

        /// <summary>
        /// Team tempo (#21 §3.3 forward-vs-retain dial). <see cref="Stage0Default"/> seeds
        /// <see cref="Tempo.Standard"/> (the §3.3 identity row ⇒ all action factors ×1.0, FR-TI-031), so
        /// the seam is a no-op until the match-engine Phase-D writer routes a live tactic in. NOTE: the
        /// zero-value struct default is <see cref="Tempo.VerySlow"/>, NOT Standard — like the other
        /// tactic fields, only valid when built via the factory. Consumed per scored option in
        /// UtilityScorer via <see cref="TacticTranslation.PlayerTacticActionMultiplier"/>.
        /// </summary>
        public Tempo Tempo;

        /// <summary>
        /// This agent's per-agent tactic (#21 §3.3 — behavioural role + duty + individual instructions).
        /// <see cref="Stage0Default"/> seeds the identity <see cref="PlayerTactic.Default(PlayerRole)"/>
        /// with <see cref="PlayerRole.Default"/> (every §3.3 product factor ×1.0, FR-TI-031). NOTE:
        /// <c>default(PlayerTactic)</c> is NOT the identity (its embedded instructions man-mark agent 0);
        /// consume only when built via the factory. Drives the per-option §3.3 utility product in
        /// UtilityScorer via <see cref="TacticTranslation.PlayerTacticActionMultiplier"/>. The per-agent
        /// config surface now exists (<c>MatchEngine.SetPlayerTactic</c> / <c>PlayerTacticConfig</c>); an
        /// unconfigured agent stays the identity, so a default match is behaviour-neutral (FR-TI-031).
        /// </summary>
        public PlayerTactic PlayerTactic;

        /// <summary>
        /// #23 routing field (FR-DM-015): the team's DismarkIntensity dial, populated solely by the
        /// match-engine Phase-D writer. Drives the §3.4 marked-pass-target penalty in UtilityScorer
        /// (FM-DM-03). Zero value = <see cref="TacticalInstructions.DismarkIntensity.Off"/> = the
        /// exact identity (#23 §2.2.3 — safe unseeded, unlike <see cref="Mentality"/>);
        /// <see cref="Stage0Default"/> seeds Off explicitly for symmetry with the other dials.
        /// </summary>
        public DismarkIntensity DismarkIntensity;

        /// <summary>
        /// Cheap-item addition (new §3.2/§7.7): whether Positioning AI #12's rest-defense coverage
        /// check judged the team's shape sufficient this tick (<c>RestDefenseEvaluator</c>).
        /// <see cref="Stage0Default"/> seeds <c>true</c> (no dampening — identity, FR-TI-031-style).
        /// NOTE: the zero-value <c>bool</c> default is <c>false</c>, NOT identity — like
        /// <see cref="Mentality"/> and <see cref="Pressing"/>, this field is only valid when the
        /// struct is built via the factory; never consume a <c>default(TacticalContext)</c>.
        /// <c>false</c> applies <see cref="TacticalWeights.RestDefenseRiskMult"/> to PASS/SHOOT/DRIBBLE
        /// in UtilityScorer.
        /// </summary>
        public bool RestDefenseSufficient;

        /// <summary>
        /// #11/#10 GK/Heading integration (ERR-008-013): true only for the goalkeeper of the team a
        /// loose on-target ball threatens this tick — the DT-emitted-SAVE gate. Set solely by
        /// <c>MatchEngine.RunMechanicsAI</c> under the opt-in <c>EnableGkHeading()</c> flag (from
        /// <c>GkHeadingIntentSource.SaveArmed</c>); when set, <c>OptionGenerator</c> emits SAVE as the
        /// SOLE off-ball option so the keeper reliably commits. Zero value <c>false</c> = identity
        /// (no save available) — safe unseeded like <see cref="DismarkIntensity"/>.Off;
        /// <see cref="Stage0Default"/> seeds <c>false</c> explicitly for symmetry. Flag-off / non-keeper
        /// ⇒ <c>false</c>, so the off-ball branch is byte-identical to pre-integration.
        /// </summary>
        public bool SaveAvailable;

        // ── Formation Slot ────────────────────────────────────────────────────

        /// <summary>
        /// Pre-computed formation slot for this agent. Set by Stage0Default or by
        /// Positioning AI #12 at Stage 1. Used by MOVE_TO_POSITION option generation (§3.1.7).
        /// </summary>
        private Vector2 _formationSlot;

        // ── Stage 1+ Stub Fields ──────────────────────────────────────────────
        // These fields are null at Stage 0. Defined here per spec amendments:
        //   ERR-014-001: TacticalContext.MarkDirective? (null at Stage 0)
        //   ERR-015-002: TacticalContext.AttackIntent[]? (null at Stage 0)
        // Concrete types will be provided by Defensive AI #14 and Attacking AI #15.

        /// <summary>Stage 1+: mark directive from Defensive AI #14. Always null at Stage 0. ERR-014-001.</summary>
        public bool HasMarkDirective;       // stub; Stage 1+ replaces with MarkDirective?

        /// <summary>Stage 1+: attack intent array from Attacking AI #15. Always false at Stage 0. ERR-015-002.</summary>
        public bool HasAttackIntent;        // stub; Stage 1+ replaces with AttackIntent[]?

        // ── Factory ───────────────────────────────────────────────────────────

        /// <summary>
        /// Creates the Stage 0 default for a single agent.
        /// formationSlot: this agent's positional anchor on the pitch (XY pitch space).
        /// Both teams use identical defaults at Stage 0 (§2.2.6).
        /// </summary>
        public static TacticalContext Stage0Default(Vector2 formationSlot)
        {
            return new TacticalContext
            {
                Pressing           = PressingMode.MEDIUM,
                Passing            = PassingStyle.MIXED,
                DefensiveLineDepth = 0.5f,
                Mentality          = Mentality.Balanced,
                Tempo              = Tempo.Standard,
                PlayerTactic       = PlayerTactic.Default(PlayerRole.Default),
                DismarkIntensity   = DismarkIntensity.Off,
                RestDefenseSufficient = true,
                SaveAvailable      = false,
                _formationSlot     = formationSlot,
                HasMarkDirective   = false,
                HasAttackIntent    = false
            };
        }

        // ── Queries ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the formation slot target for the given agent.
        /// Stage 0: returns the per-agent slot set at construction.
        /// Stage 1+: Formation System dynamically adjusts slots.
        /// §3.1.7.2.
        /// </summary>
        public Vector2 GetFormationSlot(int agentId) => _formationSlot;

        /// <summary>
        /// Formation slot adjusted for DefensiveLineDepth (§3.4.5: shifts the slot
        /// "upfield or downfield" — pitch depth). Pitch depth is the X axis
        /// (goal-to-goal, 0–105 m; Ball Physics #1 §1.2); Y is touchline-to-touchline.
        /// A higher line advances toward the OPPONENT goal, so the shift sign is
        /// team-dependent: home (+X), away (−X).
        /// AR-2 M-2: previous form shifted _formationSlot.y (sideways toward a
        /// touchline) with no team sign — latent at Stage 0 (depth fixed at 0.5 ⇒
        /// zero offset) but wrong the moment Stage 1 tactical instructions go live.
        /// The §3.4.5 spec pseudocode carried the same Y-axis error and is patched in
        /// the same commit (ERR-008-003).
        /// </summary>
        public Vector2 GetAdjustedFormationSlot(int agentId, int teamId)
        {
            float xAdjust = (DefensiveLineDepth - 0.5f) * TacticalWeights.DefensiveLineDepthRange;
            if (teamId == 1) xAdjust = -xAdjust;   // away attacks toward x = 0
            return new Vector2(_formationSlot.x + xAdjust, _formationSlot.y);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-06-11 | —      | Audit AR-2 M-2: GetAdjustedFormationSlot shifts X (pitch depth, team-signed) |
// |         |            |        |   instead of Y (touchline axis); signature gains teamId. Latent at Stage 0   |
// |         |            |        |   (depth pinned 0.5). Spec §3.4.5 pseudocode patched same commit.             |
// | 1.2     | 2026-06-28 | —      | #21 T2 seam: Mentality routing field added (default Balanced = identity,     |
// |         |            |        |   FR-TI-031); Stage0Default seeds it. Risk multiplier applied in             |
// |         |            |        |   UtilityScorer via TacticTranslation. Behaviour-neutral until Phase-D writes.|
// | 1.3     | 2026-06-29 | —      | #21 §3.3: Tempo + per-agent PlayerTactic routing fields added (Stage0Default |
// |         |            |        |   seeds Standard / identity PlayerTactic ⇒ ×1.0, FR-TI-031). Drive the per-  |
// |         |            |        |   option §3.3 utility product in UtilityScorer. Behaviour-neutral.           |
// | 1.4     | 2026-06-30 | —      | #21 §3.3: PlayerTactic field doc updated — the per-agent config surface now  |
// |         |            |        |   exists (MatchEngine.SetPlayerTactic / PlayerTacticConfig). No code change. |
// | 1.5     | 2026-07-07 | —      | Cheap-item addition: + RestDefenseSufficient routing field (new §3.2/§7.7), |
// |         |            |        |   Stage0Default seeds true (identity); consumed in UtilityScorer with a    |
// |         |            |        |   ball-carrier-awareness gate (v1.6).                                       |
// | 1.6     | 2026-07-07 | —      | Reverted after user review: the AgentLane (LaneId) half-spaces routing      |
// |         |            |        |   field (new §3.2/§7.8) is REMOVED — half-spaces are an exploitable spatial |
// |         |            |        |   gap requiring tactical/player instructions, not a flat passing bonus.     |
// |         |            |        |   decision-tree.asmdef's PositioningAI reference is reverted (no longer     |
// |         |            |        |   needed by this file).                                                     |
// | 1.7     | 2026-07-11 | —      | #23 wiring (FR-DM-015): + DismarkIntensity routing field (zero value = Off  |
// |         |            |        |   = identity, safe unseeded; Stage0Default seeds Off explicitly). Drives    |
// |         |            |        |   the FM-DM-03 marked-pass-target penalty in UtilityScorer.                 |
// | 1.8     | 2026-07-23 | —      | #11/#10 (ERR-008-013): + SaveAvailable routing field (zero value false =    |
// |         |            |        |   identity; Stage0Default seeds false). Set only by MatchEngine under the   |
// |         |            |        |   EnableGkHeading flag; gates the DT-emitted SAVE (sole off-ball option).   |
#endregion
