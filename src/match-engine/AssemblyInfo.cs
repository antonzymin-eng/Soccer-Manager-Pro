// File:     src/match-engine/AssemblyInfo.cs
// Created:  2026-06-16
// Modified: 2026-06-16
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md), Code Standards #20 FR-CS-015
// Purpose:  Assembly-level attributes for TacticalDirector.MatchEngine.
//           Grants the test assembly access to internal members so the Phase A determinism
//           tests can observe phase wiring without widening the public surface.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.MatchEngine.Tests")]

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-16 | —      | Initial creation (Phase A): InternalsVisibleTo for the test   |
// |         |            |        | assembly, parallel to deterministic-sim / first-touch /       |
// |         |            |        | pass-mechanics / agent-movement.                              |
#endregion
