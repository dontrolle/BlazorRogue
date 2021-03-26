using System;

namespace BlazorRogue
{
    public class EffectsSystem
    {
        public bool Shake { get; set; }

        internal void Reset()
        {
            Shake = false;
        }
    }
}
