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
    public async Task Decide_WhenDraftingAndNoneOfItsFamilysPiecesIsOffered_BuysItsKinsAndIsDealtSo()
    {
        // Arrange - a Western Pawn or a Shogi Horse, the Horse worth more: Naresuan, no Makruk piece offered, takes
        // the Pawn, Western chess's being most like Makruk's (the user's report of 2026-11-20)
        DraftAction pawn = new(ScriptedRules.Seat, LineRules.Pawn, new CellId(1));
        DraftAction horse = new(ScriptedRules.Seat, LineRules.Horse, new CellId(4));
        ScriptedRules rules = new(
            ImmutableDictionary<GameAction, GameState>.Empty
                .Add(pawn, ScriptedRules.State((1, LineRules.Pawn, ScriptedRules.Seat)))
                .Add(horse, ScriptedRules.State((4, LineRules.Horse, ScriptedRules.Seat))),
            origins: ImmutableDictionary<PieceKindId, string>.Empty
                .Add(LineRules.Pawn, "Western")
                .Add(LineRules.Horse, "Shogi")
        );
        FamilyBot naresuan = FamilyBot.All(new Random(1)).Single(candidate => candidate.Profile.Id == "naresuan");

        // Act
        GameAction drafted = await naresuan.Decide(rules.Request(), this._cancellation);

        // Assert
        Assert.Equal((1, "Makruk+Western"), (((DraftAction)drafted).Cell.Value, naresuan.Profile.Family));
    }

    [Fact]
    public async Task Decide_WhenNothingIsToBeDrafted_DecidesAsSageWould()
    {
        // Arrange - only a pass to make
        PassAction pass = new(ScriptedRules.Seat);
        ScriptedRules rules = new(
            ImmutableDictionary<GameAction, GameState>.Empty.Add(pass, ScriptedRules.State())
        );
        FamilyBot musashi = FamilyBot.All(new Random(1)).Single(candidate => candidate.Profile.Id == "musashi");

        // Act
        GameAction decided = await musashi.Decide(rules.Request(), this._cancellation);

        // Assert
        Assert.Equal(pass, decided);
    }

    [Theory]
    [InlineData("musashi", true, true)]
    [InlineData("musashi", false, false)]
    [InlineData("ragnar", true, false)]
    public void Knows_WhenAskedOfAGame_KnowsOnlyOneDraftedWithPiecesOfItsFamily(
        string bot,
        bool isDraftable,
        bool expected
    )
    {
        // Arrange - a Shogi Pawn and a Western Horse, drafted or dealt: no Tafl piece for Ragnar (the user's word of
        // 2026-11-20: only the bots that make sense for the game)
        ScriptedRules rules = new(
            [],
            origins: ImmutableDictionary<PieceKindId, string>.Empty
                .Add(LineRules.Pawn, "Shogi")
                .Add(LineRules.Horse, "Western"),
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
                + "ragnar:False,oberon:False",
            named
        );
    }
}
