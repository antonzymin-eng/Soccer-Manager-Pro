// File:     tools/dotnet-ci/UnityShim/Mathf.cs
// Created:  2026-06-12
// Purpose:  UnityEngine.Mathf shim for the non-certifying Linux compile/test
//           gate. Semantics replicate Unity's documented behavior EXACTLY where
//           the codebase is known to depend on it:
//             - Max/Min/Clamp/Clamp01 use raw comparisons, so NaN propagates the
//               same way it does in Unity (the project NaN-gate pattern — see
//               Pass Mechanics AR-9 M-2, First Touch AR-8 — was derived from
//               these exact semantics).
//             - RoundToInt uses round-half-to-even (System.Math.Round default),
//               matching Unity's documented "even number is returned" rule.

using System;

namespace UnityEngine
{
    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public const float Infinity = float.PositiveInfinity;
        public const float NegativeInfinity = float.NegativeInfinity;
        public const float Deg2Rad = PI * 2f / 360f;
        public const float Rad2Deg = 1f / Deg2Rad;
        public static readonly float Epsilon = float.Epsilon;

        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Cos(float f) => (float)Math.Cos(f);
        public static float Tan(float f) => (float)Math.Tan(f);
        public static float Asin(float f) => (float)Math.Asin(f);
        public static float Acos(float f) => (float)Math.Acos(f);
        public static float Atan(float f) => (float)Math.Atan(f);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Sqrt(float f) => (float)Math.Sqrt(f);
        public static float Abs(float f) => Math.Abs(f);
        public static int Abs(int value) => Math.Abs(value);
        public static float Pow(float f, float p) => (float)Math.Pow(f, p);
        public static float Exp(float power) => (float)Math.Exp(power);
        public static float Log(float f) => (float)Math.Log(f);
        public static float Log(float f, float p) => (float)Math.Log(f, p);
        public static float Log10(float f) => (float)Math.Log10(f);

        // Unity semantics: comparison-based, NOT Math.Min/Max — Max(a, NaN)
        // returns NaN, Max(NaN, b) returns b. Do not "fix" this asymmetry; the
        // codebase's NaN gates are written against it.
        public static float Min(float a, float b) => a < b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;

        public static float Min(params float[] values)
        {
            if (values.Length == 0) return 0f;
            float m = values[0];
            for (int i = 1; i < values.Length; i++) if (values[i] < m) m = values[i];
            return m;
        }

        public static float Max(params float[] values)
        {
            if (values.Length == 0) return 0f;
            float m = values[0];
            for (int i = 1; i < values.Length; i++) if (values[i] > m) m = values[i];
            return m;
        }

        public static float Ceil(float f) => (float)Math.Ceiling(f);
        public static float Floor(float f) => (float)Math.Floor(f);
        public static float Round(float f) => (float)Math.Round(f);
        public static int CeilToInt(float f) => (int)Math.Ceiling(f);
        public static int FloorToInt(float f) => (int)Math.Floor(f);
        public static int RoundToInt(float f) => (int)Math.Round(f);

        public static float Sign(float f) => f >= 0f ? 1f : -1f;

        public static float Clamp(float value, float min, float max)
        {
            if (value < min) value = min;
            else if (value > max) value = max;
            return value;
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min) value = min;
            else if (value > max) value = max;
            return value;
        }

        // NaN passes through (all comparisons false) — Unity-exact.
        public static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;

        public static float InverseLerp(float a, float b, float value)
            => a != b ? Clamp01((value - a) / (b - a)) : 0f;

        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Abs(target - current) <= maxDelta) return target;
            return current + Sign(target - current) * maxDelta;
        }

        public static bool Approximately(float a, float b)
            => Abs(b - a) < Max(1E-06f * Max(Abs(a), Abs(b)), Epsilon * 8f);

        public static float Repeat(float t, float length)
            => Clamp(t - Floor(t / length) * length, 0f, length);

        public static float DeltaAngle(float current, float target)
        {
            float delta = Repeat(target - current, 360f);
            if (delta > 180f) delta -= 360f;
            return delta;
        }
    }
}
