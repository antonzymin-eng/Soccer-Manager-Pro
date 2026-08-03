// File:     src/match-client-core/tests/MatchClientClosedLoopScenarios.cs
// Created:  2026-08-03
// Modified: 2026-08-03
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P6 / §6.1 /
//           §6.2 / §6.3 / §9), Testing Strategy & Framework #19 §3.3.1 / §3.3.3 / §3.3.5 /
//           Appendix A.1 / KD-8, Deterministic Simulation #16 §3.1 / §4.8,
//           Tactical Instructions #21 §3.1 / FR-TI-027, Code Standards #20
// Purpose:  The §5-P6 head-less closed-loop scenarios on the #19 ScenarioRunner. Both boot through the
//           real MatchSession composition root and drive it with a scripted tick-stamped command
//           sequence: (a) two runs with the same MatchSetup + the same tick-stamped log are
//           digest-identical, and a run WITHOUT the commands is not — so the log is proven load-bearing,
//           not merely present; (b) save@N -> restore -> replay the post-N log to N+K reproduces the
//           uninterrupted run byte for byte. This locks input determinism at the COMPOSITION level,
//           with no Unity host, which is why §12 sequences it ahead of the P4 render skin: every P4/P5
//           line is invisible to tools/dotnet-ci, and these predicates are checked on every push.
//
//           SCOPE NOTE — what these scenarios do NOT claim. They say nothing about a live human match
//           being reproducible from clicks: by §6.1 it is not, because the tick a wall-clock click lands
//           on is a function of scheduling, pause state and speed. Reproducibility is defined against
//           the tick-stamped log, and the log is exactly what is replayed here. They also assert nothing
//           about the football the composed engine plays underneath — the owning-spec set is the three
//           specs whose contracts the envelope actually checks.

using System;
using System.Collections.Generic;
using System.Globalization;

using TacticalDirector.DeterministicSim;
using TacticalDirector.MatchEngine;
using TacticalDirector.TacticalInstructions;
using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchClientCore.Tests
{
    /// <summary>
    /// Builds the P6 closed-loop scenario index (#19 §3.3.5 cross-spec/ layout; KD-8 ownership, in the
    /// composition root's own test assembly per the <c>MatchEngineCapstoneScenarios</c> precedent).
    /// Manifest paths live under <see cref="TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX"/>.
    ///
    /// <para>Owning specs are the three whose contracts the envelopes assert: <b>#16</b> (the 7-phase
    /// tick, the chained snapshot digest, and the save/restore round-trip the second scenario closes),
    /// <b>#19</b> (the harness), and <b>#21</b> (the stride-committed <c>SetTeamTactic</c> /
    /// <c>SetPlayerTactic</c> mutators the command channel drives, FR-TI-027). The fully composed
    /// MatchEngine runs beneath both scenarios, but no predicate here asserts any of its subsystems'
    /// behaviour — that is the <c>match-engine-kickoff-multi-second</c> capstone's job, and padding the
    /// owning set with specs this envelope does not check would misreport what a failure here means.</para>
    ///
    /// <para>The bodies draw no randomness of their own: the engine self-seeds its own
    /// <see cref="DeterministicRngService"/> from the run seed at boot (KD-7), so the recorded seeds are
    /// canonical identifiers per FR-TS-025 and a seed sweep re-runs the same script on a different
    /// match. Tier classification is B (Simulation layer).</para>
    /// </summary>
    internal static class MatchClientClosedLoopScenarios
    {
        public const string CommandLogReplayPath =
            TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX + "match-client-command-log-replay";

        public const string SaveRestoreReplayPath =
            TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX + "match-client-save-restore-replay";

        public const ulong CommandLogReplaySeed  = 0x0C1E27A5D6B94E01UL;
        public const ulong SaveRestoreReplaySeed = 0x0C1E27A5D6B94E02UL;

        // Owning specs: the three whose contracts the envelopes assert. See the class remarks for why
        // the composed engine's subsystem specs are deliberately NOT listed.
        private static readonly int[] OwningSpecIds = { 16, 19, 21 };

        // ── The scripted command sequence ────────────────────────────────────────────────────────────
        //
        // Ten commands across both teams, straddling the save tick. Both sides are scripted on purpose:
        // three home/away asymmetry defects have shipped in this engine because every worked example and
        // every fixture used the home team (#8 ERR-008-002, root CLAUDE.md "Things That Have Gone Wrong
        // Before"), so a home-only command script would be the same mistake in the client layer.
        //
        // Slot layout is MatchEngine's boot assignment: team 0 holds roster slots 0..10 and team 1 holds
        // 11..21, with slot 0 / slot 11 the goalkeepers. The substituted slots are outfielders.
        private const int HomeOutfieldSlotA = 5;
        private const int HomeAgentA        = 4;
        private const int AwayOutfieldSlotA = 16;
        private const int AwayOutfieldSlotB = 17;
        private const int AwayAgentA        = 15;
        private const int HomeAgentB        = 7;

        // Command ticks. Spread across AI strides (10 Hz AI on a 60 Hz tick = a stride every 6 ticks) so
        // staged tactics reach their FR-TI-027 commit boundary inside the run rather than at its edge.
        private const ulong TickTeamTactics    = 12;
        private const ulong TickPlayerTactics  = 30;
        private const ulong TickHomeSub        = 60;
        private const ulong TickAwaySub        = 66;
        private const ulong TickPostSaveTeam   = 108;
        private const ulong TickPostSavePlayer = 120;
        private const ulong TickPostSaveSub    = 132;
        private const ulong TickPostSaveTeam2  = 150;

        /// <summary>The tick the save is taken at. Deliberately carries no scheduled command — see
        /// <see cref="TickStampedCommandReplay.FromTick"/> on why the boundary would otherwise be
        /// ambiguous. The scenario checks that emptiness rather than assuming it.</summary>
        private const ulong SaveTick = 90;

        /// <summary>Total ticks each reference run advances (1.5 s of composed match time at 60 Hz).</summary>
        private const ulong FinalTick = 180;

        // Non-vacuity window: the first team-tactic command is drained at the top of the tick that runs
        // TickTeamTactics -> TickTeamTactics+1, and the staged tactic is serialized state (#21
        // ERR-021-002, SNAPSHOT_SCHEMA_VERSION 9), so a command-free control run must diverge from that
        // digest onward. The upper bound allows two AI strides of slack so the predicate does not pin
        // itself to the serialization detail — but it stays tight enough that "diverged eventually, for
        // unrelated reasons" cannot pass as "the commands did something". Computed at the use site
        // rather than as consts: AI_PHASE_STRIDE is static readonly, not const (#16 AR-1 M-1).
        private static float FirstDivergenceMin => TickTeamTactics + 1UL;

        private static float FirstDivergenceMax =>
            TickTeamTactics + 1UL + 2UL * (ulong)DeterministicSimConstants.AI_PHASE_STRIDE;

        public static ScenarioIndex BuildIndex()
        {
            var replayManifest = new ScenarioManifest(
                "match-client-command-log-replay",
                owningSpecIds: OwningSpecIds,
                seed: CommandLogReplaySeed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);

            var saveManifest = new ScenarioManifest(
                "match-client-save-restore-replay",
                owningSpecIds: OwningSpecIds,
                seed: SaveRestoreReplaySeed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);

            return new ScenarioIndex(new[]
            {
                new ScenarioIndexEntry(
                    CommandLogReplayPath, replayManifest,
                    new ClosedLoopScenario(replayManifest, CommandLogReplay)),
                new ScenarioIndexEntry(
                    SaveRestoreReplayPath, saveManifest,
                    new ClosedLoopScenario(saveManifest, SaveRestoreReplay)),
            });
        }

        // ── (a) Same MatchSetup + same tick-stamped log => digest-identical ─────────────────────────
        //
        // Run A is driven by the SCRIPT and produces a log. Run B is driven by A's LOG — the artifact
        // §6.1 defines reproducibility against — and must reproduce A's digest chain exactly and emit an
        // identical log of its own (the log is a fixed point of its own replay). Run C is the control:
        // same MatchSetup, no commands at all. If C matched A, every predicate above it would be
        // vacuous — the composition would be "reproducible" only because the command channel did
        // nothing. That is the exact failure mode of the 600-tick capstone that asserted a match ticked
        // while every match was a 0-0 deadlock (ERR-030-014), so the control is a first-class predicate,
        // not a nicety.
        private static void CommandLogReplay(ScenarioContext context)
        {
            TickStampedCommand[] script = BuildScript();

            var sessionA = new MatchSession(MatchSetup.NeutralDemo(context.RunSeed));
            List<byte[]> digestsA = RunTo(sessionA, new TickStampedCommandReplay(script), FinalTick);
            IReadOnlyList<TickStampedCommand> logA = sessionA.Driver.Log;

            // The stamping contract: every scripted command was applied, at exactly the tick it was
            // scheduled for. Asserted BEFORE the replay predicates, because a replay of an empty or
            // truncated log would trivially reproduce itself.
            context.Envelope.CheckEquals("script-fully-applied", logA.Count, script.Length);
            bool stampsMatchScript = LogMatchesScript(logA, script, out string stampDetail);
            context.Envelope.CheckTrue("log-stamps-match-script", stampsMatchScript, stampDetail);

            // A command the engine REFUSED is dropped and recorded separately — it never enters the log,
            // so a script of entirely-refused commands would still replay "identically". Check that the
            // script actually drove live mutators.
            context.Envelope.CheckEquals("no-refused-commands", sessionA.Driver.FailedCommands.Count, 0);
            context.Envelope.CheckEquals("queue-fully-drained", sessionA.Commands.Count, 0);

            var sessionB = new MatchSession(MatchSetup.NeutralDemo(context.RunSeed));
            List<byte[]> digestsB = RunTo(sessionB, new TickStampedCommandReplay(logA), FinalTick);

            bool replayIdentical = DigestChainsEqual(digestsA, digestsB, out int replayDivergeTick);
            context.Envelope.CheckTrue(
                "replay-from-log-is-digest-identical", replayIdentical,
                "the snapshot digest chain diverged between a scripted run and a replay of its own "
                    + "tick-stamped log at tick "
                    + replayDivergeTick.ToString(CultureInfo.InvariantCulture));

            bool logsEqual = LogsEqual(logA, sessionB.Driver.Log, out string logDetail);
            context.Envelope.CheckTrue("replay-log-is-a-fixed-point", logsEqual, logDetail);

            // Control: identical setup, no commands. Must diverge, and diverge because of the first
            // command rather than by coincidence far downstream.
            var sessionC = new MatchSession(MatchSetup.NeutralDemo(context.RunSeed));
            List<byte[]> digestsC = RunTo(
                sessionC, new TickStampedCommandReplay(Array.Empty<TickStampedCommand>()), FinalTick);

            context.Envelope.CheckEquals("control-run-applied-no-commands", sessionC.Driver.Log.Count, 0);
            DigestChainsEqual(digestsA, digestsC, out int controlDivergeTick);
            context.Envelope.CheckInRange(
                "commands-change-the-run",
                controlDivergeTick,
                FirstDivergenceMin,
                FirstDivergenceMax);
        }

        // ── (b) save@N -> restore -> replay post-N log == the uninterrupted run ─────────────────────
        private static void SaveRestoreReplay(ScenarioContext context)
        {
            TickStampedCommand[] script = BuildScript();

            // The scenario's own precondition, checked rather than assumed: a command scheduled AT the
            // save tick would be inside the snapshot or outside it depending on drain order within the
            // capture pass, while carrying the same stamp either way — which would make the
            // resume-from-N+1 slice below silently wrong rather than loudly wrong.
            context.Envelope.CheckEquals("save-tick-carries-no-command", CountAt(script, SaveTick), 0);

            // Reference: one uninterrupted run over the whole script.
            var reference = new MatchSession(MatchSetup.NeutralDemo(context.RunSeed));
            var referenceReplay = new TickStampedCommandReplay(script);
            RunTo(reference, referenceReplay, SaveTick);
            List<byte[]> referenceTail = RunTo(reference, referenceReplay, FinalTick);
            IReadOnlyList<TickStampedCommand> referenceLog = reference.Driver.Log;

            // Saved run: the same script up to the save tick, then a durable capture through the
            // ServiceOnce seam (§6.3) — the path that works while running, paused and at full time.
            var saved = new MatchSession(MatchSetup.NeutralDemo(context.RunSeed));
            RunTo(saved, new TickStampedCommandReplay(script), SaveTick);
            byte[] blob = saved.CaptureSave();

            // NOTE — the §6.3 drained-empty-before-capture invariant is deliberately NOT asserted here.
            // The replay leaves the queue empty at every tick boundary, so a "queue is drained" predicate
            // at this point would be true no matter which order the capture pass ran in — a vacuous pass
            // dressed as a guarantee. It is locked instead by MatchSessionTests, where a command is
            // enqueued immediately before the capture and must come back applied and logged.

            // Restored run: a whole session rebuilt over the restored engine, resuming the log from the
            // tick after the save. The log is in-memory only (§11) and is deliberately NOT carried
            // across the save — a resumed match reproduces from the snapshot plus the POST-restore log,
            // which is precisely what is replayed here.
            MatchSession restored = MatchSession.RestoreFrom(blob);

            context.Envelope.CheckEquals(
                "restore-resumes-at-save-tick", (long)restored.CurrentTick, (long)SaveTick);
            context.Envelope.CheckEquals(
                "restored-session-starts-with-an-empty-log", restored.Driver.Log.Count, 0);

            var resumeReplay = TickStampedCommandReplay.FromTick(referenceLog, SaveTick + 1);
            List<byte[]> restoredTail = RunTo(restored, resumeReplay, FinalTick);

            // The resume must actually have replayed commands — otherwise this degenerates into the
            // plain engine-level restore round-trip that #16's own suite already covers, and would say
            // nothing about the command channel across a save.
            long expectedPostSave = CountFrom(script, SaveTick + 1);
            context.Envelope.CheckTrue(
                "post-save-script-is-non-empty", expectedPostSave > 0,
                "the script schedules no command after the save tick, so this scenario would not "
                    + "exercise the command channel across a restore; scheduled="
                    + expectedPostSave.ToString(CultureInfo.InvariantCulture));
            context.Envelope.CheckEquals(
                "post-save-commands-replayed", restored.Driver.Log.Count, expectedPostSave);
            context.Envelope.CheckEquals(
                "no-refused-commands-after-restore", restored.Driver.FailedCommands.Count, 0);

            bool roundTripIdentical = DigestChainsEqual(referenceTail, restoredTail, out int divergeTick);
            context.Envelope.CheckTrue(
                "roundtrip-digest-identical", roundTripIdentical,
                "save@" + SaveTick.ToString(CultureInfo.InvariantCulture) + " -> restore -> replay "
                    + "diverged from the uninterrupted run at tick "
                    + (SaveTick + (ulong)Math.Max(divergeTick, 0)).ToString(CultureInfo.InvariantCulture)
                    + " (a cross-tick field omitted from the snapshot, or a command re-applied at the "
                    + "wrong tick on resume)");
        }

        // ── Script + helpers ─────────────────────────────────────────────────────────────────────────

        // The scripted (tick, command) sequence, in non-decreasing tick order — the shape a real
        // MatchClientDriver log always has. Both teams, on both sides of the save tick, across all three
        // live mutators.
        private static TickStampedCommand[] BuildScript()
        {
            return new[]
            {
                Stamp(TickTeamTactics, ManagerCommand.SetTeamTactic(
                    0, TacticVariant(Mentality.Attacking, Tempo.Fast, TacticPressing.High))),
                Stamp(TickTeamTactics, ManagerCommand.SetTeamTactic(
                    1, TacticVariant(Mentality.Defensive, Tempo.Slow, TacticPressing.Low))),

                Stamp(TickPlayerTactics, ManagerCommand.SetPlayerTactic(
                    HomeAgentA, new PlayerTactic(PlayerRole.Poacher, Duty.Attack, PlayerInstructions.Default))),
                Stamp(TickPlayerTactics, ManagerCommand.SetPlayerTactic(
                    AwayAgentA, new PlayerTactic(PlayerRole.DeepLyingPlaymaker, Duty.Support, PlayerInstructions.Default))),

                Stamp(TickHomeSub, ManagerCommand.Substitute(
                    0, HomeOutfieldSlotA, 0, SubstitutionReason.Tactical)),
                Stamp(TickAwaySub, ManagerCommand.Substitute(
                    1, AwayOutfieldSlotA, 0, SubstitutionReason.Tactical)),

                // ── save tick (90) sits here, deliberately command-free ──
                Stamp(TickPostSaveTeam, ManagerCommand.SetTeamTactic(
                    0, TacticVariant(Mentality.Cautious, Tempo.Slow, TacticPressing.Medium))),
                Stamp(TickPostSavePlayer, ManagerCommand.SetPlayerTactic(
                    HomeAgentB, new PlayerTactic(PlayerRole.DeepLyingPlaymaker, Duty.Defend, PlayerInstructions.Default))),
                Stamp(TickPostSaveSub, ManagerCommand.Substitute(
                    1, AwayOutfieldSlotB, 1, SubstitutionReason.Tactical)),
                Stamp(TickPostSaveTeam2, ManagerCommand.SetTeamTactic(
                    1, TacticVariant(Mentality.Positive, Tempo.Fast, TacticPressing.High))),
            };
        }

        private static TickStampedCommand Stamp(ulong tick, ManagerCommand command) =>
            new TickStampedCommand(tick, in command);

        // TeamTactic.Balanced with three serialized fields varied. Written out rather than mutated
        // because TeamTactic is an immutable readonly struct with no wither for these fields; keeping
        // every other field at the Balanced identity means the digest divergence a command produces is
        // attributable to the three fields this varies.
        private static TeamTactic TacticVariant(Mentality mentality, Tempo tempo, TacticPressing pressing) =>
            new TeamTactic(
                mentality,
                TacticFormation.F442,
                tempo,
                TacticWidth.Standard,
                TacticPassing.Mixed,
                pressing,
                LineOfEngagement.Standard,
                0.5f,
                TacticDefWidth.Standard,
                TransitionPlan.HoldShape,
                TransitionPlan.Regroup,
                false,
                TacticTriggerMask.None,
                FocusPlay.Mixed,
                GkDistributionPolicy.SlowDown,
                0,
                MarkingOrientation.Balanced);

        // Advances the session one tick at a time to targetTick, letting the replay enqueue at each
        // tick boundary, and captures the chained snapshot digest after every tick. One-tick steps (not
        // a single AdvanceTo call) because the per-tick digest chain is what the predicates compare —
        // a final-digest-only comparison would hide a divergence that later re-converged.
        private static List<byte[]> RunTo(
            MatchSession session, TickStampedCommandReplay replay, ulong targetTick)
        {
            ulong start = session.CurrentTick;
            var digests = new List<byte[]>((int)(targetTick - start));
            for (ulong t = start; t < targetTick; t++)
            {
                replay.AdvanceTo(session, t + 1);
                digests.Add(session.CurrentSnapshotDigest);
            }
            return digests;
        }

        private static bool LogMatchesScript(
            IReadOnlyList<TickStampedCommand> log, TickStampedCommand[] script, out string detail)
        {
            int n = Math.Min(log.Count, script.Length);
            for (int i = 0; i < n; i++)
            {
                if (log[i].AppliedTick != script[i].AppliedTick
                    || log[i].Command.Kind != script[i].Command.Kind)
                {
                    detail = "entry " + i.ToString(CultureInfo.InvariantCulture)
                        + " scheduled (tick=" + script[i].AppliedTick.ToString(CultureInfo.InvariantCulture)
                        + ", kind=" + script[i].Command.Kind
                        + ") but applied (tick=" + log[i].AppliedTick.ToString(CultureInfo.InvariantCulture)
                        + ", kind=" + log[i].Command.Kind + ")";
                    return false;
                }
            }
            detail = "log length=" + log.Count.ToString(CultureInfo.InvariantCulture)
                + " script length=" + script.Length.ToString(CultureInfo.InvariantCulture);
            return log.Count == script.Length;
        }

        private static bool LogsEqual(
            IReadOnlyList<TickStampedCommand> a, IReadOnlyList<TickStampedCommand> b, out string detail)
        {
            if (a.Count != b.Count)
            {
                detail = "log lengths differ: " + a.Count.ToString(CultureInfo.InvariantCulture)
                    + " vs " + b.Count.ToString(CultureInfo.InvariantCulture);
                return false;
            }
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].AppliedTick != b[i].AppliedTick || a[i].Command.Kind != b[i].Command.Kind)
                {
                    detail = "entry " + i.ToString(CultureInfo.InvariantCulture) + " differs: (tick="
                        + a[i].AppliedTick.ToString(CultureInfo.InvariantCulture) + ", kind="
                        + a[i].Command.Kind + ") vs (tick="
                        + b[i].AppliedTick.ToString(CultureInfo.InvariantCulture) + ", kind="
                        + b[i].Command.Kind + ")";
                    return false;
                }
            }
            detail = "entries=" + a.Count.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private static long CountAt(TickStampedCommand[] script, ulong tick)
        {
            long count = 0;
            for (int i = 0; i < script.Length; i++)
            {
                if (script[i].AppliedTick == tick) { count++; }
            }
            return count;
        }

        private static long CountFrom(TickStampedCommand[] script, ulong fromTickInclusive)
        {
            long count = 0;
            for (int i = 0; i < script.Length; i++)
            {
                if (script[i].AppliedTick >= fromTickInclusive) { count++; }
            }
            return count;
        }

        // Returns true when the two chains are byte-identical. divergeTick is the 1-based index of the
        // first differing tick WITHIN the compared window (so callers that compare a tail add their own
        // offset), or -1 when they match.
        private static bool DigestChainsEqual(List<byte[]> a, List<byte[]> b, out int divergeTick)
        {
            int n = Math.Min(a.Count, b.Count);
            for (int i = 0; i < n; i++)
            {
                if (!BytesEqual(a[i], b[i]))
                {
                    divergeTick = i + 1;
                    return false;
                }
            }
            if (a.Count != b.Count)
            {
                divergeTick = n + 1;
                return false;
            }
            divergeTick = -1;
            return true;
        }

        private static bool BytesEqual(byte[] x, byte[] y)
        {
            if (x == null || y == null || x.Length != y.Length)
            {
                return false;
            }
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-03 | —      | Initial implementation (P6). Two cross-spec closed-loop            |
// |         |            |        | scenarios on the #19 ScenarioRunner, booted through the real       |
// |         |            |        | MatchSession: match-client-command-log-replay (script -> log,      |
// |         |            |        | log -> digest-identical replay, log is a fixed point, and a        |
// |         |            |        | command-free control that MUST diverge) and                        |
// |         |            |        | match-client-save-restore-replay (save@90 -> restore -> replay     |
// |         |            |        | the post-90 log to tick 180 == the uninterrupted run). Owning      |
// |         |            |        | specs {16,19,21}; paths under SCENARIO_PATH_CROSS_SPEC_PREFIX.     |
#endregion
