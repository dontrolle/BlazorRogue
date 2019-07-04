using System;
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

        public async void PlayWalkSound()
        {
            var soundindex = random.Next(0, 10);
            await jsRuntime.InvokeAsync<object>("blazorroguefuncs.playSound", $"sound/{footstepDirtPrefix}{soundindex}.mp3");
        }
    }
}
