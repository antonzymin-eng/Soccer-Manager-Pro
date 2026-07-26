// File:     src/match-engine/tests/MatchEnginePlayDevelopmentScenarios.cs
// Created:  2026-07-26
// Modified: 2026-07-26
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5.Z Phase H (acceptance),
//           Testing Strategy & Framework #19 §3.3.1 / §3.3.3 / §3.3.5 / Appendix A.1 / KD-8,
//           Code Standards #20
// Purpose:  The §5.Z.5 acceptance scenario for ERR-030-014: over a multi-minute composed run the ball is
//           actually KICKED, is POSSESSED for a non-trivial share of ticks, play is STILL ALIVE at the
//           final tick, and across a spread of seeds the ball reaches BOTH penalty areas and a non-zero
//           scoreline is produced.
//
//           This is the test the tree did not have. The Phase F capstone asserts tick count, AI-stride
//           cadence, finiteness, on-pitch bounds and digest-chain advance — every one of which holds for
//           a match in which nothing whatsoever happens, which is exactly why a 90-minute 0–0 deadlock
//           survived 321 match-engine tests. These predicates are chosen so that the pre-Phase-H engine
//           fails EVERY one of them: it verified that the composition runs; this verifies that it plays.

using System;

using TacticalDirector.BallPhysics;
using TacticalDirector.TestingStrategy;

using UnityEngine;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Builds the §5.Z Phase H acceptance scenario index (#19 §3.3.5 cross-spec/ layout; KD-8 ownership).
    /// Tier B (Simulation layer) — it deliberately costs real composed match time, which is what makes it
    /// able to observe the failure mode it exists for.
    /// </summary>
    internal static class MatchEnginePlayDevelopmentScenarios
    {
        public const string PlayDevelopsPath =
            TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX + "match-engine-play-develops";

        public const ulong PlayDevelopsSeed = 0x0F1E2D3C4B5A6978UL;

        /// <summary>
        /// 32 400 ticks @ 60 Hz = 9 minutes of composed match time per seed, six seeds — about 1.5 minutes
        /// of wall clock in total on the Linux gate. A short run cannot do this job: the two residual
        /// deadlocks found while landing Phase H both let play run for MINUTES before dying (one at ~9 min,
        /// one at ~8 min), so the run must be long enough that "it started" and "it kept going" are
        /// distinguishable. That is what <c>play-still-alive-at-final-tick</c> below measures.
        /// </summary>
        private const int NumTicks = 32400;

        /// <summary>
        /// The seed spread. Per-seed predicates hold for EVERY seed; the scoreline and penalty-area
        /// predicates are asserted across the SET, per §5.Z.5's own wording ("a non-zero scoreline across
        /// a spread of seeds"). A single seed cannot carry those: with the current provisional balance
        /// only some nine-minute runs produce a goal, and which ones is a property of the tuning, not of
        /// the possession bootstrap this scenario exists to lock.
        /// </summary>
        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
            0x0000000000000001UL,
            0x00000000ABCDEF12UL,
            0x0000000099887766UL,
            0x000000005A5A5A5AUL,
        };

        // Owning specs: the possession loop runs through all of them. Ball Physics (#1) + Agent Movement
        // (#2); Collision (#3) + First Touch (#4) + Pass (#5) / Shot (#6) executors; Perception (#7) +
        // Decision Tree (#8) — whose ERR-008-014 loose-ball collect is half the fix — and the
        // Positioning/Pressing/Defensive/Attacking mechanics (#12–#15); Deterministic Sim (#16) drives the
        // tick; Event System (#17) carries the possession-changed event; Testing Strategy (#19) owns the
        // harness.
        private static readonly int[] OwningSpecIds =
            { 1, 2, 3, 4, 5, 6, 7, 8, 12, 13, 14, 15, 16, 17, 19 };

        // ── Thresholds ────────────────────────────────────────────────────────────────
        // Every threshold is set well below the measured value so the scenario locks the BEHAVIOUR
        // (play develops and sustains) rather than pinning today's numbers — which are provisional
        // pending the balance pass. Measured values across the six seeds are quoted per constant.

        /// <summary>A kicked ball. Measured max planar speed: 16.2–17.2 m/s. Pre-Phase-H: 0.00.</summary>
        private const float MinPeakBallSpeedMps = 5.0f;

        /// <summary>A ball played into the air at some point. Measured max height 2.45–2.91 m.
        /// Pre-Phase-H: 0.11 m, exactly the resting centre height — the ball never left the ground.</summary>
        private const float MinPeakBallHeightM = 0.5f;

        /// <summary>Share of ticks with a possessing agent. Measured 10.5–20.9%. Pre-Phase-H: 0%.</summary>
        private const float MinPossessedTickFraction = 0.05f;

        /// <summary>Distinct possession transitions. Measured 262–298. Pre-Phase-H: 0.</summary>
        private const int MinPossessionChanges = 50;

        /// <summary>
        /// Play must still be alive at the end, not merely have happened early. Measured: the last
        /// possession change lands at 96.7–99.9% of the run and the ball is still moving at 98.7–100%.
        /// These two are the predicates that catch the failure mode the landing hit TWICE — an engine
        /// that plays for several minutes and then falls back into the deadlock is not fixed, and every
        /// other predicate here would still pass for it.
        /// </summary>
        private const float MinLastChangeFraction = 0.75f;
        private const float MinLastMovingFraction = 0.90f;

        /// <summary>Penalty-area depth (Laws of the Game: 16.5 m from each goal line).</summary>
        private const float PenaltyAreaDepthM = 16.5f;

        public static ScenarioIndex BuildIndex()
        {
            var manifest = new ScenarioManifest(
                "match-engine-play-develops",
                owningSpecIds: OwningSpecIds,
                seed: PlayDevelopsSeed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);
            return new ScenarioIndex(new[]
            {
                new ScenarioIndexEntry(
                    PlayDevelopsPath,
                    manifest,
                    new ClosedLoopScenario(manifest, PlayDevelops)),
            });
        }

        private static void PlayDevelops(ScenarioContext context)
        {
            bool anySeedScored      = false;
            bool anySeedReachedHome = false;
            bool anySeedReachedAway = false;

            for (int s = 0; s < Seeds.Length; s++)
            {
                Observations o = RunMatch(Seeds[s], NumTicks);
                string tag = "seed" + s;

                // The ball is kicked. This alone falsifies the ERR-030-014 engine, in which the ball's
                // velocity was identically zero for an entire 90 minutes.
                context.Envelope.CheckTrue(
                    tag + "-ball-is-kicked", o.PeakBallSpeed >= MinPeakBallSpeedMps,
                    "peak ball speed " + o.PeakBallSpeed.ToString("F2", Inv) + " m/s < " +
                    MinPeakBallSpeedMps.ToString("F2", Inv));

                context.Envelope.CheckTrue(
                    tag + "-ball-goes-airborne", o.PeakBallHeight >= MinPeakBallHeightM,
                    "peak ball height " + o.PeakBallHeight.ToString("F2", Inv) + " m < " +
                    MinPeakBallHeightM.ToString("F2", Inv));

                // Possession is genuinely held, and genuinely contested.
                float possessedFraction = (float)o.PossessedTicks / NumTicks;
                context.Envelope.CheckTrue(
                    tag + "-possession-is-held", possessedFraction >= MinPossessedTickFraction,
                    "possessed-tick fraction " + possessedFraction.ToString("F3", Inv) + " < " +
                    MinPossessedTickFraction.ToString("F3", Inv));

                context.Envelope.CheckTrue(
                    tag + "-possession-changes-hands", o.PossessionChanges >= MinPossessionChanges,
                    "possession changed " + o.PossessionChanges + " times, expected >= " + MinPossessionChanges);

                // Play is still alive at the final whistle — the regression guard for "runs, then dies".
                context.Envelope.CheckTrue(
                    tag + "-play-still-alive-at-final-tick",
                    o.LastChangeTick >= (int)(NumTicks * MinLastChangeFraction)
                    && o.LastMovingTick >= (int)(NumTicks * MinLastMovingFraction),
                    "play stalled: last possession change at tick " + o.LastChangeTick +
                    ", ball last moving at tick " + o.LastMovingTick + " of " + NumTicks);

                anySeedScored      |= (o.HomeGoals + o.AwayGoals) > 0;
                anySeedReachedHome |= o.ReachedHomePenaltyArea;
                anySeedReachedAway |= o.ReachedAwayPenaltyArea;
            }

            // Across the spread: play reaches BOTH ends of the pitch and produces goals. Asserted over the
            // seed set rather than per seed — see the Seeds doc comment.
            context.Envelope.CheckTrue(
                "reaches-home-penalty-area", anySeedReachedHome,
                "no seed worked the ball into the home penalty area");
            context.Envelope.CheckTrue(
                "reaches-away-penalty-area", anySeedReachedAway,
                "no seed worked the ball into the away penalty area");
            context.Envelope.CheckTrue(
                "produces-a-non-zero-scoreline", anySeedScored,
                "no seed produced a goal — every match finished 0-0 (the ERR-030-014 signature)");

            // Determinism over a run in which play actually HAPPENS. The Phase F capstone already matches
            // two 600-tick digest chains, but 600 ticks of a pre-Phase-H engine were 600 ticks of nothing:
            // that check could not observe the possession loop, the executors, or a goal. This one covers
            // a horizon (~1.7 min) that reliably contains kicks, receptions and possession turnovers.
            // Sequential, never interleaved — the EventBus is process-static (#17 §3.2.1).
            byte[][] chainA = RunDigestChain(PlayDevelopsSeed, DeterminismTicks);
            byte[][] chainB = RunDigestChain(PlayDevelopsSeed, DeterminismTicks);
            int divergeTick = -1;
            for (int t = 0; t < DeterminismTicks; t++)
            {
                if (!BytesEqual(chainA[t], chainB[t]))
                {
                    divergeTick = t + 1;
                    break;
                }
            }
            context.Envelope.CheckTrue(
                "determinism-two-run-digest-match-over-live-play", divergeTick < 0,
                "the snapshot digest chain diverged between two same-seed runs at tick " + divergeTick);
        }

        /// <summary>~1.7 minutes — long enough to contain real possession turnovers and kicks.</summary>
        private const int DeterminismTicks = 6000;

        private static byte[][] RunDigestChain(ulong seed, int ticks)
        {
            var engine = new MatchEngine(seed);
            var chain = new byte[ticks][];
            for (int t = 0; t < ticks; t++)
            {
                engine.RunTick();
                chain[t] = (byte[])engine.CurrentSnapshotDigest.Clone();
            }
            return chain;
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static readonly System.Globalization.CultureInfo Inv =
            System.Globalization.CultureInfo.InvariantCulture;

        private static Observations RunMatch(ulong seed, int ticks)
        {
            var engine = new MatchEngine(seed);
            var o = new Observations { LastChangeTick = -1, LastMovingTick = -1 };
            int previousHolder = int.MinValue;

            for (int t = 1; t <= ticks; t++)
            {
                engine.RunTick();

                BallState ball = engine.TestOnly_BallSnapshot;
                float planarSpeed = new Vector2(ball.Velocity.x, ball.Velocity.y).magnitude;
                if (planarSpeed > o.PeakBallSpeed)
                {
                    o.PeakBallSpeed = planarSpeed;
                }
                if (ball.Position.z > o.PeakBallHeight)
                {
                    o.PeakBallHeight = ball.Position.z;
                }
                if (planarSpeed >= MatchEngineConstants.FIRST_TOUCH_MIN_BALL_SPEED_M_S)
                {
                    o.LastMovingTick = t;
                }

                if (ball.Position.x <= PenaltyAreaDepthM)
                {
                    o.ReachedHomePenaltyArea = true;
                }
                if (ball.Position.x >= MatchEngineConstants.PITCH_LENGTH_M - PenaltyAreaDepthM)
                {
                    o.ReachedAwayPenaltyArea = true;
                }

                int holder = engine.TestOnly_PossessingAgentId;
                if (holder >= 0)
                {
                    o.PossessedTicks++;
                }
                if (holder != previousHolder)
                {
                    o.PossessionChanges++;
                    o.LastChangeTick = t;
                    previousHolder = holder;
                }
            }

            o.HomeGoals = engine.TestOnly_Goals(0);
            o.AwayGoals = engine.TestOnly_Goals(1);
            return o;
        }

        private sealed class Observations
        {
            public float PeakBallSpeed;
            public float PeakBallHeight;
            public int   PossessedTicks;
            public int   PossessionChanges;
            public int   LastChangeTick;
            public int   LastMovingTick;
            public bool  ReachedHomePenaltyArea;
            public bool  ReachedAwayPenaltyArea;
            public int   HomeGoals;
            public int   AwayGoals;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial implementation — the §5.Z.5 Phase H acceptance scenario for  |
// |         |            |        | ERR-030-014: the ball is kicked, possession is held and contested,   |
// |         |            |        | play is still alive at the final tick, and across a seed spread the  |
// |         |            |        | ball reaches both penalty areas and goals are scored.               |
#endregion
