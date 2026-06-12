// File:     tools/dotnet-ci/UnityShim/Debug.cs
// Created:  2026-06-12
// Purpose:  UnityEngine.Debug + LogType shim for the non-certifying Linux
//           compile/test gate. All messages flow through ShimLog so that
//           UnityShim.TestTools' LogAssert can observe them (mirroring the
//           Unity Test Framework's log-scrubbing seam) without UnityShim
//           itself depending on NUnit.

using System;

namespace UnityEngine
{
    public enum LogType
    {
        Error = 0,
        Assert = 1,
        Warning = 2,
        Log = 3,
        Exception = 4
    }

    /// <summary>
    /// Internal log spine. UnityShim.TestTools subscribes via
    /// <see cref="MessageLogged"/> to implement LogAssert semantics.
    /// </summary>
    public static class ShimLog
    {
        /// <summary>Raised for every Debug.* emission. Test-framework seam.</summary>
        public static event Action<LogType, string> MessageLogged;

        public static void Emit(LogType type, string message)
        {
            Console.WriteLine($"[{type}] {message}");
            MessageLogged?.Invoke(type, message);
        }
    }

    public static class Debug
    {
        public static void Log(object message) => ShimLog.Emit(LogType.Log, message?.ToString() ?? "Null");
        public static void LogWarning(object message) => ShimLog.Emit(LogType.Warning, message?.ToString() ?? "Null");
        public static void LogError(object message) => ShimLog.Emit(LogType.Error, message?.ToString() ?? "Null");
        public static void LogException(Exception exception) => ShimLog.Emit(LogType.Exception, exception?.ToString() ?? "Null");

        public static void Assert(bool condition)
        {
            if (!condition) ShimLog.Emit(LogType.Assert, "Assertion failed");
        }

        public static void Assert(bool condition, object message)
        {
            if (!condition) ShimLog.Emit(LogType.Assert, message?.ToString() ?? "Assertion failed");
        }

        public static void AssertFormat(bool condition, string format, params object[] args)
        {
            if (!condition) ShimLog.Emit(LogType.Assert, string.Format(format, args));
        }

        public static void LogFormat(string format, params object[] args)
            => ShimLog.Emit(LogType.Log, string.Format(format, args));

        public static void LogWarningFormat(string format, params object[] args)
            => ShimLog.Emit(LogType.Warning, string.Format(format, args));

        public static void LogErrorFormat(string format, params object[] args)
            => ShimLog.Emit(LogType.Error, string.Format(format, args));
    }
}
