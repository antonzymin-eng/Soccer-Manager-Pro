// File:     src/ui-framework/Tests/NavigationShellTests.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     UI / Client Framework #38 §3.2 / §3.5 / §5.1 (T-UI-NAV-001/002/003, FR-UI-009/010/011),
//           Code Standards #20
// Purpose:  Locks the navigation state machine: the §3.5 worked transition reproduced exactly, every
//           fail-loud edge (unregistered navigation, root Pop, un-rooted Current, duplicate register),
//           and the "no Unity host needed" contract — this fixture drives the shell with no engine, no
//           streamer, and no UGUI.

using System;

using NUnit.Framework;

using TacticalDirector.UiFramework;

namespace TacticalDirector.UiFramework.Tests
{
    [TestFixture]
    public sealed class NavigationShellTests
    {
        private static readonly ScreenId MainMenu = new ScreenId(0);
        private static readonly ScreenId MatchView = new ScreenId(1);
        private static readonly ScreenId Tactics = new ScreenId(2);
        private static readonly ScreenId Unregistered = new ScreenId(99);

        private static NavigationShell ThreeScreenShell()
        {
            var shell = new NavigationShell();
            shell.Register(new ScreenRegistration(MainMenu, null, null));
            shell.Register(new ScreenRegistration(MatchView, null, null));
            shell.Register(new ScreenRegistration(Tactics, null, null));
            return shell;
        }

        // T-UI-NAV-001 — the §3.5 worked transition, step for step.
        [Test]
        public void WorkedTransition_MatchesSpecSection35()
        {
            NavigationShell shell = ThreeScreenShell();

            shell.Push(MainMenu);
            Assert.AreEqual(MainMenu, shell.Current, "start: stack=[0]");
            Assert.AreEqual(1, shell.Depth);

            shell.Push(MatchView);
            Assert.AreEqual(MatchView, shell.Current, "Push(1) -> stack=[0,1]");
            Assert.AreEqual(2, shell.Depth);

            shell.Push(Tactics);
            Assert.AreEqual(Tactics, shell.Current, "Push(2) -> stack=[0,1,2]");
            Assert.AreEqual(3, shell.Depth);

            shell.Pop();
            Assert.AreEqual(MatchView, shell.Current, "Pop() -> stack=[0,1]");
            Assert.AreEqual(2, shell.Depth);

            Assert.Throws<ArgumentException>(() => shell.Push(Unregistered), "Push(99) -> throw (F2)");
        }

        // T-UI-NAV-002 — every fail-loud edge.
        [Test]
        public void UnregisteredNavigation_Throws()
        {
            NavigationShell shell = ThreeScreenShell();
            shell.Push(MainMenu);

            Assert.Throws<ArgumentException>(() => shell.Push(Unregistered));
            Assert.Throws<ArgumentException>(() => shell.Replace(Unregistered));
            Assert.Throws<ArgumentException>(() => shell.GetRegistration(Unregistered));

            Assert.AreEqual(MainMenu, shell.Current, "a refused transition leaves the stack untouched");
            Assert.AreEqual(1, shell.Depth);
        }

        [Test]
        public void PopAtRoot_Throws_ShellNeverEmpties()
        {
            NavigationShell shell = ThreeScreenShell();
            shell.Push(MainMenu);

            Assert.Throws<InvalidOperationException>(() => shell.Pop());
            Assert.AreEqual(1, shell.Depth, "the root survives a refused Pop");
            Assert.AreEqual(MainMenu, shell.Current);
        }

        [Test]
        public void PopOnEmptyShell_Throws()
        {
            NavigationShell shell = ThreeScreenShell();
            Assert.Throws<InvalidOperationException>(() => shell.Pop());
        }

        [Test]
        public void CurrentBeforeAnyPush_Throws_NotDefaultScreenZero()
        {
            NavigationShell shell = ThreeScreenShell();

            Assert.AreEqual(0, shell.Depth);
            // The load-bearing part: it must NOT silently return default(ScreenId), which would alias
            // the perfectly real screen id 0 that this shell has registered.
            Assert.Throws<InvalidOperationException>(() => { ScreenId _ = shell.Current; });
        }

        [Test]
        public void ReplaceBeforeAnyPush_Throws()
        {
            NavigationShell shell = ThreeScreenShell();
            Assert.Throws<InvalidOperationException>(() => shell.Replace(MatchView));
        }

        [Test]
        public void Replace_SwapsTopWithoutChangingDepth()
        {
            NavigationShell shell = ThreeScreenShell();
            shell.Push(MainMenu);
            shell.Push(MatchView);

            shell.Replace(Tactics);

            Assert.AreEqual(Tactics, shell.Current);
            Assert.AreEqual(2, shell.Depth, "Replace is not a Push");
            CollectionAssert.AreEqual(new[] { MainMenu, Tactics }, shell.SnapshotStack());
        }

        [Test]
        public void DuplicateRegistration_Throws()
        {
            var shell = new NavigationShell();
            shell.Register(new ScreenRegistration(MainMenu, null, null));

            // Overwriting would swap a live screen's source/dispatcher underneath a stack that still
            // references it. Deviation from the §3.2 pseudocode's assignment, recorded as ERR-038-003.
            Assert.Throws<ArgumentException>(
                () => shell.Register(new ScreenRegistration(MainMenu, null, null)));
            Assert.AreEqual(1, shell.RegisteredCount);
        }

        [Test]
        public void Registration_RoundTripsSourceAndDispatcher()
        {
            var shell = new NavigationShell();
            var source = new StubViewModelSource();
            var dispatcher = new StubDispatcher();
            shell.Register(new ScreenRegistration(MatchView, source, dispatcher));

            ScreenRegistration read = shell.GetRegistration(MatchView);

            Assert.AreSame(source, read.ViewModelSource);
            Assert.AreSame(dispatcher, read.Dispatcher);
            Assert.AreEqual(MatchView, read.Id);
        }

        [Test]
        public void SnapshotStack_IsACopy_CallerCannotMutateTheShell()
        {
            NavigationShell shell = ThreeScreenShell();
            shell.Push(MainMenu);
            shell.Push(MatchView);

            ScreenId[] snapshot = shell.SnapshotStack();
            snapshot[0] = Unregistered;

            CollectionAssert.AreEqual(new[] { MainMenu, MatchView }, shell.SnapshotStack());
        }

        [Test]
        public void ScreenId_HasValueEquality()
        {
            Assert.AreEqual(new ScreenId(7), new ScreenId(7));
            Assert.IsTrue(new ScreenId(7) == new ScreenId(7));
            Assert.IsTrue(new ScreenId(7) != new ScreenId(8));
            Assert.AreEqual(new ScreenId(7).GetHashCode(), new ScreenId(7).GetHashCode());
        }

        private sealed class StubViewModelSource : IViewModelSource<MatchFrameView>
        {
            public MatchFrameView Project() => MatchFrameView.Empty;
        }

        private sealed class StubDispatcher : ICommandDispatcher
        {
            public void Dispatch(in ManagerIntent intent) { }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation: T-UI-NAV-001/002/003 — the §3.5 worked       |
// |         |            |        | transition, fail-loud edges, snapshot-copy and value-equality  |
// |         |            |        | locks. No Unity, no engine, no streamer in this fixture.       |
#endregion
