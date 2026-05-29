// File:     src/decision-tree/AssemblyInfo.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Code Standards #20 FR-CS-015
// Purpose:  Assembly-level attributes for TacticalDirector.DecisionTree.
//           Grants the test assembly access to internal types for unit testing.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.DecisionTree.Tests")]
