// Created 2026-11-13. Read-only frozen-lane W13 identity instrument.
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using TacticalDirector.MatchEngine;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            ulong seed = ulong.Parse(args[0], NumberStyles.HexNumber);
            int frames = int.Parse(args[1]);
            string output = args[2];
            Directory.CreateDirectory(output);
            using var log = new StreamWriter(Path.Combine(output, "engine.log"));
            TextWriter original = Console.Out;
            Console.SetOut(log);
            var engine = new MatchEngine(seed);
            engine.EnableGkHeading();
            var flag = typeof(MatchEngineConstants).GetField("MarkMovementEnabled");
            string marking = flag == null ? "absent" : flag.GetValue(null).ToString();
            if (marking != "absent" && marking != "False") throw new Exception("mark movement must be off");
            using var digests = new GZipStream(File.Create(Path.Combine(output, "digests.bin.gz")), CompressionLevel.Fastest);
            using var all = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using var checkpoints = new StreamWriter(Path.Combine(output, "checkpoints.txt"));
            var timer = System.Diagnostics.Stopwatch.StartNew();
            byte[] terminal = null;
            for (int frame = 1; frame <= frames; frame++)
            {
                engine.RunTick();
                terminal = engine.CurrentSnapshotDigest;
                if (terminal.Length != 32 || engine.CurrentTick != (ulong)frame) throw new Exception("digest/tick integrity");
                all.AppendData(terminal);
                digests.Write(terminal, 0, terminal.Length);
                if (frame % 6000 == 0) checkpoints.WriteLine(frame + " " + Convert.ToHexString(terminal).ToLowerInvariant());
                if (frame % 18000 == 0)
                {
                    log.Flush(); checkpoints.Flush();
                    File.WriteAllText(Path.Combine(output, "progress.json"), JsonSerializer.Serialize(new { frame, elapsedSeconds = timer.Elapsed.TotalSeconds }));
                }
            }
            var summary = new {
                seed = seed.ToString("X16"), frames, tick = engine.CurrentTick, markMovementEnabled = marking,
                terminal = Convert.ToHexString(terminal).ToLowerInvariant(),
                allTicks = Convert.ToHexString(all.GetHashAndReset()).ToLowerInvariant(),
                elapsedSeconds = timer.Elapsed.TotalSeconds, runtime = Environment.Version.ToString(),
                sdk = "8.0.426", configuration = "Debug", processorCount = Environment.ProcessorCount,
                buildHash = MatchEngineBuildIdentity.BuildHash
            };
            File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
            Console.SetOut(original); Console.WriteLine(JsonSerializer.Serialize(summary));
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
