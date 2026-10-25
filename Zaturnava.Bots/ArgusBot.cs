using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Watchful, like the giant of a hundred eyes: deals in prisoners as Solomon does, but on the board it
/// looks at the reply before it moves. It weighs each action by what it gains, less the worst any enemy
/// could do in return: take a piece left unguarded, or win an exchange, or step a leader onto its empty
/// throne; and it keeps out of the way of its allies' attacks. Still one move deep, and blind to the dice.
/// </summary>
public sealed class ArgusBot(Random random) : IBotPlaying
{
    public const string BotId = "argus";

    private readonly Random _random = random;

    public BotProfile Profile { get; } = new(
        BotId,
        "Argus",
        "Watchful: before it moves, it looks at what its opponents could take in reply, and keeps its pieces "
            + "guarded, its throne safe and its allies' lines of attack open. Trades prisoners as Solomon does. "
            + "A plain algorithm: no AI.",
        UsesAi: false
    );

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<GameAction>(cancellationToken)
            : Task.FromResult(
                SolomonBot.AnswerTo(request) ?? SolomonBot.FairTrade(request) ?? Careful.Choose(request, this._random)
            );
}
