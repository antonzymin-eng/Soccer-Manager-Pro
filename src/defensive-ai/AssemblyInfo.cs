// File:     src/defensive-ai/AssemblyInfo.cs
// Created:  2026-11-10
// Modified: 2026-11-10
// Author:   —
// Spec:     Defensive AI #14, Code Standards #20 FR-CS-015
// Purpose:  Assembly-level attributes for TacticalDirector.DefensiveAI.
//           Grants the test assembly access to internal members so the ThreatScore compatibility shim
//           and the generic two-orientation geometry helpers can stay internal (W13.0 review L1/M1).

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.DefensiveAI.Tests")]

#region VersionHistory
// | Version | Date       | Author | Notes                                                                      |
// | 1.0     | 2026-11-10 | —      | Initial file (W13.0 review): InternalsVisibleTo the Defensive AI test assembly. |
#endregion
