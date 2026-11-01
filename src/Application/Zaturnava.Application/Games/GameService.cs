using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;
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
/// take their turns one <see cref="Advance"/> at a time, as do decisions whose time has run out.
/// </summary>
public sealed class GameService(
    IRulesetLocating rulesets,
    IBotLocating bots,
    IScenarioLocating scenarios,
    IGamePersisting store,
    IGameNotifying notifier,
    IDiceRolling dice,
    DecisionClock clock,
    GameServiceSettings settings
)
{
    private readonly IRulesetLocating _rulesets = rulesets;
    private readonly IBotLocating _bots = bots;
    private readonly IScenarioLocating _scenarios = scenarios;
    private readonly IGamePersisting _store = store;
    private readonly IGameNotifying _notifier = notifier;
    private readonly IDiceRolling _dice = dice;
    private readonly DecisionClock _clock = clock;
    private readonly GameServiceSettings _settings = settings;

    public ImmutableList<IRulesApplying> Rulesets => this._rulesets.All;

    public ImmutableList<BotProfile> Bots => this._bots.Bots;

    public ImmutableList<Scenario> Scenarios => this._scenarios.All;

    /// <exception cref="UnknownRulesetException">There is no such ruleset.</exception>
    /// <exception cref="InvalidGameSetupException">The plan does not give every seat exactly one plan.</exception>
    /// <exception cref="UnknownBotException">A plan names no known bot.</exception>
    public Task<GameId> Create(
        [Required] string rulesetId,
        [Required] ImmutableList<SeatPlan> seats,
        CancellationToken cancellationToken
    ) =>
        this.Create(rulesetId, seats, null, cancellationToken);

    /// <summary>A new game: from the scenario <paramref name="scenarioId"/> names, if any.</summary>
    /// <exception cref="UnknownRulesetException">There is no such ruleset.</exception>
    /// <exception cref="UnknownScenarioException">There is no such scenario.</exception>
    /// <exception cref="InvalidScenarioException">
    /// The scenario is for another ruleset, or names what the ruleset lacks.
    /// </exception>
    /// <exception cref="InvalidGameSetupException">The plan does not give every seat exactly one plan.</exception>
    /// <exception cref="UnknownBotException">A plan names no known bot.</exception>
    public async Task<GameId> Create(
        [Required] string rulesetId,
        [Required] ImmutableList<SeatPlan> seats,
        string? scenarioId,
        CancellationToken cancellationToken
    )
    {
        IRulesApplying rules = this._rulesets.Find(rulesetId);
        ImmutableList<ActionDescriptor> start = scenarioId is null
            ? []
            : [ScenarioStart.Describe(this.ScenarioFor(rules, scenarioId))];
        GameState initial = ScenarioStart.StateOf(start, rules);
        GameRecord created = new(GameId.New(), rules.Id, this.ControllersFor(initial, seats), start, 0);
        Played played = await this.PlayBots(rules, created, new Played(initial, start), cancellationToken);
        GameRecord record = created with
        {
            Actions = played.Actions,
            IsOver = played.State.Outcome is not null,
            LastChanged = this._clock.Now,
        };
        await this._store.Add(record, cancellationToken);
        return record.Id;
    }

    /// <summary>
    /// The page of games <paramref name="query"/> asks for, of the rulesets still offered, as stored: none is
    /// replayed, however many there are.
    /// </summary>
    public Task<GamePage> Page([Required] GameQuery query, CancellationToken cancellationToken)
    {
        ImmutableHashSet<string> offered = [.. this.Rulesets.Select(rules => rules.Id)];
        ImmutableHashSet<string> asked = query.RulesetId is { } id ? offered.Intersect([id]) : offered;
        return this._store.Page(query.Bounded(), asked, cancellationToken);
    }

    /// <summary>
    /// Works out, once, whether each game saved before that was kept is over, by replaying it; a game of a ruleset
    /// no longer offered, or one its rules no longer replay, can no longer be played, so counts as over.
    /// </summary>
    public async Task Summarize(CancellationToken cancellationToken)
    {
        ImmutableDictionary<string, IRulesApplying> offered = this.Rulesets.ToImmutableDictionary(rules => rules.Id);
        foreach (GameRecord record in await this._store.Unsummarized(cancellationToken))
        {
            GameSnapshot? snapshot = offered.TryGetValue(record.RulesetId, out IRulesApplying? rules)
                ? Replayable(record, rules)
                : null;
            bool isOver = snapshot is null || snapshot.State.Outcome is not null;
            await this._store.MarkOver(record.Id, isOver, cancellationToken);
        }
    }

    /// <summary>Every stored game of a ruleset still offered, replayed, as a lobby lists them.</summary>
    public async Task<ImmutableList<GameSnapshot>> Snapshots(CancellationToken cancellationToken)
    {
        ImmutableList<GameRecord> records = await this._store.List(cancellationToken);
        ImmutableDictionary<string, IRulesApplying> offered = this.Rulesets.ToImmutableDictionary(rules => rules.Id);
        return
        [
            .. records
                .Where(record => offered.ContainsKey(record.RulesetId))
                .Select(record => Replayable(record, offered[record.RulesetId]))
                .OfType<GameSnapshot>(),
        ];
    }

    /// <exception cref="GameNotFoundException">There is no such game.</exception>
    public async Task<GameSnapshot> Get(GameId id, CancellationToken cancellationToken)
    {
        GameRecord record = await this.Load(id, cancellationToken);
        return this.Timed(GameSnapshot.Of(record, this._rulesets.Find(record.RulesetId)));
    }

    /// <summary>
    /// Decides for the deciding seat what its timer decides once its time has run out; otherwise lets
    /// the bot whose turn it is make one decision (rolling the dice it asks for). Either only if the
    /// game is still at <paramref name="version"/>; otherwise, or when neither is due, leaves it as it
    /// is. Clients call it to let bots play at a pace people can follow, and once a timer they show
    /// has run out; the version keeps two watchers from moving one bot twice.
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
    public Task<string> Claim(GameId id, SeatId seat, CancellationToken cancellationToken) =>
        this.Claim(id, seat, null, null, cancellationToken);

    /// <summary>Claims an open seat, for a person, as the other overload does.</summary>
    /// <exception cref="GameNotFoundException">There is no such game.</exception>
    /// <exception cref="SeatUnavailableException">The seat does not exist, or is not open.</exception>
    public Task<string> Claim(
        GameId id,
        SeatId seat,
        PlayerId? claimant,
        string? nickname,
        CancellationToken cancellationToken
    ) =>
        this.Claim(id, seat, claimant, nickname, false, cancellationToken);

    /// <summary>
    /// Claims an open seat for whoever holds the returned token: <paramref name="claimant"/>, if a player signed in
    /// claims it; else a guest, by <paramref name="nickname"/>, if they give one (<see cref="Nicknames.Of"/>);
    /// played by an AI, if <paramref name="isAi"/>. The last open seat taken, the game begins, and the bots due to
    /// play first play.
    /// </summary>
    /// <exception cref="GameNotFoundException">There is no such game.</exception>
    /// <exception cref="SeatUnavailableException">The seat does not exist, or is not open.</exception>
    public async Task<string> Claim(
        GameId id,
        SeatId seat,
        PlayerId? claimant,
        string? nickname,
        bool isAi,
        CancellationToken cancellationToken
    )
    {
        GameRecord record = await this.Load(id, cancellationToken);
        bool isOpen = record.Controllers.ElementAtOrDefault(seat.Value)?.IsOpen == true;
        string token = isOpen
            ? SeatTokens.Issue()
            : throw new SeatUnavailableException(id, seat);
        GameRecord next = record.Next();
        HumanSeat claimed =
            new(SeatTokens.Hash(token), claimant, claimant is null ? Nicknames.Of(nickname) : null, isAi, next.Version);
        GameRecord seated = next with { Controllers = record.Controllers.SetItem(seat.Value, claimed) };
        GameSnapshot snapshot = GameSnapshot.Of(seated, this._rulesets.Find(seated.RulesetId));
        Played played = await this.PlayBots(
            snapshot.Rules,
            seated,
            new Played(snapshot.State, seated.Actions),
            cancellationToken
        );
        await this.Save(
            seated with { Actions = played.Actions, IsOver = played.State.Outcome is not null },
            cancellationToken
        );
        return token;
    }

    /// <summary>
    /// Says anew who holds the seat <paramref name="token"/> holds: <paramref name="claimant"/>, if a player signed in
    /// since taking it, else a guest by <paramref name="nickname"/>, as a claim does; whether an AI plays it stays as
    /// claimed. So a player who signs in, or chooses a nickname, after taking a seat is known by it there too.
    /// </summary>
    /// <exception cref="GameNotFoundException">There is no such game.</exception>
    /// <exception cref="NotYourSeatException">The token does not hold the seat.</exception>
    public async Task Identify(
        GameId id,
        SeatId seat,
        [Required] string token,
        PlayerId? claimant,
        string? nickname,
        CancellationToken cancellationToken
    )
    {
        GameRecord record = await this.Load(id, cancellationToken);
        HumanSeat held = record.Controllers.ElementAtOrDefault(seat.Value) is HumanSeat person && person.IsHeldBy(token)
            ? person
            : throw new NotYourSeatException(id, seat);
        HumanSeat identified = held with
        {
            Claimant = claimant,
            Nickname = claimant is null ? Nicknames.Of(nickname) : null,
        };
        await this.Save(
            record.Next() with { Controllers = record.Controllers.SetItem(seat.Value, identified) },
            cancellationToken
        );
    }

    /// <summary>
    /// Fills the open seats of a game waiting for people with bots, for the person whose seat <paramref name="token"/>
    /// holds, if they may now (<see cref="Vacancy"/>): each with a bot not already seated, at random, never one paid
    /// for each use; a seat no bot is left for stays open. The game begins, if no seat is left open, and the bots due
    /// to play first play. A game no longer waiting is left as it is.
    /// </summary>
    /// <exception cref="GameNotFoundException">There is no such game.</exception>
    /// <exception cref="NotAPlayerException">The token holds no seat at the game.</exception>
    /// <exception cref="TooSoonToFillException">The seat holder may not fill the open seats yet.</exception>
    public async Task<GameSnapshot> FillOpenSeats(
        GameId id,
        [Required] string token,
        CancellationToken cancellationToken
    )
    {
        GameSnapshot snapshot = await this.Get(id, cancellationToken);
        GameRecord record = snapshot.Record;
        int held = record.Controllers.FindIndex(controller => controller.IsHeldBy(token));
        return (held, snapshot.Vacancy) switch
        {
            (-1, _) => throw new NotAPlayerException(id),
            (_, null) => snapshot,
            (_, { } vacancy) when !vacancy.MayFill(new SeatId(held)) => throw new TooSoonToFillException(id),
            _ => await this.Filled(snapshot, cancellationToken),
        };
    }

    /// <summary>
    /// Has a bot take the seat of the person the game waits for, away, for the rest of the game, for the person whose
    /// seat <paramref name="token"/> holds, if they may now (<see cref="AwayVoting.MayReplace"/>): a bot not already
    /// seated, at random, never one paid for each use. The bot then plays at once. With no bot left to seat, the game
    /// is left as it is.
    /// </summary>
    /// <exception cref="GameNotFoundException">There is no such game.</exception>
    /// <exception cref="NotAPlayerException">The token holds no seat at the game.</exception>
    /// <exception cref="TooSoonToReplaceException">The seat holder may not replace anyone now.</exception>
    public async Task<GameSnapshot> ReplaceAway(
        GameId id,
        [Required] string token,
        CancellationToken cancellationToken
    )
    {
        GameSnapshot snapshot = await this.Get(id, cancellationToken);
        GameRecord record = snapshot.Record;
        int held = record.Controllers.FindIndex(controller => controller.IsHeldBy(token));
        Queue<string> free = this.FreeBots(record);
        return (held, snapshot.Away) switch
        {
            (-1, _) => throw new NotAPlayerException(id),
            (_, { } away) when away.MayReplace(new SeatId(held)) && free.TryDequeue(out string? bot) =>
                await this.Reseated(
                    snapshot,
                    record.Controllers.SetItem(away.Absent.Value, new BotSeat(bot)),
                    cancellationToken
                ),
            (_, { } away) when away.MayReplace(new SeatId(held)) => snapshot,
            _ => throw new TooSoonToReplaceException(id),
        };
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
        Transition transition = snapshot.MayAct(action, token)
            ? snapshot.Rules.Apply(snapshot.State, action)
            : throw new NotYourTurnException(id, action.Actor);
        Played acted = new(transition.State, snapshot.Record.Actions.Add(action.Describe()));
        Played played = await this.PlayBots(snapshot.Rules, snapshot.Record, acted, cancellationToken);
        GameRecord updated = snapshot.Record.Next() with { Actions = played.Actions };
        return await this.Saved(updated, snapshot.Rules, cancellationToken);
    }

    /// <summary>
    /// The game replayed; none, if its rules have changed since so that its actions no longer replay, so that one such
    /// game does not keep every other from being listed.
    /// </summary>
    private static GameSnapshot? Replayable(GameRecord record, IRulesApplying rules)
    {
        try
        {
            return GameSnapshot.Of(record, rules);
        }
        catch (IllegalActionException)
        {
            return null;
        }
    }

    /// <exception cref="IllegalActionException">
    /// The action is how dice came up, or passing for a seat away, which only the host decides.
    /// </exception>
    private static GameAction Chosen(GameAction action) =>
        action is DiceAction or PassAwayAction
            ? throw new IllegalActionException(action)
            : action;

    private async Task<GameSnapshot> Advanced(GameSnapshot snapshot, CancellationToken cancellationToken)
    {
        Played start = new(snapshot.State, snapshot.Record.Actions);
        Played played = snapshot.TimeLeft == TimeSpan.Zero
            ? TimedOut(snapshot, start)
            : await DecisionLoop.PlayWhile(
                snapshot.Rules,
                start,
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

    /// <summary>
    /// The deciding seat's time is up: its timer decides for it; or, if no timer does, it was away, and the
    /// others have stopped waiting, so it passes.
    /// </summary>
    private static Played TimedOut(GameSnapshot snapshot, Played played)
    {
        GameAction action = snapshot.Rules.TimerOf(played.State)?.Default
            ?? new PassAwayAction(played.State.DecidingSeat.GetValueOrDefault()); // only a seat deciding times out
        Transition transition = snapshot.Rules.Apply(played.State, action);
        return new Played(transition.State, played.Actions.Add(action.Describe()));
    }

    private async Task<GameSnapshot> Saved(GameRecord record, IRulesApplying rules, CancellationToken cancellationToken)
    {
        GameSnapshot snapshot = GameSnapshot.Of(record, rules);
        GameRecord summarized = record with { IsOver = snapshot.State.Outcome is not null };
        await this.Save(summarized, cancellationToken);
        return this.Timed(snapshot with { Record = summarized });
    }

    /// <summary>
    /// The snapshot with the time its deciding seat has left, if its decision is timed; else, with who may
    /// vote to stop waiting for it, and, once they all would, the time it has left before it passes.
    /// </summary>
    private GameSnapshot Timed(GameSnapshot snapshot)
    {
        if (snapshot.Record.IsWaiting)
        {
            return snapshot with { Vacancy = this.VacancyOf(snapshot.Record) };
        }

        DecisionTimer? timer = snapshot.Rules.TimerOf(snapshot.State);
        AwayVoting? away = timer is null ? this.AwayVotingOf(snapshot) : null;
        return (timer, away) switch
        {
            ({ }, _) => snapshot with { TimeLeft = this.TimeLeft(snapshot.Record, timer) },
            (_, { IsAgreed: true }) => snapshot with { Away = away, TimeLeft = away.GraceLeft },
            _ => snapshot with { Away = away },
        };
    }

    /// <summary>
    /// The vote on waiting for the deciding seat, if a person (or no one yet) plays it and other people
    /// still play; its grace runs from when this host first saw it so waited for.
    /// </summary>
    private AwayVoting? AwayVotingOf(GameSnapshot snapshot)
    {
        GameState state = snapshot.State;
        GameRecord record = snapshot.Record;
        SeatId absent = state.DecidingSeat.GetValueOrDefault();
        ImmutableList<SeatId> voters =
        [
            .. state.Seats
                .Select(seat => seat.Id)
                .Where(seat => record.Controllers[seat.Value] is HumanSeat)
                .Where(seat => snapshot.Rules.MayVoteAway(state, seat, absent)),
        ];
        Absence absence = state.Absence;
        bool isReturning = absence.Returning.Contains(absent);
        bool hasGrace = absence.MissedBy(absent) == 0 || isReturning;
        // whom the game waits on, for whom, on which turn, and how far it has waited on them already
        string wait = string.Create(
            CultureInfo.InvariantCulture,
            $"{state.TurnNumber}/{absent.Value}/{state.ActingFor.GetValueOrDefault(absent).Value}/"
                + $"{absence.MissedBy(absent)}/{isReturning}"
        );
        TimeSpan grace = hasGrace ? this._settings.AwayGrace : TimeSpan.Zero;
        DateTimeOffset end = this._clock.WaitingSince(record.Id, wait) + grace;
        TimeSpan left = TimeSpan.FromTicks(Math.Max(0, (end - this._clock.Now).Ticks));
        bool isWaited = state.DecidingSeat is not null
            && record.Controllers[absent.Value] is not BotSeat
            && !voters.IsEmpty;
        return isWaited
            ? new AwayVoting(absent, voters, absence.StoppingFor(absent), left)
            {
                FirstArrival = FirstArrivalOf(record),
                UntilAnyoneReplaces = this.Left(
                    this._clock.AbsentSince(record.Id, AbsenceOf(record, absent))
                        + this._settings.AwayGrace
                        + this._settings.ReplaceWait
                ),
            }
            : null;
    }

    /// <summary>
    /// The seat away, and the last of the game's actions it took itself (not the host passing for it): the same until
    /// it acts again, however many turns pass meanwhile.
    /// </summary>
    private static string AbsenceOf(GameRecord record, SeatId absent)
    {
        string actor = absent.Value.ToString(CultureInfo.InvariantCulture);
        int last = record.Actions.FindLastIndex(action =>
            action.Kind != PassAwayAction.Kind
            && action.Arguments.GetValueOrDefault(ActionDescriptor.ActorArgument) == actor
        );
        return string.Create(CultureInfo.InvariantCulture, $"{absent.Value}/{last}");
    }

    /// <summary>
    /// The first of the people seated to arrive (the lowest seat, of those arrived together); none, if no one is.
    /// </summary>
    private static SeatId? FirstArrivalOf(GameRecord record) =>
        record.Controllers
            .Select((controller, seat) => (controller, seat))
            .Where(held => held.controller is HumanSeat)
            .OrderBy(held => ((HumanSeat)held.controller).Arrival)
            .Select(held => (SeatId?)new SeatId(held.seat))
            .FirstOrDefault();

    /// <summary>
    /// Who may fill the open seats of a game waiting for them, and when: the first of the people seated to
    /// arrive (the lowest seat, of those arrived together), once the game has waited
    /// <see cref="GameServiceSettings.FillWait"/> since it last changed, as this host has seen; anyone seated,
    /// after twice as long.
    /// </summary>
    private Vacancy VacancyOf(GameRecord record)
    {
        DateTimeOffset firstMay = this._clock.Since(record.Id, record.Version) + this._settings.FillWait;
        return new Vacancy(FirstArrivalOf(record), this.Left(firstMay), this.Left(firstMay + this._settings.FillWait));
    }

    /// <summary>
    /// The game with its open seats filled with bots not yet seated, at random, never one paid for each use.
    /// </summary>
    private Task<GameSnapshot> Filled(GameSnapshot snapshot, CancellationToken cancellationToken)
    {
        Queue<string> free = this.FreeBots(snapshot.Record);
        return this.Reseated(
            snapshot,
            [
                .. snapshot.Record.Controllers.Select(controller =>
                    controller.IsOpen && free.TryDequeue(out string? bot) ? new BotSeat(bot) : controller
                ),
            ],
            cancellationToken
        );
    }

    /// <summary>The bots not yet seated at the game, at random, never one paid for each use.</summary>
    private Queue<string> FreeBots(GameRecord record) =>
        new(
            this._bots.Bots
                .Where(bot => !bot.IsPaid)
                .Select(bot => bot.Id)
                .Except(record.Controllers.OfType<BotSeat>().Select(seated => seated.BotId))
                .OrderBy(_ => Random.Shared.Next())
        );

    /// <summary>The game with its seats played by <paramref name="controllers"/>, and the bots due played.</summary>
    private async Task<GameSnapshot> Reseated(
        GameSnapshot snapshot,
        ImmutableList<SeatController> controllers,
        CancellationToken cancellationToken
    )
    {
        GameRecord reseated = snapshot.Record.Next() with { Controllers = controllers };
        Played played = await this.PlayBots(
            snapshot.Rules,
            reseated,
            new Played(snapshot.State, reseated.Actions),
            cancellationToken
        );
        return await this.Saved(reseated with { Actions = played.Actions }, snapshot.Rules, cancellationToken);
    }

    /// <summary>How long from now until <paramref name="end"/>; none, once it has come.</summary>
    private TimeSpan Left(DateTimeOffset end) => TimeSpan.FromTicks(Math.Max(0, (end - this._clock.Now).Ticks));

    private TimeSpan TimeLeft(GameRecord record, DecisionTimer timer)
    {
        DateTimeOffset deadline = this._clock.Since(record.Id, record.Version) + this._settings.LengthOf(timer.Name);
        return TimeSpan.FromTicks(Math.Max(0, (deadline - this._clock.Now).Ticks));
    }

    private Scenario ScenarioFor(IRulesApplying rules, string scenarioId)
    {
        Scenario scenario = this._scenarios.Find(scenarioId);
        return scenario.RulesetId == rules.Id
            ? scenario
            : throw new InvalidScenarioException(
                $"The '{scenarioId}' scenario is for the '{scenario.RulesetId}' ruleset, not '{rules.Id}'."
            );
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

    /// <summary>
    /// Each seat's bot, told which seats people hold; null for a seat that is open or held by a person, and for
    /// every seat while the game waits for its seats to be taken.
    /// </summary>
    private Func<SeatId, IDeciding?> DecidersOf(GameRecord record)
    {
        if (record.IsWaiting)
        {
            return _ => null;
        }

        ImmutableHashSet<SeatId> people =
        [
            .. record.Controllers
                .Select((controller, seat) => (controller, seat))
                .Where(held => held.controller is HumanSeat)
                .Select(held => new SeatId(held.seat)),
        ];
        return seat => record.Controllers[seat.Value].DeciderFrom(this._bots) is { } bot
            ? new AmongPeople(bot, people)
            : null;
    }

    private async Task<GameRecord> Load(GameId id, CancellationToken cancellationToken) =>
        await this._store.Find(id, cancellationToken) ?? throw new GameNotFoundException(id);

    private async Task Save(GameRecord record, CancellationToken cancellationToken)
    {
        await this._store.Replace(record with { LastChanged = this._clock.Now }, cancellationToken);
        await this._notifier.GameChanged(record.Id, record.Version, cancellationToken);
    }
}
