#!/usr/bin/env node
// A minimal chromium-cli-alike for BlazorRogue (Blazor Server, no chromium-cli on this machine -
// see SKILL.md). Reads one command per line from stdin, keeps a single Chromium page open across
// commands, and prints a one-line JSON result per command to stdout.
//
// BlazorRogue listens for keyboard input at `document` level (see GamePage.razor's
// registerKeyup), not on a focused element - so this driver's core primitive is `key`/`keys`
// (dispatching a synthetic KeyboardEvent on `document`), not click/fill like a typical web app.
//
// Commands:
//   launch <url>                  - open the page (the dev server must already be running - see SKILL.md)
//   nav <url>                     - navigate the current page
//   wait-for <selector>           - wait (up to 20s) for a selector to appear
//   key <key> [code] [ctrl]       - dispatch one synthetic document keyup
//   keys <k1[:c1],k2[:c2],...>    - dispatch several keys in sequence, ~80ms apart (code defaults to key)
//   screenshot [path]             - save a screenshot (default: screenshots/<timestamp>.png next to this file)
//   messages                      - print the message-log entries (.message_log_entry) as JSON
//   text <selector>               - print an element's trimmed textContent (null if not found)
//   eval <js>                     - evaluate JS in the page (has implicit `return`) and print the JSON result
//   console-errors                - print any console errors/pageerrors seen so far
//   quit                          - close the browser and exit
//
// Usage: node driver.mjs <<'EOF' / or one command per send-keys under tmux for iterative debugging.

import { chromium } from "playwright";
import { createInterface } from "node:readline";
import { mkdirSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join, isAbsolute } from "node:path";

const __dirname = dirname(fileURLToPath(import.meta.url));

let browser;
let page;
const consoleMessages = [];

function print(result) {
  process.stdout.write(JSON.stringify(result) + "\n");
}

async function ensurePage(url) {
  if (!browser) {
    browser = await chromium.launch({ args: ["--no-sandbox"] });
    page = await browser.newPage();
    page.on("console", (msg) => {
      if (msg.type() === "error") consoleMessages.push(msg.text());
    });
    page.on("pageerror", (err) => consoleMessages.push("pageerror: " + err.message));
  }
  if (url) {
    await page.goto(url, { waitUntil: "networkidle" });
  }
  return page;
}

function dispatchKey(p, key, code, ctrlKey) {
  return p.evaluate(
    ({ key, code, ctrlKey }) => {
      document.dispatchEvent(
        new KeyboardEvent("keyup", { key, code, ctrlKey: !!ctrlKey, bubbles: true }),
      );
    },
    { key, code, ctrlKey },
  );
}

async function handle(line) {
  const trimmed = line.trim();
  if (!trimmed) return;
  const [cmd, ...rest] = trimmed.split(" ");
  const arg = rest.join(" ");

  switch (cmd) {
    case "launch": {
      await ensurePage(arg);
      return { ok: true, url: page.url() };
    }
    case "nav": {
      await page.goto(arg, { waitUntil: "networkidle" });
      return { ok: true, url: page.url() };
    }
    case "wait-for": {
      await page.waitForSelector(arg, { timeout: 20000 });
      return { ok: true };
    }
    case "key": {
      const [key, code, ctrl] = rest;
      await dispatchKey(page, key, code || key, ctrl === "ctrl");
      return { ok: true };
    }
    case "keys": {
      for (const part of arg.split(",")) {
        const [key, code] = part.split(":");
        await dispatchKey(page, key, code || key, false);
        await page.waitForTimeout(80);
      }
      return { ok: true };
    }
    case "screenshot": {
      const path = arg
        ? isAbsolute(arg)
          ? arg
          : join(__dirname, arg)
        : join(__dirname, "screenshots", `${Date.now()}.png`);
      mkdirSync(dirname(path), { recursive: true });
      await page.screenshot({ path });
      return { ok: true, path };
    }
    case "messages": {
      const messages = await page.$$eval(".message_log_entry", (els) =>
        els.map((e) => e.textContent.trim()),
      );
      return { ok: true, messages };
    }
    case "text": {
      const el = await page.$(arg);
      if (!el) return { ok: true, text: null };
      const text = await el.evaluate((e) => e.textContent.trim());
      return { ok: true, text };
    }
    case "eval": {
      const result = await page.evaluate(new Function("return (function(){" + arg + "})()"));
      return { ok: true, result };
    }
    case "console-errors": {
      return { ok: true, consoleMessages };
    }
    case "quit": {
      await browser?.close();
      process.exit(0);
    }
    default:
      return { ok: false, error: `unknown command: ${cmd}` };
  }
}

// Lines can arrive all at once (a heredoc), but commands share one page and must run in order -
// chain them on a single promise rather than handling `line` events concurrently.
const rl = createInterface({ input: process.stdin });
let queue = Promise.resolve();
rl.on("line", (line) => {
  queue = queue.then(async () => {
    try {
      print(await handle(line));
    } catch (err) {
      print({ ok: false, error: String(err && err.message ? err.message : err) });
    }
  });
});
rl.on("close", () => {
  queue.then(async () => {
    await browser?.close();
    process.exit(0);
  });
});
