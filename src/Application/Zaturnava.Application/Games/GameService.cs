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
    GameServiceSettings settings,
    IPlayerPersisting players,
    GameGate gate
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
    private readonly IPlayerPersisting _players = players;
    private readonly GameGate _gate = gate;

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
    public Task<GameId> Create(
        [Required] string rulesetId,
        [Required] ImmutableList<SeatPlan> seats,
        string? scenarioId,
        CancellationToken cancellationToken
    ) =>
        this.Create(rulesetId, seats, scenarioId, Dealing.Anew, cancellationToken);

    /// <summary>
    /// A new game: from the scenario <paramref name="scenarioId"/> names, if any; else, if its ruleset deals its
    /// armies, dealt as <paramref name="dealing"/> asks.
    /// </summary>
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
        [Required] Dealing dealing,
        CancellationToken cancellationToken
    )
    {
        IRulesApplying rules = this._rulesets.Find(rulesetId);
        ImmutableList<ActionDescriptor> start = (scenarioId, rules.IsDealt) switch
        {
            ({ } scenario, _) => [ScenarioStart.Describe(this.ScenarioFor(rules, scenario))],
            (null, true) => await this.DealFor(
                rules,
                dealing with { Drafting = rules.IsDraftable ? Checked(dealing.Drafting, seats.Count) : null },
                cancellationToken
            ),
            _ => [],
        };
        GameState initial = start.Skip(1).Aggregate(
            ScenarioStart.StateOf(start, rules),
            (state, action) => rules.Apply(state, rules.Parse(action)).State
        );
        GameRecord created = new(GameId.New(), rules.Id, this.ControllersFor(initial, seats), start, 0);
        Played played = await this.PlayBots(rules, created, new Played(initial, start), cancellationToken);
        DateTimeOffset now = this._clock.Now;
        GameRecord record = Summarized(
            created with { Actions = played.Actions, Created = now, LastChanged = now },
            played.State
        );
        await this._store.Add(record, cancellationToken);
        return record.Id;
    }

    /// <summary>
    /// How a game begins dealt: as the game <see cref="Dealing.ArmiesOf"/> names began, if it was dealt, so that it is
    /// dealt by the same seed, the same armies again (played again, of the same ruleset), its draft too, if its armies
    /// were drafted, so that they stand as they were drafted; else anew, its armies alike or drafted as asked.
    /// </summary>
    private async Task<ImmutableList<ActionDescriptor>> DealFor(
        IRulesApplying rules,
        Dealing dealing,
        CancellationToken cancellationToken
    )
    {
        GameRecord? previous = dealing.ArmiesOf is { } id ? await this._store.Find(id, cancellationToken) : null;
        return previous is { Actions: [{ Kind: ScenarioStart.DealKind } first, ..] }
            ? [first, .. DraftOf(previous.Actions, rules)]
            : [ScenarioStart.Dealt(this._dice, dealing.AreAlike, dealing.Drafting)];
    }

    /// <summary>The draft as set, if its budget is something to spend and its drafter, if any, a seat.</summary>
    /// <exception cref="InvalidGameSetupException">It is not.</exception>
    private static DraftSettings? Checked(DraftSettings? drafting, int seats) =>
        drafting switch
        {
            { Budget: < 1 } => throw new InvalidGameSetupException("A draft's budget is at least 1."),
            { Drafter: { } drafter } when drafter.Value < 0 || drafter.Value >= seats =>
                throw new InvalidGameSetupException($"The game has no seat {drafter.Value} to draft."),
            _ => drafting,
        };

    /// <summary>
    /// The actions that drafted the armies of a game that began with <paramref name="actions"/>, as many as it took
    /// before play began; none, if they were not drafted.
    /// </summary>
    private static ImmutableList<ActionDescriptor> DraftOf(
        ImmutableList<ActionDescriptor> actions,
        IRulesApplying rules
    )
    {
        GameState state = ScenarioStart.StateOf(actions, rules);
        int drafted = 1;
        while (drafted < actions.Count && state.Draft is not null)
        {
            state = rules.Apply(state, rules.Parse(actions[drafted])).State;
            drafted++;
        }

        return actions.GetRange(1, drafted - 1);
    }

    /// <summary>
    /// The page of games <paramref name="query"/> asks for, of the rulesets still offered (of its game and ruleset, if
    /// it names them; previews or not, if it asks), as stored: none is replayed, however many there are.
    /// </summary>
    public Task<GamePage> Page([Required] GameQuery query, CancellationToken cancellationToken) =>
        this._store.Page(query.Bounded(), this.RulesetsAsked(query), cancellationToken);

    /// <summary>
    /// Every game <paramref name="query"/> asks for, as <see cref="Page"/> lists them but on no page: for filtering
    /// further, by what the store does not know.
    /// </summary>
    public async Task<ImmutableList<GameListing>> All([Required] GameQuery query, CancellationToken cancellationToken)
    {
        GameQuery unpaged = query with { Page = 1, PageSize = int.MaxValue };
        GamePage page = await this._store.Page(unpaged, this.RulesetsAsked(query), cancellationToken);
        return page.Games;
    }

    /// <summary>The rulesets still offered that <paramref name="query"/> asks for.</summary>
    private ImmutableHashSet<string> RulesetsAsked(GameQuery query) =>
    [
        .. this.Rulesets
            .Where(rules => query.GameId is null || rules.GameId == query.GameId)
            .Where(rules => query.RulesetId is null || rules.Id == query.RulesetId)
            .Where(rules => query.IsPreview is not { } preview || rules.IsPreview == preview)
            .Select(rules => rules.Id),
    ];

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

    /// <summary>
    /// Lets go of the games abandoned, not over and with nothing done: with a person at a seat who never signed in,
    /// for <see cref="GameServiceSettings.GuestGamesKept"/>; or waiting for a signed-in player to decide, for
    /// <see cref="GameServiceSettings.AwaitedGamesKept"/>, who has not signed in for as long. How many there were.
    /// </summary>
    public async Task<int> PurgeAbandoned(CancellationToken cancellationToken)
    {
        DateTimeOffset awaitedSince = this._clock.Now - this._settings.AwaitedGamesKept;
        ImmutableList<GameRecord> guests =
            await this._store.Idle(this._clock.Now - this._settings.GuestGamesKept, cancellationToken);
        ImmutableDictionary<string, IRulesApplying> offered = this.Rulesets.ToImmutableDictionary(rules => rules.Id);
        ImmutableDictionary<GameId, PlayerId> awaited = (await this._store.Idle(awaitedSince, cancellationToken))
            .Select(game => (game.Id, Player: AwaitedPlayer(game, offered.GetValueOrDefault(game.RulesetId))))
            .Where(entry => entry.Player is not null)
            .ToImmutableDictionary(entry => entry.Id, entry => entry.Player!.Value);
        ImmutableDictionary<PlayerId, DateTimeOffset> signedIn =
            await this._players.LastSignedIn([.. awaited.Values.Distinct()], cancellationToken);
        ImmutableList<GameId> abandoned =
        [
            .. guests.Where(game => game.Controllers.Any(controller => controller.IsGuest))
                .Select(game => game.Id)
                .Concat(
                    awaited
                        .Where(entry =>
                            signedIn.TryGetValue(entry.Value, out DateTimeOffset last) && last < awaitedSince
                        )
                        .Select(entry => entry.Key)
                )
                .Distinct(),
        ];
        await this._store.Remove(abandoned, cancellationToken);
        return abandoned.Count;
    }

    /// <summary>
    /// The games waiting for <paramref name="player"/> to decide, at a seat they play themselves, not by an AI, the one
    /// that changed earliest first, so that they may be reminded of them (Stated): each replayed, of a ruleset still
    /// offered.
    /// </summary>
    public async Task<ImmutableList<AwaitedGame>> Awaiting(PlayerId player, CancellationToken cancellationToken)
    {
        ImmutableList<GameRecord> claimed = await this._store.ClaimedBy(player, cancellationToken);
        ImmutableDictionary<string, IRulesApplying> offered = this.Rulesets.ToImmutableDictionary(rules => rules.Id);
        return
        [
            .. claimed
                .OrderBy(record => record.LastChanged)
                .ThenBy(record => record.Id.Value, StringComparer.Ordinal)
                .Select(record => AwaitedBy(record, offered.GetValueOrDefault(record.RulesetId), player))
                .OfType<AwaitedGame>(),
        ];
    }

    /// <summary>The game, if it waits for <paramref name="player"/> to decide, replayed by its rules.</summary>
    private static AwaitedGame? AwaitedBy(GameRecord record, IRulesApplying? rules, PlayerId player) =>
        Awaited(record, rules) is { } awaited && !awaited.Human.IsAi && player.Equals(awaited.Human.Claimant)
            ? new AwaitedGame(record, awaited.Seat)
            : null;

    /// <summary>The signed-in player the game waits for to decide, replayed by its rules, if still offered.</summary>
    private static PlayerId? AwaitedPlayer(GameRecord record, IRulesApplying? rules) =>
        Awaited(record, rules)?.Human.Claimant;

    /// <summary>The person's seat the game waits for to decide, replayed by its rules, if still offered.</summary>
    private static (SeatId Seat, HumanSeat Human)? Awaited(GameRecord record, IRulesApplying? rules) =>
        rules is not null
        && Replayable(record, rules)?.State.DecidingSeat is { } seat
        && record.Controllers[seat.Value] is HumanSeat human
            ? (seat, human)
            : null;

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
    /// <exception cref="GameUnreplayableException">Its rules no longer replay it.</exception>
    public async Task<GameSnapshot> Get(GameId id, CancellationToken cancellationToken)
    {
        GameRecord record = await this.Load(id, cancellationToken);
        GameSnapshot snapshot = GameSnapshot.TryOf(record, this._rulesets.Find(record.RulesetId))
            ?? throw new GameUnreplayableException(id);
        return this.Timed(snapshot);
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
        using IDisposable entered = await this._gate.Enter(id);
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
    /// played by an AI, if <paramref name="isAi"/>, by the <paramref name="nickname"/> it gives itself even for a
    /// player, so that one player's AIs may be told apart. The last open seat taken, the game begins, and the bots
    /// due to play first play.
    /// </summary>
    /// <exception cref="GameNotFoundException">There is no such game.</exception>
    /// <exception cref="SeatUnavailableException">The seat does not exist, or is not open.</exception>
    /// <exception cref="SeatForPlayersException">A guest claims a seat left only for players signed in.</exception>
    public async Task<string> Claim(
        GameId id,
        SeatId seat,
        PlayerId? claimant,
        string? nickname,
        bool isAi,
        CancellationToken cancellationToken
    )
    {
        using IDisposable entered = await this._gate.Enter(id);
        GameRecord record = await this.Load(id, cancellationToken);
        string token = (record.Controllers.ElementAtOrDefault(seat.Value), claimant) switch
        {
            (OpenSeat { IsForPlayers: true }, null) => throw new SeatForPlayersException(id, seat),
            (OpenSeat, _) => SeatTokens.Issue(),
            _ => throw new SeatUnavailableException(id, seat),
        };
        GameRecord next = record.Next();
        string? named = claimant is null || isAi ? Nicknames.Of(nickname) : null;
        HumanSeat claimed =
            new(SeatTokens.Hash(token), claimant, named, isAi, next.Version)
            {
                IsForPlayers = record.Controllers[seat.Value].IsForPlayers,
            };
        GameRecord seated = next with { Controllers = record.Controllers.SetItem(seat.Value, claimed) };
        GameSnapshot snapshot = GameSnapshot.Of(seated, this._rulesets.Find(seated.RulesetId));
        Played played = await this.PlayBots(
            snapshot.Rules,
            seated,
            new Played(snapshot.State, seated.Actions),
            cancellationToken
        );
        await this.Save(Summarized(seated with { Actions = played.Actions }, played.State), cancellationToken);
        return token;
    }

    /// <summary>
    /// Says anew who holds the seat <paramref name="token"/> holds: <paramref name="claimant"/>, if a player signed in
    /// since taking it, else a guest by <paramref name="nickname"/>, as a claim does (an AI by its nickname either
    /// way); whether an AI plays it stays as claimed. So a player who signs in, or chooses a nickname, after taking a
    /// seat is known by it there too.
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
        using IDisposable entered = await this._gate.Enter(id);
        GameRecord record = await this.Load(id, cancellationToken);
        HumanSeat held = record.Controllers.ElementAtOrDefault(seat.Value) is HumanSeat person && person.IsHeldBy(token)
            ? person
            : throw new NotYourSeatException(id, seat);
        HumanSeat identified = held with
        {
            Claimant = claimant,
            Nickname = claimant is null || held.IsAi ? Nicknames.Of(nickname) : null,
        };
        GameRecord next = record.Next() with { Controllers = record.Controllers.SetItem(seat.Value, identified) };
        await this.Save(
            identified.Claimant != held.Claimant && ChangesHands(record) ? next with { HasChangedHands = true } : next,
            cancellationToken
        );
    }

    /// <summary>
    /// Hands <paramref name="player"/> a new token for the seat they claimed signed in, wherever they claimed it from
    /// (Stated): the token the seat was held by holds it no more (Proposed). A seat their AI plays is the AI's, and
    /// stays so (Proposed).
    /// </summary>
    /// <exception cref="GameNotFoundException">There is no such game.</exception>
    /// <exception cref="NotYourSeatException">The player did not claim the seat, or their AI plays it.</exception>
    public async Task<string> TakeBack(GameId id, SeatId seat, PlayerId player, CancellationToken cancellationToken)
    {
        using IDisposable entered = await this._gate.Enter(id);
        GameRecord record = await this.Load(id, cancellationToken);
        HumanSeat held = record.Controllers.ElementAtOrDefault(seat.Value) is HumanSeat { IsAi: false } person
            && person.Claimant == player
                ? person
                : throw new NotYourSeatException(id, seat);
        string token = SeatTokens.Issue();
        await this.Save(
            record.Next() with
            {
                Controllers = record.Controllers.SetItem(seat.Value, held with { TokenHash = SeatTokens.Hash(token) }),
            },
            cancellationToken
        );
        return token;
    }

    /// <summary>
    /// Hands the seat a bot plays, for the rest of the game, and a token to hold it by, to <paramref name="claimant"/>,
    /// if a player signed in takes it over; else to a guest, by <paramref name="nickname"/>, if they give one, as a
    /// claim does (Stated: a person may take a seat over from a bot; a guest may, unless the game is for players
    /// signed in only). Only in a game not over, and only to a player holding no seat at it already, so that no one
    /// plays two seats by it (Proposed); which seats a guest holds, only their browser knows. Should the game wait
    /// for that seat to decide, whoever took it over now decides for it.
    /// </summary>
    /// <exception cref="GameNotFoundException">There is no such game.</exception>
    /// <exception cref="GameUnreplayableException">Its rules no longer replay it.</exception>
    /// <exception cref="SeatNotTakenOverException">
    /// No bot plays the seat, the game is over, or the player holds a seat at it.
    /// </exception>
    /// <exception cref="SeatForPlayersException">A guest asks for a seat only for players signed in.</exception>
    public async Task<string> TakeOver(
        GameId id,
        SeatId seat,
        PlayerId? claimant,
        string? nickname,
        CancellationToken cancellationToken
    )
    {
        using IDisposable entered = await this._gate.Enter(id);
        GameSnapshot snapshot = await this.Get(id, cancellationToken);
        GameRecord record = snapshot.Record;
        BotSeat bot = record.Controllers.ElementAtOrDefault(seat.Value) is BotSeat seated
            && snapshot.State.Outcome is null
            && !(claimant is { } player && record.IsClaimedBy(player))
                ? seated
                : throw new SeatNotTakenOverException(id, seat);
        if (bot.IsForPlayers && claimant is null)
        {
            throw new SeatForPlayersException(id, seat);
        }

        string token = SeatTokens.Issue();
        GameRecord next = record.Next();
        string? guest = claimant is null ? Nicknames.Of(nickname) : null;
        HumanSeat taken =
            new(SeatTokens.Hash(token), claimant, guest, Arrival: next.Version)
            {
                IsForPlayers = bot.IsForPlayers,
            };
        GameRecord reseated = next with
        {
            Controllers = record.Controllers.SetItem(seat.Value, taken),
            HasChangedHands = ChangesHands(record),
        };
        await this.Save(Summarized(reseated, snapshot.State), cancellationToken);
        return token;
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
        using IDisposable entered = await this._gate.Enter(id);
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
        using IDisposable entered = await this._gate.Enter(id);
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
                    record.Controllers.SetItem(
                        away.Absent.Value,
                        new BotSeat(bot) { IsForPlayers = record.Controllers[away.Absent.Value].IsForPlayers }
                    ),
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
        using IDisposable entered = await this._gate.Enter(id);
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
    private static GameSnapshot? Replayable(GameRecord record, IRulesApplying rules) =>
        GameSnapshot.TryOf(record, rules);

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
        GameRecord summarized = Summarized(record, snapshot.State);
        await this.Save(summarized, cancellationToken);
        return this.Timed(snapshot with { Record = summarized });
    }

    /// <summary>
    /// The record with what a lobby lists it by, kept so that it need not replay it: whether the game is over, and
    /// which seats a person (or no one yet) plays are away.
    /// </summary>
    private static GameRecord Summarized(GameRecord record, GameState state) =>
        record with
        {
            IsOver = state.Outcome is not null,
            Away = [.. state.Absence.Away.Where(seat => record.Controllers[seat.Value] is not BotSeat)],
        };

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
        AwayVoting? away = this.AwayVotingOf(snapshot, timer is null);
        return (timer, away) switch
        {
            ({ }, _) => snapshot with { Away = away, TimeLeft = this.TimeLeft(snapshot.Record, timer) },
            (_, { IsPassing: true }) => snapshot with { Away = away, TimeLeft = away.GraceLeft },
            _ => snapshot with { Away = away },
        };
    }

    /// <summary>
    /// The vote on waiting for a seat a person (or no one yet) plays, while other people still play: one whose turns
    /// have passed while it was away, until it says it is back, the deciding seat first, whoever decides and however
    /// (Proposed), so that the others may have a bot play for it on their own turns too; else the deciding seat, if no
    /// timer limits its decision.
    /// </summary>
    private AwayVoting? AwayVotingOf(GameSnapshot snapshot, bool isUntimed)
    {
        GameState state = snapshot.State;
        Absence absence = state.Absence;
        IEnumerable<SeatId> away = absence.Away.OrderBy(seat => seat != state.DecidingSeat);
        IEnumerable<SeatId> deciding = (isUntimed, state.DecidingSeat) is (true, { } decider) ? [decider] : [];
        return away
            .Concat(deciding)
            .Where(seat => snapshot.Record.Controllers[seat.Value] is not BotSeat)
            .Select(seat => this.VotingOn(snapshot, seat))
            .FirstOrDefault(voting => !voting.Voters.IsEmpty);
    }

    /// <summary>
    /// The vote on waiting for <paramref name="absent"/>; its grace runs from when this host first saw it so waited
    /// for.
    /// </summary>
    private AwayVoting VotingOn(GameSnapshot snapshot, SeatId absent)
    {
        GameState state = snapshot.State;
        GameRecord record = snapshot.Record;
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
        return new AwayVoting(absent, voters, absence.StoppingFor(absent), left)
        {
            FirstArrival = FirstArrivalOf(record),
            IsDeciding = state.DecidingSeat.Equals(absent),
            UntilAnyoneReplaces = this.Left(
                this._clock.AbsentSince(record.Id, AbsenceOf(record, absent))
                    + this._settings.AwayGrace
                    + this._settings.ReplaceWait
            ),
        };
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
                    controller.IsOpen && free.TryDequeue(out string? bot)
                        ? new BotSeat(bot) { IsForPlayers = controller.IsForPlayers }
                        : controller
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

    /// <summary>
    /// Whether a seat handed to someone else now changes hands, as <see cref="GameRecord.HasChangedHands"/> says: once
    /// the game has begun, not while it waits for its open seats. A game begun never waits again, as no seat reopens.
    /// </summary>
    private static bool ChangesHands(GameRecord record) => !record.IsWaiting;

    /// <summary>
    /// The game with its seats played by <paramref name="controllers"/>, and the bots due played; as changed hands, if
    /// it has begun (<see cref="ChangesHands"/>).
    /// </summary>
    private async Task<GameSnapshot> Reseated(
        GameSnapshot snapshot,
        ImmutableList<SeatController> controllers,
        CancellationToken cancellationToken
    )
    {
        GameRecord reseated = snapshot.Record.Next() with
        {
            Controllers = controllers,
            HasChangedHands = ChangesHands(snapshot.Record),
        };
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
            ? new AmongPeople(new FillingArmies(bot), people)
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
