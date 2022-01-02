namespace BlazorRogue.Vision
{
  abstract class Visibility
  {
    /// <param name="origin">The location of the monster whose field of view will be calculated.</param>
    /// <param name="rangeLimit">The maximum distance from the origin that tiles will be lit.
    /// If equal to -1, no limit will be applied.
    /// </param>
    public abstract void Compute(LevelPoint origin, int rangeLimit);
  }
}
