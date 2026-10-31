using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Games;

public sealed class SeatControllerTest
{
    private static readonly FirstChoiceBot Bot = new();
    private static readonly BotCatalog Bots = new([Bot]);

    public static TheoryData<SeatController, string, string?> Stored => new()
    {
        { new OpenSeat(), "open", null },
        { new HumanSeat("HASH"), "human", "HASH" },
        { new HumanSeat("HASH", new PlayerId("ada")), "human", "HASH ada" },
        { new BotSeat("first"), "bot", "first" },
    };

    [Theory]
    [MemberData(nameof(Stored), DisableDiscoveryEnumeration = true)]
    public void From_WhenGivenAStoredKindAndDetail_RestoresTheController(
        SeatController controller,
        string kind,
        string? detail
    )
    {
        // Arrange - nothing beyond the arguments

        // Act
        SeatController restored = SeatController.From(kind, detail);

        // Assert
        Assert.Equal((controller, kind, detail), (restored, restored.Kind, restored.Detail));
    }

    [Theory]
    [MemberData(nameof(Players), DisableDiscoveryEnumeration = true)]
    public void Player_WhenRead_SaysWhoPlaysTheSeat(SeatController controller, string expected)
    {
        // Arrange - nothing beyond the arguments

        // Act
        string player = controller.Player;

        // Assert
        Assert.Equal(expected, player);
    }

    public static TheoryData<SeatController, string> Players => new()
    {
        { new OpenSeat(), "open" },
        { new HumanSeat("HASH"), "person" },
        { new BotSeat("first"), "first" },
    };

    [Fact]
    public void From_WhenTheKindIsUnknown_ThrowsArgumentException()
    {
        // Arrange - nothing beyond the arguments

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => SeatController.From("ghost", null));

        // Assert
        Assert.Equal("kind", exception.ParamName);
    }

    [Fact]
    public void IsOpen_WhenTheSeatIsClaimed_IsFalse()
    {
        // Arrange
        HumanSeat seat = new("HASH");

        // Act
        bool isOpen = seat.IsOpen;

        // Assert
        Assert.False(isOpen);
    }

    [Fact]
    public void IsHeldBy_WhenTheSeatIsOpen_IsFalse()
    {
        // Arrange
        OpenSeat seat = new();

        // Act
        bool isHeld = seat.IsHeldBy("anything");

        // Assert
        Assert.False(isHeld);
    }

    [Fact]
    public void IsHeldBy_WhenGivenTheTokenWhoseHashItKeeps_IsTrue()
    {
        // Arrange
        HumanSeat seat = new(SeatTokens.Hash("secret"));

        // Act
        bool isHeld = seat.IsHeldBy("secret");

        // Assert
        Assert.True(isHeld);
    }

    [Fact]
    public void DeciderFrom_WhenThePersonDecides_IsNull()
    {
        // Arrange
        HumanSeat seat = new("HASH");

        // Act
        IDeciding? decider = seat.DeciderFrom(Bots);

        // Assert
        Assert.Null(decider);
    }

    [Fact]
    public void DeciderFrom_WhenABotDecides_IsThatBot()
    {
        // Arrange
        BotSeat seat = new(FirstChoiceBot.BotId);

        // Act
        IDeciding decider = seat.DeciderFrom(Bots);

        // Assert
        Assert.Same(Bot, decider);
    }
}
