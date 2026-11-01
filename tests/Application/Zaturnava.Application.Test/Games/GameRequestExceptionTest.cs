using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Games;

public sealed class GameRequestExceptionTest
{
    private static readonly GameId Game = new("g");

    public static TheoryData<GameRequestException, GameRequestFailure, string> Refusals => new()
    {
        { new GameNotFoundException(Game), GameRequestFailure.NotFound, "There is no game 'g'." },
        { new InvalidGameSetupException("Bad."), GameRequestFailure.Invalid, "Bad." },
        { new UnknownRulesetException("chess"), GameRequestFailure.Invalid, "There is no 'chess' ruleset." },
        { new UnknownBotException("genius"), GameRequestFailure.Invalid, "There is no 'genius' bot." },
        { new UnknownScenarioException("odd"), GameRequestFailure.Invalid, "There is no 'odd' scenario." },
        { new InvalidScenarioException("Odd."), GameRequestFailure.Invalid, "Odd." },
        {
            new NotYourTurnException(Game, new SeatId(1)),
            GameRequestFailure.Forbidden,
            "Seat 1 of game 'g' is not yours to play now."
        },
        {
            new SeatUnavailableException(Game, new SeatId(2)),
            GameRequestFailure.Conflict,
            "Seat 2 of game 'g' is not open."
        },
        {
            new NotYourSeatException(Game, new SeatId(3)),
            GameRequestFailure.Forbidden,
            "The token does not hold seat 3 of game 'g'."
        },
        {
            new NotAPlayerException(Game),
            GameRequestFailure.Forbidden,
            "Only a player of game 'g' may fill its open seats."
        },
        {
            new TooSoonToFillException(Game),
            GameRequestFailure.Conflict,
            "Game 'g' still waits for people to take its open seats: wait a little longer."
        },
        {
            new ConcurrencyConflictException(Game),
            GameRequestFailure.Conflict,
            "Game 'g' changed meanwhile; read it again."
        },
        {
            new UnknownPlayerException(),
            GameRequestFailure.Forbidden,
            "That player token stands for no one, now: sign in again, or play as a guest."
        },
    };

    [Theory]
    [MemberData(nameof(Refusals), DisableDiscoveryEnumeration = true)]
    public void Failure_WhenRefused_SaysWhatKindOfRefusalAndWhy(
        GameRequestException exception,
        GameRequestFailure failure,
        string message
    )
    {
        // Arrange - nothing beyond the arguments

        // Act
        GameRequestFailure reported = exception.Failure;

        // Assert
        Assert.Equal((failure, message), (reported, exception.Message));
    }
}
