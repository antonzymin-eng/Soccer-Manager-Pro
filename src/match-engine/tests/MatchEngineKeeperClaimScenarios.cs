// File:     src/match-engine/tests/MatchEngineKeeperClaimScenarios.cs
// Created:  2026-08-03
// Modified: 2026-09-15 (W6 review closure — retain Controlled attachment lock and restore held-claim own-goal consequence guard)
// Modified: 2026-09-15 (W6 compatibility — held claims now assert Controlled attachment rather than treating keeper carry across the goal line as stale-shot travel)
// Modified: 2026-08-03
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.5.2 (ERR-011-008), Ball Physics #1 §3.1,
//           Testing Strategy & Framework #19 §3.3.1/§3.3.5/Appendix A.1, Code Standards #20
// Purpose:  Acceptance scenario for the conversion-at-contact pass
//           (gk-conversion-at-contact-design.md §5): a keeper CLAIM must end the incoming-ball threat.
//
//           #11 §3.5.2's catch branch is two statements — the possession record AND the velocity
//           park ("parked at hand position"). Only the first was implemented (ERR-011-008), and
//           possession in this engine was originally a FLAG, not a kinematic constraint: the ball
//           integrated independently and the goal check adjudicated on ball POSITION. So a claimed
//           shot kept its velocity and flew into the net — measured over three full matches, ball
//           speed 11.1 m/s in and 10.8 m/s out of a catch, with 7 of 10 catches followed by a goal
//           within 5 s.
//
//           W6 makes genuine possession a kinematic constraint. The scenario therefore keeps both
//           sides of the contract: a held ball must remain physically attached to the claiming
//           keeper, and the held claim must not itself end in an own goal. The latter is an outcome
//           guard, not a ban on Law-10 adjudication: if a held keeper is permitted to carry the
//           attached ball through his own goal plane, the production locomotion/carry path is wrong.

using System;

using TacticalDirector.BallPhysics;
using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Builds the keeper-claim acceptance scenario index (#19 §3.3.5 cross-spec layout; KD-8
    /// ownership). Tier B — claim structure over a bounded composed corpus.
    /// </summary>
    internal static class MatchEngineKeeperClaimScenarios
    {
        public const string KeeperClaimPath =
            TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX + "match-engine-keeper-claim";

        public const ulong KeeperClaimSeed = 0x5EED000000000003UL;

        /// <summary>
        /// FULL 90-minute matches, two seeds. A keeper CLAIM is a much rarer composed event than a
        /// contact — the post-fix instrument measured 11 claims across three full matches — so the
        /// 45-min windows the sibling contact scenario uses would thin this corpus to two or three
        /// samples, turning every predicate into a per-sample lottery (the §5.Z.21 / §5.Z.22 AR-4
        /// corpus-sizing lesson, twice learned).
        /// </summary>
        private const int NumTicks = 324000;

        private static readonly ulong[] Seeds =
        {
            KeeperClaimSeed,
            0x00000000D1A6D05EUL,
        };

        /// <summary>
        /// Ball speed (m/s) at or below which the ball counts as arrested at the claim tick.
        /// Post-fix the park writes exactly zero and the measured corpus reads 0.0; pre-fix a
        /// claimed ball carries its full shot speed (measured mean 10.8 m/s out against 11.1 in).
        /// 2.0 separates the two populations with an order of magnitude of margin on each side.
        /// </summary>
        private const float ArrestedSpeedMps = 2.0f;

        /// <summary>
        /// W6 attaches Controlled possession to the holder exactly in x/y. This tolerance is only a
        /// floating-point comparison guard for the acceptance probe; it is not a gameplay tuning dial.
        /// </summary>
        private const float ControlledAttachmentToleranceM = 0.001f;

        /// <summary>
        /// Hard cap on the attribution window after a claim (5 s at 60 Hz — the instrument's fate
        /// window). The window normally closes earlier, the moment the claiming keeper's possession
        /// ends: after that the ball is in open play and no longer tests the claim's held state.
        /// </summary>
        private const int ClaimWindowTicks = 300;

        // #11 (the claim), #1 (ball kinematics), #2 (keeper locomotion), #16 (determinism), #19.
        private static readonly int[] OwningSpecIds = { 1, 2, 11, 16, 19 };

        public static ScenarioIndex BuildIndex()
        {
            var manifest = new ScenarioManifest(
                name: "match-engine-keeper-claim",
                owningSpecIds: OwningSpecIds,
                seed: KeeperClaimSeed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);

            var scenario = new ClosedLoopScenario(manifest, RunKeeperClaim);

            return new ScenarioIndex(new[]
            {
                new ScenarioIndexEntry(KeeperClaimPath, manifest, scenario),
            });
        }

        private static void RunKeeperClaim(ScenarioContext context)
        {
            int claims = 0;
            int travellingAfterClaim = 0;
            int heldClaimsObserved = 0;
            int detachedWhileHeld = 0;
            int concededWhileHolding = 0;

            for (int s = 0; s < Seeds.Length; s++)
            {
                PlayOne(
                    Seeds[s],
                    ref claims,
                    ref travellingAfterClaim,
                    ref heldClaimsObserved,
                    ref detachedWhileHeld,
                    ref concededWhileHolding);
            }

            string inv(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // Non-vacuity: the corpus actually produced claims. Without this every predicate below
            // passes on an engine whose keepers never touch the ball (the §5.Z.15 lesson: the
            // "save quality" lever was undefined because there were zero hand contacts).
            context.Envelope.CheckTrue("claims-occur",
                claims >= 3,
                "claims=" + inv(claims));

            // The ERR-011-008 fix: a claimed ball is ARRESTED, not merely flagged. Pre-fix every
            // claim leaves the ball at shot speed.
            context.Envelope.CheckTrue("claimed-ball-is-arrested",
                travellingAfterClaim == 0,
                "travellingAfterClaim=" + inv(travellingAfterClaim) + " of " + inv(claims)
                + " (bound " + ArrestedSpeedMps.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)
                + " m/s)");

            // W6 structure lock: an observably held ball cannot travel independently of its holder.
            // Claims whose possession both begins and ends inside one RunTick have no held-state
            // observation boundary and are excluded; heldClaimsObserved keeps the predicate non-vacuous.
            context.Envelope.CheckTrue("claimed-ball-remains-attached-while-held",
                heldClaimsObserved >= 3 && detachedWhileHeld == 0,
                "heldClaimsObserved=" + inv(heldClaimsObserved)
                + " detachedWhileHeld=" + inv(detachedWhileHeld)
                + " claims=" + inv(claims));

            // Independent football consequence. ORDER in PlayOne matters: a goal restart clears
            // possession inside the scoring RunTick, so score changes are attributed against the
            // claim window as it stood entering that tick before holder-based window closure. W6's
            // pre-closure corpus measured 2 of 17 claims ending this way.
            context.Envelope.CheckTrue("held-claim-does-not-concede-own-goal",
                concededWhileHolding == 0,
                "concededWhileHolding=" + inv(concededWhileHolding) + " of " + inv(claims));
        }

        private static void PlayOne(
            ulong seed,
            ref int claims,
            ref int travellingAfterClaim,
            ref int heldClaimsObserved,
            ref int detachedWhileHeld,
            ref int concededWhileHolding)
        {
            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));

            int gkCount = GoalkeeperMechanics.GoalkeeperConstants.MaxGkAgents;
            var prevContactFrame = new int[gkCount];
            var claimTick = new int[gkCount];
            for (int t = 0; t < gkCount; t++)
            {
                prevContactFrame[t] = -1;
                claimTick[t] = int.MinValue;
            }

            var claimingAgent = new int[gkCount];
            for (int t = 0; t < gkCount; t++)
            {
                claimingAgent[t] = -1;
            }

            int prevHome = 0;
            int prevAway = 0;

            for (int tick = 0; tick < NumTicks; tick++)
            {
                engine.RunTick();

                GoalkeeperMechanics.GoalkeeperTickState gk = engine.TestOnly_GoalkeeperState;
                float ballSpeed = engine.BallView.Velocity.magnitude;
                int holder = engine.PossessingAgentId;

                // Read score changes BEFORE holder-based window closure. ApplyRestart clears the
                // possessor inside the same RunTick a goal is awarded; checking holder first would
                // make the consequence structurally unreachable on exactly the scoring tick.
                if (engine.HomeScore != prevHome)
                {
                    // Home scored => the away keeper (team/gk index 1) conceded.
                    if (claimingAgent[1] >= 0)
                    {
                        concededWhileHolding++;
                        claimingAgent[1] = -1;
                        claimTick[1] = int.MinValue;
                    }
                    prevHome = engine.HomeScore;
                }

                if (engine.AwayScore != prevAway)
                {
                    // Away scored => the home keeper (team/gk index 0) conceded.
                    if (claimingAgent[0] >= 0)
                    {
                        concededWhileHolding++;
                        claimingAgent[0] = -1;
                        claimTick[0] = int.MinValue;
                    }
                    prevAway = engine.AwayScore;
                }

                for (int t = 0; t < gkCount; t++)
                {
                    if (claimingAgent[t] < 0)
                    {
                        continue;
                    }

                    // The held-state window closes as soon as possession ends. While it remains
                    // open, W6's Controlled contract says the ball must stay physically attached
                    // to that keeper; independent travel would be the old ERR-011-008 shape again.
                    if (holder != claimingAgent[t] || tick - claimTick[t] > ClaimWindowTicks)
                    {
                        claimingAgent[t] = -1;
                        claimTick[t] = int.MinValue;
                        continue;
                    }

                    heldClaimsObserved++;
                    if (!IsControlledAtHolder(engine, claimingAgent[t]))
                    {
                        detachedWhileHeld++;
                        claimingAgent[t] = -1;   // one attribution per claim
                        claimTick[t] = int.MinValue;
                    }
                }

                for (int t = 0; t < gkCount; t++)
                {
                    int cf = gk.ContactStates[t].ActualContactFrame;
                    bool newContact = cf != prevContactFrame[t] && cf >= 0;
                    if (newContact)
                    {
                        prevContactFrame[t] = cf;
                    }

                    // A CLAIM is a contact resolved at or above the catch threshold — the §3.5.2
                    // catch branch, and the Stage-0 smother, which commits at exactly that value.
                    if (newContact
                        && gk.ContactStates[t].HandlingQualityScalar
                           >= GoalkeeperMechanics.GoalkeeperConstants.CatchThreshold)
                    {
                        claims++;
                        int agentId = GkAgentId(engine, t);

                        if (ballSpeed > ArrestedSpeedMps)
                        {
                            travellingAfterClaim++;
                        }

                        // The claim may already have ended later in this same RunTick (for example,
                        // a restart clears possession before the scenario can observe the tick). Such
                        // a claim has no held-state observation to test. Open the W6 attachment/outcome
                        // window only when the keeper is still the holder at this observation boundary.
                        if (agentId >= 0 && holder == agentId)
                        {
                            heldClaimsObserved++;
                            if (!IsControlledAtHolder(engine, agentId))
                            {
                                detachedWhileHeld++;
                                claimingAgent[t] = -1;
                                claimTick[t] = int.MinValue;
                            }
                            else
                            {
                                claimingAgent[t] = agentId;
                                claimTick[t] = tick;
                            }
                        }
                        else
                        {
                            claimingAgent[t] = -1;
                            claimTick[t] = int.MinValue;
                        }
                    }
                }
            }
        }

        private static bool IsControlledAtHolder(MatchEngine engine, int agentId)
        {
            if (engine.BallView.State != BallStateType.Controlled)
            {
                return false;
            }

            var holderPosition = engine.AgentView(agentId).Position;
            var ballPosition = engine.BallView.Position;
            float dx = ballPosition.x - holderPosition.x;
            float dy = ballPosition.y - holderPosition.y;
            return dx * dx + dy * dy
                   <= ControlledAttachmentToleranceM * ControlledAttachmentToleranceM;
        }

        /// <summary>The on-pitch goalkeeper agent for the given team, or -1.</summary>
        private static int GkAgentId(MatchEngine engine, int teamId)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (engine.AgentIsGoalkeeper(i) && engine.AgentTeamId(i) == teamId)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Position-coherent squad on the ConfigureSquads path — the
        /// MatchEngineKeeperContactScenarios recipe.</summary>
        private static Squad BuildSquad(ulong seed, int clubId)
        {
            var rng = new DeterministicRngService(seed ^ (ulong)clubId);
            int stream = rng.RegisterStream(
                "diagnostic.roster", SubsystemOrdinals.PlayerDatabase, entityId: clubId, streamVersion: 1);

            var template = new PlayerPosition[PlayerDatabaseConstants.CLUB_SQUAD_SIZE];
            int i = 0;
            for (int k = 0; k < 3; k++) template[i++] = PlayerPosition.Goalkeeper;
            for (int k = 0; k < 8; k++) template[i++] = PlayerPosition.Defender;
            for (int k = 0; k < 8; k++) template[i++] = PlayerPosition.Midfielder;
            while (i < template.Length) template[i++] = PlayerPosition.Forward;

            return RosterGenerator.Generate(rng, stream, clubId, template);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.3     | 2026-09-15 | —      | W6 review closure: retained Controlled attachment structure and     |
// |         |            |        | restored the held-claim own-goal consequence guard; score changes   |
// |         |            |        | are attributed before same-tick restart clears possession.          |
// | 1.2     | 2026-09-15 | —      | W6 probe observes attachment only at held-state tick boundaries;   |
// |         |            |        | same-tick claim+release/restart transitions are excluded and the   |
// |         |            |        | observed-held population is non-vacuity gated.                     |
// | 1.1     | 2026-09-15 | —      | W6 compatibility: predicate 3 now asserts Controlled attachment   |
// |         |            |        | while held; keeper-carried goal-line crossing is no longer        |
// |         |            |        | misclassified as independent stale-shot travel.                   |
// | 1.0     | 2026-08-03 | —      | Initial. Conversion-at-contact acceptance (ERR-011-008): a claim   |
// |         |            |        | arrests the ball, and a claiming keeper does not concede from the  |
// |         |            |        | claim. Full-match windows because a claim is rarer than a contact. |
// |         |            |        | No save-percentage or goal-rate pin.                               |
#endregion
