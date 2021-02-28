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
}