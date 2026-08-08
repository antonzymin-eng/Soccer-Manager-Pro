// File:     src/pass-mechanics/AssemblyInfo.cs
// Created:  2026-06-01
// Modified: 2026-08-08
// Author:   —
// Spec:     Pass Mechanics #5, Code Standards #20 FR-CS-015
// Purpose:  Assembly-level attributes for TacticalDirector.PassMechanics.
//           Grants the test assembly access to internal types for unit testing.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.PassMechanics.Tests")]

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-06-01 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
