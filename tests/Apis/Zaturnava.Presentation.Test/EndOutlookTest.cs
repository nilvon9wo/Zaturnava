using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation.Test;

/// <summary>
/// The outlook for a seat, were the game ended now: four seats, Black (0) with Green (2) against Yellow (1) with Red
/// (3) unless said; a Rook worth 5 and a Pawn worth 1. An enemy army that has lost nothing and has nothing on the
/// board counts as beaten.
/// </summary>
public sealed class EndOutlookTest
{
    [Theory]
    [InlineData(10, 5, EndStanding.Victory, "Declare Victory", "Accept Victory?")]
    [InlineData(5, 10, EndStanding.Defeat, "Concede Defeat", "Accept Defeat?")]
    [InlineData(7, 7, EndStanding.Draw, "Propose Draw", "Accept Draw?")]
    public void Of_ByTeamPointsAlone_SaysWhetherEndingNowWinsDrawsOrLoses(
        int black,
        int yellow,
        EndStanding standing,
        string proposal,
        string question
    )
    {
        // Arrange - Black's team far stronger on the board, which does not count for who wins
        GameViewDto view = View([black, yellow, 0, 0], [("rook", 0), ("rook", 2)]);

        // Act
        EndOutlook outlook = EndOutlook.Of(view, 0);

        // Assert
        Assert.Equal((standing, proposal, question), (outlook.Standing, outlook.Proposal, outlook.Question));
    }

    [Theory]
    [InlineData(7, 7, 0)]
    [InlineData(10, 9, 0.04)]
    [InlineData(100, 0, 0.99)]
    [InlineData(2, 1, 0)]
    public void Of_WhenOneSideLeads_IsAsSureAsTheLeadersStrengthOverTheOthers(int black, int yellow, double expected)
    {
        // Arrange - a Pawn each, but for the third, where Yellow, behind on points, holds a Rook as well; Yellow has
        // lost 99 Pawns besides, so that, it being beaten, the teams' strength decides, but for the second: no surer
        // than the 99 in 100 it has lost
        ImmutableList<(string, int)> pieces =
            black == 2 ? [("pawn", 0), ("pawn", 1), ("rook", 1)] : [("pawn", 0), ("pawn", 1)];
        GameViewDto view = View([black, yellow, 0, 0], pieces, prisoners: YellowsBeaten);

        // Act
        double certainty = EndOutlook.Of(view, 0).Certainty;

        // Assert
        Assert.Equal(expected, certainty, 2);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Of_WhenTenTimesAheadOnPointsAndPieces_IsASureThing(bool hasNoArmyLeft, bool expected)
    {
        // Arrange - Black's team 100 points to 5, with two Rooks, against a Pawn, or a Rook
        GameViewDto view = View(
            [100, 5, 0, 0],
            [("rook", 0), ("rook", 2), (hasNoArmyLeft ? "pawn" : "rook", 1)],
            prisoners: YellowsBeaten
        );

        // Act
        bool isSure = EndOutlook.Of(view, 0).IsSure;

        // Assert
        Assert.Equal(expected, isSure);
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(5, 0.5, false)]
    [InlineData(45, 0.9, true)]
    public void Of_WhenAnEnemyArmyIsWhole_IsNoSurerThanTheShareOfItLost(int lost, double expected, bool isSure)
    {
        // Arrange - Black's team 100 points to nothing, and 14 Rooks; Yellow lost its army but a Pawn; Red has a Rook,
        // or 5, and has lost pieces worth as many points as said (the user, 2026-11-27: "the red army still has
        // literally all their pieces but the 'Declare Victory' button is already visibly green")
        ImmutableList<(string, int)> pieces =
        [
            .. Enumerable.Repeat(("rook", 0), 14), ("pawn", 1), .. Enumerable.Repeat(("rook", 3), lost == 0 ? 5 : 1),
        ];
        ImmutableList<(int, string, int)> prisoners =
            [.. YellowsBeaten, .. Enumerable.Repeat((0, "pawn", 3), lost == 0 ? 0 : lost - 5)];
        GameViewDto view = View([100, 0, 0, 0], pieces, prisoners: [.. prisoners, .. RedsRook(lost)]);

        // Act
        EndOutlook outlook = EndOutlook.Of(view, 0);

        // Assert
        Assert.Equal((expected, isSure), (Math.Round(outlook.Certainty, 2), outlook.IsSure));
    }

    [Theory]
    [InlineData(false, false, 0.99, true)]
    [InlineData(true, false, 0.5, false)]
    [InlineData(false, true, 0.5, false)]
    public void Of_WhileTheEnemyHasALeaderOrAHeavyLeft_IsHalfAsSureAndNeverPastDoubt(
        bool isLeader,
        bool isHeavy,
        double expected,
        bool isSure
    )
    {
        // Arrange - Black's team 100 points to 5, two Rooks against Yellow's last piece, a Pawn, or a leader or heavy
        // piece as weak (the user, 2026-11-27: a leader or a heavy left is a reason "for the button not to be green")
        GameViewDto view = View(
            [100, 5, 0, 0],
            [("rook", 0), ("rook", 2), ("pawn", 1)],
            prisoners: YellowsBeaten
        ) with
        {
            Kinds =
            [
                new PieceKindDto("rook", "Rook", 5),
                new PieceKindDto("pawn", "Pawn", 1) { IsLeader = isLeader, IsHeavy = isHeavy },
            ],
        };

        // Act
        EndOutlook outlook = EndOutlook.Of(view, 0);

        // Assert
        Assert.Equal((expected, isSure), (Math.Round(outlook.Certainty, 2), outlook.IsSure));
    }

    /// <summary>Yellow's 99 Pawns, held by Black.</summary>
    private static readonly ImmutableList<(int Holder, string Kind, int Owner)> YellowsBeaten =
        [.. Enumerable.Repeat((0, "pawn", 1), 99)];

    /// <summary>A Rook of Red's held by Green, if Red has lost anything.</summary>
    private static ImmutableList<(int Holder, string Kind, int Owner)> RedsRook(int lost) =>
        lost == 0 ? [] : [(2, "rook", 3)];

    [Fact]
    public void Of_ForASeatOfTheTeamBehind_CountsItsAlliesPointsAndThoseOfTheBestOtherTeam()
    {
        // Arrange - each seat alone, Red leading Black, which leads the rest
        GameViewDto view = View([6, 2, 3, 9], [], teams: [0, 1, 2, 3]);

        // Act
        EndOutlook outlook = EndOutlook.Of(view, 0);

        // Assert
        Assert.Equal(EndStanding.Defeat, outlook.Standing);
    }

    [Fact]
    public void Of_WhenNoOtherTeamPlays_IsADraw()
    {
        // Arrange - all four seats of one team
        GameViewDto view = View([3, 3, 3, 3], [("mystery", 0)], teams: [0, 0, 0, 0]);

        // Act
        EndOutlook outlook = EndOutlook.Of(view, 0);

        // Assert
        Assert.Equal((EndStanding.Draw, 0d, false), (outlook.Standing, outlook.Certainty, outlook.IsSure));
    }

    private static GameViewDto View(
        int[] scores,
        ImmutableList<(string Kind, int Owner)> pieces,
        int[]? teams = null,
        ImmutableList<(int Holder, string Kind, int Owner)>? prisoners = null
    ) =>
        new(
            "game",
            "chaturaji-2027",
            "Chaturaji 2027",
            1,
            1,
            0,
            null,
            [],
            [.. pieces.Select((piece, at) => new PieceDto(at, piece.Kind, piece.Kind, piece.Owner, at))],
            [
                .. scores.Select((score, seat) =>
                    new SeatDto(
                        seat,
                        $"Seat {seat}",
                        teams is null ? seat % 2 : teams[seat],
                        score,
                        "human",
                        null,
                        [
                            .. (prisoners ?? [])
                                .Where(prisoner => prisoner.Holder == seat)
                                .Select((prisoner, at) =>
                                    new PrisonerDto(at, prisoner.Kind, prisoner.Kind, prisoner.Owner)
                                ),
                        ]
                    )
                ),
            ],
            [],
            [],
            [],
            null,
            false,
            null,
            null,
            null,
            false
        )
        {
            Kinds = [new PieceKindDto("rook", "Rook", 5), new PieceKindDto("pawn", "Pawn", 1)],
        };
}
