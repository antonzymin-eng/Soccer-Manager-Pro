// File:     src/match-engine/tests/MatchEngineKeeperClaimScenarios.cs
// Created:  2026-08-03
// Modified: 2026-08-03
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.5.2 (ERR-011-008), Ball Physics #1 §3.1,
//           Testing Strategy & Framework #19 §3.3.1/§3.3.5/Appendix A.1, Code Standards #20
// Purpose:  Acceptance scenario for the conversion-at-contact pass
//           (gk-conversion-at-contact-design.md §5): a keeper CLAIM must end the threat.
//
//           #11 §3.5.2's catch branch is two statements — the possession record AND the velocity
//           park ("parked at hand position"). Only the first was implemented (ERR-011-008), and
//           possession in this engine is a FLAG, not a kinematic constraint: the ball integrates
//           unconditionally and the goal check adjudicates on ball POSITION. So a claimed shot kept
//           its velocity and flew into the net — measured over three full matches, ball speed
//           11.1 m/s in and 10.8 m/s out of a catch, with 7 of 10 catches followed by a goal
//           within 5 s.
//
//           The predicates assert the STRUCTURE that makes a claim a claim (the ball stops; the
//           claiming keeper does not then concede from it), never a save percentage or a goal rate.

using System;

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
        /// Hard cap on the attribution window after a claim (5 s at 60 Hz — the instrument's fate
        /// window). The window normally closes earlier, the moment the claiming keeper's possession
        /// ends: after that the ball is in open play and a goal is no longer that claim's doing.
        /// <para>Attribution is deliberately narrow. A broader "any goal at this end within 5 s"
        /// rule measured 3 of 7 on the FIXED engine — the keeper claims, the release rule drops the
        /// ball loose in his own box, and the scramble produces a goal. That is a creation-side
        /// observation (§7), not a failure of the claim, and pinning it here would make the
        /// predicate fail for a reason it does not name.</para>
        /// </summary>
        private const int ClaimGoalWindowTicks = 300;

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
            int concededFromClaim = 0;

            for (int s = 0; s < Seeds.Length; s++)
            {
                PlayOne(Seeds[s], ref claims, ref travellingAfterClaim, ref concededFromClaim);
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

            // The consequence that makes it matter: the ball a keeper is RECORDED AS HOLDING does
            // not end up in his own net. Pre-fix that is precisely what happened — the possession
            // flag was set on a ball still travelling at shot speed, so the keeper "had" the ball
            // as it crossed his own goal line.
            context.Envelope.CheckTrue("held-ball-does-not-enter-own-net",
                concededFromClaim == 0,
                "concededWhileHolding=" + inv(concededFromClaim) + " of " + inv(claims));
        }

        private static void PlayOne(
            ulong seed, ref int claims, ref int travellingAfterClaim, ref int concededFromClaim)
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

            int prevHome = 0;
            int prevAway = 0;
            var claimingAgent = new int[gkCount];
            for (int t = 0; t < gkCount; t++)
            {
                claimingAgent[t] = -1;
            }

            for (int tick = 0; tick < NumTicks; tick++)
            {
                engine.RunTick();

                GoalkeeperMechanics.GoalkeeperTickState gk = engine.TestOnly_GoalkeeperState;
                float ballSpeed = engine.BallView.Velocity.magnitude;
                int holder = engine.PossessingAgentId;

                // ORDER MATTERS. The score check runs FIRST, against the window as it stood coming
                // into this tick. A goal awards through ApplyRestart, which CLEARS possession
                // inside the same RunTick — so closing the window on `holder != claimingAgent`
                // before reading the score would close it on the very tick the goal lands, making
                // this predicate structurally unreachable (verified: it read 0 pre-fix, on an
                // engine where 6 of 6 claims left the ball travelling).
                if (engine.HomeScore != prevHome)
                {
                    // Home scored ⇒ the AWAY keeper (slot 1) conceded.
                    if (claimingAgent[1] >= 0)
                    {
                        concededFromClaim++;
                        claimingAgent[1] = -1;   // one attribution per claim
                    }
                    prevHome = engine.HomeScore;
                }

                if (engine.AwayScore != prevAway)
                {
                    if (claimingAgent[0] >= 0)
                    {
                        concededFromClaim++;
                        claimingAgent[0] = -1;
                    }
                    prevAway = engine.AwayScore;
                }

                for (int t = 0; t < gkCount; t++)
                {
                    // The attribution window closes the moment the claiming keeper stops holding
                    // the ball — from then on the ball is in open play, not in his hands.
                    if (claimingAgent[t] >= 0
                        && (holder != claimingAgent[t] || tick - claimTick[t] > ClaimGoalWindowTicks))
                    {
                        claimingAgent[t] = -1;
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
                        claimTick[t] = tick;
                        claimingAgent[t] = GkAgentId(engine, t);
                        if (ballSpeed > ArrestedSpeedMps)
                        {
                            travellingAfterClaim++;
                        }
                    }
                }

            }
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
// | 1.0     | 2026-08-03 | —      | Initial. Conversion-at-contact acceptance (ERR-011-008): a claim   |
// |         |            |        | arrests the ball, and a claiming keeper does not concede from the  |
// |         |            |        | claim. Full-match windows because a claim is rarer than a contact. |
// |         |            |        | No save-percentage or goal-rate pin.                               |
#endregion
