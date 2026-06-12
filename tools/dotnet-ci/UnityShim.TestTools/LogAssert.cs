// File:     tools/dotnet-ci/UnityShim.TestTools/LogAssert.cs
// Created:  2026-06-12
// Purpose:  UnityEngine.TestTools.LogAssert shim with Unity Test Framework
//           parity semantics for the non-certifying Linux compile/test gate:
//             1. Expect(type, message) — a matching log of that type is
//                consumed silently; an expectation never satisfied by test end
//                FAILS the test (Unity: "Expected log did not appear").
//             2. An Error/Assert/Exception log with no pending expectation
//                FAILS the test unless ignoreFailingMessages is set (Unity
//                editmode-runner behavior).
//           Verification fires from LogAssertVerifyAttribute (assembly-level
//           NUnit ITestAction injected into every generated test project by
//           tools/dotnet-ci/generate_projects.py).
//           Match policy: any pending expectation may match (not strictly
//           FIFO) — Unity's runner does not require cross-type ordering and
//           strict FIFO would be flakier than the engine.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace UnityEngine.TestTools
{
    public static class LogAssert
    {
        private sealed class Expectation
        {
            public LogType Type;
            public string Literal;   // exact-match form
            public Regex Pattern;    // regex form
            public string Describe() => $"[{Type}] {(Literal ?? Pattern?.ToString() ?? "<null>")}";
            public bool Matches(LogType type, string message)
            {
                if (type != Type) return false;
                if (Pattern != null) return Pattern.IsMatch(message ?? string.Empty);
                return string.Equals(Literal, message, StringComparison.Ordinal);
            }
        }

        private static readonly List<Expectation> s_pending = new List<Expectation>();
        private static readonly List<string> s_unexpected = new List<string>();
        private static bool s_hooked;

        /// <summary>Unity API: when true, unexpected failing messages do not fail the test.</summary>
        public static bool ignoreFailingMessages { get; set; }

        public static void Expect(LogType type, string message)
        {
            EnsureHooked();
            s_pending.Add(new Expectation { Type = type, Literal = message });
        }

        public static void Expect(LogType type, Regex message)
        {
            EnsureHooked();
            s_pending.Add(new Expectation { Type = type, Pattern = message });
        }

        /// <summary>Unity API: assert immediately that no unexpected failing logs were received so far.</summary>
        public static void NoUnexpectedReceived()
        {
            if (s_unexpected.Count > 0)
            {
                string details = string.Join("\n  ", s_unexpected);
                s_unexpected.Clear();
                throw new AssertionException($"Unexpected log message(s) received:\n  {details}");
            }
        }

        internal static void Reset()
        {
            EnsureHooked();
            s_pending.Clear();
            s_unexpected.Clear();
            ignoreFailingMessages = false;
        }

        internal static void VerifyAtTestEnd()
        {
            try
            {
                if (s_unexpected.Count > 0)
                {
                    string details = string.Join("\n  ", s_unexpected);
                    throw new AssertionException($"Unexpected log message(s) received during test:\n  {details}");
                }
                if (s_pending.Count > 0)
                {
                    string details = string.Join("\n  ", s_pending.ConvertAll(e => e.Describe()));
                    throw new AssertionException($"Expected log message(s) did not appear:\n  {details}");
                }
            }
            finally
            {
                s_pending.Clear();
                s_unexpected.Clear();
            }
        }

        private static void EnsureHooked()
        {
            if (s_hooked) return;
            s_hooked = true;
            ShimLog.MessageLogged += OnMessage;
        }

        private static void OnMessage(LogType type, string message)
        {
            for (int i = 0; i < s_pending.Count; i++)
            {
                if (s_pending[i].Matches(type, message))
                {
                    s_pending.RemoveAt(i);
                    return;
                }
            }
            bool failing = type == LogType.Error || type == LogType.Assert || type == LogType.Exception;
            if (failing && !ignoreFailingMessages)
                s_unexpected.Add($"[{type}] {message}");
        }
    }
}
