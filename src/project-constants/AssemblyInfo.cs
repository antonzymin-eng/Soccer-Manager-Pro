// File:     src/project-constants/AssemblyInfo.cs
// Created:  2026-06-30
// Modified: 2026-08-08
// Author:   —
// Spec:     Code Standards #20 §3.2.3 (FR-CS-019)
// Purpose:  Exposes internal members (GameplayConfigHolder.ResetForTests) to the test assembly.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.ProjectConstants.Tests")]

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-06-30 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
