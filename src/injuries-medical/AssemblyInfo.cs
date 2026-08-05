// File:     src/injuries-medical/AssemblyInfo.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Injuries & Medical #41 §3.1/§3.4; Code Standards #20
// Purpose:  Exposes this assembly's internals to its own test assembly so the keyed occurrence draw
//           and the deterministic robustness term can be pinned directly. They stay internal in
//           production because AdvanceMedicalDay is the sole mutating entry point (FR-MD-003).

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.InjuriesMedical.Tests")]

#region VersionHistory
// | Version | Date       | Author | Notes                                              |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#41 T0): InternalsVisibleTo. |
#endregion
