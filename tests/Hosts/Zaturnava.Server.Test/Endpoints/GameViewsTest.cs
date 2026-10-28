using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using Net.NowhereAtAll.Zaturnava.Server.Endpoints;

namespace Net.NowhereAtAll.Zaturnava.Server.Test.Endpoints;

public sealed class GameViewsTest
{
    private static readonly Chaturaji2027Rules Rules = new();
    private static readonly ImmutableList<BotProfile> Bots = [new("midas", "Midas", "Greedy.", false)];

    [Fact]
    public void ViewOf_WhenTheGameIsOver_ShowsNoDecidingSeatAndHowItEnded()
    {
        // Arrange
        GameOutcome outcome = new([new SeatId(0), new SeatId(2)], "only one team has pieces left");
        GameSnapshot over = Snapshot(Rules.CreateInitialState() with { DecidingSeat = null, Outcome = outcome });

        // Act
        GameViewDto view = GameViews.ViewOf(over, Bots);

        // Assert
        Assert.Equal(
            (null, "0,2", "Black and Green won: only one team has pieces left."),
            (view.DecidingSeat, string.Join(",", view.Outcome!.Winners), view.Outcome.Text)
        );
    }

    [Fact]
    public void ViewOf_WhenTheGameIsADraw_SaysSo()
    {
        // Arrange
        GameOutcome outcome = new([], "only Rajas are left, an honourable peace");
        GameSnapshot over = Snapshot(Rules.CreateInitialState() with { DecidingSeat = null, Outcome = outcome });

        // Act
        GameViewDto view = GameViews.ViewOf(over, Bots);

        // Assert
        Assert.Equal("The game is drawn: only Rajas are left, an honourable peace.", view.Outcome?.Text);
    }

    [Fact]
    public void ViewOf_WhenASeatActsForAnother_ShowsBoth()
    {
        // Arrange
        GameSnapshot shared = Snapshot(
            Rules.CreateInitialState() with { DecidingSeat = new SeatId(3), ActingFor = new SeatId(1) }
        );

        // Act
        GameViewDto view = GameViews.ViewOf(shared, Bots);

        // Assert
        Assert.Equal((3, 1), (view.DecidingSeat, view.ActingFor));
    }

    [Fact]
    public void ViewOf_WhenSeatsHoldPrisoners_ShowsEachWithItsHolder()
    {
        // Arrange
        Piece pawn = new(new PieceId(20), Chaturaji2027Pieces.Pawn, new SeatId(1));
        GameSnapshot snapshot = Snapshot(
            Rules.CreateInitialState() with { Prisoners = [new Prisoner(pawn, new SeatId(0))] }
        );

        // Act
        GameViewDto view = GameViews.ViewOf(snapshot, Bots);

        // Assert
        Assert.Equal(
            ("pawn Pawn 1", 0),
            (
                view.Seats[0].Prisoners.Select(held => $"{held.Kind} {held.Name} {held.Owner}").Single(),
                view.Seats[1].Prisoners.Count
            )
        );
    }

    [Fact]
    public void ViewOf_WhenARansomWaitsOnItsAnswer_ShowsItsPrisonersAndTheTimeLeft()
    {
        // Arrange
        Piece pawn = new(new PieceId(20), Chaturaji2027Pieces.Pawn, new SeatId(1));
        Piece horse = new(new PieceId(9), Chaturaji2027Pieces.Horse, new SeatId(0));
        GameState state = Rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = new SeatId(1),
            Prisoners = [new Prisoner(pawn, new SeatId(0)), new Prisoner(horse, new SeatId(1))],
            Offer = new RansomOffer(new SeatId(0), new SeatId(1), [pawn.Id], [horse.Id]),
        };
        GameSnapshot snapshot = Snapshot(state) with { TimeLeft = TimeSpan.FromSeconds(12.5) };

        // Act
        GameViewDto view = GameViews.ViewOf(snapshot, Bots);

        // Assert
        Assert.Equal(
            ("0>1: 20 Pawn 1 for 9 Horse 0", false, 12.5),
            (
                $"{view.Offer!.From}>{view.Offer.To}: {Listed(view.Offer.Given)} for {Listed(view.Offer.Asked)}",
                view.MayOffer,
                view.SecondsLeft
            )
        );
    }

    [Fact]
    public void ViewOf_WhenTheDecidingSeatHoldsPrisonersOnItsTurn_SaysItMayOffer()
    {
        // Arrange
        Piece pawn = new(new PieceId(20), Chaturaji2027Pieces.Pawn, new SeatId(1));
        GameState state = Rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            Prisoners = [new Prisoner(pawn, new SeatId(0))],
        };

        // Act
        GameViewDto view = GameViews.ViewOf(Snapshot(state), Bots);

        // Assert
        Assert.Equal((true, null, null), (view.MayOffer, view.Offer, view.SecondsLeft));
    }

    [Fact]
    public void ViewOf_WhenAPawnMayBePromoted_LabelsEachPromotion()
    {
        // Arrange - Black's Pawn on c8, the far edge; Green's Elephant is held by Red
        Piece pawn = new(new PieceId(4), Chaturaji2027Pieces.Pawn, new SeatId(0));
        Piece elephant = new(new PieceId(18), Chaturaji2027Pieces.Elephant, new SeatId(2));
        GameState state = Rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            Position = Rules.CreateInitialState().Position.Place(Rules.Topology.Cells[58], pawn),
            Prisoners = [new Prisoner(elephant, new SeatId(3))],
        };

        // Act
        GameViewDto view = GameViews.ViewOf(Snapshot(state), Bots);

        // Assert
        Assert.Equal(
            ["Promote the Pawn on c8 to Green's Elephant, held by Red"],
            view.LegalActions.Where(action => action.Kind == "promote").Select(action => action.Label)
        );
    }

    [Fact]
    public void ViewOf_WhenAPawnMaySwapBackToBack_LabelsTheSwapButNoOtherMove()
    {
        // Arrange - Black's Pawn on d4 faces Green's on d5; Black's Pawn on a2 may take Yellow's on b3
        GameState start = Rules.CreateInitialState();
        GameState state = start with
        {
            RollOff = [],
            AwaitedDice = null,
            Position = start.Position
                .Place(Rules.Topology.Cells[27], new Piece(new PieceId(4), Chaturaji2027Pieces.Pawn, new SeatId(0)))
                .Place(Rules.Topology.Cells[35], new Piece(new PieceId(20), Chaturaji2027Pieces.Pawn, new SeatId(2)))
                .Place(Rules.Topology.Cells[17], new Piece(new PieceId(12), Chaturaji2027Pieces.Pawn, new SeatId(1))),
        };

        // Act
        GameViewDto view = GameViews.ViewOf(Snapshot(state), Bots);

        // Assert
        Assert.Equal(
            ["Swap places with Green's Pawn"],
            view.LegalActions.Where(action => action is { Kind: "move", Label: not null })
                .Select(action => action.Label)
        );
        Assert.Contains(view.LegalActions, action => action.Kind == "move" && action.Arguments["to"] == "17");
    }

    [Fact]
    public void ViewOf_WhenOnlyItsRajaIsLeft_LabelsRetiring()
    {
        // Arrange - Black has only its Raja, on d1
        GameState start = Rules.CreateInitialState();
        GameState state = start with
        {
            RollOff = [],
            AwaitedDice = null,
            Position = new Position(
                start.Position.Occupants.RemoveRange(
                    start.Position.Occupants
                        .Where(occupant => occupant.Value.Owner == new SeatId(0))
                        .Where(occupant => occupant.Value.Kind != Chaturaji2027Pieces.Raja)
                        .Select(occupant => occupant.Key)
                )
            ),
        };

        // Act
        GameViewDto view = GameViews.ViewOf(Snapshot(state), Bots);

        // Assert
        Assert.Equal(
            ["Retire from the game: taking your Raja will score no one"],
            view.LegalActions.Where(action => action.Kind == "retire").Select(action => action.Label)
        );
    }

    [Fact]
    public void ViewOf_WhenADieLetsNothingMove_SaysItIsNotUsable()
    {
        // Arrange - at the start, Black's Pawns may move, but its Elephant is hemmed in
        GameState state = Rules.CreateInitialState() with { RollOff = [], AwaitedDice = null, Dice = [5, 3] };

        // Act
        GameViewDto view = GameViews.ViewOf(Snapshot(state), Bots);

        // Assert
        Assert.Equal([true, false], view.Dice.Select(die => die.IsUsable));
    }

    [Fact]
    public void ViewOf_WhenAPieceMayEscape_LabelsEachEscape()
    {
        // Arrange - Black rolled two 5s; Yellow holds Black's Raja 3, whose square, d1, is free
        Piece raja = new(new PieceId(3), Chaturaji2027Pieces.Raja, new SeatId(0));
        GameState start = Rules.CreateInitialState();
        GameState state = start with
        {
            RollOff = [],
            AwaitedDice = null,
            Dice = [5, 5],
            Position = new Position(start.Position.Occupants.Remove(Rules.Thrones[new SeatId(0)])),
            Prisoners = [new Prisoner(raja, new SeatId(1))],
        };

        // Act
        GameViewDto view = GameViews.ViewOf(Snapshot(state), Bots);

        // Assert
        Assert.Equal(
            ["Escape Black's Raja, held by Yellow, to d1"],
            view.LegalActions.Where(action => action.Kind == "escape").Select(action => action.Label)
        );
    }

    [Fact]
    public void ViewOf_WhenAnAllyMayTakeTheTurn_LabelsHandingItOver()
    {
        // Arrange - Black's Raja is gone, so Green, which has its own, may take Black's turn
        GameState start = Rules.CreateInitialState();
        GameState state = start with
        {
            RollOff = [],
            AwaitedDice = null,
            Position = new Position(start.Position.Occupants.Remove(Rules.Thrones[new SeatId(0)])),
        };

        // Act
        GameViewDto view = GameViews.ViewOf(Snapshot(state), Bots);

        // Assert
        Assert.Equal(
            ["Let Green take over"],
            view.LegalActions.Where(action => action.Kind == "hand-over").Select(action => action.Label)
        );
    }

    [Fact]
    public void ViewOf_WhenEndingTheGameIsProposed_ShowsTheProposalAndLabelsTheAnswers()
    {
        // Arrange - Green proposes, giving up its turns, while Black decides
        GameState playing = Rules.CreateInitialState() with { RollOff = [], AwaitedDice = null };
        GameState state = Rules.Apply(playing, new ProposeEndAction(new SeatId(2), Resigns: true)).State;

        // Act
        GameViewDto before = GameViews.ViewOf(Snapshot(playing), Bots);
        GameViewDto view = GameViews.ViewOf(Snapshot(state), Bots);

        // Assert
        Assert.Equal((null, "True,True,True,True"), (before.Proposal, Flags(before, seat => seat.MayProposeEnd)));
        Assert.Equal(
            (new EndProposalDto(2, true), "False,False,True,False", "False,False,False,False"),
            (view.Proposal, Flags(view, seat => seat.HasGivenUp), Flags(view, seat => seat.MayProposeEnd))
        );
        Assert.Equal(["Agree to end the game", "Keep playing"], view.LegalActions.Select(action => action.Label));
    }

    [Fact]
    public void ViewOf_WhenOthersVoteOnWaitingForASeatAway_ShowsTheVoteAndTheTurnsItMissed()
    {
        // Arrange - Black missed a turn; Yellow and Red may vote, and Red would stop waiting
        GameState state = Rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            Absence = Absence.None with { Missed = Absence.None.Missed.Add(new SeatId(0), 1) },
        };
        AwayVoting away = new(new SeatId(0), [new SeatId(1), new SeatId(3)], [new SeatId(3)], TimeSpan.Zero);

        // Act
        GameViewDto view = GameViews.ViewOf(Snapshot(state) with { Away = away }, Bots);

        // Assert
        Assert.Equal((0, true, 1), (view.Away!.Seat, view.Away.IsOpen, view.Seats[0].TurnsMissed));
        Assert.Equal([1, 3], view.Away.Voters);
        Assert.Equal([3], view.Away.Stopping);
    }

    [Fact]
    public void ViewOf_WhenASeatHasGivenUpItsTurns_SaysItMayRejoinButNotProposeAgain()
    {
        // Arrange - Green gave up its turns; no proposal waits
        GameState state = Rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            Resigned = [new SeatId(2)],
        };

        // Act
        GameViewDto view = GameViews.ViewOf(Snapshot(state), Bots);

        // Assert
        Assert.Equal(
            ("False,False,True,False", "True,True,False,True"),
            (Flags(view, seat => seat.MayRejoin), Flags(view, seat => seat.MayProposeEnd))
        );
    }

    [Fact]
    public void ViewOf_WhenAPromotionIsDue_SaysWhereAndLabelsPassingAsLater()
    {
        // Arrange - Black's Pawn has just come to c8
        Piece pawn = new(new PieceId(4), Chaturaji2027Pieces.Pawn, new SeatId(0));
        Piece elephant = new(new PieceId(18), Chaturaji2027Pieces.Elephant, new SeatId(2));
        GameState state = Rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            Position = Rules.CreateInitialState().Position.Place(Rules.Topology.Cells[58], pawn),
            Prisoners = [new Prisoner(elephant, new SeatId(3))],
            PromotionDue = Rules.Topology.Cells[58],
        };

        // Act
        GameViewDto view = GameViews.ViewOf(Snapshot(state), Bots);

        // Assert
        Assert.Equal(
            (58, "pass: Promote later"),
            (view.PromotionDue, $"{view.LegalActions[0].Kind}: {view.LegalActions[0].Label}")
        );
    }

    [Theory]
    [InlineData("enemy's Raja", 1)]
    [InlineData("ally's Raja", null)]
    [InlineData("enemy's Pawn", null)]
    [InlineData("empty", null)]
    [InlineData("acting for another", null)]
    [InlineData("game over", null)]
    public void ViewOf_WhenALeaderSitsOnTheDecidingSeatsThrone_SaysWhoseIfAnEnemys(string why, int? expected)
    {
        // Arrange - Black to decide, with Yellow's (an enemy's) or Green's (an ally's) piece on its throne
        GameState start = Rules.CreateInitialState();
        CellId throne = Rules.Thrones[new SeatId(0)];
        Position position = why switch
        {
            "ally's Raja" => start.Position.Place(throne, Holder(Chaturaji2027Pieces.Raja, 2)),
            "enemy's Pawn" => start.Position.Place(throne, Holder(Chaturaji2027Pieces.Pawn, 1)),
            "empty" => new Position(start.Position.Occupants.Remove(throne)),
            _ => start.Position.Place(throne, Holder(Chaturaji2027Pieces.Raja, 1)),
        };
        GameState state = start with
        {
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = why == "game over" ? null : new SeatId(0),
            ActingFor = why == "acting for another" ? new SeatId(2) : null,
            Position = position,
        };

        // Act
        GameViewDto view = GameViews.ViewOf(Snapshot(state), Bots);

        // Assert
        Assert.Equal(expected, view.ThroneHeldBy);
    }

    [Fact]
    public void ViewOf_WhenAPieceGoingHomeWaitsOnWhereItGoes_SaysWhichAndOffersItsPlaces()
    {
        // Arrange - Black's Pawn 4 is going home from a ransom, Yellow to decide once it is placed
        Piece pawn = new(new PieceId(4), Chaturaji2027Pieces.Pawn, new SeatId(0));
        Prisoner going = new(pawn, new SeatId(1));
        GameState state = Rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            Prisoners = [going],
            Returning = new ReturnsDue([going], new SeatId(1)),
        };

        // Act
        GameViewDto view = GameViews.ViewOf(Snapshot(state), Bots);

        // Assert
        Assert.Equal(
            (new PrisonerDto(4, "pawn", "Pawn", 0), "place"),
            (view.Returning, view.LegalActions.Select(action => action.Kind).Distinct().Single())
        );
    }

    [Fact]
    public void ViewOf_WhenATurnIsOverButItsSeatMayStillOffer_LabelsPassingAsDone()
    {
        // Arrange
        Piece pawn = new(new PieceId(20), Chaturaji2027Pieces.Pawn, new SeatId(1));
        GameState state = Rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            IsRansomWindow = true,
            Prisoners = [new Prisoner(pawn, new SeatId(0))],
        };

        // Act
        GameViewDto view = GameViews.ViewOf(Snapshot(state), Bots);

        // Assert
        Assert.Equal(
            ("pass: Done offering", true),
            ($"{view.LegalActions.Single().Kind}: {view.LegalActions.Single().Label}", view.IsTurnOver)
        );
    }

    [Fact]
    public void ViewOf_WhenSeatsArePlayedByBots_NamesThemAsTheyAreOffered()
    {
        // Arrange
        GameRecord record = new(
            new GameId("bots"),
            Rules.Id,
            [new BotSeat("midas"), new BotSeat("retired"), new OpenSeat(), new OpenSeat()],
            [],
            0
        );
        GameSnapshot snapshot = new(record, Rules, Rules.CreateInitialState(), [], []);

        // Act
        GameViewDto view = GameViews.ViewOf(snapshot, Bots);

        // Assert
        Assert.Equal("Midas|retired||", string.Join("|", view.Seats.Select(seat => seat.Bot)));
    }

    [Fact]
    public void ViewOf_WhenASeatIsPlayedByABotPaidForEachUse_SaysSo()
    {
        // Arrange
        ImmutableList<BotProfile> bots = [.. Bots, new("claude", "Claude", "Asks Claude.", UsesAi: true, IsPaid: true)];
        GameRecord record = new(
            new GameId("paid"),
            Rules.Id,
            [new BotSeat("midas"), new BotSeat("claude"), new OpenSeat(), new OpenSeat()],
            [],
            0
        );
        GameSnapshot snapshot = new(record, Rules, Rules.CreateInitialState(), [], []);

        // Act
        GameViewDto view = GameViews.ViewOf(snapshot, bots);

        // Assert
        Assert.Equal([false, true, false, false], view.Seats.Select(seat => seat.IsPaidBot));
    }

    [Fact]
    public void ViewOf_WhenDrawingTheBoard_NamesEachCell()
    {
        // Arrange
        GameSnapshot snapshot = Snapshot(Rules.CreateInitialState());

        // Act
        GameViewDto view = GameViews.ViewOf(snapshot, Bots);

        // Assert
        Assert.Equal(("a1", "h8"), (view.Cells[0].Name, view.Cells[63].Name));
    }

    private static Piece Holder(PieceKindId kind, int owner) => new(new PieceId(99), kind, new SeatId(owner));

    private static string Flags(GameViewDto view, Func<SeatDto, bool> flag) =>
        string.Join(',', view.Seats.Select(flag));

    private static string Listed(ImmutableList<PrisonerDto> prisoners) =>
        string.Join(", ", prisoners.Select(prisoner => $"{prisoner.Id} {prisoner.Name} {prisoner.Owner}"));

    private static GameSnapshot Snapshot(GameState state)
    {
        GameRecord record = new(
            new GameId("g"),
            Rules.Id,
            [new OpenSeat(), new OpenSeat(), new OpenSeat(), new OpenSeat()],
            [],
            0
        );
        return new GameSnapshot(record, Rules, state, [], Rules.LegalActions(state));
    }
}
