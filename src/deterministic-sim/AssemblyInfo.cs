// File:     src/deterministic-sim/AssemblyInfo.cs
// Created:  2026-06-12
// Modified: 2026-06-12
// Author:   —
// Spec:     Deterministic Simulation #16, Code Standards #20 FR-CS-015
// Purpose:  Assembly-level attributes for TacticalDirector.DeterministicSim.
//           Grants the test assembly access to internal members so the golden-vector KAT
//           suites can drive HkdfSha256/HkdfExtract/HkdfExpand and SipHash24_64 directly.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.DeterministicSim.Tests")]

#region VersionHistory
// | Version | Date       | Author | Notes                                                                |
// | 1.0     | 2026-06-12 | —      | Initial creation (golden-vector pass): InternalsVisibleTo for the   |
// |         |            |        | test assembly, parallel to first-touch / pass-mechanics /           |
// |         |            |        | agent-movement. Without it the existing DeterministicSimTests calls |
// |         |            |        | to internal HkdfSha256/SipHash24_64 could never have compiled.      |
#endregion
