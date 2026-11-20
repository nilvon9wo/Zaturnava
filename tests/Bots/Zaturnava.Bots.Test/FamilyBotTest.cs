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
    [InlineData("sun-tzu", 4)]
    public async Task Decide_WhenDrafting_BuysItsFamilysPiecesWhileItCanElseWhatSageWould(string bot, int cell)
    {
        // Arrange - a Shogi Pawn or a Western Horse to draft, the Horse worth more: Musashi takes the Pawn; Sun Tzu,
        // no Xiangqi piece offered, takes the Horse, as Sage would
        DraftAction pawn = new(ScriptedRules.Seat, LineRules.Pawn, new CellId(1));
        DraftAction horse = new(ScriptedRules.Seat, LineRules.Horse, new CellId(4));
        ScriptedRules rules = new(
            ImmutableDictionary<GameAction, GameState>.Empty
                .Add(pawn, ScriptedRules.State((1, LineRules.Pawn, ScriptedRules.Seat)))
                .Add(horse, ScriptedRules.State((4, LineRules.Horse, ScriptedRules.Seat))),
            origins: ImmutableDictionary<PieceKindId, string>.Empty
                .Add(LineRules.Pawn, "Shogi")
                .Add(LineRules.Horse, "Western")
        );
        FamilyBot family = FamilyBot.All(new Random(1)).Single(candidate => candidate.Profile.Id == bot);

        // Act
        GameAction drafted = await family.Decide(rules.Request(), this._cancellation);

        // Assert
        Assert.Equal(cell, ((DraftAction)drafted).Cell.Value);
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
                + "ragnar:False,oberon:False",
            named
        );
    }
}
