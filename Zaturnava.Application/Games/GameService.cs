using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// The use cases every host and client goes through. Each request replays the game from its stored
/// actions, applies what was asked, lets any bot seats that are next play, then stores the result.
/// </summary>
public sealed class GameService(
    IRulesetLocating rulesets,
    IBotLocating bots,
    IGamePersisting store,
    IGameNotifying notifier,
    GameServiceSettings settings
)
{
    private readonly IRulesetLocating _rulesets = rulesets;
    private readonly IBotLocating _bots = bots;
    private readonly IGamePersisting _store = store;
    private readonly IGameNotifying _notifier = notifier;
    private readonly GameServiceSettings _settings = settings;

    public ImmutableList<IRulesApplying> Rulesets => this._rulesets.All;

    public ImmutableList<string> BotIds => this._bots.BotIds;

    /// <exception cref="UnknownRulesetException">There is no such ruleset.</exception>
    /// <exception cref="InvalidGameSetupException">The plan does not give every seat exactly one plan.</exception>
    /// <exception cref="UnknownBotException">A plan names no known bot.</exception>
    public async Task<GameId> Create(
        [Required] string rulesetId,
        [Required] ImmutableList<SeatPlan> seats,
        CancellationToken cancellationToken
    )
    {
        IRulesApplying rules = this._rulesets.Find(rulesetId);
        GameState initial = rules.CreateInitialState();
        GameRecord created = new(GameId.New(), rules.Id, this.ControllersFor(initial, seats), [], 0);
        Played played = await this.PlayBots(rules, created, new Played(initial, []), cancellationToken);
        GameRecord record = created with { Actions = played.Actions };
        await this._store.Add(record, cancellationToken);
        return record.Id;
    }

    public Task<ImmutableList<GameRecord>> List(CancellationToken cancellationToken) =>
        this._store.List(cancellationToken);

    /// <exception cref="GameNotFoundException">There is no such game.</exception>
    public async Task<GameSnapshot> Get(GameId id, CancellationToken cancellationToken)
    {
        GameRecord record = await this.Load(id, cancellationToken);
        IRulesApplying rules = this._rulesets.Find(record.RulesetId);
        GameState state = Replay(rules, record.Actions);
        return GameSnapshot.Of(record, rules, state);
    }

    /// <summary>Claims an open seat for whoever holds the returned token.</summary>
    /// <exception cref="GameNotFoundException">There is no such game.</exception>
    /// <exception cref="SeatUnavailableException">The seat does not exist, or is not open.</exception>
    public async Task<string> Claim(GameId id, SeatId seat, CancellationToken cancellationToken)
    {
        GameRecord record = await this.Load(id, cancellationToken);
        bool isOpen = record.Controllers.ElementAtOrDefault(seat.Value)?.IsOpen == true;
        string token = isOpen
            ? SeatTokens.Issue()
            : throw new SeatUnavailableException(id, seat);
        HumanSeat claimed = new(SeatTokens.Hash(token));
        GameRecord updated = record.Next() with { Controllers = record.Controllers.SetItem(seat.Value, claimed) };
        await this.Save(updated, cancellationToken);
        return token;
    }

    /// <exception cref="GameNotFoundException">There is no such game.</exception>
    /// <exception cref="MalformedActionException">The descriptor describes no action.</exception>
    /// <exception cref="NotYourTurnException">The token does not hold the seat that must decide.</exception>
    /// <exception cref="IllegalActionException">The rules do not allow the action now.</exception>
    public async Task<GameSnapshot> Submit(
        GameId id,
        [Required] string token,
        [Required] ActionDescriptor descriptor,
        CancellationToken cancellationToken
    )
    {
        GameSnapshot snapshot = await this.Get(id, cancellationToken);
        GameAction action = snapshot.Rules.Parse(descriptor);
        Transition transition = snapshot.IsDecidedBy(action.Actor, token)
            ? snapshot.Rules.Apply(snapshot.State, action)
            : throw new NotYourTurnException(id, action.Actor);
        Played acted = new(transition.State, snapshot.Record.Actions.Add(action.Describe()));
        Played played = await this.PlayBots(snapshot.Rules, snapshot.Record, acted, cancellationToken);
        GameRecord updated = snapshot.Record.Next() with { Actions = played.Actions };
        await this.Save(updated, cancellationToken);
        return GameSnapshot.Of(updated, snapshot.Rules, played.State);
    }

    private static GameState Replay(IRulesApplying rules, ImmutableList<ActionDescriptor> actions) =>
        actions.Aggregate(rules.CreateInitialState(), (state, descriptor) => Replayed(rules, state, descriptor));

    private static GameState Replayed(IRulesApplying rules, GameState state, ActionDescriptor descriptor)
    {
        GameAction action = rules.Parse(descriptor);
        return rules.Apply(state, action).State;
    }

    private ImmutableList<SeatController> ControllersFor(GameState initial, ImmutableList<SeatPlan> seats) =>
        seats.Count == initial.Seats.Count
            ? [.. seats.Select(seat => seat.ToController(this._bots))]
            : throw new InvalidGameSetupException($"The game has {initial.Seats.Count} seats, not {seats.Count}.");

    private Task<Played> PlayBots(
        IRulesApplying rules,
        GameRecord record,
        Played played,
        CancellationToken cancellationToken
    ) =>
        DecisionLoop.PlayWhile(
            rules,
            played,
            seat => record.Controllers[seat.Value].DeciderFrom(this._bots),
            this._settings.MaximumBotSteps,
            cancellationToken
        );

    private async Task<GameRecord> Load(GameId id, CancellationToken cancellationToken) =>
        await this._store.Find(id, cancellationToken) ?? throw new GameNotFoundException(id);

    private async Task Save(GameRecord record, CancellationToken cancellationToken)
    {
        await this._store.Replace(record, cancellationToken);
        await this._notifier.GameChanged(record.Id, record.Version, cancellationToken);
    }
}
