# src/CLAUDE.md — Tactical Director coding guide

> **Scope:** Required rules for changes under `src/`.
> **Expanded examples and catalogues:** [`../docs/agent-guides/coding-reference.md`](../docs/agent-guides/coding-reference.md).
> Read only the relevant sections of that file when this guide routes you there.

## Before writing code

1. Read the governing approved spec in full and confirm the target assembly exists.
2. Check `docs/specs/SPEC_INDEX.md`, `docs/tracking/data-contract-index.md`, and, for match-engine work,
   `docs/tracking/match-engine-wiring-backlog.md`.
3. Inspect the target assembly's `.asmdef`, neighboring code, and tests.
4. Use the expanded coding reference for the exact assembly taxonomy, commands, constant-catalogue
   patterns, file-header template, profiler example, or deferred-platform ledger.

## Architecture

- One production assembly per folder, with tests in `<assembly>/tests/` and a separate test asmdef.
- Reference direction is **AI → Mechanics → Physics**, never the reverse. Cross-cutting foundations
  may be referenced by every layer; the composition root may reference all production assemblies.
- Presentation reads simulation output; simulation assemblies must not reference presentation.
- Avoid circular references. Events that travel upward use struct payloads through `event-system`.
- Interfaces belong with a specified consumer. If either side is unspecified, create nothing.
- Use constructor injection; service locators, ambient contexts, mutable static singletons, and
  hot-path DI containers are prohibited.

The expanded layer/assembly table is in `docs/agent-guides/coding-reference.md` under
**Assembly Layer Taxonomy** and **Reference Direction**.

## Naming and layout

- Files and public types use `PascalCase`; interfaces use `I`; private fields use `_camelCase`;
  locals and parameters use `camelCase`.
- Constants use `UPPER_SNAKE_CASE`; booleans start with `Is`, `Has`, `Can`, or `Should`.
- One primary public type per file. Namespace format is `TacticalDirector.<AssemblyName>`.
- `using` groups are System, Unity, then project, separated by blank lines.
- Every source file needs the standard header and append-only version history. Copy the exact template
  from the expanded coding reference's **FILE HEADER** section; automated agents use `—` as Author.
- Public types/members require XML summaries. Every constant summary includes its source tag.

## Constants and numeric types

- Stage 0 uses `float`. `double` requires recorded lead-developer approval; `decimal` is banned.
- Fixed64 migration is Stage 5+.
- No magic numbers in formula code. Put constants in the assembly's designated catalogue.
- `[GT]` values use the established `GameplayConfig.Get*` pattern; do not invent another loader.
- `[FIXED]` and `[DERIVED]` values remain compile-time constants where possible; `[CROSS]` values are
  consumed read-only from their owning assembly.

For declaration shapes, naming exceptions, arrays/tables, and loader migration status, read
**CONSTANT CATALOGUES** in the expanded coding reference.

## Game-loop rules

- Per-frame code is zero-allocation: prefer structs, arrays/`Span<T>`, preallocated buffers, and `ref`
  parameters for state larger than 16 bytes.
- Prohibited on the hot path: LINQ, boxing, closures/delegates, string formatting/interpolation,
  collection growth, reflection, interface-typed enumeration, `dynamic`, `async`/`await`, and
  `try`/`catch` or virtual dispatch inside inner loops.
- `unsafe` requires lead-developer approval recorded in the PR.
- Every system entry point (`Update`, `Tick`, `RunStep`, etc.) uses a private static readonly
  `ProfilerMarker` and wraps work in `.Auto()`.
- Comments explain non-obvious reasons, not syntax. Never commit commented-out code.

See **GAME-LOOP RULES (ZERO ALLOCATION)** and **PROFILER MARKERS** in the expanded reference for the
complete banned-operation list and compliant examples.

## Determinism

- Game logic must not use `System.Random`, `DateTime.Now`, `Guid.NewGuid`, `Task.Run`, `Parallel.*`, or
  hardware-intrinsic FMA.
- Inject `MatchClock`; use the `deterministic-sim` SplitMix64 helper and deterministic ID ranges.
- Deliberate 64-bit wraparound uses `unchecked` with `// Spec #16 §3.4.4`.
- Python mirrors of SplitMix64 omit `UL` and mask intermediate multiplication to 64 bits.
- State changes must survive snapshot/restore and replay without hidden state.

## Verification

Run the narrowest relevant tests first, then the repository gate described under **BUILD AND TEST
COMMANDS** in the expanded reference. Do not claim certification from the Linux shim gate: certified
performance capture requires the pinned Unity host and `docs/tracking/cert-run-runbook.md`.

Before finishing, verify layering, allocation behavior, deterministic replay implications, file
headers/version history, constant tags, and mirrored home/away behavior where geometry is team-relative.
