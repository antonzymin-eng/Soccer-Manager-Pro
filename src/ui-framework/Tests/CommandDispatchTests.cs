// File:     src/ui-framework/Tests/CommandDispatchTests.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     UI / Client Framework #38 §3.3 / §5.1 (T-UI-DISPATCH-001..004, FR-UI-003/012/013/014/023,
//           failure modes F3/F6), Code Standards #20
// Purpose:  Locks command dispatch: each intent routes to exactly its seam and nothing else, an
//           unmapped or uninitialized intent throws rather than vanishing, and the live-match path
//           MARSHALS (enqueues, applies nothing) until the sim thread drains it.

using System;
using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.TacticalInstructions;
using TacticalDirector.UiFramework;

namespace TacticalDirector.UiFramework.Tests
{
    [TestFixture]
    public sealed class CommandDispatchTests
    {
        private const ulong Seed = 0xC0FFEE1234UL;

        // ── T-UI-DISPATCH-001 / 003 — routing, single-threaded mode ──────────────────────────────

        [Test]
        public void SetTeamTacticIntent_RoutesToSetTeamTacticSeam_AndNothingElse()
        {
            var mutations = new RecordingMutations();
            var dispatcher = new MatchTacticsDispatcher(mutations);

            dispatcher.Dispatch(ManagerIntent.SetTeamTactic(1, TeamTactic.Balanced));

            CollectionAssert.AreEqual(new[] { "SetTeamTactic(1)" }, mutations.Calls);
        }

        [Test]
        public void SetPlayerTacticIntent_RoutesToSetPlayerTacticSeam_AndNothingElse()
        {
            var mutations = new RecordingMutations();
            var dispatcher = new MatchTacticsDispatcher(mutations);

            dispatcher.Dispatch(ManagerIntent.SetPlayerTactic(7, PlayerTactic.Default(PlayerRole.Default)));

            CollectionAssert.AreEqual(new[] { "SetPlayerTactic(7)" }, mutations.Calls);
        }

        [Test]
        public void SubstituteIntent_RoutesToSubstituteSeam_WithItsPayloadIntact()
        {
            var mutations = new RecordingMutations();
            var dispatcher = new MatchTacticsDispatcher(mutations);

            dispatcher.Dispatch(ManagerIntent.Substitute(0, 5, 3, SubstitutionReason.Tactical));

            CollectionAssert.AreEqual(new[] { "SubstitutePlayer(0,5,3,Tactical)" }, mutations.Calls);
        }

        [Test]
        public void Dispatch_TouchesOnlyTheThreePublicSeams()
        {
            var mutations = new RecordingMutations();
            var dispatcher = new MatchTacticsDispatcher(mutations);

            dispatcher.Dispatch(ManagerIntent.SetTeamTactic(0, TeamTactic.Balanced));
            dispatcher.Dispatch(ManagerIntent.SetPlayerTactic(0, PlayerTactic.Default(PlayerRole.Default)));
            dispatcher.Dispatch(ManagerIntent.Substitute(0, 1, 2, SubstitutionReason.Tactical));

            // The mutation surface IS the whole reachable API: three calls in, three out, no fourth
            // path. Anything the dispatcher did beyond the seams would have to show up here.
            Assert.AreEqual(3, mutations.Calls.Count);
        }

        // ── T-UI-DISPATCH-002 — F3 fail-loud ─────────────────────────────────────────────────────

        [Test]
        public void DefaultIntent_Throws_NeverSilentlyStagesAZeroPayload()
        {
            var mutations = new RecordingMutations();
            var dispatcher = new MatchTacticsDispatcher(mutations);

            Assert.Throws<InvalidOperationException>(() => dispatcher.Dispatch(default(ManagerIntent)));
            Assert.AreEqual(0, mutations.Calls.Count, "a refused intent touches no seam");
        }

        [Test]
        public void UnmappedIntentKind_Throws_NeverInventsASeam()
        {
            var mutations = new RecordingMutations();
            var dispatcher = new MatchTacticsDispatcher(mutations);
            ManagerIntent bogus = MakeIntentWithKind((IntentKind)0x7F);

            // Non-vacuity guard: if the reflection below silently failed to set the field, the intent
            // would still be None — which ALSO throws, and this test would pass while never once
            // reaching the default: arm it exists to cover.
            Assert.AreEqual((IntentKind)0x7F, bogus.Kind, "the forced kind did not take; the assertion below would be vacuous");

            Assert.Throws<InvalidOperationException>(() => dispatcher.Dispatch(bogus));
            Assert.AreEqual(0, mutations.Calls.Count);
        }

        // ── The KD-U2 drift guard: the two vocabularies must stay in correspondence ───────────────

        [Test]
        public void EveryMatchIntentKind_MapsToACommandKind()
        {
            foreach (IntentKind kind in Enum.GetValues(typeof(IntentKind)))
            {
                if (kind == IntentKind.None) { continue; }

                ManagerCommand command = MatchTacticsDispatcher.ToCommand(MakeIntentWithKind(kind));

                Assert.AreNotEqual(ManagerCommandKind.None, command.Kind,
                    "IntentKind." + kind + " has no ManagerCommand mapping — the intent and command " +
                    "vocabularies have drifted apart.");
            }
        }

        // ── T-UI-DISPATCH-004 — FR-UI-023 marshaling ─────────────────────────────────────────────

        [Test]
        public void LiveDispatch_Enqueues_AndAppliesNothingUntilTheSimThreadDrains()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            var dispatcher = new MatchTacticsDispatcher(session);

            Assert.IsTrue(dispatcher.IsLiveMarshaling);

            dispatcher.Dispatch(ManagerIntent.SetTeamTactic(0, TeamTactic.Balanced));

            // The whole point of FR-UI-023: dispatching did NOT call the seam on this thread.
            Assert.AreEqual(1, session.Commands.Count, "intent is queued, not applied");
            Assert.AreEqual(0, session.Driver.Log.Count, "nothing has been applied yet");

            session.ServiceOnce();   // the sim-thread drain (off-tick form)

            Assert.AreEqual(0, session.Commands.Count, "queue drained");
            Assert.AreEqual(1, session.Driver.Log.Count, "applied exactly once");
            Assert.AreEqual(ManagerCommandKind.SetTeamTactic, session.Driver.Log[0].Command.Kind);
            Assert.AreEqual(0, session.Driver.Log[0].Command.TeamId);
            Assert.AreEqual(0, session.Driver.FailedCommands.Count, "the engine accepted it");
        }

        [Test]
        public void LiveDispatch_PreservesOrderAcrossKinds()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            var dispatcher = new MatchTacticsDispatcher(session);

            dispatcher.Dispatch(ManagerIntent.SetTeamTactic(0, TeamTactic.Balanced));
            dispatcher.Dispatch(ManagerIntent.SetPlayerTactic(3, PlayerTactic.Default(PlayerRole.Default)));
            session.ServiceOnce();

            Assert.AreEqual(2, session.Driver.Log.Count);
            Assert.AreEqual(ManagerCommandKind.SetTeamTactic, session.Driver.Log[0].Command.Kind);
            Assert.AreEqual(ManagerCommandKind.SetPlayerTactic, session.Driver.Log[1].Command.Kind);
            Assert.AreEqual(3, session.Driver.Log[1].Command.AgentId);
        }

        [Test]
        public void LiveDispatch_OfARefusedIntent_ThrowsNowhereAndIsRecordedAsFailed()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            var dispatcher = new MatchTacticsDispatcher(session);

            // A substitution the engine refuses (no distinct squads configured in a neutral demo, and an
            // out-of-range bench index regardless). It must not escape the drain and kill the sim thread.
            dispatcher.Dispatch(ManagerIntent.Substitute(0, 0, 9999, SubstitutionReason.Tactical));
            Assert.DoesNotThrow(() => session.ServiceOnce());

            Assert.AreEqual(0, session.Driver.Log.Count, "a refused command is never logged as applied");
            Assert.AreEqual(1, session.Driver.FailedCommands.Count);
        }

        [Test]
        public void Constructors_RejectNull()
        {
            Assert.Throws<ArgumentNullException>(() => new MatchTacticsDispatcher((MatchSession)null));
            Assert.Throws<ArgumentNullException>(() => new MatchTacticsDispatcher((ILiveMatchMutations)null));
        }

        // Builds an intent carrying an arbitrary kind, including kinds no factory produces, so the
        // unmapped-kind path (F3) is reachable from a test at all.
        private static ManagerIntent MakeIntentWithKind(IntentKind kind)
        {
            switch (kind)
            {
                case IntentKind.SetTeamTactic:
                    return ManagerIntent.SetTeamTactic(0, TeamTactic.Balanced);
                case IntentKind.SetPlayerTactic:
                    return ManagerIntent.SetPlayerTactic(0, PlayerTactic.Default(PlayerRole.Default));
                case IntentKind.Substitute:
                    return ManagerIntent.Substitute(0, 0, 1, SubstitutionReason.Tactical);
                default:
                    return ForceKind(kind);
            }
        }

        // ManagerIntent's factories cannot express an out-of-range kind (by design), so reach for the
        // struct's memory layout to build one. This exists ONLY to prove the default: arm of the
        // dispatcher switch is live; production code can never produce such a value.
        private static ManagerIntent ForceKind(IntentKind kind)
        {
            object boxed = default(ManagerIntent);
            typeof(ManagerIntent)
                .GetField("Kind", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .SetValue(boxed, kind);
            return (ManagerIntent)boxed;
        }

        /// <summary>Records every seam call so a test can assert exactly which seams were touched.</summary>
        private sealed class RecordingMutations : ILiveMatchMutations
        {
            public readonly List<string> Calls = new List<string>();

            public ulong CurrentTick => 0UL;

            public bool MatchEnded => false;

            public void SetTeamTactic(int teamId, in TeamTactic tactic) =>
                Calls.Add("SetTeamTactic(" + teamId + ")");

            public void SetPlayerTactic(int agentId, in PlayerTactic tactic) =>
                Calls.Add("SetPlayerTactic(" + agentId + ")");

            public void SubstitutePlayer(int teamId, int outSlotIndex, int benchIndex, SubstitutionReason reason) =>
                Calls.Add("SubstitutePlayer(" + teamId + "," + outSlotIndex + "," + benchIndex + "," + reason + ")");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation: T-UI-DISPATCH-001..004 — per-seam routing,   |
// |         |            |        | F3 fail-loud (default + unmapped kind), the intent/command     |
// |         |            |        | correspondence drift guard, and the FR-UI-023 marshaling lock  |
// |         |            |        | (enqueue now, apply only on the sim-thread drain).             |
#endregion
