namespace BlazorRogue.Effects;

class EffectsSystem
{
    public bool Shake { get; set; }

    internal void Reset() => Shake = false;

    /// <summary>
    /// Reads and clears <see cref="Shake"/> in one step, so a hit's screen shake plays on exactly
    /// the render that consumes it - not on every subsequent re-render (e.g. from a keypress that
    /// doesn't take a turn) until the next player action happens to reset it.
    /// </summary>
    internal bool ConsumeShake()
    {
        bool shake = Shake;
        Shake = false;
        return shake;
    }
}
