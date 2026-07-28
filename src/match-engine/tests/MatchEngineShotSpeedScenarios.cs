// File:     src/match-engine/tests/MatchEngineShotSpeedScenarios.cs
// Created:  2026-07-28
// Modified: 2026-07-28
// Modified: 2026-07-28 (shot-volume pass: + mean-shot-distance-reaches-football-band predicate, ERR-008-017)
// Modified: 2026-07-28 (keeper-contact pass: strike-time sampling via TestOnly_LastShotStrike*; 18 min/seed windows)
// Author:   —
// Spec:     Shot speed & physical woodwork design (docs/tracking/shot-speed-woodwork-design.md) §5;
//           Match Engine design note §5.Z; Ball Physics #1 §3.1.10 (ERR-001-005);
//           Shot Mechanics #6 §3.2 (ERR-006-004); Decision Tree #8 §3.5.3 (ERR-008-016);
//           Testing Strategy & Framework #19 §3.3.1 / §3.3.3 / §3.3.5 / Appendix A.1 / KD-8;
//           Code Standards #20
// Purpose:  The acceptance scenario for the shot-speed + woodwork pass.
//
//           The §5.Z.18 residual measured shot-tick speeds of 7–10 m/s means against football's
//           ~25 — the composed result of #8's PowerIntent pinning at its own 0.1 clamp floor and
//           #6's VFloor anchoring a neutral full-power shot at ~16 m/s before reducers. At those
//           speeds the §5.Z.18 crossbar is nearly inert. Once speeds rise, the goal frame needs
//           to be PHYSICAL: at 25 m/s the ball moves ~0.42 m per tick, so post/crossbar strikes
//           need a swept segment test (a discrete per-tick position test tunnels through a 0.12 m
//           post) and goal-line adjudication needs the interpolated crossing point (the detected
//           position is up to ~0.42 m past the plane).
//
//           These predicates assert REACHABILITY of the classes the pre-fix engine cannot produce:
//             * shot-tick speeds at football pace (loose floors the measured 7–10/15–19 band
//               provably cannot reach — not a pinned calibration band, the §5.Z.17 rule);
//             * a post strike REBOUNDING into play instead of adjudicating as an exit;
//             * a crossbar strike rebounding likewise;
//             * a rising ball whose segment crosses the out-plane UNDER the bar scored as a goal
//               even though its detected position is already over the bar (isolates the
//               crossing-point adjudication alone);
//             * two-run digest determinism (sequential engines — the §5.Z.7 property).

using System;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Builds the shot-speed / woodwork acceptance scenario index (#19 §3.3.5 cross-spec layout;
    /// KD-8 ownership). Tier B — composed rare events plus scripted frame probes.
    /// </summary>
    internal static class MatchEngineShotSpeedScenarios
    {
        public const string ShotSpeedPath =
            TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX + "match-engine-shot-speed";

        public const ulong ShotSpeedSeed = 0x0005107705107706UL;

        /// <summary>64 800 ticks @ 60 Hz = 18 minutes per seed, two seeds. Resized 9 → 18 min
        /// with the keeper-contact pass (the shot-outcome scenario's AR-4 precedent): the
        /// keeper-contact behaviour change thinned the 9-minute windows to ~3 strikes total,
        /// which made the distance MEAN a per-sample lottery (one wide-angle strike moved it
        /// several metres). 18-minute windows measure ~11 strikes across the corpus.</summary>
        private const int NumTicks = 64800;

        private static readonly ulong[] Seeds =
        {
            ShotSpeedSeed,
            0x0F1E2D3C4B5A6978UL,
        };

        /// <summary>Mean shot-tick speed floor (m/s) across each natural window. Pre-fix means
        /// measure 6.9–10.3 over full matches — a 14 floor is unreachable pre-fix while sitting
        /// well under the calibrated post-fix band (a reachability floor, not a band).</summary>
        private const float ShotSpeedMeanFloorMps = 14.0f;

        /// <summary>Max shot-tick speed floor (m/s) across the corpus. Pre-fix maxima measure
        /// 15.3–18.9; football-pace strikes clear 20 comfortably post-fix.</summary>
        private const float ShotSpeedMaxFloorMps = 20.0f;

        /// <summary>Mean shot-distance ceiling (m) across the corpus — the shot-volume pass's
        /// discriminator (ERR-008-017). Pre-fix means measure 30–34 m over full matches (shots
        /// cluster at the range-gate boundary); football's mean is ~17 m. A 24 m ceiling is
        /// unreachable pre-fix while leaving room over the calibrated post-fix band
        /// (a reachability ceiling, not a pinned calibration band — the §5.Z.17 rule).</summary>
        private const float ShotDistanceMeanCeilingM = 24.0f;

        // Frame-probe geometry (attack +X; away goal line at x = PITCH_LENGTH_M, mouth centre
        // y = PITCH_WIDTH_M / 2). Post axes sit OUTWARD of the 7.32 m inner-edge box by half the
        // post diameter; the crossbar axis sits ABOVE the 2.44 m lower edge by the same half.
        private static readonly float GoalLineX = MatchEngineConstants.PITCH_LENGTH_M;
        private static readonly float CentreY = MatchEngineConstants.PITCH_WIDTH_M * 0.5f;
        private static readonly float PostAxisY = CentreY
            + BallPhysics.BallPhysicsConstants.Pitch.GOAL_WIDTH * 0.5f
            + BallPhysics.BallPhysicsConstants.Pitch.POST_DIAMETER * 0.5f;
        private static readonly float BarAxisZ = BallPhysics.BallPhysicsConstants.Pitch.GOAL_HEIGHT
            + BallPhysics.BallPhysicsConstants.Pitch.POST_DIAMETER * 0.5f;

        // Owning specs: the speed runs through #8 (PowerIntent) and #6 (velocity assembly), the
        // frame through #1 (swept collision + crossing-point adjudication), composed by the engine
        // under #16 determinism on the #19 harness.
        private static readonly int[] OwningSpecIds = { 1, 6, 8, 16, 19 };

        public static ScenarioIndex BuildIndex()
        {
            var manifest = new ScenarioManifest(
                name: "match-engine-shot-speed",
                owningSpecIds: OwningSpecIds,
                seed: ShotSpeedSeed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);

            var scenario = new ClosedLoopScenario(manifest, RunShotSpeed);

            return new ScenarioIndex(new[]
            {
                new ScenarioIndexEntry(ShotSpeedPath, manifest, scenario),
            });
        }

        private static void RunShotSpeed(ScenarioContext context)
        {
            // ── 1. Natural-play speed floors + distance ceiling ─────────────────────────────────
            int totalShots = 0;
            int samples = 0;
            float speedSum = 0f;
            float speedMax = 0f;
            float distSum = 0f;
            foreach (ulong seed in Seeds)
            {
                PlayOne(seed, ref totalShots, ref samples, ref speedSum, ref speedMax, ref distSum);
            }

            context.Envelope.CheckTrue("shots-are-taken", totalShots > 0, Inv(totalShots));

            float mean = samples > 0 ? speedSum / samples : 0f;
            context.Envelope.CheckTrue("shot-speed-mean-reaches-football-band",
                mean >= ShotSpeedMeanFloorMps,
                "mean=" + InvF(mean) + " floor=" + InvF(ShotSpeedMeanFloorMps));
            context.Envelope.CheckTrue("shot-speed-max-reaches-football-band",
                speedMax >= ShotSpeedMaxFloorMps,
                "max=" + InvF(speedMax) + " floor=" + InvF(ShotSpeedMaxFloorMps));

            // Shot-volume pass (ERR-008-017): without a distance term in U_SHOOT, shots cluster
            // at the RANGE-GATE boundary (measured mean 30–34 m over full matches vs football's
            // ~17). Distance is the discriminator, not count — over a 9-minute window the count
            // is noisy but the pre/post mean-distance gap is order-one (shot-volume-design KD-V5).
            float distMean = samples > 0 ? distSum / samples : float.MaxValue;
            context.Envelope.CheckTrue("mean-shot-distance-reaches-football-band",
                distMean <= ShotDistanceMeanCeilingM,
                "distMean=" + InvF(distMean) + " ceiling=" + InvF(ShotDistanceMeanCeilingM));

            // ── 2. Post strike rebounds into play (front face, dead on the post axis) ───────────
            // Contact at x = GoalLineX − (POST_DIAMETER/2 + Ball.RADIUS) ≈ −0.17 m before the
            // line; pre-fix nothing is physical there, so the ball flies on, wholly crosses at
            // y = PostAxisY (3.72 m off centre — OUTSIDE the 3.66 m inner-edge box) and
            // adjudicates as a goal-kick exit. Post-fix it rebounds off the post and stays in
            // play with no restart and no score.
            var post = new MatchEngine(ShotSpeedSeed);
            SetBallInFlight(post, new UnityEngine.Vector3(GoalLineX - 0.5f, PostAxisY, 1.2f),
                new UnityEngine.Vector3(25f, 0f, 0f));
            (bool postRestarted, _) = TickAndWatch(post, ticks: 5);
            context.Envelope.CheckTrue("post-strike-rebounds-into-play",
                !postRestarted && post.HomeScore == 0 && post.AwayScore == 0
                && post.BallView.Position.x < GoalLineX
                && post.BallView.Velocity.x < 0f,
                DescribeProbe(post, postRestarted));

            // ── 3. Crossbar strike rebounds into play (front face, dead on the bar axis) ────────
            // Ball centre level with the bar axis (z = 2.50): pre-fix the crossing adjudicates
            // OVER the bar (z ≥ 2.44) ⇒ goal-kick exit; post-fix the swept test meets the bar at
            // the same ≈ 0.17 m stand-off and reflects the ball straight back out.
            var bar = new MatchEngine(ShotSpeedSeed);
            SetBallInFlight(bar, new UnityEngine.Vector3(GoalLineX - 0.5f, CentreY, BarAxisZ),
                new UnityEngine.Vector3(25f, 0f, 0f));
            (bool barRestarted, _) = TickAndWatch(bar, ticks: 5);
            context.Envelope.CheckTrue("crossbar-strike-rebounds-into-play",
                !barRestarted && bar.HomeScore == 0 && bar.AwayScore == 0
                && bar.BallView.Position.x < GoalLineX
                && bar.BallView.Velocity.x < 0f,
                DescribeProbe(bar, barRestarted));

            // ── 4. Rising crossing adjudicated at the crossing point ────────────────────────────
            // Segment (105.05, 34, 2.25) + (25, 0, 20)·dt: crosses the out-plane
            // (x = L + Ball.RADIUS) at z ≈ 2.30 (under the bar ⇒ goal) but is DETECTED at
            // z ≈ 2.58 (over). The trajectory's closest approach to the bar axis is ≈ 0.23 m
            // (> the 0.17 m combined radius), so the swept frame test does not fire and the
            // predicate isolates the crossing-point adjudication alone. Pre-fix: the overshot
            // position reads over-the-bar ⇒ goal kick, no goal. Post-fix: goal, home + 1.
            var rising = new MatchEngine(ShotSpeedSeed);
            SetBallInFlight(rising, new UnityEngine.Vector3(GoalLineX + 0.05f, CentreY, 2.25f),
                new UnityEngine.Vector3(25f, 0f, 20f));
            rising.RunTick();
            context.Envelope.CheckTrue("rising-crossing-adjudicated-at-the-crossing",
                rising.HomeScore == 1,
                "score=" + Inv(rising.HomeScore) + "-" + Inv(rising.AwayScore)
                + " cue=" + rising.RestartAppliedThisTick.ToString());

            // ── 5. Two-run digest determinism (sequential — the §5.Z.7 property) ────────────────
            var chain = new byte[3600][];
            var a = new MatchEngine(ShotSpeedSeed);
            for (int tick = 0; tick < chain.Length; tick++)
            {
                a.RunTick();
                chain[tick] = a.CurrentSnapshotDigest;
            }
            var b = new MatchEngine(ShotSpeedSeed);
            bool identical = true;
            for (int tick = 0; tick < chain.Length && identical; tick++)
            {
                b.RunTick();
                byte[] expected = chain[tick];
                byte[] actual = b.CurrentSnapshotDigest;
                if (expected.Length != actual.Length)
                {
                    identical = false;
                    break;
                }
                for (int i = 0; i < expected.Length; i++)
                {
                    if (expected[i] != actual[i]) { identical = false; break; }
                }
            }
            context.Envelope.CheckTrue("two-run-digest-determinism", identical,
                identical ? "digest chains identical over 3600 ticks" : "digest divergence");
        }

        private static void PlayOne(ulong seed, ref int shots, ref int samples, ref float speedSum,
            ref float speedMax, ref float distSum)
        {
            // The ConfigureSquads path — the SAME distribution the ShotOutcomeDiagnosticTests
            // instrument measures and the [GT] values were calibrated against (the neutral all-10
            // path samples a different shot population; an early draft measured it and the floors
            // did not transfer). Recipe mirrors the diagnostic's BuildSquad.
            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));

            // Shots counted via the genuine #6-strike counter, NOT ShotDetectedTickMs edges: the
            // gk-catch-parry-conversion pass (ERR-011-006) made the detection stamp ALSO seed at
            // save-episode onset (rebounds, deflections, slow threats ≥ 3 m/s), so a stamp edge
            // stopped meaning "a shot was struck" — sampling those edges would fold slow threat
            // episodes into the speed distribution this scenario's floors pin.
            int prevContacts = 0;

            for (int tick = 0; tick < NumTicks; tick++)
            {
                engine.RunTick();

                int contacts = engine.TestOnly_ShotContacts;
                if (contacts != prevContacts)
                {
                    shots += contacts - prevContacts;
                    prevContacts = contacts;
                    samples++;

                    // STRIKE-TIME ball state via the TestOnly_LastShotStrike* seam — captured
                    // beside the _shotContacts increment, post-ApplyKick and before any later
                    // same-tick Resolve step. The earlier end-of-tick BallView sampling broke
                    // under the keeper-contact pass: a defender or keeper within first-touch
                    // reach of the strike redirects the ball in the SAME Resolve tick, which
                    // diluted the speed sample (the mean floor survived by 0.08) and flipped
                    // the vx-sign goal attribution (a measured 13 m strike read as 92.3 m).
                    UnityEngine.Vector3 strikeVel = engine.TestOnly_LastShotStrikeVelocity;
                    float speed = strikeVel.magnitude;
                    speedSum += speed;
                    if (speed > speedMax) speedMax = speed;

                    // Distance from the strike point to the ATTACKED goal — the strike
                    // velocity's x-sign names the goal exactly (the aim point sits on the
                    // attacked goal plane; both teams shoot, and measuring against a fixed
                    // goal would read a shot toward x = 0 as ~70+ m and corrupt the mean).
                    UnityEngine.Vector3 shotPos = engine.TestOnly_LastShotStrikePosition;
                    float goalX = strikeVel.x >= 0f
                        ? MatchEngineConstants.PITCH_LENGTH_M : 0f;
                    float dy = shotPos.y - CentreY;
                    float dx = shotPos.x - goalX;
                    distSum += (float)Math.Sqrt(dx * dx + dy * dy);
                }
            }
        }

        /// <summary>Position-coherent squad on the ConfigureSquads path — the
        /// ShotOutcomeDiagnosticTests recipe (a position template so LineupSelector can field a
        /// back four).</summary>
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

        /// <summary>Places a moving airborne ball via the TestOnly seam (the shot-outcome probe
        /// precedent, extended with velocity — the frame probes need a flight, not a placement).</summary>
        private static void SetBallInFlight(
            MatchEngine engine, UnityEngine.Vector3 position, UnityEngine.Vector3 velocity)
        {
            BallPhysics.BallState state = BallPhysics.BallState.CreateAtPosition(position);
            state.Velocity = velocity;
            state.State = BallPhysics.BallStateType.Airborne;
            engine.TestOnly_SetBall(in state);
        }

        /// <summary>Ticks the engine, reporting whether ANY out-of-play restart was applied.</summary>
        private static (bool restarted, RestartCue lastCue) TickAndWatch(MatchEngine engine, int ticks)
        {
            bool restarted = false;
            RestartCue lastCue = RestartCue.None;
            for (int i = 0; i < ticks; i++)
            {
                engine.RunTick();
                RestartCue cue = engine.RestartAppliedThisTick;
                if (cue != RestartCue.None)
                {
                    restarted = true;
                    lastCue = cue;
                }
            }
            return (restarted, lastCue);
        }

        private static string DescribeProbe(MatchEngine engine, bool restarted)
        {
            return "restarted=" + (restarted ? "true" : "false")
                + " score=" + Inv(engine.HomeScore) + "-" + Inv(engine.AwayScore)
                + " cue=" + engine.RestartAppliedThisTick.ToString()
                + " ballX=" + InvF(engine.BallView.Position.x)
                + " ballVx=" + InvF(engine.BallView.Velocity.x);
        }

        private static string Inv(int v) =>
            v.ToString(System.Globalization.CultureInfo.InvariantCulture);

        private static string InvF(float v) =>
            v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-28 | —      | Initial. Shot-speed reachability floors (pre-fix 7–10/15–19 band   |
// |         |            |        | cannot reach them), front-face post + crossbar rebound probes, the |
// |         |            |        | rising crossing-point adjudication probe, sequential determinism.  |
// | 1.1     | 2026-07-28 | —      | gk-catch-parry-conversion: PlayOne counts shots via                |
// |         |            |        | TestOnly_ShotContacts (genuine #6 strikes) instead of              |
// |         |            |        | ShotDetectedTickMs edges — the ERR-011-006 arming stamps also fire |
// |         |            |        | for slow threat episodes (≥ 3 m/s), which would have polluted the  |
// |         |            |        | sampled speed distribution and broken the floor predicates.        |
// | 1.2     | 2026-07-28 | —      | Shot-volume pass (ERR-008-017): + mean-shot-distance ceiling       |
// |         |            |        | predicate (24 m — pre-fix means 30–34 m are unreachable; the       |
// |         |            |        | attacked goal named by the contact-tick vx sign, since measuring   |
// |         |            |        | against a fixed goal corrupts the mean for the other team's shots).|
// | 1.3     | 2026-07-28 | —      | Keeper-contact pass: sampling moved to the strike-TIME             |
// |         |            |        | TestOnly_LastShotStrike* seam and the window resized 9 → 18        |
// |         |            |        | min/seed. The end-of-tick BallView sampling broke once the keeper  |
// |         |            |        | actually contacts: a same-tick touch after the strike reversed the |
// |         |            |        | sampled velocity (a measured 13 m strike read as 92.3 m, mean      |
// |         |            |        | 51.8) and diluted the speed mean to 0.08 above its floor; and the  |
// |         |            |        | thinned 9-min windows held 3 strikes, a per-sample lottery. The    |
// |         |            |        | predicates and their bounds are UNCHANGED — pre-fix shots cluster  |
// |         |            |        | at the range gate (measured 30–34 m mean), which the 24 m ceiling  |
// |         |            |        | still refuses, and the pre-fix 7–10 m/s speed band still cannot    |
// |         |            |        | reach the floors. Measured post-fix: 11 strikes, distMean 22.7,    |
// |         |            |        | speedMean 21.9, max 24.9.                                          |
#endregion
