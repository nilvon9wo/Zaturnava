using System.Collections.Immutable;
using Microsoft.AspNetCore.Http.HttpResults;
using Net.NowhereAtAll.Zaturnava.Application.Chat;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Endpoints;

/// <summary>
/// The routes of <see cref="GameRoutes"/>, each one call of the <see cref="GameService"/> between
/// translations to and from the contracts. A refusal is thrown, and answered by
/// <see cref="RefusalHandler"/>.
/// </summary>
internal static class GameEndpoints
{
    private const string GameRoute = GameRoutes.Games + "/{gameId}";

    public static IEndpointRouteBuilder MapGames(this IEndpointRouteBuilder routes)
    {
        _ = routes.MapGet(GameRoutes.Catalog, Catalog);
        _ = routes.MapGet(GameRoutes.Games, List);
        _ = routes.MapGet(GameRoutes.Leaderboards + "/{game}", Leaderboard);
        _ = routes.MapGet(GameRoutes.Leaderboards + "/{game}/" + GameRoutes.AgainstBotsSegment, AgainstBotsLeaderboard);
        _ = routes.MapGet(GameRoutes.Leaderboards + "/{game}/" + GameRoutes.BotsSegment, BotLeaderboard);
        _ = routes.MapPost(GameRoutes.Games, Create).RequireRateLimiting(RequestLimits.NewGames);
        _ = routes.MapGet(GameRoute, Get);
        _ = routes.MapGet(GameRoute + "/history", History);
        _ = routes.MapGet(GameRoutes.Histories, Histories);
        _ = routes.MapPost(GameRoute + "/seats/{seat:int}", Claim);
        _ = routes.MapPut(GameRoute + "/seats/{seat:int}", Identify);
        _ = routes.MapPost(GameRoute + "/seats/{seat:int}/take-back", TakeBack);
        _ = routes.MapPost(GameRoute + "/seats/{seat:int}/take-over", TakeOver);
        _ = routes.MapPost(GameRoute + "/actions", Submit);
        _ = routes.MapPost(GameRoute + "/advance", Advance);
        _ = routes.MapPost(GameRoute + "/fill", Fill);
        _ = routes.MapPost(GameRoute + "/replace", Replace);
        _ = routes.MapGet(GameRoute + "/chat", Chat);
        _ = routes.MapPost(GameRoute + "/chat", Say);
        return routes;
    }

    private static CatalogDto Catalog(GameService games) =>
        GameViews.CatalogOf(games.Rulesets, games.Bots, games.Scenarios);

    /// <summary>
    /// A page of the games asked for, read as stored: none is replayed. Games asked for by who is at them are all
    /// read, to be matched by what their players are called, or by the player's id, before the page is cut.
    /// </summary>
    private static async Task<GamePageDto> List(
        [AsParameters] GameListQuery asked,
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        GameQuery query = new GameQuery(
            asked.Ruleset,
            asked.Open,
            asked.PeopleOnly,
            asked.Over,
            asked.Page ?? 1,
            asked.PageSize ?? GameQuery.DefaultPageSize,
            asked.Game,
            asked.Preview
        ).Bounded();
        ImmutableList<string> wanted = PlayerPatterns.Of(asked.Players);
        GamePage page = wanted.IsEmpty && asked.Player is null
            ? await games.Page(query, cancellationToken)
            : await Matching(query, wanted, asked.Player, games, players, cancellationToken);
        ImmutableDictionary<HumanSeat, string> names = await players.NamesOf(
            page.Games.SelectMany(game => game.Controllers),
            cancellationToken
        );
        ImmutableDictionary<string, IRulesApplying> rulesets = games.Rulesets.ToImmutableDictionary(rules => rules.Id);
        return new GamePageDto(
            [.. page.Games.Select(game => GameViews.SummaryOf(game, rulesets[game.RulesetId], names))],
            page.Total,
            query.Page,
            query.PageSize
        );
    }

    /// <summary>The players standing highest at the game, each in their place, their Elo rounded.</summary>
    private static async Task<ImmutableList<StandingDto>> Leaderboard(
        string game,
        StandingService standings,
        CancellationToken cancellationToken
    ) => [.. (await standings.Leaders(game, cancellationToken)).Select(ToStandingDto)];

    /// <summary>The players who have done best at the game against bots alone, each in their place.</summary>
    private static async Task<ImmutableList<StandingDto>> AgainstBotsLeaderboard(
        string game,
        StandingService standings,
        CancellationToken cancellationToken
    ) => [.. (await standings.LeadersAgainstBots(game, cancellationToken)).Select(ToStandingDto)];

    /// <summary>The server's bots standing highest at the game, each in its place.</summary>
    private static async Task<ImmutableList<BotStandingDto>> BotLeaderboard(
        string game,
        StandingService standings,
        CancellationToken cancellationToken
    ) =>
    [
        .. (await standings.BotLeaders(game, cancellationToken)).Select((leader, index) =>
            new BotStandingDto(
                index + 1,
                leader.Name,
                leader.Standing.Points,
                leader.Standing.Won,
                leader.Standing.Lost,
                leader.Standing.Drawn
            )
            {
                WonAgainstPeople = leader.Standing.WonAgainstPeople,
                LostAgainstPeople = leader.Standing.LostAgainstPeople,
                DrawnAgainstPeople = leader.Standing.DrawnAgainstPeople,
                BotId = leader.Standing.BotId,
            }
        ),
    ];

    /// <summary>A player in their place on a leaderboard, their Elo rounded.</summary>
    private static StandingDto ToStandingDto(Leader leader, int index) =>
        new(
            index + 1,
            leader.Name,
            (int)Math.Round(leader.Standing.Elo, MidpointRounding.AwayFromZero),
            leader.Standing.Experience,
            leader.Standing.Won,
            leader.Standing.Lost,
            leader.Standing.Drawn,
            leader.Abandoned
        )
        {
            Points = leader.Standing.Points,
            WonAgainstBots = leader.Standing.WonAgainstBots,
            LostAgainstBots = leader.Standing.LostAgainstBots,
            DrawnAgainstBots = leader.Standing.DrawnAgainstBots,
            PlayerId = leader.Standing.Player.Value,
        };

    /// <summary>
    /// The page asked for of the games everyone <paramref name="wanted"/> is at, and <paramref name="player"/> too, if
    /// asked: at a seat they claimed signed in.
    /// </summary>
    private static async Task<GamePage> Matching(
        GameQuery query,
        ImmutableList<string> wanted,
        string? player,
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        ImmutableList<GameListing> all = await games.All(query, cancellationToken);
        ImmutableDictionary<HumanSeat, string> names =
            await players.NamesOf(all.SelectMany(game => game.Controllers), cancellationToken);
        ImmutableList<GameListing> matching =
        [
            .. all.Where(game =>
                PlayerPatterns.AreAllAt(wanted, game, names, games.Bots) && IsAt(player, game)
            ),
        ];
        return new GamePage([.. matching.Skip(query.Skipped).Take(query.PageSize)], matching.Count);
    }

    /// <summary>Whether the player, by id, claimed a seat at the game signed in; yes, if none is asked for.</summary>
    private static bool IsAt(string? player, GameListing game) =>
        player is null
        || game.Controllers.Any(seat => seat is HumanSeat { Claimant: { } claimant } && claimant.Value == player);

    /// <summary>The game as its seats see it, naming the bots and whoever holds each person's seat.</summary>
    private static async Task<GameViewDto> ViewOf(
        GameSnapshot snapshot,
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    ) =>
        GameViews.ViewOf(
            snapshot,
            games.Bots,
            await players.NamesOf(snapshot.Record.Controllers, cancellationToken)
        );

    private static async Task<Created<GameCreatedDto>> Create(
        CreateGameRequest request,
        GameService games,
        ChatService chat,
        CancellationToken cancellationToken
    )
    {
        ImmutableList<SeatPlan> seats =
            [.. request.Bots.Select(bot => new SeatPlan(bot) { IsForPlayers = request.IsForPlayers })];
        Dealing dealing = new(request.AreArmiesAlike, request.ArmiesOf is { } armies ? new GameId(armies) : null);
        GameId id = await games.Create(request.RulesetId, seats, request.Scenario, dealing, cancellationToken);
        if (request.Continuing is { } previous)
        {
            chat.Continue(id, new GameId(previous));
        }

        return TypedResults.Created("/" + GameRoutes.Game(id.Value), new GameCreatedDto(id.Value));
    }

    private static async Task<GameViewDto> Get(
        string gameId,
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        GameSnapshot snapshot = await games.Get(new GameId(gameId), cancellationToken);
        return await ViewOf(snapshot, games, players, cancellationToken);
    }

    private static async Task<GameHistoryDto> History(
        string gameId,
        GameService games,
        CancellationToken cancellationToken
    )
    {
        GameSnapshot snapshot = await games.Get(new GameId(gameId), cancellationToken);
        return GameViews.HistoryOf(GameHistory.Of(snapshot));
    }

    private static async Task<ImmutableList<GameHistoryDto>> Histories(
        GameService games,
        CancellationToken cancellationToken
    )
    {
        ImmutableList<GameSnapshot> snapshots = await games.Snapshots(cancellationToken);
        return
        [
            .. snapshots
                .Where(snapshot => snapshot.State.Outcome is not null)
                .Select(snapshot => GameViews.HistoryOf(GameHistory.Of(snapshot))),
        ];
    }

    private static async Task<GameViewDto> Advance(
        string gameId,
        AdvanceRequest request,
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        GameSnapshot snapshot = await games.Advance(new GameId(gameId), request.Version, cancellationToken);
        return await ViewOf(snapshot, games, players, cancellationToken);
    }

    /// <summary>Has bots take the open seats, for whoever holds the request token's seat, when they may.</summary>
    private static async Task<GameViewDto> Fill(
        string gameId,
        HttpRequest request,
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        GameId id = new(gameId);
        GameSnapshot snapshot = await games.FillOpenSeats(id, SeatToken.From(request, id), cancellationToken);
        return await ViewOf(snapshot, games, players, cancellationToken);
    }

    /// <summary>Has a bot take the seat of one away, for whoever holds the request token's seat, if they may.</summary>
    private static async Task<GameViewDto> Replace(
        string gameId,
        HttpRequest request,
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        GameId id = new(gameId);
        GameSnapshot snapshot = await games.ReplaceAway(id, SeatToken.From(request, id), cancellationToken);
        return await ViewOf(snapshot, games, players, cancellationToken);
    }

    /// <summary>
    /// Claims the seat: for the player the request's token stands for, if it carries one (refused, if it stands
    /// for no one); else for a guest, by the nickname the body gives, if it gives one. An AI's token claims it as an
    /// AI's, whatever the body says.
    /// </summary>
    private static async Task<SeatClaimedDto> Claim(
        string gameId,
        int seat,
        ClaimSeatRequest? claim,
        HttpRequest request,
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        PlayerId? claimant = await ClaimantOf(request, players, cancellationToken);
        string token = await games.Claim(
            new GameId(gameId),
            new SeatId(seat),
            claimant,
            claim?.Nickname,
            claim?.IsAi == true || PlayerService.IsAiToken(SeatToken.OrNull(request)),
            cancellationToken
        );
        return new SeatClaimedDto(token);
    }

    /// <summary>
    /// Says anew who holds the seat the body's token holds: the player the request's token stands for, if it carries
    /// one (refused, if it stands for no one); else a guest, by the nickname the body gives, if it gives one.
    /// </summary>
    private static async Task<NoContent> Identify(
        string gameId,
        int seat,
        IdentifySeatRequest identify,
        HttpRequest request,
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        PlayerId? claimant = await ClaimantOf(request, players, cancellationToken);
        await games.Identify(
            new GameId(gameId),
            new SeatId(seat),
            identify.Token,
            claimant,
            identify.Nickname,
            cancellationToken
        );
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Hands the player the request's token stands for a new token for the seat they claimed: refused, if it carries
    /// none, or stands for no one.
    /// </summary>
    private static async Task<SeatClaimedDto> TakeBack(
        string gameId,
        int seat,
        HttpRequest request,
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        PlayerId player = await ClaimantOf(request, players, cancellationToken) ?? throw new UnknownPlayerException();
        string token = await games.TakeBack(new GameId(gameId), new SeatId(seat), player, cancellationToken);
        return new SeatClaimedDto(token);
    }

    /// <summary>
    /// Hands the seat a bot plays, and a token to hold it by, as a claim does: to the player the request's token
    /// stands for, if it carries one (refused, if it stands for no one); else to a guest, by the nickname the body
    /// gives, if it gives one.
    /// </summary>
    private static async Task<SeatClaimedDto> TakeOver(
        string gameId,
        int seat,
        ClaimSeatRequest? claim,
        HttpRequest request,
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        PlayerId? claimant = await ClaimantOf(request, players, cancellationToken);
        string token = await games.TakeOver(
            new GameId(gameId),
            new SeatId(seat),
            claimant,
            claim?.Nickname,
            cancellationToken
        );
        return new SeatClaimedDto(token);
    }

    /// <summary>The player the request's token stands for, if it carries one; refused, if it is no one's.</summary>
    private static async Task<PlayerId?> ClaimantOf(
        HttpRequest request,
        PlayerService players,
        CancellationToken cancellationToken
    ) =>
        SeatToken.OrNull(request) is { } playerToken
            ? (await players.Find(playerToken, cancellationToken) ?? throw new UnknownPlayerException()).Id
            : null;

    private static async Task<GameViewDto> Submit(
        string gameId,
        ActionDto action,
        HttpRequest request,
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        GameId id = new(gameId);
        ActionDescriptor descriptor = new(action.Kind, action.Arguments);
        string token = SeatToken.From(request, id);
        GameSnapshot snapshot = await games.Submit(id, token, descriptor, cancellationToken);
        return await ViewOf(snapshot, games, players, cancellationToken);
    }

    private static async Task<ImmutableList<ChatMessageDto>> Chat(
        string gameId,
        int? after,
        HttpRequest request,
        ChatService chat,
        CancellationToken cancellationToken
    )
    {
        ImmutableList<ChatMessage> said =
            await chat.Read(new GameId(gameId), SeatToken.OrNull(request), after ?? 0, cancellationToken);
        return [.. said.Select(ChatViews.MessageOf)];
    }

    private static async Task<ChatMessageDto> Say(
        string gameId,
        SayRequest say,
        HttpRequest request,
        ChatService chat,
        CancellationToken cancellationToken
    )
    {
        GameId id = new(gameId);
        string token = SeatToken.From(request, id);
        ChatMessage message =
            await chat.Say(id, token, ChatViews.ChannelOf(say.Channel), say.Text, cancellationToken);
        return ChatViews.MessageOf(message);
    }
}
