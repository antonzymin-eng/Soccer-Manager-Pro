// File:     src/living-world/AssemblyInfo.cs
// Created:  2026-06-21
// Modified: 2026-08-08
// Author:   —
// Spec:     Living World System #22 §4.3, Code Standards #20 FR-CS-015
// Purpose:  Assembly-level attributes for TacticalDirector.LivingWorld.
//           Grants the test assembly access to internal types for unit testing.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.LivingWorld.Tests")]

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-06-21 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
