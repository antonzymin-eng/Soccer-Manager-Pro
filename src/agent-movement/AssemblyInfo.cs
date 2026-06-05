// File:     src/agent-movement/AssemblyInfo.cs
// Created:  2026-06-04
// Modified: 2026-06-04
// Author:   —
// Spec:     Code Standards #20 FR-CS-015, Agent Movement #2 test-plan.md
// Purpose:  Assembly-level attributes for TacticalDirector.AgentMovement.
//           Grants the test assembly access to the internal MovementCommand
//           tooling-override factory used by T-AM-030..032 regression tests.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.AgentMovement.Tests")]
