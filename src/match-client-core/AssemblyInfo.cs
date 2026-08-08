// File:     src/match-client-core/AssemblyInfo.cs
// Created:  2026-07-24
// Modified: 2026-08-08
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P0), Code Standards #20
// Purpose:  Grants the test assembly access to internal seams (ManagerCommandQueue.DrainInto — the
//           sim-thread-only drain the driver uses, exercised directly in the queue tests).

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.MatchClientCore.Tests")]

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-07-24 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
