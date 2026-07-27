// File:     src/goalkeeper-mechanics/GoalkeeperMechanics.cs
// Created:  2026-05-28
// Modified: 2026-06-14
// Modified: 2026-07-23 (GK/Heading engine-integration Phase 2: CaptureState/RestoreState snapshot seam over
//           the per-GK cross-tick arrays, for the Match Engine v18 save/restore path)
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.1–§3.8, §4.6, KD-9, KD-12, KD-13, KD-15, KD-16, Code Standards #20
// Purpose:  Main 10 Hz + 60 Hz orchestrator. Manages per-GK state, dive kinematics, reaction pipeline,
//           handling quality, cross-claim duels, rush dispatch, and distribution. Constructor-injected.

using System;

using UnityEngine;
using Unity.Profiling;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// 10 Hz and 60 Hz orchestrator for Goalkeeper Mechanics #11.
    /// Dispatches §3.2–§3.8 sub-systems per GK per frame. Resolves cross-claim duels.
    /// Publishes SaveAttemptedEvent / BallClaimedEvent / DistributionExecutedEvent / GoalkeeperRushEvent.
    /// Constructor-injected dependencies (FR-CS-051–054). Zero heap allocation on hot path.
    /// Goalkeeper Mechanics #11 §4.6.
    /// </summary>
    public sealed class GoalkeeperMechanics
    {
        // ── Dependencies ─────────────────────────────────────────────────────────────

        private readonly IGoalkeeperBallSystem _ballSystem;
        private readonly IGoalkeeperRngService _rng;
        private readonly GoalkeeperCrossClaimDuel _crossClaimDuel;
        private readonly GoalkeeperTelemetry   _telemetry;

        // ── Per-GK state arrays (pre-allocated; indexed by [0, MaxGkAgents)) ────────

        private readonly GoalkeeperState[]             _states;
        private readonly GoalkeeperAgentAttributes[]   _attrs;
        private readonly GkContactState[]              _contactStates;
        private readonly SaveIntent[]                  _saveIntents;
        private readonly bool[]                        _saveIntentActive;
        private readonly RushIntent[]                  _rushIntents;
        private readonly bool[]                        _rushIntentActive;
        private readonly DistributeIntent[]            _distributeIntents;
        private readonly bool[]                        _distributeIntentActive;
        private readonly GoalkeeperPositioningContract[] _positioningContracts;

        // Dive state
        private readonly int[]   _diveLaunchFrames;
        private readonly int[]   _diveDurationFrames;
        private readonly float[] _divePeakHandZ;
        private readonly float[] _diveDirectionLateral;
        private readonly float[] _rushLaunchMps;
        private readonly int[]   _rushInitialAttackerId;

        // Shot reaction state
        private readonly float[] _shotDetectedTickMs;
        private readonly float[] _requiredReactionMs;
        private readonly bool[]  _shotEventPending;

        // Hold-rule state
        private readonly int[]  _claimTick;
        private readonly int[]  _releaseTickEarliest;
        private readonly int[]  _recoveryCooldownEndTick;

        // ── Profiler Markers ─────────────────────────────────────────────────────────

        private static readonly ProfilerMarker s_tacticalTickMarker =
            new ProfilerMarker("GoalkeeperMechanics.TacticalTick");

        private static readonly ProfilerMarker s_updateMarker =
            new ProfilerMarker("GoalkeeperMechanics.Update");

        // ── Constructor ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Allocates all per-GK buffers and wires dependencies.
        /// No allocation occurs during TacticalTick or Update calls after this point.
        /// Goalkeeper Mechanics #11 §4.6.
        /// </summary>
        public GoalkeeperMechanics(
            IGoalkeeperBallSystem ballSystem,
            IGoalkeeperRngService rng)
        {
            _ballSystem     = ballSystem;
            _rng            = rng;
            _crossClaimDuel = new GoalkeeperCrossClaimDuel();
            _telemetry      = new GoalkeeperTelemetry();

            int maxGks = GoalkeeperConstants.MaxGkAgents;

            _states                 = new GoalkeeperState[maxGks];
            _attrs                  = new GoalkeeperAgentAttributes[maxGks];
            _contactStates          = new GkContactState[maxGks];
            _saveIntents            = new SaveIntent[maxGks];
            _saveIntentActive       = new bool[maxGks];
            _rushIntents            = new RushIntent[maxGks];
            _rushIntentActive       = new bool[maxGks];
            _distributeIntents      = new DistributeIntent[maxGks];
            _distributeIntentActive = new bool[maxGks];
            _positioningContracts   = new GoalkeeperPositioningContract[maxGks];

            _diveLaunchFrames       = new int[maxGks];
            _diveDurationFrames     = new int[maxGks];
            _divePeakHandZ          = new float[maxGks];
            _diveDirectionLateral   = new float[maxGks];
            _rushLaunchMps          = new float[maxGks];
            _rushInitialAttackerId  = new int[maxGks];

            _shotDetectedTickMs     = new float[maxGks];
            _requiredReactionMs     = new float[maxGks];
            _shotEventPending       = new bool[maxGks];

            _claimTick              = new int[maxGks];
            _releaseTickEarliest    = new int[maxGks];
            _recoveryCooldownEndTick = new int[maxGks];

            // Sentinel init
            for (int i = 0; i < maxGks; i++)
            {
                _diveLaunchFrames[i]        = -1;
                _claimTick[i]               = -1;
                _releaseTickEarliest[i]      = int.MaxValue;
                _recoveryCooldownEndTick[i]  = 0;
                _rushInitialAttackerId[i]   = -1;
                _contactStates[i]           = GkContactState.CreateNew();
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the Positioning AI #12 baseline slot for the specified GK.
        /// Called once per 10 Hz tick per §4.6.1 / KD-13.
        /// </summary>
        public void UpdateBaselineSlot(int gkIndex, Vector2 gkBaselineSlot)
        {
            if ((uint)gkIndex >= (uint)GoalkeeperConstants.MaxGkAgents)
            {
                return;
            }

            _positioningContracts[gkIndex].GkBaselineSlot = gkBaselineSlot;
        }

        /// <summary>
        /// Notifies that a shot has been struck at the specified GK's goal, opening the §3.2 reaction
        /// window. Sets the pending-shot flag for the next 60 Hz tick.
        ///
        /// <para>The caller supplies <paramref name="attrs"/> because the §3.2 latency and required-reaction
        /// formulas are attribute-driven and this is frequently the FIRST call for an episode — earlier
        /// than <see cref="CommitSaveIntent"/>, which is the only other writer of the per-GK attribute
        /// snapshot. Reading a stale or default snapshot here would date the reaction window off a keeper
        /// with zeroed Reflexes. Same convention as <c>CommitSaveIntent</c>: the composition root owns the
        /// projection (KD-P4 — runtime TeamId/Fatigue are the caller's to supply).</para>
        ///
        /// §3.2 / §4.6.2. ERR-011-004 gave this method its first production caller.
        /// </summary>
        /// <param name="gkIndex">Keeper index (== team id; KD-1).</param>
        /// <param name="shotMatchTimeMs">Match time (ms) at which the shot was struck.</param>
        /// <param name="ballSpeedMps">Ball speed (m/s) at shot execution.</param>
        /// <param name="attrs">The keeper's projected attributes for this episode.</param>
        public void OnShotExecutedEvent(
            int gkIndex, float shotMatchTimeMs, float ballSpeedMps, GoalkeeperAgentAttributes attrs)
        {
            if ((uint)gkIndex >= (uint)GoalkeeperConstants.MaxGkAgents)
            {
                return;
            }

            _attrs[gkIndex]              = attrs;
            _shotEventPending[gkIndex]   = true;
            _shotDetectedTickMs[gkIndex] =
                GoalkeeperReactionPipeline.ComputeShotDetectedTickMs(shotMatchTimeMs, attrs);
            _requiredReactionMs[gkIndex] =
                GoalkeeperReactionPipeline.ComputeRequiredReactionMs(attrs, ballSpeedMps, _states[gkIndex]);
        }

        /// <summary>
        /// Commits a SaveIntent for the specified GK from the 10 Hz Decision Tree output.
        /// §3.1 / §4.6.1.
        /// </summary>
        public void CommitSaveIntent(int gkIndex, SaveIntent intent, GoalkeeperAgentAttributes attrs)
        {
            if ((uint)gkIndex >= (uint)GoalkeeperConstants.MaxGkAgents)
            {
                return;
            }

            _saveIntents[gkIndex]      = intent;
            _saveIntentActive[gkIndex] = true;
            _attrs[gkIndex]            = attrs;
            _contactStates[gkIndex]    = GkContactState.CreateNew();
            _contactStates[gkIndex].HandChoice       = intent.TargetHand;
            _contactStates[gkIndex].ClutchFirmness   = intent.ClutchFirmness;
        }

        /// <summary>
        /// Commits a RushIntent for the specified GK from the 10 Hz Decision Tree output.
        /// §3.7 / §4.6.1.
        /// </summary>
        public void CommitRushIntent(int gkIndex, RushIntent intent, GoalkeeperAgentAttributes attrs)
        {
            if ((uint)gkIndex >= (uint)GoalkeeperConstants.MaxGkAgents)
            {
                return;
            }

            _rushIntents[gkIndex]      = intent;
            _rushIntentActive[gkIndex] = true;
            _attrs[gkIndex]            = attrs;
            _rushLaunchMps[gkIndex]    = GoalkeeperRushDispatch.ComputeRushLaunchMps(attrs);
            // Lock the attacker the rush commits against (current ball holder) so a later
            // F-08 interception abort fires only on possession passing to a THIRD party (KD-15).
            _rushInitialAttackerId[gkIndex] = _ballSystem.GetBallPossessorId();
        }

        /// <summary>
        /// Commits a DistributeIntent for the specified GK from the 10 Hz Decision Tree output.
        /// §3.8 / §4.6.1.
        /// </summary>
        public void CommitDistributeIntent(int gkIndex, DistributeIntent intent)
        {
            if ((uint)gkIndex >= (uint)GoalkeeperConstants.MaxGkAgents)
            {
                return;
            }

            _distributeIntents[gkIndex]      = intent;
            _distributeIntentActive[gkIndex] = true;
        }

        // ── Snapshot seam (design note §2.6 / GK-Heading Phase 2) ─────────────────────

        /// <summary>
        /// Snapshot seam: bundles this orchestrator's full per-GK cross-tick state (state machine, intents +
        /// active latches, dive / reaction / hold-rule buffers) into a <see cref="GoalkeeperTickState"/> view
        /// so a host snapshot layer can serialize it canonically for deterministic save/restore (parallel to
        /// the Pressing <see cref="TacticalDirector.PressingAI.PressingTickState"/> seam). The bundled arrays
        /// are the live, allocated-once instances (read-only serialization use only).
        /// </summary>
        public GoalkeeperTickState CaptureState() =>
            new GoalkeeperTickState(
                _states, _attrs, _contactStates,
                _saveIntents, _saveIntentActive,
                _rushIntents, _rushIntentActive,
                _distributeIntents, _distributeIntentActive,
                _positioningContracts,
                _diveLaunchFrames, _diveDurationFrames, _divePeakHandZ, _diveDirectionLateral,
                _rushLaunchMps, _rushInitialAttackerId,
                _shotDetectedTickMs, _requiredReactionMs, _shotEventPending,
                _claimTick, _releaseTickEarliest, _recoveryCooldownEndTick);

        /// <summary>
        /// Restores this orchestrator's cross-tick state from a snapshot produced by
        /// <see cref="CaptureState"/> (deterministic save/restore — the goalkeeper analogue of the Pressing /
        /// Defensive <c>RestoreState</c> seams). Each array in <paramref name="state"/> is copied element-wise
        /// into the live, allocated-once container (the caller supplies a freshly-built view with matching
        /// <c>MaxGkAgents</c> length — the internal containers stay the authoritative instances). No
        /// per-tick output buffers exist outside these arrays, so forward replay from the restored tick is
        /// byte-identical.
        /// </summary>
        public void RestoreState(in GoalkeeperTickState state)
        {
            Array.Copy(state.States,                  _states,                  _states.Length);
            Array.Copy(state.Attrs,                   _attrs,                   _attrs.Length);
            Array.Copy(state.ContactStates,           _contactStates,           _contactStates.Length);
            Array.Copy(state.SaveIntents,             _saveIntents,             _saveIntents.Length);
            Array.Copy(state.SaveIntentActive,        _saveIntentActive,        _saveIntentActive.Length);
            Array.Copy(state.RushIntents,             _rushIntents,             _rushIntents.Length);
            Array.Copy(state.RushIntentActive,        _rushIntentActive,        _rushIntentActive.Length);
            Array.Copy(state.DistributeIntents,       _distributeIntents,       _distributeIntents.Length);
            Array.Copy(state.DistributeIntentActive,  _distributeIntentActive,  _distributeIntentActive.Length);
            Array.Copy(state.PositioningContracts,    _positioningContracts,    _positioningContracts.Length);
            Array.Copy(state.DiveLaunchFrames,        _diveLaunchFrames,        _diveLaunchFrames.Length);
            Array.Copy(state.DiveDurationFrames,      _diveDurationFrames,      _diveDurationFrames.Length);
            Array.Copy(state.DivePeakHandZ,           _divePeakHandZ,           _divePeakHandZ.Length);
            Array.Copy(state.DiveDirectionLateral,    _diveDirectionLateral,    _diveDirectionLateral.Length);
            Array.Copy(state.RushLaunchMps,           _rushLaunchMps,           _rushLaunchMps.Length);
            Array.Copy(state.RushInitialAttackerId,   _rushInitialAttackerId,   _rushInitialAttackerId.Length);
            Array.Copy(state.ShotDetectedTickMs,      _shotDetectedTickMs,      _shotDetectedTickMs.Length);
            Array.Copy(state.RequiredReactionMs,      _requiredReactionMs,      _requiredReactionMs.Length);
            Array.Copy(state.ShotEventPending,        _shotEventPending,        _shotEventPending.Length);
            Array.Copy(state.ClaimTick,               _claimTick,               _claimTick.Length);
            Array.Copy(state.ReleaseTickEarliest,     _releaseTickEarliest,     _releaseTickEarliest.Length);
            Array.Copy(state.RecoveryCooldownEndTick, _recoveryCooldownEndTick, _recoveryCooldownEndTick.Length);
        }

        // ── 10 Hz tactical loop ──────────────────────────────────────────────────────

        /// <summary>
        /// 10 Hz tactical tick entry point per §4.6.1.
        /// Evaluates state-machine transitions, applies positioning micro-update,
        /// and updates hold-rule counters.
        /// Called once per GK in #16 §3.2 entity order. §3.1.2.
        /// </summary>
        /// <param name="currentTick">Current 10 Hz tactical tick index.</param>
        /// <param name="agentStates">AM #2 agent state array indexed by agentId.</param>
        /// <param name="ballState">Current BallState snapshot from Ball Physics #1.</param>
        /// <param name="gkAgentIds">Array of GK agent IDs, indexed by gkIndex (length = MaxGkAgents).</param>
        public void TacticalTick(
            int currentTick,
            AgentState[] agentStates,
            BallState ballState,
            int[] gkAgentIds)
        {
            using var _ = s_tacticalTickMarker.Auto();

            for (int gkIndex = 0; gkIndex < GoalkeeperConstants.MaxGkAgents; gkIndex++)
            {
                int agentId = gkAgentIds[gkIndex];
                if ((uint)agentId >= (uint)agentStates.Length)
                {
                    continue;
                }

                AgentState agentState = agentStates[agentId];
                GoalkeeperAgentAttributes attrs = _attrs[gkIndex];
                GoalkeeperPositioningContract positioning = _positioningContracts[gkIndex];

                // ERR-011-002 — how close the ball is to the goal THIS keeper defends.
                //
                // This was a per-side constant PAIR computing the third the keeper's OWN TEAM ATTACKS,
                // then handing it to a state-machine parameter whose own doc reads "the attacking third
                // from the perspective of the OPPOSING team (i.e. threatening GK's goal)" — the opposite
                // end of the pitch. The keeper therefore went Set/Anticipate when the ball was 70 m away
                // and sat Resting while it was in its own box. Measured over three full matches, keepers
                // spent 76-92% of every match parked in Anticipate (there is no Anticipate exit but a
                // dive), entered for the wrong reason.
                //
                // Fixed per §5.Z.12 — "a pair has two places that must agree; a mirror has one": ONE
                // signed distance to the keeper's own goal, and both predicates derived from it. Team 0
                // defends x = 0 and team 1 defends x = PitchLengthM, the same convention
                // GkHeadingIntentSource.SaveArmed and MatchEngine's goal detection use.
                float ownGoalX = attrs.TeamId == 0 ? 0.0f : GoalkeeperConstants.PitchLengthM;
                float ballDistToOwnGoalM = Mathf.Abs(ballState.Position.x - ownGoalX);

                // The third in front of the keeper's own goal: the ball is a threat.
                float defensiveThirdDepthM = GoalkeeperConstants.PitchLengthM
                                           - GoalkeeperConstants.BallAttackingThirdXM;
                bool ballThreateningOwnGoal = ballDistToOwnGoalM <= defensiveThirdDepthM;

                // The far third: play is at the other end and the keeper can stand down.
                bool ballSafelyUpfield = ballDistToOwnGoalM >= GoalkeeperConstants.BallAttackingThirdXM;

                // Anticipation score from Decision Tree — Stage 0 approximation based on ball proximity
                // Full Decision Tree integration is a Stage 1 concern per §4.6.1
                // Stage 0 stub: full Decision Tree anticipation score at Stage 1 (§4.6.1)
                float anticipationScore = ballThreateningOwnGoal
                    ? GoalkeeperConstants.Stage0AnticipationScoreActive
                    : 0.0f;

                float rushCommitmentLevel = _rushIntentActive[gkIndex]
                    ? _rushIntents[gkIndex].CommitmentLevel : 0.0f;

                Vector2 gkXY = new Vector2(agentState.Position.x, agentState.Position.y);

                GoalkeeperState newState = GoalkeeperStateMachine.EvaluateTacticalTransition(
                    currentState:           _states[gkIndex],
                    ballState:              ballState,
                    hasSaveIntent:          _saveIntentActive[gkIndex],
                    hasRushIntent:          _rushIntentActive[gkIndex],
                    hasDistributeIntent:    _distributeIntentActive[gkIndex],
                    anticipationScore:      anticipationScore,
                    rushCommitmentLevel:    rushCommitmentLevel,
                    currentTick:            currentTick,
                    claimTick:              _claimTick[gkIndex],
                    releaseTickEarliest:    _releaseTickEarliest[gkIndex],
                    recoveryCooldownEndTick: _recoveryCooldownEndTick[gkIndex],
                    gkPosition:             gkXY,
                    gkBaselineSlot:         positioning.GkBaselineSlot,
                    ballThreateningOwnGoal: ballThreateningOwnGoal,
                    ballSafelyUpfield:      ballSafelyUpfield);

                // Forced release telemetry
                if (_states[gkIndex] == GoalkeeperState.HandsOnBall &&
                    newState == GoalkeeperState.Distributing &&
                    _claimTick[gkIndex] >= 0 &&
                    (currentTick - _claimTick[gkIndex]) >= GoalkeeperConstants.GK_HOLD_MAX_TICKS)
                {
                    _telemetry.RecordForcedRelease(agentId);
                }

                _states[gkIndex] = newState;
            }
        }

        // ── 60 Hz physics loop ───────────────────────────────────────────────────────

        /// <summary>
        /// 60 Hz physics tick entry point per §4.6.2.
        /// Advances dive kinematics, resolves hand-ball contacts, handles rush updates,
        /// manages distribution release, and publishes events.
        /// Called once per GK per physics frame. §3.1.2 / §4.6.2.
        /// </summary>
        /// <param name="currentFrame">Current 60 Hz physics frame index.</param>
        /// <param name="currentMatchTimeMs">Current match time (ms) from kickoff.</param>
        /// <param name="agentStates">AM #2 agent state array indexed by agentId.</param>
        /// <param name="ballState">Current BallState snapshot from Ball Physics #1.</param>
        /// <param name="gkAgentIds">Array of GK agent IDs, indexed by gkIndex (length = MaxGkAgents).</param>
        public void Update(
            int currentFrame,
            float currentMatchTimeMs,
            AgentState[] agentStates,
            BallState ballState,
            int[] gkAgentIds)
        {
            using var _ = s_updateMarker.Auto();

            // §3.6 cross-claim / aerial duel resolution requires opponent hand/head collider
            // geometry that the single-GK Stage 0 contact path does not yet plumb through this
            // entry point. The duel buffer is cleared each frame and the resolver is exercised by
            // GoalkeeperCrossClaimDuelTests; wiring contested multi-agent claims here is a Stage 1
            // deliverable (pending the multi-agent contact feed). Until then no participants are
            // registered, so ResolveHandContactDuel is intentionally not called.
            _crossClaimDuel.ClearFrameBuffer();

            for (int gkIndex = 0; gkIndex < GoalkeeperConstants.MaxGkAgents; gkIndex++)
            {
                int agentId = gkAgentIds[gkIndex];
                if ((uint)agentId >= (uint)agentStates.Length)
                {
                    continue;
                }

                AgentState agentState = agentStates[agentId];
                GoalkeeperState gkState = _states[gkIndex];

                // ── 60 Hz shot detection (Anticipate early trigger via ShotExecutedEvent) ──
                bool shotEventDetected = _shotEventPending[gkIndex];
                _shotEventPending[gkIndex] = false;

                // ── Per-frame reaction window update ──────────────────────────────────
                float reactionWindowAchieved = 0.0f;
                if (gkState == GoalkeeperState.Anticipate || gkState == GoalkeeperState.Diving || gkState == GoalkeeperState.Airborne)
                {
                    if (_shotDetectedTickMs[gkIndex] > 0.0f)
                    {
                        float elapsed = currentMatchTimeMs - _shotDetectedTickMs[gkIndex];
                        reactionWindowAchieved = GoalkeeperReactionPipeline.ComputeReactionWindowAchieved(
                            elapsed, _requiredReactionMs[gkIndex]);

                        _contactStates[gkIndex].ReactionWindowAchieved = reactionWindowAchieved;

                        ReactionLabel reactionLabel = GoalkeeperReactionPipeline.ComputeReactionLabel(reactionWindowAchieved);
                        _telemetry.RecordSaveReactionWindow(reactionWindowAchieved, reactionLabel);
                    }
                }

                // ── Dive kinematics (Diving / Airborne) ──────────────────────────────
                bool handBallContactOccurred = false;
                float handlingQualityScalar  = 0.0f;
                bool groundReEntry           = false;

                if (gkState == GoalkeeperState.Diving)
                {
                    // Transition Diving → Airborne: launch impulse applied this frame
                    if (_diveLaunchFrames[gkIndex] < 0)
                    {
                        _diveLaunchFrames[gkIndex]   = currentFrame;
                        _diveDurationFrames[gkIndex] = GoalkeeperDiveKinematics.ComputeDiveDurationFrames();

                        // Compute timing jitter via draw-site
                        float jitterGaussian = _rng.NextGaussian(
                            GoalkeeperConstants.DrawSiteDiveTimingJitter,
                            GoalkeeperConstants.DomainTagGoalkeeper);
                        float jitterMs = GoalkeeperConstants.DiveTimingJitterSigmaMs * jitterGaussian;

                        _divePeakHandZ[gkIndex] = GoalkeeperDiveKinematics.ComputePeakHandZ(_attrs[gkIndex], jitterMs);

                        // Dive direction: toward where the ball will cross the keeper's plane
                        // (lateral = Y, across the goal mouth). ERR-011-003 — see the helper.
                        _diveDirectionLateral[gkIndex] = _saveIntentActive[gkIndex]
                            ? ComputeDiveDirectionLateral(_saveIntents[gkIndex], agentState, ballState)
                            : 0.0f;

                        // Emit rush event (Launched phase) if applicable — not for save dives
                    }
                }

                if (gkState == GoalkeeperState.Airborne)
                {
                    int launchFrame    = _diveLaunchFrames[gkIndex];
                    int durationFrames = _diveDurationFrames[gkIndex];

                    float handZ = GoalkeeperDiveKinematics.ComputeHandPathZ(
                        currentFrame, launchFrame, durationFrames, _divePeakHandZ[gkIndex]);

                    // Ground re-entry check: end of dive phase without contact
                    int frameOffset = currentFrame - launchFrame;
                    groundReEntry = frameOffset >= durationFrames;

                    if (!groundReEntry)
                    {
                        // Check if ball is within reach envelope
                        float reachRadius = GoalkeeperDiveKinematics.ComputeReachRadius(_attrs[gkIndex]);
                        Vector3 gkPos3    = agentState.Position;
                        Vector3 reachCenter = GoalkeeperDiveKinematics.ComputeReachCenter(
                            gkPos3, currentFrame, launchFrame, durationFrames,
                            _diveDirectionLateral[gkIndex], handZ);

                        float distToBallSq = (ballState.Position - reachCenter).sqrMagnitude;

                        if (distToBallSq <= reachRadius * reachRadius)
                        {
                            handBallContactOccurred = true;

                            // Telemetry: real hand-envelope-vs-ball offset at contact (XY metres).
                            // The §3.5.1 pointQuality term consumes a SEPARATE cm-scale placement
                            // error (below); this field is diagnostic only.
                            _contactStates[gkIndex].ContactPointError = new Vector2(
                                reachCenter.x - ballState.Position.x,
                                reachCenter.y - ballState.Position.y);

                            // Compute handling quality
                            // Raw unit-variance Gaussian draws; GoalkeeperHandlingQuality.Compute
                            // scales handlingScaleNoise by HandlingNoiseSigma internally (§3.5.1).
                            float handlingNoiseRaw = _rng.NextGaussian(
                                GoalkeeperConstants.DrawSiteHandlingNoise,
                                GoalkeeperConstants.DomainTagGoalkeeper);

                            float pointNoiseRaw = _rng.NextGaussian(
                                GoalkeeperConstants.DrawSiteHandlingPointNoise,
                                GoalkeeperConstants.DomainTagGoalkeeper);

                            float pointNoise = GoalkeeperConstants.HandlingPointErrorSigmaM * pointNoiseRaw;

                            // §3.5.1 pointQuality is a cm-scale placement-error term (worked
                            // example: 0.03 m). The reach gate above guarantees the ball is inside
                            // the hand envelope, so the Stage 0 model treats the hand as reaching
                            // the ball with the residual deviation supplied by pointErrorNoise —
                            // both anchors are the ball position. Feeding the metre-scale envelope
                            // offset here would saturate pointQuality to 0 (divisor is 0.05 m) and
                            // make clean catches/parries unreachable.
                            handlingQualityScalar = GoalkeeperHandlingQuality.Compute(
                                attrs:                  _attrs[gkIndex],
                                handContactActual:      ballState.Position,
                                targetHandContact:      ballState.Position,
                                ballSpeedMps:           ballState.Velocity.magnitude,
                                reactionWindowAchieved: _contactStates[gkIndex].ReactionWindowAchieved,
                                state:                  gkState,
                                handlingScaleNoise:     handlingNoiseRaw,
                                pointErrorNoise:        pointNoise,
                                handlingLabel:          out HandlingQualityLabel handlingLabel);

                            _contactStates[gkIndex].HandlingQualityScalar = handlingQualityScalar;
                            _contactStates[gkIndex].ActualContactFrame     = currentFrame;

                            _telemetry.RecordSaveHandlingQuality(handlingQualityScalar, handlingLabel);
                            _telemetry.RecordSaveOutcome(handlingLabel);

                            // Publish SaveAttemptedEvent
                            ReactionLabel rLabel = GoalkeeperReactionPipeline.ComputeReactionLabel(
                                _contactStates[gkIndex].ReactionWindowAchieved);

                            SaveAttemptedEvent saveEvt = new SaveAttemptedEvent
                            {
                                AgentId                = agentId,
                                MatchTimeMs            = currentMatchTimeMs,
                                HandlingQualityScalar  = handlingQualityScalar,
                                HandlingLabel          = handlingLabel,
                                ReactionWindowAchieved = _contactStates[gkIndex].ReactionWindowAchieved,
                                ReactionLabel          = rLabel,
                                IncomingBallState      = ballState,
                                ContactPointError      = _contactStates[gkIndex].ContactPointError,
                                FailureCause           = handlingLabel == HandlingQualityLabel.Missed
                                                            ? FailureCause.MissedContact
                                                            : default,
                                HandContactPosition    = reachCenter,
                                HandUsed               = _contactStates[gkIndex].HandChoice,
                                ContactBodyPart        = BodyPartEnum.Hand
                            };

                            EventBusStub.Publish(in saveEvt);

                            // If caught: Ball.SetPossessor + BallClaimedEvent
                            if (handlingQualityScalar >= GoalkeeperConstants.CatchThreshold)
                            {
                                _ballSystem.SetPossessor(agentId);
                                _claimTick[gkIndex]            = currentFrame / GoalkeeperConstants.FramesPerTacticalTick;
                                _releaseTickEarliest[gkIndex]   = _claimTick[gkIndex] + 1;

                                BallClaimedEvent claimEvt = new BallClaimedEvent
                                {
                                    AgentId               = agentId,
                                    MatchTimeMs           = currentMatchTimeMs,
                                    HandlingQualityScalar = handlingQualityScalar,
                                    ClaimType             = ClaimType.ShotCatch,
                                    ClaimPosition         = reachCenter,
                                    ContactBodyPart       = BodyPartEnum.Hand,
                                    ContestedDuelId       = -1
                                };

                                EventBusStub.Publish(in claimEvt);
                                _telemetry.RecordBallClaim(ClaimType.ShotCatch);
                            }
                            else if (handlingQualityScalar >= GoalkeeperConstants.ParryThreshold)
                            {
                                Vector3 parryVel = GoalkeeperHandlingQuality.ComputeParryVelocity(
                                    ballState.Velocity, handlingQualityScalar,
                                    _contactStates[gkIndex].ClutchFirmness);

                                _ballSystem.ApplyKick(parryVel, ballState.AngularVelocity, agentId, currentMatchTimeMs);
                            }
                            else if (handlingQualityScalar >= GoalkeeperConstants.DeflectThreshold)
                            {
                                Vector3 deflectionTarget = _saveIntentActive[gkIndex] && _saveIntents[gkIndex].DeflectionTarget.HasValue
                                    ? _saveIntents[gkIndex].DeflectionTarget.Value
                                    : ballState.Position + ballState.Velocity.normalized * GoalkeeperConstants.Stage0DeflectFallbackProjectionM;

                                Vector3 deflectVel = GoalkeeperHandlingQuality.ComputeDeflectVelocity(
                                    ballState.Velocity, handlingQualityScalar, deflectionTarget, reachCenter);

                                _ballSystem.ApplyKick(deflectVel, ballState.AngularVelocity, agentId, currentMatchTimeMs);
                            }
                            else if (handlingQualityScalar >= GoalkeeperConstants.MinHandlingQuality)
                            {
                                Vector3 spillVel = GoalkeeperHandlingQuality.ComputeSpillVelocity(
                                    ballState.Velocity, handlingQualityScalar);

                                _ballSystem.ApplyKick(spillVel, ballState.AngularVelocity, agentId, currentMatchTimeMs);
                            }
                        }
                    }
                }

                // ── Rush update ───────────────────────────────────────────────────────
                bool rushBallIntercepted            = false;
                bool attackerWithinOneVsOneRadius   = false;
                bool gkWithinSmotherRadius          = false;

                if (gkState == GoalkeeperState.Rushing)
                {
                    // Advance the GK toward the locked rush target and write the new position back
                    // to the AM #2 state array so the rush actually moves the keeper (§3.7.2).
                    Vector3 gkMutablePos = agentState.Position;
                    GoalkeeperRushDispatch.UpdateRushFrame(
                        ref gkMutablePos,
                        _rushIntents[gkIndex].RushTarget,
                        _rushLaunchMps[gkIndex]);
                    agentStates[agentId].Position = new Vector2(gkMutablePos.x, gkMutablePos.y);

                    // F-08 ball-interception check (KD-15): abort only when possession passes to a
                    // THIRD party — not the GK, not the attacker the rush committed against. The
                    // committed attacker still holding the ball is the expected case and must not abort.
                    int ballPossessorId = _ballSystem.GetBallPossessorId();
                    rushBallIntercepted = ballPossessorId >= 0
                                       && ballPossessorId != agentId
                                       && ballPossessorId != _rushInitialAttackerId[gkIndex];

                    if (rushBallIntercepted)
                    {
                        GoalkeeperRushEvent rushEvt = new GoalkeeperRushEvent
                        {
                            AgentId        = agentId,
                            MatchTimeMs    = currentMatchTimeMs,
                            RushPhase      = RushPhase.Aborted,
                            AbortReason    = AbortReason.BallIntercepted,
                            RushTarget     = _rushIntents[gkIndex].RushTarget,
                            GkPosition     = gkMutablePos,
                            RushLaunchMps  = _rushLaunchMps[gkIndex]
                        };
                        EventBusStub.Publish(in rushEvt);
                        _telemetry.RecordRushAbort(AbortReason.BallIntercepted);
                        _rushIntentActive[gkIndex] = false;
                    }
                    else
                    {
                        // Check 1v1 and smother triggers from the UPDATED GK position this frame.
                        attackerWithinOneVsOneRadius = CheckAttackerWithinRadius(
                            gkMutablePos, ballState, GoalkeeperConstants.OneVsOneTriggerRadiusM);

                        gkWithinSmotherRadius = CheckAttackerWithinRadius(
                            gkMutablePos, ballState, GoalkeeperConstants.SmotherTriggerRadiusM);
                    }
                }

                // ── OneOnOne close-down (Stage 0) ─────────────────────────────────────
                // Keep advancing toward the locked rush target and evaluate the smother trigger
                // so OneOnOne → Smothered can fire. The trigger is otherwise computed only while
                // Rushing, which stranded the keeper in OneOnOne (it could exit only via a 10 Hz
                // SaveIntent → Diving). Movement is the same locked-target dispatch as the rush.
                if (gkState == GoalkeeperState.OneOnOne)
                {
                    Vector3 gkMutablePos = agentState.Position;
                    if (_rushIntentActive[gkIndex])
                    {
                        GoalkeeperRushDispatch.UpdateRushFrame(
                            ref gkMutablePos,
                            _rushIntents[gkIndex].RushTarget,
                            _rushLaunchMps[gkIndex]);
                        agentStates[agentId].Position = new Vector2(gkMutablePos.x, gkMutablePos.y);
                    }

                    gkWithinSmotherRadius = CheckAttackerWithinRadius(
                        gkMutablePos, ballState, GoalkeeperConstants.SmotherTriggerRadiusM);
                }

                // ── Smother / 1v1 terminal contact (Stage 0 approximation) ────────────
                // The full §3.6 contested hand-ball resolution for Smothered/OneOnOne depends on
                // the #3 collision feed that is not plumbed into this entry point at Stage 0 (see
                // the ClearFrameBuffer note above). To avoid stranding the keeper in Smothered, a
                // close-range smother that reaches the ball within the save volume is resolved as a
                // committed 1v1 claim (catch) here; richer parry/deflect outcomes land at Stage 1.
                if (gkState == GoalkeeperState.Smothered)
                {
                    Vector3 gkBodyPos = agentState.Position;
                    float   smotherReach = GoalkeeperConstants.GkSaveVolumeRadiusM;
                    float   smotherDistSq = (ballState.Position - gkBodyPos).sqrMagnitude;

                    if (smotherDistSq <= smotherReach * smotherReach)
                    {
                        handBallContactOccurred = true;
                        handlingQualityScalar   = GoalkeeperConstants.CatchThreshold; // claim
                        _contactStates[gkIndex].HandlingQualityScalar = handlingQualityScalar;
                        _contactStates[gkIndex].ActualContactFrame    = currentFrame;

                        _ballSystem.SetPossessor(agentId);
                        _claimTick[gkIndex]           = currentFrame / GoalkeeperConstants.FramesPerTacticalTick;
                        _releaseTickEarliest[gkIndex]  = _claimTick[gkIndex] + 1;

                        BallClaimedEvent smotherClaim = new BallClaimedEvent
                        {
                            AgentId               = agentId,
                            MatchTimeMs           = currentMatchTimeMs,
                            HandlingQualityScalar = handlingQualityScalar,
                            ClaimType             = ClaimType.OneOnOne,
                            ClaimPosition         = gkBodyPos,
                            ContactBodyPart       = BodyPartEnum.Body,
                            ContestedDuelId       = -1
                        };
                        EventBusStub.Publish(in smotherClaim);
                        _telemetry.RecordBallClaim(ClaimType.OneOnOne);
                    }
                    else
                    {
                        // Attacker took the ball out of the smother volume — resolve as a failed
                        // close-down (contact, quality below MIN_HANDLING_QUALITY) so the Smothered
                        // state machine routes to Recovering rather than stalling. No claim emitted.
                        handBallContactOccurred = true;
                        handlingQualityScalar   = 0.0f;
                    }
                }

                // ── Distribution release ──────────────────────────────────────────────
                bool distributionReleaseReached = false;

                if (gkState == GoalkeeperState.Distributing && _distributeIntentActive[gkIndex])
                {
                    ref DistributeIntent distIntent = ref _distributeIntents[gkIndex];

                    GoalkeeperDistribution.ValidateTarget(
                        ref distIntent,
                        agentRosterContains: true, // Stage 0 stub: always true pending roster query
                        receiverMissingWarningEmitted: out bool receiverWarning,
                        targetPointClamped: out bool targetClamped);

                    if (receiverWarning) _telemetry.WarnDistributionTargetReceiverMissing(agentId);
                    if (targetClamped)   _telemetry.WarnDistributionTargetPointClamped(agentId);

                    float windupMs      = GoalkeeperDistribution.ComputeWindupMs(distIntent.DeliveryKind);
                    float accuracyCoeff = GoalkeeperDistribution.ComputeAccuracyCoeff(distIntent.DeliveryKind, _attrs[gkIndex]);
                    Vector3 releasePoint = GoalkeeperDistribution.ComputeReleasePoint(agentState.Position, distIntent.DeliveryKind);

                    // Stage 0 approximation: release on the frame the state was entered (windup is tracked externally)
                    distributionReleaseReached = true;

                    float emittedPower = distIntent.PowerIntent * accuracyCoeff;

                    DistributionExecutedEvent distEvt = new DistributionExecutedEvent
                    {
                        AgentId          = agentId,
                        MatchTimeMs      = currentMatchTimeMs,
                        DeliveryKind     = distIntent.DeliveryKind,
                        ReleasePoint     = releasePoint,
                        // DistributeIntent.TargetReceiverId is int? (null = zone-targeted);
                        // the event field is int with sentinel -1 for zone (v1.2 AR-2 row).
                        TargetReceiverId = distIntent.TargetReceiverId ?? -1,
                        TargetPoint      = distIntent.TargetPoint,
                        EmittedPowerIntent = emittedPower,
                        WindupMs         = windupMs
                    };

                    EventBusStub.Publish(in distEvt);
                    _telemetry.RecordDistribution(distIntent.DeliveryKind);
                    _distributeIntentActive[gkIndex] = false;
                }
                else if (gkState == GoalkeeperState.Distributing)
                {
                    // Forced 6-second release (§3.1 / FR-GK-028) reaches Distributing with no
                    // committed DistributeIntent. Release the state anyway so the GK does not
                    // stall holding the ball indefinitely; no DistributionExecutedEvent is
                    // published (the Decision Tree never supplied a delivery).
                    distributionReleaseReached = true;
                }

                // ── Physics state transition ──────────────────────────────────────────
                _states[gkIndex] = GoalkeeperStateMachine.EvaluatePhysicsTransition(
                    currentState:                 gkState,
                    handBallContactOccurred:      handBallContactOccurred,
                    handlingQualityScalar:         handlingQualityScalar,
                    groundReEntry:                groundReEntry,
                    distributionReleaseReached:    distributionReleaseReached,
                    rushBallIntercepted:           rushBallIntercepted,
                    attackerWithinOneVsOneRadius:  attackerWithinOneVsOneRadius,
                    gkWithinSmotherRadius:         gkWithinSmotherRadius,
                    shotEventDetected:             shotEventDetected);

                // ── Recovery cooldown tracking ────────────────────────────────────────
                if (_states[gkIndex] == GoalkeeperState.Recovering &&
                    gkState != GoalkeeperState.Recovering)
                {
                    int currentTacticalTick = currentFrame / GoalkeeperConstants.FramesPerTacticalTick;
                    _recoveryCooldownEndTick[gkIndex] = currentTacticalTick + GoalkeeperConstants.RecoveryCooldownTicks;
                }

                // ── Reset dive launch frame when no longer in dive states ─────────────
                GoalkeeperState newState = _states[gkIndex];
                if (newState != GoalkeeperState.Diving && newState != GoalkeeperState.Airborne)
                {
                    _diveLaunchFrames[gkIndex] = -1;
                }

                // ── Clear save intent once the save attempt is fully resolved ─────────
                // Airborne → HandsOnBall: caught. Airborne → Recovering: parry/deflect/spill/miss.
                // Smothered → HandsOnBall or Recovering also clears the active flag.
                if (_saveIntentActive[gkIndex])
                {
                    bool saveResolved = (gkState == GoalkeeperState.Airborne || gkState == GoalkeeperState.Smothered)
                                    && (newState == GoalkeeperState.HandsOnBall || newState == GoalkeeperState.Recovering);
                    if (saveResolved)
                    {
                        _saveIntentActive[gkIndex] = false;
                    }
                }

                // ── Clear rush intent once the rush chain is fully resolved ───────────
                // The chain is Rushing → {Smothered, OneOnOne} → {Smothered} → {HandsOnBall,
                // Recovering}. Clear when leaving any chain state into a terminal/holding state so a
                // stale active rush intent cannot spuriously re-trigger Set → Rushing later.
                if (_rushIntentActive[gkIndex])
                {
                    bool inRushChain = gkState == GoalkeeperState.Rushing
                                    || gkState == GoalkeeperState.OneOnOne
                                    || gkState == GoalkeeperState.Smothered;
                    if (inRushChain &&
                        (newState == GoalkeeperState.Smothered
                         || newState == GoalkeeperState.Recovering
                         || newState == GoalkeeperState.HandsOnBall))
                    {
                        _rushIntentActive[gkIndex] = false;
                    }
                }
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        // Lateral dive axis is Y (touchline-to-touchline): the goal mouth spans Y, so the keeper
        // dives left/right across the goal along Y — not along the goal-to-goal X axis (§1.2 / §3.3.1).
        //
        // ERR-011-003: this returned 0 in production for every dive ever launched. The only non-zero
        // branch is gated on SaveIntent.DeflectionTarget, and the engine's sole producer
        // (MatchEngine.HostSaveDispatch.CommitSave) sets DeflectionTarget = null — so the reach
        // envelope never displaced laterally and the keeper dived straight up on the spot. Measured
        // over three full matches: mean |diveDirectionLateral| = 0.000 across all six keepers, with the
        // closest approach of the envelope to the ball 2.75 m short over an entire match.
        //
        // The conflation is the root cause. DeflectionTarget is where the keeper wants to PUT the ball
        // (§3.5.3, the deflect aim point); it is not where the keeper should DIVE. A keeper dives at the
        // ball, so the direction is derived from the ball — and specifically from where the ball WILL
        // cross the keeper's own plane, not where it is now: a ball struck across the face of goal
        // arrives several metres from its current lateral position, and diving at the current position
        // is diving behind it.
        private static float ComputeDiveDirectionLateral(
            SaveIntent intent, AgentState agentState, BallState ballState)
        {
            // An explicit intent target still wins — a #8-supplied aim point is a deliberate instruction
            // and the Stage-1 producer may set it. Today's producer supplies none, so the ball decides.
            if (intent.DeflectionTarget.HasValue)
            {
                float targetDy = intent.DeflectionTarget.Value.y - agentState.Position.y;
                return Sign(targetDy);
            }

            // Linear interception in the XY plane: the time for the ball to reach the keeper's x, then
            // the ball's y at that time. Pure, allocation-free, and deterministic — no clock read and no
            // draw, so it cannot perturb the RNG cursor or the digest beyond the dive it steers.
            float dx = agentState.Position.x - ballState.Position.x;
            float vx = ballState.Velocity.x;

            float predictedY = ballState.Position.y;

            // Only extrapolate when the ball is actually closing on the keeper's plane. A ball moving
            // away (or laterally, vx ~ 0) gives a degenerate or negative time-to-plane; diving at an
            // extrapolation of it would send the keeper the wrong way, so fall back to the ball's
            // current lateral position, which is never worse than the pre-fix zero.
            if (Mathf.Abs(vx) > GoalkeeperConstants.DEGENERACY_EPSILON && dx * vx > 0.0f)
            {
                float timeToPlaneS = dx / vx;
                if (timeToPlaneS > 0.0f && timeToPlaneS <= GoalkeeperConstants.DivePredictionHorizonS)
                {
                    predictedY = ballState.Position.y + ballState.Velocity.y * timeToPlaneS;
                }
            }

            return Sign(predictedY - agentState.Position.y);
        }

        /// <summary>Signum with an exact zero at zero — the dive direction is a discrete
        /// {-1, 0, +1} axis selector per §3.3.4, not a magnitude.</summary>
        private static float Sign(float v)
        {
            return v > 0.0f ? 1.0f : v < 0.0f ? -1.0f : 0.0f;
        }

        private bool CheckAttackerWithinRadius(Vector3 gkPosition, BallState ballState, float radius)
        {
            // Stage 0: use ball position as attacker proxy when ball is possessed
            if (_ballSystem.GetBallPossessorId() >= 0)
            {
                float distSq = (ballState.Position - gkPosition).sqrMagnitude;
                return distSq <= radius * radius;
            }
            return false;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-28 | —      | AR-1: H-5 _rushLaunchMps set in CommitRushIntent; M-2 intent clear. |
// | 1.2     | 2026-06-12 | —      | Build fix (dotnet CI gate): using UnityEngine.Profiling ->          |
// |         |            |        | Unity.Profiling. ProfilerMarker's actual namespace is               |
// |         |            |        | Unity.Profiling; the old using was CS0246 under Unity and the Linux |
// |         |            |        | compile gate alike, so this assembly could not have compiled        |
// |         |            |        | in-engine. No functional change.                                    |
// | 1.3     | 2026-06-12 | —      | Build fix (dotnet CI gate):                                         |
// |         |            |        | DistributionExecutedEvent.TargetReceiverId assignment passed        |
// |         |            |        | DistributeIntent.TargetReceiverId (int?) into the int event field - |
// |         |            |        | CS0266 everywhere; assembly never compiled. Now coalesces null ->   |
// |         |            |        | -1, the zone-target sentinel the event field documented in its v1.2 |
// |         |            |        | AR-2 row. No behaviour change for receiver-targeted distributions.  |
// | 1.4     | 2026-06-14 | —      | AR-3 fix pass (2H+3M+L). H-1: §3.5.1 handling Compute was fed the  |
// |         |            |        | metre-scale reach-envelope offset (reachCenter vs ball) as the     |
// |         |            |        | point error, so pointQuality (divisor 0.05 m) saturated to 0 and   |
// |         |            |        | clean catches/parries were unreachable; both contact anchors are   |
// |         |            |        | now the ball position (cm-scale noise-driven error per §3.5.4).    |
// |         |            |        | H-2: rush UpdateRushFrame result was written to a discarded local; |
// |         |            |        | the GK never moved during a rush — now written back to            |
// |         |            |        | agentStates[agentId].Position and used for the same-frame          |
// |         |            |        | 1v1/smother radius checks. M-1: rush abort no longer fires on the  |
// |         |            |        | committed attacker holding the ball (GetInitialAttackerTargetId    |
// |         |            |        | stub −1 made every possession abort); attacker captured at         |
// |         |            |        | CommitRushIntent (_rushInitialAttackerId). M-2: dive lateral axis  |
// |         |            |        | X→Y (goal mouth spans Y, §1.2). M-3: cross-claim duel wiring       |
// |         |            |        | documented as a Stage 1 deliverable. L: SaveAttemptedEvent.        |
// |         |            |        | ContactPointError now populated (was always 0); dead              |
// |         |            |        | GetInitialAttackerTargetId + empty smother-if removed.            |
// | 1.5     | 2026-06-14 | —      | AR-4. M-1: forced 6-second release with no committed              |
// |         |            |        | DistributeIntent reached Distributing and stalled forever (the     |
// |         |            |        | release guard required an active intent); now exits to Recovering  |
// |         |            |        | without publishing when no delivery was supplied. M-2: Smothered/  |
// |         |            |        | OneOnOne had NO terminal contact resolver (contact was computed    |
// |         |            |        | only in the Airborne dive path), so a rush that reached the        |
// |         |            |        | smother radius stranded the keeper. Added a Stage 0 close-range    |
// |         |            |        | smother resolution: ball within the save volume → 1v1 claim       |
// |         |            |        | (SetPossessor + BallClaimedEvent); else a failed close-down routes |
// |         |            |        | to Recovering. Full §3.6 contested outcomes are Stage 1.          |
// | 1.6     | 2026-06-14 | —      | AR-5 M-1: OneOnOne → Smothered was dead — gkWithinSmotherRadius   |
// |         |            |        | was computed only while Rushing, so a keeper that reached OneOnOne |
// |         |            |        | (now reachable after the v1.4 H-2 rush-motion fix) stranded there. |
// |         |            |        | OneOnOne now advances toward the locked rush target and evaluates  |
// |         |            |        | the smother trigger; rush-intent clear broadened to the whole      |
// |         |            |        | Rushing/OneOnOne/Smothered chain so a stale intent cannot          |
// |         |            |        | re-trigger Set → Rushing.                                          |
// | 1.6     | 2026-07-23 | —      | GK/Heading engine-integration Phase 2: CaptureState() bundles the |
// |         |            |        | per-GK cross-tick arrays into a GoalkeeperTickState view;          |
// |         |            |        | RestoreState(in) copies them back into the live containers (the    |
// |         |            |        | Match Engine v18 snapshot seam, the PressingTickState pattern).    |
#endregion
