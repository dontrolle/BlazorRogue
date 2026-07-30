namespace BlazorRogue;

class EffectsSystem
{
    public bool Shake { get; set; }

    internal void Reset() => Shake = false;
}
