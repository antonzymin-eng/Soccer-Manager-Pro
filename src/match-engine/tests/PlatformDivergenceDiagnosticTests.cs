// File:     src/match-engine/tests/PlatformDivergenceDiagnosticTests.cs
// Created:  2026-10-11
// Modified: 2026-10-11
// Author:   —
// Spec:     Deterministic Simulation #16 (cross-platform determinism); Positioning AI #12 §3.0
//           (ERR-012-011, the measured phase); match-engine-wiring-backlog.md §3 C1;
//           Testing Strategy & Framework #19; Code Standards #20
// Purpose:  Answers one question for docs/tracking/open-issues.md ("sim_match_engine_inposs_gate fails
//           on the governing build"): is the Unity-vs-Linux gap in the C1 final-third possession-phase
//           share a property of one seed, or systematic? On seed 0x0F1E2D3C4B5A6978 the Unity editor
//           measures 0.361 and the Linux shim 0.928 at the same commit.
//
//           It runs the gate's own match loop (MatchEngineInPossGateScenarios.PlayOne, so the share is
//           that measurement and not a copy) over the six frozen seeds of
//           docs/tracking/w2-six-seed-evidence-preregistration.md and writes, per platform:
//             <prefix>-shares.tsv          seed, samples, homeShare, awayShare (+ exact float bits);
//             <prefix>-trace-<seed>.tsv    a bitwise state fingerprint: every tick for the first
//                                          FineTraceTicks ticks, then every CheckpointStrideTicks.
//           Diffing two platforms' traces gives the first tick at which their worlds differ, which
//           separates "the platforms play different matches from the start" from "they drift late".
//
//           Diagnostic, not a lock: it asserts nothing about engine behaviour. Env-gated because one
//           run is six full matches (about 20 min on the .NET shim, about an hour in the Unity editor).
//
//   TD_PLATFORM_DIVERGENCE_OUT=<dir> [TD_PLATFORM_DIVERGENCE_PREFIX=<name>] \
//     [TD_PLATFORM_DIVERGENCE_TICKS=<n>] dotnet test --filter PlatformDivergenceDiagnostic
//
//           TD_PLATFORM_DIVERGENCE_TICKS caps the ticks per seed (default the gate's full 324,000).
//           A capped run is for locating the first divergent tick only; its shares are not the gate's.

using System;
using System.Globalization;
using System.IO;
using System.Text;

using NUnit.Framework;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    internal sealed class PlatformDivergenceDiagnosticTests
    {
        /// <summary>The six frozen seeds, in the preregistration's order (w2-six-seed-evidence-preregistration.md).</summary>
        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
            0x5EED000000000003UL,
            0x5EED000000000004UL,
            0x00000000D1A6D05FUL,
            0x1A2B3C4D5E6F7081UL,
        };

        /// <summary>Ticks fingerprinted individually from kickoff (one minute at 60 Hz).</summary>
        private const int FineTraceTicks = 3600;

        /// <summary>Fingerprint stride after the fine window (ten seconds at 60 Hz).</summary>
        private const int CheckpointStrideTicks = 600;

        [Test]
        [Category("Calibration")]
        [Timeout(7200000)] // six full matches run ~1 h in the editor; Unity's default is 180 s
        public void PlatformDivergenceDiagnostic_SixSeedSharesAndStateTrace()
        {
            string outDir = Environment.GetEnvironmentVariable("TD_PLATFORM_DIVERGENCE_OUT");
            if (string.IsNullOrEmpty(outDir))
            {
                Assert.Ignore(
                    "Set TD_PLATFORM_DIVERGENCE_OUT=<dir> to run the six-seed platform-divergence diagnostic.");
            }

            string prefix = Environment.GetEnvironmentVariable("TD_PLATFORM_DIVERGENCE_PREFIX");
            if (string.IsNullOrEmpty(prefix)) prefix = "platform";

            int numTicks = MatchEngineInPossGateScenarios.NumTicks;
            string ticksOverride = Environment.GetEnvironmentVariable("TD_PLATFORM_DIVERGENCE_TICKS");
            if (!string.IsNullOrEmpty(ticksOverride))
            {
                numTicks = int.Parse(ticksOverride, CultureInfo.InvariantCulture);
            }

            Directory.CreateDirectory(outDir);

            var shares = new StringBuilder();
            shares.Append("seed\tticks\tsamples\thomeShare\tawayShare\thomeShareBits\tawayShareBits\tsquadHome\tsquadAway\n");

            for (int s = 0; s < Seeds.Length; s++)
            {
                ulong seed = Seeds[s];
                string seedLabel = "0x" + seed.ToString("X16", CultureInfo.InvariantCulture);

                uint squadHome = SquadFingerprint(MatchEngineInPossGateScenarios.BuildSquad(seed, clubId: 1));
                uint squadAway = SquadFingerprint(MatchEngineInPossGateScenarios.BuildSquad(seed, clubId: 2));

                var trace = new StringBuilder();
                trace.Append("tick\tball\tagents\n");

                int samples = 0;
                int homeView = 0;
                int awayView = 0;
                MatchEngineInPossGateScenarios.PlayOne(
                    seed,
                    numTicks,
                    (tick, engine) =>
                    {
                        if (tick >= FineTraceTicks && tick % CheckpointStrideTicks != 0) return;
                        AppendFingerprint(trace, tick, engine);
                    },
                    ref samples, ref homeView, ref awayView);

                File.WriteAllText(
                    Path.Combine(outDir, prefix + "-trace-" + seedLabel + ".tsv"), trace.ToString());

                float homeShare = samples > 0 ? (float)homeView / samples : 0f;
                float awayShare = samples > 0 ? (float)awayView / samples : 0f;
                string line =
                    seedLabel + "\t" +
                    numTicks.ToString(CultureInfo.InvariantCulture) + "\t" +
                    samples.ToString(CultureInfo.InvariantCulture) + "\t" +
                    homeShare.ToString("F6", CultureInfo.InvariantCulture) + "\t" +
                    awayShare.ToString("F6", CultureInfo.InvariantCulture) + "\t" +
                    Hex(BitConverter.SingleToInt32Bits(homeShare)) + "\t" +
                    Hex(BitConverter.SingleToInt32Bits(awayShare)) + "\t" +
                    Hex(unchecked((int)squadHome)) + "\t" +
                    Hex(unchecked((int)squadAway));
                shares.Append(line).Append('\n');

                // Rewritten after every seed so a long editor run that is stopped part-way keeps what it has.
                File.WriteAllText(Path.Combine(outDir, prefix + "-shares.tsv"), shares.ToString());
                UnityEngine.Debug.Log("PLATFORM_DIVERGENCE_SHARE " + line.Replace('\t', ' '));
            }

            Assert.Pass("Diagnostic only — see " + Path.Combine(outDir, prefix + "-shares.tsv"));
        }

        /// <summary>
        /// One line per fingerprinted tick: the ball's position, velocity and state as raw float bits,
        /// then one FNV-1a hash per roster index over that agent's position, velocity, facing,
        /// movement state and stamina pools. Raw bits, so a one-ulp difference shows.
        /// </summary>
        private static void AppendFingerprint(StringBuilder sb, int tick, MatchEngine engine)
        {
            BallState ball = engine.BallView;
            sb.Append(tick.ToString(CultureInfo.InvariantCulture)).Append('\t');
            sb.Append(Hex(BitConverter.SingleToInt32Bits(ball.Position.x)))
              .Append(Hex(BitConverter.SingleToInt32Bits(ball.Position.y)))
              .Append(Hex(BitConverter.SingleToInt32Bits(ball.Position.z)))
              .Append(Hex(BitConverter.SingleToInt32Bits(ball.Velocity.x)))
              .Append(Hex(BitConverter.SingleToInt32Bits(ball.Velocity.y)))
              .Append(Hex(BitConverter.SingleToInt32Bits(ball.Velocity.z)))
              .Append(((int)ball.State).ToString(CultureInfo.InvariantCulture))
              .Append('\t');

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                AgentState a = engine.AgentView(i);
                uint h = FnvOffset;
                h = Mix(h, a.Position.x);
                h = Mix(h, a.Position.y);
                h = Mix(h, a.Velocity.x);
                h = Mix(h, a.Velocity.y);
                h = Mix(h, a.FacingDirection.x);
                h = Mix(h, a.FacingDirection.y);
                h = Mix(h, (int)a.CurrentState);
                h = Mix(h, a.AerobicPool);
                h = Mix(h, a.SprintReservoir);
                if (i > 0) sb.Append(',');
                sb.Append(Hex(unchecked((int)h)));
            }

            sb.Append('\n');
        }

        /// <summary>Order-sensitive hash of every generated roster field the engine can read.</summary>
        private static uint SquadFingerprint(Squad squad)
        {
            uint h = FnvOffset;
            for (int i = 0; i < squad.Count; i++)
            {
                PlayerRecord p = squad.GetPlayer(i);
                h = Mix(h, p.PlayerId);
                h = Mix(h, p.Age);
                h = Mix(h, (int)p.Position);
                h = MixString(h, p.FirstName);
                h = MixString(h, p.LastName);

                PlayerDatabase.PlayerAttributes t = p.Attributes;
                int[] attributes =
                {
                    t.Pace, t.Acceleration, t.Agility, t.Balance, t.Strength, t.Stamina,
                    t.Passing, t.Technique, t.Finishing, t.LongShots, t.Dribbling, t.Crossing,
                    t.Heading, t.Decisions, t.Vision, t.Composure, t.Anticipation, t.WorkRate,
                    t.Aggression, t.Positioning, t.Reflexes, t.Handling, t.Aerial, t.OneVsOne,
                    t.Throwing, t.Kicking, t.Tackling, t.Marking, t.Concentration, t.Teamwork,
                    t.FirstTouchAbility, t.WeakFootRating,
                };
                for (int k = 0; k < attributes.Length; k++) h = Mix(h, attributes[k]);
            }

            return h;
        }

        private const uint FnvOffset = 2166136261u;
        private const uint FnvPrime = 16777619u;

        private static uint Mix(uint h, float value) => Mix(h, BitConverter.SingleToInt32Bits(value));

        private static uint Mix(uint h, int value)
        {
            unchecked
            {
                uint v = (uint)value;
                for (int b = 0; b < 4; b++)
                {
                    h ^= (v >> (8 * b)) & 0xFFu;
                    h *= FnvPrime;
                }
            }

            return h;
        }

        private static uint MixString(uint h, string value)
        {
            if (value == null) return Mix(h, -1);
            for (int i = 0; i < value.Length; i++) h = Mix(h, value[i]);
            return Mix(h, value.Length);
        }

        private static string Hex(int bits) => bits.ToString("X8", CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-11 | —      | Initial: env-gated six-seed share report plus per-tick bitwise |
// |         |            |        |   state fingerprints, to tell whether the Unity/Linux C1 gap   |
// |         |            |        |   is one seed or systematic and where the platforms diverge.   |
#endregion
