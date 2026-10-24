using System.Collections.Immutable;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Server.Endpoints;

/// <summary>What has happened in a game, told for people in the ruleset's own names.</summary>
internal static class GameLog
{
    /// <summary>How many of the latest events a view carries.</summary>
    public const int Length = 40;

    /// <summary>The latest events, each told knowing those before it: a move says if it was rolled for.</summary>
    public static ImmutableList<EventDto> LatestOf(GameSnapshot snapshot)
    {
        Teller start = new(snapshot.Rules, [], HasRolled: false);
        ImmutableList<EventDto> told = snapshot.Events
            .Select((happened, number) => (happened, number))
            .Aggregate(start, (teller, next) => teller.Tell(next.happened, next.number))
            .Told;
        return told.Count > Length
            ? told.RemoveRange(0, told.Count - Length)
            : told;
    }

    /// <summary>"Black", "Black and Green", "Black, Yellow and Green".</summary>
    public static string Listed(IRulesApplying rules, ImmutableList<SeatId> seats)
    {
        ImmutableList<string> names = [.. seats.Select(rules.NameOf)];
        return names.Count > 1
            ? $"{string.Join(", ", names.RemoveAt(names.Count - 1))} and {names[^1]}"
            : names[0];
    }

    /// <summary>"Black and Green won: only one team has pieces left.", or "The game is drawn: ...".</summary>
    public static string Story(IRulesApplying rules, GameOutcome outcome) =>
        outcome.IsDraw
            ? $"The game is drawn: {outcome.Reason}."
            : $"{Listed(rules, outcome.Winners)} won: {outcome.Reason}.";
}

/// <summary>What has been told so far, and whether the seat whose turn it is has rolled yet.</summary>
file sealed record Teller(IRulesApplying Rules, ImmutableList<EventDto> Told, bool HasRolled)
{
    public Teller Tell(GameEvent happened, int number) =>
        happened switch
        {
            RollOffRolled rolled => this.Add(
                number,
                EventDto.RolledOff,
                rolled.Seat,
                $"{this.Name(rolled.Seat)} rolled {Text(rolled.Value)} to see who goes first."
            ),
            RollOffTied tied => this.Add(
                number,
                EventDto.RolledOff,
                tied.Seats[0],
                $"{GameLog.Listed(this.Rules, tied.Seats)} rolled highest together, and roll again."
            ),
            RollOffWon won => this.Add(
                number,
                EventDto.RolledOff,
                won.Seat,
                $"{this.Name(won.Seat)} goes first, with the {this.Face(won.Value)} they rolled."
            ) with
            { HasRolled = true },
            DiceRolled rolled => this.Add(
                number,
                EventDto.Rolled,
                rolled.Seat,
                $"{this.Name(rolled.Seat)} rolled {string.Join(" and ", rolled.Values.Select(this.Face))}."
            ) with
            { HasRolled = true },
            PieceMoved moved => this.Moved(number, moved),
            PieceCaptured captured => this.Add(
                number,
                EventDto.Captured,
                captured.CapturedBy,
                $"{this.Name(captured.CapturedBy)} took {this.Name(captured.Piece.Owner)}'s "
                    + $"{this.Rules.NameOf(captured.Piece.Kind)} (+{Text(captured.Points)})."
            ),
            RajaCapturedOnThrone onThrone => this.Add(
                number,
                EventDto.Bonus,
                onThrone.CapturedBy,
                $"{this.Name(onThrone.CapturedBy)} took that Raja on its own throne (+{Text(onThrone.Points)} more)."
            ),
            ThroneEntered entered => this.Add(
                number,
                EventDto.Bonus,
                entered.Raja.Owner,
                $"{this.Name(entered.Raja.Owner)}'s Raja entered {this.Name(entered.Throne)}'s throne "
                    + $"(+{Text(entered.Points)})."
            ),
            TurnShared shared => this.Add(
                number,
                EventDto.Shared,
                shared.Occupier,
                $"{this.Name(shared.Occupier)} holds {this.Name(shared.Seat)}'s throne, so moves for "
                    + $"{this.Name(shared.Seat)} now."
            ),
            TurnPassed passed => this.Add(number, EventDto.Passed, passed.From, $"{this.Name(passed.To)} to play.")
                with
            { HasRolled = false },
            GameEnded ended => this.Add(
                number,
                EventDto.Ended,
                ended.Outcome.Winners.FirstOrDefault(),
                GameLog.Story(this.Rules, ended.Outcome)
            ),
            _ => throw new NotSupportedException($"There is no way yet to tell of {happened.GetType().Name}."),
        };

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

    private Teller Add(int number, string kind, SeatId seat, string text) =>
        this with { Told = this.Told.Add(new EventDto(number, kind, seat.Value, text, null, null)) };

    private string Name(SeatId seat) => this.Rules.NameOf(seat);

    private string Face(int value) => $"{Text(value)} ({this.Rules.NameOfDie(value)})";

    /// <summary>"Black moved their Horse from b1 to c3, without rolling.", or "Black moved Red's Pawn...".</summary>
    private Teller Moved(int number, PieceMoved moved)
    {
        string whose = moved.Piece.Owner == moved.MovedBy
            ? "their"
            : $"{this.Name(moved.Piece.Owner)}'s";
        string how = this.HasRolled
            ? string.Empty
            : ", without rolling";
        string from = this.Rules.Topology.NameOf(moved.From);
        string to = this.Rules.Topology.NameOf(moved.To);
        string text = $"{this.Name(moved.MovedBy)} moved {whose} {this.Rules.NameOf(moved.Piece.Kind)} "
            + $"from {from} to {to}{how}.";
        EventDto told = new(number, EventDto.Moved, moved.MovedBy.Value, text, moved.From.Value, moved.To.Value);
        return this with { Told = this.Told.Add(told) };
    }
}
