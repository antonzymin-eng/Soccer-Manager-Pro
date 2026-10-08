// File:     src/client-app/tests/ClientChangeRequestsTests.cs
// Created:  2026-10-08
// Modified: 2026-10-08
// Author:   —
// Spec:     S0 journey §14.5 / I-Q06–10, Code Standards #20
// Purpose:  Adversarial suffix/payload/cursor and send/refusal/end outcome coverage.

using System;
using NUnit.Framework;
using TacticalDirector.MatchClientCore;
using TacticalDirector.TacticalInstructions;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class ClientChangeRequestsTests
    {
        private sealed class Dispatcher : ICommandDispatcher
        {
            internal int Count;
            internal bool Throw;
            public void Dispatch(in ManagerIntent intent)
            {
                Count++;
                if (Throw)
                    throw new InvalidOperationException("private diagnostics");
            }
        }

        private static readonly TickStampedCommand[] Empty = new TickStampedCommand[0];
        private static ClientChangeRecord Submit(ClientChangeRequests requests, Dispatcher dispatcher)
        {
            TeamTactic t = requests.WithMentality(Mentality.Attacking);
            var c = ManagerCommand.SetTeamTactic(0, t);
            var i = ManagerIntent.SetTeamTactic(0, t);
            return requests.Submit(c, i, default, default, dispatcher);
        }

        [Test]
        public void OnePendingKindDispatchesOnceAndRefusalLeavesCurrentUnchanged()
        {
            var requests = new ClientChangeRequests(TeamTactic.Balanced);
            var d = new Dispatcher();
            var row = Submit(requests, d);
            Assert.Throws<InvalidOperationException>(() => Submit(requests, d));
            Assert.AreEqual(1, d.Count);
            requests.Reconcile(Empty, new[] { new TickStampedCommand(10, row.Command) }, false);
            Assert.AreEqual(ClientChangeStatus.Refused, row.Status);
            Assert.AreEqual(Mentality.Balanced, requests.CurrentMentality);
            Assert.IsFalse(requests.IsMentalityPending);
        }

        [Test]
        public void SendFailureIsPersistentAndDoesNotBecomeEngineRefusal()
        {
            var requests = new ClientChangeRequests(TeamTactic.Balanced);
            var d = new Dispatcher
            {
                Throw = true
            };
            var row = Submit(requests, d);
            requests.Reconcile(Empty, Empty, true);
            Assert.AreEqual(ClientChangeStatus.SendFailure, row.Status);
            Assert.IsFalse(requests.IsMentalityPending);
            Assert.AreEqual(Mentality.Balanced, requests.CurrentMentality);
        }

        [Test]
        public void RepeatedIdenticalRequestCannotReuseOldAppliedSuffix()
        {
            var requests = new ClientChangeRequests(TeamTactic.Balanced);
            var d = new Dispatcher();
            var first = Submit(requests, d);
            var one = new[]
            {
                new TickStampedCommand(10, first.Command)
            };
            requests.Reconcile(one, Empty, false);
            var second = Submit(requests, d);
            requests.Reconcile(one, Empty, false);
            Assert.AreEqual(ClientChangeStatus.Pending, second.Status);
            requests.Reconcile(new[] { one[0], new TickStampedCommand(11, second.Command) }, Empty, true);
            Assert.AreEqual(ClientChangeStatus.Applied, first.Status);
            Assert.AreEqual(ClientChangeStatus.Applied, second.Status);
            Assert.AreEqual(11UL, second.OutcomeTick);
        }

        [Test]
        public void ForeignHomePayloadCannotSettlePendingAndShrinkingEvidenceIsRejected()
        {
            var requests = new ClientChangeRequests(TeamTactic.Balanced);
            var row = Submit(requests, new Dispatcher());
            var different = ManagerCommand.SetTeamTactic(0, TeamTactic.Balanced);
            Assert.Throws<InvalidOperationException>(() => requests.Reconcile(new[] { new TickStampedCommand(10, different) }, Empty, false));
            Assert.AreEqual(ClientChangeStatus.Pending, row.Status);
            Assert.Throws<InvalidOperationException>(() => requests.Reconcile(Empty, Empty, false));
        }

        [Test]
        public void FullTimeOnlyMarksUnmatchedPendingAndClosesFurtherSubmission()
        {
            var requests = new ClientChangeRequests(TeamTactic.Balanced);
            var row = Submit(requests, new Dispatcher());
            requests.Reconcile(Empty, Empty, true);
            Assert.AreEqual(ClientChangeStatus.NotApplied, row.Status);
            Assert.Throws<InvalidOperationException>(() => Submit(requests, new Dispatcher()));
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Adversarial new-evidence and persistent outcome cases. |
#endregion
