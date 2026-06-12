// File:     tools/dotnet-ci/LogAssertVerifyAssemblyInfo.cs
// Created:  2026-06-12
// Purpose:  Linked into every generated test project by generate_projects.py.
//           Applies the Unity-parity log contract (LogAssert reset/verify
//           around each test) to all tests in the assembly.

[assembly: UnityEngine.TestTools.LogAssertVerify]
