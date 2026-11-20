// Physical-trajectory fingerprint: ball and agent position/velocity and the score only. It does not read any
// other serialized engine state (Heading attempt state, the KD-7 trigger latch, ...), so it cannot say which
// snapshot fields differ between two trees.
using System; using System.IO; using System.Security.Cryptography;
using TacticalDirector.MatchEngine;
static class P {
  static void Main() {
    ulong[] seeds = {0x0F1E2D3C4B5A6978UL,0xD1A6D05EUL,0x1UL,0xABCDEF12UL,0x99887766UL,0x5A5A5A5AUL};
    foreach (var seed in seeds) {
      var e = new MatchEngine(seed); e.EnableGkHeading();
      using var ms = new MemoryStream(); var w = new BinaryWriter(ms);
      while (!e.MatchEnded && e.CurrentTick < 324000) {
        e.RunTick();
        var b = e.BallView; w.Write(b.Position.x); w.Write(b.Position.y); w.Write(b.Position.z);
        w.Write(b.Velocity.x); w.Write(b.Velocity.y); w.Write(b.Velocity.z);
        for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++) { var a = e.AgentView(i); w.Write(a.Position.x); w.Write(a.Position.y); w.Write(a.Velocity.x); w.Write(a.Velocity.y); }
      }
      w.Flush();
      Console.WriteLine($"0x{seed:X16} tick={e.CurrentTick} score={e.HomeScore}-{e.AwayScore} physical={Convert.ToHexString(SHA256.HashData(ms.ToArray()))[..32]}");
    }
  }
}
