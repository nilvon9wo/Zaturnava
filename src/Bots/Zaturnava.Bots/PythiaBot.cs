using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Sage, with a second opinion: it weighs the board and looks ahead as Sage does, then asks
/// <paramref name="advisor"/> (an AI service, say) which of the few moves it likes best to make. If the advisor
/// cannot answer, or takes longer than <paramref name="patience"/>, it makes Sage's own choice; so it plays
/// as well as Sage at worst, whatever the service does. Deals in prisoners as Sage does, asking no one.
/// </summary>
public sealed class PythiaBot(Random random, IMoveAdvising advisor, TimeSpan patience) : IBotPlaying
{
    public const string BotId = "pythia";

    /// <summary>How many of the moves it likes best it asks about.</summary>
    public const int Choices = 3;

    private const int Depth = 3;
    private const int Beam = 3;

    private readonly Random _random = random;
    private readonly IMoveAdvising _advisor = advisor;
    private readonly TimeSpan _patience = patience;

    public BotProfile Profile { get; } = new(
        BotId,
        "Pythia",
        "Sage with a second opinion: of the few moves Sage likes best, it asks a free AI service which to make, "
            + "and makes Sage's own choice if the service does not answer in time. Trades prisoners as Sage does. "
            + "Uses AI, at no cost to anyone.",
        UsesAi: true
    );

    public async Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Ending.AnswerTo(request)
            ?? SolomonBot.AnswerTo(request)
            ?? SolomonBot.FairTrade(request, false)
            ?? Trading.BeforePassing(request, await this.Choose(request, cancellationToken), SolomonBot.FairTrade);
    }

    private async Task<GameAction> Choose(DecisionRequest request, CancellationToken cancellationToken)
    {
        SageWeights weights = SageWeights.Learned.GetValueOrDefault(request.Rules.Id, SageWeights.Default);
        ImmutableList<GameAction> choices =
            [.. new Foresight(weights, Depth, Beam).Ranked(request, this._random).Take(Choices)];
        int? advised = choices.Count > 1
            ? await this.Advised(Consultation.Of(request, choices), cancellationToken)
            : null;
        return choices.ElementAtOrDefault(advised ?? -1) ?? choices[0];
    }

    /// <summary>The advisor's answer, or null if it has none within the bot's patience.</summary>
    private async Task<int?> Advised(MoveQuestion question, CancellationToken cancellationToken)
    {
        using CancellationTokenSource patience = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        patience.CancelAfter(this._patience);
        try
        {
            return await this._advisor.Advise(question, patience.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
