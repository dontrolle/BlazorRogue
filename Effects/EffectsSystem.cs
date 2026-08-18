namespace BlazorRogue.Effects;

class EffectsSystem
{
    public bool Shake { get; set; }

    internal void Reset() => Shake = false;
}
