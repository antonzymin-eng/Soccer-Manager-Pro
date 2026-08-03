// File:     src/match-viewer/AssemblyInfo.cs
// Created:  2026-07-15
// Modified: 2026-08-03
// Author:   —
// Spec:     Interactive match view (docs/tracking/interactive-match-view-design.md),
//           Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P6/§6.1),
//           Code Standards #20 FR-CS-015
// Purpose:  Assembly-level attributes for TacticalDirector.MatchViewer. Grants the test assembly and
//           the match-client-core composition root access to LiveMatchStreamer's internal TickOnce()
//           seam, so both can drive a deterministic tick sequence directly (no wall-clock dependency).

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.MatchViewer.Tests")]

// P6: MatchSession.TickOnce() — the head-less deterministic advance the §5-P6 closed-loop scenario and
// any log-driven replay drive the composition through — needs the same internal TickOnce() seam the
// pacing loop uses. Routing it through the REAL seam (rather than a parallel tick path in the client)
// is what makes the scenario a proof about the shipping composition: the hook fires, the frame is
// captured, and the auto-pause rule applies exactly as they do under paced playback. The seam stays
// internal to match-viewer, so this widens nothing for the browser viewer or any other consumer, and
// MatchSession re-states the "never concurrently with a running pacing loop" contract as a fail-loud
// guard rather than a comment.
[assembly: InternalsVisibleTo("TacticalDirector.MatchClientCore")]

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-15 | —      | Initial creation: InternalsVisibleTo for the test assembly, so  |
// |         |            |        | LiveMatchStreamerTests can call TickOnce() directly.            |
// | 1.1     | 2026-08-03 | —      | P6: InternalsVisibleTo TacticalDirector.MatchClientCore so      |
// |         |            |        | MatchSession.TickOnce() drives the real TickOnce() seam         |
// |         |            |        | head-lessly instead of a parallel client-side tick path.        |
#endregion
