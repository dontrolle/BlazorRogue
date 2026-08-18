using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BlazorRogue.Effects;

class SoundManager(IJSRuntime jsRuntime)
{
    readonly IJSRuntime jsRuntime = jsRuntime;
    readonly Random random = new();
    readonly string footstepDirtPrefix = "Footstep_Dirt_0";

    async Task PlaySound(string sound) =>
        await InvokeJs("blazorroguefuncs.playSound", $"sound/{sound}").ConfigureAwait(false);

    async Task InvokeJs(string jsFunction, string arg)
    {
        try
        {
            _ = await jsRuntime.InvokeAsync<object>(jsFunction, arg).ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            // The circuit went away (typically a page reload) while a fire-and-forget sound was
            // still in flight. Every caller below is `async void`, so letting this escape would
            // surface as an unhandled exception on a thread pool thread rather than as a lost
            // sound effect. Reloads are routine now that games survive them.
        }
        catch (ObjectDisposedException)
        {
            // As above, for a JS runtime that has already been torn down.
        }
    }

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

    public async void PlayBlockedDoorSound()
    {
        string sound = $"car-door-shut.mp3";
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

    public async void PlayBackgroundMusic(string track) =>
        await InvokeJs("blazorroguefuncs.playBackgroundMusic", $"sound/{track}")
            .ConfigureAwait(false);
}
