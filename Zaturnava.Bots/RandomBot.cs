using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>Picks any action it may, uniformly at random: the baseline every smarter bot must beat.</summary>
public sealed class RandomBot(Random random) : IBotPlaying
{
    public const string BotId = "random";

    private readonly Random _random = random;

    public BotProfile Profile { get; } = new(
        BotId,
        "Random",
        "Picks any legal move, roll or pass, at random. A plain algorithm: no AI.",
        UsesAi: false
    );

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken)
    {
        ImmutableList<GameAction> choices = Playable.Of(request);
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<GameAction>(cancellationToken)
            : Task.FromResult(choices[this._random.Next(choices.Count)]);
    }
}
