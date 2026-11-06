# Career drift measurement — club-strength ramp and regen quality (ERR-028-026)

**Captured:** November 4, 2026, Linux .NET 8.0.133 (non-certifying gate lane).\
**Purpose:** replace ERR-028-026's estimated "about 18 seasons" erosion figure with a measurement, at the
owner's request, before D-13 decides the regen fix. Open issue: [`regen-club-strength`](../../open-issues.md#regen-club-strength).\
**Instrument:** `src/season-save/tests/CareerDriftDiagnosticTests.cs` v1.0, catalogued as `career-drift`
(`tools/dotnet-ci/measurement_catalog.py`). Command:
`TD_CAREER_DRIFT=1 TD_CAREER_DRIFT_SEASONS=40 dotnet test src/season-save/tests/season-save-tests.gen.csproj --filter FullyQualifiedName~CareerDriftDiagnostic`.\
**Source:** the commit that adds this directory (parent `446b7ce`); no production file differs from the parent.\
**Composition:** `CareerSession.StartNew`'s wiring restated in the test (20-club `LeagueBootstrap`, #28
progression store, career states with injury occurrence on, #44 discipline), `RoundResolutionMode.QuickSimAll`,
five world seeds `0x5EED0001`–`0x5EED0005`, 40 seasons each. Rating = `SquadRating.StartingElevenMean`, the
quick-sim's own input, on the `[1,20]` attribute scale.\
**Determinism:** the default 5 × 20 run reproduced seasons 0–20 of this 5 × 40 run byte for byte.\
**Integrity:** `SHA256SUMS` covers `README.md` and `report-5x40.txt`.

## Result (means over five seeds)

| Season | Ramp gap, top-5 minus bottom-5 genesis clubs | Share of genesis gap left | Rating SD across clubs | Spearman vs genesis order | Regen share of players |
|---:|---:|---:|---:|---:|---:|
| 0 | 4.78 | 100% | 1.85 | 1.00 | 0.00 |
| 3 | 4.16 | 87% | 1.63 | 0.99 | 0.09 |
| 5 | 3.77 | 79% | 1.50 | 0.98 | 0.16 |
| 10 | 2.70 | 56% | 1.14 | 0.95 | 0.39 |
| 15 | 1.76 | 37% | 0.80 | 0.90 | 0.63 |
| 20 | 0.81 | 17% | 0.43 | 0.77 | 0.87 |
| 24 | 0.07 | ~0% | 0.12 | 0.18 | 0.99 |
| 40 | 0.01 | ~0% | 0.10 | −0.05 | 1.00 |

- **Erosion is roughly linear with squad turnover.** About 4–5% of the genesis gap is lost per season.
  Half is gone after about 11 seasons; the ramp is gone (noise floor, SD ≈ 0.10) after about 23–24 seasons,
  when the genesis players have all left. ERR-028-026's ~18-season estimate is the right order; the measured
  full-erosion horizon is longer.
- **Within a PM-3 horizon of 3–5 seasons**, 13–21% of the gap is lost and the strength order is intact
  (Spearman ≥ 0.98). Visible but not league-breaking.
- **Youth discount.** At genesis, 16–18-year-olds already average CA 5298, the same as 24–29-year-olds (5287):
  `LeagueBootstrap`'s squads carry no age gradient either, not only regens. Later, progression opens a gap
  (seasons 25–40: 16–18 ≈ 5300 vs 24–29 ≈ 5590, about 5%). Arriving regens average CA ≈ 5250–5300 in the first
  seasons, the league average at genesis.
- **League level.** Mean rating rises from 10.66 to 11.17 by season 13, then settles at about 10.81
  (+0.15 against genesis) once turnover completes.

`report-5x40.txt` holds every season with per-seed ranges and CA by age band.
