# Browser Match Client — Design Supplement (roadmap B6)

> **Created:** July 27, 2026
> **Status:** DESIGN SUPPLEMENT — the class-(b) governance the path-to-playable roadmap §6 item 2
> names for the B6 renderer. It opens no numbered spec and changes no `SPEC_INDEX.md` row; it
> permanently governs `src/match-client-web/`, exactly as `match-engine-design.md` governs the
> composition root and `interactive-unity-client-design.md` governs the client core.
> **Version:** 1.0
> **Scope:** the PM-1 browser surface — one playable match a person can watch, adjust and read a
> report on. Not the season screens (roadmap C3), not the UGUI skin (Unity P4–P6).

---

## 0. Why this exists

The roadmap's §7 fork was decided on July 25, 2026: **option (b), extend the browser surface**,
because it reaches PM-1 with no external blocker and because #38's view models make the renderer a
leaf. What §7 did not settle is *where* the extension lands, and that turns out to be the whole
design question — because the obvious answer is wrong.

The obvious answer is "add routes to `LiveMatchServer`". That server's **playback-only invariant is
load-bearing**, not incidental. `LiveMatchStreamer`'s own class doc states it: the streamer is the
only thing that holds the engine, and the server holds no engine reference at all, *"so the 'thing
that can mutate the match' and 'the thing an HTTP request can reach' are disjoint by construction,
not just by convention."* The interactive-Unity-client AR-1 H-2 finding and **ERR-038-001** both turn
on it — #38's §3.3 sketch proposed a `LiveMatchStreamer.EnqueueIntent`, and that was rejected for
exactly this reason one day after #38's approval.

PM-1 needs manager input. So the surface that can change a match is built **above** `match-client-core`,
in a new assembly, and `match-viewer` keeps its invariant intact.

---

## 1. Key decisions

### KD-W1 — A new assembly, not a mode of the spectator server

`src/match-client-web/` (`TacticalDirector.MatchClientWeb`) sits above `match-client-core`,
`ui-framework`, `match-analytics` and `match-viewer`. Nothing references it.

Two surfaces with different privileges do not share a port, a lifecycle, or a routing table where one
wrong `case` grants the wrong one. The spectator viewer stays exactly what it is — a thing you can
hand someone without handing them the match.

### KD-W2 — Three routes, three privileges, deliberately not merged

| Route | Can it change the match? | Path to the engine |
|---|---|---|
| `GET /frame`, `GET /report` | No | Read-only projections |
| `GET /playback?action=…` | Changes **when** ticks happen, never what is in them | Streamer pacing; never the queue |
| `GET /intent?kind=…` | **Yes** | `ManagerCommandQueue` → pre-tick drain → engine |

Playback is not a game command (§6.4 of the client note), so it must never enter the tick-stamped
log — a replay that re-applies a pause is not a replay of the match. Keeping the two on separate
routes makes that a structure rather than a convention, and the router tests assert it directly by
watching the command queue rather than by inspecting the code path.

### KD-W3 — Intents go through the queue, always

`/intent` never touches the engine. It builds a `ManagerIntent`, hands it to `MatchTacticsDispatcher`,
which enqueues; `MatchClientDriver`'s pre-tick hook drains it on the sim thread at a tick boundary and
records it in the tick-stamped log. That log is what makes a live match replay-reproducible (§6.1),
so a change applied around it is not merely unsafe across threads — it is **absent from the record**,
and the replay diverges silently.

### KD-W4 — The analytics pump needs a *new* seam, and why it could not reuse the old one

#37 §3.5 requires its aggregator to consume **every** tick exactly once, and F6 makes that
enforceable: `ObserveTick` throws on a non-consecutive tick rather than under-counting quietly.

The streamer owns the tick loop on its own thread, so the pump has to live there. The existing
`SetPreTickHook` cannot carry it, for two independent reasons:

1. It is **set-once** and already taken by the command drain.
2. It also fires from **`ServiceOnce()`**, where no tick advances — an every-tick consumer hung off
   it would see a repeated tick and refuse.

So `LiveMatchStreamer` gains `SetPostTickObserver(Action)`, fired inside `TickOnce` after `RunTick`
and **not** from `ServiceOnce`. It does not weaken the playback-only invariant: the observer is
handed nothing and returns nothing, so it can only read what its creator already had.

`MatchSession.AttachTickObserver(Func<MatchEngine, Action>)` is the client-side seam. The factory
receives the engine once and returns the per-tick action; the session still exposes no standing
engine property. The contract — *build a reader, never a mutator* — is documented at the seam with
its reason (KD-W3), and is the same trust level `SetPreTickHook` already asks for.

### KD-W5 — A failing observer disarms; it does not kill the match, and it does not vanish

The pacing loop does **not** guard `TickOnce`, so an unhandled exception on that thread ends the
process. A derived statistic must never be able to kill a match in progress.

Nor may it be swallowed — a report frozen at the failing tick looks merely stale, and a reader has no
way to tell. So the first exception **disarms the observer and latches the cause** on
`PostTickObserverFault`; `/report` carries `healthy:false` plus the message, and the page shows it.
This is the same posture `MatchClientDriver` already takes for a command the engine refuses: isolate,
record, carry on.

### KD-W6 — Router and transport are separate types

`MatchClientRouter` maps `(method, pathAndQuery)` to a `MatchClientResponse` value. `MatchClientServer`
owns sockets and threads and decides nothing. The spectator viewer had to be tested through real
loopback sockets because routing and transport were one method; here the interesting half — what is
refused, what a bad parameter produces, what reaches the queue — is a pure function under test, and
the socket code has nothing left to get wrong.

### KD-W7 — Everything is refused loudly, and an omitted dial is an identity, not a guess

An unparseable enum, an out-of-range team, an unknown action: 400 with the reason.

Two specific traps:

- **`Enum.TryParse` accepts `"9"`** for an enum with five members. Unguarded, that ordinal reaches a
  `[GT]` lookup table indexed by ordinal and runs off the end of it. Every enum parse is
  `Enum.IsDefined`-checked.
- **An omitted tactical dial keeps its `Balanced` identity** rather than being inferred. A dial that
  quietly became something else would be indistinguishable from one the manager chose.

`SetPlayerTactic` returns **501**, not a partial tactic. The seam exists; a browser surface for
eleven per-agent instruction sets does not, and applying defaults for the ten dials the manager never
chose is worse than refusing.

### KD-W8 — Loopback only

`127.0.0.1`, never `IPAddress.Any`. This matters more here than for the spectator viewer: `/intent`
can change a match in progress and there is no authentication, so binding wide would put the
manager's controls on the LAN.

### KD-W9 — The page renders the #38 view model, not the engine

Every field the page draws comes from `/frame`, which serializes `MatchFrameView`. When the UGUI skin
lands it binds the same projection, which is what makes this surface a leaf rather than a fork —
and is exactly what #38 §7.2 says will happen (*"the rendering is a view over the already-defined
substrate"*). Pitch geometry is read from the streamer, which reports the engine's own constants; a
fourth independent copy of the pitch dimensions in a page template is the drift this project keeps
finding.

---

## 2. What this deliberately does not do

- **No season, squad or new-game screens.** Those are roadmap C3/C4 and need #30 wired to a client;
  building them here would fork the navigation model before `NavigationShell` has a second screen.
- **No `POST`.** Every route is a `GET`, which is not REST-correct for `/intent` — it is a
  loopback-only single-user tool, and a query string is what a `<select>` and a `fetch` produce with
  no ceremony. Recorded as a deliberate deviation rather than an oversight.
- **No authentication and no CSRF defence.** Both follow from loopback-only + single-user. If this
  surface ever binds beyond loopback, both become mandatory and this note is wrong.
- **No post-match report *screen*.** The statistics panel is live and continues to serve after full
  time (the frame carries `matchEnded`), which satisfies PM-1's "read a post-match report" through
  the same surface. A dedicated end-of-match screen belongs with the C3 navigation work.
- **No frame interpolation in the page yet.** `FrameInterpolator` (B4) is host-free and ready; the
  page currently draws the latest polled frame directly. Wiring it is a page-only change with no
  contract consequence.

---

## 3. Testing

| Concern | Where |
|---|---|
| Routing table, privilege split, fail-loud parsing | `MatchClientRouterTests` — pure, no socket |
| Every-tick pump over a really-running match | `MatchClientHostTests` — F6 makes it self-checking: a gap or a double-fire latches a fault, so a null fault after 300 ticks *is* the proof |
| `ServiceOnce` does not advance the analytics clock | `MatchClientHostTests` — the reason KD-W4's seam is distinct |
| A failing observer disarms, latches, and the match plays on | `MatchClientHostTests` |
| Framing, request-line bound, post-`Stop` refusal, rebind | `MatchClientServerTests` — real loopback sockets |

The composed tests start real pacing threads. The EventBus is process-static (#17 §3.2.1), so like
every other composed match test they run sequentially and assert tick counts and derived quantities,
never a cross-run digest.

---

## Version History

| Version | Date | Notes |
|---|---|---|
| 1.0 | 2026-07-27 | Initial creation, landed with the B6 code: KD-W1..KD-W9, the deliberate non-goals, and the test map. |
