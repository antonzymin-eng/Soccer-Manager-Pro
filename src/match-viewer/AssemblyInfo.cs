// File:     src/match-viewer/AssemblyInfo.cs
// Created:  2026-07-15
// Modified: 2026-07-15
// Author:   —
// Spec:     Interactive match view (docs/tracking/interactive-match-view-design.md), Code Standards #20 FR-CS-015
// Purpose:  Assembly-level attributes for TacticalDirector.MatchViewer. Grants the test assembly
//           access to LiveMatchStreamer's internal TickOnce() seam, so tests can drive a
//           deterministic tick sequence directly (no wall-clock dependency).

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.MatchViewer.Tests")]

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-15 | —      | Initial creation: InternalsVisibleTo for the test assembly, so  |
// |         |            |        | LiveMatchStreamerTests can call TickOnce() directly.            |
#endregion
