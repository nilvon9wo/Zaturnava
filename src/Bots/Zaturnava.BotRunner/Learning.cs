using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;
using Net.NowhereAtAll.Zaturnava.Bots;
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
/// and judges positions in the middle of it too. Each round (<c>epoch</c>) goes through every seat's view of
/// every game once, in an order of its own, and how far off the network was on average is written to
/// <paramref name="progress"/>. A network may start from one learned before, or from <see cref="Start"/>:
/// judging as Sage does, so that what it learns corrects Sage.
/// </summary>
internal sealed class Learning(IRulesApplying rules, TextWriter progress)
{
    private readonly IRulesApplying _rules = rules;
    private readonly TextWriter _progress = progress;

    /// <summary>
    /// A network that has learned nothing yet but judges as Sage does, by Sage's weights for the ruleset: its
    /// <paramref name="hidden"/> units change nothing until it learns.
    /// </summary>
    public ValueNetwork Start([Range(1, int.MaxValue)] int hidden, [Required] Random random)
    {
        PositionEncoder encoder = new(this._rules);
        SageWeights weights = SageWeights.Learned.GetValueOrDefault(this._rules.Id, SageWeights.Default);
        return ValueNetwork.Judging(encoder.Size, hidden, encoder.Weighing(weights), ValueNetwork.MarginScale, random);
    }

    public async Task<ValueNetworkData> Learn(
        [Required] IEnumerable<string> histories,
        [Required] ValueNetwork network,
        [Required] LearningPace pace,
        [Required] Random random,
        CancellationToken cancellationToken
    )
    {
        PositionEncoder encoder = new(this._rules);
        ImmutableList<Sequence> sequences =
        [
            .. histories
                .Where(line => line.Length > 0)
                .Select(line => JsonSerializer.Deserialize<GameHistory>(line, JsonSerializerOptions.Web)!)
                .Where(history => history.RulesetId == this._rules.Id && history.Outcome is not null)
                .SelectMany(history => this.SequencesOf(encoder, history)),
        ];
        int positions = sequences.Sum(sequence => sequence.Inputs.Count);
        for (int epoch = 1; epoch <= pace.Epochs; epoch++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double error = sequences
                .OrderBy(_ => random.Next())
                .Sum(sequence => Taught(network, sequence, pace));
            await this._progress.WriteLineAsync(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"epoch {epoch}: {positions} positions, error {error / Math.Max(1, positions):0.####}"
                )
            );
        }

        return network.ToData(this._rules.Id);
    }

    /// <summary>
    /// The network taught one seat's view of a game; how far off it was, squared, all told. Each step is the
    /// rate shared out over how big the position's numbers are (normalized least mean squares): a position of
    /// big numbers (a late game's scores and counts) would otherwise move the network past what it is taught,
    /// further each time, until its numbers are no numbers at all.
    /// </summary>
    private static double Taught(ValueNetwork network, Sequence sequence, LearningPace pace) =>
        TargetsOf(network, sequence, pace.Lambda)
            .Select((target, at) => network.Learn(
                sequence.Inputs[at],
                target.Outcome,
                target.Margin,
                pace.Rate / (1 + sequence.Inputs[at].Sum(entry => entry.Value * entry.Value))
            ))
            .Sum();

    /// <summary>
    /// What each position is taught (its λ-return, as temporal-difference learning has it): from the last
    /// back, how the game went, mixed with what the network now judges of the position after it, by
    /// <paramref name="lambda"/>. At 1, each is taught how the game went, alone; at 0, how the network judges
    /// the next position, alone, but the last.
    /// </summary>
    private static (double Outcome, double Margin)[] TargetsOf(ValueNetwork network, Sequence sequence, double lambda)
    {
        (double Outcome, double Margin)[] targets = new (double, double)[sequence.Inputs.Count];
        (double Outcome, double Margin) target = (sequence.Outcome, sequence.Margin);
        for (int at = targets.Length - 1; at >= 0; at--)
        {
            targets[at] = target;
            (double outcome, double margin) = network.Evaluate(sequence.Inputs[at]);
            target = (
                ((1 - lambda) * outcome) + (lambda * target.Outcome),
                ((1 - lambda) * margin) + (lambda * target.Margin)
            );
        }

        return targets;
    }

    /// <summary>The game as each seat saw it, position by position, with how it went for the seat's team.</summary>
    private IEnumerable<Sequence> SequencesOf(PositionEncoder encoder, GameHistory history)
    {
        ImmutableList<GameState> positions = this.PositionsOf(history);
        return positions.Take(1).SelectMany(start => start.Seats.Select(seat => new Sequence(
            [.. positions.Select(state => encoder.Encode(state, seat.Id))],
            OutcomeFor(start, history.Outcome!, seat.Id),
            LeadOf(start, history, seat.Id) / ValueNetwork.MarginScale
        )));
    }

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

    /// <summary>A game as one seat saw it, as a network sees each position, and how it went for that seat.</summary>
    private sealed record Sequence(
        ImmutableList<ImmutableList<(int Index, double Value)>> Inputs,
        double Outcome,
        double Margin
    );
}

/// <summary>
/// How a network learns: in so many rounds (<see cref="Epochs"/>); by how much each position is taught how the
/// game went rather than what follows it (<see cref="Lambda"/>, from 0 to 1); and how big a step it takes
/// towards each (<see cref="Rate"/>).
/// </summary>
internal sealed record LearningPace(int Epochs, double Lambda, double Rate);
