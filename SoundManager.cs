using System;
using System.Threading.Tasks;

using Microsoft.JSInterop;

namespace BlazorRogue;

class SoundManager(IJSRuntime jsRuntime)
{
    readonly IJSRuntime jsRuntime = jsRuntime;
    readonly Random random = new();
    readonly string footstepDirtPrefix = "Footstep_Dirt_0";

    async Task PlaySound(string sound) => _ = await jsRuntime.InvokeAsync<object>("blazorroguefuncs.playSound", $"sound/{sound}").ConfigureAwait(false);

    public async void PlayWalkSound()
    {
        int soundindex = random.Next(0, 10);
        string sound = $"{footstepDirtPrefix}{soundindex}.mp3";
        await PlaySound(sound).ConfigureAwait(false);
    }

    public async void PlayDoorSound(bool open)
    {
        string prefix = open ? "open" : "close";
        int soundindex = random.Next(1, 3);
        string sound = $"{prefix}_door_{soundindex}.mp3";
        await PlaySound(sound).ConfigureAwait(false);
    }

    public async void PlayCombatSound(bool hit)
    {
        string sound = hit ? "sfx-attack-sword-001.wav" : "Swoosh.mp3";
        await PlaySound(sound).ConfigureAwait(false);
    }

    internal async void PlayKillMonsterSound()
    {
        string sound = "creature_die8.wav";
        await PlaySound(sound).ConfigureAwait(false);
    }

    internal async void PlayGameLoose()
    {
        string sound = "Jingle_Lose_00.mp3";
        await PlaySound(sound).ConfigureAwait(false);
    }

    internal async void PlayPickupMoney()
    {
        int soundindex = random.Next(0, 5);
        string sound = $"Pickup_Gold_0{soundindex}.mp3";
        await PlaySound(sound).ConfigureAwait(false);
    }
}
