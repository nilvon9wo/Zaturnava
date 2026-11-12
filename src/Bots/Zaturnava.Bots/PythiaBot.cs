using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Bots.Learning;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Like the oracle at Delphi, sees by sampling what may come: Monte Carlo tree search (<see cref="MonteCarlo"/>),
/// trying each line of play many times over, the dice rolled afresh each time, and judging where each leads by
/// Athena's neural network (<see cref="NetworkValuing"/>), as AlphaZero judges by its own. Where Tactician looks at
/// every reply a few moves deep, Pythia spends its <paramref name="iterations"/> on the lines that look most worth
/// it, as deep as <paramref name="horizon"/> decisions, and on the dice as often as they are likely. Ends games as
/// Athena does, and deals in prisoners as Sage does, by the position a trade leaves. For a ruleset it has no
/// network for (or one that cannot judge the ruleset's positions), it judges positions as Sage does.
/// </summary>
public sealed class PythiaBot(
    Random random,
    int iterations,
    int horizon,
    ImmutableDictionary<string, ValueNetworkData> networks
) : IBotPlaying
{
    public const string BotId = "pythia";

    /// <summary>How many passes down the tree it makes for each decision (Proposed).</summary>
    public const int Iterations = 1000;

    /// <summary>How many decisions deep a pass goes before it judges where it stands (Proposed).</summary>
    public const int Horizon = 8;

    /// <summary>How much UCT favours trying the actions tried seldom (Proposed: √2, the textbook constant).</summary>
    private const double Exploration = 1.41;

    private readonly Random _random = random;
    private readonly int _iterations = iterations;
    private readonly int _horizon = horizon;
    private readonly ImmutableDictionary<string, ValueNetworkData> _networks = networks;
    private readonly ConcurrentDictionary<string, NetworkValuing?> _valuings = new(StringComparer.Ordinal);

    /// <summary>Pythia with the networks trained so far, looking as far and as often as it does by default.</summary>
    public PythiaBot(Random random)
        : this(random, Iterations, Horizon, ValueNetworks.Learned)
    {
    }

    public BotProfile Profile { get; } = new(
        BotId,
        "Pythia",
        "Oracular: Monte Carlo tree search, as AlphaZero searches. Plays each promising line out many times over, "
            + "rolling the dice afresh each time, and judges where each leads by Athena's neural network. Uses AI of "
            + "its own, run on the server at no cost to anyone: no AI service. Only Chaturaji 2027 has a network so "
            + "far; at any other game, it judges positions as Sage does.",
        UsesAi: true
    );

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
        MonteCarlo search = new(valuing, this._iterations, this._horizon, Exploration);
        return SageBot.Play(request, ending, valuing, deciding => search.Choose(deciding, this._random));
    }

    private NetworkValuing? LearnedFor(IRulesApplying rules) =>
        this._valuings.GetOrAdd(rules.Id, _ => this.Valuing(rules));

    /// <summary>The network's judgement, as Athena's, if it can judge the ruleset's positions; else null.</summary>
    private NetworkValuing? Valuing(IRulesApplying rules) =>
        this._networks.TryGetValue(rules.Id, out ValueNetworkData? data)
            && PositionEncoder.Fitting(rules, data.Inputs) is { } encoder
            ? new NetworkValuing(encoder, ValueNetwork.From(data), WeightsFor(rules))
            : null;

    private static SageWeights WeightsFor(IRulesApplying rules) =>
        SageWeights.Learned.GetValueOrDefault(rules.Id, SageWeights.Default);
}
