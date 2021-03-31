using System;
using System.Threading.Tasks;

using Microsoft.JSInterop;

namespace BlazorRogue
{
    public class SoundManager
    {
        private readonly IJSRuntime jsRuntime;
        private readonly Random random = new Random();
        private readonly string footstepDirtPrefix = "Footstep_Dirt_0";

        public SoundManager(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        private async Task PlaySound(string sound)
        {
            await jsRuntime.InvokeAsync<object>("blazorroguefuncs.playSound", $"sound/{sound}");
        }

        public async void PlayWalkSound()
        {
            var soundindex = random.Next(0, 10);
            var sound = $"{footstepDirtPrefix}{soundindex}.mp3";
            await PlaySound(sound);
        }

        public async void PlayDoorSound(bool open)
        {
            var prefix = open ? "open" : "close";
            var soundindex = random.Next(1, 3);
            var sound = $"{prefix}_door_{soundindex}.mp3";
            await PlaySound(sound);
        }

        public async void PlayCombatSound(bool hit)
        {
            var sound = hit ? "sfx-attack-sword-001.wav" : "Swoosh.mp3";
            await PlaySound(sound);
        }

        internal async void PlayKillMonsterSound()
        {
            var sound = "creature_die8.wav";
            await PlaySound(sound);
        }

        internal async void PlayGameLoose()
        {
            var sound = "Jingle_Lose_00.mp3";
            await PlaySound(sound);
        }

        internal async void PlayPickupMoney()
        {
            var soundindex = random.Next(0, 5);
            var sound = $"Pickup_Gold_0{soundindex}.mp3";
            await PlaySound(sound);
        }
    }
}
