# D-07 six-seed handling census — `main` at `1a3b4271`

**Captured:** October 19, 2026, 00:03–00:24 UTC (17:03–17:24 on October 18 on the host's Pacific
clock).\
**Purpose:** Count how often keepers concede D-07 outside-area handling free kicks on the standard
six W8 seeds, after PR #515 landed, and check whether D-07 moved the adopted post-law B comparison
digests.\
**Source:** merge commit `1a3b4271b5d7a50bbff3a88ef242345c8e49e89b` (PR #515), `src` tree
`c3bb11ffa5ffe11832a2c7a4f74bb0238304df7e`, the same tree as the Unity-validated head `a7800579`.
Built in a clean scratch worktree outside the Unity project.\
**Runtime:** Windows .NET SDK 8.0.425 through the generated `tools/dotnet-ci` projects. This is the
non-certifying shim runtime, not the Unity host.\
**Integrity:** `SHA256SUMS` covers every other file in this directory.

## 1. Result

| Measure, six 90-minute matches | Post-law B baseline (`751b1143`) | Post-D-07 `main` (`1a3b4271`) |
| --- | ---: | ---: |
| Handling free kicks (indirect + direct, both teams) | 0 | **0** |
| Restricted (own-release) hand contacts | 0 | 0 |
| Canonical keeper distributions | 126 | 126 |
| Ordinary fouls / cautions / straight reds | 48 / 8 / 1 | 48 / 8 / 1 |
| Final snapshot digests equal to the adopted B digests | — | **6 of 6** |

**Keepers conceded no D-07 free kick on this corpus.** All six final digests are byte-identical to
the adopted post-law B digests in `w8-b-implementation-results.md`, so D-07 did not change a single
tick of these matches and the adopted B→C comparison digests remain valid for them.

Release positions show why. Across the 126 distributions, the keeper's release point is a median
1.89 m from its own goal line, 8.93 m at the 90th percentile and 12.82 m at most; none is beyond the
16.5 m line, and the largest lateral offset is 8.26 m against a 20.16 m area half-width.

## 2. Limits

- Six matches with zero events bound the rate only loosely: by the rule of three, the 95% upper
  bound is about 3 / 6 = 0.5 D-07 free kicks per match. The census cannot distinguish rates below
  that, and it cannot show that the keeper AI avoids the offence. It shows that these six matches
  never put a keeper's hands on the ball outside the area.
- Keepers still have no area-aware hand avoidance (owner decision D-07 option A). Situations this
  corpus never produced, such as a long rush to the 22 m `RushCommitMaxDistanceM` limit that ends in
  a smother, would be sanctioned.
- The census is the frozen W8 B instrument (`W8BDistributionDiagnosticTests`, `TD_W8_B=1`). Its
  sanction counter records every `RestartAwardedEvent` with kind 5 or 6, which covers both the
  re-handling and D-07 offences. Its attempt counter records only own-release contacts.

## 3. Method

```bash
TD_W8_B=1 dotnet test src/match-engine/tests/match-engine-tests.gen.csproj \
  --filter "FullyQualifiedName~W8BDistributionDiagnostic_ReportsFrozenCorpusAndLongKickLandings" \
  --logger "trx;LogFileName=d07count.trx"
```

Seeds: `0F1E2D3C4B5A6978`, `D1A6D05E`, `1`, `ABCDEF12`, `99887766`, `5A5A5A5A`, each for 324,000
frames, with `EnableGkHeading()`. The two composed LongKick fixtures in the same test are excluded
from every corpus figure above. The test passed in 20 min 47 s.

| File | What it is |
| --- | --- |
| `census-rows.txt.xz` | The census rows from the test's standard output (engine `[Log]`/`[Warning]` lines removed): `handlingSummary`, `bSummary` with digests, and per-distribution `commit`/`contact`/`landing`/`passOutcome`/`terminal` rows |
| `w8b-diagnostic-results.trx.xz` | The complete VSTest TRX, including the unfiltered standard output |

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 18, 2026 | Initial post-D-07 six-seed census at `1a3b4271`. |
