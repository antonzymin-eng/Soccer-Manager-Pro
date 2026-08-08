// File:     src/decision-tree/AssemblyInfo.cs
// Created:  2026-05-29
// Modified: 2026-08-08
// Author:   —
// Spec:     Code Standards #20 FR-CS-015
// Purpose:  Assembly-level attributes for TacticalDirector.DecisionTree.
//           Grants the test assembly access to internal types for unit testing.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.DecisionTree.Tests")]

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-05-29 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
