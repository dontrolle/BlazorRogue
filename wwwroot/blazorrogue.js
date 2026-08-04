window.blazorroguefuncs = {
    playSound: function (soundname) {
        //console.log("play sound" + soundname);
        var audio = new Audio(soundname);
        audio.play();
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
}