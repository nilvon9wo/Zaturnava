using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;

namespace Net.NowhereAtAll.Zaturnava.BotRunner.Test;

/// <summary>
/// Not a test of the code, but bots rated on request, through the test runner, so that no other command need be
/// allowed to run them: when <c>artifacts/arena/request.json</c> asks for an arena (<see cref="ArenaRequest"/>),
/// every pair of its bots plays as many games with each team moving first, and how each pair fared, and how long it
/// took, is written to <c>artifacts/arena/results.json</c>; a game won counts for its team, one that ran out of
/// steps for the team ahead on points, if either is. With no request, or one of fewer than two bots, it is skipped,
/// as on CI.
/// </summary>
public sealed class ArenaRun
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerOptions.Web) { WriteIndented = true };

    [Fact]
    public async Task Play_WhenAnArenaIsRequested_WritesHowEachPairFared()
    {
        // Arrange
        string folder = Path.Combine(Root(), "artifacts", "arena");
        string requestPath = Path.Combine(folder, "request.json");
        Assert.SkipUnless(File.Exists(requestPath), "No arena is requested.");
        ArenaRequest request = JsonSerializer.Deserialize<ArenaRequest>(
            await File.ReadAllTextAsync(requestPath, TestContext.Current.CancellationToken),
            Json
        )!;
        Assert.SkipWhen(request.Bots.Count < 2, "The arena requested has no pair of bots.");

        // Act - one pair at a time, so that each is timed alone
        ImmutableList<object> results = [];
        foreach ((string first, string second) in request.Bots
            .SelectMany((first, at) => request.Bots.Skip(at + 1).Select(second => (first, second))))
        {
            results = results.Add(await Pair(request, first, second));
            await File.WriteAllTextAsync(
                Path.Combine(folder, "results.json"),
                JsonSerializer.Serialize(results, Json),
                TestContext.Current.CancellationToken
            );
        }

        // Assert
        Assert.NotEmpty(results);
    }

    /// <summary>How the first bot fared against the second, and how long their games took.</summary>
    private static async Task<object> Pair(ArenaRequest request, string first, string second)
    {
        Stopwatch watch = Stopwatch.StartNew();
        ImmutableList<int> margins =
        [
            .. await Margins(request, [first, second, first, second], [0, 2]),
            .. await Margins(request, [second, first, second, first], [1, 3]),
        ];
        return new
        {
            First = first,
            Second = second,
            Won = margins.Count(margin => margin > 0),
            Lost = margins.Count(margin => margin < 0),
            Drawn = margins.Count(margin => margin == 0),
            Seconds = Math.Round(watch.Elapsed.TotalSeconds),
        };
    }

    /// <summary>How far the <paramref name="seats"/>' team came out ahead in each game the bots played.</summary>
    private static async Task<ImmutableList<int>> Margins(
        ArenaRequest request,
        ImmutableList<string> bots,
        ImmutableList<int> seats
    )
    {
        await using StringWriter output = new();
        string[] args =
        [
            "--ruleset", request.Ruleset, "--bots", string.Join(',', bots), "--games", $"{request.Games}",
            "--max-steps", $"{request.MaximumSteps}",
        ];
        _ = await SelfPlayCommand.Run(args, output, TextWriter.Null, TestContext.Current.CancellationToken);
        return
        [
            .. output.ToString()
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => MarginOf(JsonDocument.Parse(line).RootElement, seats)),
        ];
    }

    private static int MarginOf(JsonElement game, ImmutableList<int> seats)
    {
        JsonElement outcome = game.GetProperty("outcome");
        if (outcome.ValueKind == JsonValueKind.Null)
        {
            return game.GetProperty("seats").EnumerateArray().Sum(seat =>
                (seats.Contains(seat.GetProperty("id").GetInt32()) ? 1 : -1) * seat.GetProperty("score").GetInt32()
            );
        }

        ImmutableList<int> winners =
            [.. outcome.GetProperty("winners").EnumerateArray().Select(winner => winner.GetInt32())];
        return winners.IsEmpty
            ? 0
            : seats.Contains(winners[0]) ? 1 : -1;
    }

    /// <summary>The repository's root: the nearest folder above the tests holding the solution.</summary>
    private static string Root()
    {
        DirectoryInfo folder = new(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(folder.FullName, "Zaturnava.slnx")))
        {
            folder = folder.Parent!;
        }

        return folder.FullName;
    }

    /// <summary>The bots to play one another, the ruleset, the games each way, and the most steps in each.</summary>
    private sealed record ArenaRequest(
        ImmutableList<string> Bots,
        string Ruleset = "chaturaji-2027",
        int Games = 10,
        int MaximumSteps = 2000
    );
}
