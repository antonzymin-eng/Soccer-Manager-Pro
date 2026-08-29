// File:     src/discipline/AssemblyInfo.cs
// Created:  2026-08-13
// Modified: 2026-08-13
// Author:   —
// Spec:     Discipline & Suspensions #44 §2.2 / §4.2; Code Standards #20
// Purpose:  Exposes this assembly's internals to its own test assembly so DisciplineState's mutators
//           can be pinned directly. They stay internal in production because DisciplineRules is the
//           sole public mutating entry point (the #41 FR-MD-003 / AdvanceMedicalDay precedent).

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.Discipline.Tests")]

#region VersionHistory
// | Version | Date       | Author | Notes                                                |
// | 1.0     | 2026-08-13 | —      | Initial implementation (#44 T0): InternalsVisibleTo. |
#endregion
