// File:     src/match-client-core/AssemblyInfo.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P0), Code Standards #20
// Purpose:  Grants the test assembly access to internal seams (ManagerCommandQueue.DrainInto — the
//           sim-thread-only drain the driver uses, exercised directly in the queue tests).

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.MatchClientCore.Tests")]
