using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Bots;

namespace Net.NowhereAtAll.Zaturnava.BotRunner;

/// <summary>
/// Tunes Sage's weights by playing it against itself: each round, each weight in turn is nudged up - and,
/// if that does not win, down, unless it is nothing already - and a nudge is kept when Sage with it beats
/// Sage without it (<paramref name="match"/>) clearly enough: winning more than it loses by twice the
/// spread chance alone would give (the square root of the games won or lost), as two evenly matched would
/// so win about one time in forty-four. What each nudge did is written to <paramref name="progress"/>, as it
/// goes.
/// </summary>
/// <remarks>
/// Proposed. It was a tenth of the games won or lost; but over 60 games that let a nudge changing nothing at all
/// be kept (35 won to 25), and with some forty nudges a round, several such each round.
/// </remarks>
internal sealed class Training(
    Func<SageWeights, SageWeights, CancellationToken, Task<(int Won, int Lost)>> match,
    TextWriter progress
)
{
    private const double Spreads = 2;
    private const double Up = 1.3;
    private const double Down = 0.7;
    private const double FromNothing = 0.3;

    private readonly Func<SageWeights, SageWeights, CancellationToken, Task<(int Won, int Lost)>> _match = match;
    private readonly TextWriter _progress = progress;

    public Task<SageWeights> Tune(SageWeights start, int rounds, CancellationToken cancellationToken) =>
        Enumerable
            .Range(1, rounds)
            .SelectMany(round => start.Names.Select((_, index) => (Round: round, Index: index)))
            .Aggregate(
                Task.FromResult(start),
                async (tuned, step) => await this.Nudged(await tuned, step.Round, step.Index, cancellationToken)
            );

    /// <summary>The weights with the one at <paramref name="index"/> nudged up, or else down, if either wins.</summary>
    private async Task<SageWeights> Nudged(
        SageWeights current,
        int round,
        int index,
        CancellationToken cancellationToken
    )
    {
        double value = current.Values[index];
        SageWeights up = current.With(index, value > 0 ? value * Up : FromNothing);
        SageWeights down = current.With(index, value * Down);
        return await this.Kept(current, up, round, index, cancellationToken)
            ?? (value == 0 ? null : await this.Kept(current, down, round, index, cancellationToken)) // none lower
            ?? current;
    }

    /// <summary>The candidate, if it beats the current weights clearly enough; else null.</summary>
    private async Task<SageWeights?> Kept(
        SageWeights current,
        SageWeights candidate,
        int round,
        int index,
        CancellationToken cancellationToken
    )
    {
        (int won, int lost) = await this._match(candidate, current, cancellationToken);
        bool isKept = won - lost > Spreads * Math.Sqrt(won + lost);
        await this._progress.WriteLineAsync(
            string.Create(
                CultureInfo.InvariantCulture,
                $"round {round}, {current.Names[index]} {current.Values[index]:0.###} to "
                    + $"{candidate.Values[index]:0.###}: won {won}, lost {lost}{(isKept ? ", kept" : string.Empty)}"
            )
        );
        return isKept ? candidate : null;
    }
}
