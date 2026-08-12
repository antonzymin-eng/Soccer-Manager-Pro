// File:     src/match-engine/tests/MatchEngineTackleTests.cs
// Created:  2026-08-12
// Modified: 2026-08-12
// Author:   —
// Spec:     Defensive AI #14 §3.6.5, Pass Mechanics #5 §3.8.5/§4.4.2, Shot Mechanics #6 §4.4.2,
//           foul-discipline-balance-design.md KD-F1/KD-F2/KD-F4, Code Standards #20
// Purpose:  Composed-engine locks for wiring backlog W2 — the tackle. These assert the OUTCOME the
//           wiring exists to produce (a controlled carrier being dispossessed), not that the engine
//           ticks without throwing.
//
//           The ERR-030-014 lesson governs this file. The 600-tick capstone asserted tick count,
//           cadence, finiteness, bounds and digest advance — every one of which is true of a match in
//           which nothing happens — and every match was a 0-0 deadlock for months underneath it. So
//           the question each test here has to answer is "would this fail if the mechanism were
//           reverted", and the tautology shapes this repo has shipped before are named at each case.

using System;

using NUnit.Framework;

using TacticalDirector.DefensiveAI;
using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.MatchEngine
{
    /// <summary>Wiring backlog W2 — the tackle, through a real composed <c>MatchEngine</c>.</summary>
    [TestFixture]
    internal class MatchEngineTackleTests
    {
        /// <summary>
        /// Half a match. MEASURED, not guessed: the gate anatomy reports ~10 resolved challenges per
        /// 40 000 ticks on the livelier of the two seeds and ~1 on the quieter one, so a per-seed
        /// assertion at 40 000 ticks is a coin flip dressed as a lock. At this length both seeds carry
        /// challenges, and the cases below pool the two rather than asserting per seed — pooling is
        /// right here because the claim is "this engine produces tackles", not "this seed does".
        ///
        /// <para><b>Corrected at AR-1 M-4.</b> This doc used to claim home and away were distinguished
        /// "see <c>ChallengesOccurForBothDefendingTeams</c>" — a test that was never written, and the
        /// only hit for that name in the tree was the sentence itself. It exists now, and the outcome
        /// counters are split per team so it can, because a defect in which only team 0 ever resolves a
        /// challenge would otherwise pass every case in this file (ERR-008-002).</para>
        /// </summary>
        private const int Ticks = 150_000;

        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
        };

        private static Squad BuildSquad(ulong seed, int clubId)
        {
            var rng = new DeterministicRngService(seed ^ (ulong)clubId);
            int stream = rng.RegisterStream(
                "tackle.roster", SubsystemOrdinals.PlayerDatabase, entityId: clubId, streamVersion: 1);

            var template = new PlayerPosition[PlayerDatabaseConstants.CLUB_SQUAD_SIZE];
            int i = 0;
            for (int k = 0; k < 3; k++) template[i++] = PlayerPosition.Goalkeeper;
            for (int k = 0; k < 8; k++) template[i++] = PlayerPosition.Defender;
            for (int k = 0; k < 8; k++) template[i++] = PlayerPosition.Midfielder;
            while (i < template.Length) template[i++] = PlayerPosition.Forward;

            return RosterGenerator.Generate(rng, stream, clubId, template);
        }

        /// <summary>The <c>ISquadProvider</c> the restore path needs to rebuild the rosters
        /// <c>ConfigureSquads</c> loaded — the engine serializes each team's ClubId, not its squad.</summary>
        private sealed class TwoClubProvider : ISquadProvider
        {
            private readonly ulong _seed;

            public TwoClubProvider(ulong seed)
            {
                _seed = seed;
            }

            public Squad ResolveByClubId(int clubId) => BuildSquad(_seed, clubId);
        }

        private static MatchEngine Booted(ulong seed)
        {
            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));
            return engine;
        }

        // ── One-time corpus ───────────────────────────────────────────────────
        //
        // The composed run is the expensive part, and every case below asks a different question of
        // the SAME run. Simulating per assertion would multiply that cost by the case count for no
        // extra coverage — and a suite that costs six matches to answer six questions about one match
        // is how a gate stops being run.

        private static (int Won, int Loose, int Foul, int Missed) s_pooled;
        private static int s_dispossessions;
        private static bool s_sawCooldownWhileOnlyMisses;
        private static readonly int[] s_perTeamResolved = new int[MatchEngineConstants.TEAM_COUNT];
        private static int s_slideTackleFouls;

        [OneTimeSetUp]
        public void RunTheCorpusOnce()
        {
            int won = 0, loose = 0, foul = 0, missed = 0;

            foreach (ulong seed in Seeds)
            {
                MatchEngine engine = Booted(seed);
                int prevHolder = MatchEngineConstants.NO_POSSESSION;
                var before = engine.TestOnly_TackleOutcomeCounts;
                int missesSeen = 0;

                for (int t = 0; t < Ticks; t++)
                {
                    engine.RunTick();

                    var now = engine.TestOnly_TackleOutcomeCounts;

                    // Dispossession: a tackle landed on this tick AND the ball changed hands. The
                    // conjunction is what makes it a tackle observation and not a pass observation.
                    bool landed = (now.Won + now.Loose) > (before.Won + before.Loose);
                    int holder = engine.PossessingAgentId;
                    if (landed && prevHolder >= 0 && holder != prevHolder)
                    {
                        s_dispossessions++;
                    }

                    // A cooldown armed while NOTHING but misses has happened yet.
                    if (!s_sawCooldownWhileOnlyMisses
                        && now.Missed > missesSeen
                        && now.Won + now.Loose + now.Foul == 0)
                    {
                        for (int a = 0; a < MatchEngineConstants.SQUAD_SIZE; a++)
                        {
                            if (engine.TestOnly_TackleCooldown(a) > 0)
                            {
                                s_sawCooldownWhileOnlyMisses = true;
                                break;
                            }
                        }
                    }

                    missesSeen = now.Missed;
                    before = now;
                    prevHolder = holder;
                }

                for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
                {
                    s_perTeamResolved[t] += engine.TestOnly_TackleResolvedByTeam(t);
                }
                s_slideTackleFouls += engine.TestOnly_TackleSlideTackleFouls;

                var oc = engine.TestOnly_TackleOutcomeCounts;
                won += oc.Won;
                loose += oc.Loose;
                foul += oc.Foul;
                missed += oc.Missed;
            }

            s_pooled = (won, loose, foul, missed);
        }

        /// <summary>The corpus in one line, appended to every assertion message: a red lock that does
        /// not say what the engine actually produced sends the next reader back to re-run it.</summary>
        private static string Counts() =>
            $"won={s_pooled.Won} loose={s_pooled.Loose} foul={s_pooled.Foul} missed={s_pooled.Missed} " +
            $"dispossessions={s_dispossessions}";

        [Test]
        public void TacklesHappenInComposedPlay()
        {
            // The headline. Before W2 this was zero for every match this engine had ever played — not
            // "rare" but structurally impossible, since GetTackleIntentRequests had no reader at all.
            // Deleting the TryResolveTackle call site fails this by construction.
            int resolved = s_pooled.Won + s_pooled.Loose + s_pooled.Foul + s_pooled.Missed;

            Assert.That(resolved, Is.GreaterThan(0), $"no challenge was resolved at all ({Counts()})");
            Assert.That(s_pooled.Won + s_pooled.Loose, Is.GreaterThan(0),
                $"challenges happen but none ever dispossesses anyone — the outcome model is inert ({Counts()})");
        }

        [Test]
        public void AControlledCarrierIsActuallyDispossessed()
        {
            // The property the whole item exists for, and the one an outcome counter cannot establish:
            // possession moving off a controlled carrier WITHOUT him kicking it. Before W2 the engine
            // had exactly two turnover mechanisms — a kick, and a foul restart — so this predicate was
            // unsatisfiable however long the match ran.
            Assert.That(s_dispossessions, Is.GreaterThan(0),
                "no tick exists where a tackle landed and the ball changed hands");
        }

        [Test]
        public void BothOutcomesOccur_TheBallIsSometimesWonAndSometimesKnockedLoose()
        {
            // BallLoose is the remainder of a remainder in §3.6.5's nested inverse transforms, so a
            // sign error in either one deletes it silently while the other outcomes stay healthy. The
            // pure suite covers that analytically; this covers it through the attribute distribution a
            // generated roster actually produces, which the pure suite cannot.
            Assert.That(s_pooled.Loose, Is.GreaterThan(0), $"no challenge ever knocked the ball loose ({Counts()})");
            Assert.That(s_pooled.Won, Is.GreaterThan(0), $"no challenge was ever won cleanly ({Counts()})");
        }

        [Test]
        public void ChallengesAreBoundedAbove_NotJustNonZero()
        {
            // "> 0" is this file's tautology risk: the failure mode of a NEW turnover source is far more
            // likely to be too many than too few, and a floor-only assertion is green either way. The
            // bound below is loose enough not to be a calibration lock (KD-W1 forbids one) and tight
            // enough that a cooldown regression trips it — without the per-agent cooldown a single
            // standing geometry re-challenges at 10 Hz and the count runs to thousands.
            int resolved = s_pooled.Won + s_pooled.Loose + s_pooled.Foul + s_pooled.Missed;

            Assert.That(resolved, Is.LessThan(2_000),
                $"{resolved} challenges over {Ticks} ticks x {Seeds.Length} seeds — the per-agent " +
                "cooldown is not limiting re-challenges");
        }

        [Test]
        public void TheCooldownArmsOnEveryOutcomeIncludingAMiss()
        {
            // Arming only on success is the plausible-looking bug: a defender who keeps missing would
            // then re-challenge every stride forever. "Some agent is on cooldown at some point" is the
            // tautology here — true if the cooldown arms on ANY branch — so this pins that a cooldown
            // is observed while the ONLY outcomes so far have been misses.
            Assert.That(s_sawCooldownWhileOnlyMisses, Is.True,
                "no cooldown was ever observed armed while only MISSED outcomes had occurred — " +
                "the cooldown is not arming on a miss");
        }

        [Test]
        public void ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice()
        {
            // #14 §3.6.5 already adjudicated the foul, so ApplyFoulIfCaptured must not re-run KD-F1's
            // referee-call probability over it. If it did, the overwhelming majority of tackle fouls
            // would be waved on and the branch would look wired while being nearly inert. So this
            // asserts tackle fouls reach the discipline path at a rate near §3.6.5's own foul share,
            // not at that share multiplied by FoulCallProbability.
            int connected = s_pooled.Won + s_pooled.Loose + s_pooled.Foul;

            Assume.That(connected, Is.GreaterThan(30),
                $"too few connecting challenges to judge the share ({Counts()})");
            Assert.That(s_pooled.Foul, Is.GreaterThan(0), $"no connecting challenge was ever a foul ({Counts()})");

            double share = s_pooled.Foul / (double)connected;
            Assert.That(share, Is.GreaterThan(0.02),
                $"tackle fouls are {share:P1} of connecting challenges — far below §3.6.5's foul " +
                "share, which is what double-judging through ComputeFoulCallProbability looks like");
        }

        [Test]
        public void ChallengesOccurForBothDefendingTeams()
        {
            // ERR-008-002, the standing house rule: three home/away asymmetry defects have shipped in
            // this tree because every fixture used the home team. It is load-bearing HERE specifically,
            // because until AR-1 H-1 the resolution ran inside the per-team mechanics loop, where team
            // 0's tackle mutated possession before team 1's snapshots were built and never the reverse.
            Assert.That(s_perTeamResolved[0], Is.GreaterThan(0), $"team 0 never challenged ({Counts()})");
            Assert.That(s_perTeamResolved[1], Is.GreaterThan(0), $"team 1 never challenged ({Counts()})");
        }

        [Test]
        public void ATackleFoulIsPublishedAsASlideTackle()
        {
            // AR-1 H-3: the landing claimed FoulCommittedEvent.FoulKind was "meaningful for the first
            // time" — every foul this engine had ever given was published as FROM_BEHIND regardless of
            // cause — and nothing asserted it. ContactType.SLIDE_TACKLE had no producer anywhere in the
            // tree before W2, so a regression that dropped the foulKind branch would be invisible.
            Assume.That(s_pooled.Foul, Is.GreaterThan(0), $"no tackle foul occurred ({Counts()})");
            Assert.That(s_slideTackleFouls, Is.GreaterThan(0),
                $"tackle fouls occurred but none published ContactType.SLIDE_TACKLE ({Counts()})");
            Assert.That(s_slideTackleFouls, Is.EqualTo(s_pooled.Foul),
                $"every tackle foul must publish SLIDE_TACKLE ({Counts()})");
        }

        [Test]
        public void SaveAndRestoreCarryTheTackleLatches([ValueSource(nameof(Seeds))] ulong seed)
        {
            // SNAPSHOT_SCHEMA_VERSION 21's reason to exist. A restore that dropped the cooldown would
            // let every defender re-challenge immediately, diverging the digest on the very next stride
            // — and in the direction of MORE tackles, which is the hard-to-notice direction.
            MatchEngine engine = Booted(seed);
            // MEASURED: challenges occur at roughly one per 4 000 ticks, so the 6 000-tick pre-save
            // and 1 200-tick replay this test first used contained none at all, and every equality
            // below held vacuously. Both windows are sized from that rate.
            for (int t = 0; t < 40_000; t++)
            {
                engine.RunTick();
            }

            byte[] blob = MatchSaveManager.Encode(engine);
            MatchEngine restored = MatchSaveManager.Restore(blob, new TwoClubProvider(seed));

            for (int a = 0; a < MatchEngineConstants.SQUAD_SIZE; a++)
            {
                Assert.That(restored.TestOnly_TackleCooldown(a),
                    Is.EqualTo(engine.TestOnly_TackleCooldown(a)), $"cooldown lost for agent {a}");
                Assert.That(restored.TestOnly_TackleFlag(a),
                    Is.EqualTo(engine.TestOnly_TackleFlag(a)), $"tackle flag lost for agent {a}");
            }

            // The field-by-field comparison above cannot establish the property that matters: a latch
            // can round-trip correctly and still be READ at the wrong time. So replay both forward and
            // require the same number of challenges to occur.
            //
            // The counters are deliberately not serialized (the WoodworkStrikes class), so the restored
            // engine starts from zero and the comparison is delta-against-absolute — which is the whole
            // point: the restored run must PRODUCE the same challenges over the same window, not merely
            // remember that the earlier ones happened.
            var beforeReplay = engine.TestOnly_TackleOutcomeCounts;

            for (int t = 0; t < 40_000; t++)
            {
                engine.RunTick();
                restored.RunTick();
            }

            var afterReplay = engine.TestOnly_TackleOutcomeCounts;
            var restoredCounts = restored.TestOnly_TackleOutcomeCounts;

            Assert.Multiple(() =>
            {
                Assert.That(restoredCounts.Won, Is.EqualTo(afterReplay.Won - beforeReplay.Won), "won diverged");
                Assert.That(restoredCounts.Loose, Is.EqualTo(afterReplay.Loose - beforeReplay.Loose), "loose diverged");
                Assert.That(restoredCounts.Foul, Is.EqualTo(afterReplay.Foul - beforeReplay.Foul), "foul diverged");
                Assert.That(restoredCounts.Missed, Is.EqualTo(afterReplay.Missed - beforeReplay.Missed), "missed diverged");
            });

            // A window in which nothing happened would satisfy every equality above trivially.
            Assert.That(
                (afterReplay.Won + afterReplay.Loose + afterReplay.Foul + afterReplay.Missed)
                - (beforeReplay.Won + beforeReplay.Loose + beforeReplay.Foul + beforeReplay.Missed),
                Is.GreaterThan(0),
                "no challenge occurred in the replay window, so the equalities above prove nothing");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-12 | —      | Initial. Composed-engine locks for wiring backlog W2. Asserts the |
// |         |            |        | OUTCOME (a controlled carrier dispossessed without kicking) that  |
// |         |            |        | was unsatisfiable before this landing, plus a CEILING as well as  |
// |         |            |        | a floor, the cooldown arming on a miss, the foul not being        |
// |         |            |        | judged twice, and the v21 latches surviving save/restore.         |
#endregion
