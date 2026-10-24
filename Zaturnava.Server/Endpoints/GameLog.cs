using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Server.Endpoints;

/// <summary>What has happened in a game, told for people in the ruleset's own names.</summary>
internal static class GameLog
{
    /// <summary>How many of the latest events a view carries.</summary>
    public const int Length = 40;

    public static ImmutableList<EventDto> LatestOf(GameSnapshot snapshot)
    {
        int skipped = Math.Max(0, snapshot.Events.Count - Length);
        return
        [
            .. snapshot
                .Events.Skip(skipped)
                .Select((happened, index) => EventOf(snapshot.Rules, happened, skipped + index)),
        ];
    }

    private static EventDto EventOf(IRulesApplying rules, GameEvent happened, int number) =>
        happened switch
        {
            DiceRolled rolled => new(
                number,
                EventDto.Rolled,
                rolled.Seat.Value,
                $"{rules.NameOf(rolled.Seat)} rolled {Faces(rules, rolled.Values)}.",
                null,
                null
            ),
            PieceCaptured captured => new(
                number,
                EventDto.Captured,
                captured.CapturedBy.Value,
                $"{rules.NameOf(captured.CapturedBy)} took {rules.NameOf(captured.Piece.Owner)}'s "
                    + $"{rules.NameOf(captured.Piece.Kind)} (+{captured.Points}).",
                null,
                null
            ),
            PieceMoved moved => new(
                number,
                EventDto.Moved,
                moved.Piece.Owner.Value,
                $"{rules.NameOf(moved.Piece.Owner)}'s {rules.NameOf(moved.Piece.Kind)} moved.",
                moved.From.Value,
                moved.To.Value
            ),
            TurnPassed passed => new(
                number,
                EventDto.Passed,
                passed.From.Value,
                $"{rules.NameOf(passed.To)} to play.",
                null,
                null
            ),
            _ => throw new NotSupportedException($"There is no way yet to tell of {happened.GetType().Name}."),
        };

    private static string Faces(IRulesApplying rules, IEnumerable<int> values) =>
        string.Join(" and ", values.Select(value => $"{value} ({rules.NameOfDie(value)})"));
}
