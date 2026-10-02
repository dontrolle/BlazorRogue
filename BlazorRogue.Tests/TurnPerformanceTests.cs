using System.Diagnostics;
using BlazorRogue.Tests.TestSupport;
using Xunit.Abstractions;

namespace BlazorRogue.Tests;

// Server-side half of the keypress-to-screen latency budget: how long Map.TakeTurn (player action,
// monster AI, visibility recompute) takes on real generated levels. Deliberately loose ceilings -
// wall-clock asserts on shared CI hardware are noisy, so these only catch order-of-magnitude
// regressions (e.g. an accidentally quadratic FOV or AI pass), not small drifts. Render/SignalR
// cost is not covered here.
public class TurnPerformanceTests(ITestOutputHelper output)
{
    const int Games = 5;
    const int TurnsPerGame = 150;
    const int WarmupTurns = 20;

    // Measured locally: median ~0.05 ms, p95 ~0.1 ms, max a few ms - these leave ~100x headroom.
    const double P95CeilingMs = 10;
    const double MaxCeilingMs = 100;

    [Fact]
    public void TakeTurnStaysWithinLatencyBudgetOnGeneratedLevels()
    {
        var samplesMs = new List<double>();

        for (int g = 0; g < Games; g++)
        {
            var driver = new HeadlessPlayDriver(seed: g);
            var policy = new RandomHazardAvoidingPolicy(new Random(g));

            for (int turn = 0; turn < TurnsPerGame && !driver.Map.IsGameOver; turn++)
            {
                var action = policy.NextAction(driver.Map);

                long start = Stopwatch.GetTimestamp();
                _ = driver.TakeTurn(action);
                double ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

                // Skip JIT/first-call noise at the start of each game.
                if (turn >= WarmupTurns)
                {
                    samplesMs.Add(ms);
                }
            }
        }

        Assert.NotEmpty(samplesMs);
        samplesMs.Sort();
        double median = samplesMs[samplesMs.Count / 2];
        double p95 = samplesMs[(int)(samplesMs.Count * 0.95)];
        double max = samplesMs[^1];
        output.WriteLine(
            $"TakeTurn over {samplesMs.Count} turns: median {median:F3} ms, p95 {p95:F3} ms, max {max:F3} ms"
        );

        Assert.True(p95 < P95CeilingMs, $"p95 turn time {p95:F1} ms exceeds {P95CeilingMs} ms");
        Assert.True(max < MaxCeilingMs, $"max turn time {max:F1} ms exceeds {MaxCeilingMs} ms");
    }
}
