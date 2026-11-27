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
/// trade leaves, as the network judges it (<see cref="ByPosition"/>). It knows only the rulesets it has a network
/// for (<see cref="Knows"/>); asked to play another anyway, it plays as Sage does.
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
        "Learned. A small neural network, trained on thousands of games between bots, judges each position (who "
            + "will win, and by how much), and it plans the rest of its turn, dice included, by that judgement; "
            + "rarely random, but its choices can surprise, as it judges by experience rather than by rules of "
            + "thumb. Agrees to end a game when ending it now is worth as much to its team as playing on; trades "
            + "prisoners when the board would be better for it. As an ally: plays for the team's result, not its "
            + "own points. As an enemy: among the strongest, though it can still leave a piece to be taken. Uses "
            + "AI of its own, run on the server at no cost to anyone: no AI service. Offered only for games it has "
            + "a network for: so far, Chaturaji 2027.",
        UsesAi: true
    );

    /// <summary>
    /// Only a game it has a network for, that can judge the game's positions: at any other, it would play just as Sage
    /// does (D1).
    /// </summary>
    public bool Knows([Required] IRulesApplying rules) => this.LearnedFor(rules) is not null;

    /// <summary>A chance that gains its team points (<see cref="Chances"/>).</summary>
    public Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        Chances.Gainful(request, cancellationToken);

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<GameAction>(cancellationToken)
            : Task.FromResult(this.Chosen(request));

    private GameAction Chosen(DecisionRequest request)
    {
        NetworkValuing? learned = this.LearnedFor(request.Rules);
        IPositionValuing valuing = (IPositionValuing?)learned
            ?? new Appraiser(WeightsFor(request.Rules));
        GameAction? ending = (request.State.Proposal, learned) switch
        {
            ({ }, { } judge) => new AnswerEndAction(request.Seat, judge.IsBetterEnded(request)),
            _ => Ending.AnswerTo(request),
        };
        return SageBot.Play(request, ending, valuing, this._random);
    }

    private NetworkValuing? LearnedFor(IRulesApplying rules) =>
        this._valuings.GetOrAdd(rules.Id, _ => this.Valuing(rules));

    /// <summary>
    /// The network's judgement, steadied by Sage's (<see cref="NetworkValuing.Steadiness"/>), if it judges
    /// positions as the ruleset's can be given (<see cref="PositionEncoder.Fitting"/>); else null.
    /// </summary>
    private NetworkValuing? Valuing(IRulesApplying rules) =>
        this._networks.TryGetValue(rules.Id, out ValueNetworkData? data)
            && PositionEncoder.Fitting(rules, data.Inputs) is { } encoder
            ? new NetworkValuing(encoder, ValueNetwork.From(data), WeightsFor(rules))
            : null;

    private static SageWeights WeightsFor(IRulesApplying rules) =>
        SageWeights.Learned.GetValueOrDefault(rules.Id, SageWeights.Default);
}
