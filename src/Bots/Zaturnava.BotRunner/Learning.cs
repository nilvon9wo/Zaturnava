using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;
using Net.NowhereAtAll.Zaturnava.Bots.Learning;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.BotRunner;

/// <summary>
/// Trains a network to judge positions (<see cref="ValueNetwork"/>, which Athena plays by) on games played:
/// each game's history, as self-play writes it or a server gives it, replayed; each position seen from every
/// seat (<see cref="PositionEncoder"/>), and taught how the game went for that seat's team: won, drawn or
/// lost, and by how many points. Games of another ruleset, and games that did not end, are passed over. Every
/// position is learned from, not only those a turn begins with, as Athena looks through the rest of its turn
/// and judges positions in the middle of it too. Each round (<c>epoch</c>) goes through every position once,
/// in an order of its own, and how far off the network was on average is written to
/// <paramref name="progress"/>.
/// </summary>
internal sealed class Learning(IRulesApplying rules, TextWriter progress)
{
    /// <summary>How big a step the network takes towards each position.</summary>
    public const double Rate = 0.02;

    private readonly IRulesApplying _rules = rules;
    private readonly TextWriter _progress = progress;

    public async Task<ValueNetworkData> Learn(
        [Required] IEnumerable<string> histories,
        [Range(1, int.MaxValue)] int epochs,
        [Range(1, int.MaxValue)] int hidden,
        [Required] Random random,
        CancellationToken cancellationToken
    )
    {
        PositionEncoder encoder = new(this._rules);
        ImmutableList<Sample> samples =
        [
            .. histories
                .Where(line => line.Length > 0)
                .Select(line => JsonSerializer.Deserialize<GameHistory>(line, JsonSerializerOptions.Web)!)
                .Where(history => history.RulesetId == this._rules.Id && history.Outcome is not null)
                .SelectMany(history => this.SamplesOf(encoder, history)),
        ];
        ValueNetwork network = ValueNetwork.Untrained(encoder.Size, hidden, random);
        for (int epoch = 1; epoch <= epochs; epoch++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double error = samples
                .OrderBy(_ => random.Next())
                .Sum(sample => network.Learn(sample.Input, sample.Outcome, sample.Margin, Rate));
            await this._progress.WriteLineAsync(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"epoch {epoch}: {samples.Count} positions, error {error / Math.Max(1, samples.Count):0.####}"
                )
            );
        }

        return network.ToData(this._rules.Id);
    }

    /// <summary>Each position, from each seat, with how the game went for its team.</summary>
    private IEnumerable<Sample> SamplesOf(PositionEncoder encoder, GameHistory history) =>
        this.PositionsOf(history).SelectMany(state => state.Seats.Select(seat => new Sample(
            [.. encoder.Encode(state, seat.Id)],
            OutcomeFor(state, history.Outcome!, seat.Id),
            LeadOf(state, history, seat.Id) / ValueNetwork.MarginScale
        )));

    /// <summary>The game replayed: where it stood after each action, while it went on.</summary>
    private ImmutableList<GameState> PositionsOf(GameHistory history)
    {
        GameState start = ScenarioStart.StateOf(history.Actions, this._rules);
        return
        [
            .. history.Actions
                .Where(action => action.Kind != ScenarioStart.Kind)
                .Aggregate(
                    ImmutableList.Create(start),
                    (sofar, action) => sofar.Add(this._rules.Apply(sofar[^1], this._rules.Parse(action)).State)
                )
                .Where(state => state.Outcome is null),
        ];
    }

    /// <summary>1 if the seat's team won, -1 if it lost, 0 if no one did.</summary>
    private static double OutcomeFor(GameState state, HistoryOutcome outcome, SeatId seat) =>
        (outcome.Winners.IsEmpty, outcome.Winners.Any(winner => IsAllied(state, winner, seat))) switch
        {
            (true, _) => 0,
            (_, true) => 1,
            _ => -1,
        };

    /// <summary>The final points of the seat's team, less its enemies'.</summary>
    private static int LeadOf(GameState state, GameHistory history, SeatId seat) =>
        history.Seats.Sum(other => (IsAllied(state, other.Id, seat) ? 1 : -1) * other.Score);

    private static bool IsAllied(GameState state, int other, SeatId seat) =>
        state.Teams.AreAllied(new SeatId(other), seat);

    /// <summary>A position, as a network sees it, and how its game went for the seat it is seen from.</summary>
    private sealed record Sample((int Index, double Value)[] Input, double Outcome, double Margin);
}
