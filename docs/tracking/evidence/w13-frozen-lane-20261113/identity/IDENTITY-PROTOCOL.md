# W13 frozen-lane identity protocol

Created 2026-11-13, frozen before result-bearing runs.

Reproduce all six historical v27 terminal fingerprints using the unmodified historical source first. Stop the acceptance sequence on any mismatch. Then compare W13 switched OFF against the post-W13.0 branch tip: six fixed seeds, 324000 ticks each, all per-tick digests, terminal digests and 6000-tick checkpoints. No config, gameplay change, tuning, observer callback, or RNG draw. Exact raw equality, with no schema alignment needed (both W13 arms v28). One fresh process per seed, at most two simultaneous captures. SDK 8.0.426 / CLR 8.0.32, Debian 13 x64, Debug, optimization off, DEVELOPMENT_BUILD, default JIT settings, DOTNET_PROCESSOR_COUNT=4. This is the frozen Linux measurement lane, not Unity determinism certification. Every attempt and exit is retained. Historical controls and new W13 evidence remain separate; no prior fingerprint is adopted for W13. Any mismatch is a hard stop, never a rebaseline.

The W13 baseline is f64729d2 (post-W13.0, including fd73de77); W13 candidate is 4171d3d1 (same runtime source as 4f4e9094). Main 39de4662 is not substituted for the explicit plan baseline. #543 and §10 require a separate source integration and observer-neutrality checks.
