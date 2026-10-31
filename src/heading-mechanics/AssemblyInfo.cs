// File:     src/heading-mechanics/AssemblyInfo.cs
// Created:  2026-10-10
// Modified: 2026-10-10
// Author:   —
// Spec:     Code Standards #20 FR-CS-015; issue #441 counters (docs/tracking/header-reachability-441-counters.md)
// Purpose:  Assembly-level attributes for TacticalDirector.HeadingMechanics.
//           Grants the match-engine test assembly access to the internal #441 observation hook so it stays
//           off the public surface. No production assembly is a friend.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.MatchEngine.Tests")]

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-10 | —      | #441: test-only friend for the reachability observer.          |
#endregion
