using System.Collections.Immutable;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.BotRunner.Test;

/// <summary>
/// Not a test of the code, but two bots compared where they part ways, on request, through the test runner (the
/// user's thought of 2026-11-27: to "fork the universe" at a decision, and see how each game plays out): when
/// <c>artifacts/forks/request.json</c> asks (<see cref="ForkRequest"/>), the games it names are replayed, and at each
/// decision of theirs with no more than so many pieces on the board, both bots are asked what they would do. Where
/// they differ, the game is played out from each choice as many times over, by one bot in every seat, each pair of
/// play-outs with the same dice; each fork scores how much better the challenger's choice did for its team, from -2 to
/// 2. Forking only where the bots differ is what makes it practical: where they agree, the two universes are one. Their
/// mean, and its standard error, go to <c>artifacts/forks/results.json</c>. With no request, it is skipped, as on CI.
/// </summary>
public sealed class ForkRun
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerOptions.Web) { WriteIndented = true };

    private readonly Chaturaji2027Rules _rules = new();

    [Fact]
    public async Task Fork_WhenAskedTo_WritesHowMuchBetterTheChallengersChoicesDid()
    {
        // Arrange
        string folder = Path.Combine(Root(), "artifacts", "forks");
        string requestPath = Path.Combine(folder, "request.json");
        Assert.SkipUnless(File.Exists(requestPath), "No forks are requested.");
        ForkRequest request = JsonSerializer.Deserialize<ForkRequest>(
            await File.ReadAllTextAsync(requestPath, TestContext.Current.CancellationToken),
            Json
        )!;
        Assert.SkipWhen(request.Challenger is null, "The forks requested have no challenger.");
        ImmutableDictionary<string, IBotPlaying> bots = Bots();
        IBotPlaying challenger = bots[request.Challenger!];
        IBotPlaying baseline = bots[request.Baseline];
        IBotPlaying playout = bots[request.Playout];
        ImmutableList<Played> starts = Starts.From(
            this._rules,
            await File.ReadAllTextAsync(Path.Combine(Root(), request.Games), TestContext.Current.CancellationToken),
            request.Pieces
        );

        // Act - the decisions where they part, then each played out
        ImmutableList<(GameState State, GameAction Theirs, GameAction Ours)> forks = [];
        foreach (Played start in starts)
        {
            forks = forks.AddRange(await this.Parted(start, request, challenger, baseline));
            if (forks.Count >= request.Most)
            {
                break;
            }
        }

        ImmutableList<Fork> scored = [];
        foreach ((GameState state, GameAction theirs, GameAction ours) in forks.Take(request.Most))
        {
            double score = await this.Scored(state, ours, theirs, request, playout);
            scored = scored.Add(new Fork(Told(ours), Told(theirs), score));
            await Write(folder, request, scored);
        }

        // Assert
        Assert.True(File.Exists(Path.Combine(folder, "results.json")) || forks.IsEmpty);
    }

    /// <summary>
    /// The decisions where the two bots part ways, in the game from <paramref name="start"/> on: each position where a
    /// seat has more than one action and no dice await, and what each would do; the game going on as it was played.
    /// </summary>
    private async Task<ImmutableList<(GameState State, GameAction Theirs, GameAction Ours)>> Parted(
        Played start,
        ForkRequest request,
        IBotPlaying challenger,
        IBotPlaying baseline
    )
    {
        // the game as it went on is not kept with the start, so it is played on by the baseline, as it would have
        Played played = await DecisionLoop.PlayWhile(
            this._rules,
            start,
            _ => baseline,
            new RandomDice(new Random(start.Actions.Count)),
            request.MaximumSteps,
            TestContext.Current.CancellationToken
        );
        ImmutableList<(GameState State, GameAction Theirs, GameAction Ours)> parted = [];
        GameState state = start.State;
        foreach (ActionDescriptor action in played.Actions.Skip(start.Actions.Count))
        {
            if (state.AwaitedDice is null && state.DecidingSeat is { } seat)
            {
                ImmutableList<GameAction> legal = this._rules.LegalActions(state);
                DecisionRequest asked = new(this._rules, state, seat, legal);
                GameAction theirs = await baseline.Decide(asked, TestContext.Current.CancellationToken);
                GameAction ours = await challenger.Decide(asked, TestContext.Current.CancellationToken);
                parted = legal.Count > 1 && !ours.Equals(theirs) ? parted.Add((state, theirs, ours)) : parted;
            }

            state = this._rules.Apply(state, this._rules.Parse(action)).State;
        }

        return parted;
    }

    /// <summary>
    /// How much better <paramref name="ours"/> did than <paramref name="theirs"/> for the deciding seat's team, as the
    /// mean over the play-outs of each pair's results (1 a win, 0 a draw, -1 a loss), with the same dice.
    /// </summary>
    private async Task<double> Scored(
        GameState state,
        GameAction ours,
        GameAction theirs,
        ForkRequest request,
        IBotPlaying playout
    )
    {
        SeatId seat = state.DecidingSeat!.Value;
        double total = 0;
        for (int rollout = 0; rollout < request.Rollouts; rollout++)
        {
            total += await this.Result(state, ours, seat, rollout, request, playout)
                - await this.Result(state, theirs, seat, rollout, request, playout);
        }

        return total / request.Rollouts;
    }

    /// <summary>How the game went for <paramref name="seat"/>'s team, played out from the action.</summary>
    private async Task<int> Result(
        GameState state,
        GameAction action,
        SeatId seat,
        int rollout,
        ForkRequest request,
        IBotPlaying playout
    )
    {
        Played after = new(this._rules.Apply(state, action).State, []);
        Played played = await DecisionLoop.PlayWhile(
            this._rules,
            after,
            _ => playout,
            new RandomDice(new Random(rollout + 1)),
            request.MaximumSteps,
            TestContext.Current.CancellationToken
        );
        GameState end = played.State;
        return end.Outcome is { } outcome
            ? outcome.IsDraw ? 0 : outcome.Winners.Any(winner => end.Teams.AreAllied(winner, seat)) ? 1 : -1
            : Math.Sign(end.Scores.Sum(score => (end.Teams.AreAllied(score.Key, seat) ? 1 : -1) * score.Value));
    }

    private static async Task Write(string folder, ForkRequest request, ImmutableList<Fork> scored)
    {
        ImmutableList<double> scores = [.. scored.Select(fork => fork.Score)];
        double mean = scores.Average();
        double spread = scores.Count > 1
            ? Math.Sqrt(scores.Sum(score => (score - mean) * (score - mean)) / (scores.Count - 1) / scores.Count)
            : 0;
        await File.WriteAllTextAsync(
            Path.Combine(folder, "results.json"),
            JsonSerializer.Serialize(
                new
                {
                    request.Challenger,
                    request.Baseline,
                    request.Playout,
                    Forks = scores.Count,
                    Mean = Math.Round(mean, 3),
                    StandardError = Math.Round(spread, 3),
                    Each = scored,
                },
                Json
            ),
            TestContext.Current.CancellationToken
        );
    }

    /// <summary>An action as its kind and its arguments.</summary>
    private static string Told(GameAction action)
    {
        ActionDescriptor described = action.Describe();
        return string.Join(
            ' ',
            [described.Kind, .. described.Arguments.Select(argument => $"{argument.Key}={argument.Value}")]
        );
    }

    /// <summary>Our bots and this assembly's challengers, by id, with a random safe across threads.</summary>
    private static ImmutableDictionary<string, IBotPlaying> Bots() =>
        new IBotPlaying[]
        {
            new AthenaBot(Random.Shared), new TacticianBot(Random.Shared), new SageBot(Random.Shared),
            new ArgusBot(Random.Shared),
        }
            .Concat(BotAssembly.BotsIn(typeof(Challenger).Assembly.Location, Random.Shared))
            .ToImmutableDictionary(bot => bot.Profile.Id);

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
    /// The bot to try, the one to try it against, and the one to play every seat in the play-outs; the games to
    /// replay, and at how few pieces to begin asking; how many play-outs for each choice, the most steps in each, and
    /// the most forks to play out.
    /// </summary>
    private sealed record ForkRequest(
        string? Challenger,
        string Baseline = "tactician",
        string Playout = "sage",
        string Games = "artifacts/blunders/histories.json",
        int Pieces = 8,
        int Rollouts = 20,
        int MaximumSteps = 400,
        int Most = 60
    );

    /// <summary>One fork: what the challenger would do, what the baseline would, and how much better it did.</summary>
    private sealed record Fork(string Ours, string Theirs, double Score);
}
