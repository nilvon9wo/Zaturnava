using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Application.Test.Players;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Persistence.InMemory;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Bots;

public sealed class BotRatingServiceTest
{
    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);

    private readonly InMemoryBotMatchupStore _matchups = new();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("Black won", 0, 1, 0, 0)]
    [InlineData("Black won", 1, 0, 1, 0)]
    [InlineData("drawn", 0, 0, 0, 1)]
    [InlineData("Black ahead on points", 0, 1, 0, 0)]
    [InlineData("Yellow ahead on points", 0, 0, 1, 0)]
    [InlineData("level on points", 0, 0, 0, 1)]
    public async Task PlayNext_WhenThePairingHasPlayedSoMany_SeatsTheFirstBotByTurnsAndKeepsHowItWent(
        string how,
        int played,
        int won,
        int lost,
        int drawn
    )
    {
        // Arrange - a and b, the first at Black's side in the first game and Yellow's in the second
        if (played > 0)
        {
            await this.Keep(new BotMatchup(EndedRules.RulesetId, "a", "b") { Drawn = played });
        }

        GameState ended = how switch
        {
            "Black won" => EndedRules.State([Black]),
            "drawn" => EndedRules.State([]),
            _ => EndedRules.State([]) with
            {
                Outcome = null,
                Scores = how switch
                {
                    "Black ahead on points" => ImmutableDictionary<SeatId, int>.Empty.Add(Black, 5).Add(Yellow, 3),
                    "Yellow ahead on points" => ImmutableDictionary<SeatId, int>.Empty.Add(Yellow, 2),
                    _ => [],
                },
            },
        };
        BotRatingService service = this.Service([new EndedRules(ended)], []);

        // Act
        BotMatchup game = await service.PlayNext(this._cancellation);

        // Assert
        Assert.Equal(
            (
                new BotMatchup(EndedRules.RulesetId, "a", "b") { Won = won, Lost = lost, Drawn = drawn },
                played + 1
            ),
            (game, Assert.Single(await this._matchups.Matchups(this._cancellation)).Played)
        );
    }

    [Fact]
    public async Task PlayNext_WhenOneRulesetsPairingHasPlayedMore_PlaysTheOtherRulesetsNext()
    {
        // Arrange
        await this.Keep(new BotMatchup(EndedRules.RulesetId, "a", "b") { Won = 1 });
        BotRatingService service = this.Service(
            [new EndedRules(EndedRules.State([Black])), new EndedRules(EndedRules.State([Black]), "other")],
            []
        );

        // Act
        BotMatchup game = await service.PlayNext(this._cancellation);

        // Assert
        Assert.Equal("other", game.Ruleset);
    }

    [Fact]
    public async Task Apply_WhenEveryPairAtARulesetHasPlayedEnough_RatesItByItsGamesAndLeavesTheOthersAsShipped()
    {
        // Arrange - b won 3 of 3 against a, the anchor; a bot no longer offered played too; at other, 1 game only
        await this.Keep(new BotMatchup(EndedRules.RulesetId, "a", "b") { Lost = 3 });
        await this.Keep(new BotMatchup(EndedRules.RulesetId, "a", "gone") { Won = 9 });
        await this.Keep(new BotMatchup("other", "a", "b") { Won = 1 });
        BotCatalog bots = Catalog(
            ImmutableDictionary<string, ImmutableDictionary<string, int>>.Empty
                .Add(EndedRules.RulesetId, ImmutableDictionary<string, int>.Empty.Add("a", 1200))
                .Add("other", ImmutableDictionary<string, int>.Empty.Add("a", 1300))
        );
        BotRatingService service = this.Service(
            [new EndedRules(EndedRules.State([])), new EndedRules(EndedRules.State([]), "other")],
            bots
        );

        // Act
        ImmutableList<string> rated = await service.Apply(this._cancellation);

        // Assert
        Assert.Equal(
            (EndedRules.RulesetId, "a ended 1000, other 1300; b ended 1338"),
            (
                string.Join(",", rated),
                string.Join(
                    "; ",
                    bots.Bots.Select(bot =>
                        $"{bot.Id} " + string.Join(
                            ", ",
                            bot.Ratings.OrderBy(rating => rating.Key, StringComparer.Ordinal)
                                .Select(rating => $"{rating.Key} {rating.Value}")
                        )
                    )
                )
            )
        );
    }

    private static BotCatalog Catalog(ImmutableDictionary<string, ImmutableDictionary<string, int>> ratings) =>
        new([new FirstChoiceBot("a"), new FirstChoiceBot("b")], ratings);

    private Task Keep(BotMatchup games) => this._matchups.Add(games, this._cancellation);

    private BotRatingService Service(
        ImmutableList<IRulesApplying> rules,
        ImmutableDictionary<string, ImmutableDictionary<string, int>> ratings
    ) =>
        this.Service(rules, Catalog(ratings));

    private BotRatingService Service(ImmutableList<IRulesApplying> rules, BotCatalog bots) =>
        new(new RulesetCatalog(rules), bots, this._matchups, new LoadedDice(1), new BotRatingSettings("a"));
}
