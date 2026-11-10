# #440 Stage A evidence — capture pending

Created November 10, 2026. Purpose: retain the observation-only investigation against
main d4b026aa951432d342217314b8c4d49b53a19e7d. No gameplay fix is included.
Definitions, overlap/censor rules and the proposed policy are in
[the preregistered design](../../w-440-ball-loose-design.md).

The collector's 20 always-run tests pass. A Release build with development/editor/test
symbols unset also passes; reflection confirms the delegate, callback and copied event
and kind types are absent. This is Linux evidence, not pinned Unity certification.
Full six-seed enabled/disabled neutrality and episode measurements remain pending.

Build committed inputs in a fresh workspace (SDK 8.0.426 and CLR 8.0.32):

```bash
python3 docs/tracking/evidence/ball-loose-stage-a-20261110/prepare.py --repo "$PWD" --work-dir /tmp/w440-capture --head "$(git rev-parse HEAD)" --dotnet /path/to/dotnet --packages /path/to/nuget-cache
python3 docs/tracking/evidence/ball-loose-stage-a-20261110/run.py --work-dir /tmp/w440-capture --main-control /path/to/formation-duty-control
```

For a reproduction, use the measured observer SHA from the retained `prepared.json`,
not an unrelated checkout. The main-control argument is the independently measured
#434 base C0 control, with its folder structure restored from its evidence bundle.
The shared `tools/dotnet-ci/retained_trace_store.py` reader accepts direct gzip traces
and verified lossless packed traces. Packing preserves decoded bytes exactly; NumPy
only accelerates an optional integer codec, with a portable identical fallback.
