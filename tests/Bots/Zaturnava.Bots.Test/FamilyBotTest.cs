using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class FamilyBotTest
{
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("musashi", 1)]
    [InlineData("naresuan", 4)]
    [InlineData("sun-tzu", 0)]
    public async Task Decide_WhenDrafting_BuysOnlyItsFamilysPiecesElseIsDone(string bot, int cell)
    {
        // Arrange - a Shogi Pawn, or a Western Horse that Makruk plays too, the Horse worth more; or be done, or have
        // the rest filled at random: Musashi takes the Pawn; Naresuan the Horse, Makruk's Ma; Sun Tzu, no Xiangqi
        // piece offered, is done, never taking another's piece, nor any at random (the user's word of 2026-11-20)
        DraftAction pawn = new(ScriptedRules.Seat, LineRules.Pawn, new CellId(1));
        DraftAction horse = new(ScriptedRules.Seat, LineRules.Horse, new CellId(4));
        ScriptedRules rules = new(
            ImmutableDictionary<GameAction, GameState>.Empty
                .Add(pawn, ScriptedRules.State((1, LineRules.Pawn, ScriptedRules.Seat)))
                .Add(horse, ScriptedRules.State((4, LineRules.Horse, ScriptedRules.Seat)))
                .Add(new PassAction(ScriptedRules.Seat), ScriptedRules.State())
                .Add(
                    new FillArmyAction(ScriptedRules.Seat),
                    ScriptedRules.State((2, LineRules.Horse, ScriptedRules.Seat))
                ),
            origins: ImmutableDictionary<PieceKindId, string>.Empty
                .Add(LineRules.Pawn, "Shogi")
                .Add(LineRules.Horse, "Western+Makruk")
        );
        FamilyBot family = FamilyBot.All(new Random(1)).Single(candidate => candidate.Profile.Id == bot);

        // Act
        GameAction decided = await family.Decide(rules.Request(), this._cancellation);

        // Assert - the cell drafted on; 0, for being done
        Assert.Equal(cell, (decided as DraftAction)?.Cell.Value ?? (decided is PassAction ? 0 : -1));
    }

    [Theory]
    [InlineData("pass", "pass")]
    [InlineData("fill", "fill")]
    [InlineData("throne,fill", "throne")]
    public async Task Decide_WhenNothingIsToBeDrafted_DecidesAsSageWouldButNeverFillsItsArmyIfItMayDoElse(
        string offered,
        string expected
    )
    {
        // Arrange - out of the draft, only a pass to make; only to have the army filled at random, which it would
        // rather not, but must; or that, or setting its throne, which it does
        ImmutableDictionary<string, GameAction> actions = ImmutableDictionary<string, GameAction>.Empty
            .Add("pass", new PassAction(ScriptedRules.Seat))
            .Add("fill", new FillArmyAction(ScriptedRules.Seat))
            .Add("throne", new ThroneAction(ScriptedRules.Seat, new CellId(0)));
        ScriptedRules rules = new(
            offered.Split(',').ToImmutableDictionary(name => actions[name], _ => ScriptedRules.State())
        );
        FamilyBot musashi = FamilyBot.All(new Random(1)).Single(candidate => candidate.Profile.Id == "musashi");

        // Act
        GameAction decided = await musashi.Decide(rules.Request(), this._cancellation);

        // Assert
        Assert.Equal(actions[expected], decided);
    }

    [Theory]
    [InlineData("musashi", true, true)]
    [InlineData("musashi", false, false)]
    [InlineData("ragnar", true, false)]
    [InlineData("desdemona", true, true)]
    public void Knows_WhenAskedOfAGame_KnowsOnlyOneDraftedWithPiecesOfItsFamily(
        string bot,
        bool isDraftable,
        bool expected
    )
    {
        // Arrange - a Shogi Pawn and a fairy Horse that Othello plays too, drafted or dealt: no Tafl piece for Ragnar
        // (the user's word of 2026-11-20: only the bots that make sense for the game)
        ScriptedRules rules = new(
            [],
            origins: ImmutableDictionary<PieceKindId, string>.Empty
                .Add(LineRules.Pawn, "Shogi")
                .Add(LineRules.Horse, "Fairy+Othello"),
            isDraftable: isDraftable
        );
        FamilyBot family = FamilyBot.All(new Random(1)).Single(candidate => candidate.Profile.Id == bot);

        // Act
        bool knows = family.Knows(rules);

        // Assert
        Assert.Equal(expected, knows);
    }

    [Fact]
    public async Task Seize_WhenNothingIsOffered_SeizesNothing()
    {
        // Arrange
        DecisionRequest request = new ScriptedRules([]).Request();

        // Act
        GameAction? seized = await FamilyBot.All(new Random(1))[0].Seize(request, this._cancellation);

        // Assert
        Assert.Null(seized);
    }

    [Fact]
    public void All_WhenAsked_IsOnePlainAlgorithmForEachFamily()
    {
        // Arrange - the specification's families (D4)

        // Act
        string named = string.Join(
            ",",
            FamilyBot.All(new Random(1)).Select(bot => $"{bot.Profile.Id}:{bot.Profile.UsesAi}")
        );

        // Assert
        Assert.Equal(
            "philidor:False,sun-tzu:False,yi-sun-sin:False,musashi:False,naresuan:False,palamedes:False,"
                + "ragnar:False,oberon:False,desdemona:False",
            named
        );
    }
}
