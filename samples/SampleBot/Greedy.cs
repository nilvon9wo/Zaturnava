using System.Collections.Immutable;
using System.Globalization;

namespace SampleBot;

/// <summary>
/// A mechanical mind, no AI at all: it takes the most valuable enemy piece it can; else rolls, or moves, at
/// random; and never gives anything away (it declines ransoms, and never proposes ending the game). It is about as
/// strong as the server's Midas, no stronger: its point is to show the loop, not to win. Replace it
/// (<see cref="IChoosing"/>) to do better.
/// </summary>
internal sealed class Greedy(Random random) : IChoosing
{
    /// <summary>
    /// What taking each kind of piece is worth, for Chaturaji 2027 (docs/rules.md); any other kind, such as Grand
    /// Legions' many, counts as a Pawn. A better mind would read worth from the rules, or learn it.
    /// </summary>
    private static readonly ImmutableDictionary<string, int> Worth =
        ImmutableDictionary<string, int>.Empty
            .Add("pawn", 1)
            .Add("boat", 2)
            .Add("horse", 3)
            .Add("elephant", 4)
            .Add("raja", 5);

    private readonly Random _random = random;

    public GameAction Choose(GameView view, int seat) =>
        view.LegalActions
            .Select(action => (action, score: ScoreOf(view, seat, action) + (this._random.NextDouble() / 10)))
            .MaxBy(scored => scored.score)
            .action;

    /// <summary>
    /// How much the bot likes the action: a capture by what it takes; doing something over passing; and nothing
    /// that ends its game early, or hands back what it holds.
    /// </summary>
    private static double ScoreOf(GameView view, int seat, GameAction action) =>
        action.Kind switch
        {
            "move" => CaptureOf(view, seat, action),
            "roll" or "rejoin" => 1,
            "promote" or "escape" or "place" => 1,
            "answer-end" => IsAgreeing(action) == IsLeading(view, seat) ? 1 : -1,
            "pass" or "decline" => 0,
            _ => -10,
        };

    /// <summary>
    /// What a move takes, if anything: ten times an enemy piece's worth; less than nothing for an ally's (some games
    /// allow it); a little more than nothing for a quiet move, which beats passing.
    /// </summary>
    private static double CaptureOf(GameView view, int seat, GameAction action)
    {
        int to = int.Parse(action.Arguments["to"], CultureInfo.InvariantCulture);
        Piece? taken = view.Pieces.FirstOrDefault(piece => piece.Cell == to);
        return taken switch
        {
            null => 0.5,
            _ when view.Seats[taken.Owner].Team == view.Seats[seat].Team => -5,
            _ => 10 * Worth.GetValueOrDefault(taken.Kind, 1),
        };
    }

    private static bool IsAgreeing(GameAction action) =>
        string.Equals(action.Arguments.GetValueOrDefault("agrees"), "true", StringComparison.Ordinal);

    /// <summary>Whether the bot's team has more points than any other.</summary>
    private static bool IsLeading(GameView view, int seat)
    {
        int team = view.Seats[seat].Team;
        ImmutableDictionary<int, int> points = view.Seats
            .GroupBy(other => other.Team)
            .ToImmutableDictionary(group => group.Key, group => group.Sum(other => other.Score));
        return points.Where(other => other.Key != team).All(other => points[team] > other.Value);
    }
}
