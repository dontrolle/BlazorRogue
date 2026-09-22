window.blazorroguefuncs = {
    playSound: function (soundname) {
        //console.log("play sound" + soundname);
        var audio = new Audio(soundname);
        audio.play();
    },

    // Drives the persistent looping #bgsound element (rather than firing a one-shot Audio like
    // playSound above) so level transitions can restart a level-specific ambient track.
    playBackgroundMusic: function (soundname) {
        var audio = document.getElementById("bgsound");
        if (!audio) {
            return;
        }
        var source = audio.querySelector("source");
        source.src = soundname;
        audio.load();
        audio.play().catch(() => {});
    },

    showById: function (id) {
        document.getElementById(id).style.display = "block";
        return true;
    },

    hideById: function (id) {
        document.getElementById(id).style.display = "none";
        return true;
    },

    // Returns a stable per-browser id, creating one on first visit. The server keys the play
    // session off this id, which is what lets a game survive a page reload instead of a new
    // dungeon being generated. Read-or-create happens here so it costs a single interop round trip.
    ensureSessionId: function () {
        const key = "blazorrogue.sessionId";
        const newId = () =>
            (window.crypto && window.crypto.randomUUID)
                ? window.crypto.randomUUID()
                : Date.now().toString(36) + Math.random().toString(36).slice(2);

        try {
            let id = window.localStorage.getItem(key);
            if (!id) {
                id = newId();
                window.localStorage.setItem(key, id);
            }
            return id;
        } catch {
            // localStorage can be unavailable (blocked storage, hardened privacy settings). Fall
            // back to a throwaway id: the game then behaves as it did before sessions existed,
            // i.e. a fresh dungeon per page load, rather than failing to start at all.
            return newId();
        }
    },

    // Listens for keyup on the whole document rather than a focused element, so movement keys
    // always drive the game - no click-to-focus step, and focus lost between actions (e.g. after
    // clicking a button) doesn't stop input from working.
    registerKeyup: function (dotNetRef) {
        window.blazorroguefuncs.unregisterKeyup();

        // Ctrl+A toggles the ASCII/tileset renderer, Ctrl+D toggles verbose debug output, and
        // Ctrl+G (while debug mode is on) jumps into the configured debug level - see KeyUp in
        // GamePage.razor. Without intercepting them here, the browser's native "select all" /
        // "bookmark this page" / "find next" fires on keydown before our keyup handler ever runs.
        const keydownHandler = (e) => {
            if (e.ctrlKey && ["a", "d", "g"].includes(e.key.toLowerCase())) {
                e.preventDefault();
            }
        };

        const keyupHandler = (e) => {
            dotNetRef.invokeMethodAsync("OnGlobalKeyUp", e.key, e.code, e.shiftKey, e.ctrlKey);
        };

        window.blazorroguefuncs._keydownHandler = keydownHandler;
        window.blazorroguefuncs._keyupHandler = keyupHandler;
        document.addEventListener("keydown", keydownHandler);
        document.addEventListener("keyup", keyupHandler);
    },

    unregisterKeyup: function () {
        if (window.blazorroguefuncs._keydownHandler) {
            document.removeEventListener("keydown", window.blazorroguefuncs._keydownHandler);
            window.blazorroguefuncs._keydownHandler = null;
        }
        if (window.blazorroguefuncs._keyupHandler) {
            document.removeEventListener("keyup", window.blazorroguefuncs._keyupHandler);
            window.blazorroguefuncs._keyupHandler = null;
        }
    },
}

// Visually replays a fast moveable's extra action(s) within a single player turn -
// GamePage.razor's server-side render already snaps every moveable straight to its final
// square (that stays a single synchronous Map.TakeTurn + render, deliberately - see
// GamePage.razor's KeyUp), so this steps a moveable's DOM element backward through its
// intermediate square(s) via CSS transform, then lets go, entirely client-side. Snap-through by
// design (not a tween) - each step is a discrete jump, matching the turn-based feel elsewhere.
window.blazorRogueReplay = {
    // Timeout ids and elements touched by the still-running replay, if any - cleared at the start
    // of every call so a keypress landing mid-replay (fast player input) can't leave a moveable's
    // element stuck with a stale transform from a turn that's already been superseded.
    _pending: [],
    _touched: [],

    // entries: [{ id, offsets }], offsets: [[dxPx, dyPx], ...] - see GamePage.razor's
    // MoveReplayEntry/BuildMoveReplay for exactly what these mean and how they're computed.
    // delayMs: gap between steps: reuses GamePage.razor's message-reveal delay so the two staggered
    // effects (log lines, move replay) read as one consistent pace.
    play: function (entries, delayMs) {
        window.blazorRogueReplay._pending.forEach(clearTimeout);
        window.blazorRogueReplay._pending = [];
        window.blazorRogueReplay._touched.forEach((el) => {
            el.style.transform = "";
        });
        window.blazorRogueReplay._touched = [];

        for (const entry of entries) {
            const el = document.querySelector('[data-moveable-id="' + entry.id + '"]');
            if (!el || entry.offsets.length === 0) {
                continue;
            }

            window.blazorRogueReplay._touched.push(el);

            const [firstDx, firstDy] = entry.offsets[0];
            el.style.transform = `translate(${firstDx}px, ${firstDy}px)`;

            let delay = delayMs;
            for (let i = 1; i < entry.offsets.length; i++) {
                const [dx, dy] = entry.offsets[i];
                window.blazorRogueReplay._pending.push(
                    setTimeout(() => {
                        el.style.transform = `translate(${dx}px, ${dy}px)`;
                    }, delay)
                );
                delay += delayMs;
            }

            // The final step is simply "let go" - the element's actual rendered position (this
            // turn's real, final square) already matches, with no transform needed.
            window.blazorRogueReplay._pending.push(
                setTimeout(() => {
                    el.style.transform = "";
                }, delay)
            );
        }
    },
}
