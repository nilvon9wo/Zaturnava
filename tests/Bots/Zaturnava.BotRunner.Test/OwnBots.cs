using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.BotRunner.Test;

/// <summary>A bot of someone's own, as a bot assembly offers one: made with a <see cref="Random"/>.</summary>
public sealed class SeededOwnBot(Random random) : IBotPlaying
{
    public const string BotId = "seeded-own";

    private readonly Random _random = random;

    public BotProfile Profile { get; } = new(BotId, "Seeded", "Any legal action, at random.", UsesAi: false);

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(request.LegalActions[this._random.Next(request.LegalActions.Count)]);
}

/// <summary>A bot of someone's own, made with nothing: it always takes the first legal action.</summary>
public sealed class PlainOwnBot : IBotPlaying
{
    public const string BotId = "plain-own";

    public BotProfile Profile { get; } = new(BotId, "Plain", "The first legal action.", UsesAi: false);

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(request.LegalActions[0]);
}

/// <summary>A bot no one can make without telling it something it cannot be told: passed over.</summary>
public sealed class NamedOwnBot(string name) : IBotPlaying
{
    public BotProfile Profile { get; } = new(name, name, "Never made.", UsesAi: false);

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(request.LegalActions[0]);
}
