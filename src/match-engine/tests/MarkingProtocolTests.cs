// File:     src/match-engine/tests/MarkingProtocolTests.cs
// Created:  2026-11-23
// Modified: 2026-11-23 (Unity digest formatting and compression enum)
// Author:   —
// Spec:     W13 wiring plan §3 and §10
// Purpose:  Opt-in full-match capture for the marking measurement protocol.

using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

using NUnit.Framework;

using TacticalDirector.AgentMovement;
using TacticalDirector.DecisionTree;
using TacticalDirector.DefensiveAI;
using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;
using TacticalDirector.PressingAI;
using TacticalDirector.ProjectConstants;
using TacticalDirector.ShotMechanics;
using TacticalDirector.TacticalInstructions;
using UnityEngine;

namespace TacticalDirector.MatchEngine
{
    internal sealed partial class MarkingFlipMeasurementTests
    {
        private static readonly ulong[] ProtocolSeeds =
        {
            0x1UL, 0x5A5A5A5AUL, 0x99887766UL, 0xABCDEF12UL, 0xD1A6D05EUL, 0x0F1E2D3C4B5A6978UL,
        };

        [Test]
        [Category("Calibration")]
        public void MarkingProtocol_Capture()
        {
            string root = Environment.GetEnvironmentVariable("TD_MARKING_PROTOCOL_OUT");
            if (string.IsNullOrWhiteSpace(root))
                Assert.Ignore("Set TD_MARKING_PROTOCOL_OUT to capture W13 §10.");
            string config = Environment.GetEnvironmentVariable("TD_MARKING_PROTOCOL_CONFIG");
            if (!string.IsNullOrEmpty(config))
                GameplayConfigHolder.Bind(GameplayConfigFileLoader.Parse(File.ReadAllText(config)));
            CaptureProtocolMatrix(root);
        }

        // Keep the first catalogue reference after the optional config binding.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void CaptureProtocolMatrix(string root)
        {
            string seedInput = Environment.GetEnvironmentVariable("TD_MARKING_MEASURE_SEEDS");
            ulong[] seeds = string.IsNullOrEmpty(seedInput) ? ProtocolSeeds : ParseSeeds(seedInput);
            bool armed = MatchEngineConstants.MarkMovementEnabled;
            int[] cases = ParseInts(Environment.GetEnvironmentVariable("TD_MARKING_MEASURE_CASES"),
                armed ? new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 } : new[] { 0 });
            string ticksInput = Environment.GetEnvironmentVariable("TD_MARKING_MEASURE_TICKS");
            int ticks = string.IsNullOrEmpty(ticksInput) ? (int)MatchEngineConstants.MATCH_TICKS_TOTAL
                : int.Parse(ticksInput, CultureInfo.InvariantCulture);
            bool tight = string.Equals(Environment.GetEnvironmentVariable("TD_MARKING_PROTOCOL_TIGHT"), "true",
                StringComparison.OrdinalIgnoreCase);
            string schemeInput = Environment.GetEnvironmentVariable("TD_MARKING_PROTOCOL_SCHEME");
            MarkingScheme scheme = string.IsNullOrEmpty(schemeInput)
                ? MarkingScheme.Mixed : Enum.Parse<MarkingScheme>(schemeInput);
            Assert.That(Enum.IsDefined(typeof(MarkingScheme), scheme), Is.True);
            bool previousLogSetting = UnityEngine.TestTools.LogAssert.ignoreFailingMessages;
            try
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
                foreach (ulong seed in seeds)
                    foreach (int c in cases)
                        CaptureProtocol(root, seed, c, ticks, armed, scheme, tight);
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = previousLogSetting;
            }
        }

        private static MatchEngine ProtocolEngine(ulong seed, int c, MarkingScheme scheme, bool tight)
        {
            var engine = new MatchEngine(seed);
            engine.EnableGkHeading();
            SetCase(engine, c, scheme, tight);
            return engine;
        }

        private static BinaryWriter ProtocolStream(string directory, string name) =>
            new BinaryWriter(new GZipStream(new FileStream(Path.Combine(directory, name + ".bin.gz"),
                FileMode.CreateNew), System.IO.Compression.CompressionLevel.Fastest));

        private static void CaptureProtocol(string root, ulong seed, int c, int ticks, bool armed, MarkingScheme scheme, bool tight)
        {
            Assert.That(c, Is.InRange(0, 9));
            Assert.That(ticks, Is.InRange(1, (int)MatchEngineConstants.MATCH_TICKS_TOTAL));
            string directory = Path.Combine(root, armed ? "on" : "off", $"C{c}", seed.ToString("X16"));
            if (Directory.Exists(directory))
                Assert.Fail($"A completed capture already exists at {directory}.");
            string attempt = Path.Combine(root, ".attempts", $"{(armed ? "on" : "off")}-C{c}-{seed:X16}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(attempt);
            CaptureProtocolAttempt(attempt, seed, c, ticks, armed, scheme, tight);
            Directory.CreateDirectory(Path.GetDirectoryName(directory));
            Directory.Move(attempt, directory);
        }

        private static void CaptureProtocolAttempt(string directory, ulong seed, int c, int ticks, bool armed, MarkingScheme scheme, bool tight)
        {
            using var samples = ProtocolStream(directory, "samples");
            using var marking = ProtocolStream(directory, "marking");
            using var witness = ProtocolStream(directory, "witness");
            using var events = new StreamWriter(Path.Combine(directory, "events.csv"));
            events.WriteLine("frame,event,team,agent,detail,nearest,gk_nearest,goal_side,gk_goal_side");
            using var digests = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            MatchEngine engine = ProtocolEngine(seed, c, scheme, tight);
            SnapshotHeader checkpointHeader = null;
            SnapshotPayload checkpointPayload = null;
            using var continuation = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            int[] shots = new int[2], goals = new int[2], fouls = new int[2], offsides = new int[2], intents = new int[2];
            int[] capDemotions = new int[2], groundedEntries = new int[2], collisionKnockdowns = new int[2];
            int[] slidingTackleEntries = new int[2], divingHeaderEntries = new int[2], otherGroundedEntries = new int[2];
            bool[] groundedBefore = new bool[MatchEngineConstants.SQUAD_SIZE];
            int sampleCount = 0, markerPresses = 0, nonCarrierPresses = 0;
            for (int frame = 1; frame <= ticks; frame++)
            {
                engine.RunTick();
                byte[] digest = engine.CurrentSnapshotDigest;
                digests.AppendData(digest);
                if (frame > ticks / 2)
                    continuation.AppendData(digest);
                if (frame == ticks / 2)
                {
                    checkpointHeader = engine.CaptureCommittedSnapshotHeader();
                    checkpointPayload = engine.CaptureCommittedSnapshotPayload();
                }

                int holder = engine.PossessingAgentId;
                int receiver = engine.TestOnly_PassInFlightReceiverId;
                int team = holder >= 0 ? engine.AgentTeamId(holder) : receiver >= 0 ? engine.AgentTeamId(receiver) : -1;
                Vector3 ball = engine.BallView.Position;
                RequireFinite(ball.x, ball.y, frame);
                RequireFinite(ball.z, engine.BallView.Velocity.z, frame);
                RequireFinite(engine.BallView.Velocity.x, engine.BallView.Velocity.y, frame);
                witness.Write((sbyte)team);
                witness.Write(ball.x);
                for (int a = 0; a < MatchEngineConstants.SQUAD_SIZE; a++)
                {
                    var state = engine.TestOnly_AgentSnapshot(a);
                    RequireFinite(state.Position.x, state.Position.y, frame);
                    RequireFinite(state.Velocity.x, state.Velocity.y, frame);
                    bool grounded = state.CurrentState == AgentMovementState.GROUNDED;
                    if (grounded && !groundedBefore[a])
                    {
                        int agentTeam = engine.AgentTeamId(a);
                        groundedEntries[agentTeam]++;
                        switch (state.GroundedReason)
                        {
                            case GroundedReason.COLLISION: collisionKnockdowns[agentTeam]++; break;
                            case GroundedReason.SLIDING_TACKLE: slidingTackleEntries[agentTeam]++; break;
                            case GroundedReason.DIVING_HEADER: divingHeaderEntries[agentTeam]++; break;
                            default: otherGroundedEntries[agentTeam]++; break;
                        }
                    }
                    groundedBefore[a] = grounded;
                }

                WriteProtocolEvents(engine, events, frame, shots, goals, fouls, offsides);
                if (frame % DeterministicSimConstants.AI_PHASE_STRIDE != 0 || engine.MatchEnded)
                    continue;

                sampleCount++;
                samples.Write(frame);
                int inactiveMask = 0, goalkeeperMask = 0;
                for (int a = 0; a < MatchEngineConstants.SQUAD_SIZE; a++)
                {
                    if (engine.AgentIsSentOff(a)) inactiveMask |= 1 << a;
                    if (engine.AgentIsGoalkeeper(a)) goalkeeperMask |= 1 << a;
                }
                samples.Write(inactiveMask);
                samples.Write(goalkeeperMask);

                for (int t = 0; t < 2; t++)
                {
                    if (!engine.TestOnly_MarkInvariantsHold(t))
                        Assert.Fail($"Assignment invariant breach at frame {frame}, team {t}.");
                    intents[t] += engine.TestOnly_TackleIntents(t).Length;
                    capDemotions[t] += engine.TestOnly_ManMarkCapDemotions(t);
                }

                for (int a = 0; a < MatchEngineConstants.SQUAD_SIZE; a++)
                {
                    Vector2 position = engine.AgentView(a).Position;
                    Vector2 slot = engine.TestOnly_FormationSlot(a);
                    RequireFinite(slot.x, slot.y, frame);
                    samples.Write(position.x);
                    samples.Write(position.y);
                    samples.Write(slot.x);
                    samples.Write(slot.y);

                    var assignment = engine.TestOnly_MarkAssignment(a);
                    var mark = engine.TestOnly_MarkTarget(a);
                    marking.Write((byte)assignment.Mode);
                    marking.Write(checked((sbyte)assignment.TargetEntityId));
                    marking.Write((byte)(mark.HasTarget ? 1 : 0));
                    marking.Write(mark.Target.x);
                    marking.Write(mark.Target.y);
                    if (!mark.HasTarget)
                        continue;
                    int agentTeam = engine.AgentTeamId(a);
                    RequireFinite(mark.Target.x, mark.Target.y, frame);
                    var possession = engine.TestOnly_MarkPossession(agentTeam);
                    if (!armed || engine.AgentIsGoalkeeper(a) || engine.AgentIsSentOff(a)
                        || possession.Possession || engine.TestOnly_PressRole(a) != PressRole.HoldShape
                        || assignment.Mode == MarkMode.Zonal)
                        Assert.Fail($"Ineligible mark target at frame {frame}, agent {a}.");
                    AgentAction action = engine.TestOnly_DtLastAction(a);
                    if (action.Type == ActionType.PRESS)
                    {
                        markerPresses++;
                        if (action.TargetAgentId != possession.Carrier) nonCarrierPresses++;
                    }
                }
            }

            // EventBus is shared. Finish the original match before constructing the restored engine.
            if (checkpointHeader != null)
            {
                MatchEngine restored = MatchEngine.RestoreFromSnapshot(checkpointHeader, checkpointPayload, seed);
                Assert.That(RunDigests(restored, ticks - ticks / 2), Is.EqualTo(continuation.GetHashAndReset()),
                    "Restore must reproduce every digest from half time to full time.");
            }
            Assert.That(nonCarrierPresses, Is.Zero, "Marker dispatched PRESS on a non-carrier.");
            Assert.That(goals, Is.EqualTo(new[] { engine.HomeScore, engine.AwayScore }));
            Assert.That(shots[0] + shots[1], Is.EqualTo(engine.TestOnly_ShotContacts),
                "The ledger must account for every engine-observed shot contact.");
            byte[] allTicks = digests.GetHashAndReset();
            string terminal = DigestHex(engine.CurrentSnapshotDigest);
            Assert.That(RunDigests(ProtocolEngine(seed, c, scheme, tight), ticks), Is.EqualTo(allTicks),
                "A second run from kickoff must reproduce every capture digest.");
            File.WriteAllLines(Path.Combine(directory, "summary.txt"), new[]
            {
                $"runtime={Environment.Version}",
                $"buildHash={MatchEngineBuildIdentity.BuildHash}",
                $"schema={MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION}",
                $"frames={ticks}",
                $"fullTime={engine.MatchEnded}",
                $"scheme={scheme}",
                $"tight={tight}",
                FormattableString.Invariant($"standoffs={DefensiveAIConstants.MarkStandoffM:R},{DefensiveAIConstants.MarkStandoffTightM:R}"),
                $"samples={sampleCount}",
                $"shots={shots[0]},{shots[1]}",
                $"goals={goals[0]},{goals[1]}",
                $"fouls={fouls[0]},{fouls[1]}",
                $"offsides={offsides[0]},{offsides[1]}",
                $"groundedEntries={groundedEntries[0]},{groundedEntries[1]}",
                $"collisionKnockdowns={collisionKnockdowns[0]},{collisionKnockdowns[1]}",
                $"slidingTackleEntries={slidingTackleEntries[0]},{slidingTackleEntries[1]}",
                $"divingHeaderEntries={divingHeaderEntries[0]},{divingHeaderEntries[1]}",
                $"otherGroundedEntries={otherGroundedEntries[0]},{otherGroundedEntries[1]}",
                $"capDemotions={capDemotions[0]},{capDemotions[1]}",
                $"tackleIntents={intents[0]},{intents[1]}",
                $"tackleResolutions={engine.TestOnly_TackleResolvedByTeam(0)},{engine.TestOnly_TackleResolvedByTeam(1)}",
                $"markerPresses={markerPresses}",
                $"nonCarrierPresses={nonCarrierPresses}",
                $"terminal={terminal}",
                $"allTicks={DigestHex(allTicks)}",
                "determinism=passed",
                $"restore={(checkpointHeader == null ? "not-run" : "passed")}",
            });
        }

        private static byte[] RunDigests(MatchEngine engine, int ticks)
        {
            using var digests = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            for (int tick = 0; tick < ticks; tick++)
            {
                engine.RunTick();
                digests.AppendData(engine.CurrentSnapshotDigest);
            }
            return digests.GetHashAndReset();
        }

        private static void RequireFinite(float x, float y, int frame)
        {
            if (!float.IsFinite(x) || !float.IsFinite(y))
                Assert.Fail($"Non-finite state at frame {frame}.");
        }

        private static void WriteProtocolEvents(MatchEngine e, StreamWriter stream, int frame,
            int[] shots, int[] goals, int[] fouls, int[] offsides)
        {
            for (int i = 0; i < e.TickLedgerCount; i++)
            {
                byte ordinal = e.TickLedgerOrdinal(i);
                if (ordinal == EventRegistry.GetOrdinal<ShotExecutedEvent>())
                {
                    var shot = e.TickLedgerRecord<ShotExecutedEvent>(i);
                    shots[shot.TeamId]++;
                    var outfield = ShotGeometry(e, shot.ShootingAgentId, shot.TeamId, false);
                    var all = ShotGeometry(e, shot.ShootingAgentId, shot.TeamId, true);
                    stream.WriteLine(FormattableString.Invariant(
                        $"{frame},shot,{shot.TeamId},{shot.ShootingAgentId},0,{outfield.Distance:R},{all.Distance:R},{outfield.GoalSide},{all.GoalSide}"));
                }
                else if (ordinal == EventRegistry.GetOrdinal<GoalAwardedEvent>())
                {
                    var goal = e.TickLedgerRecord<GoalAwardedEvent>(i);
                    goals[goal.ScoringTeam]++;
                    WriteEvent(stream, frame, "goal", goal.ScoringTeam, goal.Scorer);
                }
                else if (ordinal == EventRegistry.GetOrdinal<RestartAwardedEvent>())
                    WriteEvent(stream, frame, "restart", e.TickLedgerRecord<RestartAwardedEvent>(i).AwardedTeam);
                else if (ordinal == EventRegistry.GetOrdinal<CardIssuedEvent>())
                {
                    var card = e.TickLedgerRecord<CardIssuedEvent>(i);
                    WriteEvent(stream, frame, "card", e.AgentTeamId(card.Recipient), card.Recipient, card.CardKind);
                }
                else if (ordinal == EventRegistry.GetOrdinal<SubstitutionEvent>())
                {
                    var sub = e.TickLedgerRecord<SubstitutionEvent>(i);
                    WriteEvent(stream, frame, "substitution", sub.Team, sub.Outgoing, sub.Incoming);
                }
                else if (ordinal == EventRegistry.GetOrdinal<FoulCommittedEvent>())
                {
                    var foul = e.TickLedgerRecord<FoulCommittedEvent>(i);
                    int team = e.AgentTeamId(foul.Offender);
                    fouls[team]++;
                    WriteEvent(stream, frame, "foul", team, foul.Offender);
                }
                else if (ordinal == EventRegistry.GetOrdinal<OffsideCalledEvent>())
                {
                    var offside = e.TickLedgerRecord<OffsideCalledEvent>(i);
                    offsides[offside.Team]++;
                    WriteEvent(stream, frame, "offside", offside.Team, offside.OffendingAgentId);
                }
            }
        }

        private static void WriteEvent(StreamWriter stream, int frame, string kind, int team, int agent = -1, int detail = 0) =>
            stream.WriteLine(FormattableString.Invariant($"{frame},{kind},{team},{agent},{detail},,,,"));

        private static (float Distance, bool GoalSide) ShotGeometry(MatchEngine e, int shooter, int attackingTeam, bool includeGk)
        {
            Vector2 attacker = e.AgentView(shooter).Position;
            Vector2 goal = new Vector2(attackingTeam == 0 ? 105f : 0f, 34f);
            float nearest = float.PositiveInfinity;
            bool goalSide = false;
            for (int a = 0; a < MatchEngineConstants.SQUAD_SIZE; a++)
            {
                if (e.AgentTeamId(a) == attackingTeam || e.AgentIsSentOff(a) || (!includeGk && e.AgentIsGoalkeeper(a)))
                    continue;
                Vector2 position = e.AgentView(a).Position;
                float distance = Vector2.Distance(attacker, position);
                if (distance < nearest)
                {
                    nearest = distance;
                    goalSide = Vector2.Distance(position, goal) < Vector2.Distance(attacker, goal);
                }
            }
            return (nearest, goalSide);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-23 | —      | Add §10 capture, determinism and restore receipts, and classify GROUNDED entries by cause. |
// | 1.1     | 2026-11-23 | —      | Use Unity-compatible digest formatting and select the GZip compression enum explicitly. |
#endregion
