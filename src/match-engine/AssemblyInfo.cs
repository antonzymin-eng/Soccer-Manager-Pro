// File:     src/match-engine/AssemblyInfo.cs
// Created:  2026-06-16
// Modified: 2026-10-15 (Match Flow §6.4: match-viewer and season-save test friends for the internal SubstitutePlayer)
// Modified (prior): 2026-10-06 (P5b lifecycle/identity)
// Modified (prior): 2026-06-16
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md), Code Standards #20 FR-CS-015
// Purpose:  Assembly-level attributes for TacticalDirector.MatchEngine.
//           Grants the test assembly access to internal members so the Phase A determinism
//           tests can observe phase wiring without widening the public surface.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.MatchEngine.Tests")]
[assembly: InternalsVisibleTo("TacticalDirector.ClientApp.Tests")]
[assembly: InternalsVisibleTo("TacticalDirector.MatchViewer.Tests")]
[assembly: InternalsVisibleTo("TacticalDirector.SeasonSave.Tests")]

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-16 | —      | Initial creation (Phase A): InternalsVisibleTo for the test   |
// |         |            |        | assembly, parallel to deterministic-sim / first-touch /       |
// |         |            |        | pass-mechanics / agent-movement.                              |
// | 1.1     | 2026-10-06 | —      | Client lifecycle tests exercise the existing real end transition; no production friend. |
// | 1.2     | 2026-10-15 | —      | Match Flow §6.4: SubstitutePlayer is internal (test-only immediate swap). The |
// |         |            |        | match-viewer and season-save test assemblies keep their existing swap locks   |
// |         |            |        | (keeper cue; player-identity bookkeeping) through these friends. No production |
// |         |            |        | friend.                                                                       |
#endregion
