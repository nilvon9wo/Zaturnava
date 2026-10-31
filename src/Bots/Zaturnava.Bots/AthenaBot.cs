using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Bots.Learning;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Sage's look ahead through the rest of its turn and the dice (<see cref="Foresight"/>), but with each position
/// judged by a neural network trained on games played (<see cref="NetworkValuing"/>): how the game will end for
/// its team, and by how much. Asked to end the game, it agrees when ending it now, points deciding, is worth as
/// much as playing on, as the network judges playing on. Deals in prisoners as Sage does, by the position a
/// trade leaves, as the network judges it (<see cref="ByPosition"/>). For a ruleset it
/// has no network for (or one of another size than the ruleset's positions), it plays as Sage does.
/// </summary>
public sealed class AthenaBot(Random random, ImmutableDictionary<string, ValueNetworkData> networks) : IBotPlaying
{
    public const string BotId = "athena";

    private readonly Random _random = random;
    private readonly ImmutableDictionary<string, ValueNetworkData> _networks = networks;
    private readonly ConcurrentDictionary<string, NetworkValuing?> _valuings = new(StringComparer.Ordinal);

    /// <summary>Athena with the networks trained so far.</summary>
    public AthenaBot(Random random)
        : this(random, ValueNetworks.Learned)
    {
    }

    public BotProfile Profile { get; } = new(
        BotId,
        "Athena",
        "Learned: a small neural network, trained on two thousand games of bots playing each other, judges "
            + "each position (who will win, and by how much), and it thinks through the rest of its turn as Sage "
            + "does. It agrees to end a game when ending it now is worth as much as playing on. Trades prisoners when "
            + "the board would be better for it. Uses AI of its own, run on the server at no cost to anyone: "
            + "no AI service.",
        UsesAi: true
    );

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<GameAction>(cancellationToken)
            : Task.FromResult(this.Chosen(request));

    private GameAction Chosen(DecisionRequest request)
    {
        NetworkValuing? learned = this.LearnedFor(request.Rules);
        IPositionValuing valuing = (IPositionValuing?)learned
            ?? new Appraiser(SageWeights.Learned.GetValueOrDefault(request.Rules.Id, SageWeights.Default));
        GameAction? ending = (request.State.Proposal, learned) switch
        {
            ({ }, { } judge) => new AnswerEndAction(request.Seat, judge.IsBetterEnded(request)),
            _ => Ending.AnswerTo(request),
        };
        return SageBot.Play(request, ending, valuing, this._random);
    }

    private NetworkValuing? LearnedFor(IRulesApplying rules) =>
        this._valuings.GetOrAdd(rules.Id, _ => this.Valuing(rules));

    private NetworkValuing? Valuing(IRulesApplying rules) =>
        this._networks.TryGetValue(rules.Id, out ValueNetworkData? data)
            ? Fitting(new PositionEncoder(rules), ValueNetwork.From(data))
            : null;

    /// <summary>The network's judgement, if it judges positions of the size the ruleset's are; else null.</summary>
    private static NetworkValuing? Fitting(PositionEncoder encoder, ValueNetwork network) =>
        network.Inputs == encoder.Size
            ? new NetworkValuing(encoder, network)
            : null;
}
