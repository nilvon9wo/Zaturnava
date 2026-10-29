using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class PythiaBotTest
{
    private const int Seed = 7;
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);
    private static readonly MoveAction Taking = LineRules.Move(3, 4);

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public void Profile_WhenAsked_SaysItUsesAiAtNoCost()
    {
        // Arrange
        PythiaBot bot = new(new Random(Seed), new Advisor((_, _) => Task.FromResult<int?>(0)), Patience);

        // Act
        BotProfile profile = bot.Profile;

        // Assert
        Assert.Equal(
            (PythiaBot.BotId, "Pythia", true, false),
            (profile.Id, profile.Name, profile.UsesAi, profile.IsPaid)
        );
    }

    [Fact]
    public async Task Decide_WhenAsking_TellsTheAdvisorTheGameAndTheMovesSageLikesBestFirst()
    {
        // Arrange - Pythia's Pawn beside an enemy Horse no one guards; an ally holds an enemy Horse
        DecisionRequest free = Free(new PromoteAction(LineRules.Seat, new CellId(3), new PieceId(9)));
        DecisionRequest request = free with
        {
            State = free.State with
            {
                Prisoners = [new Prisoner(new Piece(new PieceId(8), LineRules.Horse, LineRules.Enemy), new SeatId(2))],
                Dice = [3, 5],
            },
        };
        MoveQuestion? asked = null;
        Advisor advisor = new((question, _) =>
        {
            asked = question;
            return Task.FromResult<int?>(null);
        });

        // Act
        _ = await new PythiaBot(new Random(Seed), advisor, Patience).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(
            string.Join(
                '\n',
                "You are playing Line, as Seat0.",
                "Your team: Seat0, Seat2.",
                "Scores: Seat0 0, Seat1 0, Seat2 0, Seat3 0.",
                "Pieces: Seat0's pawn on c3, Seat1's horse on c4.",
                "Prisoners: Seat2 holds Seat1's horse.",
                "Dice rolled and not yet used: 3, 5."
            ),
            asked!.Situation
        );
        Assert.Equal(["move from c3 to c4, taking Seat1's horse", "promote cell=3 into=9", "pass"], asked.Choices);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Decide_WhenTheAdvisorNamesAnotherChoice_MakesIt(int advised)
    {
        // Arrange
        ImmutableList<string> choices = [];
        Advisor advisor = new((question, _) =>
        {
            choices = question.Choices;
            return Task.FromResult<int?>(advised);
        });

        // Act
        GameAction action = await new PythiaBot(new Random(Seed), advisor, Patience)
            .Decide(Free(), this._cancellation);

        // Assert
        Assert.Equal(choices[advised], Described(action));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    [InlineData(PythiaBot.Choices)]
    public async Task Decide_WhenTheAdvisorNamesNoChoice_MakesSagesOwn(int? advised)
    {
        // Arrange
        Advisor advisor = new((_, _) => Task.FromResult(advised));

        // Act
        GameAction action = await new PythiaBot(new Random(Seed), advisor, Patience)
            .Decide(Free(), this._cancellation);

        // Assert
        Assert.Equal(Taking, action);
    }

    [Fact]
    public async Task Decide_WhenTheAdvisorTakesTooLong_MakesSagesOwn()
    {
        // Arrange
        Advisor advisor = new(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 1;
        });

        // Act
        GameAction action = await new PythiaBot(new Random(Seed), advisor, TimeSpan.FromMilliseconds(10))
            .Decide(Free(), this._cancellation);

        // Assert
        Assert.Equal(Taking, action);
    }

    [Fact]
    public async Task Decide_WhenThereIsNothingToChooseBetween_AsksNoOne()
    {
        // Arrange
        int asked = 0;
        Advisor advisor = new((_, _) => Task.FromResult<int?>(++asked));
        DecisionRequest request = LineRules.Request(
            [Taking],
            (3, LineRules.Pawn, LineRules.Seat),
            (4, LineRules.Horse, LineRules.Enemy)
        );

        // Act
        GameAction action =
            await new PythiaBot(new Random(Seed), advisor, Patience).Decide(request, this._cancellation);

        // Assert
        Assert.Equal((Taking, 0), (action, asked));
    }

    [Fact]
    public async Task Decide_WhenCancelledWhileTheAdvisorThinks_ThrowsOperationCanceledException()
    {
        // Arrange
        using CancellationTokenSource cancelling = new();
        TaskCompletionSource thinking = new();
        Advisor advisor = new(async (_, cancellationToken) =>
        {
            thinking.SetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 1;
        });

        // Act
        Task<GameAction> deciding = new PythiaBot(new Random(Seed), advisor, Patience).Decide(Free(), cancelling.Token);
        await thinking.Task;
        await cancelling.CancelAsync();

        // Assert
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => deciding);
    }

    [Fact]
    public async Task Decide_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();
        Advisor advisor = new((_, _) => Task.FromResult<int?>(0));

        // Act
        Task<GameAction> deciding = new PythiaBot(new Random(Seed), advisor, Patience).Decide(Free(), cancelled.Token);

        // Assert
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => deciding);
    }

    [Fact]
    public async Task Decide_WhenAskedToEndTheGame_AnswersAsSageDoes()
    {
        // Arrange
        Advisor advisor = new((_, _) => throw new InvalidOperationException("Not to be asked"));

        // Act
        GameAction action = await new PythiaBot(new Random(Seed), advisor, Patience)
            .Decide(Proposals.Asked(5, 1), this._cancellation);

        // Assert
        Assert.Equal(new AnswerEndAction(Proposals.Black, true), action);
    }

    [Fact]
    public async Task Decide_WhenOfferedARansom_AnswersAsSageDoes()
    {
        // Arrange
        Advisor advisor = new((_, _) => throw new InvalidOperationException("Not to be asked"));

        // Act
        GameAction action = await new PythiaBot(new Random(Seed), advisor, Patience)
            .Decide(Ransoms.Offered([20], [10]), this._cancellation);

        // Assert
        Assert.Equal(
            await new SageBot(new Random(Seed)).Decide(Ransoms.Offered([20], [10]), this._cancellation),
            action
        );
    }

    [Fact]
    public async Task Decide_WhenItsTurnIsOver_GivesItsAllysPieceBackFirst()
    {
        // Arrange
        Advisor advisor = new((_, _) => throw new InvalidOperationException("Not to be asked"));

        // Act
        GameAction action = await new PythiaBot(new Random(Seed), advisor, Patience)
            .Decide(Ransoms.Window(), this._cancellation);

        // Assert
        Assert.Equal(new OfferAction(Ransoms.Black, Ransoms.Green, [new PieceId(30)], []), action);
    }

    /// <summary>
    /// Pythia's Pawn on c3, beside an enemy Horse on c4 no one guards: taking it, stepping back, or passing.
    /// </summary>
    private static DecisionRequest Free(GameAction? instead = null) =>
        LineRules.Request(
            [Taking, instead ?? LineRules.Move(3, 2), new PassAction(LineRules.Seat)],
            (3, LineRules.Pawn, LineRules.Seat),
            (4, LineRules.Horse, LineRules.Enemy)
        );

    private static string Described(GameAction action) =>
        action switch
        {
            MoveAction { From.Value: 3, To.Value: 4 } => "move from c3 to c4, taking Seat1's horse",
            MoveAction move => $"move from c{move.From.Value} to c{move.To.Value}",
            _ => action.Describe().Kind,
        };
}

/// <summary>An advisor answering as the test says.</summary>
file sealed class Advisor(Func<MoveQuestion, CancellationToken, Task<int?>> answer) : IMoveAdvising
{
    private readonly Func<MoveQuestion, CancellationToken, Task<int?>> _answer = answer;

    public Task<int?> Advise([Required] MoveQuestion question, CancellationToken cancellationToken) =>
        this._answer(question, cancellationToken);
}
