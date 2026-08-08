// File:     src/agent-movement/AssemblyInfo.cs
// Created:  2026-06-04
// Modified: 2026-08-08
// Author:   —
// Spec:     Code Standards #20 FR-CS-015, Agent Movement #2 test-plan.md
// Purpose:  Assembly-level attributes for TacticalDirector.AgentMovement.
//           Grants the test assembly access to the internal MovementCommand
//           tooling-override factory used by T-AM-030..032 regression tests.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.AgentMovement.Tests")]

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-06-04 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
