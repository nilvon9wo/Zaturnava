using System.Collections.Immutable;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Server.Endpoints;

/// <summary>What has happened in a game, told for people in the ruleset's own names.</summary>
internal static class GameLog
{
    /// <summary>How many of the latest events a view carries, besides every highlight before them.</summary>
    public const int Length = 40;

    /// <summary>
    /// The latest events, and every highlight before them, each told knowing those before it: a move says if
    /// it was rolled for.
    /// </summary>
    public static ImmutableList<EventDto> LatestOf(GameSnapshot snapshot)
    {
        Teller start = new(snapshot.Rules, [], HasRolled: false);
        ImmutableList<EventDto> told = snapshot.Events
            .Select((happened, number) => (happened, number))
            .Aggregate(start, (teller, next) => teller.Tell(next.happened, next.number))
            .Told;
        return told.Count > Length
            ?
            [
                .. told.Take(told.Count - Length).Where(happened => EventDto.Highlights.Contains(happened.Kind)),
                .. told.TakeLast(Length),
            ]
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

/// <summary>
/// What has been told so far, whether the seat whose turn it is has rolled yet, and whom an occupier
/// moves for, while it shares that turn.
/// </summary>
file sealed record Teller(IRulesApplying Rules, ImmutableList<EventDto> Told, bool HasRolled)
{
    private SeatId? SharedFor { get; init; }

    /// <summary>The piece moved last, which takes whatever the move takes.</summary>
    private Piece? LastMoved { get; init; }

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
                $"{this.Name(rolled.Seat)} rolled {this.Faces(rolled.Values)}."
            ) with
            { HasRolled = true },
            DiceForfeited forfeited => this.Add(
                number,
                EventDto.Forfeited,
                forfeited.Seat,
                $"{this.Name(forfeited.Seat)} could move nothing{this.OfWhom()} with "
                    + $"{this.Faces(forfeited.Values)}, so lost {(forfeited.Values.Count == 1 ? "it" : "them")}."
            ),
            PieceMoved moved => this.Moved(number, moved),
            PawnsSwapped swapped => this.Swapped(number, swapped),
            PieceCaptured captured => this.Captured(number, captured),
            CommandBegun begun => this.Add(number, EventDto.Command, begun.Commander, this.Begun(begun)),
            CommandEnded ended => this.Ended(
                number,
                EventDto.Command,
                ended.Commander,
                $"{this.Name(ended.Commander)} no longer controls {this.Name(ended.Army)}'s army."
            ),
            EmpireBegun empire => this.Add(
                number,
                EventDto.Empire,
                empire.Rulers[0],
                $"{GameLog.Listed(this.Rules, empire.Rulers)} now rule an empire: no enemy Raja is left on the board, "
                    + "and one of them commands an ally's army. It scores only as the last enemy Raja is taken, "
                    + "so no one scores for it now."
            ),
            EmpireEnded fallen => this.Ended(
                number,
                EventDto.Empire,
                fallen.Rulers[0],
                $"{GameLog.Listed(this.Rules, fallen.Rulers)}'s empire is over."
            ),
            PiecePromoted promoted => this.Promoted(number, promoted),
            RansomWindowOpened window => this.Add(
                number,
                EventDto.Ransom,
                window.Seat,
                $"{this.Name(window.Seat)}'s turn is over, but they may still offer prisoners back."
            ),
            RansomOffered offered => this.Add(
                number,
                EventDto.Ransom,
                offered.Offer.From,
                $"{this.Name(offered.Offer.From)} offered {this.Name(offered.Offer.To)} "
                    + $"{Prisoners(offered.Offer.Given.Count)} back, {Asking(offered.Offer.Asked.Count)}."
            ),
            RansomAccepted accepted => this.Add(
                number,
                EventDto.Ransom,
                accepted.Offer.To,
                $"{this.Name(accepted.Offer.To)} accepted {this.Name(accepted.Offer.From)}'s offer."
            ),
            RansomRewarded rewarded => this.Add(
                number,
                EventDto.Bonus,
                rewarded.Seat,
                $"{this.Name(rewarded.Seat)} scored for accepting: a point for each prisoner going home "
                    + $"(+{rewarded.Points})."
            ),
            RansomDeclined declined => this.Add(
                number,
                EventDto.Declined,
                declined.Offer.To,
                $"{this.Name(declined.Offer.To)} declined {this.Name(declined.Offer.From)}'s offer."
            ),
            PieceReturned returned => this.Returned(number, returned),
            PieceEscaped escaped => this.Escaped(number, escaped),
            RajaCapturedOnThrone onThrone => this.Add(
                number,
                EventDto.Bonus,
                onThrone.CapturedBy,
                $"{this.Name(onThrone.CapturedBy)} took that Raja on its own throne (+{Text(onThrone.Points)} more)."
            ),
            EmpireCreated empire => this.Add(
                number,
                EventDto.Empire,
                empire.Creator,
                $"{this.Name(empire.Creator)} created the empire, with no enemy Raja left on the board "
                    + $"(+{Text(empire.Points)})."
            ),
            EveryPieceCommanded commanded => this.Add(
                number,
                EventDto.Bonus,
                commanded.Commander,
                $"{this.Name(commanded.Commander)} commands every piece on the board (+{Text(commanded.Points)})."
            ),
            ThroneEntered entered => this.Add(
                number,
                EventDto.Bonus,
                entered.Raja.Owner,
                $"{this.Name(entered.Raja.Owner)}'s Raja entered {this.Name(entered.Throne)}'s throne "
                    + $"(+{Text(entered.Points)})."
            ),
            EndProposed proposed => this.Add(
                number,
                EventDto.Ending,
                proposed.Proposer,
                $"{this.Name(proposed.Proposer)} proposed ending the game"
                    + (proposed.Resigns, proposed.IsAhead) switch
                    {
                        (true, true) => ", and rests on their team's lead, taking no more turns.",
                        (true, false) => ", and gave up their turns.",
                        _ => ", and waits for the others to agree.",
                    }
            ),
            EndAgreed agreed => this.Add(
                number,
                EventDto.Ending,
                agreed.Seat,
                $"{this.Name(agreed.Seat)} agreed to end the game."
            ),
            EndRefused refused => this.Add(
                number,
                EventDto.Ending,
                refused.Seat,
                $"{this.Name(refused.Seat)} would rather play on, so the game goes on."
            ),
            Rejoined rejoined => this.Add(
                number,
                EventDto.Ending,
                rejoined.Seat,
                $"{this.Name(rejoined.Seat)} is back in the game."
            ),
            AwayVoted voted => this.Add(
                number,
                EventDto.Away,
                voted.Voter,
                voted.Stops
                    ? $"{this.Name(voted.Voter)} would stop waiting for {this.Name(voted.Absent)}."
                    : $"{this.Name(voted.Voter)} will keep waiting for {this.Name(voted.Absent)}."
            ),
            TurnPassedAway missed => this.Add(number, EventDto.Away, missed.Seat, this.Missed(missed)),
            TurnGivenUp givenUp => this.Add(
                number,
                EventDto.Passed,
                givenUp.Seat,
                $"{this.Name(givenUp.Seat)} takes no more turns, so passes."
            ),
            RajaRetired retired => this.Add(
                number,
                EventDto.Passed,
                retired.Seat,
                $"{this.Name(retired.Seat)} retired from the game: their Raja stays, and taking it scores no one."
            ),
            TurnHandedBack back => this.Add(
                number,
                EventDto.Shared,
                back.Seat,
                $"{this.Name(back.Occupier)} left the rest of {this.Name(back.Seat)}'s turn to {this.Name(back.Seat)}."
            ),
            TurnHandedOver handed => this.Add(
                number,
                EventDto.Shared,
                handed.Regent,
                $"{this.Name(handed.Seat)} let {this.Name(handed.Regent)} take the rest of their turn."
            ) with
            { SharedFor = handed.Seat },
            TurnShared shared => this.Add(
                number,
                EventDto.Shared,
                shared.Occupier,
                $"{this.Name(shared.Occupier)} holds {this.Name(shared.Seat)}'s throne, so moves for "
                    + $"{this.Name(shared.Seat)} now."
            ) with
            { SharedFor = shared.Seat },
            TurnPassed passed => this.Add(number, EventDto.Passed, passed.To, $"{this.Name(passed.To)} to play.")
                with
            { HasRolled = false, SharedFor = null },
            GameEnded ended => this.Add(
                number,
                EventDto.Ended,
                ended.Outcome.Winners.FirstOrDefault(),
                GameLog.Story(this.Rules, ended.Outcome)
            ),
            _ => throw new NotSupportedException($"There is no way yet to tell of {happened.GetType().Name}."),
        };

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>"+3"; "no points", for taking a piece of one's own team.</summary>
    private static string Scored(int points) =>
        points > 0
            ? $"+{Text(points)}"
            : "no points";

    private static string Prisoners(int count) =>
        count == 1
            ? "a prisoner"
            : $"{Text(count)} prisoners";

    private static string Asking(int count) =>
        count == 0
            ? "as a gift"
            : $"for {Prisoners(count)} in return";

    /// <summary>"their", for the seat's own piece; "Red's", for another's.</summary>
    private string Whose(SeatId owner, SeatId seat) =>
        owner == seat
            ? "their"
            : $"{this.Name(owner)}'s";

    /// <summary>"Black promoted Red's Pawn on h4 to Green's Elephant."</summary>
    private Teller Promoted(int number, PiecePromoted promoted)
    {
        string pawn = $"{this.Whose(promoted.Pawn.Owner, promoted.PromotedBy)} {this.Rules.NameOf(promoted.Pawn.Kind)}";
        string into = $"{this.Whose(promoted.Into.Owner, promoted.PromotedBy)} {this.Rules.NameOf(promoted.Into.Kind)}";
        string cell = this.Rules.Topology.NameOf(promoted.Cell);
        string text = $"{this.Name(promoted.PromotedBy)} promoted {pawn} on {cell} to {into}.";
        EventDto told = new(number, EventDto.Promoted, promoted.PromotedBy.Value, text, null, promoted.Cell.Value);
        return this with { Told = this.Told.Add(told) };
    }

    /// <summary>"Yellow's Pawn went home, to a7."</summary>
    private Teller Returned(int number, PieceReturned returned)
    {
        Piece piece = returned.Piece;
        string cell = this.Rules.Topology.NameOf(returned.Cell);
        string text = $"{this.Name(piece.Owner)}'s {this.Rules.NameOf(piece.Kind)} went home, to {cell}.";
        EventDto told = new(
            number,
            EventDto.Ransom,
            piece.Owner.Value,
            text,
            null,
            returned.Cell.Value,
            piece.Owner.Value,
            this.Rules.IsLeader(piece.Kind)
        );
        return this with { Told = this.Told.Add(told) };
    }

    /// <summary>"Black's Raja escaped, to d1.", or "Black let Green's Raja escape, to h4."</summary>
    private Teller Escaped(int number, PieceEscaped escaped)
    {
        Piece piece = escaped.Piece;
        string kind = this.Rules.NameOf(piece.Kind);
        string cell = this.Rules.Topology.NameOf(escaped.Cell);
        string taking = escaped.Took is { } took
            ? $", taking {this.Name(took.Owner)}'s {this.Rules.NameOf(took.Kind)}!"
            : ".";
        string text = piece.Owner == escaped.EscapedBy
            ? $"{this.Name(piece.Owner)}'s {kind} escaped, to {cell}{taking}"
            : $"{this.Name(escaped.EscapedBy)} let {this.Name(piece.Owner)}'s {kind} escape, to {cell}{taking}";
        EventDto told = new(
            number,
            EventDto.Escaped,
            escaped.EscapedBy.Value,
            text,
            null,
            escaped.Cell.Value,
            piece.Owner.Value,
            this.Rules.IsLeader(piece.Kind)
        );
        return this with { Told = this.Told.Add(told), LastMoved = piece };
    }

    private Teller Captured(int number, PieceCaptured captured)
    {
        Piece piece = captured.Piece;
        string text = $"{this.Name(captured.CapturedBy)} took {this.Whose(piece.Owner, captured.CapturedBy)} "
            + $"{this.Rules.NameOf(piece.Kind)}{this.With(captured.CapturedBy)} ({Scored(captured.Points)}).";
        EventDto told = new(
            number,
            EventDto.Captured,
            captured.CapturedBy.Value,
            text,
            null,
            null,
            piece.Owner.Value,
            this.Rules.IsLeader(piece.Kind)
        );
        return this with { Told = this.Told.Add(told) };
    }

    /// <summary>
    /// "Green now has partial control of Black's army: ..." - complete, while it plays Black's turn as
    /// regent; partial, while it moves Black's army on its own turns, saying why and what more it may do.
    /// </summary>
    private string Begun(CommandBegun begun)
    {
        string commander = this.Name(begun.Commander);
        string army = this.Name(begun.Army);
        string partial = $"{commander} now has partial control of {army}'s army:";
        return begun.Basis switch
        {
            CommandBasis.Regency =>
                $"{commander} now has complete control of {army}'s army, for the rest of {army}'s turn.",
            CommandBasis.AllyThrone => $"{partial} with their Raja on {army}'s throne, they move it on their own "
                + $"turns, and may play {army}'s turns, if {army} lets them.",
            CommandBasis.EnemyThrone => $"{partial} with their Raja on {army}'s throne, they move it on their own "
                + $"turns, and make the second move of {army}'s.",
            _ => $"{partial} as {army} has no Raja, they move it on their own turns, and may play {army}'s turns, "
                + $"if {army} lets them.",
        };
    }

    /// <summary>
    /// "Black is away, so passes: 1 of 3 turns missed in a row.", and, at the last, that Black has given up
    /// their turns.
    /// </summary>
    private string Missed(TurnPassedAway missed)
    {
        string name = this.Name(missed.Seat);
        string passed = string.Create(
            CultureInfo.InvariantCulture,
            $"{name} is away, so passes: {missed.Missed} of {missed.Limit} turns missed in a row."
        );
        return missed.Missed >= missed.Limit
            ? $"{passed} {name} has given up their turns, until they rejoin."
            : passed;
    }

    private Teller Add(int number, string kind, SeatId seat, string text) =>
        this with { Told = this.Told.Add(new EventDto(number, kind, seat.Value, text, null, null)) };

    /// <summary>A command or an empire over.</summary>
    private Teller Ended(int number, string kind, SeatId seat, string text) =>
        this with { Told = this.Told.Add(new EventDto(number, kind, seat.Value, text, null, null, IsOver: true)) };

    private string Name(SeatId seat) => this.Rules.NameOf(seat);

    private string Face(int value) => $"{Text(value)} ({this.Rules.NameOfDie(value)})";

    /// <summary>
    /// ", with Red's Elephant, and holds it prisoner", when the captor took with another army's piece;
    /// nothing otherwise.
    /// </summary>
    private string With(SeatId captor) =>
        this.LastMoved is { } taker && taker.Owner != captor
            ? $", with {this.Name(taker.Owner)}'s {this.Rules.NameOf(taker.Kind)}, and holds it prisoner"
            : string.Empty;

    /// <summary>" of Red's", while moving for Red; nothing otherwise.</summary>
    private string OfWhom() => this.SharedFor is { } seat ? $" of {this.Name(seat)}'s" : string.Empty;

    /// <summary>"2 (Horse) and 5 (Pawn or Raja)".</summary>
    private string Faces(ImmutableList<int> values) => string.Join(" and ", values.Select(this.Face));

    /// <summary>"Black moved their Horse from b1 to c3, without rolling.", or "Black moved Red's Pawn...".</summary>
    private Teller Moved(int number, PieceMoved moved)
    {
        string whose = this.Whose(moved.Piece.Owner, moved.MovedBy);
        string how = this.HasRolled
            ? string.Empty
            : ", without rolling";
        string from = this.Rules.Topology.NameOf(moved.From);
        string to = this.Rules.Topology.NameOf(moved.To);
        string text = $"{this.Name(moved.MovedBy)} moved {whose} {this.Rules.NameOf(moved.Piece.Kind)} "
            + $"from {from} to {to}{how}.";
        EventDto told = new(number, EventDto.Moved, moved.MovedBy.Value, text, moved.From.Value, moved.To.Value);
        return this with { Told = this.Told.Add(told), LastMoved = moved.Piece };
    }

    /// <summary>"Black swapped their Pawn on c4 with Green's Pawn on c5, back to back, without rolling.".</summary>
    private Teller Swapped(int number, PawnsSwapped swapped)
    {
        string whose = this.Whose(swapped.Pawn.Owner, swapped.MovedBy);
        string how = this.HasRolled
            ? string.Empty
            : ", without rolling";
        string from = this.Rules.Topology.NameOf(swapped.From);
        string to = this.Rules.Topology.NameOf(swapped.To);
        string pawn = this.Rules.NameOf(swapped.Pawn.Kind);
        string other = this.Rules.NameOf(swapped.Other.Kind);
        string text = $"{this.Name(swapped.MovedBy)} swapped {whose} {pawn} on {from} with "
            + $"{this.Name(swapped.Other.Owner)}'s {other} on {to}, back to back{how}.";
        EventDto told = new(
            number,
            EventDto.Moved,
            swapped.MovedBy.Value,
            text,
            swapped.From.Value,
            swapped.To.Value
        );
        return this with { Told = this.Told.Add(told), LastMoved = swapped.Pawn };
    }
}
