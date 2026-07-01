// File:     src/project-constants/AssemblyInfo.cs
// Created:  2026-06-30
// Modified: 2026-06-30
// Author:   —
// Spec:     Code Standards #20 §3.2.3 (FR-CS-019)
// Purpose:  Exposes internal members (GameplayConfigHolder.ResetForTests) to the test assembly.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.ProjectConstants.Tests")]
