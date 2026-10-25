using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// The use cases every host and client goes through. Each request replays the game from its stored
/// actions, applies what was asked, lets any bot seats that are next play (up to
/// <see cref="GameServiceSettings.MaximumBotSteps"/>), then stores the result. Bots left to play
/// take their turns one <see cref="Advance"/> at a time.
/// </summary>
public sealed class GameService(
    IRulesetLocating rulesets,
    IBotLocating bots,
    IGamePersisting store,
    IGameNotifying notifier,
    IDiceRolling dice,
    GameServiceSettings settings
)
{
    private readonly IRulesetLocating _rulesets = rulesets;
    private readonly IBotLocating _bots = bots;
    private readonly IGamePersisting _store = store;
    private readonly IGameNotifying _notifier = notifier;
    private readonly IDiceRolling _dice = dice;
    private readonly GameServiceSettings _settings = settings;

    public ImmutableList<IRulesApplying> Rulesets => this._rulesets.All;

    public ImmutableList<BotProfile> Bots => this._bots.Bots;

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
        return GameSnapshot.Of(record, this._rulesets.Find(record.RulesetId));
    }

    /// <summary>
    /// Lets the bot whose turn it is make one decision (rolling the dice it asks for), if the game is
    /// still at <paramref name="version"/>; otherwise, or when no bot is deciding, leaves it as it is.
    /// Clients call it to let bots play at a pace people can follow; the version keeps two watchers
    /// from moving one bot twice.
    /// </summary>
    /// <exception cref="GameNotFoundException">There is no such game.</exception>
    public async Task<GameSnapshot> Advance(GameId id, int version, CancellationToken cancellationToken)
    {
        GameSnapshot snapshot = await this.Get(id, cancellationToken);
        return snapshot.Record.Version == version
            ? await this.Advanced(snapshot, cancellationToken)
            : snapshot;
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
    /// <exception cref="IllegalActionException">
    /// The rules do not allow the action now, or it is how dice came up, which only chance decides.
    /// </exception>
    public async Task<GameSnapshot> Submit(
        GameId id,
        [Required] string token,
        [Required] ActionDescriptor descriptor,
        CancellationToken cancellationToken
    )
    {
        GameSnapshot snapshot = await this.Get(id, cancellationToken);
        GameAction action = Chosen(snapshot.Rules.Parse(descriptor));
        Transition transition = snapshot.IsDecidedBy(action.Actor, token)
            ? snapshot.Rules.Apply(snapshot.State, action)
            : throw new NotYourTurnException(id, action.Actor);
        Played acted = new(transition.State, snapshot.Record.Actions.Add(action.Describe()));
        Played played = await this.PlayBots(snapshot.Rules, snapshot.Record, acted, cancellationToken);
        GameRecord updated = snapshot.Record.Next() with { Actions = played.Actions };
        return await this.Saved(updated, snapshot.Rules, cancellationToken);
    }

    /// <exception cref="IllegalActionException">The action is how dice came up.</exception>
    private static GameAction Chosen(GameAction action) =>
        action is DiceAction
            ? throw new IllegalActionException(action)
            : action;

    private async Task<GameSnapshot> Advanced(GameSnapshot snapshot, CancellationToken cancellationToken)
    {
        Played played = await DecisionLoop.PlayWhile(
            snapshot.Rules,
            new Played(snapshot.State, snapshot.Record.Actions),
            this.DecidersOf(snapshot.Record),
            this._dice,
            1,
            cancellationToken
        );
        GameRecord updated = snapshot.Record.Next() with { Actions = played.Actions };
        return played.Actions.Count == snapshot.Record.Actions.Count
            ? snapshot
            : await this.Saved(updated, snapshot.Rules, cancellationToken);
    }

    private async Task<GameSnapshot> Saved(GameRecord record, IRulesApplying rules, CancellationToken cancellationToken)
    {
        await this.Save(record, cancellationToken);
        return GameSnapshot.Of(record, rules);
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
            this.DecidersOf(record),
            this._dice,
            this._settings.MaximumBotSteps,
            cancellationToken
        );

    /// <summary>Each seat's bot; null for a seat that is open or held by a person.</summary>
    private Func<SeatId, IDeciding?> DecidersOf(GameRecord record) =>
        seat => record.Controllers[seat.Value].DeciderFrom(this._bots);

    private async Task<GameRecord> Load(GameId id, CancellationToken cancellationToken) =>
        await this._store.Find(id, cancellationToken) ?? throw new GameNotFoundException(id);

    private async Task Save(GameRecord record, CancellationToken cancellationToken)
    {
        await this._store.Replace(record, cancellationToken);
        await this._notifier.GameChanged(record.Id, record.Version, cancellationToken);
    }
}
