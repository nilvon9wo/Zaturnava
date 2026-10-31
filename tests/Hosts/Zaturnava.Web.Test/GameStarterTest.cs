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

    [Theory]
    [InlineData(false, false, false, "Bot: Plain")]
    [InlineData(false, true, false, "Bot: Plain (Elo 1300)")]
    [InlineData(true, true, false, "Bot: Plain (AI, Elo 1300)")]
    [InlineData(true, false, true, "Bot: Plain (AI, costs tokens)")]
    public void LabelOf_WhenABotIsOffered_NamesItWithWhatItIs(bool usesAi, bool isRated, bool isPaid, string expected)
    {
        // Arrange
        BotDto bot = new("plain", "Plain", "Plays.", usesAi, isPaid)
        {
            Ratings = isRated ? Rated(1300) : [],
        };

        // Act
        string label = GameStarter.LabelOf(bot, Ruleset);

        // Assert
        Assert.Equal(expected, label);
    }

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

    private static ImmutableDictionary<string, int> Rated(int rating) =>
        ImmutableDictionary<string, int>.Empty.Add(Ruleset, rating);

    private static CatalogDto Catalog(params BotDto[] bots) =>
        new([new RulesetDto(Ruleset, "Chaturaji 2027", 4)], [.. bots]);
}
