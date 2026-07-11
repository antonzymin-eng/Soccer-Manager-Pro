// File:     src/match-engine/ManagerAdaptation.cs
// Created:  2026-07-11
// Modified: 2026-07-11
// Author:   —
// Spec:     Tactical Presets #26 §3.3 (FM-TP-03) / §3.4 (FM-TP-04), FR-TP-004/005/008/009/010/011,
//           KD-1/KD-5/KD-8, F3; Code Standards #20
// Purpose:  Manager-AI selection + adaptation logic: kickoff scoring, the one-rung StepToward
//           ladder with hold hysteresis, and the two apply paths (boot appliers at kickoff;
//           SetTeamTactic/SetPlayerTactic mid-match — never the appliers, F3).

using System;

using UnityEngine;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// The #26 manager-AI decision functions. Everything here is a pure function of
    /// (profile, score, clock, current preset) with a deterministic lowest-ordinal tiebreak —
    /// no RNG draw site, no domain tag (KD-8 / FR-TP-009) — and consumes ONLY own-state inputs
    /// (own score differential, time remaining, own preset, own profile): the signatures admit no
    /// opponent tactic or AI internals (KD-5 / FR-TP-008, mechanically auditable).
    ///
    /// APPLY PATHS (KD-1): <see cref="ApplyKickoff"/> is the FR-TP-004 boot path through the
    /// existing <see cref="TeamTacticConfigApplier"/>/<see cref="PlayerTacticConfigApplier"/>
    /// (pre-kickoff only); <see cref="RunDecisionPoint"/> is the FR-TP-005 mid-match path through
    /// <see cref="MatchEngine.SetTeamTactic"/>/<see cref="MatchEngine.SetPlayerTactic"/> and never
    /// references the appliers (F3 — no call site exists).
    /// </summary>
    public static class ManagerAdaptation
    {
        /// <summary>
        /// FM-TP-03 kickoff score for one preset:
        /// <c>BASE_FIT[p] + Aggression × AGGR_AFFINITY[p] + Caution × CAUT_AFFINITY[p]</c>.
        /// Dimensionless; rows bounded [−1, +1] (FR-TP-020). Worked example (Appendix B.1):
        /// the Aggressive archetype scores Gegenpress 0.66 / Balanced 0.50 / ParkTheBus −0.58.
        /// </summary>
        /// <param name="profile">The manager's own profile (KD-5 — the only disposition input).</param>
        /// <param name="presetOrdinal">A <see cref="TacticPresetLibrary"/> ordinal.</param>
        public static float KickoffScore(in ManagerProfile profile, int presetOrdinal)
        {
            if (presetOrdinal < 0 || presetOrdinal >= TacticPresetLibrary.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(presetOrdinal), presetOrdinal, "Preset ordinal beyond the A.1 catalogue (#26 F2).");
            }
            return TacticalPresetsConstants.BaseFit[presetOrdinal]
                 + profile.Aggression * TacticalPresetsConstants.AggrAffinity[presetOrdinal]
                 + profile.Caution * TacticalPresetsConstants.CautAffinity[presetOrdinal];
        }

        /// <summary>
        /// FM-TP-03 kickoff selection: argmax of <see cref="KickoffScore"/> over the catalogue;
        /// ties break to the lowest ordinal (KD-8 — the ascending scan with a strict
        /// greater-than compare keeps the first maximum).
        /// </summary>
        public static byte SelectKickoffPreset(in ManagerProfile profile)
        {
            int best = 0;
            float bestScore = float.NegativeInfinity;
            for (int p = 0; p < TacticPresetLibrary.Count; p++)
            {
                float score = KickoffScore(in profile, p);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }
            return (byte)best;
        }

        /// <summary>
        /// §3.4/§3.5 <c>StepToward</c>: one rung along the pinned A.1 attacking-order ladder
        /// (ParkTheBus 0 … Gegenpress 4), saturating at both ends. One step per decision — with
        /// the FR-TP-011 hold this makes oscillation structurally impossible within
        /// <c>2 × hold</c> windows.
        /// </summary>
        /// <param name="moreAttacking">True steps toward Gegenpress, false toward ParkTheBus.</param>
        /// <param name="current">The current preset ordinal.</param>
        public static byte StepToward(bool moreAttacking, byte current)
        {
            if (current >= TacticPresetLibrary.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(current), current, "Preset ordinal beyond the A.1 catalogue (#26 F2).");
            }
            if (moreAttacking)
            {
                return current == TacticPresetLibrary.Count - 1 ? current : (byte)(current + 1);
            }
            return current == 0 ? current : (byte)(current - 1);
        }

        /// <summary>
        /// FM-TP-04 adaptation ladder — the pure target computation. Consumes exactly the KD-5
        /// own-state inputs; the caller owns the hold hysteresis (<see cref="RunDecisionPoint"/>).
        /// <para>
        /// <c>t01 = clamp01(ticksRemaining / matchTicksTotal)</c> (1 = full match left, 0 = final
        /// whistle); <c>urgency = Aggression × (1−t01) × min(−goalDiff, URGENCY_DIFF_CAP)</c> when
        /// trailing, <c>protect = Caution × (1−t01) × min(goalDiff, cap)</c> when leading; a term
        /// ≥ <c>ADAPT_STEP_THRESHOLD</c> steps one rung (urgency → attacking wins when both fire —
        /// they cannot both be non-zero, the goal diff has one sign). Worked example (Appendix
        /// B.2): Aggressive 0–1 down at 70′ (t01 ≈ 0.222) → urgency 0.622 ≥ 0.35 → step;
        /// Pragmatic same state → 0.233 → hold.
        /// </para>
        /// <para>
        /// PREREQUISITE (PASS-1 M-1): <paramref name="matchTicksTotal"/> is the engine-owned
        /// <c>MATCH_TICKS_TOTAL</c>, still <c>[CROSS-PENDING]</c>, and <paramref name="goalDiff"/>
        /// requires engine score state that has no producer yet — both are explicit parameters so
        /// the reviewed contract is test-exercisable today (#26 §3.4) and the live inputs arrive
        /// with goal detection (§7.2).
        /// </para>
        /// </summary>
        /// <param name="profile">The manager's own profile.</param>
        /// <param name="current">The team's current preset ordinal.</param>
        /// <param name="goalDiff">Own goals − opponent goals.</param>
        /// <param name="ticksRemaining">60 Hz ticks left in the match.</param>
        /// <param name="matchTicksTotal">Total 60 Hz ticks in the match (must be positive).</param>
        public static byte EvaluateLadder(
            in ManagerProfile profile, byte current, int goalDiff, long ticksRemaining, long matchTicksTotal)
        {
            if (matchTicksTotal <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(matchTicksTotal), matchTicksTotal, "matchTicksTotal must be positive (#26 §3.4).");
            }
            float t01 = Mathf.Clamp01((float)ticksRemaining / matchTicksTotal);
            float urgency = goalDiff < 0
                ? profile.Aggression * (1f - t01) * Math.Min(-goalDiff, TacticalPresetsConstants.UrgencyDiffCap)
                : 0f;
            float protect = goalDiff > 0
                ? profile.Caution * (1f - t01) * Math.Min(goalDiff, TacticalPresetsConstants.UrgencyDiffCap)
                : 0f;

            if (urgency >= TacticalPresetsConstants.AdaptStepThreshold)
            {
                return StepToward(moreAttacking: true, current);
            }
            if (protect >= TacticalPresetsConstants.AdaptStepThreshold)
            {
                return StepToward(moreAttacking: false, current);
            }
            return current;
        }

        /// <summary>
        /// One fired decision point for an AI-managed team (the FR-TP-005 mid-match path). Stamps
        /// <see cref="ManagerState.LastDecisionTick"/>, runs the FR-TP-011 hold hysteresis
        /// (decrement-then-check, so a switch is evaluable at the decision point where the hold
        /// reaches 0 — the B.2 worked example's 70′→80′ cadence; the #25 RotationController hold
        /// precedent), and on a ladder step applies the target preset via
        /// <see cref="MatchEngine.SetTeamTactic"/>/<see cref="MatchEngine.SetPlayerTactic"/> —
        /// NEVER the boot appliers (F3). The staged change commits at the same stride boundary
        /// (FR-TP-018 — the engine evaluates the gate before its FR-TI-027 commit).
        /// </summary>
        /// <param name="engine">The host engine (mid-match apply seam).</param>
        /// <param name="teamId">The AI-managed team.</param>
        /// <param name="state">The team's manager state (mutated: tick stamp, hold, preset).</param>
        /// <param name="tick">The fired decision tick.</param>
        /// <param name="goalDiff">Own score differential (see <see cref="EvaluateLadder"/>'s prerequisite note).</param>
        /// <param name="ticksRemaining">60 Hz ticks left in the match.</param>
        /// <param name="matchTicksTotal">Total 60 Hz ticks in the match.</param>
        internal static void RunDecisionPoint(
            MatchEngine engine, int teamId, ref ManagerState state, int tick,
            int goalDiff, long ticksRemaining, long matchTicksTotal)
        {
            state.LastDecisionTick = tick;

            if (state.HoldIntervalsRemaining > 0)
            {
                state.HoldIntervalsRemaining--;
                if (state.HoldIntervalsRemaining > 0)
                {
                    return;
                }
                // Hold reached 0 at THIS decision point: the switch is evaluable now (B.2).
            }

            ManagerProfile profile = ManagerProfile.FromArchetype(state.ProfileOrdinal);
            byte target = EvaluateLadder(in profile, state.CurrentPresetOrdinal, goalDiff, ticksRemaining, matchTicksTotal);
            if (target == state.CurrentPresetOrdinal)
            {
                return;
            }

            TacticPreset preset = TacticPresetLibrary.Presets[target];
            preset.ValidatePlayers(MatchEngineConstants.PLAYERS_PER_TEAM);
            engine.SetTeamTactic(teamId, preset.Team);
            if (preset.Players != null)
            {
                int baseIndex = teamId * MatchEngineConstants.PLAYERS_PER_TEAM;
                for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
                {
                    engine.SetPlayerTactic(baseIndex + k, preset.Players[k]);
                }
            }
            state.CurrentPresetOrdinal = target;
            state.HoldIntervalsRemaining =
                TacticalPresetsConstants.ManagerSwitchHoldIntervals * profile.PatienceIntervals;
        }

        /// <summary>
        /// The FR-TP-004/010 kickoff boot path. For every team the engine has in
        /// <see cref="ManagerMode.AI"/>: selects a preset from the team's own profile (FM-TP-03),
        /// composes the final configs — an AI team's entries come from its selected preset, every
        /// other entry stays the caller's baseline (the FM-TP-01 rule, generalized to both teams;
        /// the single-team pure form is <see cref="TacticPresetProjection.Project"/>, T-TP-U-002) —
        /// applies them via the existing boot appliers, and seeds the team's
        /// <see cref="ManagerState"/> (current preset; <c>LastDecisionTick = 0</c>, consuming the
        /// kickoff decision so the tick-0 in-engine gate does not double-fire).
        /// Call BEFORE the first <see cref="MatchEngine.RunTick"/> — the appliers are
        /// pre-kickoff-only (KD-1). With no AI-managed team this applies the baselines unchanged.
        /// </summary>
        /// <param name="engine">The engine to configure (before its first tick).</param>
        /// <param name="teamBaseline">Per-team human/config tactics (null ⇒ <see cref="TeamTacticConfig.Default"/>).</param>
        /// <param name="playerBaseline">Per-agent human/config tactics (null ⇒ <see cref="PlayerTacticConfig.Identity"/>).</param>
        public static void ApplyKickoff(
            MatchEngine engine, TeamTacticConfig teamBaseline = null, PlayerTacticConfig playerBaseline = null)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }
            teamBaseline = teamBaseline ?? TeamTacticConfig.Default;
            playerBaseline = playerBaseline ?? PlayerTacticConfig.Identity;

            TeamTactic homeTactic = teamBaseline.ForTeam(0);
            TeamTactic awayTactic = teamBaseline.ForTeam(1);
            PlayerTactic[] byAgent = new PlayerTactic[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                byAgent[i] = playerBaseline.ForAgent(i);
            }

            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                ManagerState state = engine.GetManagerState(t);
                if (state.Mode != ManagerMode.AI)
                {
                    continue;
                }
                ManagerProfile profile = ManagerProfile.FromArchetype(state.ProfileOrdinal);
                byte selected = SelectKickoffPreset(in profile);
                TacticPreset preset = TacticPresetLibrary.Presets[selected];
                preset.ValidatePlayers(MatchEngineConstants.PLAYERS_PER_TEAM);

                if (t == 0)
                {
                    homeTactic = preset.Team;
                }
                else
                {
                    awayTactic = preset.Team;
                }
                if (preset.Players != null)
                {
                    int baseIndex = t * MatchEngineConstants.PLAYERS_PER_TEAM;
                    for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
                    {
                        byAgent[baseIndex + k] = preset.Players[k];
                    }
                }
                engine.SeedManagerKickoff(t, selected);
            }

            TeamTacticConfigApplier.Apply(engine, new TeamTacticConfig(homeTactic, awayTactic));
            PlayerTacticConfigApplier.Apply(engine, new PlayerTacticConfig(byAgent));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-11 | —      | Initial implementation (#26 T3/T4 — FM-TP-03 scoring, FM-TP-04     |
// |         |            |        |   ladder + decrement-then-check hold, boot + mid-match apply paths). |
#endregion
