using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class GameStarterTest
{
    private const string Ruleset = "chaturaji-2027";

    private static readonly BotDto Random = Bot("random", 1000);
    private static readonly BotDto Midas = Bot("midas", 1100);
    private static readonly BotDto Solomon = Bot("solomon", 1250);
    private static readonly BotDto Oracle = Bot("oracle", 1400);
    private static readonly BotDto Claude = new("claude", "Claude", "Asks Claude.", UsesAi: true, IsPaid: true);
    private static readonly BotDto Pythia = Unrated("pythia");
    private static readonly BotDto Tactician = Unrated("tactician");

    [Fact]
    public void Strengths_WhenThereAreEnoughBots_SplitsThemEasiestFirstLeavingOutThosePaidFor()
    {
        // Arrange
        CatalogDto catalog = Catalog(Oracle, Claude, Random, Solomon, Midas);

        // Act
        ImmutableList<Strength> strengths = GameStarter.Strengths(catalog, Ruleset);

        // Assert
        Assert.Equal(
            "Easy bot (Elo 1000)|Medium bot (Elo 1100)|Hard bot (Elo 1250–1400)",
            string.Join("|", strengths.Select(strength => strength.Label))
        );
    }

    [Fact]
    public void Strengths_WhenThereAreFewerBotsThanStrengths_OffersOnlyThoseWithBots()
    {
        // Arrange
        CatalogDto catalog = Catalog(Random, Oracle);

        // Act
        ImmutableList<Strength> strengths = GameStarter.Strengths(catalog, Ruleset);

        // Assert
        Assert.Equal("Medium|Hard", string.Join("|", strengths.Select(strength => strength.Name)));
    }

    [Fact]
    public void Strengths_WhenSomeBotsAreNotRated_OffersThemApartAsUnratedRatherThanAsEasiest()
    {
        // Arrange
        CatalogDto catalog = Catalog(Pythia, Random, Solomon, Midas, Tactician);

        // Act
        ImmutableList<Strength> strengths = GameStarter.Strengths(catalog, Ruleset);

        // Assert
        Assert.Equal(
            ("Easy bot (Elo 1000)|Medium bot (Elo 1100)|Hard bot (Elo 1250)|Unrated bot", "pythia,tactician"),
            (
                string.Join("|", strengths.Select(strength => strength.Label)),
                string.Join(",", strengths[3].Bots.Select(bot => bot.Id).Order(StringComparer.Ordinal))
            )
        );
    }

    [Fact]
    public void Strengths_WhenNoBotIsRated_SplitsThemAllWithoutRatings()
    {
        // Arrange
        CatalogDto catalog = Catalog(Pythia, Tactician, Unrated("sage"));

        // Act
        ImmutableList<Strength> strengths = GameStarter.Strengths(catalog, Ruleset);

        // Assert
        Assert.Equal("Easy bot|Medium bot|Hard bot", string.Join("|", strengths.Select(strength => strength.Label)));
    }

    [Theory]
    [InlineData("Easy", 2, "midas,random")]
    [InlineData("Hard", 2, "midas,solomon")]
    [InlineData("Medium", 3, "midas,random,solomon")]
    [InlineData("Hard", 4, "midas,pythia,random,solomon")]
    public void QuickBots_WhenSomeBotsAreNotRated_SeatsThemOnlyIfTooFewAreRated(
        string difficulty,
        int count,
        string expected
    )
    {
        // Arrange
        CatalogDto catalog = Catalog(Pythia, Random, Solomon, Midas);

        // Act
        IEnumerable<string> bots =
            GameStarter.QuickBots(catalog, Ruleset, Enum.Parse<Difficulty>(difficulty), count);

        // Assert
        Assert.Equal(expected, string.Join(",", bots.Order(StringComparer.Ordinal)));
    }

    [Fact]
    public void ByStrength_WhenEachSeatHasItsOwn_SeatsABotOfThatStrengthAndNoneForAPerson()
    {
        // Arrange
        ImmutableList<Strength> strengths = GameStarter.Strengths(Catalog(Random, Midas, Solomon, Oracle), Ruleset);

        // Act
        ImmutableList<string?> bots =
            GameStarter.ByStrength([new(null), new(strengths[0]), new(strengths[2]), new(strengths[2])], strengths);

        // Assert
        Assert.Equal(
            (null, "random", "oracle,solomon"),
            (bots[0], bots[1], string.Join(",", bots.Skip(2).Order(StringComparer.Ordinal)))
        );
    }

    [Fact]
    public void ByStrength_WhenAStrengthRunsOutOfBots_SeatsTheNearestLeft()
    {
        // Arrange
        ImmutableList<Strength> strengths = GameStarter.Strengths(Catalog(Random, Midas, Solomon, Oracle), Ruleset);

        // Act
        ImmutableList<string?> bots = GameStarter.ByStrength([new(strengths[0]), new(strengths[0])], strengths);

        // Assert
        Assert.Equal(["random", "midas"], bots);
    }

    [Fact]
    public void ByStrength_WhenABotIsChosenForALaterSeat_SeatsItThereAndNoneOfItsLikeEarlier()
    {
        // Arrange
        ImmutableList<Strength> strengths = GameStarter.Strengths(Catalog(Random, Midas, Solomon, Oracle), Ruleset);

        // Act
        ImmutableList<string?> bots =
            GameStarter.ByStrength([new(strengths[2]), new(strengths[2], "solomon")], strengths);

        // Assert
        Assert.Equal(["oracle", "solomon"], bots);
    }

    [Fact]
    public void ByStrength_WhenEveryBotIsSeated_LeavesTheSeatToAPerson()
    {
        // Arrange
        ImmutableList<Strength> strengths = GameStarter.Strengths(Catalog(Random), Ruleset);

        // Act
        ImmutableList<string?> bots = GameStarter.ByStrength([new(strengths[0]), new(strengths[0])], strengths);

        // Assert
        Assert.Equal(["random", null], bots);
    }

    [Theory]
    [InlineData(true, "solomon (Elo 1250)")]
    [InlineData(false, "solomon")]
    public void LabelOf_WhenABotOfAStrengthIsOffered_NamesItWithItsRatingIfItHasOne(bool isRated, string expected)
    {
        // Arrange
        Strength strength = GameStarter.Strengths(Catalog(Random, Midas, Solomon), Ruleset)[2];
        BotDto bot = isRated ? Solomon : Solomon with { Ratings = [] };

        // Act
        string label = strength.LabelOf(bot);

        // Assert
        Assert.Equal(expected, label);
    }

    [Theory]
    [InlineData(new[] { 1 }, 1, 0)]
    [InlineData(new[] { 0, 1 }, 1, 2)]
    [InlineData(new[] { 0, 1, 2 }, 1, GameStarter.PersonLevel)]
    public void LevelWithRoom_WhenOtherSeatsHaveBots_IsTheNearestWithOneLeftTheEasierOnATie(
        int[] taken,
        int wanted,
        int expected
    )
    {
        // Arrange - a bot of each strength: Random easy, Midas medium, Solomon hard
        ImmutableList<Strength> strengths = GameStarter.Strengths(Catalog(Random, Midas, Solomon), Ruleset);

        // Act
        int level = GameStarter.LevelWithRoom(strengths, taken, wanted);

        // Assert
        Assert.Equal(expected, level);
    }

    private static BotDto Bot(string id, int rating) =>
        new(id, id, "Plays.", UsesAi: false) { Ratings = Rated(rating) };

    private static BotDto Unrated(string id) => new(id, id, "Plays.", UsesAi: true);

    private static ImmutableDictionary<string, int> Rated(int rating) =>
        ImmutableDictionary<string, int>.Empty.Add(Ruleset, rating);

    [Theory]
    [InlineData("chaturaji-2027", 0, "chaturaji-2027")]
    [InlineData("gone", 0, "gone")]
    [InlineData("grand-custom", 0, "grand-medium")]
    [InlineData("grand-custom", 20, "grand-large")]
    [InlineData("grand-custom", 6, "grand-small")]
    [InlineData("lone-custom", 9, "lone-custom")]
    public void RatedAs_WhenTheBoardIsSetOrNot_RatesAsTheSameGamesNearestSizeIfAny(
        string rulesetId,
        int side,
        string expected
    )
    {
        // Arrange - the custom board 10 × 10, kingdoms 4 × 3, as near the medium (8, 4 × 2) as the large (12, 6 × 3)
        // by side, but nearer it by kingdom; one of Grand's own sizes tells no board; another game has no other size
        BoardSizeDto own = new(10, 10, 4, 3);
        CatalogDto catalog = new(
            [
                new RulesetDto(Ruleset, "Chaturaji 2027", 4) { Game = Ruleset },
                SizeOf("grand-small", new BoardSizeDto(6, 6, 3, 3)),
                SizeOf("grand-medium", new BoardSizeDto(8, 8, 4, 2)),
                SizeOf("grand-large", new BoardSizeDto(12, 12, 6, 3)),
                SizeOf("grand-odd", null),
                SizeOf("grand-custom", own) with { SizableBoard = own },
                new RulesetDto("lone-custom", "Lone", 4) { Game = "lone", SizableBoard = own, Board = own },
            ],
            []
        );

        // Act
        string rated = GameStarter.RatedAs(catalog, rulesetId, side == 0 ? null : new BoardSizeDto(side, side, 3, 3));

        // Assert
        Assert.Equal(expected, rated);

        static RulesetDto SizeOf(string id, BoardSizeDto? board) =>
            new(id, id, 4) { Game = "grand", Board = board };
    }

    private static CatalogDto Catalog(params BotDto[] bots) =>
        new([new RulesetDto(Ruleset, "Chaturaji 2027", 4)], [.. bots]);
}
