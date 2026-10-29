using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.BotRunner;

/// <summary>
/// Sage with one set of weights against Sage with another, over <paramref name="games"/> games with each
/// in the first seat's team and as many with each in the other, in parallel: which won each game, by its
/// result, or on points, where it ran out of steps.
/// </summary>
internal sealed class SageMatch(IRulesApplying rules, IDiceRolling dice, int games, int maximumSteps)
{
    private readonly IRulesApplying _rules = rules;
    private readonly IDiceRolling _dice = dice;
    private readonly int _games = games;
    private readonly int _maximumSteps = maximumSteps;

    /// <summary>
    /// How many games the <paramref name="candidate"/> won, and lost, against the <paramref name="current"/>.
    /// </summary>
    public async Task<(int Won, int Lost)> Play(
        SageWeights candidate,
        SageWeights current,
        CancellationToken cancellationToken
    )
    {
        ImmutableList<int> margins =
        [
            .. await Task.WhenAll(
                Enumerable.Range(0, 2 * this._games).Select(game => Task.Run(
                    () => this.MarginOf(candidate, current, isFirst: game % 2 == 0, cancellationToken),
                    cancellationToken
                ))
            ),
        ];
        return (margins.Count(margin => margin > 0), margins.Count(margin => margin < 0));
    }

    /// <summary>
    /// How far the team of <paramref name="seat"/> came out ahead at the end: one game won, or lost, if the
    /// game was won; else what it scored, less what the other team did.
    /// </summary>
    internal static int MarginOf(GameState final, SeatId seat) =>
        (final.Outcome, final.Outcome?.Winners.Contains(seat)) switch
        {
            ({ Winners.IsEmpty: false }, true) => 1,
            ({ Winners.IsEmpty: false }, _) => -1,
            _ => final.Scores.Sum(score => final.Teams.AreAllied(score.Key, seat) ? score.Value : -score.Value),
        };

    private async Task<int> MarginOf(
        SageWeights candidate,
        SageWeights current,
        bool isFirst,
        CancellationToken cancellationToken
    )
    {
        GameState start = this._rules.CreateInitialState();
        SeatId first = start.Seats[0].Id;
        SeatId candidateSeat = isFirst ? first : start.Seats.First(seat => !start.Teams.AreAllied(seat.Id, first)).Id;
        ImmutableList<IDeciding> players =
        [
            .. start.Seats.Select(seat => new SageBot(
                Random.Shared,
                start.Teams.AreAllied(seat.Id, candidateSeat) ? candidate : current
            )),
        ];
        Played played = await DecisionLoop.PlayAll(
            this._rules,
            players,
            this._dice,
            this._maximumSteps,
            cancellationToken
        );
        return MarginOf(played.State, candidateSeat);
    }
}
