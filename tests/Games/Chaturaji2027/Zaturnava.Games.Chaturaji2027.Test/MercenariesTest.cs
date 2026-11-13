using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

/// <summary>
/// Mercenaries, given here to Chaturaji 2027's Horses and Pawns (as the Shogi pieces have them in Grand Legions), and
/// to its Rajas, which, as leaders, are none: a Horse (3) costs 4 to hire, then 5.
/// </summary>
public sealed class MercenariesTest
{
    /// <summary>The edition of the rules a game is dealt from with mercenaries.</summary>
    private const int MercenaryEdition = 2;

    private const int HorseDie = 2;
    private const int BoatDie = 1;

    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SeatId Green = new(2);
    private static readonly SquareGridTopology Grid = new(Chaturaji2027Setup.Side, Chaturaji2027Setup.Side);
    private static readonly CellId BlackThrone = Grid.CellAt(3, 0);

    private static readonly Piece BlackRaja = new(new PieceId(3), Chaturaji2027Pieces.Raja, Black);
    private static readonly Piece BlackPawn = new(new PieceId(4), Chaturaji2027Pieces.Pawn, Black);
    private static readonly Piece YellowRaja = new(new PieceId(11), Chaturaji2027Pieces.Raja, Yellow);
    private static readonly Piece YellowHorse = new(new PieceId(9), Chaturaji2027Pieces.Horse, Yellow);
    private static readonly Piece GreenElephant = new(new PieceId(20), Chaturaji2027Pieces.Elephant, Green);

    private readonly Chaturaji2027Rules _rules = new(
        DiceProfile.WeightedSixSided,
        Chaturaji2027Rules.DefaultIdleRounds,
        PawnPromotion.OtherArmies,
        variant: Chaturaji2027Variant.Standard with
        {
            Mercenaries = [Chaturaji2027Pieces.Horse, Chaturaji2027Pieces.Pawn, Chaturaji2027Pieces.Raja],
        }
    );

    [Theory]
    [InlineData("as its move", true)]
    [InlineData("on a die that moves it", true)]
    [InlineData("on a die that moves it not", false)]
    [InlineData("in a game dealt before mercenaries", false)]
    [InlineData("short of points", false)]
    [InlineData("held by another", false)]
    [InlineData("a leader", false)]
    [InlineData("a piece that started as no mercenary", false)]
    [InlineData("hired before, for 4", true)]
    [InlineData("hired before, for 4, with 4 points", false)]
    [InlineData("in a game that started as the rules set it", true)]
    public void LegalActions_WhenAMercenaryIsHeldSo_OffersHiringItOnlyToItsHolderWithThePoints(string how, bool mayHire)
    {
        // Arrange
        Piece prisoner = how switch
        {
            "a leader" => YellowRaja,
            "a piece that started as no mercenary" => GreenElephant,
            _ => YellowHorse,
        };
        GameState start = BlackToMove(how switch
        {
            "on a die that moves it" => [HorseDie],
            "on a die that moves it not" => [BoatDie],
            _ => [],
        });
        GameState state = start with
        {
            Edition = how == "in a game dealt before mercenaries" ? 1 : MercenaryEdition,
            Scores = start.Scores.SetItem(Black, how switch
            {
                "short of points" => 3,
                "hired before, for 4, with 4 points" => 4,
                _ => 10,
            }),
            Start = how == "in a game that started as the rules set it" ? null : start.Start,
            Prisoners = [new Prisoner(prisoner, how == "held by another" ? Green : Black)],
            MercenaryCosts = how.StartsWith("hired before", StringComparison.Ordinal)
                ? ImmutableDictionary<PieceId, int>.Empty.Add(YellowHorse.Id, 4)
                : [],
        };
        ImmutableList<HireAction> expected = mayHire ? [new HireAction(Black, prisoner.Id)] : [];

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(expected, actions.OfType<HireAction>());
    }

    [Fact]
    public void Apply_WhenAMercenaryIsHiredAsTheTurnsMove_MakesItTheHirersOffTheBoardForItsCostAndEndsTheTurn()
    {
        // Arrange
        GameState state = Holding(BlackToMove([]));

        // Act
        Transition transition = this._rules.Apply(state, new HireAction(Black, YellowHorse.Id));

        // Assert
        Assert.Equal(
            (new PieceHired(YellowHorse, Black, 4), YellowHorse with { Owner = Black }, 0, 6, 4, true),
            (
                transition.Events[0],
                Assert.Single(transition.State.Hired),
                transition.State.Prisoners.Count,
                transition.State.Scores[Black],
                transition.State.MercenaryCosts[YellowHorse.Id],
                transition.State.DecidingSeat != Black
            )
        );
    }

    [Fact]
    public void Apply_WhenAMercenaryIsHiredOnOneOfTwoDiceThatMoveIt_LetsTheOtherPlaceIt()
    {
        // Arrange
        GameState state = Holding(BlackToMove([HorseDie, HorseDie]));

        // Act
        GameState hired = this._rules.Apply(state, new HireAction(Black, YellowHorse.Id)).State;

        // Assert
        Assert.Equal(
            (Black, "2", true),
            (
                hired.DecidingSeat,
                string.Join(',', hired.Dice),
                this._rules.LegalActions(hired).OfType<DeployAction>().Any()
            )
        );
    }

    [Theory]
    [InlineData(false, "c1,e1,c2,d2,e2")]
    [InlineData(true, "c2,e2")]
    public void LegalActions_WhenAMercenaryWasHired_OffersPlacingItOnTheFreeSquaresOfThePalaceOrAboutTheThrone(
        bool hasPalace,
        string squares
    )
    {
        // Arrange - Black's Raja on its throne, d1; its palace, if any, d1 and the row above it, b2 to f2
        GameState start = Hired(BlackToMove([]));
        GameState state = hasPalace
            ? start with
            {
                Palaces = ImmutableDictionary<SeatId, ImmutableList<CellId>>.Empty.Add(
                    Black,
                    [BlackThrone, Grid.CellAt(2, 1), Grid.CellAt(3, 1), Grid.CellAt(4, 1)]
                ),
                Position = start.Position.Place(Grid.CellAt(3, 1), BlackPawn),
            }
            : start;

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(
            squares,
            string.Join(
                ',',
                actions.OfType<DeployAction>()
                    .OrderBy(deploy => deploy.Home.Value)
                    .Select(deploy => Grid.NameOf(deploy.Home))
            )
        );
    }

    [Fact]
    public void Apply_WhenAHiredMercenaryIsPlaced_PutsItOnTheBoardForItsHirer()
    {
        // Arrange
        GameState state = Hired(BlackToMove([]));

        // Act
        Transition transition = this._rules.Apply(state, new DeployAction(Black, YellowHorse.Id, Grid.CellAt(3, 1)));

        // Assert
        Piece hired = YellowHorse with { Owner = Black };
        Assert.Equal(
            (new PieceDeployed(hired, Grid.CellAt(3, 1)), hired, 0),
            (
                transition.Events[0],
                transition.State.Position.PieceAt(Grid.CellAt(3, 1)),
                transition.State.Hired.Count
            )
        );
    }

    [Theory]
    [InlineData("horse", 4, 4)]
    [InlineData("elephant", 2, 4)]
    [InlineData("hired horse", 2, 6)]
    public void Apply_WhenAMercenaryIsPromoted_MakesWhatItBecomesOneNeverCheaper(
        string becomes,
        int pawnCost,
        int lastCost
    )
    {
        // Arrange - Black's Pawn, a mercenary that last cost pawnCost, a step from the far edge; a piece of another
        // army is held: Green's Horse (3), or Elephant (4), or Yellow's Horse, a mercenary that last cost 6
        Piece into = becomes == "hired horse"
            ? YellowHorse
            : new Piece(new PieceId(20), new PieceKindId(becomes), Green);
        GameState start = BlackToMove([]);
        GameState state = start with
        {
            Position = start.Position.Place(Grid.CellAt(2, 6), BlackPawn),
            Prisoners = [new Prisoner(into, Yellow)],
            MercenaryCosts = ImmutableDictionary<PieceId, int>.Empty
                .Add(BlackPawn.Id, pawnCost)
                .Add(YellowHorse.Id, 6),
        };
        GameState moved = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(2, 6), Grid.CellAt(2, 7))).State;

        // Act
        Transition transition = this._rules.Apply(moved, new PromoteAction(Black, Grid.CellAt(2, 7), into.Id));

        // Assert - a Horse is 3, less than the Pawn's 4; an Elephant is 4, more than the Pawn's 2
        Assert.Equal(lastCost, transition.State.MercenaryCosts[into.Id]);
    }

    [Fact]
    public void GuideOf_WhenTheKindIsAMercenaryButNotALeader_TellsSo()
    {
        // Arrange - nothing beyond the rules

        // Act
        ImmutableList<string> horse = this._rules.GuideOf(Chaturaji2027Pieces.Horse).SpecialPowers;
        ImmutableList<string> raja = this._rules.GuideOf(Chaturaji2027Pieces.Raja).SpecialPowers;

        // Assert
        Assert.Equal(
            (true, false),
            (
                horse.Any(power => power.StartsWith("Mercenary: ", StringComparison.Ordinal)),
                raja.Any(power => power.StartsWith("Mercenary: ", StringComparison.Ordinal))
            )
        );
    }

    /// <summary>Black holds Yellow's Horse prisoner, with 10 points.</summary>
    private static GameState Holding(GameState state) =>
        state with
        {
            Scores = state.Scores.SetItem(Black, 10),
            Prisoners = [new Prisoner(YellowHorse, Black)],
        };

    /// <summary>Black hired Yellow's Horse, for 4.</summary>
    private static GameState Hired(GameState state) =>
        state with
        {
            Hired = [YellowHorse with { Owner = Black }],
            MercenaryCosts = ImmutableDictionary<PieceId, int>.Empty.Add(YellowHorse.Id, 4),
        };

    /// <summary>
    /// Black to move with <paramref name="dice"/>, in a game with mercenaries: its Raja on its throne, d1, Yellow's on
    /// a8. The game started with Black's Pawn and Raja, Yellow's Raja and Horse.
    /// </summary>
    private static GameState BlackToMove(int[] dice) =>
        new Chaturaji2027Rules().CreateInitialState() with
        {
            TurnNumber = 2,
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = Black,
            Dice = [.. dice],
            Edition = MercenaryEdition,
            Position = Position.Empty.Place(BlackThrone, BlackRaja).Place(Grid.CellAt(0, 7), YellowRaja),
            Start = Position.Empty
                .Place(BlackThrone, BlackRaja)
                .Place(Grid.CellAt(4, 1), BlackPawn)
                .Place(Grid.CellAt(0, 7), YellowRaja)
                .Place(Grid.CellAt(1, 7), YellowHorse),
        };
}
