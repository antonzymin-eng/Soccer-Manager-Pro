// File:     tools/dotnet-ci/UnityShim/Profiling.cs
// Created:  2026-06-12
// Purpose:  No-op shims for UnityEngine.Profiling.Profiler and
//           Unity.Profiling.ProfilerMarker so instrumented hot paths compile
//           on the non-certifying Linux gate. Profiling data is a Unity-editor
//           concern (Spec #18); these stubs intentionally do nothing.

using System;

namespace UnityEngine.Profiling
{
    public static class Profiler
    {
        public static void BeginSample(string name) { }
        public static void EndSample() { }
    }
}

namespace Unity.Profiling
{
    public readonly struct ProfilerMarker
    {
        public ProfilerMarker(string name) { }

        public AutoScope Auto() => default;
        public void Begin() { }
        public void End() { }

        public readonly struct AutoScope : IDisposable
        {
            public void Dispose() { }
        }
    }
}
