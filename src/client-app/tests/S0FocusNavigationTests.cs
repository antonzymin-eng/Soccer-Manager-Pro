// File:     src/client-app/tests/S0FocusNavigationTests.cs
// Created:  2026-10-08
// Modified: 2026-10-08 (explicit Match View order)
// Author:   —
// Spec:     S0 journey §14.3, I-Q15, Code Standards #20
// Purpose:  Permanent regressions for composite pitch traversal, modal/selector eligibility and retained feedback.

using System;
using NUnit.Framework;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class S0FocusNavigationTests
    {
        private readonly struct Target
        {
            internal readonly S0FocusNavigation.Role Role;
            internal readonly int Marker;
            internal readonly bool Available;
            internal Target(S0FocusNavigation.Role role, int marker = -1, bool available = true)
            {
                Role = role;
                Marker = marker;
                Available = available;
            }
        }

        [TestCase(0)]
        [TestCase(7)]
        [TestCase(21)]
        public void PitchIsOneTabStopAndTabMovesOnFromTheInspectedPlayer(int entry)
        {
            var order = new Target[26];
            order[0] = new Target(S0FocusNavigation.Role.Anchor);
            order[1] = new Target(S0FocusNavigation.Role.Action);
            for (int i = 0; i < 22; i++)
                order[i + 2] = new Target(S0FocusNavigation.Role.PitchMarker, i);
            order[24] = new Target(S0FocusNavigation.Role.Anchor);
            order[25] = new Target(S0FocusNavigation.Role.Action);
            Predicate<Target> eligible = t => t.Available && S0FocusNavigation.IsTabStop(t.Role, t.Marker, entry);
            Assert.AreEqual(entry + 2, S0FocusNavigation.Move(order, 1, 1, eligible));
            Assert.AreEqual(25, S0FocusNavigation.Move(order, entry + 2, 1, eligible));
            Assert.AreEqual(entry + 2, S0FocusNavigation.Move(order, 25, -1, eligible));
            Assert.AreEqual(1, S0FocusNavigation.Move(order, entry + 2, -1, eligible));
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void ArrowsReachEveryHomeAndAwayMarkerAndWrap(int step)
        {
            var visible = new bool[22];
            for (int i = 0; i < visible.Length; i++)
                visible[i] = true;
            var reached = new bool[22];
            int current = 0;
            for (int i = 0; i < visible.Length; i++)
            {
                reached[current] = true;
                current = S0FocusNavigation.Move(visible, current, step, v => v);
            }
            Assert.AreEqual(0, current);
            CollectionAssert.AreEqual(visible, reached);
        }

        [TestCase(1, 3)]
        [TestCase(-1, 4)]
        public void RemovedMarkerRecoversToAnAvailableNeighbor(int step, int expected)
        {
            Assert.AreEqual(expected, S0FocusNavigation.Move(new[] { false, false, false, true, true }, 1, step, v => v));
        }

        [Test]
        public void ModalWrapExcludesBackgroundPitchActionsAndAnchors()
        {
            var order = new[]
            {
                new Target(S0FocusNavigation.Role.Action, available: false),
                new Target(S0FocusNavigation.Role.PitchMarker, 0, false),
                new Target(S0FocusNavigation.Role.Anchor),
                new Target(S0FocusNavigation.Role.Action),
                new Target(S0FocusNavigation.Role.Action)
            };
            Predicate<Target> eligible = t => t.Available && S0FocusNavigation.IsTabStop(t.Role, t.Marker, 0);
            Assert.AreEqual(3, S0FocusNavigation.Move(order, 4, 1, eligible));
            Assert.AreEqual(4, S0FocusNavigation.Move(order, 3, -1, eligible));
        }

        [Test]
        public void NoVisiblePitchHasNoTabEntryOrArrowTarget()
        {
            Assert.AreEqual(-1, S0FocusNavigation.FirstAvailable(new[] { false, false }, v => v));
            Assert.AreEqual(-1, S0FocusNavigation.Move(new[] { false, false }, 0, 1, v => v));
            Assert.IsFalse(S0FocusNavigation.IsTabStop(S0FocusNavigation.Role.PitchMarker, 0, -1));
        }

        [Test]
        public void DialogEntrySkipsHiddenOrDisabledSelectors()
        {
            Assert.AreEqual(2, S0FocusNavigation.FirstAvailable(new[] { false, false, true, true }, v => v));
            Assert.AreEqual(-1, S0FocusNavigation.FirstAvailable(Array.Empty<bool>(), v => v));
        }

        [TestCase(1, 6, 0)]
        [TestCase(3, 8, 2)]
        [TestCase(0, 8, -1)]
        public void FeedbackUsesCurrentRecordCountAcrossRepeatMatches(int current, int retained, int expected)
            => Assert.AreEqual(expected, S0FocusNavigation.LatestFeedbackIndex(current, retained));

        [TestCase(1, 0)]
        [TestCase(-1, 5)]
        public void UnboundFeedbackIsAnIntegrationError(int current, int retained)
            => Assert.Throws<ArgumentOutOfRangeException>(() => S0FocusNavigation.LatestFeedbackIndex(current, retained));

        [TestCase(1, 0)]
        [TestCase(-1, 2)]
        public void UnselectedTraversalStartsAtTheAppropriateEnd(int step, int expected)
            => Assert.AreEqual(expected, S0FocusNavigation.Move(new[] { true, false, true }, -1, step, v => v));

        [Test]
        public void EmptyTraversalReturnsNoTarget()
            => Assert.AreEqual(-1, S0FocusNavigation.Move(Array.Empty<bool>(), -1, 1, v => v));

        private sealed class Node
        {
            internal readonly string Name;
            internal bool Available = true;
            internal bool IsTabStop = true;
            internal Node(string name) => Name = name;
            public override string ToString() => Name;
        }

        // Hierarchy mirrors the Match View binding: the earlier-history disclosure lives in the
        // feedback region, which precedes the statistics toggle in the rail.
        private static Node[] MatchHierarchy(out Node[] logical)
        {
            Node heading = new Node("heading") { IsTabStop = false };
            Node slower = new Node("slower"), pause = new Node("pause"), faster = new Node("faster");
            Node pitch = new Node("pitch");
            Node mentality = new Node("mentality"), substitution = new Node("substitution");
            Node feedbackHeading = new Node("feedback heading") { IsTabStop = false };
            Node earlier = new Node("earlier"), row = new Node("feedback row") { IsTabStop = false };
            Node statistics = new Node("statistics"), report = new Node("report") { Available = false };
            logical = new[] { slower, pause, faster, mentality, substitution, statistics, earlier, report, pitch };
            return new[] { heading, slower, pause, faster, pitch, mentality, substitution, feedbackHeading, earlier, row, statistics, report };
        }

        private static string Next(Node[] logical, Node[] hierarchy, string from, int step)
        {
            int index = Array.FindIndex(hierarchy, n => n.Name == from);
            int next = S0FocusNavigation.MoveInLogicalOrder(logical, hierarchy, index, step, n => n.Available && n.IsTabStop);
            return next < 0 ? null : logical[next].Name;
        }

        [Test]
        public void MatchViewTabVisitsStatisticsBeforeEarlierFeedback()
        {
            Node[] hierarchy = MatchHierarchy(out Node[] logical);
            Assert.AreEqual("statistics", Next(logical, hierarchy, "substitution", 1));
            Assert.AreEqual("earlier", Next(logical, hierarchy, "statistics", 1));
            Assert.AreEqual("statistics", Next(logical, hierarchy, "earlier", -1));
            Assert.AreEqual("substitution", Next(logical, hierarchy, "statistics", -1));
        }

        [Test]
        public void MatchViewOrderSkipsUnavailableReportAndEndsAtThePitchEntry()
        {
            Node[] hierarchy = MatchHierarchy(out Node[] logical);
            Assert.AreEqual("pitch", Next(logical, hierarchy, "earlier", 1));
            Assert.AreEqual("slower", Next(logical, hierarchy, "pitch", 1));
            Assert.AreEqual("pitch", Next(logical, hierarchy, "slower", -1));
            Assert.AreEqual("mentality", Next(logical, hierarchy, "faster", 1));
        }

        [Test]
        public void AnchorsEnterTheLogicalOrderAtTheirNextListedSuccessor()
        {
            Node[] hierarchy = MatchHierarchy(out Node[] logical);
            Assert.AreEqual("slower", Next(logical, hierarchy, "heading", 1));
            Assert.AreEqual("pitch", Next(logical, hierarchy, "heading", -1));
            // Submit focuses the feedback row; Tab continues to statistics, Shift-Tab to the prior action.
            Assert.AreEqual("statistics", Next(logical, hierarchy, "feedback row", 1));
            Assert.AreEqual("substitution", Next(logical, hierarchy, "feedback row", -1));
            // Nothing focused: Tab starts at the first available listed target.
            Assert.AreEqual(0, S0FocusNavigation.MoveInLogicalOrder(logical, hierarchy, -1, 1, n => n.Available && n.IsTabStop));
        }

        [Test]
        public void AnchorAfterEveryListedTargetWrapsToTheEnds()
        {
            var logical = new[] { new Node("a"), new Node("b") };
            var trailing = new Node("trailing anchor") { IsTabStop = false };
            var hierarchy = new[] { logical[0], logical[1], trailing };
            Assert.AreEqual(0, S0FocusNavigation.MoveInLogicalOrder(logical, hierarchy, 2, 1, n => n.IsTabStop));
            Assert.AreEqual(1, S0FocusNavigation.MoveInLogicalOrder(logical, hierarchy, 2, -1, n => n.IsTabStop));
            Assert.AreEqual(-1, S0FocusNavigation.MoveInLogicalOrder(Array.Empty<Node>(), hierarchy, 2, 1, n => n.IsTabStop));
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Composite Tab/arrow, visibility/modal, first-selector and repeated-history regressions for the shipping focus policy. |
// | 1.1     | 2026-10-08 | —      | Journey §9.3 Match View order: statistics before earlier feedback, anchor entry and end wrap. |
#endregion
