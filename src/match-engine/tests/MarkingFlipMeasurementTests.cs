// File:     src/match-engine/tests/MarkingFlipMeasurementTests.cs
// Created:  2026-11-21
// Modified: 2026-11-21
// Author:   —
// Spec:     W13 plan §10 (post-wiring measurement, reduced), Defensive AI #14 §3.13.1, Agent Movement #2 §3.5.3.1, Code Standards #20
// Purpose:  Env-gated, descriptive full-match measurement of the W13 default flip: the same seed / case played with mark
//           movement OFF and ON, one CSV row per (seed, case, arm). Asserts only hard stops (finite state, determinism of
//           the per-match digest is checked by the gate elsewhere); every football metric is descriptive.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

using NUnit.Framework;

using TacticalDirector.AgentMovement;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// W13 default-flip measurement (descriptive). Selected by environment so it never runs in the gate:
    /// <c>TD_MARKING_MEASURE_OUT</c> (output CSV path, required), <c>TD_MARKING_MEASURE_SEEDS</c> (comma-separated hex seeds;
    /// default: the historical six), <c>TD_MARKING_MEASURE_CASES</c> (comma-separated #434 case ids 0-9; default 0),
    /// <c>TD_MARKING_MEASURE_ARMS</c> (<c>off,on</c> by default), <c>TD_MARKING_MEASURE_TICKS</c> (default a full match).
    /// Cases are the #434 characterization matrix: C0 both F442; C1 / C2 F433 home / away; C3 / C4 F4231 home / away;
    /// C5 / C6 all-Defend home / away; C7 / C8 all-Attack home / away; C9 both F4231. GK heading is enabled, as in that harness.
    /// </summary>
    [TestFixture]
    internal sealed partial class MarkingFlipMeasurementTests
    {
        private static readonly ulong[] DefaultSeeds =
        {
            0x0123456789ABCDEFUL, 0x1111111111111111UL, 0x2222222222222222UL,
            0x3333333333333333UL, 0xDEADBEEFCAFEBABEUL, 0x0F0F0F0F0F0F0F0FUL,
        };

        private static TeamTactic Tactic(TacticFormation f, MarkingScheme scheme) => new TeamTactic(
            Mentality.Balanced, f, Tempo.Standard, TacticWidth.Standard, TacticPassing.Mixed, TacticPressing.Medium,
            LineOfEngagement.Standard, 0.5f, TacticDefWidth.Standard, TransitionPlan.HoldShape, TransitionPlan.Regroup, false,
            TacticTriggerMask.None, FocusPlay.Mixed, GkDistributionPolicy.SlowDown, 0, markingScheme: scheme);

        [Test]
        [Category("Calibration")]
        public void MarkingFlip_FullMatchMeasurement()
        {
            string output = Environment.GetEnvironmentVariable("TD_MARKING_MEASURE_OUT");
            if (string.IsNullOrEmpty(output))
                Assert.Ignore("Set TD_MARKING_MEASURE_OUT to run the W13 flip measurement.");
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            ulong[] seeds = ParseSeeds(Environment.GetEnvironmentVariable("TD_MARKING_MEASURE_SEEDS"));
            int[] cases = ParseInts(Environment.GetEnvironmentVariable("TD_MARKING_MEASURE_CASES"), new[] { 0 });
            string armsEnv = Environment.GetEnvironmentVariable("TD_MARKING_MEASURE_ARMS");
            string[] arms = string.IsNullOrEmpty(armsEnv) ? new[] { "off", "on" } : armsEnv.Split(',');
            string ticksEnv = Environment.GetEnvironmentVariable("TD_MARKING_MEASURE_TICKS");
            int ticks = string.IsNullOrEmpty(ticksEnv) ? (int)MatchEngineConstants.MATCH_TICKS_TOTAL : int.Parse(ticksEnv, CultureInfo.InvariantCulture);

            bool header = !File.Exists(output);
            using var w = new StreamWriter(output, append: true);
            if (header)
                w.WriteLine("seed,case,arm,ticks,goals0,goals1,shots0,shots1,passes0,passes1,poss0,poss1,tackleWon,tackleLoose,tackleFoul,tackleMissed,yellows,sentOff,markAgentStrides,groundedAgentTicks,guardLockEntries,nonFinite,wallSeconds,digest,groundedEntries,groundedCollision,groundedSlide,groundedOther,groundedMarker,groundedDefending,groundedAttacking,groundedLoose");

            foreach (ulong seed in seeds)
                foreach (int c in cases)
                    foreach (string arm in arms)
                    {
                        string row = RunOne(seed, c, arm == "on", ticks);
                        w.WriteLine(row);
                        w.Flush();
                        TestContext.Progress.WriteLine(row);
                    }

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            Assert.Pass("Descriptive measurement — see " + output);
        }

        private static string RunOne(ulong seed, int c, bool armed, int ticks)
        {
            var e = new MatchEngine(seed);
            e.EnableGkHeading();
            e.TestOnly_ArmMarkMovement(armed);
            SetCase(e, c);

            int n = MatchEngineConstants.SQUAD_SIZE;
            int[] shots = new int[2], passes = new int[2], poss = new int[2];
            bool[] shotBusy = new bool[n], passBusy = new bool[n], wasLocked = new bool[n];
            long markAgentStrides = 0, groundedAgentTicks = 0, lockEntries = 0, nonFinite = 0;
            // Knockdown anatomy: each transition INTO GROUNDED, by reason, by whether the agent held a mark target at the last
            // stride, and by his team's possession state at that tick (defending / attacking / ball loose).
            long gEntries = 0, gCollision = 0, gSlide = 0, gOther = 0, gMarker = 0, gDef = 0, gAtt = 0, gLoose = 0;
            bool[] wasGrounded = new bool[n], hadMark = new bool[n];
            var sw = Stopwatch.StartNew();
            for (int tick = 0; tick < ticks; tick++)
            {
                e.RunTick();
                int holder = e.PossessingAgentId;
                if (holder >= 0) poss[e.AgentTeamId(holder)]++;
                bool stride = e.CurrentTick % (ulong)TacticalDirector.DeterministicSim.DeterministicSimConstants.AI_PHASE_STRIDE == 0;
                for (int a = 0; a < n; a++)
                {
                    bool sIdle = e.TestOnly_ShotExecutorIdle(a);
                    if (!sIdle && !shotBusy[a]) shots[e.AgentTeamId(a)]++;
                    shotBusy[a] = !sIdle;
                    bool pIdle = e.TestOnly_PassExecutorIdle(a);
                    if (!pIdle && !passBusy[a]) passes[e.AgentTeamId(a)]++;
                    passBusy[a] = !pIdle;

                    AgentState s = e.TestOnly_AgentSnapshot(a);
                    bool grounded = s.CurrentState == AgentMovementState.GROUNDED;
                    if (grounded) groundedAgentTicks++;
                    if (grounded && !wasGrounded[a])
                    {
                        gEntries++;
                        if (s.GroundedReason == GroundedReason.COLLISION) gCollision++;
                        else if (s.GroundedReason == GroundedReason.SLIDING_TACKLE) gSlide++;
                        else gOther++;
                        if (hadMark[a]) gMarker++;
                        if (holder < 0) gLoose++;
                        else if (e.AgentTeamId(holder) == e.AgentTeamId(a)) gAtt++;
                        else gDef++;
                    }
                    wasGrounded[a] = grounded;
                    bool locked = s.OscillationGuard.GetState().IsLocked;
                    if (locked && !wasLocked[a]) lockEntries++;
                    wasLocked[a] = locked;
                    if (!float.IsFinite(s.Position.x) || !float.IsFinite(s.Position.y) || !float.IsFinite(s.Velocity.x) || !float.IsFinite(s.Velocity.y))
                        nonFinite++;
                    if (stride)
                    {
                        hadMark[a] = e.TestOnly_MarkTarget(a).HasTarget;
                        if (hadMark[a]) markAgentStrides++;
                    }
                }
            }
            sw.Stop();
            (int won, int loose, int foul, int missed) = e.TestOnly_TackleOutcomeCounts;
            int yellows = 0, sentOff = 0;
            for (int a = 0; a < n; a++) { yellows += e.TestOnly_YellowCards(a); if (e.AgentIsSentOff(a)) sentOff++; }
            string digest = Convert.ToHexString(e.CurrentSnapshotDigest);
            Assert.AreEqual(0, nonFinite, $"hard stop: non-finite agent state (seed {seed:X16} case {c} armed {armed})");
            return string.Join(",",
                seed.ToString("X16", CultureInfo.InvariantCulture), c.ToString(CultureInfo.InvariantCulture), armed ? "on" : "off",
                ticks.ToString(CultureInfo.InvariantCulture),
                e.HomeScore.ToString(CultureInfo.InvariantCulture), e.AwayScore.ToString(CultureInfo.InvariantCulture),
                shots[0].ToString(CultureInfo.InvariantCulture), shots[1].ToString(CultureInfo.InvariantCulture),
                passes[0].ToString(CultureInfo.InvariantCulture), passes[1].ToString(CultureInfo.InvariantCulture),
                poss[0].ToString(CultureInfo.InvariantCulture), poss[1].ToString(CultureInfo.InvariantCulture),
                won.ToString(CultureInfo.InvariantCulture), loose.ToString(CultureInfo.InvariantCulture),
                foul.ToString(CultureInfo.InvariantCulture), missed.ToString(CultureInfo.InvariantCulture),
                yellows.ToString(CultureInfo.InvariantCulture), sentOff.ToString(CultureInfo.InvariantCulture),
                markAgentStrides.ToString(CultureInfo.InvariantCulture), groundedAgentTicks.ToString(CultureInfo.InvariantCulture),
                lockEntries.ToString(CultureInfo.InvariantCulture), nonFinite.ToString(CultureInfo.InvariantCulture),
                sw.Elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture), digest,
                gEntries.ToString(CultureInfo.InvariantCulture), gCollision.ToString(CultureInfo.InvariantCulture),
                gSlide.ToString(CultureInfo.InvariantCulture), gOther.ToString(CultureInfo.InvariantCulture),
                gMarker.ToString(CultureInfo.InvariantCulture), gDef.ToString(CultureInfo.InvariantCulture),
                gAtt.ToString(CultureInfo.InvariantCulture), gLoose.ToString(CultureInfo.InvariantCulture));
        }

        private static void SetCase(MatchEngine e, int c, MarkingScheme scheme = MarkingScheme.Mixed, bool tight = false)
        {
            int[] families = { c == 1 ? 1 : c == 3 || c == 9 ? 2 : 0, c == 2 ? 1 : c == 4 || c == 9 ? 2 : 0 };
            for (int t = 0; t < 2; t++)
                e.SetTeamTactic(t, Tactic((TacticFormation)families[t], scheme));

            int dutyTeam = c is >= 5 and <= 8 ? (c == 5 || c == 7 ? 0 : 1) : -1;
            var instructions = tight
                ? new PlayerInstructions(InstrBias.Default, InstrBias.Default, InstrBias.Default,
                    InstrBias.Default, InstrBias.Default, InstrBias.Default, true, -1, SetPieceDutyFlags.None)
                : PlayerInstructions.Default;
            for (int t = 0; t < 2; t++)
            {
                if (!tight && t != dutyTeam)
                    continue;
                Duty duty = t == dutyTeam ? (c <= 6 ? Duty.Defend : Duty.Attack) : Duty.Support;
                for (int slot = 1; slot < 11; slot++)
                    e.SetPlayerTactic(t * 11 + slot, new PlayerTactic(PlayerRole.Default, duty, instructions));
            }
        }

        private static ulong[] ParseSeeds(string s)
        {
            if (string.IsNullOrEmpty(s)) return DefaultSeeds;
            string[] parts = s.Split(',');
            var r = new ulong[parts.Length];
            for (int i = 0; i < parts.Length; i++) r[i] = ulong.Parse(parts[i].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return r;
        }

        private static int[] ParseInts(string s, int[] dflt)
        {
            if (string.IsNullOrEmpty(s)) return dflt;
            string[] parts = s.Split(',');
            var r = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++) r[i] = int.Parse(parts[i].Trim(), CultureInfo.InvariantCulture);
            return r;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-11-21 | —      | W13 default-flip measurement instrument (env-gated, descriptive; off vs on per seed / #434 case). |
// | 1.1     | 2026-11-21 | —      | Knockdown anatomy columns: GROUNDED entries by reason, marker / defending / attacking / loose. |
#endregion
