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

        // Ctrl+A doubles as the ASCII/tileset toggle (see KeyUp in Indoor.razor). Without this,
        // the browser's native "select all" fires on keydown before our keyup handler ever runs,
        // highlighting the whole page every time the game is toggled.
        const keydownHandler = (e) => {
            if (e.ctrlKey && e.key.toLowerCase() === "a") {
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