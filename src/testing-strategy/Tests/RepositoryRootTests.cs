// File:     src/testing-strategy/Tests/RepositoryRootTests.cs
// Created:  2026-10-09
// Modified: 2026-10-09
// Author:   —
// Spec:     Testing Strategy & Framework #19 (test infrastructure), Code Standards #20
// Purpose:  Locks the shared repository-root resolver: the sentinel is required beyond src/ + tools/,
//           the nearest qualifying ancestor wins, bad input returns null rather than throwing, and —
//           non-vacuity — the real test directory resolves to THIS checkout under whichever runner.

using System.IO;

using NUnit.Framework;

namespace TacticalDirector.TestingStrategy.Tests
{
    [TestFixture]
    public sealed class RepositoryRootTests
    {
        private string _scratch;

        [SetUp]
        public void SetUp()
        {
            // Per-test scratch tree outside the checkout, named by the test's own id. A leftover tree
            // from an interrupted run is cleared first.
            _scratch = Path.Combine(Path.GetTempPath(), "td-repo-root-" + TestContext.CurrentContext.Test.ID);
            if (Directory.Exists(_scratch))
            {
                Directory.Delete(_scratch, true);
            }

            Directory.CreateDirectory(_scratch);
        }

        [TearDown]
        public void TearDown()
        {
            if (_scratch != null && Directory.Exists(_scratch))
            {
                Directory.Delete(_scratch, true);
            }
        }

        private static DirectoryInfo MakeCandidate(string path, bool withSentinel)
        {
            Directory.CreateDirectory(Path.Combine(path, "src"));
            Directory.CreateDirectory(Path.Combine(path, "tools"));
            if (withSentinel)
            {
                string sentinel = Path.Combine(path, RepositoryRoot.SENTINEL_RELATIVE_PATH.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(sentinel));
                File.WriteAllText(sentinel, "sentinel");
            }

            return new DirectoryInfo(path);
        }

        [Test]
        public void TheRealTestDirectoryResolvesToThisCheckout()
        {
            DirectoryInfo root = RepositoryRoot.Find(TestContext.CurrentContext.TestDirectory);

            Assert.That(root, Is.Not.Null,
                "could not locate the repo root from " + TestContext.CurrentContext.TestDirectory);
            // Non-vacuity: the resolved root is this repository, not merely some tree with src/ + tools/.
            Assert.That(File.Exists(Path.Combine(root.FullName, "src", "testing-strategy", "RepositoryRoot.cs")), Is.True,
                "resolved root " + root.FullName + " does not contain the resolver's own source");
        }

        [Test]
        public void SrcAndToolsWithoutTheSentinelDoNotQualify()
        {
            DirectoryInfo bare = MakeCandidate(Path.Combine(_scratch, "bare"), withSentinel: false);

            Assert.That(RepositoryRoot.IsRoot(bare), Is.False);
        }

        [Test]
        public void SrcToolsAndTheSentinelQualify()
        {
            DirectoryInfo full = MakeCandidate(Path.Combine(_scratch, "full"), withSentinel: true);

            Assert.That(RepositoryRoot.IsRoot(full), Is.True);
        }

        [Test]
        public void TheNearestQualifyingAncestorWins()
        {
            DirectoryInfo outer = MakeCandidate(Path.Combine(_scratch, "outer"), withSentinel: true);
            DirectoryInfo inner = MakeCandidate(Path.Combine(outer.FullName, "inner"), withSentinel: true);
            string start = Path.Combine(inner.FullName, "src", "some-assembly", "bin");
            Directory.CreateDirectory(start);

            DirectoryInfo root = RepositoryRoot.Find(start);

            Assert.That(root, Is.Not.Null);
            Assert.That(root.FullName, Is.EqualTo(inner.FullName));
        }

        [Test]
        public void AnAncestorWithoutTheSentinelIsSkipped()
        {
            DirectoryInfo outer = MakeCandidate(Path.Combine(_scratch, "outer"), withSentinel: true);
            DirectoryInfo decoy = MakeCandidate(Path.Combine(outer.FullName, "decoy"), withSentinel: false);
            string start = Path.Combine(decoy.FullName, "tools");

            DirectoryInfo root = RepositoryRoot.Find(start);

            Assert.That(root, Is.Not.Null);
            Assert.That(root.FullName, Is.EqualTo(outer.FullName));
        }

        [Test]
        public void MissingOrEmptyStartReturnsNullWithoutThrowing()
        {
            Assert.That(RepositoryRoot.Find(null), Is.Null);
            Assert.That(RepositoryRoot.Find(string.Empty), Is.Null);
            Assert.That(RepositoryRoot.IsRoot(null), Is.False);
            Assert.That(RepositoryRoot.IsRoot(new DirectoryInfo(Path.Combine(_scratch, "does-not-exist"))), Is.False);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-10-09 | —      | Initial creation: sentinel requirement, nearest-ancestor and decoy |
// |         |            |        | cases, null/empty input, and the real-checkout non-vacuity lock.   |
#endregion
