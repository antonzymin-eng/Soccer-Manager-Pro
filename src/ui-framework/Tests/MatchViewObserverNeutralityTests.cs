// File:     src/ui-framework/Tests/MatchViewObserverNeutralityTests.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     UI / Client Framework #38 §5.1 (T-UI-NEU-001, T-UI-LAYER-001, FR-UI-001/017),
//           Code Standards #20
// Purpose:  The two locks that keep the presentation layer honest: watching a match through the UI is
//           byte-identical to not watching it (digest chains compared tick for tick), and no sim, loop,
//           or analytics assembly references the UI assembly.

using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;
using TacticalDirector.UiFramework;

namespace TacticalDirector.UiFramework.Tests
{
    [TestFixture]
    public sealed class MatchViewObserverNeutralityTests
    {
        private const ulong Seed = 0x5EED0B5E12345678UL;
        private const int Ticks = 240;   // 4 s at 60 Hz — past kickoff and into real play

        // ── T-UI-NEU-001 — observer neutrality ───────────────────────────────────────────────────

        [Test]
        public void ObservingThroughTheViewModel_IsByteIdenticalToNotObserving()
        {
            List<string> unobserved = RunUnobserved(Seed, Ticks);
            List<string> observed = RunObserved(Seed, Ticks);

            Assert.AreEqual(unobserved.Count, observed.Count);
            for (int i = 0; i < unobserved.Count; i++)
            {
                Assert.AreEqual(unobserved[i], observed[i],
                    "digest diverged at tick " + i + ": projecting the view perturbed the simulation");
            }
        }

        [Test]
        public void ObservedRun_ActuallyProjectedSomething()
        {
            // Guards the test above against passing vacuously: if projection silently produced nothing,
            // "identical digests" would prove nothing at all.
            var engine = new MatchEngine.MatchEngine(Seed);
            var frames = new MatchViewProjectionTests.StubFrameSource();
            var source = new MatchViewModelSource(frames);

            engine.RunTick();
            frames.Publish(MatchViewProjectionTests.CaptureFrom(engine));
            MatchFrameView view = source.Project();

            Assert.IsFalse(view.IsEmpty);
            Assert.AreEqual(MatchEngineConstants.SQUAD_SIZE, view.AgentPositions.Count);
            Assert.AreEqual(engine.CurrentTick, view.Tick);
        }

        private static List<string> RunUnobserved(ulong seed, int ticks)
        {
            var engine = new MatchEngine.MatchEngine(seed);
            var digests = new List<string>(ticks);
            for (int i = 0; i < ticks; i++)
            {
                engine.RunTick();
                digests.Add(Hex(engine.CurrentSnapshotDigest));
            }
            return digests;
        }

        private static List<string> RunObserved(ulong seed, int ticks)
        {
            var engine = new MatchEngine.MatchEngine(seed);
            var frames = new MatchViewProjectionTests.StubFrameSource();
            var source = new MatchViewModelSource(frames);
            var digests = new List<string>(ticks);

            for (int i = 0; i < ticks; i++)
            {
                engine.RunTick();
                digests.Add(Hex(engine.CurrentSnapshotDigest));

                // The presentation loop: sample the engine's public observation surface, publish, and
                // project — every tick, which is heavier than any real render cadence would be.
                frames.Publish(MatchViewProjectionTests.CaptureFrom(engine));
                source.Project();
            }
            return digests;
        }

        private static string Hex(byte[] bytes)
        {
            var chars = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i * 2]     = "0123456789abcdef"[bytes[i] >> 4];
                chars[i * 2 + 1] = "0123456789abcdef"[bytes[i] & 0xF];
            }
            return new string(chars);
        }

        // ── T-UI-LAYER-001 — no reverse reference ────────────────────────────────────────────────

        [Test]
        public void NoOtherAssemblyReferencesTheUiFramework()
        {
            DirectoryInfo root = FindRepoRoot();
            Assert.IsNotNull(root,
                "could not locate the repo root from " + AppContext.BaseDirectory +
                " — this lock must fail loud rather than scan nothing and pass.");

            FileInfo[] asmdefs = new DirectoryInfo(Path.Combine(root.FullName, "src"))
                .GetFiles("*.asmdef", SearchOption.AllDirectories);

            // A mis-resolved root would yield a handful of files and a vacuous pass.
            Assert.Greater(asmdefs.Length, 20, "scanned implausibly few asmdefs — root resolution is wrong");

            var offenders = new List<string>();
            foreach (FileInfo asmdef in asmdefs)
            {
                string name = Path.GetFileNameWithoutExtension(asmdef.Name);
                // The UI assembly and its own test assembly are allowed to reference it.
                if (name == "ui-framework" || name == "ui-framework-tests") { continue; }

                if (File.ReadAllText(asmdef.FullName).Contains("TacticalDirector.UiFramework"))
                {
                    offenders.Add(asmdef.FullName.Substring(root.FullName.Length + 1));
                }
            }

            CollectionAssert.IsEmpty(offenders,
                "sim/loop/analytics assemblies must never reference the presentation layer (FR-UI-001)");
        }

        private static DirectoryInfo FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "src")) &&
                    Directory.Exists(Path.Combine(dir.FullName, "tools")))
                {
                    return dir;
                }
                dir = dir.Parent;
            }
            return null;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation: T-UI-NEU-001 digest-chain neutrality (with a |
// |         |            |        | non-vacuity guard) and T-UI-LAYER-001 reverse-reference scan   |
// |         |            |        | with fail-loud root resolution + a scanned-count floor.        |
#endregion
