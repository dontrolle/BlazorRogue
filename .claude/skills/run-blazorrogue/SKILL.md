---
name: run-blazorrogue
description: Build, run, and drive BlazorRogue (the Blazor Server rogue-like). Use when asked to start the game, take a screenshot of the map/UI, or interact with the running app (movement, pickup, inventory, combat) to verify a change - not just run its unit tests.
---

BlazorRogue is an ASP.NET Core Blazor Server app with no client-side click/fill surface to speak
of - the whole game is driven by keyboard events dispatched on `document` (see `GamePage.razor`'s
`registerKeyup`). There's no `chromium-cli` on this machine, so driving it means: start the dev
server, then pipe commands to `.claude/skills/run-blazorrogue/driver.mjs` (a small
Playwright-based REPL built for this project - see "Run (agent path)" below).

All paths below are relative to the repo root, except where noted.

## Prerequisites

.NET 10 SDK (already required by the repo - see `CLAUDE.md`). For the driver, Node.js plus a
one-time Chromium download:

```bash
cd .claude/skills/run-blazorrogue
npm install                        # installs playwright (pinned in package.json)
npx playwright install chromium    # ~115MB, one-time - cached under
                                    # ~/AppData/Local/ms-playwright (Linux: ~/.cache/ms-playwright)
```

If that cache already has a matching `chromium-*` folder (check `ls ~/AppData/Local/ms-playwright`
on Windows), `npx playwright install chromium` is a no-op - it does not redownload.

## Build

```bash
dotnet build
```

(Warnings are treated as errors - see `CLAUDE.md`.)

## Run (agent path)

1. Start the dev server in the background and wait for it to actually serve - don't `sleep`, poll:

   ```bash
   dotnet run --urls http://localhost:5099 &
   timeout 60 bash -c 'until curl -sf http://localhost:5099 >/dev/null; do sleep 1; done'
   ```

2. Drive it by piping commands to the driver, from inside the skill directory (it needs its own
   `node_modules`):

   ```bash
   cd .claude/skills/run-blazorrogue
   node driver.mjs <<'EOF'
   launch http://localhost:5099
   wait-for #mapcontainer
   screenshot initial.png
   keys d:KeyD,g:KeyG
   messages
   quit
   EOF
   ```

   Each line prints one JSON result to stdout. `wait-for #mapcontainer` after `launch` is the
   reliable "the SignalR circuit is up and the map rendered" marker - `networkidle` alone isn't
   enough for a Blazor Server page.

   Screenshots with no path go to `.claude/skills/run-blazorrogue/screenshots/<timestamp>.png`; a
   relative path (like `initial.png` above) is resolved against the skill directory too. **Look at
   the screenshot** - a blank or all-black image means the map never rendered.

3. Stop the server (Windows has no `lsof`; this is the one-liner that actually works here):

   ```powershell
   Get-NetTCPConnection -LocalPort 5099 -State Listen | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }
   ```

### Driver commands

| command | what it does |
|---|---|
| `launch <url>` | opens the page (server must already be running) |
| `nav <url>` | navigates the current page |
| `wait-for <selector>` | waits up to 20s for a selector to appear |
| `key <key> [code] [ctrl]` | dispatches one synthetic `document` keyup. **Set `code`** for movement/action keys - `GamePage.OnKeyPress` reads `e.Code` (e.g. `KeyD`, `KeyG`), not just `e.key` |
| `keys <k1[:c1],k2[:c2],...>` | dispatches several keys in sequence, ~80ms apart (code defaults to key if omitted, e.g. `keys i,Escape` for `i`/`Escape`) |
| `screenshot [path]` | saves a PNG (default: `screenshots/<timestamp>.png`) |
| `messages` | the message-log entries (`.message_log_entry`) as JSON |
| `text <selector>` | an element's trimmed `textContent` (`null` if not found) |
| `eval <js>` | runs `<js>` in the page inside an implicit function body (use `return`) and prints the JSON result |
| `console-errors` | console errors / page errors seen so far |
| `quit` | closes the browser |

Example - locate a floor item's map cell before walking to it (item/monster placement is
randomized per new game, so a fixed walk sequence isn't reproducible - find your target first):

```
eval const els = Array.from(document.querySelectorAll('.decoration')); const hit = els.find(e => (e.getAttribute('alt')||'').includes('Health potion')); return hit ? hit.closest('.cell').id : null;
```

## Run (human path)

`dotnet run` (or `docker run` per `CLAUDE.md`), then open `https://localhost:5001` in a real
browser. Ctrl+C to stop. Not useful for an agent - no window to look at headless.

## Test

```bash
dotnet test
dotnet csharpier check .
```

---

## Gotchas

- **No `chromium-cli` on this machine.** Adapted the Electron-REPL-driver pattern to plain
  Playwright `chromium` instead - `driver.mjs` in this directory. If a future machine *does* have
  `chromium-cli`, its `nav`/`wait-for`/`screenshot`/`console --errors` map directly, but you'd
  still need a `key`/`keys`-equivalent, since `chromium-cli` has no notion of a document-level
  synthetic `KeyboardEvent` - this app has no clickable form inputs to `fill`.
- **`key`/`code` both matter.** `GamePage.OnKeyPress` branches on `keyCode` (the JS `code`, e.g.
  `KeyD`/`KeyG`/`Numpad7`) for movement/action keys, not on `key`. Dispatching `key d` without a
  `code` (defaults `code` to `"d"`, not `"KeyD"`) silently does nothing - always pass both for
  movement/pickup/etc. (`?`, `Escape`, and letters used only for inventory selection are the
  exceptions - `HandleInventoryKey`/the help toggle match on `key`).
- **Item/monster/room layout is randomized per new game.** A screenshot or a scripted walk from
  one run won't reproduce on the next `launch`. For anything that needs to reach a specific game
  object (an item, a monster), locate it first with `eval` reading `.decoration`'s `alt` attribute
  (format: `x,y (Name=..., Blocking=...)`) rather than hardcoding coordinates or a move sequence.
- **The proprietary tileset happens to be installed on this machine** (`wwwroot/img/uf_*`,
  gitignored, not in the repo). Screenshots here show real art; on a machine without it the game
  silently falls back to the ASCII renderer (`renderAscii` in `GamePage.razor`) and screenshots
  will look completely different (colored text glyphs, no `background-image` decorations) - that's
  expected, not a bug, per `CLAUDE.md`.
- **No `lsof`/`fuser` on Windows/Git Bash.** Use the `Get-NetTCPConnection ... | Stop-Process`
  PowerShell one-liner above to free the port before relaunching `dotnet run`.

## Troubleshooting

- **`Stop-Process: Cannot find a process with the process identifier ...`** after the stop
  one-liner: harmless. `dotnet run` spawns a child process and the listener sometimes belongs to
  that child rather than the PID `Get-NetTCPConnection` still reports; the real listener is
  already gone by the time this prints. Confirm with `curl` (expect a connection failure) rather
  than trusting the one-liner's exit status.
