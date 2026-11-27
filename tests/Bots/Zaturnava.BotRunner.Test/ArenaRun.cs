using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.BotRunner.Test;

/// <summary>
/// Not a test of the code, but bots rated on request, through the test runner, so that no other command need be
/// allowed to run them: when <c>artifacts/arena/request.json</c> asks for an arena (<see cref="ArenaRequest"/>),
/// every pair of its bots plays as many games with each team moving first, and how each pair fared, and how long it
/// took, is written to <c>artifacts/arena/results.json</c>; a game won counts for its team, one that ran out of
/// steps for the team ahead on points, if either is. With <c>botAssembly</c>, the bots of that assembly may play too;
/// <c>"self"</c> names this one, with its challengers (<see cref="Challenger"/>). Each game is played with the bots
/// seated both ways, with the same dice, and the pairs of games are counted too. With <c>starts</c>, a file of games
/// played before (as <c>artifacts/blunders/histories.json</c>), the games start where those came down to
/// <c>pieces</c> pieces (<see cref="Starts"/>): for ways of ending a game, each game spent where they show.
/// With no request, or one of fewer than two bots, it is skipped, as on CI.
/// </summary>
public sealed class ArenaRun
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerOptions.Web) { WriteIndented = true };

    private static readonly Chaturaji2027Rules Rules = new();

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

    /// <summary>
    /// How the first bot fared against the second, and how long their games took; and in how many games a leader of
    /// each sat on a throne not its team's, in a Chaturaji 2027 arena (the user, 2026-11-27: seizing a throne is rare,
    /// "but when it counts, it really can count"), so that it is known whether a way of guarding thrones can show.
    /// </summary>
    /// <remarks>
    /// Each game is played twice, the bots seated one way and then the other, with the same dice (the user's thought
    /// of 2026-11-27, of testing bots more efficiently): so the luck of the dice falls to each in turn, and a pair of
    /// games is won by the bot that did better with the same luck. The pairs show a difference sooner than the games
    /// alone.
    /// </remarks>
    /// <remarks>
    /// Started from games played before, each start is given to each bot on each side, whoever reached it (the user,
    /// 2026-11-27: "giving them different endgames - not necessarily endgames they reached on their own - and see how
    /// well they do or don't finish, including whether they maintain their leads or are self-sabotaging"); so each
    /// bot's finishing is counted too, by whether its team led at the start (<see cref="Finishing"/>).
    /// </remarks>
    private static async Task<object> Pair(ArenaRequest request, string first, string second)
    {
        Stopwatch watch = Stopwatch.StartNew();
        int seed = Random.Shared.Next(1, int.MaxValue / 2);
        ImmutableList<Played> starts = request.Starts is { } file
            ? Starts.From(
                Rules,
                await File.ReadAllTextAsync(Path.Combine(Root(), file), TestContext.Current.CancellationToken),
                request.Pieces
            )
            : [];
        ImmutableList<Fared> ahead = await Margins(request, [first, second, first, second], [0, 2], seed, starts);
        ImmutableList<Fared> behind = await Margins(request, [second, first, second, first], [1, 3], seed, starts);
        ImmutableList<Fared> games = [.. ahead, .. behind];
        ImmutableList<int> pairs =
            [.. ahead.Zip(behind, (one, other) => Math.Sign(one.Margin) + Math.Sign(other.Margin))];
        return new
        {
            First = first,
            Second = second,
            Won = games.Count(game => game.Margin > 0),
            Lost = games.Count(game => game.Margin < 0),
            Drawn = games.Count(game => game.Margin == 0),
            Pairs = new
            {
                Won = pairs.Count(pair => pair > 0),
                Lost = pairs.Count(pair => pair < 0),
                Even = pairs.Count(pair => pair == 0),
            },
            ThronesSeized = new
            {
                ByFirst = games.Count(game => game.Seized),
                BySecond = games.Count(game => game.WasSeized),
            },
            Finishing = starts.IsEmpty
                ? null
                : new
                {
                    First = Finishing(games),
                    Second = Finishing(games.Select(game => game with { Lead = -game.Lead, Margin = -game.Margin })),
                },
            Seconds = Math.Round(watch.Elapsed.TotalSeconds),
        };
    }

    /// <summary>
    /// How a bot finished the games it was given, for its team: of those it began ahead in (on points and on the
    /// board), how many it won and how many it threw away; of those it began behind in, how many it turned round.
    /// </summary>
    private static object Finishing(IEnumerable<Fared> games)
    {
        ImmutableList<Fared> given = [.. games];
        return new
        {
            Ahead = given.Count(game => game.Lead > 0),
            Kept = given.Count(game => game is { Lead: > 0, Margin: > 0 }),
            Thrown = given.Count(game => game is { Lead: > 0, Margin: < 0 }),
            Behind = given.Count(game => game.Lead < 0),
            Turned = given.Count(game => game is { Lead: < 0, Margin: > 0 }),
        };
    }

    /// <summary>
    /// How far the <paramref name="seats"/>' team came out ahead in each game the bots played, and whether a leader of
    /// either team sat on a throne not its team's once the game was the bots'; each game in order, its dice from the
    /// <paramref name="seed"/>, and how far the team led as it began, from each of the <paramref name="starts"/> in
    /// turn.
    /// </summary>
    private static async Task<ImmutableList<Fared>> Margins(
        ArenaRequest request,
        ImmutableList<string> bots,
        ImmutableList<int> seats,
        int seed,
        ImmutableList<Played> starts
    )
    {
        await using StringWriter output = new();
        string[] args =
        [
            "--ruleset", request.Ruleset, "--bots", string.Join(',', bots), "--games", $"{request.Games}",
            "--max-steps", $"{request.MaximumSteps}", "--seed", $"{seed}",
            .. request.Starts is { } file
                ? ["--starts", Path.Combine(Root(), file), "--pieces", $"{request.Pieces}"]
                : (string[])[],
            .. request.BotAssembly switch
            {
                null => (string[])[],
                "self" => ["--bot-assembly", typeof(Challenger).Assembly.Location],
                { } assembly => ["--bot-assembly", Path.Combine(Root(), assembly)],
            },
        ];
        _ = await SelfPlayCommand.Run(args, output, TextWriter.Null, TestContext.Current.CancellationToken);
        return
        [
            .. output.ToString()
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => JsonElement.Parse(line))
                .Select((game, at) =>
                {
                    Played? start = starts.IsEmpty ? null : starts[at % starts.Count];
                    int given = start?.Actions.Count ?? 0;
                    ImmutableHashSet<int> usurpers = request.Ruleset == Rules.Id ? Usurpers(game, given) : [];
                    return new Fared(
                        MarginOf(game, seats),
                        usurpers.Overlaps(seats),
                        usurpers.Except(seats).Count > 0,
                        start is null ? 0 : LeadOf(start.State, seats)
                    );
                }),
        ];
    }

    /// <summary>
    /// How far the <paramref name="seats"/>' team leads, its points and what its pieces on the board are worth, less
    /// its enemies'.
    /// </summary>
    private static int LeadOf(GameState state, ImmutableList<int> seats) =>
        state.Scores.Sum(score => (seats.Contains(score.Key.Value) ? 1 : -1) * score.Value)
            + state.Position.Occupants.Values.Sum(piece =>
                (seats.Contains(piece.Owner.Value) ? 1 : -1) * Rules.WorthOf(piece.Kind)
            );

    /// <summary>
    /// The seats a leader of which sat, after the first <paramref name="given"/> actions (those of a game played
    /// before, the game started from), on a throne not its team's.
    /// </summary>
    private static ImmutableHashSet<int> Usurpers(JsonElement game, int given)
    {
        ImmutableList<ActionDescriptor> actions = [.. game.GetProperty("actions").EnumerateArray().Select(Described)];
        GameState state = ScenarioStart.StateOf(actions, Rules);
        ImmutableHashSet<int> usurpers = [];
        int at = actions.Count(ScenarioStart.IsStart);
        foreach (ActionDescriptor action in actions.Where(action => !ScenarioStart.IsStart(action)))
        {
            at++;
            state = Rules.Apply(state, Rules.Parse(action)).State;
            usurpers = at <= given ? usurpers : usurpers.Union(UsurpersIn(state));
        }

        return usurpers;
    }

    /// <summary>The seats a leader of which sits on a throne not its team's.</summary>
    private static IEnumerable<int> UsurpersIn(GameState state) =>
        Rules.ThronesOf(state)
            .Select(throne => (Throne: throne.Key, Occupant: state.Position.PieceAt(throne.Value)))
            .Where(held => held.Occupant is not null
                && Rules.IsLeader(held.Occupant.Kind)
                && !state.Teams.AreAllied(held.Occupant.Owner, held.Throne)
            )
            .Select(held => held.Occupant!.Owner.Value);

    private static ActionDescriptor Described(JsonElement action) =>
        new(
            action.GetProperty("kind").GetString()!,
            action.GetProperty("arguments").EnumerateObject().ToImmutableSortedDictionary(
                argument => argument.Name,
                argument => argument.Value.GetString() ?? string.Empty,
                StringComparer.Ordinal
            )
        );

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

    /// <summary>
    /// The bots to play one another, the ruleset, the games each way, and the most steps in each; an assembly of
    /// more bots to seat beside ours, if any, from the repository's root or anywhere (as Fairy-Stockfish's); and games
    /// to start from, if any, and at how few pieces.
    /// </summary>
    private sealed record ArenaRequest(
        ImmutableList<string> Bots,
        string Ruleset = "chaturaji-2027",
        int Games = 10,
        int MaximumSteps = 2000,
        string? BotAssembly = null,
        string? Starts = null,
        int Pieces = 8
    );

    /// <summary>
    /// One game, for the first bot's team: how far it came out ahead, whether a leader of its own sat on a throne not
    /// its team's (<paramref name="Seized"/>), whether an enemy leader did (<paramref name="WasSeized"/>), and how far
    /// its team led as the game began (<see cref="LeadOf"/>; none, from the beginning).
    /// </summary>
    private sealed record Fared(int Margin, bool Seized, bool WasSeized, int Lead);
}
