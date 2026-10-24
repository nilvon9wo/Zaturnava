using System.Collections.Immutable;
using System.Text.Json;

namespace Net.NowhereAtAll.Zaturnava.Contracts.Test;

/// <summary>Every contract survives the trip as JSON, in the web defaults the server and clients use.</summary>
public sealed class WireFormatTest
{
    private static readonly JsonSerializerOptions Wire = JsonSerializerOptions.Web;

    [Fact]
    public void GameView_WhenSentAsJson_ArrivesUnchanged()
    {
        // Arrange
        GameViewDto view = new(
            "g",
            "classic",
            "Classic",
            3,
            2,
            1,
            3,
            [new CellDto(0, 0.5, 7.5, "square", 0, "a8")],
            [new PieceDto(0, "raja", "Raja", 0, 0)],
            [
                new SeatDto(0, "Black", 0, 5, "bot", "Random", [new PrisonerDto("pawn", "Pawn", 1)]),
                new SeatDto(1, "Yellow", 1, 0, "open", null, []),
            ],
            [new ActionDto("pass", ImmutableSortedDictionary.CreateRange([KeyValuePair.Create("actor", "1")]))],
            [new DieDto(5, "Pawn or Raja"), new DieDto(3, "Elephant")],
            [new EventDto(7, EventDto.Moved, 0, "Black moved their Pawn from a2 to a3.", 8, 16)],
            new OutcomeDto([0, 2], "Black and Green won: only one team has pieces left.")
        );

        // Act
        GameViewDto? arrived = RoundTrip(view);

        // Assert
        Assert.Equivalent(view, arrived, strict: true);
    }

    [Fact]
    public void GameView_WhenTheGameIsOver_SendsNoDecidingSeat()
    {
        // Arrange
        GameViewDto view = new("g", "classic", "Classic", 3, 2, null, null, [], [], [], [], [], [], null);

        // Act
        string json = JsonSerializer.Serialize(view, Wire);

        // Assert
        Assert.Contains("\"decidingSeat\":null", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_WhenSentAsJson_ArrivesUnchanged()
    {
        // Arrange
        CatalogDto catalog = new(
            [new RulesetDto("classic", "Classic", 4)],
            [new BotDto("random", "Random", "Any move.", UsesAi: false)]
        );

        // Act
        CatalogDto? arrived = RoundTrip(catalog);

        // Assert
        Assert.Equivalent(catalog, arrived, strict: true);
    }

    [Fact]
    public void CreateGameRequest_WhenSentAsJson_KeepsItsOpenSeats()
    {
        // Arrange
        CreateGameRequest request = new("classic", [null, "random", null, "random"]);

        // Act
        CreateGameRequest? arrived = RoundTrip(request);

        // Assert
        Assert.Equivalent(request, arrived, strict: true);
    }

    [Fact]
    public void GameSummary_WhenSentAsJson_ArrivesUnchanged()
    {
        // Arrange
        ImmutableList<string> players = [GameSummaryDto.Person, GameSummaryDto.Open, "midas"];
        GameSummaryDto summary = new("g", "classic", 4, [1, 3], players, 9);

        // Act
        GameSummaryDto? arrived = RoundTrip(summary);

        // Assert
        Assert.Equivalent(summary, arrived, strict: true);
    }

    [Fact]
    public void Replies_WhenSentAsJson_ArriveUnchanged()
    {
        // Arrange
        GameCreatedDto created = new("g");
        SeatClaimedDto claimed = new("token");

        // Act
        (GameCreatedDto?, SeatClaimedDto?) arrived = (RoundTrip(created), RoundTrip(claimed));

        // Assert
        Assert.Equal((created, claimed), arrived);
    }

    private static T? RoundTrip<T>(T value) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Wire), Wire);
}
