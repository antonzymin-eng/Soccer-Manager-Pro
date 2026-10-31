// File:     src/testing-strategy/RepositoryRoot.cs
// Created:  2026-10-09
// Modified: 2026-10-09
// Author:   —
// Spec:     Testing Strategy & Framework #19 (test infrastructure), Code Standards #20
// Purpose:  The one repository-root resolver for structural test locks — tests that read asmdefs or
//           source files from the checkout rather than reflecting over loaded assemblies. It exists
//           because four such locks each walked up from AppContext.BaseDirectory, which under Unity's
//           test runner is the Editor install, so on the governing build they could never pass.

using System.IO;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Resolves this repository's checkout root by walking up from a caller-supplied start directory.
    /// Callers pass the test assembly's directory (<c>TestContext.CurrentContext.TestDirectory</c>).
    /// That anchor is inside the checkout under both runners: the Unity Editor (observed on the
    /// pinned 6000.4.9f1 host, October 9, 2026) and the Linux <c>dotnet-ci</c> shim.
    /// <c>AppContext.BaseDirectory</c> is not, because under Unity it is the Editor install.
    /// <para>
    /// The resolver never falls back to the process working directory or any other host-dependent
    /// guess. A structural lock that silently scanned the wrong tree would be worse than one that
    /// fails. A directory qualifies only if it holds <c>src/</c>, <c>tools/</c> and the
    /// repository-specific sentinel <see cref="SENTINEL_RELATIVE_PATH"/>. <c>src/</c> + <c>tools/</c>
    /// alone is common to many checkouts. Callers keep their own non-vacuity floor, such as a
    /// minimum scanned-file count.
    /// </para>
    /// </summary>
    public static class RepositoryRoot
    {
        /// <summary>[FIXED] The repository-specific file a candidate root must contain, relative to it.</summary>
        public const string SENTINEL_RELATIVE_PATH = "docs/specs/SPEC_INDEX.md";

        /// <summary>
        /// Returns the nearest ancestor of <paramref name="startDirectory"/> (inclusive) that is this
        /// repository's root, or <c>null</c> when none is. Never throws for a missing or empty start
        /// directory; the caller's assertion is what fails loud.
        /// </summary>
        public static DirectoryInfo Find(string startDirectory)
        {
            if (string.IsNullOrEmpty(startDirectory))
            {
                return null;
            }

            DirectoryInfo dir = new DirectoryInfo(startDirectory);
            while (dir != null)
            {
                if (IsRoot(dir))
                {
                    return dir;
                }

                dir = dir.Parent;
            }

            return null;
        }

        /// <summary>True when <paramref name="dir"/> holds <c>src/</c>, <c>tools/</c> and the sentinel.</summary>
        public static bool IsRoot(DirectoryInfo dir)
        {
            if (dir == null || !dir.Exists)
            {
                return false;
            }

            return Directory.Exists(Path.Combine(dir.FullName, "src"))
                && Directory.Exists(Path.Combine(dir.FullName, "tools"))
                && File.Exists(Path.Combine(dir.FullName, SENTINEL_RELATIVE_PATH.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-10-09 | —      | Initial creation: shared TestDirectory-anchored resolver with the  |
// |         |            |        | SPEC_INDEX.md sentinel; replaces four private AppContext.          |
// |         |            |        | BaseDirectory walks that could not reach the repo under Unity.     |
#endregion
