namespace BlazorRogue.Tests.TestSupport;

/// <summary>
/// Runs a generation check over a few fixed, consecutive seeds - so it sees several distinct but
/// reproducible layouts - and names the failing seed, since an assertion inside the loop would not.
/// Replay a failure with <c>new Game(seed: n)</c>.
/// </summary>
static class SeededRuns
{
    /// <summary>Calls <paramref name="check"/> with seeds 0 to <paramref name="count"/> - 1.</summary>
    public static void Each(int count, Action<int> check)
    {
        for (int seed = 0; seed < count; seed++)
        {
#pragma warning disable CA1031 // Rethrown below, wrapped with the seed that produced it.
            try
            {
                check(seed);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Failed with seed {seed} (replay with new Game(seed: {seed})): {e.Message}",
                    e
                );
            }
#pragma warning restore CA1031
        }
    }
}
